import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../workspace/search/search-workspace.context-token.js';
import { UmbWorkspaceActionBase } from '@umbraco-cms/backoffice/workspace';
import {umbConfirmModal} from "@umbraco-cms/backoffice/modal";

export class UmbRebuildIndexWorkspaceAction extends UmbWorkspaceActionBase {
  async execute() {
    const workspaceContext = await this.getContext(UMB_SEARCH_WORKSPACE_CONTEXT);

    if (!workspaceContext) {
      throw new Error('Workspace context is not available');
    }

    try {
      await umbConfirmModal(this, {
        color: 'warning',
        headline: '#search_rebuildConfirmHeadline',
        content: '#search_rebuildConfirmMessage',
        confirmLabel: '#search_rebuildConfirmLabel',
      });

      await workspaceContext.rebuildIndex();
    } catch {
      // Do nothing if the user cancels
    }
  }

  async getHref() {
    return undefined;
  }
}
