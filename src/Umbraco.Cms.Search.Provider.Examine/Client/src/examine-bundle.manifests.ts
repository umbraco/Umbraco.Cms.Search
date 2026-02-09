export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'entityAction',
    kind: 'default',
    alias: 'Umbraco.Cms.Search.Provider.Examine.EntityAction.ShowFields',
    name: 'Umbraco Search Provider Examine - Show Fields',
    weight: 100,
    api: () =>
      import('./show-fields.entity-action.js').then((m) => ({
        default: m.UmbSearchExamineShowFieldsEntityAction,
      })),
    forEntityTypes: ['search-document'],
    meta: {
      icon: 'icon-search',
      label: 'Show Fields',
      additionalOptions: false,
    },
  },
  {
    type: 'modal',
    alias: 'Umbraco.Cms.Search.Provider.Examine.Modal.Fields',
    name: 'Umbraco Search Provider Examine - Fields Modal',
    element: () => import('./show-fields.modal.js'),
    elementName: 'umb-search-examine-show-fields-modal',
  },
];
