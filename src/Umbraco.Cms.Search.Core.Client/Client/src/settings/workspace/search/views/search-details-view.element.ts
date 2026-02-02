import { html, customElement, css, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UMB_SEARCH_WORKSPACE_CONTEXT } from '../search-workspace.context-token.js';
import type { UmbSearchIndexState } from '../../../types.js';

@customElement('umb-search-details-view')
export class UmbSearchDetailsViewElement extends UmbLitElement {
  @state()
  private _state?: UmbSearchIndexState;

  constructor() {
    super();

    this.consumeContext(UMB_SEARCH_WORKSPACE_CONTEXT, (context) => {
      if (!context) return;
      this.observe(
        context.state,
        (state) => {
          this._state = state;
        },
        '_observeState',
      );
    });
  }

  override render() {
    // Show loading state - replaces entire extension slot
    if (this._state === 'loading') {
      return html`
        <div class="loading-state">
          <uui-loader></uui-loader>
          <span><umb-localize key="search_rebuildingIndex">Rebuilding index...</umb-localize></span>
        </div>
      `;
    }

    // Normal state - show all extension boxes
    return html`
      <div class="container">
        <umb-extension-slot type="searchIndexDetailBox"></umb-extension-slot>
      </div>
    `;
  }

  static override styles = [
    UmbTextStyles,
    css`
      :host {
        display: block;
        padding: var(--uui-size-layout-1);
      }

      .container {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-layout-1);
      }

      .loading-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        min-height: 400px;
        gap: var(--uui-size-space-4);
      }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    'umb-search-details-view': UmbSearchDetailsViewElement;
  }
}
