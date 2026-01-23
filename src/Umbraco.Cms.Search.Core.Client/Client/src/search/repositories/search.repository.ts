import { indexes, rebuild } from '../../api';
import { UmbSearchIndex } from '../types.js';
import { UMB_SEARCH_INDEX_ENTITY_TYPE } from "../constants.js";
import { UmbCollectionFilterModel, UmbCollectionRepository} from '@umbraco-cms/backoffice/collection';
import { UmbPagedModel, UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';

export class UmbSearchCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository<UmbSearchIndex, never> {
    async requestCollection(_filter?: UmbCollectionFilterModel | undefined) {
        const {data, error} = await tryExecute(this, indexes());

        if (error) return { data: undefined, error };

        const pagedData: UmbPagedModel<UmbSearchIndex> = {
            items: data?.items.map(x => ({
              ...x,
              entityType: UMB_SEARCH_INDEX_ENTITY_TYPE,
              unique: x.indexAlias,
            })) ?? [],
            total: data?.total ?? 0,
        };

        return { data: pagedData };
    }

    async rebuildIndex(indexAlias: string): Promise<void> {
        const { error } = await tryExecute(this, rebuild({ query: { indexAlias } }));
        if (error) throw error;
    }
}
