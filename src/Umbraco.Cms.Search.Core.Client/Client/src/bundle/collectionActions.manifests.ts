import { UMB_SEARCH_ROOT_COLLECTION_ALIAS } from '../constants.js';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collectionAction',
    kind: 'button',
    name: 'Umbraco Search Collection Action - Reload',
    alias: 'Umbraco.Search.CollectionAction.Reload',
    api: () => import('/App_Plugins/UmbracoSearch/search-library.js').then(m => ({ default: m.UmbSearchCollectionReloadAction })),
    meta: {
      label: '#search_collectionActionReload',
    },
    conditions: [
      {
        alias: 'Umb.Condition.CollectionAlias',
        match: UMB_SEARCH_ROOT_COLLECTION_ALIAS
      }
    ]
  }
]
