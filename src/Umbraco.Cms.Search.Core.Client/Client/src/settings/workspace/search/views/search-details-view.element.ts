import { html, customElement, css } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';

@customElement('umb-search-details-view')
export class UmbSearchDetailsViewElement extends UmbLitElement {
  override render() {
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
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    'umb-search-details-view': UmbSearchDetailsViewElement;
  }
}
