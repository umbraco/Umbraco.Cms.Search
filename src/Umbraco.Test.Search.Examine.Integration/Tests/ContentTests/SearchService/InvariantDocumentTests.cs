using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public class InvariantDocumentTests : SearcherTestBase
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task SearchWithNoParamsYieldsNoDocuments(bool publish)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, null, null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(0));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchName(bool publish)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, "Test", null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchTextProperty(bool publish)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, "The root title", null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchIntegerValue(bool publish)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, "12", null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchDecimalValues(bool publish)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, DecimalValue.ToString(), null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchDateTimeValues(bool publish)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, CurrentDateTimeOffset.DateTime.ToString(), null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }


    [TestCase("title", "updated title", false)]
    [TestCase("title", "updated title", true)]
    [TestCase("count", 12, false)]
    [TestCase("count", 12, true)]
    [TestCase("decimalproperty", 1.45, false)]
    [TestCase("decimalproperty", 1.45, true)]
    public async Task CanSearchUpdatedProperties(string propertyName, object updatedValue, bool publish)
    {
        UpdateProperty(propertyName, updatedValue, publish);

        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, updatedValue.ToString(), null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchUpdatedDateTime(bool publish)
    {
        var updatedValue = new DateTime(2000, 1, 1);
        UpdateProperty("datetime", new DateTime(2000, 1, 1), publish);
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, updatedValue.ToString(), null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }

    [SetUp]
    public void CreateInvariantDocument()
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

        var root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithName("Test")
            .WithPropertyValues(
                new
                {
                    title = "The root title",
                    count = 12,
                    datetime = CurrentDateTimeOffset.DateTime,
                    decimalproperty = DecimalValue
                })
            .Build();

        ContentService.Save(root);
        ContentService.Publish(root, new []{ "*"});
        Thread.Sleep(3000);

        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }

    private void UpdateProperty(string propertyName, object value, bool publish)
    {
        var content = ContentService.GetById(RootKey)!;
        content.SetValue(propertyName, value);

        if (publish)
        {
            ContentService.Save(content);
            ContentService.Publish(content, ["*"]);
        }
        else
        {
            ContentService.Save(content);
        }

        Thread.Sleep(3000);
    }
}
