import { UmbSearchNotificationContext } from '@umbraco-cms/search/global';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'globalContext',
    alias: 'Umbraco.Search.Notification.GlobalContext',
    name: 'Umbraco Search Notification Global Context',
    api: UmbSearchNotificationContext
  }
];
