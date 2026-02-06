import { search } from '../api';
import type {
  UmbSearchRequest,
  UmbSearchResult,
  UmbSearchDocument,
  UmbSearchFacetResult,
} from '../types.js';
import type { SearchRequestModel, DocumentModel, FacetResultModel } from '../api';

import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { client } from '@umbraco-cms/backoffice/external/backend-api';
import type { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export class UmbSearchQueryServerDataSource {
  readonly #host: UmbControllerHost;

  constructor(host: UmbControllerHost) {
    this.#host = host;
  }

  async search(request: UmbSearchRequest): Promise<UmbDataSourceResponse<UmbSearchResult>> {
    // Map domain types to API types
    const apiRequest: SearchRequestModel = {
      indexAlias: request.indexAlias,
      query: request.query ?? null,
      filters: request.filters ?? null,
      facets: request.facets ?? null,
      sorters: request.sorters ?? null,
      culture: request.culture ?? null,
      segment: request.segment ?? null,
      accessContext: request.accessContext ?? null,
    };

    const { data, error } = await tryExecute(
      this.#host,
      search({
        body: apiRequest,
        query: {
          skip: request.skip ?? 0,
          take: request.take ?? 10,
        },
        client: client as never,
      }),
    );

    if (error || !data) {
      return { error };
    }

    // Map API types to domain types
    const result: UmbSearchResult = {
      total: data.total,
      documents: data.documents.map(this.#mapDocument),
      facets: data.facets.map(this.#mapFacetResult),
    };

    return { data: result };
  }

  #mapDocument(apiDoc: DocumentModel): UmbSearchDocument {
    return {
      unique: apiDoc.id,
      objectType: String(apiDoc.objectType),
    };
  }

  #mapFacetResult(apiFacet: FacetResultModel): UmbSearchFacetResult {
    return {
      fieldName: apiFacet.fieldName,
      values: apiFacet.values.map((v) => ({ count: v.count })),
    };
  }
}
