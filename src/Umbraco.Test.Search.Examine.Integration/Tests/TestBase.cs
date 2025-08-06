using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class TestBase : UmbracoIntegrationTest
{
    protected static readonly Guid RootKey = Guid.Parse("D9EBF985-C65C-4341-955F-FFADA160F6D9");
    protected static readonly Guid ChildKey = Guid.Parse("C84E91B2-3351-4BA9-9906-09C2260D77EC");
    protected static readonly Guid GrandchildKey = Guid.Parse("201858C2-5AC2-4505-AC2E-E4BF38F39AC4");
    
    protected DateTime CurrentDateTime { get; set; }
    
    protected DateTimeOffset CurrentDateTimeOffset = DateTimeOffset.Now;

    protected decimal DecimalValue = 12.431167165486823626216m;
    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    protected IContentService ContentService => GetRequiredService<IContentService>();
    protected IDataTypeService DataTypeService => GetRequiredService<IDataTypeService>();
    protected ILocalizationService LocalizationService => GetRequiredService<ILocalizationService>();
    
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        
        builder.Services.AddSingleton<TestInMemoryDirectoryFactory>();
        builder.Services.ConfigureOptions<TestIndexConfigureOptions>();
        builder.Services.ConfigureOptions<ConfigureIndexOptions>();
        builder.AddExamineSearchProviderForTest<TestIndex, TestInMemoryDirectoryFactory>();

        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
    }
}