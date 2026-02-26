using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Search.Core.Models.Configuration;

public record ContentIndexRegistration(string IndexAlias, Type Indexer, Type Searcher, Type ContentChangeStrategy, IEnumerable<UmbracoObjectTypes> ContainedObjectTypes)
    : IndexRegistration(IndexAlias, Indexer, Searcher);
