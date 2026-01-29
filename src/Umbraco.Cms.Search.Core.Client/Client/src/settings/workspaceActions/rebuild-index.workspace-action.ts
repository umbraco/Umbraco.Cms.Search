import { UmbSearchDetailRepository } from '../repositories/search-detail.repository.js';
import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../workspace/search/search-workspace.context-token.js';
import { UmbWorkspaceActionBase } from '@umbraco-cms/backoffice/workspace';

export class UmbRebuildIndexWorkspaceAction extends UmbWorkspaceActionBase {
  #repository = new UmbSearchDetailRepository(this);

  async execute() {
    const workspaceContext = await this.getContext(UMB_SEARCH_WORKSPACE_CONTEXT);
    const indexAlias = workspaceContext?.getUnique();

    if (!indexAlias) {
      throw new Error('Index alias is not available');
    }

    // Repository handles: confirm modal → notification → API call → waiting state
    await this.#repository.rebuildIndex(indexAlias);
  }

  async getHref() {
    return undefined;
  }
}
