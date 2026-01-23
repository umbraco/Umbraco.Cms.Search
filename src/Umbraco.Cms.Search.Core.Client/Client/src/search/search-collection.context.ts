import { UMB_SEARCH_COLLECTION_VIEW_ALIAS } from './constants.js';
import type { UmbSearchIndex, UmbSearchIndexState } from './types.js';
import { UmbDefaultCollectionContext } from '@umbraco-cms/backoffice/collection';
import type { UmbControllerHostElement } from '@umbraco-cms/backoffice/controller-api';

export class UmbSearchCollectionContext extends UmbDefaultCollectionContext<
  UmbSearchIndex,
  never
> {
  constructor(host: UmbControllerHostElement) {
    super(host, UMB_SEARCH_COLLECTION_VIEW_ALIAS);
  }

  setIndexState(indexAlias: string, state: UmbSearchIndexState) {
    this._items.updateOne(indexAlias, { state });
  }
}

export { UmbSearchCollectionContext as api };
