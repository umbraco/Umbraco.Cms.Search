import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client';
import { tryExecute } from '@umbraco-cms/backoffice/resources';

export class UmbSearchExamineProviderRepository extends UmbRepositoryBase {

  constructor(host) {
    super(host);
  }

  async requestSearchDocument(unique, indexAlias) {
    if (!unique) {
      return { error: new Error('Search document unique identifier is not provided') };
    }

    if (!indexAlias) {
      return { error: new Error('Index alias is not provided') };
    }

    const config = umbHttpClient.getConfig();
    const token = typeof config.auth === 'function' ? await config.auth() : config.auth;

    const { data, error } = await tryExecute(this, umbHttpClient.request({
      url: `/umbraco/examine/api/v1/${indexAlias}/document/${unique}`,
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    }));

    return { data, error };
  }
}
