import { UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS } from '../constants.ts';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'repository',
    name: 'Umbraco Search Collection Repository',
    alias: UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS,
    api: () => import('@umbraco-cms/search/core').then(m => ({ default: m.UmbSearchCollectionRepository })),
  }
]
