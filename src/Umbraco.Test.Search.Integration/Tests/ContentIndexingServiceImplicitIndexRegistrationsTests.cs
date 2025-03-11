using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public class ContentIndexingServiceImplicitIndexRegistrationsTests : ContentIndexingServiceTestsBase
{
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.Services.AddTransient<IIndexService, TestIndexService>();
        builder.Services.AddTransient<IPublishedContentChangeStrategy>(_ => Strategy);
        builder.Services.AddTransient<IDraftContentChangeStrategy>(_ => Strategy);

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<IIndexService, IPublishedContentChangeStrategy>(Constants.IndexAliases.PublishedContent);
            options.RegisterIndex<IIndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftContent);
        });
    }

    [Test]
    public void IndexesAreRegistered()
    {
        var sut = GetRequiredService<IContentIndexingService>();
        sut.Handle([new ContentChange(Guid.NewGuid(), ContentChangeType.Refresh, true)]);

        // two different change strategies registered (although it's the implementation)
        Assert.That(Strategy.HandledIndexInfos, Has.Count.EqualTo(2));
        // ...each invoked once
        Assert.That(Strategy.HandledIndexInfos[0], Has.Count.EqualTo(1));
        Assert.That(Strategy.HandledIndexInfos[1], Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(Strategy.HandledIndexInfos[0][0].IndexAlias, Is.EqualTo(Constants.IndexAliases.PublishedContent));
            Assert.That(Strategy.HandledIndexInfos[0][0].IndexService, Is.TypeOf<TestIndexService>());
            
            Assert.That(Strategy.HandledIndexInfos[1][0].IndexAlias, Is.EqualTo(Constants.IndexAliases.DraftContent));
            Assert.That(Strategy.HandledIndexInfos[1][0].IndexService, Is.TypeOf<TestIndexService>());
        });
    }
}