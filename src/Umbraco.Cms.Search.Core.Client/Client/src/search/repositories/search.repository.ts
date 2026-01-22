import { indexes } from '../../api';
import { UmbCollectionFilterModel, UmbCollectionRepository} from '@umbraco-cms/backoffice/collection';
import { UmbPagedModel, UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { UmbSearchIndex } from '../types.ts';

export class UmbSearchCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository<UmbSearchIndex, never> {
    async requestCollection(_filter?: UmbCollectionFilterModel | undefined) {
        const {data, error} = await tryExecute(this, indexes());

        if (error) return { data: undefined, error };

        const pagedData: UmbPagedModel<UmbSearchIndex> = {
            items: data?.items.map(x => ({
              ...x,
              entityType: 'search-index',
              unique: x.indexAlias,
            })) ?? [],
            total: data?.total ?? 0,
        };

        return { data: pagedData };
    }
}
