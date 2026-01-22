export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'localization',
    name: 'Umbraco Search Localization - English',
    alias: 'Umbraco.Search.Localization.En',
    meta: { culture: 'en' },
    js: () => import('./en.js'),
  }
]
