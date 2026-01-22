import {
  UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS,
  UMB_SEARCH_ROOT_COLLECTION_ALIAS,
} from '../constants.js';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collection',
    kind: 'default',
    name: 'Umbraco Search - Root Collection',
    alias: UMB_SEARCH_ROOT_COLLECTION_ALIAS,
    meta: {
      repositoryAlias: UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS
    },
  },
  {
    type: 'collectionView',
    name: 'Umbraco Search - Root Collection View',
    alias: 'Umbraco.Search.CollectionView.Root',
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
