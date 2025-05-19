namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

public class FacetOptions
{
    public FacetEntry[] Facets { get; set; } = [];
    
    public class FacetEntry
    {
        public required string PropertyName { get; set; } = string.Empty;
        public required string[] Values { get; set; } = [];
        
        public required string FacetType { get; set; } = string.Empty;
    }
}

