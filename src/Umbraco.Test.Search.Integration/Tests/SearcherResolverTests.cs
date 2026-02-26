using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Core.Models.ViewModels;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.None)]
public class SearcherResolverTests : UmbracoIntegrationTest
{
    private ISearcherResolver SearcherResolver => GetRequiredService<ISearcherResolver>();
    private Mock<ILogger<SearcherResolver>>? _loggerMock;

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddSearchCore();

        builder.Services.AddTransient<FirstSearcher>();
        builder.Services.AddTransient<SecondSearcher>();

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<TestIndexer, FirstSearcher, TestContentChangeStrategy>("FirstIndex", UmbracoObjectTypes.Document);
            options.RegisterIndex<TestIndexer, SecondSearcher, TestContentChangeStrategy>("SecondIndex", UmbracoObjectTypes.Document);
            options.RegisterIndex<TestIndexer, UnregisteredSearcher, TestContentChangeStrategy>("IndexWithUnregisteredSearcher", UmbracoObjectTypes.Document);
        });

        _loggerMock = new Mock<ILogger<SearcherResolver>>();
        builder.Services.AddSingleton(_loggerMock.Object);
    }

    [Test]
    public void FirstIndex_ResolvesFirstSearcher()
    {
        ISearcher? searcher = SearcherResolver.GetSearcher("FirstIndex");
        Assert.That(searcher, Is.Not.Null);
        Assert.That(searcher, Is.TypeOf<FirstSearcher>());
    }

    [Test]
    public void SecondIndex_ResolvesSecondSearcher()
    {
        ISearcher? searcher = SearcherResolver.GetSearcher("SecondIndex");
        Assert.That(searcher, Is.Not.Null);
        Assert.That(searcher, Is.TypeOf<SecondSearcher>());
    }

    [Test]
    public void UnknownIndex_ResolvesNoSearcher()
    {
        ISearcher? searcher = SearcherResolver.GetSearcher("UnknownIndex");
        Assert.That(searcher, Is.Null);
        VerifyLogging(LogLevel.Warning, "No index registration was found");
    }

    [Test]
    public void UnregisteredSearcher_ResolvesNoSearcher()
    {
        ISearcher? searcher = SearcherResolver.GetSearcher("IndexWithUnregisteredSearcher");
        Assert.That(searcher, Is.Null);
        VerifyLogging(LogLevel.Error, "Could not resolve type");
    }

    private void VerifyLogging(LogLevel logLevel, string startOfMessage)
        => _loggerMock!.Verify(logger =>
            logger.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString()!.StartsWith(startOfMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

    private class FirstSearcher : SearcherBase
    {
    }

    private class SecondSearcher : SearcherBase
    {
    }

    private class UnregisteredSearcher : SearcherBase
    {
    }

    private class TestContentChangeStrategy : IContentChangeStrategy
    {
        public Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task RebuildAsync(IndexInfo indexInfo, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private class TestIndexer : IIndexer
    {
        public Task AddOrUpdateAsync(string indexAlias, Guid id, UmbracoObjectTypes objectType, IEnumerable<Variation> variations, IEnumerable<IndexField> fields, ContentProtection? protection)
            => throw new NotImplementedException();

        public Task DeleteAsync(string indexAlias, IEnumerable<Guid> ids)
            => throw new NotImplementedException();

        public Task ResetAsync(string indexAlias)
            => throw new NotImplementedException();

        public Task<IndexMetadata> GetMetadataAsync(string indexAlias)
            => Task.FromResult(new IndexMetadata(0, HealthStatus.Healthy));
    }

    private abstract class SearcherBase : ISearcher
    {
        public Task<SearchResult> SearchAsync(
            string indexAlias,
            string? query = null,
            IEnumerable<Filter>? filters = null,
            IEnumerable<Facet>? facets = null,
            IEnumerable<Sorter>? sorters = null,
            string? culture = null,
            string? segment = null,
            AccessContext? accessContext = null,
            int skip = 0,
            int take = 10,
            int maxSuggestions = 0)
            => throw new NotImplementedException();
    }
}
