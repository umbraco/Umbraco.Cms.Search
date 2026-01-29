import { UmbSearchCollectionContext } from '../search-collection.context.js';
import { UmbSearchDetailRepository } from '../repositories/search-detail.repository.js';
import { UMB_SEARCH_CONTEXT } from '@umbraco-cms/search/global';

import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { umbConfirmModal } from '@umbraco-cms/backoffice/modal';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import { UmbLocalizationController } from '@umbraco-cms/backoffice/localization-api';
import { UMB_COLLECTION_CONTEXT } from '@umbraco-cms/backoffice/collection';

export class UmbSearchRebuildIndexEntityAction extends UmbEntityActionBase<never> {
  #searchRepository = new UmbSearchDetailRepository(this);

  override async execute() {
    if (!this.args.unique) {
      throw new Error('Index alias is not provided');
    }

    await umbConfirmModal(this, {
      color: 'warning',
      headline: '#search_rebuildConfirmHeadline',
      content: '#search_rebuildConfirmMessage',
      confirmLabel: '#search_rebuildConfirmLabel',
    });

    const localize = new UmbLocalizationController(this);
    const [notificationContext, searchContext] = await Promise.all([
      this.getContext(UMB_NOTIFICATION_CONTEXT),
      this.getContext(UMB_SEARCH_CONTEXT)
    ]);

    if (!searchContext) throw new Error('Search context is not available');

    notificationContext?.peek('warning', {
      data: {
        title: localize.term('search_rebuildConfirmHeadline'),
        message: localize.term('search_rebuildStartedMessage', this.args.unique),
      }
    });

    await this.#searchRepository.rebuildIndex(this.args.unique);

    // See if we have a collection context to update the index state
    const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT);
    if (collectionContext instanceof UmbSearchCollectionContext) {
      collectionContext.setIndexState(this.args.unique, 'loading');
      collectionContext.setUserWaitingForIndexUpdate(this.args.unique, true);
    }
  }
}
