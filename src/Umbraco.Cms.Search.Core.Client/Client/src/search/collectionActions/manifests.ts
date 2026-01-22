import {UMB_SEARCH_ROOT_COLLECTION_ALIAS} from "../constants.ts";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collectionAction',
    kind: 'button',
    name: 'Umbraco Search Collection Action - Reload',
    alias: 'Umbraco.Search.CollectionAction.Reload',
    api: () => import('./reload.collection-action.js'),
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
