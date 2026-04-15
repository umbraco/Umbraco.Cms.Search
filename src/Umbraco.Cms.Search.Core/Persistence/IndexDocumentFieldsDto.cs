using System.Data;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Cms.Search.Core.Persistence;

[TableName(Constants.Persistence.IndexDocumentFieldsTableName)]
[PrimaryKey("id")]
[ExplicitColumns]
public class IndexDocumentFieldsDto
{
    [Column("id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("indexDocumentId")]
    [ForeignKey(typeof(IndexDocumentDto), OnDelete = Rule.Cascade)]
    public required int IndexDocumentId { get; set; }

    [Column("culture")]
    public required string Culture { get; set; }

    [Column("fields")]
    public required byte[] Fields { get; set; }
}
