using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public class ContentIndexingServiceExplicitIndexRegistrationsTests : ContentIndexingServiceTestsBase
{
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.Services.AddTransient<TestIndexService>();
        builder.Services.AddTransient<TestContentChangeStrategy>(_ => Strategy);

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<TestIndexService, TestContentChangeStrategy>(Constants.IndexAliases.PublishedContent);
            options.RegisterIndex<TestIndexService, TestContentChangeStrategy>(Constants.IndexAliases.DraftContent);
        });
    }

    [Test]
    public void IndexesAreRegistered()
    {
        var sut = GetRequiredService<IContentIndexingService>();
        sut.Handle([new ContentChange(Guid.NewGuid(), ContentChangeType.Refresh, true)]);

        // one change strategy registered (same for both indexes)
        Assert.That(Strategy.HandledIndexInfos, Has.Count.EqualTo(1));
        // ...invoked twice
        Assert.That(Strategy.HandledIndexInfos[0], Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(Strategy.HandledIndexInfos[0][0].IndexAlias, Is.EqualTo(Constants.IndexAliases.PublishedContent));
            Assert.That(Strategy.HandledIndexInfos[0][0].IndexService, Is.TypeOf<TestIndexService>());
            
            Assert.That(Strategy.HandledIndexInfos[0][1].IndexAlias, Is.EqualTo(Constants.IndexAliases.DraftContent));
            Assert.That(Strategy.HandledIndexInfos[0][1].IndexService, Is.TypeOf<TestIndexService>());
        });
    }
}