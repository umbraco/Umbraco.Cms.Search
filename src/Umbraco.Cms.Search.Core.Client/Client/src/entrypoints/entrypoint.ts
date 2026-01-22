import { client } from '../api/client.gen.js';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from '@umbraco-cms/backoffice/extension-api';

export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
  host.consumeContext(UMB_AUTH_CONTEXT, authContext => {
    const config = authContext?.getOpenApiConfiguration();
    // Set the auth token on the generated client
    client.setConfig({
      baseUrl: config?.base,
      credentials: config?.credentials ?? 'include',
      auth: config?.token
    });
  })
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
  // Empty
};
