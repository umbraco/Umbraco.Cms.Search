import type { UmbSearchIndex } from '../types.js';
import { rebuild } from '../api/index.js';
import { UmbSearchServerDataSource } from './search-detail.server.data-source.js';
import { UMB_SEARCH_DETAIL_STORE_CONTEXT } from './search-detail.store.context-token.js';
import { UmbDetailRepositoryBase } from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { client } from '@umbraco-cms/backoffice/external/backend-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export class UmbSearchDetailRepository extends UmbDetailRepositoryBase<UmbSearchIndex> {
  constructor(host: UmbControllerHost) {
    super(host, UmbSearchServerDataSource, UMB_SEARCH_DETAIL_STORE_CONTEXT);
  }

  async rebuildIndex(indexAlias: string): Promise<void> {
    const { error } = await tryExecute(this, rebuild({ query: { indexAlias }, client: client as any }));
    if (error) throw error;
  }

  override async save(model: UmbSearchIndex) {
    console.error('Saving search indexes is not supported.');
    return { data: model, error: undefined };
  }
}
