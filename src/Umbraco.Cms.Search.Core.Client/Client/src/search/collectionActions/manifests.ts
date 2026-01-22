import {UMB_SEARCH_ROOT_COLLECTION_ALIAS} from "../constants.ts";
import UmbSearchCollectionReloadAction from "./reload.collection-action.ts";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collectionAction',
    kind: 'button',
    name: 'Umbraco Search Collection Action - Reload',
    alias: 'Umbraco.Search.CollectionAction.Reload',
    api: UmbSearchCollectionReloadAction,
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
