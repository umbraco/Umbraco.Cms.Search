using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public class IndexingService : IIndexingService
{
    private readonly IContentIndexingDataCollectionService _contentIndexingDataCollectionService;
    private readonly IContentProtectionProvider _contentProtectionProvider;
    private readonly IContentService _contentService;
    private readonly IDocumentService _documentService;
    private readonly IEventAggregator _eventAggregator;

    public IndexingService(IContentIndexingDataCollectionService contentIndexingDataCollectionService, IContentProtectionProvider contentProtectionProvider, IContentService contentService, IDocumentService documentService, IEventAggregator eventAggregator)
    {
        _contentIndexingDataCollectionService = contentIndexingDataCollectionService;
        _contentProtectionProvider = contentProtectionProvider;
        _contentService = contentService;
        _documentService = documentService;
        _eventAggregator = eventAggregator;
    }

    public async Task<bool> IndexContentAsync(IndexInfo[] indexInfos, IContentBase content, string changeStrategy, bool published, CancellationToken cancellationToken)
    {
        // fetch the doc from service, make sure not to use database here, as it will be deleted
        Document? document = await CalculateDocumentAsync(content, published, cancellationToken);

        if (document is null)
        {
            return false;
        }

        // Delete old entry and persist new fields to database
        await _documentService.DeleteAsync(content.Key, changeStrategy);
        await _documentService.AddAsync(document, changeStrategy);
        UmbracoObjectTypes objectType = content.ObjectType();

        foreach (IndexInfo indexInfo in indexInfos)
        {
            var notification = new IndexingNotification(indexInfo, content.Key, objectType, document.Variations, document.Fields);
            if (await _eventAggregator.PublishCancelableAsync(notification))
            {
                // the indexing operation was cancelled for this index; continue with the rest of the indexes
                continue;
            }

            await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, objectType, document.Variations, notification.Fields, document.Protection);
        }

        return document.Variations.Length != 0;
    }

    public async Task RemoveAsync(IndexInfo[] indexInfos, string changeStrategy, Guid[] documentKeys)
    {
        foreach (IndexInfo indexInfo in indexInfos)
        {
            await indexInfo.Indexer.DeleteAsync(indexInfo.IndexAlias, documentKeys);
        }

        foreach (Guid documentKey in documentKeys)
        {
            await _documentService.DeleteAsync(documentKey, changeStrategy);
        }
    }

    private async Task<Document?> CalculateDocumentAsync(IContentBase content, bool published, CancellationToken cancellationToken)
    {
        IEnumerable<IndexField>? fields = await _contentIndexingDataCollectionService.CollectAsync(content, published, cancellationToken);
        if (fields is null)
        {
            return null;
        }

        Variation[] variations;
        ContentProtection? protection;

        if (published)
        {
            variations = RoutablePublishedVariations(content);
            protection = await GetProtection(content);

            // the fields collection is for all published variants of the content - but it's not certain that a published
            // variant is also routable, because the published routing state can be broken at ancestor level.
            fields = fields.Where(field => variations.Any(v => (field.Culture is null || v.Culture == field.Culture) && (field.Segment is null || v.Segment == field.Segment))).ToArray();
        }
        else
        {
            variations = Variations(content);
            protection = null;
        }

        return new Document
        {
            DocumentKey = content.Key,
            Fields = fields.ToArray(),
            ObjectType = content.ObjectType(),
            Variations = variations,
            Protection = protection,
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
