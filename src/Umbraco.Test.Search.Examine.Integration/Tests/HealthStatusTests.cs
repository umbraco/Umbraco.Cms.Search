using NUnit.Framework;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.ViewModels;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
public class HealthStatusTests : SearcherTestBase
{
    private const string IndexAlias = Constants.IndexAliases.PublishedContent;

    [Test]
    [Order(1)]
    public async Task GetHealthStatus_IndexWithDocuments_ReturnsHealthy()
    {
        IIndexer indexer = GetRequiredService<IIndexer>();

        HealthStatus status = await indexer.GetHealthStatus(IndexAlias);

        Assert.That(status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    [Order(2)]
    public async Task GetHealthStatus_EmptyIndex_ReturnsEmpty()
    {
        IIndexer indexer = GetRequiredService<IIndexer>();

        // Reset to ensure index exists but is empty
        await indexer.ResetAsync(IndexAlias);

        HealthStatus status = await indexer.GetHealthStatus(IndexAlias);

        Assert.That(status, Is.EqualTo(HealthStatus.Empty));
    }

    [Test]
    [Order(3)]
    public async Task GetHealthStatus_UnknownIndex_ReturnsUnknown()
    {
        IIndexer indexer = GetRequiredService<IIndexer>();

        HealthStatus status = await indexer.GetHealthStatus("NonExistentIndex");

        Assert.That(status, Is.EqualTo(HealthStatus.Unknown));
    }
}
