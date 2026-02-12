import { UmbSearchExamineProviderRepository } from './examine-provider.repository.js';
import type { ExamineField, ExamineIndexDocument } from './types.js';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { html, when, css, nothing, state } from '@umbraco-cms/backoffice/external/lit';

import './document-fields.element.js';

const CULTURE_FIELD_NAME = 'Sys_Culture';
const INVARIANT_CULTURE = 'none';

export interface ShowFieldsModalData {
  searchDocument: { unique: string };
  indexAlias: string;
  culture?: string;
}

interface CultureDocument {
  culture: string;
  label: string;
  fields: Array<ExamineField>;
}

export class UmbSearchExamineShowFieldsModal extends UmbModalBaseElement<ShowFieldsModalData> {
  @state()
  private _cultureDocuments: Array<CultureDocument> = [];

  @state()
  private _activeCulture?: string;

  @state()
  private _isLoading = true;

  #repository = new UmbSearchExamineProviderRepository(this);

  override firstUpdated() {
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

    this._cultureDocuments = (data?.documents ?? []).map((doc) => this.#mapCultureDocument(doc));

    // sort so the selected culture comes first (if present)
    const preferredCulture = this.data?.culture;
    if (preferredCulture) {
      this._cultureDocuments.sort((a, b) => {
        const aMatch = a.culture === preferredCulture ? -1 : 0;
        const bMatch = b.culture === preferredCulture ? -1 : 0;
        return aMatch - bMatch;
      });
    }

    this._activeCulture = this._cultureDocuments[0]?.culture;
    this._isLoading = false;
  }

  #mapCultureDocument(doc: ExamineIndexDocument): CultureDocument {
    const cultureField = doc.fields.find((f) => f.name === CULTURE_FIELD_NAME);
    const culture = cultureField?.values[0] ?? INVARIANT_CULTURE;
    return {
      culture,
      label: culture === INVARIANT_CULTURE ? 'Invariant' : culture,
      fields: doc.fields,
    };
  }

  get #hasMultipleCultures(): boolean {
    return this._cultureDocuments.length > 1;
  }

  #renderCultureTabs() {
    if (!this.#hasMultipleCultures) return nothing;
    return html`
      <uui-tab-group slot="navigation">
        ${this._cultureDocuments.map(
          (doc) => html`
            <uui-tab
              label=${doc.label}
              ?active=${this._activeCulture === doc.culture}
              @click=${() => (this._activeCulture = doc.culture)}>
              <uui-icon slot="icon" name="icon-globe"></uui-icon>
              ${doc.label}
            </uui-tab>
          `,
        )}
      </uui-tab-group>
    `;
  }

  #renderDocuments() {
    return this._cultureDocuments.map(
      (doc) => html`
        <umb-search-examine-document-fields
          .fields=${doc.fields}
          ?hidden=${this.#hasMultipleCultures && doc.culture !== this._activeCulture}
        ></umb-search-examine-document-fields>
      `,
    );
  }

  override render() {
    return html`
      <umb-body-layout headline=${this.localize.term('searchExamine_headline')}>
        ${this.#renderCultureTabs()}
        <uui-scroll-container id="field-viewer">
          ${when(
            this._isLoading,
            () => html`<uui-loader></uui-loader>`,
            () => this.#renderDocuments(),
          )}
        </uui-scroll-container>
        <div slot="actions">
          <uui-button
            look="primary"
            label=${this.localize.term('general_close')}
            @click=${() => this.modalContext?.reject()}
          ></uui-button>
        </div>
      </umb-body-layout>
    `;
  }

  static override styles = [
    css`
      #field-viewer {
        height: 100%;
        width: 100%;
      }

      umb-search-examine-document-fields[hidden] {
        display: none;
      }

      uui-tab-group {
        --uui-tab-divider: var(--uui-color-border);
        --uui-tab-text: var(--uui-color-text-alt);
        --uui-tab-text-active: var(--uui-color-default);
        --uui-tab-text-hover: var(--uui-color-default);
        border-left: 1px solid var(--uui-color-border);
        border-right: 1px solid var(--uui-color-border);
      }
    `,
  ];
}

export { UmbSearchExamineShowFieldsModal as element };

customElements.define('umb-search-examine-show-fields-modal', UmbSearchExamineShowFieldsModal);
