import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';

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

    try {
      const data = await fetch(`/umbraco/examine/api/v1/${indexAlias}/document/${unique}`, {
        credentials: 'include',
        headers: {
          'Authorization': `Bearer [redacted]`,
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
