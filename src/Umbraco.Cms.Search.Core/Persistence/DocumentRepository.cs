using MessagePack;
using MessagePack.Resolvers;
using NPoco;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Persistence;

public class DocumentRepository : IDocumentRepository
{
    private readonly IScopeAccessor _scopeAccessor;
    private readonly MessagePackSerializerOptions _options;

    public DocumentRepository(IScopeAccessor scopeAccessor)
    {
        _scopeAccessor = scopeAccessor;

        MessagePackSerializerOptions defaultOptions = ContractlessStandardResolver.Options;
        IFormatterResolver resolver = CompositeResolver.Create(defaultOptions.Resolver);
        _options = defaultOptions
            .WithResolver(resolver)
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithSecurity(MessagePackSecurity.UntrustedData);
    }

    public async Task AddAsync(Document document, string changeStrategy)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        DocumentDto dto = ToDto(document, changeStrategy);
        await _scopeAccessor.AmbientScope.Database.InsertAsync(dto);
    }

    public async Task<Document?> GetAsync(Guid id, string changeStrategy)
    {
        Sql<ISqlContext>? sql = _scopeAccessor.AmbientScope?.Database.SqlContext.Sql()
            .Select<DocumentDto>()
            .From<DocumentDto>()
            .Where<DocumentDto>(x => x.DocumentKey == id && x.ChangeStrategy == changeStrategy);

        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        DocumentDto? documentDto = await _scopeAccessor.AmbientScope.Database.FirstOrDefaultAsync<DocumentDto>(sql);

        return ToDocument(documentDto);
    }

    public async Task DeleteAsync(Guid id, string changeStrategy)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope!.Database.SqlContext.Sql()
            .Delete<DocumentDto>()
            .Where<DocumentDto>(x => x.DocumentKey == id && x.ChangeStrategy == changeStrategy);

        await _scopeAccessor.AmbientScope?.Database.ExecuteAsync(sql)!;
    }

    public async Task<IEnumerable<Document>> GetByChangeStrategyAsync(string changeStrategy)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot get documents as there is no ambient scope.");
        }

        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope.Database.SqlContext.Sql()
            .Select<DocumentDto>()
            .From<DocumentDto>()
            .Where<DocumentDto>(x => x.ChangeStrategy == changeStrategy);

        List<DocumentDto> documentDtos = await _scopeAccessor.AmbientScope.Database.FetchAsync<DocumentDto>(sql);

        return documentDtos.Select(ToDocument).WhereNotNull();
    }


    private DocumentDto ToDto(Document document, string changeStrategy) =>
        new()
        {
            DocumentKey = document.DocumentKey,
            ChangeStrategy = changeStrategy,
            Fields = MessagePackSerializer.Serialize(document.Fields, _options),
            ObjectType = document.ObjectType.ToString(),
            Variations = MessagePackSerializer.Serialize(document.Variations, _options),
        };

    private Document? ToDocument(DocumentDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new()
        {
            DocumentKey = dto.DocumentKey,
            Fields = MessagePackSerializer.Deserialize<IndexField[]>(dto.Fields, _options) ?? [],
            ObjectType = Enum.Parse<UmbracoObjectTypes>(dto.ObjectType),
            Variations = MessagePackSerializer.Deserialize<Variation[]>(dto.Variations, _options) ?? [],
        };
    }
}
