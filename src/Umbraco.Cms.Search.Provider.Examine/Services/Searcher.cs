using System.Reflection;
using Examine;
using Examine.Lucene;
using Examine.Search;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Exceptions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Extensions;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using Umbraco.Cms.Search.Provider.Examine.Models.Searching.Filtering;
using Umbraco.Extensions;
using FacetResult = Umbraco.Cms.Search.Core.Models.Searching.Faceting.FacetResult;
using SearchResult = Umbraco.Cms.Search.Core.Models.Searching.SearchResult;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

internal sealed class Searcher : IExamineSearcher
{
    private readonly IExamineManager _examineManager;
    private readonly SearcherOptions _searcherOptions;

    public Searcher(IExamineManager examineManager, IOptions<SearcherOptions> searcherOptions)
    {
        _examineManager = examineManager;
        _searcherOptions = searcherOptions.Value;
    }

    public Task<SearchResult> SearchAsync(
        string indexAlias,
        string? query,
        IEnumerable<Filter>? filters,
        IEnumerable<Facet>? facets,
        IEnumerable<Sorter>? sorters,
        string? culture,
        string? segment,
        AccessContext? accessContext,
        int skip,
        int take)
    {
        // Special case if no parameters are provided, return an empty list.
        if (query is null && filters is null && facets is null && sorters is null && culture is null && segment is null && accessContext is null)
        {
            return Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }

        if (_examineManager.TryGetIndex(indexAlias, out IIndex? index) is false)
        {
            return Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }

        SearchResult? searchResult;

        if (_searcherOptions.ExpandFacetValues)
        {
            Filter[]? filtersAsArray = filters as Filter[] ?? filters?.ToArray();
            Facet[]? facetsAsArray = facets as Facet[] ?? facets?.ToArray();
            Facet[] filterFacets = filtersAsArray is not null && facetsAsArray is not null
                ? facetsAsArray.Where(facet => filtersAsArray.Any(filter => filter.FieldName == facet.FieldName)).ToArray()
                : [];

            var facetFilterResults = new List<FacetResult>();
            foreach (Facet facet in filterFacets)
            {
                IBooleanOperation facetSearchQuery = CreateBaseQuery();
                Filter[] effectiveFilters = filtersAsArray!.Where(filter => filter.FieldName != facet.FieldName).ToArray();
                facetFilterResults.AddRange(Search(facetSearchQuery, effectiveFilters, [facet], null, 0, 0).Facets);
            }

            SearchResult documentsSearchResult = Search(CreateBaseQuery(), filtersAsArray, facetsAsArray?.Except(filterFacets), sorters, skip, take);
            searchResult = documentsSearchResult with
            {
                Facets = facetFilterResults.Union(documentsSearchResult.Facets)
            };
        }
        else
        {
            searchResult = Search(CreateBaseQuery(), filters, facets, sorters, skip, take);
        }

        return Task.FromResult(searchResult);

        IBooleanOperation CreateBaseQuery()
        {
            IBooleanOperation searchQuery = index.Searcher
                .CreateQuery()
                .Field(Constants.SystemFields.Culture, culture ?? Constants.Variance.Invariant)
                .And()
                .Field(Constants.SystemFields.Segment, segment ?? Constants.Variance.Invariant);

            if (query is not null)
            {
                // This looks a little hacky, but managed query alone cannot handle some multicultural words, as the analyser is english based.
                // For example any japanese letters will not get a hit in the managed query.
                // We luckily can also query on the aggregated text field, to assure that these cases also gets included.
                searchQuery.And().Group(nestedQuery =>
                {
                    var transformedQuery = query.TransformDashes();
                    INestedBooleanOperation fieldQuery = nestedQuery.Field(Constants.SystemFields.AggregatedTextsR1, transformedQuery.Boost(_searcherOptions.BoostFactorTextR1));
                    fieldQuery.Or().Field(Constants.SystemFields.AggregatedTextsR2, transformedQuery.Boost(_searcherOptions.BoostFactorTextR2));
                    fieldQuery.Or().Field(Constants.SystemFields.AggregatedTextsR3, transformedQuery.Boost(_searcherOptions.BoostFactorTextR3));
                    fieldQuery.Or().ManagedQuery(transformedQuery);
                    fieldQuery.Or().Field(Constants.SystemFields.AggregatedTexts, transformedQuery.Escape());

                    return fieldQuery;
                });
            }

            AddProtection(searchQuery, accessContext);

            return searchQuery;
        }

    }

    private SearchResult Search(
        IBooleanOperation searchQuery,
        IEnumerable<Filter>? filters,
        IEnumerable<Facet>? facets,
        IEnumerable<Sorter>? sorters,
        int skip,
        int take)
    {
        // Examine will overwrite subsequent facets of the same field, so make sure there aren't any duplicates.
        Facet[] deduplicateFacets = DeduplicateFacets(facets);

        Sorter[]? sortersAsArray = sorters as Sorter[] ?? sorters?.ToArray();

        // Add facets and filters
        AddFilters(searchQuery, filters);
        AddFacets(searchQuery, deduplicateFacets);
        AddSorters(searchQuery, sortersAsArray);

        ISearchResults results;
        try
        {
            results = searchQuery.Execute(new QueryOptions(skip, take));
        }
        catch (ArgumentException e)
        {
            if (e.Message.Contains("field \"$facets\" was not indexed with SortedSetDocValues"))
            {
                throw new ConfigurationException("Tried querying a facet that did not exist, please configure your facets with FieldOptions.", e);
            }

            if (e.Message.Contains("dimension \"") && e.Message.Contains("\" was not indexed"))
            {
                throw new ConfigurationException("Tried querying a facet that did not exist, but the field did exist, please configure your facets with FieldOptions and set Facetable to true.", e);
            }

            throw;
        }

        IEnumerable<ISearchResult> searchResults;

        ScoreSorter? scoreSorter = sortersAsArray?.OfType<ScoreSorter>().FirstOrDefault();
        if (scoreSorter is not null)
        {
            searchResults = results.OrderBy(x => x.Score, scoreSorter.Direction);
        }
        else
        {
            searchResults = results;
        }

        FacetResult[] facetResults = facets is null ? [] : MapFacets(results, deduplicateFacets).ToArray();
        Document[] searchResultDocuments = take > 0 ? searchResults.Select(MapToDocument).WhereNotNull().ToArray() : [];

        return new SearchResult(results.TotalItemCount, searchResultDocuments, facetResults);
    }

    private void AddProtection(IBooleanOperation searchQuery, AccessContext? accessContext)
    {
        if (accessContext is null)
        {
            searchQuery.And().Field(Constants.SystemFields.Protection, Guid.Empty.AsKeyword());
        }
        else
        {
            List<string> keys = [Guid.Empty.AsKeyword(), accessContext.PrincipalId.AsKeyword()];

            if (accessContext.GroupIds is not null)
            {
                keys.AddRange(accessContext.GroupIds.Select(groupId => groupId.AsKeyword()));
            }

            searchQuery.And().GroupedOr([Constants.SystemFields.Protection], keys.ToArray());
        }
    }

    private void AddSorters(IBooleanOperation searchQuery, IEnumerable<Sorter>? sorters)
    {
        if (sorters is null)
        {
            return;
        }

        // TODO: Handling of multiple sorters, does this hold up?
        foreach (Sorter sorter in sorters)
        {
            SortableField[] sortableFields = MapSorter(sorter);
            if (sortableFields.Length == 0)
            {
                continue;
            }

            if (sorter.Direction is Direction.Ascending)
            {
                searchQuery.OrderBy(sortableFields);
            }
            else
            {
                searchQuery.OrderByDescending(sortableFields);
            }
        }
    }

    private SortableField[] MapSorter(Sorter sorter)
        => sorter switch
        {
            IntegerSorter => [new SortableField(FieldNameHelper.FieldName(sorter.FieldName, Constants.FieldValues.Integers), SortType.Int)],
            DecimalSorter => [new SortableField(FieldNameHelper.FieldName(sorter.FieldName, Constants.FieldValues.Decimals), SortType.Double)],
            DateTimeOffsetSorter => [new SortableField(FieldNameHelper.FieldName(sorter.FieldName, Constants.FieldValues.DateTimeOffsets), SortType.Long)],
            KeywordSorter => [new SortableField(FieldNameHelper.QueryableKeywordFieldName(FieldNameHelper.FieldName(sorter.FieldName, Constants.FieldValues.Keywords)), SortType.String)],
            TextSorter =>
            [
                new SortableField(FieldNameHelper.FieldName(sorter.FieldName, Constants.FieldValues.TextsR1), SortType.String),
                new SortableField(FieldNameHelper.FieldName(sorter.FieldName, Constants.FieldValues.TextsR2), SortType.String),
                new SortableField(FieldNameHelper.FieldName(sorter.FieldName, Constants.FieldValues.TextsR3), SortType.String),
                new SortableField(FieldNameHelper.FieldName(sorter.FieldName, Constants.FieldValues.Texts), SortType.String),
            ],
            ScoreSorter => [],
            _ => throw new ArgumentOutOfRangeException(nameof(sorter))
        };

    private void AddFilters(IBooleanOperation searchQuery, IEnumerable<Filter>? filters)
    {
        if (filters is null)
        {
            return;
        }

        foreach (Filter filter in filters)
        {
            switch (filter)
            {
                case KeywordFilter keywordFilter:
                    var keywordFieldName = FieldNameHelper.FieldName(filter.FieldName, Constants.FieldValues.Keywords);

                    if (keywordFilter.Negate)
                    {
                        searchQuery.Not().GroupedOr([keywordFieldName], keywordFilter.Values);
                    }
                    else
                    {
                        searchQuery.And().GroupedOr([keywordFieldName], keywordFilter.Values);
                    }

                    break;
                case TextFilter textFilter:
                    IExamineValue[] textFilterValue = textFilter.Values.Select(value => value.TransformDashes().MultipleCharacterWildcard()).ToArray();
                    string[] textFields =
                    [
                        FieldNameHelper.FieldName(filter.FieldName, Constants.FieldValues.Texts),
                        FieldNameHelper.FieldName(filter.FieldName, Constants.FieldValues.TextsR1),
                        FieldNameHelper.FieldName(filter.FieldName, Constants.FieldValues.TextsR2),
                        FieldNameHelper.FieldName(filter.FieldName, Constants.FieldValues.TextsR3)
                    ];

                    if (textFilter.Negate)
                    {
                        searchQuery.Not().GroupedOr(textFields, textFilterValue);
                    }
                    else
                    {
                        searchQuery.And().GroupedOr(textFields, textFilterValue);
                    }

                    break;
                case IntegerRangeFilter integerRangeFilter:
                    var integerRangeFieldName = FieldNameHelper.FieldName(integerRangeFilter.FieldName, Constants.FieldValues.Integers);
                    FilterRange<int>[] integerRanges = integerRangeFilter.Ranges
                        .Select(r => new FilterRange<int>(r.MinValue ?? int.MinValue, r.MaxValue ?? int.MaxValue))
                        .ToArray();
                    searchQuery.AddRangeFilter(integerRangeFieldName, integerRangeFilter.Negate, integerRanges);
                    break;
                case IntegerExactFilter integerExactFilter:
                    var integerExactFieldName = FieldNameHelper.FieldName(integerExactFilter.FieldName, Constants.FieldValues.Integers);
                    searchQuery.AddExactFilter(integerExactFieldName, integerExactFilter);
                    break;
                case DecimalRangeFilter decimalRangeFilter:
                    var decimalRangeFieldName = FieldNameHelper.FieldName(decimalRangeFilter.FieldName, Constants.FieldValues.Decimals);
                    FilterRange<double>[] doubleRanges = decimalRangeFilter.Ranges
                        .Select(r => new FilterRange<double>(Convert.ToDouble(r.MinValue ?? decimal.MinValue), Convert.ToDouble(r.MaxValue ?? decimal.MaxValue)))
                        .ToArray();
                    searchQuery.AddRangeFilter(decimalRangeFieldName, decimalRangeFilter.Negate, doubleRanges);
                    break;
                case DecimalExactFilter decimalExactFilter:
                    var decimalExactFieldName = FieldNameHelper.FieldName(decimalExactFilter.FieldName, Constants.FieldValues.Decimals);
                    var doubleExactFilter = new DoubleExactFilter(filter.FieldName, decimalExactFilter.Values.Select(xx => (double)xx).ToArray(), filter.Negate);
                    searchQuery.AddExactFilter(decimalExactFieldName, doubleExactFilter);
                    break;
                case DateTimeOffsetRangeFilter dateTimeOffsetRangeFilter:
                    var dateTimeOffsetRangeFieldName = FieldNameHelper.FieldName(dateTimeOffsetRangeFilter.FieldName, Constants.FieldValues.DateTimeOffsets);
                    FilterRange<DateTime>[] dateTimeRanges = dateTimeOffsetRangeFilter.Ranges
                        .Select(r => new FilterRange<DateTime>(r.MinValue?.DateTime ?? DateTime.MinValue, r.MaxValue?.DateTime ?? DateTime.MaxValue))
                        .ToArray();
                    searchQuery.AddRangeFilter(dateTimeOffsetRangeFieldName, dateTimeOffsetRangeFilter.Negate, dateTimeRanges);
                    break;
                case DateTimeOffsetExactFilter dateTimeOffsetExactFilter:
                    var dateTimeOffsetExactFieldName = FieldNameHelper.FieldName(dateTimeOffsetExactFilter.FieldName, Constants.FieldValues.DateTimeOffsets);
                    var datetimeExactFilter = new DateTimeExactFilter(filter.FieldName, dateTimeOffsetExactFilter.Values.Select(value => value.DateTime).ToArray(), filter.Negate);
                    searchQuery.AddExactFilter(dateTimeOffsetExactFieldName, datetimeExactFilter);
                    break;
            }
        }
    }

    private void AddFacets(IOrdering searchQuery, IEnumerable<Facet>? facets)
    {
        if (facets is null)
        {
            return;
        }

        searchQuery.WithFacets(facetOperations =>
        {
            foreach (Facet facet in facets)
            {
                switch (facet)
                {
                    case IntegerExactFacet integerExactFacet:
                        facetOperations.FacetString(FieldNameHelper.FieldName(integerExactFacet.FieldName, Constants.FieldValues.Integers), config => config.MaxCount(_searcherOptions.MaxFacetValues));
                        break;
                    case IntegerRangeFacet integerRangeFacet:
                        facetOperations.FacetLongRange(
                            FieldNameHelper.FieldName(integerRangeFacet.FieldName, Constants.FieldValues.Integers),
                            integerRangeFacet.Ranges
                                .Select(x =>
                                    new Int64Range(x.Key, x.MinValue ?? 0, true, x.MaxValue ?? int.MaxValue, false))
                                .ToArray());
                        break;
                    case DecimalExactFacet decimalExactFacet:
                        facetOperations.FacetString(FieldNameHelper.FieldName(decimalExactFacet.FieldName, Constants.FieldValues.Decimals), config => config.MaxCount(_searcherOptions.MaxFacetValues));
                        break;
                    case DecimalRangeFacet decimalRangeFacet:
                    {
                        DoubleRange[] doubleRanges = decimalRangeFacet.Ranges.Select(x =>
                                new DoubleRange(
                                    x.Key,
                                    decimal.ToDouble(x.MinValue ?? 0),
                                    true,
                                    decimal.ToDouble(x.MaxValue ?? 0),
                                    false))
                            .ToArray();
                        facetOperations.FacetDoubleRange(
                            FieldNameHelper.FieldName(decimalRangeFacet.FieldName, Constants.FieldValues.Decimals),
                            doubleRanges);
                        break;
                    }
                    case DateTimeOffsetExactFacet dateTimeOffsetExactFacet:
                        facetOperations.FacetString(FieldNameHelper.FieldName(dateTimeOffsetExactFacet.FieldName, Constants.FieldValues.DateTimeOffsets), config => config.MaxCount(_searcherOptions.MaxFacetValues));
                        break;
                    case DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet:
                        facetOperations.FacetLongRange(
                            FieldNameHelper.FieldName(dateTimeOffsetRangeFacet.FieldName, Constants.FieldValues.DateTimeOffsets),
                            dateTimeOffsetRangeFacet.Ranges.Select(x => new Int64Range(
                                x.Key,
                                x.MinValue?.Ticks ?? DateTime.MinValue.Ticks,
                                true,
                                x.MaxValue?.Ticks ?? DateTime.MaxValue.Ticks,
                                false))
                                .ToArray());
                        break;
                    case KeywordFacet keywordFacet:
                        var keywordFieldName = FieldNameHelper.QueryableKeywordFieldName(FieldNameHelper.FieldName(keywordFacet.FieldName, Constants.FieldValues.Keywords));
                        facetOperations.FacetString(keywordFieldName, config => config.MaxCount(_searcherOptions.MaxFacetValues));
                        break;
                }
            }
        });
    }

    private Facet[] DeduplicateFacets(IEnumerable<Facet>? facets)
    {
        if (facets is null)
        {
            return [];
        }

        return facets
            .GroupBy(f => (f.FieldName, f.GetType())) // group by field + facet type
            .Select(group =>
            {
                Facet first = group.First();

                return first.GetType().IsSubclassOf(typeof(RangeFacet<>)) ? MergeRangeFacets(group) : first;
            })
            .ToArray();
    }

    private Facet MergeRangeFacets(IEnumerable<Facet> facets)
    {
        Facet first = facets.First();
        Type type = first.GetType();

        PropertyInfo rangesProperty = type.GetProperty("Ranges")!;
        var allRanges = facets
            .SelectMany(f => (IEnumerable<object>)rangesProperty.GetValue(f)!)
            .Distinct()
            .ToArray();

        // Construct new facet with FieldName + merged ranges
        return (Facet)Activator.CreateInstance(type, first.FieldName, allRanges)!;
    }


    private static Document? MapToDocument(ISearchResult item)
    {
        var objectTypeString = item.Values.GetValueOrDefault(Constants.SystemFields.IndexType);

        Enum.TryParse(objectTypeString, out UmbracoObjectTypes umbracoObjectType);

        if (Guid.TryParse(item.Id, out Guid guidId))
        {
            return new Document(guidId, umbracoObjectType);
        }

        // The id of an item may be appended with _{culture_{segment}, so strip those and map to guid.
        var indexofUnderscore = item.Id.IndexOf('_');
        var idWithOutCulture = item.Id.Remove(indexofUnderscore);
        return Guid.TryParse(idWithOutCulture, out Guid idWithoutCultureGuid)
            ? new Document(idWithoutCultureGuid, umbracoObjectType)
            : null;
    }

    private IEnumerable<FacetResult> MapFacets(ISearchResults searchResults, IEnumerable<Facet> queryFacets)
    {
        foreach (Facet facet in queryFacets)
        {
            switch (facet)
            {
                case IntegerRangeFacet integerRangeFacet:
                {
                    IEnumerable<IntegerRangeFacetValue> integerRangeFacetResult = integerRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount(FieldNameHelper.FieldName(integerRangeFacet.FieldName, Constants.FieldValues.Integers), x.Key, searchResults);
                        return new IntegerRangeFacetValue(x.Key, x.MinValue, x.MaxValue, value);
                    });
                    yield return new FacetResult(facet.FieldName, integerRangeFacetResult);
                    break;
                }
                case IntegerExactFacet integerExactFacet:
                    IFacetResult? examineIntegerFacets = searchResults.GetFacet(FieldNameHelper.FieldName(integerExactFacet.FieldName, Constants.FieldValues.Integers));
                    if (examineIntegerFacets is null)
                    {
                        continue;
                    }

                    var integerExactFacetValues = new List<IntegerExactFacetValue>();
                    foreach (IFacetValue integerExactFacetValue in examineIntegerFacets)
                    {
                        if (int.TryParse(integerExactFacetValue.Label, out var labelValue) is false)
                        {
                            // Cannot convert the label to int, skipping.
                            continue;
                        }
                        integerExactFacetValues.Add(new IntegerExactFacetValue(labelValue, (int)integerExactFacetValue.Value));
                    }

                    yield return new FacetResult(facet.FieldName, integerExactFacetValues.OrderBy(x => x.Key));
                    break;
                case DecimalRangeFacet decimalRangeFacet:
                    IEnumerable<DecimalRangeFacetValue> decimalRangeFacetResult = decimalRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount(FieldNameHelper.FieldName(decimalRangeFacet.FieldName, Constants.FieldValues.Decimals), x.Key, searchResults);
                        return new DecimalRangeFacetValue(x.Key, x.MinValue, x.MaxValue, value);
                    });
                    yield return new FacetResult(facet.FieldName, decimalRangeFacetResult);
                    break;
                case DecimalExactFacet decimalExactFacet:
                    IFacetResult? examineDecimalFacets = searchResults.GetFacet(FieldNameHelper.FieldName(decimalExactFacet.FieldName, Constants.FieldValues.Decimals));
                    if (examineDecimalFacets is null)
                    {
                        continue;
                    }

                    var decimalExactFacetValues = new List<DecimalExactFacetValue>();

                    foreach (IFacetValue decimalExactFacetValue in examineDecimalFacets)
                    {
                        if (decimal.TryParse(decimalExactFacetValue.Label, out var labelValue) is false)
                        {
                            // Cannot convert the label to decimal, skipping.
                            continue;
                        }
                        decimalExactFacetValues.Add(new DecimalExactFacetValue(labelValue, (int)decimalExactFacetValue.Value));
                    }

                    yield return new FacetResult(facet.FieldName, decimalExactFacetValues.OrderBy(x => x.Key));
                    break;
                case DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet:
                    IEnumerable<DateTimeOffsetRangeFacetValue> dateTimeOffsetRangeFacetResult = dateTimeOffsetRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount(FieldNameHelper.FieldName(dateTimeOffsetRangeFacet.FieldName, Constants.FieldValues.DateTimeOffsets), x.Key, searchResults);
                        return new DateTimeOffsetRangeFacetValue(x.Key, x.MinValue, x.MaxValue, value);
                    });
                    yield return new FacetResult(facet.FieldName, dateTimeOffsetRangeFacetResult);
                    break;
                case DateTimeOffsetExactFacet dateTimeOffsetExactFacet:
                    IFacetResult? examineDatetimeFacets = searchResults.GetFacet(FieldNameHelper.FieldName(dateTimeOffsetExactFacet.FieldName, Constants.FieldValues.DateTimeOffsets));
                    if (examineDatetimeFacets is null)
                    {
                        continue;
                    }

                    var datetimeOffsetExactFacetValues = new List<DateTimeOffsetExactFacetValue>();

                    foreach (IFacetValue datetimeExactFacetValue in examineDatetimeFacets)
                    {
                        if (long.TryParse(datetimeExactFacetValue.Label, out var ticks) is false)
                        {
                            // Cannot convert the label to ticks (long), skipping.
                            continue;
                        }

                        DateTimeOffset offSet = new DateTimeOffset().AddTicks(ticks);
                        datetimeOffsetExactFacetValues.Add(new DateTimeOffsetExactFacetValue(offSet, (int)datetimeExactFacetValue.Value));
                    }

                    yield return new FacetResult(facet.FieldName, datetimeOffsetExactFacetValues.OrderBy(x => x.Key));
                    break;
                case KeywordFacet keywordFacet:
                    IFacetResult? examineKeywordFacets = searchResults.GetFacet(FieldNameHelper.QueryableKeywordFieldName(FieldNameHelper.FieldName(keywordFacet.FieldName, Constants.FieldValues.Keywords)));
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

    private static int GetFacetCount(string fieldName, string key, ISearchResults results)
        => (int?)results.GetFacet(fieldName)?.Facet(key)?.Value ?? 0;
}
