import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { fieldsRouteBuilder } from './fields-route-provider.element.js';

export class UmbSearchExamineShowFieldsEntityAction extends UmbEntityActionBase<never> {
	override execute(): Promise<void> {
		const args = this.args as typeof this.args & {
			searchDocument?: { unique: string };
		};

		if (!args.searchDocument) {
			throw new Error('Search document is not provided');
		}

		const culture = new URL(window.location.href).searchParams.get('culture') ?? '_';

		if (fieldsRouteBuilder) {
			history.pushState(
				{},
				'',
				fieldsRouteBuilder({
					documentUnique: args.searchDocument.unique,
					culture,
				}),
			);
		}

		return Promise.resolve();
	}
}

export default UmbSearchExamineShowFieldsEntityAction;
