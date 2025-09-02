using System.Diagnostics;
using System.Reflection;
using Examine;
using Examine.Lucene.Providers;
using NUnit.Framework;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Examine.Integration.Attributes;
using Umbraco.Test.Search.Examine.Integration.Extensions;
using Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class TestBase : UmbracoIntegrationTest
{
    protected static readonly Guid RootKey = Guid.Parse("D9EBF985-C65C-4341-955F-FFADA160F6D9");
    protected static readonly Guid ChildKey = Guid.Parse("C84E91B2-3351-4BA9-9906-09C2260D77EC");
    protected static readonly Guid GrandchildKey = Guid.Parse("201858C2-5AC2-4505-AC2E-E4BF38F39AC4");
    private bool _indexingComplete;

    protected DateTime CurrentDateTime { get; set; }

    protected DateTimeOffset CurrentDateTimeOffset { get; } = DateTimeOffset.Now;

    protected decimal DecimalValue { get; } = 12.431167165486823626216m;

    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    protected IContentService ContentService => GetRequiredService<IContentService>();

    protected IDataTypeService DataTypeService => GetRequiredService<IDataTypeService>();

    protected ILocalizationService LocalizationService => GetRequiredService<ILocalizationService>();

    protected void SaveAndPublish(IContent content)
    {
        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);
    }

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddExamineSearchProviderForTest<TestIndex, TestInMemoryDirectoryFactory>();

        builder.AddSearchCore();

        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();

        // the core ConfigureBuilderAttribute won't execute from other assemblies at the moment, so this is a workaround
        var testType = Type.GetType(TestContext.CurrentContext.Test.ClassName!);
        if (testType is not null)
        {
            MethodInfo? methodInfo = testType.GetMethod(TestContext.CurrentContext.Test.Name);
            if (methodInfo is not null)
            {
                foreach (ConfigureUmbracoBuilderAttribute attribute in methodInfo.GetCustomAttributes(typeof(ConfigureUmbracoBuilderAttribute), true).OfType<ConfigureUmbracoBuilderAttribute>())
                {
                    attribute.Execute(builder, testType);
                }
            }
        }
    }

    protected async Task WaitForIndexing(string indexAlias, Func<Task> indexUpdatingAction)
    {
        var index = (LuceneIndex)GetRequiredService<IExamineManager>().GetIndex(indexAlias);
        index.IndexCommitted += IndexCommited;

        var hasDoneAction = false;

        var stopWatch = Stopwatch.StartNew();

        while (_indexingComplete is false)
        {
            if (hasDoneAction is false)
            {
                await indexUpdatingAction();
                hasDoneAction = true;
            }

            if (stopWatch.ElapsedMilliseconds > 5000)
            {
                throw new TimeoutException("Indexing timed out");
            }

            await Task.Delay(250);
        }

        _indexingComplete = false;
        index.IndexCommitted -= IndexCommited;
    }

    private void IndexCommited(object? sender, EventArgs e)
    {
        _indexingComplete = true;
    }

    protected string GetIndexAlias(bool publish) => publish ? Cms.Search.Core.Constants.IndexAliases.PublishedContent : Cms.Search.Core.Constants.IndexAliases.DraftContent;
}
