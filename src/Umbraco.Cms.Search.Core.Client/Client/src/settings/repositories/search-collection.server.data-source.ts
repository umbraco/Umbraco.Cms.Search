import { indexes } from '../api';
import type { UmbSearchIndex, UmbSearchCollectionDataSource } from '../types.js';
import { UMB_SEARCH_INDEX_ENTITY_TYPE } from '@umbraco-cms/search/global';

import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { client } from "@umbraco-cms/backoffice/external/backend-api";
import type { UmbDataSourceResponse, UmbPagedModel } from '@umbraco-cms/backoffice/repository';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export class UmbSearchCollectionServerDataSource implements UmbSearchCollectionDataSource {
  readonly #host;

  constructor(host: UmbControllerHost) {
    this.#host = host;
  }

  async getCollection(_filter: unknown): Promise<UmbDataSourceResponse<UmbPagedModel<UmbSearchIndex>>> {
    const { data, error } = await tryExecute(this.#host, indexes({
      client: client as any
    }));

    if (error || !data) {
      return { error };
    }

    const items = data.items.map<UmbSearchIndex>((item) => (
      {
        unique: item.indexAlias,
        name: item.indexAlias,
        documentCount: item.documentCount,
        healthStatus: item.healthStatus,
        entityType: UMB_SEARCH_INDEX_ENTITY_TYPE,
        state: 'idle'
      }
    ));

    return { data: { items, total: data.total } };
  }
}
