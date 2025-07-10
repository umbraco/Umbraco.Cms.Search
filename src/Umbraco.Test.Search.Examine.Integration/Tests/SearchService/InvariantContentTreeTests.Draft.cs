using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public partial class InvariantContentTreeTests : SearcherTestBase
{
    [Test]
    public async Task DraftStructure_WithRootInRecycleBin_YieldsAllDocuments()
    {
        CreateInvariantDocumentTree(false);
        var root = ContentService.GetById(RootKey);
        var result = ContentService.MoveToRecycleBin(root);
        Thread.Sleep(3000);
        
        var indexAlias = GetIndexAlias(false);
        var rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        var childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        var grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);
        
        Assert.Multiple(() =>
        {
            Assert.That(rootResult.Total, Is.EqualTo(1));
            Assert.That(childResult.Total, Is.EqualTo(1));
            Assert.That(grandChildResult.Total, Is.EqualTo(1));
            Assert.That(rootResult.Documents.First().Id, Is.EqualTo(RootKey));
            Assert.That(childResult.Documents.First().Id, Is.EqualTo(ChildKey));
            Assert.That(grandChildResult.Documents.First().Id, Is.EqualTo(GrandchildKey));
        });
    }
    
        
    [Test]
    public async Task DraftStructure_WithChildDeleted_YieldsNothingBelowRoot()
    {
        CreateInvariantDocumentTree(false);
        var child = ContentService.GetById(ChildKey);
        ContentService.Delete(child);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var indexAlias = GetIndexAlias(false);
        var rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        var childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        var grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);
        
        Assert.Multiple(() =>
        {
            Assert.That(rootResult.Total, Is.EqualTo(1));
            Assert.That(childResult.Total, Is.EqualTo(0));
            Assert.That(grandChildResult.Total, Is.EqualTo(0));
            Assert.That(rootResult.Documents.First().Id, Is.EqualTo(RootKey));
        });
    }
    
    [Test]
    public async Task DraftStructure_WithGrandchildDeleted_YieldsNothingBelowChild()
    {
        CreateInvariantDocumentTree(false);
        var grandchild = ContentService.GetById(GrandchildKey);
        ContentService.Delete(grandchild);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var indexAlias = GetIndexAlias(false);
        var rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        var childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        var grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);
        
        Assert.Multiple(() =>
        {
            Assert.That(rootResult.Total, Is.EqualTo(1));
            Assert.That(childResult.Total, Is.EqualTo(1));
            Assert.That(grandChildResult.Total, Is.EqualTo(0));
            Assert.That(rootResult.Documents.First().Id, Is.EqualTo(RootKey));
            Assert.That(childResult.Documents.First().Id, Is.EqualTo(ChildKey));
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