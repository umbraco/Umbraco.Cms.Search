using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing.Indexers;

internal sealed class SystemFieldsContentIndexer : ISystemFieldsContentIndexer
{
    private readonly IIdKeyMap _idKeyMap;
    private readonly ITagService _tagService;
    private readonly IDateTimeOffsetConverter _dateTimeOffsetConverter;
    private readonly ILogger<SystemFieldsContentIndexer> _logger;

    public SystemFieldsContentIndexer(
        IIdKeyMap idKeyMap,
        ITagService tagService,
        IDateTimeOffsetConverter dateTimeOffsetConverter,
        ILogger<SystemFieldsContentIndexer> logger)
    {
        _idKeyMap = idKeyMap;
        _tagService = tagService;
        _dateTimeOffsetConverter = dateTimeOffsetConverter;
        _logger = logger;
    }

    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IContentBase content, string?[] cultures, bool published, CancellationToken cancellationToken)
        => Task.FromResult(CollectSystemFields(content, cultures));

    private IEnumerable<IndexField> CollectSystemFields(IContentBase content, string?[] cultures)
    {
        if (TryGetParentKey(content, out var parentKey) is false)
        {
            return [];
        }

        if (TryGetPathKeys(content, out var pathKeys) is false)
        {
            return [];
        }
        
        var fields = new List<IndexField>
        {
            new(Constants.FieldNames.Id, new() { Keywords = [content.Key.ToString("D")] }, null, null),
            new(Constants.FieldNames.ParentId, new() { Keywords = [parentKey.Value.ToString("D")] }, null, null),
            new(Constants.FieldNames.PathIds, new() { Keywords = pathKeys.Select(key => key.ToString("D")).ToArray() }, null, null),
            new(Constants.FieldNames.ContentType, new() { Keywords = [content.ContentType.Alias] }, null, null),
            new(Constants.FieldNames.CreateDate, new() { DateTimeOffsets = [_dateTimeOffsetConverter.ToDateTimeOffset(content.CreateDate)] }, null, null),
            new(Constants.FieldNames.UpdateDate, new() { DateTimeOffsets = [_dateTimeOffsetConverter.ToDateTimeOffset(content.UpdateDate)] }, null, null),
            new(Constants.FieldNames.Level, new() { Integers = [content.Level] }, null, null),
            new(Constants.FieldNames.SortOrder, new() { Integers = [content.SortOrder] }, null, null),
        };

        fields.AddRange(GetCultureTagFields(content, cultures));
        fields.AddRange(GetCultureNameFields(content, cultures));

        return fields;
    }

    private bool TryGetParentKey(IContentBase content, [NotNullWhen(true)] out Guid? parentKey)
    {
        if (content.ParentId <= 0)
        {
            parentKey = Guid.Empty;
            return true;
        }

        var parentKeyAttempt = _idKeyMap.GetKeyForId(content.ParentId, content.GetObjectType());
        if (parentKeyAttempt.Success is false)
        {
            _logger.LogWarning(
                "Could not resolve parent key for parent ID {parentId} - aborting indexing of content item {contentKey}.",
                content.ParentId,
                content.Key);
            parentKey = null;
            return false;
        }

        parentKey = parentKeyAttempt.Result;
        return true;
    }
    
    private bool TryGetPathKeys(IContentBase content, out IList<Guid> pathKeys)
    {
        var ancestorIds = content.GetAncestorIds();
        pathKeys = new List<Guid>();
        foreach (var ancestorId in ancestorIds)
        {
            var attempt = _idKeyMap.GetKeyForId(ancestorId, content.GetObjectType());
            if (attempt.Success is false)
            {
                _logger.LogWarning(
                    "Could not resolve ancestor key for ancestor ID {ancestorId} - aborting indexing of content item {contentKey}.",
                    ancestorId,
                    content.Key);
                return false;
            }

            pathKeys.Add(attempt.Result);
        }

        pathKeys.Add(content.Key);
        return true;
    }

    private IEnumerable<IndexField> GetCultureTagFields(IContentBase content, string?[] cultures)
    {
        foreach (var culture in cultures)
        {
            var tags = _tagService
                .GetTagsForEntity(content.Key, group: null, culture: culture)
                .Select(tag => tag.Text)
                .ToArray();
            if (tags.Length == 0)
            {
                continue;
            }

            yield return new IndexField(Constants.FieldNames.Tags, new() { Keywords = tags }, culture, null);
        }
    }

    private IEnumerable<IndexField> GetCultureNameFields(IContentBase content, string?[] cultures)
    {
        foreach (var culture in cultures)
        {
            var name = content.GetCultureName(culture);
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning(
                    "Could not obtain a name for indexing for content item {contentKey} in culture {culture}.",
                    content.Key,
                    culture ?? "[invariant]");
                continue;
                
            }

            yield return new IndexField(Constants.FieldNames.Name, new() { Texts = [name] }, culture, null);
        }
    }
}