import { indexes } from '../../api/index.js';
import { UmbCollectionFilterModel, UmbCollectionRepository} from '@umbraco-cms/backoffice/collection';
import { UmbPagedModel, UmbRepositoryBase, UmbRepositoryResponse } from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';

export class UmbSearchCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    async requestCollection(_filter?: UmbCollectionFilterModel | undefined): Promise<UmbRepositoryResponse<UmbPagedModel<any>>> {
        const {data, error} = await tryExecute(this, indexes());
        return { data, error };
    }
}
