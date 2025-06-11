using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

namespace Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

public class MediaIndexServiceTests : IndexTestBase
{
    [Test]
    public async Task CanIndexAnyMedia()
    {
        await CreateMediaAsync();

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftMedia);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.TotalItemCount, Is.EqualTo(1));
    }
    
    private async Task CreateMediaAsync()
    {
        var mediaType = new MediaTypeBuilder()
            .WithAlias("theMediaType")
            .AddPropertyGroup()
            .AddPropertyType()
            .WithAlias("altText")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Done()
            .Build();
        await GetRequiredService<IMediaTypeService>().CreateAsync(mediaType, Constants.Security.SuperUserKey);

        GetRequiredService<IMediaService>().Save(
            new MediaBuilder()
                .WithMediaType(mediaType)
                .WithName("The Media")
                .WithPropertyValues(
                    new
                    {
                        altText = "The media alt text"
                    })
                .Build()
        );
        
        Thread.Sleep(3000);
    }
}