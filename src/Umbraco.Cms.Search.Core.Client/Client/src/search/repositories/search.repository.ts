import { rebuild } from '../../api';
import type { UmbSearchIndex } from '../types.js';
import { UmbSearchCollectionServerDataSource } from './search-collection.server.data-source.js';
import { UmbRepositoryBase} from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import {UMB_COLLECTION_CONTEXT, UmbCollectionRepository} from '@umbraco-cms/backoffice/collection';
import {UmbSearchCollectionContext} from "../search-collection.context.ts";

export class UmbSearchCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository<UmbSearchIndex, never> {
  #collectionSource = new UmbSearchCollectionServerDataSource(this);

  async requestCollection(filter: unknown) {
    return this.#collectionSource.getCollection(filter);
  }

  async rebuildIndex(indexAlias: string): Promise<void> {
    const { error } = await tryExecute(this, rebuild({ query: { indexAlias } }));
    if (error) throw error;

    // See if we have a collection context to update the index state
    const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT);
    if (collectionContext instanceof UmbSearchCollectionContext) {
      collectionContext.setIndexState(indexAlias, 'loading');
    }
  }
}
