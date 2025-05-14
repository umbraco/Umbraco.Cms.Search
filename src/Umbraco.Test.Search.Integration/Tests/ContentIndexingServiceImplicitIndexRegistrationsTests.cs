﻿using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
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

        builder.Services.AddTransient<IIndexer, TestIndexer>();
        builder.Services.AddTransient<ISearcher, TestIndexer>();
        builder.Services.AddTransient<IPublishedContentChangeStrategy>(_ => Strategy);
        builder.Services.AddTransient<IDraftContentChangeStrategy>(_ => Strategy);

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<IIndexer, ISearcher, IPublishedContentChangeStrategy>(Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
        });
    }

    [Test]
    public void IndexesAreRegistered()
    {
        var sut = GetRequiredService<IContentIndexingService>();
        sut.Handle([ContentChange.Document(Guid.NewGuid(), ChangeImpact.Refresh, ContentState.Published)]);

        // two different change strategies registered (although it's the implementation)
        Assert.That(Strategy.HandledIndexInfos, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            // ...each invoked once
            Assert.That(Strategy.HandledIndexInfos[0], Has.Count.EqualTo(1));
            Assert.That(Strategy.HandledIndexInfos[1], Has.Count.EqualTo(1));
        });

        Assert.Multiple(() =>
        {
            Assert.That(Strategy.HandledIndexInfos[0][0].IndexAlias, Is.EqualTo(Constants.IndexAliases.PublishedContent));
            Assert.That(Strategy.HandledIndexInfos[0][0].Indexer, Is.TypeOf<TestIndexer>());
            
            Assert.That(Strategy.HandledIndexInfos[1][0].IndexAlias, Is.EqualTo(Constants.IndexAliases.DraftContent));
            Assert.That(Strategy.HandledIndexInfos[1][0].Indexer, Is.TypeOf<TestIndexer>());
        });
    }
}