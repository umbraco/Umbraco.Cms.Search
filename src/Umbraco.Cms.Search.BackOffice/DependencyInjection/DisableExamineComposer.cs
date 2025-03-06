using Examine;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Search.BackOffice.Examine;

namespace Umbraco.Cms.Search.BackOffice.DependencyInjection;

public class DisableExamineComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddSingleton<IExamineManager, EmptyExamineManager>();
}