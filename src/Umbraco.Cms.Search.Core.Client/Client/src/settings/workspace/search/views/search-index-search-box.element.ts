import { UmbSearchQueryRepository } from '../../../repositories/search-query.repository.js';
import type { UmbSearchRequest, UmbSearchResult, UmbHealthStatusModel } from '../../../types.js';
import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../search-workspace.context-token.js';
import { UMB_SEARCH_DOCUMENT_ENTITY_TYPE } from '@umbraco-cms/search/global';

import {
  css,
  customElement,
  html,
  nothing,
  state,
  when,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { debounce, UmbPaginationManager } from '@umbraco-cms/backoffice/utils';
import type {
  UmbTableColumn,
  UmbTableConfig,
  UmbTableItem,
} from '@umbraco-cms/backoffice/components';
import { UmbModalRouteRegistrationController } from '@umbraco-cms/backoffice/router';
import { UMB_WORKSPACE_MODAL } from '@umbraco-cms/backoffice/workspace';

const PAGE_SIZE = 10;

@customElement('umb-search-index-search-box')
export class UmbSearchIndexSearchBoxElement extends UmbLitElement {
  #workspaceContext?: typeof UMB_SEARCH_WORKSPACE_CONTEXT.TYPE;
  #queryRepository = new UmbSearchQueryRepository(this);
  #inputValue = ''; // Non-reactive property for input value
  #routeBuilder?: (params: { entityType: string }) => string; // Route builder function
  #debouncedSearch = debounce(() => {
    void this.#handleSearch();
  }, 300);

  #pagination = new UmbPaginationManager();
  #initialPage?: number;

  private _tableConfig: UmbTableConfig = {
    allowSelection: false,
  };

  private _tableColumns: Array<UmbTableColumn> = [
    {
      name: this.localize.term('search_tableColumnName'),
      alias: 'name',
    },
    {
      name: this.localize.term('search_tableColumnEntityType'),
      alias: 'entityType',
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

  @state()
  private _currentPage = 1;

  @state()
  private _totalPages = 1;

  constructor() {
    super();

    this.#pagination.setPageSize(PAGE_SIZE);

    this.observe(
      this.#pagination.currentPage,
      (page) => {
        this._currentPage = page;
      },
      '_observeCurrentPage',
    );

    this.observe(
      this.#pagination.totalPages,
      (totalPages) => {
        this._totalPages = totalPages;
      },
      '_observeTotalPages',
    );

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

  override connectedCallback() {
    super.connectedCallback();
    this.#readUrlParams();
  }

  #readUrlParams() {
    const url = new URL(window.location.href);
    const query = url.searchParams.get('query');
    const page = url.searchParams.get('page');

    if (query) {
      this.#inputValue = query;
      this._searchQuery = query;

      if (page) {
        const pageNumber = parseInt(page, 10);
        if (!isNaN(pageNumber) && pageNumber >= 1) {
          this.#initialPage = pageNumber;
        }
      }

      // Trigger search after microtask to ensure context is ready
      void this.updateComplete.then(() => {
        void this.#handleSearch();
      });
    }
  }

  #updateUrlParams() {
    const url = new URL(window.location.href);

    if (this._searchQuery.trim()) {
      url.searchParams.set('query', this._searchQuery);
      const currentPage = this.#pagination.getCurrentPageNumber();
      if (currentPage > 1) {
        url.searchParams.set('page', String(currentPage));
      } else {
        url.searchParams.delete('page');
      }
    } else {
      url.searchParams.delete('query');
      url.searchParams.delete('page');
    }

    history.replaceState(null, '', url.toString());
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
      this.#updateUrlParams();
      return;
    }

    this._isSearching = true;
    this._error = undefined;
    this._searchStatusMessage = this.localize.term('search_searching');

    const skip = this.#initialPage
      ? (this.#initialPage - 1) * PAGE_SIZE
      : this.#pagination.getSkip();

    const request: UmbSearchRequest = {
      indexAlias: this._indexAlias,
      query: this._searchQuery,
      skip,
      take: PAGE_SIZE,
    };

    const { data, error } = await this.#queryRepository.search(request);

    if (error || !data) {
      this._error = error?.message ?? 'An error occurred while searching';
      this._searchResults = undefined;
      this._tableItems = [];
      this._searchStatusMessage = this.localize.term('search_searchFailed');
    } else {
      this._searchResults = data;
      this.#pagination.setTotalItems(data.total);
      if (this.#initialPage) {
        this.#pagination.setCurrentPageNumber(this.#initialPage);
        this.#initialPage = undefined;
      }
      this.#createTableItems(data);
      this._searchStatusMessage = this.localize.term('search_searchComplete', data.total);
    }

    this._isSearching = false;
    this.#updateUrlParams();
  }

  #createTableItems(results: UmbSearchResult) {
    this._tableItems = results.documents.map((doc) => ({
      id: doc.id,
      icon: doc.icon,
      data: [
        {
          columnAlias: 'name',
          value: html`
            <div style="padding: var(--uui-size-2) 0;">
              <uui-button
                look="secondary"
                label="Open"
                aria-label=${this.localize.term('search_openEntity', doc.entityType, doc.id)}
                href=${this.#getModalUrl(doc.id, doc.entityType)}
              >
                ${doc.name}
              </uui-button>
              <div><small>${doc.id}</small></div>
            </div>
          `,
        },
        {
          columnAlias: 'entityType',
          value: doc.entityType,
        },
        {
          columnAlias: 'actions',
          value: html`<umb-entity-actions-table-column-view
            .value=${{
              entityType: UMB_SEARCH_DOCUMENT_ENTITY_TYPE,
              unique: doc.id,
              name: doc.id,
            }}
          ></umb-entity-actions-table-column-view>`,
        },
      ],
    }));
  }

  #getModalUrl(id: string, entityType: string): string {
    if (!this.#routeBuilder) {
      console.error('Route builder not initialized');
      return '#';
    }

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
      this.#pagination.setCurrentPageNumber(1);
      this.#updateUrlParams();
      return;
    }

    // Reset to page 1 on new query input
    this.#pagination.setCurrentPageNumber(1);

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

  #onPageChange(event: Event) {
    const target = event.target as HTMLElement & { current: number };
    this.#pagination.setCurrentPageNumber(target.current);
    void this.#handleSearch();
  }

  #renderResults() {
    if (!this._searchResults) return nothing;

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
        ${this._totalPages > 1
          ? html`
              <uui-pagination
                .current=${this._currentPage}
                .total=${this._totalPages}
                @change=${this.#onPageChange}
                aria-label=${this.localize.term('search_paginationLabel')}
              ></uui-pagination>
            `
          : nothing}
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

      uui-pagination {
        display: flex;
        justify-content: center;
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
