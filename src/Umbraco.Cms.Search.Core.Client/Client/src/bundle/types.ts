import type {
  ManifestElement,
  ManifestWithDynamicConditions
} from '@umbraco-cms/backoffice/extension-api';

export interface ManifestSearchIndexDetailBox extends ManifestElement, ManifestWithDynamicConditions {
  type: 'searchIndexDetailBox';
  meta?: MetaSearchIndexDetailBox;
}

export interface MetaSearchIndexDetailBox {
  label?: string;
}

declare global {
  interface UmbExtensionManifestMap {
    umbSearchIndexDetailBox: ManifestSearchIndexDetailBox;
  }
}
