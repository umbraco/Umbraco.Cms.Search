using System.Text.Json.Serialization.Metadata;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Site.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExamineSearchProvider(this IUmbracoBuilder builder)
    {
        // by default, Examine (Lucene) filters out facet values that are not active (picked) within a facet group,
        // if any facet value is active within that facet group.
        // expanding facets changes that behavior to include non-active (valid) facet values in the result.
        // NOTE: this incurs a performance penalty when querying. 
        builder.Services.Configure<SearcherOptions>(options => options.ExpandFacetValues = true);

        // the Examine search provider requires explicit definitions of the fields used for faceting and/or sorting. 
        builder.Services.Configure<FieldOptions>(options => options.Fields =
            [
                new ()
                {
                    PropertyName = "length",
                    FieldValues = FieldValues.Keywords,
                    Facetable = true
                },
                new ()
                {
                    PropertyName = "authorNationality",
                    FieldValues = FieldValues.Keywords,
                    Facetable = true
                },
                new ()
                {
                    PropertyName = "publishYear",
                    FieldValues = FieldValues.Integers,
                    Facetable = true,
                    Sortable = true
                },
            ]
        );

        return builder;
    }
    
    public static IUmbracoBuilder ConfigureJsonOptions(this IUmbracoBuilder builder)
    {
        builder.Services.AddControllers().AddJsonOptions(
            options =>
            {
                options.JsonSerializerOptions.TypeInfoResolver =
                    options.JsonSerializerOptions.TypeInfoResolver!.WithAddedModifier(typeInfo =>
                    {
                        if (typeInfo.Type != typeof(FacetValue))
                        {
                            return;
                        }

                        // allow all the search core facet value types to be serialized as implementations of FacetValue
                        typeInfo.PolymorphismOptions = new()
                        {
                            DerivedTypes =
                            {
                                new JsonDerivedType(typeof(IntegerRangeFacetValue)),
                                new JsonDerivedType(typeof(DecimalRangeFacetValue)),
                                new JsonDerivedType(typeof(DateTimeOffsetRangeFacetValue)),
                                new JsonDerivedType(typeof(IntegerExactFacetValue)),
                                new JsonDerivedType(typeof(DecimalExactFacetValue)),
                                new JsonDerivedType(typeof(DateTimeOffsetExactFacetValue)),
                                new JsonDerivedType(typeof(KeywordFacetValue)),
                            }
                        };
                    });
            });

        return builder;
    }
}