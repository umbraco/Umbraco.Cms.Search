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

    public async Task AddAsync(Document document)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        DocumentDto dto = ToDto(document);
        await _scopeAccessor.AmbientScope.Database.InsertAsync(dto);
    }

    public async Task<Document?> GetAsync(Guid id, string indexAlias)
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

    public async Task<IReadOnlyDictionary<Guid, Document>> GetManyAsync(IEnumerable<Guid> ids, string indexAlias)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot get documents as there is no ambient scope.");
        }

        Guid[] idsArray = ids as Guid[] ?? ids.ToArray();
        if (idsArray.Length == 0)
        {
            return new Dictionary<Guid, Document>();
        }

        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope.Database.SqlContext.Sql()
            .Select<DocumentDto>()
            .From<DocumentDto>()
            .Where<DocumentDto>(x => x.Index == indexAlias)
            .WhereIn<DocumentDto>(x => x.DocumentKey, idsArray);

        List<DocumentDto> documentDtos = await _scopeAccessor.AmbientScope.Database.FetchAsync<DocumentDto>(sql);

        return documentDtos
            .Select(ToDocument)
            .Where(doc => doc is not null)
            .ToDictionary(doc => doc!.DocumentKey, doc => doc!);
    }

    public async Task DeleteAsync(Guid id, string indexAlias)
    {
        if (_scopeAccessor.AmbientScope is null)
        {
            throw new InvalidOperationException("Cannot add document as there is no ambient scope.");
        }

        Sql<ISqlContext> sql = _scopeAccessor.AmbientScope!.Database.SqlContext.Sql()
            .Delete<DocumentDto>()
            .Where<DocumentDto>(x => x.DocumentKey == id && x.Index == indexAlias);

        await _scopeAccessor.AmbientScope?.Database.ExecuteAsync(sql)!;
    }


    private DocumentDto ToDto(Document document) =>
        new()
        {
            DocumentKey = document.DocumentKey,
            Index = document.Index,
            Fields = document.Fields,
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
            Index = dto.Index,
            Fields = dto.Fields,
        };
    }
}
