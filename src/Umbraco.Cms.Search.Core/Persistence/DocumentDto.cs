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

    [Column("changeStrategy")]
    public required string ChangeStrategy { get; set; }

    [Column("fields")]
    public required byte[] Fields { get; set; }

    [Column("objectType")]
    public required string ObjectType { get; set; }

    [Column("variations")]
    public required byte[] Variations { get; set; }
}
