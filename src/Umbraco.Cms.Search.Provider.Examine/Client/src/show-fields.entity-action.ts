import { fieldsRouteBuilder } from './fields-route-provider.element.js';
import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';

export class UmbSearchExamineShowFieldsEntityAction extends UmbEntityActionBase<never> {
  // eslint-disable-next-line @typescript-eslint/require-await
  override async getHref() {
    const unique = this.args.unique ?? null;
    if (!unique) return '#';

    const culture = new URL(window.location.href).searchParams.get('culture') ?? '_';

    return fieldsRouteBuilder?.({ documentUnique: unique, culture }) ?? '#';
  }

  override execute(): Promise<void> {
    return Promise.resolve(undefined);
  }
}

export default UmbSearchExamineShowFieldsEntityAction;
