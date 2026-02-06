import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';

export class UmbSearchExamineShowFieldsEntityAction extends UmbEntityActionBase {
  async execute() {
    if (!this.args.searchDocument) {
      throw new Error('Search document is not provided');
    }

    await umbOpenModal(this, 'Umbraco.Cms.Search.Provider.Examine.Modal.Fields', {
      modal: {
        type: 'sidebar',
        size: 'medium',
      },
      data: {
        searchDocument: this.args.searchDocument,
        indexAlias: this.args.indexAlias,
      }
    }).catch(() => undefined);
  }
}

export default UmbSearchExamineShowFieldsEntityAction;
