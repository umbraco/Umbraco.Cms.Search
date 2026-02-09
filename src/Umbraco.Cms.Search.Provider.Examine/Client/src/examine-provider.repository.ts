import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export interface ExamineField {
  name: string;
  type: string;
  values: Array<string>;
}

export interface ExamineDocument {
  fields: Array<ExamineField>;
}

export class UmbSearchExamineProviderRepository extends UmbRepositoryBase {
  constructor(host: UmbControllerHost) {
    super(host);
  }

  async requestSearchDocument(unique: string | undefined, indexAlias: string | undefined) {
    if (!unique) {
      return { error: new Error('Search document unique identifier is not provided') };
    }

    if (!indexAlias) {
      return { error: new Error('Index alias is not provided') };
    }

    const authContext = await this.getContext(UMB_AUTH_CONTEXT);
    const token = await authContext!.getLatestToken();

    const { data, error } = await tryExecute(
      this,
      umbHttpClient.request({
        url: `/umbraco/examine/api/v1/${indexAlias}/document/${unique}`,
        method: 'GET',
        headers: {
          Authorization: `Bearer ${token}`,
        },
      }),
    );

    return { data: data as ExamineDocument | undefined, error };
  }
}
