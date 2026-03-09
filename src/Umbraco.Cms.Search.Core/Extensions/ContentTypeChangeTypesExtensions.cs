using Umbraco.Cms.Core.Services.Changes;

namespace Umbraco.Cms.Search.Core.Extensions;

internal static class ContentTypeChangeTypesExtensions
{
    internal static bool RequiresIndexRebuild(this ContentTypeChangeTypes change)
        => change is ContentTypeChangeTypes.RefreshMain or ContentTypeChangeTypes.Remove;
}
