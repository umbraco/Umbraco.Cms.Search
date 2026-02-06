import type { UmbSearchDocument } from '../../../types.js';
import { ManifestEntityAction } from '@umbraco-cms/backoffice/entity-action';
import { customElement, html, property } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_SEARCH_DOCUMENT_ENTITY_TYPE } from '@umbraco-cms/search/global';

@customElement('umb-search-index-search-result-actions')
export class UmbSearchIndexSearchResultActionsElement extends UmbLitElement {
  @property({ type: Object, attribute: false })
  searchDocument!: UmbSearchDocument;

  @property({ type: String, attribute: false })
  indexAlias!: string;

  render() {
    return html` <umb-extension-with-api-slot
      type="entityAction"
      .filter=${(ext: ManifestEntityAction) =>
        ext.forEntityTypes.includes(UMB_SEARCH_DOCUMENT_ENTITY_TYPE)}
      .apiArgs=${[{ searchDocument: this.searchDocument, indexAlias: this.indexAlias }]}
    ></umb-extension-with-api-slot>`;
  }
}
