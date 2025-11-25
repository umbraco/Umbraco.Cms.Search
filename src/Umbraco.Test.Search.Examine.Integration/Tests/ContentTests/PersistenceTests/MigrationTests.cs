using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Install;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.PersistenceTests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class MigrationTests : UmbracoIntegrationTest
{
    private IScopeProvider _scopeProvider => GetRequiredService<IScopeProvider>();
    private PackageMigrationRunner _packageMigrationRunner => GetRequiredService<PackageMigrationRunner>();

    private IRuntimeState RuntimeState => Services.GetRequiredService<IRuntimeState>();


    [Test]
    public async Task MigrationHasRun()
    {
        var resultOfMigration = await _packageMigrationRunner.RunPackageMigrationsIfPendingAsync("Umbraco CMS Search").ConfigureAwait(false);
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        Assert.That(RuntimeState.Level, Is.EqualTo(RuntimeLevel.Run));

        IEnumerable<string> tables = scope.Database.SqlContext.SqlSyntax.GetTablesInSchema(scope.Database);
        var result = tables.Any(x => x.InvariantEquals(Constants.Persistence.DocumentTableName));
        Assert.That(result, Is.True);
    }

    // private async Task RunMigration(IUmbracoDatabase database)
    // {
    //     var contx = new MigrationContext(new CustomPackageMigrationPlan(), database, NullLogger<MigrationContext>.Instance);
    //     var migration = new CustomPackageMigration(
    //         GetRequiredService<IPackagingService>(),
    //         GetRequiredService<IMediaService>(),
    //         GetRequiredService<MediaFileManager>(),
    //         GetRequiredService<MediaUrlGeneratorCollection>(),
    //         GetRequiredService<IShortStringHelper>(),
    //         GetRequiredService<IContentTypeBaseServiceProvider>(),
    //         GetRequiredService<IMigrationContext>(),
    //         GetRequiredService<IOptions<PackageMigrationSettings>>());
    //
    //     await migration.RunAsync().ConfigureAwait(false);
    // }
}
