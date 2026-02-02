import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../search-workspace.context-token.js';
import { UmbSearchQueryRepository } from '../../../repositories/search-query.repository.js';
import type { UmbSearchRequest, UmbSearchResult } from '../../../types.js';
import { html, customElement, state, css, repeat, when } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { debounce } from '@umbraco-cms/backoffice/utils';

@customElement('umb-search-index-search-box')
export class UmbSearchIndexSearchBoxElement extends UmbLitElement {
  #workspaceContext?: typeof UMB_SEARCH_WORKSPACE_CONTEXT.TYPE;
  #queryRepository = new UmbSearchQueryRepository(this);
  #inputValue = ''; // Non-reactive property for input value
  #debouncedUpdateQuery = debounce((value: string) => {
    this._searchQuery = value;
  }, 300);

  @state()
  private _indexAlias?: string;

  @state()
  private _searchQuery = '';

  @state()
  private _searchResults?: UmbSearchResult;

  @state()
  private _isSearching = false;

  @state()
  private _error?: string;

  constructor() {
    super();

    this.consumeContext(UMB_SEARCH_WORKSPACE_CONTEXT, (context) => {
      this.#workspaceContext = context;
      this.#observeIndexAlias();
    });
  }

  #observeIndexAlias() {
    this.observe(
      this.#workspaceContext?.name,
      (alias) => {
        this._indexAlias = alias ?? 'Unknown';
      },
      '_observeIndexAlias'
    );
  }

  async #handleSearch() {
    // Sync state with current input value
    this._searchQuery = this.#inputValue;

    if (!this._indexAlias || !this._searchQuery.trim()) {
      this._searchResults = undefined;
      return;
    }

    this._isSearching = true;
    this._error = undefined;

    const request: UmbSearchRequest = {
      indexAlias: this._indexAlias,
      query: this._searchQuery,
      skip: 0,
      take: 10,
    };

    const { data, error } = await this.#queryRepository.search(request);

    if (error) {
      this._error = error.message;
      this._searchResults = undefined;
    } else {
      this._searchResults = data;
    }

    this._isSearching = false;
  }

  #handleInputChange(e: Event) {
    const input = e.target as HTMLInputElement;
    this.#inputValue = input.value;
    this.#debouncedUpdateQuery(input.value);
  }

  #handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      this.#handleSearch();
    }
  }

  override render() {
    return html`
      <uui-box headline=${this.localize.term('search_searchBox')}>
        <div class="search-container">
          <div class="search-input-row">
            <uui-input
              .value=${this.#inputValue}
              @input=${this.#handleInputChange}
              @keydown=${this.#handleKeyDown}
              placeholder=${this.localize.term('search_searchPlaceholder', 'Search index...')}
              label="Search">
            </uui-input>
            <uui-button
              look="primary"
              color="positive"
              @click=${this.#handleSearch}
              ?disabled=${this._isSearching || !this.#inputValue.trim()}>
              <umb-localize key="search_searchButton">Search</umb-localize>
            </uui-button>
          </div>

          ${when(this._isSearching, () => html`<uui-loader></uui-loader>`)}
          ${when(this._error, () => html`<div class="error-message">${this._error}</div>`)}
          ${this.#renderResults()}
        </div>
      </uui-box>
    `;
  }

  #renderResults() {
    if (!this._searchResults) return '';

    if (this._searchResults.total === 0) {
      return html`
        <div class="no-results">
          <umb-localize key="search_noResults">No results found</umb-localize>
        </div>
      `;
    }

    return html`
      <div class="results-container">
        <div class="results-header">
          <strong>
            <umb-localize key="search_resultsCount" .args=${[this._searchResults.total]}>
              Found ${this._searchResults.total} results
            </umb-localize>
          </strong>
        </div>
        <div class="results-list">
          ${repeat(
            this._searchResults.documents,
            (doc) => doc.id,
            (doc) => html`
              <div class="result-item">
                <div class="result-id">
                  <strong>ID:</strong> ${doc.id}
                </div>
                <div class="result-type">
                  <uui-tag look="placeholder">
                    ${doc.objectType || 'Unknown'}
                  </uui-tag>
                </div>
              </div>
            `
          )}
        </div>
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
      }

      .results-list {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-2);
      }

      .result-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: var(--uui-size-space-3);
        background-color: var(--uui-color-surface);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
      }

      .result-id {
        font-family: var(--uui-font-family-monospace);
        font-size: var(--uui-type-small-size);
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
