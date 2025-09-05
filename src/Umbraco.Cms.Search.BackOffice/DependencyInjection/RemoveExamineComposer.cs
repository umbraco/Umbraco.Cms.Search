using Examine;
using Examine.Lucene.Directories;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.BackOffice.DependencyInjection;

[Disable(typeof(AddExamineComposer))]
public sealed class RemoveExamineComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        IServiceCollection services = builder.Services;

        // must re-add essential services and configuration for Examine to behave like it normally does

        // - the actual ExamineManager (the default is a no-op)
        services.AddUnique<IExamineManager, ExamineManager>();

        // - the Umbraco implementations for app root and lock factory
        services.AddSingleton<IApplicationRoot, UmbracoApplicationRoot>();
        services.AddSingleton<ILockFactory, UmbracoLockFactory>();
    }
}
