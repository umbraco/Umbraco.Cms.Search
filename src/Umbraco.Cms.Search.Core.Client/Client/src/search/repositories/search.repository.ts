import { rebuild } from '../../api';
import { UmbSearchIndex } from '../types.js';
import { UmbSearchCollectionServerDataSource } from './search-collection.server.data-source.js';
import { UmbCollectionRepository} from '@umbraco-cms/backoffice/collection';
import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';

export class UmbSearchCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository<UmbSearchIndex, never> {
  #collectionSource = new UmbSearchCollectionServerDataSource(this);

  async requestCollection(filter: never) {
    return this.#collectionSource.getCollection(filter);
  }

  async rebuildIndex(indexAlias: string): Promise<void> {
    const { error } = await tryExecute(this, rebuild({ query: { indexAlias } }));
    if (error) throw error;
  }
}
