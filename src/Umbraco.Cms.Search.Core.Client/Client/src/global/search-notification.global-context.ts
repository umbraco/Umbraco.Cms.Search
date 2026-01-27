import { UmbContextBase } from '@umbraco-cms/backoffice/class-api';
import { UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT } from '@umbraco-cms/backoffice/management-api';
import { UmbLocalizationController } from '@umbraco-cms/backoffice/localization-api';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';

export class UmbSearchNotificationContext extends UmbContextBase {
  #serverEventContext?: typeof UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT.TYPE;
  #notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;
  #userWaitingForIndexUpdate = new Set<string>();
  #localize = new UmbLocalizationController(this);

  constructor(host: UmbControllerHost) {
    super(host, UMB_SEARCH_NOTIFICATION_CONTEXT);

    this.consumeContext(UMB_NOTIFICATION_CONTEXT, (instance) => {
      this.#notificationContext = instance;
    });

    this.consumeContext(UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT, (instance) => {
      this.#serverEventContext = instance;
      this.#observeSearchIndexChanges();
    });
  }

  setUserWaitingForIndexUpdate(indexAlias: string, isWaiting: boolean) {
    if (isWaiting) {
      this.#userWaitingForIndexUpdate.add(indexAlias);
    } else {
      this.#userWaitingForIndexUpdate.delete(indexAlias);
    }
  }

  #isUserWaitingForIndexUpdate(indexAlias: string): boolean {
    return this.#userWaitingForIndexUpdate.has(indexAlias);
  }

  #observeSearchIndexChanges() {
    console.log('Global: Watching for search index changes');
    this.observe(this.#serverEventContext?.byEventSource('IndexRebuildCompleted'), (args) => {
      console.log('Global: index updated', args);

      if (!args.eventSource) return;

      if (!this.#isUserWaitingForIndexUpdate(args.eventSource)) {
        return;
      }

      this.setUserWaitingForIndexUpdate(args.eventSource, false);

      this.#notificationContext?.peek('positive', {
        data: {
          title: this.#localize.term('search_rebuildCompletedTitle'),
          message: this.#localize.term('search_rebuildCompletedMessage', args.eventSource),
        }
      });
    }, 'index-rebuild-notification-observer');
  }
}

export default UmbSearchNotificationContext;

export const UMB_SEARCH_NOTIFICATION_CONTEXT = new UmbContextToken<UmbSearchNotificationContext>('UmbSearchNotificationContext');
