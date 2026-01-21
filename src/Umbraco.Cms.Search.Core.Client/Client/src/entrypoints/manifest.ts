export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Umbraco Cms Search Core Client Entrypoint",
    alias: "Umbraco.Cms.Search.Core.Client.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
