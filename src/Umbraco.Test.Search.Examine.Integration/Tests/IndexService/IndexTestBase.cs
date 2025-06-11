using Examine;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class IndexTestBase : TestBase
{
    protected DateTimeOffset CurrentDateTimeOffset = DateTimeOffset.UtcNow;

    protected double DecimalValue = 12.43;
    protected Guid RootKey { get; } = Guid.NewGuid();
    
    protected Guid ChildKey { get; } = Guid.NewGuid();

    protected Guid GrandchildKey { get; } = Guid.NewGuid();
    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    protected IContentService ContentService => GetRequiredService<IContentService>();
    protected IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
    protected IDataTypeService DataTypeService => GetRequiredService<IDataTypeService>();
    protected ILocalizationService LocalizationService => GetRequiredService<ILocalizationService>();

    protected void SaveAndPublish(IContent content)
    {
        ContentService.Save(content);
        ContentService.Publish(content, new []{ "*"});
        Thread.Sleep(3000);
    }
    
}