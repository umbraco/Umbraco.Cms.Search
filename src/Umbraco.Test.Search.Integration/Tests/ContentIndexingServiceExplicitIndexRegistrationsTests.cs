﻿using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
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

        builder.Services.AddTransient<TestIndexer>();
        builder.Services.AddTransient<TestContentChangeStrategy>(_ => Strategy);

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<TestIndexer, TestIndexer, TestContentChangeStrategy>(Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<TestIndexer, TestIndexer, TestContentChangeStrategy>(Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
        });
    }

    [Test]
    public void IndexesAreRegistered()
    {
        IContentIndexingService sut = GetRequiredService<IContentIndexingService>();
        sut.Handle([ContentChange.Document(Guid.NewGuid(), ChangeImpact.Refresh, ContentState.Published)]);

        // one change strategy registered (same for both indexes)
        Assert.That(Strategy.HandledIndexInfos, Has.Count.EqualTo(1));
        // ...invoked twice
        Assert.That(Strategy.HandledIndexInfos[0], Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(Strategy.HandledIndexInfos[0][0].IndexAlias, Is.EqualTo(Constants.IndexAliases.PublishedContent));
            Assert.That(Strategy.HandledIndexInfos[0][0].Indexer, Is.TypeOf<TestIndexer>());

            Assert.That(Strategy.HandledIndexInfos[0][1].IndexAlias, Is.EqualTo(Constants.IndexAliases.DraftContent));
            Assert.That(Strategy.HandledIndexInfos[0][1].Indexer, Is.TypeOf<TestIndexer>());
        });
    }
}
