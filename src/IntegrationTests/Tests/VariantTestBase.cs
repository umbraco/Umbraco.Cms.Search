using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace IntegrationTests.Tests;

public abstract class VariantTestBase : TestBaseWithContent
{
    [SetUp]
    public void SetupTest()
    {
        GetRequiredService<ILocalizationService>().Save(
            new LanguageBuilder().WithCultureInfo("da-DK").Build()
        );
        
        var contentType = new ContentTypeBuilder()
            .WithAlias("variant")
            .WithContentVariation(ContentVariation.Culture)
            .AddPropertyType()
            .WithAlias("title")
            .WithVariations(ContentVariation.Culture)
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("count")
            .WithVariations(ContentVariation.Nothing)
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Id, 0)];
        ContentTypeService.Save(contentType);

        var root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Root EN")
            .WithCultureName("da-DK", "Root DA")
            .Build();
        root.SetValue("title", "The root title in English", "en-US");
        root.SetValue("title", "The root title in Danish", "da-DK");
        root.SetValue("count", 12);
        ContentService.Save(root);

        var child = new ContentBuilder()
            .WithKey(ChildKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Child EN")
            .WithCultureName("da-DK", "Child DA")
            .WithParent(root)
            .Build();
        child.SetValue("title", "The child title in English", "en-US");
        child.SetValue("title", "The child title in Danish", "da-DK");
        child.SetValue("count", 34);
        ContentService.Save(child);

        var grandchild = new ContentBuilder()
            .WithKey(GrandchildKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Grandchild EN")
            .WithCultureName("da-DK", "Grandchild DA")
            .WithParent(child)
            .Build();
        grandchild.SetValue("title", "The grandchild title in English", "en-US");
        grandchild.SetValue("title", "The grandchild title in Danish", "da-DK");
        grandchild.SetValue("count", 56);
        ContentService.Save(grandchild);

        var greatGrandchild = new ContentBuilder()
            .WithKey(GreatGrandchildKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Great Grandchild EN")
            .WithCultureName("da-DK", "Great Grandchild DA")
            .WithParent(grandchild)
            .Build();
        greatGrandchild.SetValue("title", "The great grandchild title in English", "en-US");
        greatGrandchild.SetValue("title", "The great grandchild title in Danish", "da-DK");
        greatGrandchild.SetValue("count", 78);
        ContentService.Save(greatGrandchild);

        IndexService.Reset();
    }
}