import type { UmbSearchIndex } from '../../types.js';
import { UmbCollectionDataSource } from '@umbraco-cms/backoffice/collection';

export type UmbSearchCollectionDataSource = UmbCollectionDataSource<
  UmbSearchIndex,
  never
>;
