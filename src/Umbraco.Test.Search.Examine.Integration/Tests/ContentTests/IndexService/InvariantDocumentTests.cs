using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public class InvariantDocumentTests : IndexTestBase
{
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAnyDocument(bool publish)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute().ToArray();
        Assert.That(results.Length, Is.EqualTo(1));
        Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanRemoveAnyDocument(bool publish)
    {
        var content = ContentService.GetById(RootKey);
        ContentService.Delete(content);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Empty);
    }
    
    [Test]
    public void CanRemoveUnpublishedDocument()
    {
        var content = ContentService.GetById(RootKey);
        ContentService.Unpublish(content);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Empty);
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexTextProperty(bool publish)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("Umb_title_texts");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == "Umb_title_texts").Value, Is.EqualTo("The root title"));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexIntegerValues(bool publish)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("Umb_count_integers");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == "Umb_count_integers").Value, Is.EqualTo("12"));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDecimalValues(bool publish)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("Umb_decimalproperty_decimals");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == "Umb_decimalproperty_decimals").Value, Is.EqualTo(((double)DecimalValue).ToString()));
    }    
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDateTimeValues(bool publish)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("Umb_datetime_datetimeoffsets");
        var results = queryBuilder.Execute();
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

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.Search(updatedValue.ToString());
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Value == updatedValue.ToString()).Value, Is.EqualTo(updatedValue.ToString()));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAggregatedTexts(bool publish)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("Umb_aggregated_texts");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().AllValues.First(x => x.Key == "Umb_aggregated_texts").Value.Contains("The root title"), Is.True);
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

        CurrentDateTime = CurrentDateTimeOffset.DateTime.TruncateTo(DateTimeExtensions.DateTruncate.Second);

        var root = new ContentBuilder()
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

        ContentService.Save(root);
        ContentService.Publish(root, new []{ "*"});
        Thread.Sleep(3000);
        
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }
    
    private void UpdateProperty(string propertyName, object value, bool publish)
    {
        var content = ContentService.GetById(RootKey);
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