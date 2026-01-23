import { UmbSearchCollectionRepository } from '../repositories/index.js';
import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import {UmbLocalizationController} from "@umbraco-cms/backoffice/localization-api";

export class UmbSearchRebuildIndexEntityAction extends UmbEntityActionBase<never> {
  #searchRepository = new UmbSearchCollectionRepository(this);

  override async execute() {
    if (!this.args.unique) {
      throw new Error('Index alias is not provided');
    }

    try {
      await umbConfirmModal(this, {
        color: 'warning',
        headline: '#search_rebuildConfirmHeadline',
        content: '#search_rebuildConfirmMessage',
        confirmLabel: '#search_rebuildConfirmLabel',
      });

      const localize = new UmbLocalizationController(this);
      const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);

      notificationContext?.peek('warning', {
        data: {
          title: localize.term('search_rebuildConfirmHeadline'),
          message: localize.term('search_rebuildStartedMessage', this.args.unique),
        }
      });

      await this.#searchRepository.rebuildIndex(this.args.unique);
    } catch {
      // Ignore errors
    }
  }
}

export default UmbSearchRebuildIndexEntityAction;
