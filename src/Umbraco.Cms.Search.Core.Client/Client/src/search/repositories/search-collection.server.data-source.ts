import { indexes } from '../../api';
import type { UmbSearchIndex } from '../types.js';
import { UmbSearchCollectionDataSource } from './types.js';
import { UMB_SEARCH_INDEX_ENTITY_TYPE } from '../constants.js';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export class UmbSearchCollectionServerDataSource implements UmbSearchCollectionDataSource {
  readonly #host;

  constructor(host: UmbControllerHost) {
    this.#host = host;
  }

  async getCollection(_filter: never) {
    const { data, error } = await tryExecute(this.#host, indexes());

    if (error || !data) {
      return { error };
    }

    const items = data.items.map<UmbSearchIndex>((item) => (
      {
        ...item,
        entityType: UMB_SEARCH_INDEX_ENTITY_TYPE,
        unique: item.indexAlias,
      })
    );

    return { data: { items, total: data.total } };
  }
}
