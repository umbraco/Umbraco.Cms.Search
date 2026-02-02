import { UmbCollectionDataSource } from '@umbraco-cms/backoffice/collection';

export type UmbSearchCollectionDataSource = UmbCollectionDataSource<
  UmbSearchIndex,
  never
>;

export type UmbSearchIndexState = 'idle' | 'loading' | 'error';
export type UmbHealthStatusModel = 'Healthy' | 'Rebuilding' | 'Corrupted' | 'Empty' | 'Unknown';

export type UmbSearchIndex = {
  unique: string;
  documentCount: number;
  healthStatus: UmbHealthStatusModel;
  entityType: string;
  state: UmbSearchIndexState;
}
