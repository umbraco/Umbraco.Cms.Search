using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Persistence;

public class DocumentRepository : IDocumentRepository
{
    private readonly IScopeAccessor _scopeAccessor;

    public DocumentRepository(IScopeAccessor scopeAccessor) => _scopeAccessor = scopeAccessor;

    public async Task Add(Document document)
    {
        DocumentDto dto = ToDto(document);

        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        await _scopeAccessor.AmbientScope.Database.InsertAsync(dto);
    }

    public async Task<Document> Get(Guid id, string indexAlias)
    {
        Sql<ISqlContext>? sql = _scopeAccessor.AmbientScope?.Database.SqlContext.Sql()
            .Select<DocumentDto>()
            .From<DocumentDto>()
            .Where<DocumentDto>(x => x.DocumentKey == id && x.Index == indexAlias);

        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        DocumentDto? documentDto = await _scopeAccessor.AmbientScope.Database.FirstOrDefaultAsync<DocumentDto>(sql);

        return ToDocument(documentDto);
    }

    public async Task Remove(Guid id)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope!.Database.SqlContext.Sql()
            .Delete<DocumentDto>()
            .Where<DocumentDto>(x => x.DocumentKey == id);

        await _scopeAccessor.AmbientScope?.Database.ExecuteAsync(sql)!;

    }


    private DocumentDto ToDto(Document document) =>
        new()
        {
            DocumentKey = document.DocumentKey,
            Index = document.Index,
            Fields = document.Fields,
        };

    private Document ToDocument(DocumentDto dto)
    {
        return new()
        {
            DocumentKey = dto.DocumentKey,
            Index = dto.Index,
            Fields = dto.Fields,
        };
    }
}
