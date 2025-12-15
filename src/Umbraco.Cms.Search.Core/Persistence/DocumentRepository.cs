using System.Text.Json;
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

    public DocumentRepository(IScopeAccessor scopeAccessor) => _scopeAccessor = scopeAccessor;

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

    public async Task<IReadOnlyDictionary<Guid, Document>> GetManyAsync(IEnumerable<Guid> ids, string changeStrategy)
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
            .Where<DocumentDto>(x => x.ChangeStrategy == changeStrategy)
            .WhereIn<DocumentDto>(x => x.DocumentKey, idsArray);

        List<DocumentDto> documentDtos = await _scopeAccessor.AmbientScope.Database.FetchAsync<DocumentDto>(sql);

        return documentDtos
            .Select(ToDocument)
            .Where(doc => doc is not null)
            .ToDictionary(doc => doc!.DocumentKey, doc => doc!);
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
            Fields = JsonSerializer.Serialize(document.Fields),
            ObjectType = document.ObjectType.ToString(),
            Variations = JsonSerializer.Serialize(document.Variations),
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
            Fields = JsonSerializer.Deserialize<IndexField[]>(dto.Fields) ?? [],
            ObjectType = Enum.Parse<UmbracoObjectTypes>(dto.ObjectType),
            Variations = JsonSerializer.Deserialize<Variation[]>(dto.Variations) ?? [],
        };
    }
}
