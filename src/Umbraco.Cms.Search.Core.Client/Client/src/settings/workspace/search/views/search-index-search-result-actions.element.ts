import type { UmbSearchDocument } from '../../../types.js';
import type { ManifestEntityAction } from '@umbraco-cms/backoffice/entity-action';
import { css, customElement, html, property, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbExtensionsApiInitializer } from '@umbraco-cms/backoffice/extension-api';
import { umbExtensionsRegistry } from '@umbraco-cms/backoffice/extension-registry';
import { UMB_SEARCH_DOCUMENT_ENTITY_TYPE } from '@umbraco-cms/search/global';

interface ActionMeta {
  icon?: string;
  label?: string;
  look?: string;
  color?: string;
}

interface ActionController {
  manifest?: {
    name?: string;
    meta?: ActionMeta;
  };
  api?: {
    execute: () => Promise<void>;
  };
}

@customElement('umb-search-index-search-result-actions')
export class UmbSearchIndexSearchResultActionsElement extends UmbLitElement {
  @property({ type: Object, attribute: false })
  searchDocument!: UmbSearchDocument;

  @property({ type: String, attribute: false })
  indexAlias!: string;

  @state()
  private _actions: Array<ActionController> = [];

  override connectedCallback() {
    super.connectedCallback();
    this.#initializeActions();
  }

  #initializeActions() {
    new UmbExtensionsApiInitializer(
      this,
      umbExtensionsRegistry,
      'entityAction',
      [{ searchDocument: this.searchDocument, indexAlias: this.indexAlias }],
      (ext) =>
        (ext as unknown as ManifestEntityAction).forEntityTypes.includes(
          UMB_SEARCH_DOCUMENT_ENTITY_TYPE,
        ),
      (controllers) => {
        this._actions = controllers as unknown as Array<ActionController>;
      },
    );
  }

  #onActionClick(event: Event, action: ActionController) {
    event.stopPropagation();
    void action.api?.execute();
  }

  override render() {
    if (this._actions.length === 0) return html``;

    return html`
      <uui-button-group>
        ${this._actions.map(
          (action) => html`
            <uui-button
              compact
              look=${action.manifest?.meta?.look ?? 'secondary'}
              color=${action.manifest?.meta?.color ?? 'default'}
              @click=${(event: Event) => this.#onActionClick(event, action)}
            >
              <uui-icon name=${action.manifest?.meta?.icon ?? 'icon-settings'}></uui-icon>
              ${this.localize.string(action.manifest?.meta?.label ?? action.manifest?.name ?? '')}
            </uui-button>
          `,
        )}
      </uui-button-group>
    `;
  }

  static override readonly styles = css`
    :host {
      display: contents;
    }
  `;
}
