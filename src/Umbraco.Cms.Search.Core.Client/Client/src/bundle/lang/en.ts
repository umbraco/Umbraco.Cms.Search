import { UmbLocalizationDictionary } from '@umbraco-cms/backoffice/localization-api';
import { HealthStatusModel } from '../../api';

export default {
  search: {
    treeHeader: 'Search',
    tableColumnAlias: 'Alias',
    tableColumnHealthStatus: 'Health Status',
    tableColumnDocumentCount: 'Document Count',
    healthStatus: (status: HealthStatusModel) => status,
    documentCount: (cnt: number) => {
      switch (cnt) {
        case 0:
          return 'Empty';
        case 1:
          return '1 document';
        default:
          return `${cnt} documents`;
      }
    },
    collectionActionReload: 'Refresh list',
    rebuildConfirmHeadline: 'Rebuild Search Index',
    rebuildConfirmMessage: 'Are you sure you want to rebuild the search index? This operation may take a while depending on the size of your content.',
    rebuildConfirmLabel: 'Rebuild Index',
    rebuildStartedMessage: 'The rebuild of search index "{0}" has started. You can continue working while the process runs in the background.',
    rebuildCompletedTitle: 'Search Index Rebuild Completed',
    rebuildCompletedMessage: 'The rebuild of search index "{0}" has completed successfully.',
  }
} satisfies UmbLocalizationDictionary;
