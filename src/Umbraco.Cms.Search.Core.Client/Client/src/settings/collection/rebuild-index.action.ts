import { UmbSearchDetailRepository } from '../repositories/search-detail.repository.js';
import { UmbSearchCollectionContext } from './search-collection.context.js';
import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../workspace/search/search-workspace.context-token.js';

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

    // Check for workspace context first (when triggered from workspace header)
    const workspaceContext = await this.getContext(UMB_SEARCH_WORKSPACE_CONTEXT).catch(() => undefined);
    if (workspaceContext) {
      workspaceContext.setState('loading');
    }

    // User confirmed - repository handles: notification → API call → waiting state
    await this.#repository.rebuildIndex(this.args.unique);

    // Set loading state immediately for UI feedback (when triggered from collection view)
    const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT).catch(() => undefined);
    if (collectionContext instanceof UmbSearchCollectionContext) {
      collectionContext.setIndexState(this.args.unique, 'loading');
    }
  }
}
