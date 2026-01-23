import { UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS } from '../constants.ts';
import { UmbSearchCollectionRepository } from './search.repository.ts';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'repository',
    name: 'Umbraco Search Collection Repository',
    alias: UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS,
    api: UmbSearchCollectionRepository,
  }
]
