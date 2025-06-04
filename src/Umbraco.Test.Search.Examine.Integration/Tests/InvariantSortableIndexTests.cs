using Examine;
using Examine.Lucene;
using Examine.Search;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class InvariantSortableIndexTests : IndexTestBase
{
    public IContentType ContentType { get; set; }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetSortedTitles(bool publish)
    {
        await CreateTitleDocuments(["C Title", "A Title", "B Title"]);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().OrderBy(new SortableField("sortableTitle_texts", SortType.String)).Execute();
        var values = results
            .SelectMany(x => x.Values.Where(x => x.Key == "sortableTitle_texts")).Select(x => x.Value);
        
        Assert.Multiple(() =>
        {
            Assert.That(results, Is.Not.Empty);
            Assert.That(values.First(), Is.EqualTo("A Title"));
            Assert.That(values.Skip(1).First(), Is.EqualTo("B Title"));
            Assert.That(values.Last(), Is.EqualTo("C Title"));
        });
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetUnSortedTitles(bool publish)
    {
        await CreateTitleDocuments(["C Title", "A Title", "B Title"]);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().OrderBy(new SortableField("title_texts", SortType.String)).Execute();
        var values = results
            .SelectMany(x => x.Values.Where(x => x.Key == "title_texts")).Select(x => x.Value);
        
        Assert.Multiple(() =>
        {
            Assert.That(results, Is.Not.Empty);
            Assert.That(values.First(), Is.EqualTo("C Title"));
            Assert.That(values.Skip(1).First(), Is.EqualTo("A Title"));
            Assert.That(values.Last(), Is.EqualTo("B Title"));
        });
    }
    
    
    private async Task CreateTitleDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("sortableTitle")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("title")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }
    
    private async Task CreateTitleDocuments(string[] values)
    {
        await CreateTitleDocType();

        foreach (var stringValue in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{stringValue}")
                .WithPropertyValues(
                    new
                    {
                        sortableTitle = stringValue,
                        title = stringValue
                    })
                .Build();
            
            SaveAndPublish(document);
        }
    }
}