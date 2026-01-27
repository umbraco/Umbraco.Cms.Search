import { UMB_SEARCH_COLLECTION_VIEW_ALIAS } from '../constants.js';
import type { UmbSearchIndex, UmbSearchIndexState } from './types.js';
import { UMB_SEARCH_NOTIFICATION_CONTEXT } from '@umbraco-cms/search/global';
import { UmbDefaultCollectionContext } from '@umbraco-cms/backoffice/collection';
import type { UmbControllerHostElement } from '@umbraco-cms/backoffice/controller-api';
import { UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT } from '@umbraco-cms/backoffice/management-api';

export class UmbSearchCollectionContext extends UmbDefaultCollectionContext<
  UmbSearchIndex,
  never
> {
  #searchContext?: typeof UMB_SEARCH_NOTIFICATION_CONTEXT.TYPE;
  #serverEventContext?: typeof UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT.TYPE;

  constructor(host: UmbControllerHostElement) {
    super(host, UMB_SEARCH_COLLECTION_VIEW_ALIAS);

    this.consumeContext(UMB_SEARCH_NOTIFICATION_CONTEXT, (instance) => {
      this.#searchContext = instance;
    });

    this.consumeContext(UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT, (instance) => {
      this.#serverEventContext = instance;
      this.#observeSearchIndexChanges();
    });
  }

  setIndexState(indexAlias: string, state: UmbSearchIndexState) {
    this._items.updateOne(indexAlias, { state });
  }

  setUserWaitingForIndexUpdate(indexAlias: string, isWaiting: boolean) {
    this.#searchContext?.setUserWaitingForIndexUpdate(indexAlias, isWaiting);
  }

  #observeSearchIndexChanges() {
    this.observe(this.#serverEventContext?.byEventSource('IndexRebuildCompleted'), (args) => {
      if (!args.eventSource) return;

      // Try and get latest collection context and reload
      this.loadCollection();
    }, 'index-rebuild-completed-observer');
  }
}

export { UmbSearchCollectionContext as api };
