import { UmbSearchDetailRepository } from '../repositories/search-detail.repository.js';
import { UmbSearchCollectionContext } from './search-collection.context.js';

import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { UMB_COLLECTION_CONTEXT } from '@umbraco-cms/backoffice/collection';
import { umbConfirmModal } from '@umbraco-cms/backoffice/modal';

export class UmbSearchRebuildIndexEntityAction extends UmbEntityActionBase<never> {
  #repository = new UmbSearchDetailRepository(this);

  override async execute() {
    if (!this.args.unique) {
      throw new Error('Index alias is not provided');
    }

    // Show confirm modal first
    await umbConfirmModal(this, {
      color: 'warning',
      headline: '#search_rebuildConfirmHeadline',
      content: '#search_rebuildConfirmMessage',
      confirmLabel: '#search_rebuildConfirmLabel',
    });

    // User confirmed - repository handles: notification → API call → waiting state
    await this.#repository.rebuildIndex(this.args.unique);

    // Collection-specific: set loading state for UI feedback
    const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT);
    if (collectionContext instanceof UmbSearchCollectionContext) {
      collectionContext.setIndexState(this.args.unique, 'loading');
    }
  }
}
