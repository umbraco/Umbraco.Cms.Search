using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Examine;

namespace Umbraco.Cms.Search.BackOffice.DependencyInjection;

[Disable(typeof(AddExamineComposer))]
public class RemoveExamineComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
    }
}
