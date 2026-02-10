import { manifests as langManifests } from './lang/manifests.js';
import UmbSearchExamineShowFieldsEntityAction from './show-fields.entity-action.js';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'entityAction',
    kind: 'default',
    alias: 'Umbraco.Cms.Search.Provider.Examine.EntityAction.ShowFields',
    name: 'Umbraco Search Provider Examine - Show Fields',
    weight: 100,
    api: UmbSearchExamineShowFieldsEntityAction,
    forEntityTypes: ['search-document'],
    meta: {
      icon: 'icon-search',
      label: '#searchExamine_showFields',
      additionalOptions: false,
    },
  },
  {
    type: 'modal',
    alias: 'Umbraco.Cms.Search.Provider.Examine.Modal.Fields',
    name: 'Umbraco Search Provider Examine - Fields Modal',
    element: () => import('./show-fields.modal.js'),
  },
  ...langManifests,
];
