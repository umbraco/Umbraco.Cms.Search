import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

export class UmbSearchExamineProviderRepository extends UmbRepositoryBase {
  #authContext;

  constructor(host) {
    super(host);

    this.consumeContext(UMB_AUTH_CONTEXT, (instance) => {
      this.#authContext = instance;
    });
  }

  async requestSearchDocument(unique, indexAlias) {
    if (!unique) {
      return { error: new Error('Search document unique identifier is not provided') };
    }

    if (!indexAlias) {
      return { error: new Error('Index alias is not provided') };
    }

    const openApiConfig = await this.#authContext.getOpenApiConfiguration();

    try {
      const data = await fetch(`${openApiConfig.base}/umbraco/examine/api/v1/${indexAlias}/document/${unique}`, {
        credentials: openApiConfig.credentials,
        headers: {
          'Authorization': `Bearer ${await openApiConfig.token()}`,
        }
      }).then((response) => {
        if (!response.ok) {
          throw new Error('Failed to fetch search document fields');
        }
        return response.json();
      });

      return { data };
    } catch (error) {
      return { error };
    }
  }
}
