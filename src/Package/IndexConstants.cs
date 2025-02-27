namespace Package;

public static class IndexConstants
{
    public static class Aliases
    {
        private const string FieldPrefix = "Umb_"; 
        
        public const string Id = $"{FieldPrefix}Id";

        public const string ParentId = $"{FieldPrefix}ParentId";

        public const string AncestorIds = $"{FieldPrefix}AncestorIds";

        public const string Name = $"{FieldPrefix}Name";

        public const string ContentType = $"{FieldPrefix}ContentType";

        public const string CreateDate = $"{FieldPrefix}CreateDate";

        public const string UpdateDate = $"{FieldPrefix}UpdateDate";
    }
}