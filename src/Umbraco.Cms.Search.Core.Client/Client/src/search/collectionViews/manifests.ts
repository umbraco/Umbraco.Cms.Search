import {
  UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS,
  UMB_SEARCH_COLLECTION_VIEW_ALIAS,
  UMB_SEARCH_ROOT_COLLECTION_ALIAS,
} from '../constants.js';
import { UmbSearchCollectionContext } from '../search-collection.context.js';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collection',
    kind: 'default',
    name: 'Umbraco Search - Root Collection',
    alias: UMB_SEARCH_ROOT_COLLECTION_ALIAS,
    api: UmbSearchCollectionContext,
    meta: {
      repositoryAlias: UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS
    },
  },
  {
    type: 'collectionView',
    name: 'Umbraco Search - Root Collection View',
    alias: UMB_SEARCH_COLLECTION_VIEW_ALIAS,
    element: () => import('./search-root-collection-view.element.js'),
    meta: {
      label: '#search_collectionViewRootHeader',
      icon: 'icon-search',
      pathName: 'table'
    },
    conditions: [
      {
        alias: 'Umb.Condition.CollectionAlias',
        match: UMB_SEARCH_ROOT_COLLECTION_ALIAS
      }
    ]
  }
];
