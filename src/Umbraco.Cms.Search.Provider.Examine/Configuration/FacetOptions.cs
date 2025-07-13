namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

public class FacetOptions
{
    public Field[] Fields { get; set; } = [];
    
    public class Field
    {
        public required string PropertyName { get; set; } = string.Empty;
        public required string[] Values { get; set; } = [];
        
        public required string Type { get; set; } = string.Empty;
    }
}

