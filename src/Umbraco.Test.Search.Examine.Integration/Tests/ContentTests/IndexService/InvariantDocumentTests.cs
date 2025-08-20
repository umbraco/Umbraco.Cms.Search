using System.Globalization;
using Examine;
using Examine.Search;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Provider.Examine;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public class InvariantDocumentTests : IndexTestBase
{
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAnyDocument(bool publish)
    {
        IIndex index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        ISearchResult[] results = index.Searcher.CreateQuery().All().Execute().ToArray();
        Assert.That(results.Length, Is.EqualTo(1));
        Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CanRemoveAnyDocument(bool publish)
    {
        IContent content = ContentService.GetById(RootKey)!;
        ContentService.Delete(content);

        IIndex index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        ISearchResults results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void CanRemoveUnpublishedDocument()
    {
        IContent content = ContentService.GetById(RootKey)!;
        ContentService.Unpublish(content);

        IIndex index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        ISearchResults results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Empty);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexTextProperty(bool publish)
    {
        IIndex index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        IOrdering queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"{Constants.Fields.FieldPrefix}title_{Constants.Fields.Texts}");
        ISearchResults results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == $"{Constants.Fields.FieldPrefix}title_{Constants.Fields.Texts}").Value, Is.EqualTo("The root title"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexIntegerValues(bool publish)
    {
        IIndex index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        IOrdering queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"{Constants.Fields.FieldPrefix}count_{Constants.Fields.Integers}");
        ISearchResults results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == $"{Constants.Fields.FieldPrefix}count_{Constants.Fields.Integers}").Value, Is.EqualTo("12"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDecimalValues(bool publish)
    {
        IIndex index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        IOrdering queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"{Constants.Fields.FieldPrefix}decimalproperty_{Constants.Fields.Decimals}");
        ISearchResults results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(
            results
            .First()
            .Values
            .First(x => x.Key == $"{Constants.Fields.FieldPrefix}decimalproperty_{Constants.Fields.Decimals}")
            .Value,
            Is.EqualTo(((double)DecimalValue).ToString()));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDateTimeValues(bool publish)
    {
        IIndex index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        IOrdering queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"{Constants.Fields.FieldPrefix}datetime_{Constants.Fields.DateTimeOffsets}");
        ISearchResults results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo(CurrentDateTime.Ticks.ToString()));
    }


    [TestCase("title", "updated title", false)]
    [TestCase("title", "updated title", true)]
    [TestCase("count", 12, false)]
    [TestCase("count", 12, true)]
    [TestCase("decimalproperty", 1.45, false)]
    [TestCase("decimalproperty", 1.45, true)]
    public void CanIndexUpdatedProperties(string propertyName, object updatedValue, bool publish)
    {
        UpdateProperty(propertyName, updatedValue, publish);

        IIndex index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        ISearchResults results = index.Searcher.Search(updatedValue.ToString()!);
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Value == updatedValue.ToString()).Value, Is.EqualTo(updatedValue.ToString()));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAggregatedTexts(bool publish)
    {
        IIndex index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        IOrdering queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"{Constants.Fields.FieldPrefix}{Constants.Fields.AggregatedTexts}");
        ISearchResults results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().AllValues.First(x => x.Key == $"{Constants.Fields.FieldPrefix}{Constants.Fields.AggregatedTexts}").Value.Contains("The root title"), Is.True);
    }


    [SetUp]
    public async Task CreateInvariantDocument()
    {
        DataType dataType = new DataTypeBuilder()
            .WithId(0)
            .WithoutIdentity()
            .WithDatabaseType(ValueStorageType.Decimal)
            .AddEditor()
            .WithAlias(Cms.Core.Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();

        DataTypeService.Save(dataType);
        IContentType contentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("title")
            .WithDataTypeId(Cms.Core.Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Cms.Core.Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("count")
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Cms.Core.Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .AddPropertyType()
            .WithAlias("datetime")
            .WithDataTypeId(Cms.Core.Constants.DataTypes.DateTime)
            .WithPropertyEditorAlias(Cms.Core.Constants.PropertyEditors.Aliases.DateTime)
            .Done()
            .AddPropertyType()
            .WithAlias("decimalproperty")
            .WithDataTypeId(dataType.Id)
            .WithPropertyEditorAlias(Cms.Core.Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);

        CurrentDateTime = CurrentDateTimeOffset.DateTime.TruncateTo(DateTimeExtensions.DateTruncate.Second);

        Content root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithName("Root")
            .WithPropertyValues(
                new
                {
                    title = "The root title",
                    count = 12,
                    datetime = CurrentDateTime,
                    decimalproperty = DecimalValue
                })
            .Build();

        await WaitForIndexing(Cms.Search.Core.Constants.IndexAliases.PublishedContent, () =>
        {
            ContentService.Save(root);
            ContentService.Publish(root, ["*"]);
            return Task.CompletedTask;
        });

        IContent? content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }

    private void UpdateProperty(string propertyName, object value, bool publish)
    {
        IContent content = ContentService.GetById(RootKey)!;
        content.SetValue(propertyName, value);

        ContentService.Save(content);
        if (publish)
        {
            ContentService.Publish(content, ["*"]);
        }

        Thread.Sleep(3000);
    }
}
