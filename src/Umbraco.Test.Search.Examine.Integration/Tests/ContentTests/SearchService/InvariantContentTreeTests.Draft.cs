using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public partial class InvariantContentTreeTests : SearcherTestBase
{
    [Test]
    public async Task DraftStructure_WithRootInRecycleBin_YieldsAllDocuments()
    {
        await WaitForIndexing(GetIndexAlias(false), () =>
        {
            CreateInvariantDocumentTree(false);
            IContent root = ContentService.GetById(RootKey)!;
            ContentService.MoveToRecycleBin(root);
            return Task.CompletedTask;
        });

        var indexAlias = GetIndexAlias(false);
        SearchResult rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        SearchResult childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        SearchResult grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);

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
        var indexAlias = GetIndexAlias(false);
        await WaitForIndexing(indexAlias, () =>
        {
            CreateInvariantDocumentTree(false);
            return Task.CompletedTask;
        });

        await WaitForIndexing(indexAlias, () =>
        {
            IContent child = ContentService.GetById(ChildKey)!;
            ContentService.Delete(child);
            return Task.CompletedTask;
        });

        SearchResult rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        SearchResult childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        SearchResult grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);

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
        var indexAlias = GetIndexAlias(false);
        await WaitForIndexing(indexAlias, () =>
        {
            CreateInvariantDocumentTree(false);
            return Task.CompletedTask;
        });

        await WaitForIndexing(indexAlias, () =>
        {
            IContent grandchild = ContentService.GetById(GrandchildKey)!;
            ContentService.Delete(grandchild);
            return Task.CompletedTask;
        });

        SearchResult rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        SearchResult childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        SearchResult grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);

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
        DataType dataType = new DataTypeBuilder()
            .WithId(0)
            .WithoutIdentity()
            .WithDatabaseType(ValueStorageType.Decimal)
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();

        DataTypeService.Save(dataType);
        IContentType contentType = new ContentTypeBuilder()
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

        Content root = new ContentBuilder()
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


        Content child = new ContentBuilder()
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

        Content grandchild = new ContentBuilder()
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
    }
}
