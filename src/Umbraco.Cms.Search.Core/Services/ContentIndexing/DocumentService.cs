using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Persistence;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public class DocumentService : IDocumentService
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IDocumentRepository _documentRepository;
    private readonly IContentIndexingDataCollectionService _contentIndexingDataCollectionService;
    private readonly IContentProtectionProvider _contentProtectionProvider;
    private readonly IContentService _contentService;

    public DocumentService(ICoreScopeProvider scopeProvider, IDocumentRepository documentRepository, IContentIndexingDataCollectionService contentIndexingDataCollectionService, IContentProtectionProvider contentProtectionProvider, IContentService contentService)
    {
        _scopeProvider = scopeProvider;
        _documentRepository = documentRepository;
        _contentIndexingDataCollectionService = contentIndexingDataCollectionService;
        _contentProtectionProvider = contentProtectionProvider;
        _contentService = contentService;
    }

    public async Task<Document?> CalculateDocument(IContentBase content, bool published, CancellationToken cancellationToken)
    {
        Document? document = await CalculateDocumentAsync(content, published, cancellationToken);

        return document;
    }

    public async Task AddAsync(Document document, string changeStrategy)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.AddAsync(document, changeStrategy);
        scope.Complete();
    }

    public async Task DeleteAsync(Guid id, string changeStrategy)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.DeleteAsync(id, changeStrategy);
        scope.Complete();
    }

    public async Task<IEnumerable<Document>> GetByChangeStrategyAsync(string changeStrategy)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        IEnumerable<Document> documents = await _documentRepository.GetByChangeStrategyAsync(changeStrategy);
        scope.Complete();
        return documents;
    }

    private async Task<Document?> CalculateDocumentAsync(IContentBase content, bool published, CancellationToken cancellationToken)
    {
        // Not in database, calculate fields and persist
        IEnumerable<IndexField>? fields = await _contentIndexingDataCollectionService.CollectAsync(content, published, cancellationToken);
        if (fields is null)
        {
            return null;
        }

        return new Document
        {
            DocumentKey = content.Key,
            Fields = fields.ToArray(),
            ObjectType = content.ObjectType(),
            Variations = published ? RoutablePublishedVariations(content) : Variations(content),
            Protection = published ? await GetProtection(content) : null,
        };
    }

        // NOTE: for the time being, segments are not individually publishable, but it will likely happen at some point,
    //       so this method deals with variations - not cultures.
    private Variation[] RoutablePublishedVariations(IContentBase content)
    {
        if (content.IsPublished() is false)
        {
            return [];
        }

        var variesByCulture = content.VariesByCulture();

        // if the content varies by culture, the indexable cultures are the published
        // cultures - otherwise "null" represents "no culture"
        var cultures = content.PublishedCultures();

        // now iterate all ancestors and make sure all cultures are published all the way up the tree
        foreach (var ancestorId in content.AncestorIds())
        {
            IContent? ancestor = _contentService.GetById(ancestorId);
            if (ancestor is null || ancestor.Published is false)
            {
                // no published ancestor => don't index anything
                cultures = [];
            }
            else if (variesByCulture && ancestor.VariesByCulture())
            {
                // both the content and the ancestor are culture variant => only index the published cultures they have in common
                cultures = cultures.Intersect(ancestor.PublishedCultures).ToArray();
            }

            // if we've already run out of cultures to index, there is no reason to iterate the ancestors any further
            if (cultures.Any() == false)
            {
                break;
            }
        }

        // for now, segments are not individually routable, so we only need to deal with cultures and append all known segments
        if (content.Properties.Any(p => p.PropertyType.VariesBySegment()) is false)
        {
            // no segment variant properties - just return the found cultures
            return cultures.Select(c => new Variation(c, null)).ToArray();
        }

        // segments are not "known" - we can only determine segment variation by looking at the property values
        return cultures.SelectMany(culture => content
                .Properties
                .SelectMany(property => property.Values.Where(value => value.Culture.InvariantEquals(culture)))
                .DistinctBy(value => value.Segment).Select(value => value.Segment)
                .Select(segment => new Variation(culture, segment)))
            .ToArray();
    }

    private async Task<ContentProtection?> GetProtection(IContentBase content) => await _contentProtectionProvider.GetContentProtectionAsync(content);

    private Variation[] Variations(IContentBase content)
    {
        string?[] cultures = content.AvailableCultures();

        return content.ContentType.VariesBySegment()
            ? cultures
                .SelectMany(culture => content
                    .Properties
                    .SelectMany(property => property.Values.Where(value => value.Culture.InvariantEquals(culture)))
                    .DistinctBy(value => value.Segment).Select(value => value.Segment)
                    .Select(segment => new Variation(culture, segment)))
                .ToArray()
            : cultures
                .Select(culture => new Variation(culture, null))
                .ToArray();
    }
}
