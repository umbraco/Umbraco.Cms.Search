import { UMB_SEARCH_COLLECTION_VIEW_ALIAS } from '../constants.js';
import type { UmbSearchIndex, UmbSearchIndexState } from './types.js';
import { UmbDefaultCollectionContext } from '@umbraco-cms/backoffice/collection';
import type { UmbControllerHostElement } from '@umbraco-cms/backoffice/controller-api';
import { UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT } from '@umbraco-cms/backoffice/management-api';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import { UmbLocalizationController } from '@umbraco-cms/backoffice/localization-api';

export class UmbSearchCollectionContext extends UmbDefaultCollectionContext<
  UmbSearchIndex,
  never
> {
  #serverEventContext?: typeof UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT.TYPE;
  #userWaitingForIndexUpdate = new Set<string>();
  #localize = new UmbLocalizationController(this);

  constructor(host: UmbControllerHostElement) {
    super(host, UMB_SEARCH_COLLECTION_VIEW_ALIAS);

    this.consumeContext(UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT, (instance) => {
      this.#serverEventContext = instance;
      this.#observeSearchIndexChanges();
    });
  }

  setIndexState(indexAlias: string, state: UmbSearchIndexState) {
    this._items.updateOne(indexAlias, { state });
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
    console.log('Watching for search index changes');
    this.observe(this.#serverEventContext?.byEventSource('IndexRebuildCompleted'), (args) => {
      console.log('index updated', args);

      if (!args.eventSource) return;

      // Try and get latest collection context and reload
      this.loadCollection();

      // TODO: Move this to a global context so the user gets notified no matter where they are in the backoffice
      if (this.#isUserWaitingForIndexUpdate(args.eventSource)) {
        this.setUserWaitingForIndexUpdate(args.eventSource, false);
        this.getContext(UMB_NOTIFICATION_CONTEXT)
          .then(notificationContext => {
            notificationContext?.peek('positive', {
              data: {
                title: this.#localize.term('search_rebuildCompletedTitle'),
                message: this.#localize.term('search_rebuildCompletedMessage', args.eventSource),
              }
            });
          });
      }
    }, 'index-rebuild-completed-observer');
  }
}

export { UmbSearchCollectionContext as api };
