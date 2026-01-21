export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Umbraco Cms Search Back Office Client Entrypoint",
    alias: "Umbraco.Cms.Search.BackOffice.Client.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
