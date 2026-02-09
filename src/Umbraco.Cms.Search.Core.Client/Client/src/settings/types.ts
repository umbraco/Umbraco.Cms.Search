import { UmbCollectionDataSource } from '@umbraco-cms/backoffice/collection';

export type UmbSearchCollectionDataSource = UmbCollectionDataSource<UmbSearchIndex, never>;

export type UmbSearchIndexState = 'idle' | 'loading' | 'error';
export type UmbHealthStatusModel = 'Healthy' | 'Rebuilding' | 'Corrupted' | 'Empty' | 'Unknown';

export type UmbSearchIndex = {
  unique: string;
  name: string;
  documentCount: number;
  healthStatus: UmbHealthStatusModel;
  entityType: string;
  state: UmbSearchIndexState;
};

// Search request types
export type UmbSearchDirection = 'Ascending' | 'Descending';

export type UmbSearchFilter = {
  fieldName: string;
  negate: boolean;
};

export type UmbSearchFacet = {
  fieldName: string;
};

export type UmbSearchSorter = {
  fieldName: string;
  direction: UmbSearchDirection;
};

export type UmbSearchAccessContext = {
  principalId: string;
  groupIds?: string[];
};

export type UmbSearchRequest = {
  indexAlias: string;
  query?: string;
  filters?: UmbSearchFilter[];
  facets?: UmbSearchFacet[];
  sorters?: UmbSearchSorter[];
  culture?: string;
  segment?: string;
  accessContext?: UmbSearchAccessContext;
  skip?: number;
  take?: number;
};

// Search result types
export type UmbSearchDocument = {
  id: string;
  objectType: string;
};

export type UmbSearchFacetValue = {
  count: number;
};

export type UmbSearchFacetResult = {
  fieldName: string;
  values: UmbSearchFacetValue[];
};

export type UmbSearchResult = {
  total: number;
  documents: UmbSearchDocument[];
  facets: UmbSearchFacetResult[];
};
