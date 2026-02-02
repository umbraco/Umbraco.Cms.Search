import { UmbSearchWorkspaceEditorElement } from './search-workspace-editor.element.js';
import { UmbSearchDetailRepository, UmbSearchIndex, UmbSearchIndexState } from '@umbraco-cms/search/settings';
import {
  UMB_SEARCH_CONTEXT,
  UMB_SEARCH_DETAIL_REPOSITORY_ALIAS,
  UMB_SEARCH_ENTITY_TYPE,
  UMB_SEARCH_WORKSPACE_ALIAS,
} from '@umbraco-cms/search/global';

import {
  type UmbRoutableWorkspaceContext,
  UmbEntityNamedDetailWorkspaceContextBase,
} from '@umbraco-cms/backoffice/workspace';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export class UmbSearchWorkspaceContext
  extends UmbEntityNamedDetailWorkspaceContextBase<UmbSearchIndex, UmbSearchDetailRepository>
  implements UmbRoutableWorkspaceContext
{
  public readonly repository = new UmbSearchDetailRepository(this);
  public readonly documentCount = this._data.createObservablePartOfPersisted((x) => x?.documentCount);
  public readonly healthStatus = this._data.createObservablePartOfPersisted((x) => x?.healthStatus);
  public readonly state = this._data.createObservablePartOfCurrent((x) => x?.state);

  setState(state: UmbSearchIndexState) {
    this._data.updateCurrent({ state });
  }

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

    this.consumeContext(UMB_SEARCH_CONTEXT, (searchContext) => {
      if (!searchContext) return;
      this.observe(
        searchContext.indexRebuilt,
        async (indexAlias) => {
          if (!indexAlias) return;
          if (indexAlias !== this.getUnique()) return;
          await this.reload();
        },
        'index-rebuild-completed-detail-observer',
      );
    });
  }
}
