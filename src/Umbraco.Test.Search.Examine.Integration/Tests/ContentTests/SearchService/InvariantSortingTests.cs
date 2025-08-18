using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public class InvariantSortingTests : SearcherTestBase
{
    private IContentType ContentType { get; set; } = null!;


    [TestCase(Direction.Descending)]
    [TestCase(Direction.Ascending)]
    public async Task CanSortIntegers(Direction direction)
    {
        int[] integers = [9, 3, 4, 2, 5, 7, 1, 11, 10];
        var keys = (await CreateCountDocuments(integers)).OrderBy(x => x.Value, direction).ToArray();

        var indexAlias = GetIndexAlias(true);
        var result = await Searcher.SearchAsync(
            indexAlias,
            null,
            [new IntegerRangeFilter("count", [new IntegerRangeFilterRange(null, null)], false)],
            null,
            [new IntegerSorter("count", direction)],
            null,
            null,
            null,
            0,
            100);

        Assert.Multiple(() =>
        {
            var documents = result.Documents.ToArray();
            Assert.That(documents, Is.Not.Empty);
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.That(documents[i].Id, Is.EqualTo(keys[i].Key));
            }
        });
    }

    [TestCase(true, Direction.Descending)]
    [TestCase(false, Direction.Ascending)]
    public async Task CanSortDecimals(bool publish, Direction direction)
    {
        double[] doubles = [5,12412d, 0,51251d, 1.15215d, 3.251d, 2.2515125d, 125.5215d, 142.214124d];
        var keys = (await CreateDecimalDocuments(doubles)).OrderBy(x => x.Value, direction).ToArray();

        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(
            indexAlias,
            null,
            [new DecimalRangeFilter("decimalproperty", [new DecimalRangeFilterRange(null, null)], false)],
            null,
            [new DecimalSorter("decimalproperty", direction)],
            null,
            null,
            null,
            0,
            100);

        Assert.Multiple(() =>
        {
            var documents = result.Documents.ToArray();
            Assert.That(documents, Is.Not.Empty);
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.That(documents[i].Id, Is.EqualTo(keys[i].Key));
            }
        });
    }

    [TestCase(true, Direction.Descending)]
    [TestCase(false, Direction.Ascending)]
    public async Task CanSortDateTimeOffsets(bool publish, Direction direction)
    {
        DateTime[] dateTimes = [new(2025, 06, 06), new(2025, 02, 01), new(2024, 01, 01), new(2019, 01, 01), new(2000, 01, 01), new(2003, 01, 01)];

        var keys = (await CreateDatetimeDocuments(dateTimes)).OrderBy(x => x.Value, direction).ToArray();

        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(
            indexAlias,
            null,
            [new DateTimeOffsetRangeFilter("datetime", [new DateTimeOffsetRangeFilterRange(null, null)], false)],
            null,
            [new DateTimeOffsetSorter("datetime", direction)],
            null,
            null,
            null,
            0,
            100);

        Assert.Multiple(() =>
        {
            var documents = result.Documents.ToArray();
            Assert.That(documents, Is.Not.Empty);
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.That(documents[i].Id, Is.EqualTo(keys[i].Key));
            }
        });
    }

    [TestCase(true, Direction.Descending)]
    [TestCase(false, Direction.Ascending)]
    public async Task CanSortKeywords(bool publish, Direction direction)
    {
        var keys = (await CreateDocuments(5)).OrderBy(x => x, direction).Select(x => x.ToString()).ToArray();

        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(
            indexAlias,
            null,
            [new KeywordFilter("Umb_Id", keys.ToArray(), false)],
            null,
            [new KeywordSorter("Umb_Id", direction)],
            null,
            null,
            null,
            0,
            100);

        Assert.Multiple(() =>
        {
            var documents = result.Documents.ToArray();
            Assert.That(documents, Is.Not.Empty);
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.That(documents[i].Id.ToString(), Is.EqualTo(keys[i]));
            }
        });
    }

    [TestCase(true, Direction.Descending)]
    [TestCase(false, Direction.Ascending)]
    public async Task CanSortTexts(bool publish, Direction direction)
    {
        string[] titles = ["aa", "ccc", "bbb", "ddd", "zzz", "xxx"];

        var keys = (await CreateTitleDocuments(titles)).OrderBy(x => x.Value, direction).ToArray();

        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(
            indexAlias,
            null,
            [new TextFilter("title", titles, false)],
            null,
            [new TextSorter("title", direction)],
            null,
            null,
            null,
            0,
            100);

        Assert.Multiple(() =>
        {
            var documents = result.Documents.ToArray();
            Assert.That(documents, Is.Not.Empty);
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.That(documents[i].Id, Is.EqualTo(keys[i].Key));
            }
        });
    }

    // TODO: Remake these with actual properties in different r1 texts...
    // [TestCase(true, Direction.Descending)]
    // [TestCase(false, Direction.Ascending)]
    // public async Task CanSortByScore(bool publish, Direction direction)
    // {
    //     string[] titles = ["exact", "exact aa ", "exact aaa aaaa", "exact aaa aaaa aaaaa"];
    //
    //     var keys = (await CreateTitleDocuments(titles)).OrderBy(x => x.Value, direction).ToArray();
    //
    //     var indexAlias = GetIndexAlias(publish);
    //     var result = await Searcher.SearchAsync(
    //         indexAlias,
    //         "exact",
    //         null,
    //         null,
    //         [new ScoreSorter(direction)],
    //         null,
    //         null,
    //         null,
    //         0,
    //         100);
    //
    //     Assert.Multiple(() =>
    //     {
    //         var documents = result.Documents.ToArray();
    //         Assert.That(documents, Is.Not.Empty);
    //         for (int i = 0; i < keys.Length; i++)
    //         {
    //             Assert.That(documents[i].Id, Is.EqualTo(keys[i].Key));
    //         }
    //     });
    // }


    private async Task CreateCountDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("count")
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }

    private async Task CreateDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }

    private async Task<IEnumerable<Guid>> CreateDocuments(int numberOfDocuments)
    {
        var keys = new List<Guid>();
        await CreateDocType();

        for (int i = 0; i < numberOfDocuments; i++)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{i}")
                .Build();

            ContentService.Save(document);
            ContentService.Publish(document, new []{ "*"});
            keys.Add(document.Key);
        }

        Thread.Sleep(3000);
        return keys;
    }

    private async Task CreateTitleDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("title")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }

    private async Task<Dictionary<Guid, string>> CreateTitleDocuments(string[] values)
    {
        var keys = new Dictionary<Guid, string>();
        await CreateTitleDocType();

        foreach (var stringValue in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{stringValue}")
                .WithPropertyValues(
                    new
                    {
                        title = stringValue
                    })
                .Build();

            ContentService.Save(document);
            ContentService.Publish(document, new []{ "*"});
            keys.Add(document.Key, stringValue);
        }

        Thread.Sleep(3000);
        return keys;
    }

    private async Task CreateDatetimeDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("datetime")
            .WithDataTypeId(Constants.DataTypes.DateTime)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DateTime)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }

    private async Task<Dictionary<Guid, DateTime>> CreateDatetimeDocuments(DateTime[] values)
    {
        var keys = new Dictionary<Guid, DateTime>();
        await CreateDatetimeDocType();

        foreach (var dateTimeOffset in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{dateTimeOffset.ToString()}")
                .WithPropertyValues(
                    new
                    {
                        datetime = dateTimeOffset
                    })
                .Build();

            ContentService.Save(document);
            ContentService.Publish(document, new []{ "*"});
            keys.Add(document.Key, dateTimeOffset);
        }

        Thread.Sleep(3000);
        return keys;
    }

    private async Task CreateDecimalDocType()
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
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("decimalproperty")
            .WithDataTypeId(dataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }

    private async Task<Dictionary<Guid, double>> CreateDecimalDocuments(double[] values)
    {
        var keys = new Dictionary<Guid, double>();
        await CreateDecimalDocType();

        foreach (var doubleValue in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{doubleValue.ToString()}")
                .WithPropertyValues(
                    new
                    {
                        decimalproperty = doubleValue
                    })
                .Build();

            ContentService.Save(document);
            ContentService.Publish(document, new []{ "*"});
            keys.Add(document.Key, doubleValue);
        }

        Thread.Sleep(3000);
        return keys;
    }

    private async Task<Dictionary<Guid, int>> CreateCountDocuments(int[] values)
    {
        var keys = new Dictionary<Guid, int>();
        await CreateCountDocType();

        foreach (var countValue in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{countValue}")
                .WithPropertyValues(
                    new
                    {
                        count = countValue,
                    })
                .Build();

            ContentService.Save(document);
            ContentService.Publish(document, new []{ "*"});
            keys.Add(document.Key, countValue);
        }

        Thread.Sleep(3000);
        return keys;
    }
}
