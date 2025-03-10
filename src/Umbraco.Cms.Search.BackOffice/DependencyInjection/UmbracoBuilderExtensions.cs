using Examine;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Search.BackOffice.Examine;
using Umbraco.Cms.Web.BackOffice.Trees;

namespace Umbraco.Cms.Search.BackOffice.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddBackOfficeSearch(this IUmbracoBuilder builder)
    {
        // disable all Examine indexes
        builder.Services.AddSingleton<IExamineManager, EmptyExamineManager>();

        // remove searchable trees depending on Examine
        // - the DraftContentSearchableTree implementations will perform the actual search against the search abstraction
        builder.SearchableTrees()
            .Exclude<ContentTreeController>()
            .Exclude<MediaTreeController>()
            .Exclude<MemberTreeController>();

        return builder;
    }
}
