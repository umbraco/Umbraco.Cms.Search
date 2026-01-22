import { IndexModel } from '../../api';
import { UmbSearchIndex } from '../types.js';
import { UmbSearchReactiveController } from "../reactivity/search-reactive.controller.ts";
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UMB_COLLECTION_CONTEXT, UmbDefaultCollectionContext } from '@umbraco-cms/backoffice/collection';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UmbTableColumn, UmbTableConfig, UmbTableItem } from '@umbraco-cms/backoffice/components';

@customElement('umb-search-root-collection-view')
export default class UmbSearchRootCollectionView extends UmbLitElement {
  @state()
  private _tableItems: Array<UmbTableItem> = [];

  private _tableConfig: UmbTableConfig = {
    allowSelection: false,
  };

  private _tableColumns: Array<UmbTableColumn> = [
    {
      name: this.localize.term('search_tableColumnAlias'),
      alias: 'indexAlias',
    },
    {
      name: this.localize.term('search_tableColumnHealthStatus'),
      alias: 'healthStatus'
    },
    {
      name: this.localize.term('search_tableColumnDocumentCount'),
      alias: 'documentCount',
    },
    {
      name: '',
      alias: 'actions',
      align: 'right'
    },
  ];

  #collectionContext?: UmbDefaultCollectionContext<UmbSearchIndex, never>;

  constructor() {
    super();

    this.consumeContext(UMB_COLLECTION_CONTEXT, (instance) => {
      this.#collectionContext = instance;
      this.#observeCollectionItems();
    });

    new UmbSearchReactiveController(this);
  }

  override render() {
    return html`
      <umb-table .config=${this._tableConfig} .columns=${this._tableColumns} .items=${this._tableItems}></umb-table>
    `;
  }

  #observeCollectionItems() {
    this.observe(this.#collectionContext?.items, (items) => {
      this.#createTable(items);
    }, '_itemsObserver')
  }

  #createTable(items: IndexModel[]) {
    this._tableItems = items.map(item => {
      return {
        id: item.indexAlias,
        icon: this.#healthStatusIcon(item),
        data: [
          {
            columnAlias: 'indexAlias',
            value: item.indexAlias
          },
          {
            columnAlias: 'healthStatus',
            value: this.localize.term('search_healthStatus', item.healthStatus),
          },
          {
            columnAlias: 'documentCount',
            value: this.localize.term('search_documentCount', this.localize.number(item.documentCount)),
          },
          // TODO: Extension point?
          /*{
            columnAlias: 'actions',
            value: html`<uui-copy-text-button .text="${item.key}" label="Copy key"></uui-copy-text-button>`
          }*/
        ]
      }
    })
  }

  #healthStatusIcon(item: IndexModel) {
    switch (item.healthStatus) {
      case 'Healthy':
        return 'icon-check color-green';
      case 'Rebuilding':
        return 'icon-time color-yellow';
      case 'Empty':
        return 'icon-check color-yellow';
      default:
        // Corrupted or any other status
        return 'icon-alert color-red';
    }
  }

  static override readonly styles = [
    UmbTextStyles,
  ];
}
