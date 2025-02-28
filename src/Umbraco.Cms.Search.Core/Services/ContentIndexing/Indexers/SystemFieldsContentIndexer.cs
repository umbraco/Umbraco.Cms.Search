using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing.Indexers;

internal sealed class SystemFieldsContentIndexer : ISystemFieldsContentIndexer
{
    private readonly IIdKeyMap _idKeyMap;
    private readonly IDateTimeOffsetConverter _dateTimeOffsetConverter;
    private readonly ILogger<SystemFieldsContentIndexer> _logger;

    public SystemFieldsContentIndexer(
        IIdKeyMap idKeyMap,
        IDateTimeOffsetConverter dateTimeOffsetConverter,
        ILogger<SystemFieldsContentIndexer> logger)
    {
        _idKeyMap = idKeyMap;
        _dateTimeOffsetConverter = dateTimeOffsetConverter;
        _logger = logger;
    }

    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IContent content, string?[] cultures, bool published, CancellationToken cancellationToken)
        => Task.FromResult(CollectSystemFields(content, cultures));

    private IEnumerable<IndexField> CollectSystemFields(IContent content, string?[] cultures)
    {
        var parentKey = Guid.Empty;
        if (content.ParentId > 0)
        {
            var parentKeyAttempt = _idKeyMap.GetKeyForId(content.ParentId, UmbracoObjectTypes.Document);
            if (parentKeyAttempt.Success is false)
            {
                _logger.LogWarning(
                    "Could not resolve parent key for parent ID {parentId} - aborting indexing of content item {contentKey}.",
                    content.ParentId,
                    content.Key);
                return [];
            }

            parentKey = parentKeyAttempt.Result;
        }  
        
        var ancestorIds = content.GetAncestorIds() ?? [];
        var pathKeys = new List<Guid>();
        foreach (var ancestorId in ancestorIds)
        {
            var attempt = _idKeyMap.GetKeyForId(ancestorId, UmbracoObjectTypes.Document);
            if (attempt.Success is false)
            {
                _logger.LogWarning(
                    "Could not resolve ancestor key for ancestor ID {ancestorId} - aborting indexing of content item {contentKey}.",
                    ancestorId,
                    content.Key);
                return [];
            }

            pathKeys.Add(attempt.Result);
        }
        pathKeys.Add(content.Key);
        
        // TODO: add tags here
        var fields = new List<IndexField>
        {
            new(IndexConstants.FieldNames.Id, new() { Keywords = [content.Key.ToString("D")] }, null, null),
            new(IndexConstants.FieldNames.ParentId, new() { Keywords = [parentKey.ToString("D")] }, null, null),
            new(IndexConstants.FieldNames.PathIds, new() { Keywords = pathKeys.Select(key => key.ToString("D")).ToArray() }, null, null),
            new(IndexConstants.FieldNames.ContentType, new() { Keywords = [content.ContentType.Alias] }, null, null),
            new(IndexConstants.FieldNames.CreateDate, new() { DateTimeOffsets = [_dateTimeOffsetConverter.ToDateTimeOffset(content.CreateDate)] }, null, null),
            new(IndexConstants.FieldNames.UpdateDate, new() { DateTimeOffsets = [_dateTimeOffsetConverter.ToDateTimeOffset(content.UpdateDate)] }, null, null),
            new(IndexConstants.FieldNames.Level, new() { Integers = [content.Level] }, null, null),
            new(IndexConstants.FieldNames.SortOrder, new() { Integers = [content.SortOrder] }, null, null),
        };

        fields.AddRange(cultures.Select(culture =>
            {
                var name = content.GetCultureName(culture);
                if (string.IsNullOrEmpty(name) is false)
                {
                    return new IndexField(IndexConstants.FieldNames.Name, new() { Texts = [name] }, culture, null);
                }

                _logger.LogWarning(
                    "Could not obtain a name for indexing for content item {contentKey} in culture {culture}.",
                    content.Key,
                    culture ?? "[invariant]");
                return null;
            }).WhereNotNull()
        );

        return fields;
    }
}