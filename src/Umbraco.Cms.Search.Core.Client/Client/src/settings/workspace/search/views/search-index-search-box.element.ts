import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../search-workspace.context-token.js';
import { search } from '../../../api';
import type { DocumentModel, SearchResultModel } from '../../../api';
import { html, customElement, state, css } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { client } from '@umbraco-cms/backoffice/external/backend-api';

@customElement('umb-search-index-search-box')
export class UmbSearchIndexSearchBoxElement extends UmbLitElement {
  #workspaceContext?: typeof UMB_SEARCH_WORKSPACE_CONTEXT.TYPE;

  @state()
  private _indexAlias?: string;

  @state()
  private _searchQuery = '';

  @state()
  private _searchResults?: SearchResultModel;

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
    if (!this._indexAlias || !this._searchQuery.trim()) {
      this._searchResults = undefined;
      return;
    }

    this._isSearching = true;
    this._error = undefined;

    try {
      const result = await search({
        body: {
          indexAlias: this._indexAlias,
          query: this._searchQuery,
        },
        query: {
          skip: 0,
          take: 10,
        },
        client: client as any,
      });

      this._searchResults = result.data;
    } catch (error) {
      this._error = error instanceof Error ? error.message : 'An error occurred while searching';
      this._searchResults = undefined;
    } finally {
      this._isSearching = false;
    }
  }

  #handleInputChange(e: Event) {
    const input = e.target as HTMLInputElement;
    this._searchQuery = input.value;
  }

  #handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      this.#handleSearch();
    }
  }

  #getObjectTypeLabel(objectType: DocumentModel['objectType']): string {
    return objectType.replace(/([A-Z])/g, ' $1').trim();
  }

  override render() {
    return html`
      <uui-box headline=${this.localize.term('search_searchBox')}>
        <div class="search-container">
          <div class="search-input-row">
            <uui-input
              .value=${this._searchQuery}
              @input=${this.#handleInputChange}
              @keydown=${this.#handleKeyDown}
              placeholder=${this.localize.term('search_searchPlaceholder', 'Search index...')}
              label="Search">
            </uui-input>
            <uui-button
              look="primary"
              color="positive"
              @click=${this.#handleSearch}
              ?disabled=${this._isSearching || !this._searchQuery.trim()}>
              <umb-localize key="search_searchButton">Search</umb-localize>
            </uui-button>
          </div>

          ${this._isSearching ? html`<uui-loader></uui-loader>` : ''}
          ${this._error ? html`<div class="error-message">${this._error}</div>` : ''}
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
          ${this._searchResults.documents.map(
            (doc) => html`
              <div class="result-item">
                <div class="result-id">
                  <strong>ID:</strong> ${doc.id}
                </div>
                <div class="result-type">
                  <uui-tag look="placeholder">
                    ${this.#getObjectTypeLabel(doc.objectType)}
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
