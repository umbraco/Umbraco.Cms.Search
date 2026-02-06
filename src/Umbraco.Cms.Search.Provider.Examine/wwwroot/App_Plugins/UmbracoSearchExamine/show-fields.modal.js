import { UmbSearchExamineProviderRepository } from './examine-provider.repository.js';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { html, repeat, when, css, nothing } from '@umbraco-cms/backoffice/external/lit';

const MAX_VALUE_LENGTH = 100;

export class UmbSearchExamineShowFieldsModal extends UmbModalBaseElement {
  static properties = {
    _fields: { type: Array },
    _isLoading: { type: Boolean },
    _filterQuery: { type: String },
    _expandedFields: { type: Object },
  };

  #repository = new UmbSearchExamineProviderRepository(this);

  constructor() {
    super();
    this._filterQuery = '';
    this._expandedFields = new Set();
    this._isLoading = true;
  }

  async firstUpdated() {
    void this.#requestSearchDocumentFields();
  }

  async #requestSearchDocumentFields() {
    this._isLoading = true;
    const { data, error } = await this.#repository.requestSearchDocument(
      this.data?.searchDocument.unique,
      this.data?.indexAlias,
    );

    if (error) {
      this._isLoading = false;
      console.error(error.message);
      return;
    }

    this._fields = data.fields;
    this._isLoading = false;
  }

  get #filteredAndSortedFields() {
    if (!this._fields) return [];

    const query = this._filterQuery.toLowerCase();
    return this._fields
      .filter(
        (field) =>
          field.name.toLowerCase().includes(query) ||
          field.values.some((v) => v.toLowerCase().includes(query)),
      )
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  #onFilterInput(e) {
    this._filterQuery = e.target.value;
  }

  #toggleExpanded(fieldName) {
    if (this._expandedFields.has(fieldName)) {
      this._expandedFields.delete(fieldName);
    } else {
      this._expandedFields.add(fieldName);
    }
    this.requestUpdate();
  }

  #renderValue(field, value, index, showIndex) {
    const isLong = value.length > MAX_VALUE_LENGTH;
    const fieldKey = `${field.name}-${index}`;
    const isExpanded = this._expandedFields.has(fieldKey);
    const indexPrefix = showIndex ? html`<span class="value-index" title="Value ${index + 1}">[${index}]</span>` : nothing;

    if (!isLong) {
      return html`
        <div class="value-item">
          ${indexPrefix}
          <span class="value-content">${value}</span>
          <uui-button-copy-text class="copy-button" .text=${value} look="placeholder" compact label="Copy value"></uui-button-copy-text>
        </div>
      `;
    }

    return html`
      <div class="value-item">
        ${indexPrefix}
        <span class="value-content">
          ${isExpanded ? value : `${value.substring(0, MAX_VALUE_LENGTH)}...`}
          <button class="see-more" @click=${() => this.#toggleExpanded(fieldKey)}>
            ${isExpanded ? 'See less' : 'See more'}
          </button>
        </span>
        <uui-button-copy-text class="copy-button" .text=${value} look="placeholder" compact label="Copy value"></uui-button-copy-text>
      </div>
    `;
  }

  #renderField(field) {
    const showIndex = field.values.length > 1;

    return html`
      <tr class="field-row">
        <td class="field-name">
          <span class="field-name-text">
            ${field.name}
            ${field.type
              ? html`<uui-icon name="icon-info" title="${field.type}" class="type-icon"></uui-icon>`
              : nothing}
          </span>
          <uui-button-copy-text class="copy-button" .text=${field.name} look="placeholder" compact label="Copy name"></uui-button-copy-text>
        </td>
        <td class="field-value">
          ${field.values.map((value, index) => this.#renderValue(field, value, index, showIndex))}
        </td>
      </tr>
    `;
  }

  render() {
    return html`
      <umb-body-layout headline="Search Document Fields">
        <uui-scroll-container id="field-viewer">
          ${when(
            this._isLoading,
            () => html`<uui-loader></uui-loader>`,
            () => html`
              <div class="filter-bar">
                <uui-input
                  type="search"
                  placeholder="Filter fields by name or value..."
                  label="Filter fields by name or value"
                  .value=${this._filterQuery}
                  @input=${this.#onFilterInput}>
                  <uui-icon name="icon-search" slot="prepend" style="padding-left:var(--uui-size-space-2)"></uui-icon>
                </uui-input>
                <span class="field-count">${this.#filteredAndSortedFields.length} fields</span>
              </div>
              ${when(
                this.#filteredAndSortedFields.length > 0,
                () => html`
                  <table class="fields-table">
                    <thead>
                      <tr>
                        <th class="th-name">Name</th>
                        <th class="th-value">Value</th>
                      </tr>
                    </thead>
                    <tbody>
                      ${repeat(this.#filteredAndSortedFields, (field) => field.name, (field) => this.#renderField(field))}
                    </tbody>
                  </table>
                `,
                () => html`<div class="empty-state">No fields match your filter.</div>`,
              )}
            `,
          )}
        </uui-scroll-container>
        <div slot="actions">
          <uui-button
            look="primary"
            label=${this.localize.term('general_close')}
            @click=${() => this.modalContext?.reject()}></uui-button>
        </div>
      </umb-body-layout>
    `;
  }

  static styles = [
    css`
      #field-viewer {
        height: 100%;
        width: 100%;
      }

      .filter-bar {
        display: flex;
        align-items: center;
        gap: var(--uui-size-4);
        padding: var(--uui-size-4);
        border-bottom: 1px solid var(--uui-color-border);
        position: sticky;
        top: 0;
        background: var(--uui-color-background);
        z-index: 1;
      }

      .filter-bar uui-input {
        flex: 1;
      }

      .field-count {
        font-size: var(--uui-font-size-2);
        color: var(--uui-color-text-muted);
        white-space: nowrap;
      }

      .fields-table {
        width: 100%;
        border-collapse: collapse;
        font-size: var(--uui-font-size-3);
      }

      .fields-table th {
        text-align: left;
        padding: var(--uui-size-3) var(--uui-size-4);
        font-weight: 600;
        color: var(--uui-color-text-muted);
        border-bottom: 1px solid var(--uui-color-border);
        position: sticky;
        top: 52px;
        background: var(--uui-color-background);
      }

      .th-name {
        width: 200px;
      }

      .th-value {
        width: auto;
      }

      .field-row {
        border-bottom: 1px solid var(--uui-color-border);
      }

      .field-row:hover {
        background: var(--uui-color-surface-alt);
      }

      .field-row td {
        padding: var(--uui-size-3) var(--uui-size-4);
        vertical-align: top;
      }

      .field-name {
        display: flex;
        align-items: center;
        gap: var(--uui-size-2);
      }

      .field-name-text {
        font-weight: 600;
        font-family: var(--uui-font-family-monospace);
        word-break: break-word;
      }

      .field-name .copy-button {
        opacity: 0;
        transition: opacity 0.15s ease;
      }

      .field-name:hover .copy-button,
      .field-name:focus-within .copy-button {
        opacity: 1;
      }

      .type-icon {
        margin-left: var(--uui-size-2);
        color: var(--uui-color-text-muted);
        font-size: var(--uui-font-size-2);
        cursor: help;
        vertical-align: middle;
      }

      .field-value {
        font-family: var(--uui-font-family-monospace);
        color: var(--uui-color-text);
        line-height: 1.4;
      }

      .value-item {
        display: flex;
        align-items: flex-start;
        gap: var(--uui-size-2);
        padding: var(--uui-size-2);
        background-color: var(--uui-color-surface-alt);
        border-radius: var(--uui-border-radius);
        margin-bottom: var(--uui-size-2);
        word-break: break-word;
      }

      .value-item:last-child {
        margin-bottom: 0;
      }

      .value-content {
        flex: 1;
        min-width: 0;
      }

      .copy-button {
        opacity: 0;
        transition: opacity 0.15s ease;
        margin-left: auto;
        flex-shrink: 0;
      }

      .value-item:hover .copy-button,
      .value-item:focus-within .copy-button {
        opacity: 1;
      }

      .value-index {
        margin-right: var(--uui-size-2);
        user-select: none;
        opacity: 0.7;
        flex-shrink: 0;
        white-space: nowrap;
      }

      .see-more {
        display: inline;
        background: none;
        border: none;
        color: var(--uui-color-interactive);
        cursor: pointer;
        padding: 0;
        margin-left: var(--uui-size-2);
        font-size: var(--uui-font-size-2);
        text-decoration: underline;
        white-space: nowrap;
      }

      .see-more:hover {
        color: var(--uui-color-interactive-emphasis);
      }

      .empty-state {
        padding: var(--uui-size-6);
        text-align: center;
        color: var(--uui-color-text-muted);
      }
    `,
  ];
}

export { UmbSearchExamineShowFieldsModal as element };

customElements.define('umb-search-examine-show-fields-modal', UmbSearchExamineShowFieldsModal);
