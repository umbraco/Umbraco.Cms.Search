import { UmbSearchCollectionRepository } from '../repositories/index.js';
import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';

export class UmbSearchRebuildIndexEntityAction extends UmbEntityActionBase<never> {
  #searchRepository = new UmbSearchCollectionRepository(this);

  override async execute() {
    if (!this.args.unique) {
      throw new Error('Index alias is not provided');
    }

    const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);

    notificationContext?.peek('warning', {
      data: {
        title: 'Search Index Rebuild',
        message: `Rebuilding search index "${this.args.unique}" has started. This may take a while depending on the size of your content.`,
      }
    });

    await this.#searchRepository.rebuildIndex(this.args.unique);
  }
}

export default UmbSearchRebuildIndexEntityAction;
