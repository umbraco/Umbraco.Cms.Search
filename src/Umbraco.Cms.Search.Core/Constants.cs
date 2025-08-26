namespace Umbraco.Cms.Search.Core;

public static class Constants
{
    public static class IndexAliases
    {
        private const string IndexPrefix = "Umb_";

        public const string PublishedContent = $"{IndexPrefix}PublishedContent";

        // TODO: "Draft" should be removed from these index constants (values only; we do have functional "Draft" notation elsewhere in the code).

        public const string DraftContent = $"{IndexPrefix}DraftContent";

        public const string DraftMedia = $"{IndexPrefix}DraftMedia";

        public const string DraftMembers = $"{IndexPrefix}DraftMembers";
    }

    public static class FieldNames
    {
        private const string FieldPrefix = "Umb_";

        public const string Id = $"{FieldPrefix}Id";

        public const string ParentId = $"{FieldPrefix}ParentId";

        public const string PathIds = $"{FieldPrefix}PathIds";

        public const string Name = $"{FieldPrefix}Name";

        public const string ContentTypeId = $"{FieldPrefix}ContentTypeId";

        public const string CreateDate = $"{FieldPrefix}CreateDate";

        public const string UpdateDate = $"{FieldPrefix}UpdateDate";

        public const string Level = $"{FieldPrefix}Level";

        public const string SortOrder = $"{FieldPrefix}SortOrder";

        public const string ObjectType = $"{FieldPrefix}ObjectType";

        public const string Tags = $"{FieldPrefix}Tags";
    }
}
