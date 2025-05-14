using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Search.Core.Models.Configuration;

public record IndexRegistration(string IndexAlias, IEnumerable<UmbracoObjectTypes> ContainedObjectTypes, Type Indexer, Type Searcher, Type ContentChangeHandler);
