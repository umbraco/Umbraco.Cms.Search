import { UmbSearchDetailRepository } from '../repositories/search-detail.repository.js';
import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../workspace/search/search-workspace.context-token.js';
import { UmbWorkspaceActionBase } from '@umbraco-cms/backoffice/workspace';
import { umbConfirmModal } from '@umbraco-cms/backoffice/modal';

export class UmbRebuildIndexWorkspaceAction extends UmbWorkspaceActionBase {
  #repository = new UmbSearchDetailRepository(this);

  async execute() {
    const workspaceContext = await this.getContext(UMB_SEARCH_WORKSPACE_CONTEXT);

    if (!workspaceContext) {
      throw new Error('Workspace context is not available');
    }

    const indexAlias = workspaceContext.getUnique();

    if (!indexAlias) {
      throw new Error('Index alias is not available');
    }

    // Show confirm modal first
    await umbConfirmModal(this, {
      color: 'warning',
      headline: '#search_rebuildConfirmHeadline',
      content: '#search_rebuildConfirmMessage',
      confirmLabel: '#search_rebuildConfirmLabel',
    });

    // User confirmed - set loading state for UI feedback
    workspaceContext.setState('loading');

    // Repository handles: notification → API call → waiting state (no modal)
    await this.#repository.rebuildIndex(indexAlias);
  }

  async getHref() {
    return undefined;
  }
}
