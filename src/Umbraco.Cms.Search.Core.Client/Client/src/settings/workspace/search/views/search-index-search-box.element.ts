import { UmbSearchQueryRepository } from '../../../repositories/search-query.repository.js';
import type { UmbSearchRequest, UmbSearchResult, UmbHealthStatusModel } from '../../../types.js';
import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../search-workspace.context-token.js';

import { css, customElement, html, state, when } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { debounce } from '@umbraco-cms/backoffice/utils';
import type {
  UmbTableColumn,
  UmbTableConfig,
  UmbTableItem,
} from '@umbraco-cms/backoffice/components';
import { UmbModalRouteRegistrationController } from '@umbraco-cms/backoffice/router';
import { UMB_WORKSPACE_MODAL } from '@umbraco-cms/backoffice/workspace';

import './search-index-search-result-actions.element.js';

@customElement('umb-search-index-search-box')
export class UmbSearchIndexSearchBoxElement extends UmbLitElement {
  #workspaceContext?: typeof UMB_SEARCH_WORKSPACE_CONTEXT.TYPE;
  #queryRepository = new UmbSearchQueryRepository(this);
  #inputValue = ''; // Non-reactive property for input value
  #routeBuilder?: (params: { entityType: string }) => string; // Route builder function
  #debouncedSearch = debounce(() => {
    void this.#handleSearch();
  }, 300);

  private _tableConfig: UmbTableConfig = {
    allowSelection: false,
  };

  private _tableColumns: Array<UmbTableColumn> = [
    {
      name: this.localize.term('search_tableColumnDocumentId'),
      alias: 'documentId',
    },
    {
      name: this.localize.term('search_tableColumnObjectType'),
      alias: 'objectType',
    },
    {
      name: '',
      alias: 'actions',
      align: 'right',
    },
  ];

  @state()
  private _tableItems: Array<UmbTableItem> = [];

  @state()
  private _indexAlias?: string;

  @state()
  private _healthStatus?: UmbHealthStatusModel;

  @state()
  private _searchQuery = '';

  @state()
  private _searchResults?: UmbSearchResult;

  @state()
  private _isSearching = false;

  @state()
  private _error?: string;

  @state()
  private _searchStatusMessage = ''; // For screen reader announcements

  constructor() {
    super();

    // Register modal route for opening entities
    new UmbModalRouteRegistrationController(this, UMB_WORKSPACE_MODAL)
      .addAdditionalPath(':entityType')
      .onSetup((routingInfo) => {
        return {
          data: {
            entityType: routingInfo.entityType,
            preset: {},
          },
        };
      })
      .observeRouteBuilder((routeBuilder) => {
        // Store the route builder function to call dynamically per entity type
        this.#routeBuilder = routeBuilder;
      });

    this.consumeContext(UMB_SEARCH_WORKSPACE_CONTEXT, (context) => {
      this.#workspaceContext = context;
      this.#observeIndexAlias();
      this.#observeHealthStatus();
    });
  }

  #observeHealthStatus() {
    this.observe(
      this.#workspaceContext?.healthStatus,
      (status) => {
        this._healthStatus = status;
      },
      '_observeHealthStatus',
    );
  }

  get #isSearchDisabled(): boolean {
    return this._healthStatus !== 'Healthy';
  }

  override render() {
    return html`
      <uui-box headline=${this.localize.term('search_searchBox')}>
        <div
          class="search-container"
          role="search"
          aria-label=${this.localize.term('search_searchFormLabel', this._indexAlias)}
          aria-busy=${this._isSearching ? 'true' : 'false'}
        >
          <!-- Screen reader status announcements -->
          <div class="visually-hidden" role="status" aria-live="polite" aria-atomic="true">
            ${this._searchStatusMessage}
          </div>

          ${when(
            this.#isSearchDisabled,
            () => html`
              <div class="search-disabled-message">
                <umb-localize key="search_searchDisabled">
                  Search is disabled because the index is not healthy. Current status:
                </umb-localize>
                ${this.localize.term('search_healthStatus', this._healthStatus)}
              </div>
            `,
          )}

          <div class="search-input-row">
            <uui-input
              id="search-input"
              .value=${this.#inputValue}
              @input=${this.#handleInputChange}
              @keydown=${this.#handleKeyDown}
              ?disabled=${this.#isSearchDisabled}
              placeholder=${this.localize.term('search_searchPlaceholder')}
              label=${this.localize.term('search_searchInputLabel')}
              aria-label=${this.localize.term('search_searchInputAriaLabel', this._indexAlias)}
              aria-describedby="search-hint"
            >
            </uui-input>
            <uui-button
              look="primary"
              color="positive"
              @click=${this.#handleButtonClick}
              ?disabled=${this.#isSearchDisabled || this._isSearching || !this.#inputValue.trim()}
              aria-label=${this.localize.term('search_searchButtonAriaLabel')}
            >
              <umb-localize key="search_searchButton">Search</umb-localize>
            </uui-button>
          </div>

          <div id="search-hint" class="visually-hidden">
            <umb-localize key="search_searchHint">
              Press Enter or click Search button to execute search
            </umb-localize>
          </div>
          ${when(
            this._isSearching,
            () => html`
              <div role="status" aria-label=${this.localize.term('search_loading')}>
                <uui-loader></uui-loader>
              </div>
            `,
          )}
          ${when(
            this._error,
            () => html`
              <div class="error-message" role="alert" aria-live="assertive">${this._error}</div>
            `,
          )}
          ${this.#renderResults()}
        </div>
      </uui-box>
    `;
  }

  #observeIndexAlias() {
    this.observe(
      this.#workspaceContext?.name,
      (alias) => {
        this._indexAlias = alias ?? 'Unknown';
      },
      '_observeIndexAlias',
    );
  }

  async #handleSearch() {
    // Prevent concurrent searches
    if (this._isSearching) {
      return;
    }

    // Sync state with current input value
    this._searchQuery = this.#inputValue;

    if (!this._indexAlias || !this._searchQuery.trim()) {
      this._searchResults = undefined;
      return;
    }

    this._isSearching = true;
    this._error = undefined;
    this._searchStatusMessage = this.localize.term('search_searching');

    const request: UmbSearchRequest = {
      indexAlias: this._indexAlias,
      query: this._searchQuery,
      skip: 0,
      take: 10,
    };

    const { data, error } = await this.#queryRepository.search(request);

    if (error || !data) {
      this._error = error?.message ?? 'An error occurred while searching';
      this._searchResults = undefined;
      this._tableItems = [];
      this._searchStatusMessage = this.localize.term('search_searchFailed');
    } else {
      this._searchResults = data;
      this.#createTableItems(data);
      this._searchStatusMessage = this.localize.term('search_searchComplete', data.total);
    }

    this._isSearching = false;
  }

  #createTableItems(results: UmbSearchResult) {
    this._tableItems = results.documents.map((doc) => ({
      id: doc.unique,
      data: [
        {
          columnAlias: 'documentId',
          value: html`
            <uui-button
              look="secondary"
              label="Open"
              aria-label=${this.localize.term('search_openEntity', doc.objectType, doc.unique)}
              href=${this.#getModalUrl(doc.unique, doc.objectType)}
            >
              ${doc.unique}
            </uui-button>
          `,
        },
        {
          columnAlias: 'objectType',
          value: doc.objectType || 'Unknown',
        },
        {
          columnAlias: 'actions',
          value: html`
            <umb-search-index-search-result-actions
              .searchDocument=${doc}
              .indexAlias=${this._indexAlias!}
            ></umb-search-index-search-result-actions>
          `,
        },
      ],
    }));
  }

  #getEntityType(objectType: string): string {
    // Map UmbracoObjectTypes enum values to entity type strings
    const typeMap: Record<string, string> = {
      Document: 'document',
      Media: 'media',
      Member: 'member',
      DocumentType: 'document-type',
      MediaType: 'media-type',
      MemberType: 'member-type',
      DataType: 'data-type',
    };

    return typeMap[objectType] || objectType.toLowerCase();
  }

  #getModalUrl(id: string, objectType: string): string {
    if (!this.#routeBuilder) {
      console.error('Route builder not initialized');
      return '#';
    }

    const entityType = this.#getEntityType(objectType);
    const modalPath = this.#routeBuilder({ entityType });
    return `${modalPath}edit/${id}`;
  }

  #handleInputChange(e: Event) {
    const input = e.target as HTMLInputElement;
    this.#inputValue = input.value;

    // Clear results if input is empty
    if (!this.#inputValue.trim()) {
      this._searchResults = undefined;
      this._error = undefined;
      return;
    }

    // Trigger debounced search
    this.#debouncedSearch();
  }

  #handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      // Execute search immediately (debounced search will be skipped if already searching)
      void this.#handleSearch();
    }
  }

  #handleButtonClick() {
    // Execute search immediately (debounced search will be skipped if already searching)
    void this.#handleSearch();
  }

  #renderResults() {
    if (!this._searchResults) return '';

    if (this._searchResults.total === 0) {
      return html`
        <div class="no-results" role="status" aria-live="polite">
          <umb-localize key="search_noResults">No results found</umb-localize>
        </div>
      `;
    }

    return html`
      <div
        class="results-container"
        role="region"
        aria-label=${this.localize.term('search_resultsRegion')}
      >
        <div class="results-header" id="results-summary">
          <strong>
            <umb-localize key="search_resultsCount" .args=${[this._searchResults.total]}>
              Found ${this._searchResults.total} result${this._searchResults.total === 1 ? '' : 's'}
            </umb-localize>
          </strong>
        </div>
        <umb-table
          .config=${this._tableConfig}
          .columns=${this._tableColumns}
          .items=${this._tableItems}
          aria-describedby="results-summary"
          aria-label=${this.localize.term('search_resultsTable')}
        >
        </umb-table>
      </div>
    `;
  }
  static override styles = [
    UmbTextStyles,
    css`
      :host {
        display: block;
      }

      .search-container {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-4);
      }

      /* Visually hidden but accessible to screen readers */
      .visually-hidden {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        white-space: nowrap;
        border-width: 0;
      }

      .search-input-row {
        display: flex;
        gap: var(--uui-size-space-3);
        align-items: flex-end;
      }

      uui-input {
        flex: 1;
      }

      .error-message {
        padding: var(--uui-size-space-4);
        background-color: var(--uui-color-danger-standalone);
        color: var(--uui-color-danger-contrast);
        border-radius: var(--uui-border-radius);
      }

      .search-disabled-message {
        color: var(--uui-color-danger);
        font-size: 0.875rem;
      }

      .no-results {
        padding: var(--uui-size-space-4);
        text-align: center;
        color: var(--uui-color-text-alt);
      }

      .results-container {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-3);
      }

      .results-header {
        padding-bottom: var(--uui-size-space-2);
        border-bottom: 1px solid var(--uui-color-border);
        margin-bottom: var(--uui-size-space-3);
      }
    `,
  ];
}

export default UmbSearchIndexSearchBoxElement;

declare global {
  interface HTMLElementTagNameMap {
    'umb-search-index-search-box': UmbSearchIndexSearchBoxElement;
  }
}
