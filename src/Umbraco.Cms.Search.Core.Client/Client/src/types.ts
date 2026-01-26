import { IndexModel } from './api';

export type UmbSearchIndexState = 'idle' | 'loading' | 'error';

export type UmbSearchIndex = IndexModel & {
  entityType: string;
  unique: string;
  state: UmbSearchIndexState;
}
