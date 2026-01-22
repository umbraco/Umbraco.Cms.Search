import { UmbLocalizationDictionary } from '@umbraco-cms/backoffice/localization-api';
import {HealthStatusModel} from "../api";

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
    }
  }
} satisfies UmbLocalizationDictionary;
