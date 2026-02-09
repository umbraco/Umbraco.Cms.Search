import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';

export class UmbSearchExamineShowFieldsEntityAction extends UmbEntityActionBase<never> {
  override async execute() {
    const args = this.args as typeof this.args & {
      searchDocument?: { unique: string };
      indexAlias?: string;
    };

    if (!args.searchDocument) {
      throw new Error('Search document is not provided');
    }

    await umbOpenModal(this, 'Umbraco.Cms.Search.Provider.Examine.Modal.Fields', {
      modal: {
        type: 'sidebar',
        size: 'large',
      },
      data: {
        searchDocument: args.searchDocument,
        indexAlias: args.indexAlias,
      },
    }).catch(() => undefined);
  }
}

export default UmbSearchExamineShowFieldsEntityAction;
