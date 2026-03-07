namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public class OriginProvider : IOriginProvider
{
    private static string _origin = Guid.NewGuid().ToString("N");

    public string GetCurrent() => _origin;
}
