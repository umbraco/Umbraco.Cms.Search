using NUnit.Framework;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Umbraco.Test.Search.Examine.Integration.Tests.NonUmbracoTests;

public class NonUmbracoIntegerTests : NonUmbracoTestBase
{
    [Test]
    public async Task ExactFilterSingleValueTest()
    {
        
        // var resultsQuery = await Searcher.SearchAsync(IndexAlias, "5", null, null, null, null, null, null, 0 , 100);

        var results = await Searcher.SearchAsync(IndexAlias, null, [new IntegerExactFilter(FieldSingleValue, [5], false)], null, null, null, null, null, 0 , 100);
        
        Assert.That(results.Total, Is.EqualTo(1));
    }
    
    [Test]
    public async Task ExactFilterMultipleValueTest()
    {
        var results = await Searcher.SearchAsync(IndexAlias, null, [new IntegerExactFilter(FieldMultipleValue, [5], false)], null, null, null, null, null, 0 , 100);
        
        Assert.That(results.Total, Is.EqualTo(2));
    }
    
    [Test]
    public async Task RangeFilterSingleValueTest()
    {
        var results = await Searcher.SearchAsync(IndexAlias, null, [new IntegerRangeFilter(FieldSingleValue, [new FilterRange<int?>(0, int.MaxValue)], false)], null, null, null, null, null, 0 , 100);
        
        Assert.That(results.Total, Is.EqualTo(100));
    }
    [Test]
    public async Task RangeFilterMultipleValueTest()
    {
        var results = await Searcher.SearchAsync(IndexAlias, null, [new IntegerRangeFilter(FieldMultipleValue, [new FilterRange<int?>(0, int.MaxValue)], false)], null, null, null, null, null, 0 , 100);
        
        Assert.That(results.Total, Is.EqualTo(100));
    }
    
    
}