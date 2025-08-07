using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public partial class InvariantDocumentTreeTests
{
    [Test]
    public void DraftStructure_YieldsAllDocuments()
    {
        CreateInvariantDocumentTree(false);
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(3));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
            Assert.That(results[1].Id, Is.EqualTo(ChildKey.ToString()));
            Assert.That(results[2].Id, Is.EqualTo(GrandchildKey.ToString()));
        });
    }
    
    [Test]
    public void DraftStructure_YieldsNoPublishedDocuments()
    {
        CreateInvariantDocumentTree(false);
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.Count(), Is.EqualTo(0));
    }

    [Test]
    public void DraftStructure_WithRootInRecycleBin_YieldsAllDocuments()
    {
        CreateInvariantDocumentTree(false);
        var root = ContentService.GetById(RootKey);
        var result = ContentService.MoveToRecycleBin(root);
        Thread.Sleep(3000);
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);
        var results = index.Searcher.CreateQuery().All().Execute().ToArray();
        
        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(3));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
            Assert.That(results[1].Id, Is.EqualTo(ChildKey.ToString()));
            Assert.That(results[2].Id, Is.EqualTo(GrandchildKey.ToString()));
        });
    }
    
        
    [Test]
    public void DraftStructure_WithChildDeleted_YieldsNothingBelowRoot()
    {
        CreateInvariantDocumentTree(false);
        var child = ContentService.GetById(ChildKey);
        ContentService.Delete(child);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);
        var results = index.Searcher.CreateQuery().All().Execute().ToArray();
        
        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
        });
    }
    
    [Test]
    public void DraftStructure_WithGrandchildDeleted_YieldsNothingBelowChild()
    {
        CreateInvariantDocumentTree(false);
        var grandchild = ContentService.GetById(GrandchildKey);
        ContentService.Delete(grandchild);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);
        var results = index.Searcher.CreateQuery().All().Execute().ToArray();
        
        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
            Assert.That(results[1].Id, Is.EqualTo(ChildKey.ToString()));
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