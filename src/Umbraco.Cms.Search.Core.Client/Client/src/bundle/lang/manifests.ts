export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'localization',
    name: 'Umbraco Search Localization - English',
    alias: 'Umbraco.Search.Localization.En',
    meta: { culture: 'en' },
    js: () => import('./en.js'),
  },
  {
    type: 'localization',
    name: 'Umbraco Search Localization - Danish',
    alias: 'Umbraco.Search.Localization.Da',
    meta: { culture: 'da' },
    js: () => import('./da.js'),
  }
]
