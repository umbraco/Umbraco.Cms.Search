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
                    title = "The root title",
                    count = 12,
                    datetime = CurrentDateTimeOffset.DateTime,
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
                    title = "The child title",
                    count = 12,
                    datetime = CurrentDateTimeOffset.DateTime,
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
                    count = 12,
                    datetime = CurrentDateTimeOffset.DateTime,
                    decimalproperty = DecimalValue
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