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

        IndexDocumentDto dto = ToDto(indexDocument);
        await _scopeAccessor.AmbientScope.Database.InsertAsync(dto);
    }

    public async Task<IndexDocument?> GetAsync(Guid id, bool published)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot get document as there is no ambient scope.");
        }

        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope.Database.SqlContext.Sql()
            .Select<IndexDocumentDto>()
            .From<IndexDocumentDto>()
            .Where<IndexDocumentDto>(x => x.Key == id && x.Published == published);

        IndexDocumentDto? documentDto = await _scopeAccessor.AmbientScope.Database.FirstOrDefaultAsync<IndexDocumentDto>(sql);

        return ToDocument(documentDto);
    }

    public async Task DeleteAsync(Guid[] ids, bool published)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot delete document as there is no ambient scope.");
        }

        List<Guid> idsAsList = [..ids];
        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope.Database.SqlContext.Sql()
            .Delete<IndexDocumentDto>()
            .Where<IndexDocumentDto>(x => idsAsList.Contains(x.Key) && x.Published == published);

        await _scopeAccessor.AmbientScope.Database.ExecuteAsync(sql);
    }

    public async Task DeleteAllAsync()
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot delete all documents as there is no ambient scope.");
        }

        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope.Database.SqlContext.Sql()
            .Delete<IndexDocumentDto>();

        await _scopeAccessor.AmbientScope.Database.ExecuteAsync(sql);
    }

    public async Task RemoveFieldsByCultureAsync(IReadOnlyCollection<string> isoCodes)
    {
        ArgumentNullException.ThrowIfNull(_scopeAccessor.AmbientScope);

        var isoCodeSet = new HashSet<string>(isoCodes, StringComparer.OrdinalIgnoreCase);
        IUmbracoDatabase database = _scopeAccessor.AmbientScope.Database;

        Sql<ISqlContext> sql = database.SqlContext.Sql()
            .Select<IndexDocumentDto>()
            .From<IndexDocumentDto>();

        List<IndexDocumentDto> allDtos = await database.FetchAsync<IndexDocumentDto>(sql);

        var idsToDelete = allDtos
            .Where(dto =>
            {
                IndexField[] fields = MessagePackSerializer.Deserialize<IndexField[]>(dto.Fields, _options) ?? [];
                return fields.Any(f => f.Culture is not null && isoCodeSet.Contains(f.Culture));
            })
            .Select(dto => dto.Id)
            .ToList();

        if (idsToDelete.Count > 0)
        {
            Sql<ISqlContext> deleteSql = database.SqlContext.Sql()
                .Delete<IndexDocumentDto>()
                .Where<IndexDocumentDto>(x => idsToDelete.Contains(x.Id));

            await database.ExecuteAsync(deleteSql);
        }
    }

    private IndexDocumentDto ToDto(IndexDocument indexDocument) =>
        new()
        {
            Key = indexDocument.Key,
            Published = indexDocument.Published,
            Fields = MessagePackSerializer.Serialize(indexDocument.Fields, _options),
        };

    private IndexDocument? ToDocument(IndexDocumentDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new IndexDocument
        {
            Key = dto.Key,
            Fields = MessagePackSerializer.Deserialize<IndexField[]>(dto.Fields, _options) ?? [],
            Published = dto.Published,
        };
    }
}
