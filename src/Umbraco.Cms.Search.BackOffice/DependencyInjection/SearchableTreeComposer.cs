using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.BackOffice.Trees;

namespace Site.DependencyInjection;

public class SearchableTreeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.SearchableTrees()
            .Exclude<ContentTreeController>()
            .Exclude<MediaTreeController>()
            .Exclude<MemberTreeController>();
    }
}