import type { UmbSearchRequest } from '../types.js';
import { UmbSearchQueryServerDataSource } from './search-query.server.data-source.js';

import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export class UmbSearchQueryRepository extends UmbRepositoryBase {
  #dataSource: UmbSearchQueryServerDataSource;

  constructor(host: UmbControllerHost) {
    super(host);
    this.#dataSource = new UmbSearchQueryServerDataSource(host);
  }

  async search(request: UmbSearchRequest) {
    return this.#dataSource.search(request);
  }
}
