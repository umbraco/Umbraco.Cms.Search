namespace Umbraco.Cms.Search.Core;

public static class Constants
{
    public static class IndexAliases
    {
        private const string IndexPrefix = "Umb_"; 

        public const string PublishedContent = $"{IndexPrefix}PublishedContent";

        // TODO: "Draft" should be removed here; it makes no sense for Media and Members, and we don't have "DraftContent" anywhere else - just "Content".
        
        public const string DraftContent = $"{IndexPrefix}DraftContent";

        public const string DraftMedia = $"{IndexPrefix}DraftMedia";

        public const string DraftMembers = $"{IndexPrefix}DraftMembers";
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

        public const string ObjectType = $"{FieldPrefix}ObjectType";

        public const string Tags = $"{FieldPrefix}Tags";
    }
}