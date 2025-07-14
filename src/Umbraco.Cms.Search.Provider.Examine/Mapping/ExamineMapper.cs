using Examine;
using Examine.Lucene;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;

namespace Umbraco.Cms.Search.Provider.Examine.Mapping;

public class ExamineMapper : IExamineMapper
{
    public IEnumerable<FacetResult> MapFacets(ISearchResults searchResults, IEnumerable<Facet> queryFacets)
    {
        foreach (var facet in queryFacets)
        {
            switch (facet)
            {
                case IntegerRangeFacet integerRangeFacet:
                {
                    var integerRangeFacetResult = integerRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount($"Umb_{facet.FieldName}_integers", x.Key, searchResults);
                        return new IntegerRangeFacetValue(x.Key, x.Min, x.Max, value);
                    });
                    yield return new FacetResult(facet.FieldName, integerRangeFacetResult);
                    break;
                }
                case IntegerExactFacet integerExactFacet:
                    var examineIntegerFacets = searchResults.GetFacet($"Umb_{integerExactFacet.FieldName}_integers");
                    if (examineIntegerFacets is null)
                    {
                        continue;
                    }

                    var integerExactFacetValues = new List<IntegerExactFacetValue>();
                    foreach (var integerExactFacetValue in examineIntegerFacets)
                    {
                        if (int.TryParse(integerExactFacetValue.Label, out var labelValue) is false)
                        {
                            // Cannot convert the label to int, skipping.
                            continue;
                        }
                        integerExactFacetValues.Add(new IntegerExactFacetValue(labelValue, (int)integerExactFacetValue.Value));
                    }
                    
                    yield return new FacetResult(facet.FieldName, integerExactFacetValues);
                    break;
                case DecimalRangeFacet decimalRangeFacet:
                    var decimalRangeFacetResult = decimalRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount($"Umb_{facet.FieldName}_decimals", x.Key, searchResults);
                        return new DecimalRangeFacetValue(x.Key, x.Min, x.Max, value);
                    });
                    yield return new FacetResult(facet.FieldName, decimalRangeFacetResult);
                    break;
                case DecimalExactFacet decimalExactFacet:
                    var examineDecimalFacets = searchResults.GetFacet($"Umb_{decimalExactFacet.FieldName}_decimals");
                    if (examineDecimalFacets is null)
                    {
                        continue;
                    }
                    
                    var decimalExactFacetValues = new List<DecimalExactFacetValue>();
                    
                    foreach (var decimalExactFacetValue in examineDecimalFacets)
                    {
                        if (decimal.TryParse(decimalExactFacetValue.Label, out var labelValue) is false)
                        {
                            // Cannot convert the label to decimal, skipping.
                            continue;
                        }
                        decimalExactFacetValues.Add(new DecimalExactFacetValue(labelValue, (int)decimalExactFacetValue.Value));
                    }

                    yield return new FacetResult(facet.FieldName, decimalExactFacetValues);
                    break;
                 case DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet:
                    var dateTimeOffsetRangeFacetResult = dateTimeOffsetRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount($"Umb_{facet.FieldName}_datetimeoffsets", x.Key, searchResults);
                        return new DateTimeOffsetRangeFacetValue(x.Key, x.Min, x.Max, value);
                    });
                    yield return new FacetResult(facet.FieldName, dateTimeOffsetRangeFacetResult);
                    break;
                 case DateTimeOffsetExactFacet dateTimeOffsetExactFacet:
                    var examineDatetimeFacets = searchResults.GetFacet($"Umb_{dateTimeOffsetExactFacet.FieldName}_datetimeoffsets");
                    if (examineDatetimeFacets is null)
                    {
                        continue;
                    }
                    
                    var datetimeOffsetExactFacetValues = new List<DateTimeOffsetExactFacetValue>();
                    
                    foreach (var datetimeExactFacetValue in examineDatetimeFacets)
                    {
                        if (long.TryParse(datetimeExactFacetValue.Label, out var ticks) is false)
                        {
                            // Cannot convert the label to ticks (long), skipping.
                            continue;
                        }
                        var offSet = new DateTimeOffset().AddTicks(ticks);
                        datetimeOffsetExactFacetValues.Add(new DateTimeOffsetExactFacetValue(offSet, (int)datetimeExactFacetValue.Value));
                    }
                    yield return new FacetResult(facet.FieldName, datetimeOffsetExactFacetValues);
                    break;
                case KeywordFacet keywordFacet:
                    var examineKeywordFacets = searchResults.GetFacet($"Umb_{keywordFacet.FieldName}_texts");
                    if (examineKeywordFacets is null)
                    {
                        continue;
                    }

                    var keywordFacetValues = examineKeywordFacets.Select(examineKeywordFacet => new KeywordFacetValue(examineKeywordFacet.Label, (int)examineKeywordFacet.Value)).ToList();
                    yield return new FacetResult(facet.FieldName, keywordFacetValues);
                    break;
            }
        }
    }
    
    private int GetFacetCount(string fieldName, string key, ISearchResults results)
    {
        return (int?)results.GetFacet(fieldName)?.Facet(key)?.Value ?? 0;
    }
}