using MessagePack;
using MessagePack.Resolvers;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Persistence;

public class IndexDocumentRepository : IIndexDocumentRepository
{
    private readonly IScopeAccessor _scopeAccessor;
    private readonly MessagePackSerializerOptions _options;

    public IndexDocumentRepository(IScopeAccessor scopeAccessor)
    {
        _scopeAccessor = scopeAccessor;

        MessagePackSerializerOptions defaultOptions = ContractlessStandardResolver.Options;
        IFormatterResolver resolver = CompositeResolver.Create(defaultOptions.Resolver);
        _options = defaultOptions
            .WithResolver(resolver)
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithSecurity(MessagePackSecurity.UntrustedData);
    }

    public async Task AddAsync(IndexDocument indexDocument)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        IUmbracoDatabase database = _scopeAccessor.AmbientScope.Database;

        var parentDto = new IndexDocumentDto
        {
            Key = indexDocument.Key,
            Published = indexDocument.Published,
        };
        await database.InsertAsync(parentDto);

        IGrouping<string, IndexField>[] groups = indexDocument.Fields
            .GroupBy(f => f.Culture ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (IGrouping<string, IndexField> group in groups)
        {
            IndexField[] fieldsForSerialization = group
                .Select(f => f with { Culture = null })
                .ToArray();

            var childDto = new IndexDocumentFieldsDto
            {
                IndexDocumentId = parentDto.Id,
                Culture = group.Key,
                Fields = MessagePackSerializer.Serialize(fieldsForSerialization, _options),
            };
            await database.InsertAsync(childDto);
        }
    }

    public async Task<IndexDocument?> GetAsync(Guid id, bool published)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot get document as there is no ambient scope.");
        }

        IUmbracoDatabase database = _scopeAccessor.AmbientScope.Database;

        Sql<ISqlContext> parentSql = database.SqlContext.Sql()
            .Select<IndexDocumentDto>()
            .From<IndexDocumentDto>()
            .Where<IndexDocumentDto>(x => x.Key == id && x.Published == published);

        IndexDocumentDto? parentDto = await database.FirstOrDefaultAsync<IndexDocumentDto>(parentSql);
        if (parentDto is null)
        {
            return null;
        }

        Sql<ISqlContext> childSql = database.SqlContext.Sql()
            .Select<IndexDocumentFieldsDto>()
            .From<IndexDocumentFieldsDto>()
            .Where<IndexDocumentFieldsDto>(x => x.IndexDocumentId == parentDto.Id);

        List<IndexDocumentFieldsDto> childDtos = await database.FetchAsync<IndexDocumentFieldsDto>(childSql);

        if (childDtos.Count == 0)
        {
            return null;
        }

        var allFields = new List<IndexField>();
        foreach (IndexDocumentFieldsDto child in childDtos)
        {
            string? culture = child.Culture == string.Empty ? null : child.Culture;
            IndexField[] fields = MessagePackSerializer.Deserialize<IndexField[]>(child.Fields, _options);
            allFields.AddRange(fields.Select(f => f with { Culture = culture }));
        }

        return new IndexDocument
        {
            Key = parentDto.Key,
            Published = parentDto.Published,
            Fields = allFields.ToArray(),
        };
    }

    public async Task DeleteAsync(Guid[] ids, bool published)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot delete document as there is no ambient scope.");
        }

        IUmbracoDatabase database = _scopeAccessor.AmbientScope.Database;
        List<Guid> idsAsList = [..ids];

        Sql<ISqlContext> sql = database.SqlContext.Sql()
            .Delete<IndexDocumentDto>()
            .Where<IndexDocumentDto>(x => idsAsList.Contains(x.Key) && x.Published == published);

        await database.ExecuteAsync(sql);
    }

    public async Task DeleteAllAsync()
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot delete all documents as there is no ambient scope.");
        }

        IUmbracoDatabase database = _scopeAccessor.AmbientScope.Database;

        Sql<ISqlContext> sql = database.SqlContext.Sql()
            .Delete<IndexDocumentDto>();

        await database.ExecuteAsync(sql);
    }

    public async Task DeleteCulturesAsync(IReadOnlyCollection<string> isoCodes)
    {
        ArgumentNullException.ThrowIfNull(_scopeAccessor.AmbientScope);

        IUmbracoDatabase database = _scopeAccessor.AmbientScope.Database;
        var isoCodesList = isoCodes.ToList();

        Sql<ISqlContext> deleteSql = database.SqlContext.Sql()
            .Delete<IndexDocumentFieldsDto>()
            .Where<IndexDocumentFieldsDto>(x => isoCodesList.Contains(x.Culture));

        await database.ExecuteAsync(deleteSql);

        // Clean up orphan parents that no longer have any child rows
        Sql<ISqlContext> orphanSql = database.SqlContext.Sql()
            .Select<IndexDocumentDto>()
            .From<IndexDocumentDto>()
            .LeftJoin<IndexDocumentFieldsDto>()
            .On<IndexDocumentDto, IndexDocumentFieldsDto>(
                (parent, child) => parent.Id == child.IndexDocumentId)
            .Where<IndexDocumentFieldsDto>(x => x.Id == null!);

        List<IndexDocumentDto> orphans = await database.FetchAsync<IndexDocumentDto>(orphanSql);

        if (orphans.Count > 0)
        {
            List<int> orphanIds = orphans.Select(x => x.Id).ToList();
            Sql<ISqlContext> deleteOrphansSql = database.SqlContext.Sql()
                .Delete<IndexDocumentDto>()
                .Where<IndexDocumentDto>(x => orphanIds.Contains(x.Id));
            await database.ExecuteAsync(deleteOrphansSql);
        }
    }
}
