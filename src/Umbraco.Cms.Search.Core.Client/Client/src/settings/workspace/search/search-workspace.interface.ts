import type { UmbSearchIndexState } from '../../types.js';

/**
 * Interface for search workspace contexts that support state management.
 * This allows entity actions to update the workspace state when triggered from the workspace header.
 */
export interface UmbSearchWorkspaceStateContext {
  /**
   * Sets the workspace state (e.g., 'loading', 'idle', 'error')
   */
  setState(state: UmbSearchIndexState): void;
}
