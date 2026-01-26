import { manifests as searchManifests } from './search/manifests.js';
import { manifests as localizationManifests } from './lang/manifests.js';

// Job of the bundle is to collate all the manifests from different parts of the extension and load other manifests
// We load this bundle from umbraco-package.json
export const manifests: Array<UmbExtensionManifest> = [
  ...localizationManifests,
  ...searchManifests,
];
