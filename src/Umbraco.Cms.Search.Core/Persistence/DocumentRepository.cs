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

    public async Task AddAsync(Document document)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        DocumentDto dto = ToDto(document);
        await _scopeAccessor.AmbientScope.Database.InsertAsync(dto);
    }

    public async Task<Document?> GetAsync(Guid id, bool published)
    {
        Sql<ISqlContext>? sql = _scopeAccessor.AmbientScope?.Database.SqlContext.Sql()
            .Select<DocumentDto>()
            .From<DocumentDto>()
            .Where<DocumentDto>(x => x.DocumentKey == id && x.Published == published);

        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        DocumentDto? documentDto = await _scopeAccessor.AmbientScope.Database.FirstOrDefaultAsync<DocumentDto>(sql);

        return ToDocument(documentDto);
    }

    public async Task DeleteAsync(Guid[] ids, bool published)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot delete document as there is no ambient scope.");
        }

        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope.Database.SqlContext.Sql()
            .Delete<DocumentDto>()
            .Where<DocumentDto>(x => ids.Contains(x.DocumentKey) && x.Published == published);

        await _scopeAccessor.AmbientScope.Database.ExecuteAsync(sql);
    }


    private DocumentDto ToDto(Document document) =>
        new()
        {
            DocumentKey = document.DocumentKey,
            Published = document.Published,
            Fields = MessagePackSerializer.Serialize(document.Fields, _options),
        };

    private Document? ToDocument(DocumentDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new Document
        {
            DocumentKey = dto.DocumentKey,
            Fields = MessagePackSerializer.Deserialize<IndexField[]>(dto.Fields, _options) ?? [],
            Published = dto.Published,
        };
    }
}
