using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;

namespace Umbraco.Cms.Search.Core.Persistence.Migration;

public class AddIndexDocumentFieldsTableMigration : AsyncPackageMigrationBase
{
    public AddIndexDocumentFieldsTableMigration(
        IPackagingService packagingService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IMigrationContext context,
        IOptions<PackageMigrationSettings> packageMigrationsSettings)
        : base(
            packagingService,
            mediaService,
            mediaFileManager,
            mediaUrlGenerators,
            shortStringHelper,
            contentTypeBaseServiceProvider,
            context,
            packageMigrationsSettings)
    {
    }

    protected override Task MigrateAsync()
    {
        if (TableExists(Constants.Persistence.IndexDocumentFieldsTableName) == false)
        {
            Create.Table<IndexDocumentFieldsDto>().Do();
        }

        if (ColumnExists(Constants.Persistence.IndexDocumentTableName, "fields"))
        {
            Delete.FromTable(Constants.Persistence.IndexDocumentTableName).AllRows().Do();
            Delete.Column("fields").FromTable(Constants.Persistence.IndexDocumentTableName).Do();
        }

        return Task.CompletedTask;
    }
}
