namespace Umbraco.Cms.Search.Core;

public static class Constants
{
    public static class IndexAliases
    {
        private const string IndexPrefix = "Umb_"; 

        public const string PublishedContent = $"{IndexPrefix}PublishedContent";

        public const string DraftContent = $"{IndexPrefix}DraftContent";
    }

    public static class FieldNames
    {
        private const string FieldPrefix = "Umb_"; 

        public const string Score = $"{FieldPrefix}Score";

        public const string Id = $"{FieldPrefix}Id";

        public const string ParentId = $"{FieldPrefix}ParentId";

        public const string PathIds = $"{FieldPrefix}PathIds";

        public const string Name = $"{FieldPrefix}Name";

        public const string ContentType = $"{FieldPrefix}ContentType";

        public const string CreateDate = $"{FieldPrefix}CreateDate";

        public const string UpdateDate = $"{FieldPrefix}UpdateDate";

        public const string Level = $"{FieldPrefix}Level";

        public const string SortOrder = $"{FieldPrefix}SortOrder";

        public const string Tags = $"{FieldPrefix}Tags";
    }
}