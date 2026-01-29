import { UmbSearchWorkspaceEditorElement } from './search-workspace-editor.element.js';
import { UmbSearchDetailRepository, UmbSearchIndex } from '@umbraco-cms/search/settings';
import {
  UMB_SEARCH_CONTEXT,
  UMB_SEARCH_DETAIL_REPOSITORY_ALIAS,
  UMB_SEARCH_ENTITY_TYPE,
  UMB_SEARCH_WORKSPACE_ALIAS
} from '@umbraco-cms/search/global';

import {
  type UmbRoutableWorkspaceContext,
  UmbEntityNamedDetailWorkspaceContextBase,
} from '@umbraco-cms/backoffice/workspace';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import {UmbLocalizationController} from "@umbraco-cms/backoffice/localization-api";
import {UMB_NOTIFICATION_CONTEXT} from "@umbraco-cms/backoffice/notification";

export class UmbSearchWorkspaceContext
  extends UmbEntityNamedDetailWorkspaceContextBase<UmbSearchIndex, UmbSearchDetailRepository>
  implements UmbRoutableWorkspaceContext
{
  public readonly repository = new UmbSearchDetailRepository(this);
  public readonly documentCount = this._data.createObservablePartOfCurrent(x => x?.documentCount);
  public readonly healthStatus = this._data.createObservablePartOfCurrent(x => x?.healthStatus);

  constructor(host: UmbControllerHost) {
    super(host, {
      workspaceAlias: UMB_SEARCH_WORKSPACE_ALIAS,
      entityType: UMB_SEARCH_ENTITY_TYPE,
      detailRepositoryAlias: UMB_SEARCH_DETAIL_REPOSITORY_ALIAS,
    });

    this.routes.setRoutes([
      {
        path: 'edit/:unique',
        component: UmbSearchWorkspaceEditorElement,
        setup: (_component, info) => {
          this.load(info.match.params.unique);
        },
      },
    ]);
  }

  async rebuildIndex() {
    const indexAlias = this.getUnique();
    if (!indexAlias) throw new Error('Index alias is missing');

    const localize = new UmbLocalizationController(this);
    const [notificationContext, searchContext] = await Promise.all([
      this.getContext(UMB_NOTIFICATION_CONTEXT),
      this.getContext(UMB_SEARCH_CONTEXT)
    ]);

    if (!searchContext) throw new Error('Search context is not available');

    notificationContext?.peek('warning', {
      data: {
        title: localize.term('search_rebuildConfirmHeadline'),
        message: localize.term('search_rebuildStartedMessage', indexAlias),
      }
    });

    await this.repository.rebuildIndex(indexAlias);

    // Mark that the user is waiting for this index to rebuild
    searchContext.setUserWaitingForIndexUpdate(indexAlias, true);
  }
}
