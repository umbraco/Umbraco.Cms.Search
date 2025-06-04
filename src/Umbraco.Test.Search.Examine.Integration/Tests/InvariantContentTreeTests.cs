using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class InvariantContentTreeTests : IndexTestBase
{
    private const string RootTitle = "The root title";
    private const string ChildTitle = "The child title";
    private const string GrandChildTitle = "The grandchild title";
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDocumentTree(bool publish)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.Count(), Is.EqualTo(3));
    }
    
    [Test]
    public void UnpublishingRootWillRemoveDescendants()
    {
        var root = ContentService.GetById(RootKey);
        ContentService.Unpublish(root);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = publishedIndex.Searcher.Search(RootTitle);
        var publishedResultsChild = publishedIndex.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = publishedIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Empty);
            Assert.That(publishedResultsChild, Is.Empty);
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });

        AssertDraft();
    }
    
    [Test]
    public void UnpublishingChildWillRemoveDescendants()
    {
        var child = ContentService.GetById(ChildKey);
        ContentService.Unpublish(child);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = publishedIndex.Searcher.Search(RootTitle);
        var publishedResultsChild = publishedIndex.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = publishedIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Not.Empty);
            Assert.That(publishedResultsRoot.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(RootTitle));
            Assert.That(publishedResultsChild, Is.Empty);
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });

        AssertDraft();
    }
    
    [Test]
    public void UnpublishingGrandChildWillRemoveDescendants()
    {
        var grandChild = ContentService.GetById(GrandchildKey);
        ContentService.Unpublish(grandChild);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = publishedIndex.Searcher.Search(RootTitle);
        var publishedResultsChild = publishedIndex.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = publishedIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Not.Empty);
            Assert.That(publishedResultsRoot.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(RootTitle));
            Assert.That(publishedResultsChild, Is.Not.Empty);
            Assert.That(publishedResultsChild.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(ChildTitle));
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });
        
        AssertDraft();
    }
    
    [SetUp]
    public void CreateInvariantDocumentTree()
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

        SaveAndPublish(root);
        
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

        SaveAndPublish(child);

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
        
        SaveAndPublish(grandchild);

    }
    
    private void AssertDraft()
    {
        var draftIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);
        var draftResultsRoot = draftIndex.Searcher.Search(RootTitle);
        var draftResultsChild = draftIndex.Searcher.Search(ChildTitle);
        var draftResultsGrandChild = draftIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(draftResultsRoot, Is.Not.Empty);
            Assert.That(draftResultsRoot.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(RootTitle));            
            Assert.That(draftResultsChild, Is.Not.Empty);
            Assert.That(draftResultsChild.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(ChildTitle));    
            Assert.That(draftResultsGrandChild, Is.Not.Empty);
            Assert.That(draftResultsGrandChild.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(GrandChildTitle));
        });
    }
}