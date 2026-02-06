using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace Umbraco.Cms.Search.Core.Controllers;

[ApiController]
[BackOfficeRoute("search/api/v{version:apiVersion}")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
[MapToApi(Constants.Api.Name)]
public abstract class ApiControllerBase : ControllerBase
{
    // TODO: replace this with an IIndexerResolver service (like the ISearcherResolver)
    internal static bool TryGetIndexer(IServiceProvider serviceProvider, Type type, ILogger logger, [NotNullWhen(true)] out IIndexer? indexer)
    {
        if (serviceProvider.GetService(type) is IIndexer resolvedIndexer)
        {
            indexer = resolvedIndexer;
            return true;
        }

        logger.LogError($"Could not resolve type {{type}} as {nameof(IIndexer)}. Make sure the type is registered in the DI.", type.FullName);
        indexer = null;
        return false;
    }
}
