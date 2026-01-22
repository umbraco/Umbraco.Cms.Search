import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT } from '@umbraco-cms/backoffice/management-api';
import { UMB_COLLECTION_CONTEXT } from '@umbraco-cms/backoffice/collection';

/**
 * This controller is able to react to search related events and updates through SignalR.
 */
export class UmbSearchReactiveController extends UmbControllerBase {
  #context?: typeof UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT.TYPE;

  constructor(host: UmbControllerHost) {
    super(host);

    this.consumeContext(UMB_MANAGEMENT_API_SERVER_EVENT_CONTEXT, (instance) => {
      this.#context = instance;
      this.#observeSearchIndexChanges();
    });
  }

  #observeSearchIndexChanges() {
    console.log('Watching for search index changes');
    this.observe(this.#context?.byEventSource('IndexRebuildCompleted'), (indexAlias) => {
      console.log('index updated', indexAlias);

      if (!indexAlias) return;

      // Try and get latest collection context and reload
      this.#reloadActiveContext();
    }, 'index-rebuild-completed-observer');
  }

  async #reloadActiveContext() {
    const context = await this.getContext(UMB_COLLECTION_CONTEXT);
    context?.loadCollection();
  }
}
