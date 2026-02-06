import { UmbSearchExamineProviderRepository } from './examine-provider.repository.js';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { html, repeat, when, css } from '@umbraco-cms/backoffice/external/lit';

export class UmbSearchExamineShowFieldsModal extends UmbModalBaseElement {
  static properties = {
    _fields: { type: Array },
    _isLoading: { type: Boolean },
  };

  #repository = new UmbSearchExamineProviderRepository(this);

  async firstUpdated() {
    void this.#requestSearchDocumentFields();
  }

  async #requestSearchDocumentFields() {
    this._isLoading = true;
    const { data, error } = await this.#repository.requestSearchDocument(this.data?.searchDocument.unique, this.data?.indexAlias);

    if (error) {
      this._isLoading = false;
      console.error(error.message);
      return;
    }

    console.log('data', data);

    this._fields = data.fields;
    this._isLoading = false;
  }

  render() {
    return html`
      <umb-body-layout headline="Search Document Fields">
        <uui-scroll-container id="field-viewer">
          ${when(
            this._isLoading,
            () => html`<uui-loader></uui-loader>`,
            () => html`
              ${when(
                this._fields?.length > 0,
                () => html`
                  <div class="fields">
                    ${repeat(this._fields, (field) => field.key, (field) => html`<div class="field">${field}            </div>`)}
                  </div>
                `,
                () => html`<div class="empty-state">No fields found for this search document.</div>`,
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
        height: 400px;
        width: 100%;
      }

      .fields {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-3);
      }

      .field {
        padding: var(--uui-size-3);
        background-color: var(--uui-color-background);
        border-radius: var(--uui-border-radius);
        box-shadow: var(--uui-shadow-depth-1);
        font-family: var(--uui-font-family-monospace);
        font-size: var(--uui-font-size-3);
      }

      .empty-state {
        padding: var(--uui-size-3);
        text-align: center;
        color: var(--uui-color-text-muted);
      }
    `,
  ];
}

export { UmbSearchExamineShowFieldsModal as element };

customElements.define('umb-search-examine-show-fields-modal', UmbSearchExamineShowFieldsModal);
