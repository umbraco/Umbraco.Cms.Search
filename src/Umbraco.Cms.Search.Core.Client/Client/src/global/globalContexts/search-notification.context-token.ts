import { UmbSearchNotificationContext } from './search-notification.global-context.js';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';

export const UMB_SEARCH_NOTIFICATION_CONTEXT = new UmbContextToken<UmbSearchNotificationContext>('UmbSearchNotificationContext');
