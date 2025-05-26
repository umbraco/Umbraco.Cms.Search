namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

public class IndexOptions
{
    public Entry[] Entries { get; set; } = [];
    
    public class Entry
    {
        public required string PropertyName { get; set; } = string.Empty;
        public required string[] Values { get; set; } = [];
        
        public required string Type { get; set; } = string.Empty;
    }
}

