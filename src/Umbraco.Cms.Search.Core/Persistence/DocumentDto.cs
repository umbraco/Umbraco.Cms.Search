using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Cms.Search.Core.Persistence;

[TableName(Constants.Persistence.DocumentTableName)]
[PrimaryKey("id")]
[ExplicitColumns]
public class DocumentDto
{
    [Column("id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("documentKey")]
    public required Guid DocumentKey { get; set; }

    [Column("index")]
    public required string Index { get; set; }

    [Column("fields")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public required string Fields { get; set; }
}
