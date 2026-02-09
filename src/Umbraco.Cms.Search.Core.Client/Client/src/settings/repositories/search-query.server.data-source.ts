import { search } from '../api';
import type {
  UmbSearchRequest,
  UmbSearchResult,
  UmbSearchDocument,
  UmbSearchFacetResult,
} from '../types.js';
import type { SearchRequestModel, FacetResultModel } from '../api';

import { UmbDocumentItemRepository } from '@umbraco-cms/backoffice/document';
import { UmbMediaItemRepository } from '@umbraco-cms/backoffice/media';
import { UMB_APP_LANGUAGE_CONTEXT } from '@umbraco-cms/backoffice/language';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { client } from '@umbraco-cms/backoffice/external/backend-api';
import type { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UmbMemberItemRepository } from '@umbraco-cms/backoffice/member';

export class UmbSearchQueryServerDataSource extends UmbControllerBase {
  #documentItemRepository = new UmbDocumentItemRepository(this);
  #mediaItemRepository = new UmbMediaItemRepository(this);
  #memberItemRepository = new UmbMemberItemRepository(this);

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
      this,
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

    // Map API documents to domain types with defaults
    const documents: UmbSearchDocument[] = data.documents.map((apiDoc) => ({
      id: apiDoc.id,
      objectType: String(apiDoc.objectType),
      entityType: this.#getEntityType(apiDoc.objectType),
      name: 'Unknown',
      icon: undefined,
    }));

    // Enrich documents with name/icon from item repositories
    await this.#enrichDocuments(documents);

    const result: UmbSearchResult = {
      total: data.total,
      documents,
      facets: data.facets.map(this.#mapFacetResult),
    };

    return { data: result };
  }

  async #enrichDocuments(documents: UmbSearchDocument[]): Promise<void> {
    const docById = new Map(documents.map((doc) => [doc.id, doc]));
    const entityTypes = [...new Set(documents.map((doc) => doc.entityType))];

    // Resolve the default content language for variant name lookup
    const languageContext = await this.getContext(UMB_APP_LANGUAGE_CONTEXT);
    const defaultLanguage = languageContext?.getAppCulture() ?? null;

    await Promise.all(
      entityTypes.map(async (entityType) => {
        const ids = documents.filter((doc) => doc.entityType === entityType).map((doc) => doc.id);
        const resolved = await this.#resolveItems(entityType, ids, defaultLanguage);

        for (const item of resolved) {
          const doc = docById.get(item.id);
          if (doc) {
            doc.name = item.name;
            doc.icon = item.icon;
          }
        }
      }),
    );
  }

  async #resolveItems(
    entityType: string,
    ids: string[],
    culture: string | null,
  ): Promise<Array<{ id: string; name: string; icon?: string }>> {
    switch (entityType) {
      case 'document':
        return this.#resolveDocumentItems(ids, culture);
      case 'media':
        return this.#resolveMediaItems(ids, culture);
      case 'member':
        return this.#resolveMemberItems(ids, culture);
      default:
        return [];
    }
  }

  async #resolveDocumentItems(
    ids: string[],
    culture: string | null,
  ): Promise<Array<{ id: string; name: string; icon?: string }>> {
    const { data: items } = await this.#documentItemRepository.requestItems(ids);
    if (!items) return [];

    return items.map((item) => ({
      id: item.unique,
      name: this.#resolveVariantName(item.variants, culture),
      icon: item.documentType.icon,
    }));
  }

  async #resolveMediaItems(
    ids: string[],
    culture: string | null,
  ): Promise<Array<{ id: string; name: string; icon?: string }>> {
    const { data: items } = await this.#mediaItemRepository.requestItems(ids);
    if (!items) return [];

    return items.map((item) => ({
      id: item.unique,
      name: this.#resolveVariantName(item.variants, culture) ?? item.name ?? 'Unknown',
      icon: item.mediaType.icon,
    }));
  }

  async #resolveMemberItems(
    ids: string[],
    culture: string | null,
  ): Promise<Array<{ id: string; name: string; icon?: string }>> {
    const { data: items } = await this.#memberItemRepository.requestItems(ids);
    if (!items) return [];

    return items.map((item) => ({
      id: item.unique,
      name: this.#resolveVariantName(item.variants, culture) ?? item.name ?? 'Unknown',
      icon: item.memberType.icon,
    }));
  }

  #resolveVariantName(
    variants: Array<{ name: string; culture: string | null }>,
    culture: string | null,
  ): string {
    const variant =
      variants.find((v) => v.culture === culture) ??
      variants.find((v) => v.culture === null) ??
      variants[0];
    return variant?.name ?? 'Unknown';
  }

  #mapFacetResult(apiFacet: FacetResultModel): UmbSearchFacetResult {
    return {
      fieldName: apiFacet.fieldName,
      values: apiFacet.values.map((v) => ({ count: v.count })),
    };
  }

  #getEntityType(objectType: string): string {
    // Map UmbracoObjectTypes enum values to entity type strings
    const typeMap: Record<string, string> = {
      Document: 'document',
      Media: 'media',
      Member: 'member',
      DocumentType: 'document-type',
      MediaType: 'media-type',
      MemberType: 'member-type',
      DataType: 'data-type',
    };

    return typeMap[objectType] || objectType.toLowerCase();
  }
}
