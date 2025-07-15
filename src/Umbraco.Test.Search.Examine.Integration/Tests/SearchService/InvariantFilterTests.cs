using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public class InvariantFilterTests : SearcherTestBase
{
    
    [TestCase("RootKey", 2, false)]
    [TestCase("ChildKey", 1, false)]
    [TestCase("GrandchildKey", 0, false)]  
    [TestCase("RootKey", 0, true)]
    [TestCase("ChildKey", 1, true)]
    [TestCase("GrandchildKey", 2, true)]
    public async Task CanFilterByPathIds(string keyName, int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        // Resolve the keys, we cannot use them directly in TestCase, as they are not constant.. :(
        var key = keyName switch
        {
            "RootKey" => RootKey.ToString(),
            "ChildKey" => ChildKey.ToString(),
            "GrandchildKey" => GrandchildKey.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(keyName), keyName, null)
        };

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new KeywordFilter("Umb_PathIds", [key], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
        
    [TestCase("RootKey", 1, false)]
    [TestCase("ChildKey", 1, false)]
    [TestCase("GrandchildKey", 0, false)]    
    [TestCase("RootKey", 2, true)]
    [TestCase("ChildKey", 2, true)]
    [TestCase("GrandchildKey", 3, true)]
    public async Task CanFilterByParentId(string keyName, int count, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        // Resolve the keys, we cannot use them directly in TestCase, as they are not constant.. :(
        var key = keyName switch
        {
            "RootKey" => RootKey.ToString(),
            "ChildKey" => ChildKey.ToString(),
            "GrandchildKey" => GrandchildKey.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(keyName), keyName, null)
        };

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new KeywordFilter("Umb_ParentId", [key], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.Multiple(() =>
        {
            Assert.That(results.Documents.Count(), Is.EqualTo(count));
        });
    }
    
    [TestCase(1, true)]
    [TestCase(2, false)]
    public async Task CanFilterByText(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new TextFilter("title", ["Test"], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(0, true)]
    [TestCase(3, false)]
    public async Task CanFilterByIntegerRangeMinAndMaxNull(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new IntegerRangeFilter("count", [new FilterRange<int?>(null, null)], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(1, true)]
    [TestCase(2, false)]
    public async Task CanFilterByOneIntegerRange(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new IntegerRangeFilter("count", [new FilterRange<int?>(0, 50)], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(0, true)]
    [TestCase(3, false)]
    public async Task CanFilterByMultipleIntegerRanges(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new IntegerRangeFilter("count", [new FilterRange<int?>(0, 50), new FilterRange<int?>(0, 1000)], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(1, true)]
    [TestCase(2, false)]
    public async Task CanFilterByExactInteger(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new IntegerExactFilter("count", [12], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(1, true)]
    [TestCase(2, false)]
    public async Task CanFilterByOneDecimalRange(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new DecimalRangeFilter("decimalproperty", [new FilterRange<decimal?>(10m, 50m)], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(0, true)]
    [TestCase(3, false)]
    public async Task CanFilterByDecimalRangeWithMaxAndMinNull(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new DecimalRangeFilter("decimalproperty", [new FilterRange<decimal?>(null, null)], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(0, true)]
    [TestCase(3, false)]
    public async Task CanFilterByMultipleDecimalRanges(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new DecimalRangeFilter("decimalproperty", [new FilterRange<decimal?>(0m, 2m), new FilterRange<decimal?>(5m, 200m)], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(1, true)]
    [TestCase(2, false)]
    public async Task CanFilterByExactDecimal(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new DecimalExactFilter("decimalproperty", [DecimalValue], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(1, true)]
    [TestCase(2, false)]
    public async Task CanFilterByExactDatetimeOffset(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new DateTimeOffsetExactFilter("datetime", [ new DateTime(2025, 06, 06)], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(1, true)]
    [TestCase(2, false)]
    public async Task CanFilterByOneDatetimeOffsetRange(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new DateTimeOffsetRangeFilter("datetime", [new FilterRange<DateTimeOffset?>(new DateTimeOffset(new DateTime(2025, 01, 01)), new DateTimeOffset(new DateTime(2025, 12, 31)))], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(0, true)]
    [TestCase(3, false)]
    public async Task CanFilterByDatetimeOffsetRangeWithMaxAndMinNull(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { new DateTimeOffsetRangeFilter("datetime", [new FilterRange<DateTimeOffset?>(null, null)], negate) },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    [TestCase(0, true)]
    [TestCase(3, false)]
    public async Task CanFilterByMultipleDatetimeOffsetRanges(int expectedCount, bool negate)
    {
        CreateInvariantDocumentTree(false);
    
        var indexAlias = GetIndexAlias(false);

        var results = await Searcher.SearchAsync(
            indexAlias,
            null,
            new List<Filter> { 
                new DateTimeOffsetRangeFilter("datetime", [
                new FilterRange<DateTimeOffset?>(new DateTimeOffset(new DateTime(2023, 01, 01)), new DateTimeOffset(new DateTime(2024, 12, 31))),
                new FilterRange<DateTimeOffset?>(new DateTimeOffset(new DateTime(2025, 01, 01)), new DateTimeOffset(new DateTime(2025, 12, 31)))
                ],
                negate)
            },
            null, null, null, null, null,
            0, 100);

        Assert.That(results.Total, Is.EqualTo(expectedCount));
    }
    
    private void CreateInvariantDocumentTree(bool publish)
    {
        var dataType = new DataTypeBuilder()
            .WithId(0)
            .WithoutIdentity()
            .WithDatabaseType(ValueStorageType.Decimal)
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        
        DataTypeService.Save(dataType);
        var contentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("title")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("count")
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .AddPropertyType()
            .WithAlias("datetime")
            .WithDataTypeId(Constants.DataTypes.DateTime)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DateTime)
            .Done()
            .AddPropertyType()
            .WithAlias("decimalproperty")
            .WithDataTypeId(dataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Key, 0, contentType.Alias)];
        ContentTypeService.Save(contentType);

        var root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithName("Root")
            .WithPropertyValues(
                new
                {
                    title = "Test",
                    count = 12,
                    datetime =  new DateTime(2025, 06, 06),
                    decimalproperty = DecimalValue
                })
            .Build();

        if (publish)
        {
            SaveAndPublish(root);
        }
        else
        {
            ContentService.Save(root);
        }


        var child = new ContentBuilder()
            .WithKey(ChildKey)
            .WithContentType(contentType)
            .WithName("Child")
            .WithParent(root)
            .WithPropertyValues(
                new
                {
                    title = "Test",
                    count = 12,
                    datetime =  new DateTime(2025, 06, 06),
                    decimalproperty = DecimalValue
                })
            .Build();

        if (publish)
        {
            SaveAndPublish(child);
        }
        else
        {
            ContentService.Save(child);
        }

        var grandchild = new ContentBuilder()
            .WithKey(GrandchildKey)
            .WithContentType(contentType)
            .WithName("Grandchild")
            .WithParent(child)
            .WithPropertyValues(
                new
                {
                    title = "The grandchild title",
                    count = 100,
                    datetime = new DateTime(2024, 06, 07),
                    decimalproperty = 1.11111m
                })
            .Build();
        
        if (publish)
        {
            SaveAndPublish(grandchild);
        }
        else
        {
            ContentService.Save(grandchild);
        }

        Thread.Sleep(3000);
    }
}