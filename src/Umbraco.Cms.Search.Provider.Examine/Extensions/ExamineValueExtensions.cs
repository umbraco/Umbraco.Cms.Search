using Examine.Search;

namespace Umbraco.Cms.Search.Provider.Examine.Extensions;

// TODO: remove this with the next version of Examine
internal static class ExamineValueExtensions
{
    public static IExamineValue WithBoost(this IExamineValue value, float _) => value;
}
