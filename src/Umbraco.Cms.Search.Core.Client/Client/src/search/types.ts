import { IndexModel } from '../api';

export type UmbSearchIndex = IndexModel & {
  entityType: string;
  unique: string;
}
