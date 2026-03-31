using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Persistence;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.PersistenceTests;

[TestFixture]
public class RemoveFieldsByCultureTests : TestBase
{
    private IIndexDocumentRepository IndexDocumentRepository => GetRequiredService<IIndexDocumentRepository>();

    private IContentIndexingService ContentIndexingService => GetRequiredService<IContentIndexingService>();

    [TestCase(true)]
    [TestCase(false)]
    public async Task RemoveFieldsByCulture_RemovesDocumentsWithMatchingCulture(bool publish)
    {
        await CreateVariantContent(publish);

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            IndexDocument? doc = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(doc, Is.Not.Null);
            Assert.That(doc!.Fields.Any(f => f.Culture == "da-DK"), Is.True);

            await IndexDocumentRepository.RemoveFieldsByCultureAsync(["da-DK"]);

            IndexDocument? docAfter = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(docAfter, Is.Null, "Document with fields for the deleted culture should be removed from cache");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RemoveFieldsByCulture_PreservesInvariantOnlyDocuments(bool publish)
    {
        await CreateInvariantAndVariantContent(publish);

        Guid invariantKey;
        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // Both documents should exist
            IndexDocument? variantDoc = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(variantDoc, Is.Not.Null);

            invariantKey = ChildKey;
            IndexDocument? invariantDoc = await IndexDocumentRepository.GetAsync(invariantKey, publish);
            Assert.That(invariantDoc, Is.Not.Null);
            Assert.That(invariantDoc!.Fields.All(f => f.Culture is null), Is.True, "Invariant document should have no culture-specific fields");

            await IndexDocumentRepository.RemoveFieldsByCultureAsync(["da-DK"]);

            // Variant document should be removed (it had da-DK fields)
            IndexDocument? variantDocAfter = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(variantDocAfter, Is.Null, "Variant document should be removed from cache");

            // Invariant document should be preserved (no culture-specific fields)
            IndexDocument? invariantDocAfter = await IndexDocumentRepository.GetAsync(invariantKey, publish);
            Assert.That(invariantDocAfter, Is.Not.Null, "Invariant document should be preserved in cache");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RemoveFieldsByCulture_RebuildRecreatesDocumentsFromContent(bool publish)
    {
        await CreateVariantContent(publish);
        var indexAlias = GetIndexAlias(publish);

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            IndexDocument? doc = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(doc, Is.Not.Null);

            await IndexDocumentRepository.RemoveFieldsByCultureAsync(["da-DK"]);

            IndexDocument? docAfterRemove = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(docAfterRemove, Is.Null);
        }

        // Rebuild should re-collect the document (without the deleted language, since
        // the language no longer exists in the system at that point - but for this test
        // the language still exists, so all cultures will be re-collected)
        ContentIndexingService.Rebuild(indexAlias, DefaultOrigin);

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            IndexDocument? docAfterRebuild = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(docAfterRebuild, Is.Not.Null, "Document should be re-created after rebuild");
            Assert.That(docAfterRebuild!.Fields.Length, Is.GreaterThan(0));
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RemoveFieldsByCulture_MultipleIsoCodes_RemovesAllMatching(bool publish)
    {
        await CreateVariantContent(publish);

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            IndexDocument? doc = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(doc, Is.Not.Null);
            Assert.That(doc!.Fields.Any(f => f.Culture == "da-DK"), Is.True);
            Assert.That(doc.Fields.Any(f => f.Culture == "ja-JP"), Is.True);

            await IndexDocumentRepository.RemoveFieldsByCultureAsync(["da-DK", "ja-JP"]);

            IndexDocument? docAfter = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(docAfter, Is.Null, "Document with fields for any deleted culture should be removed from cache");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeletingLanguage_RemovesVariantDocumentsFromCache_ButPreservesInvariant(bool publish)
    {
        await CreateInvariantAndVariantContent(publish);

        IndexField[] invariantFieldsBefore;
        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // Both documents should exist in cache before language deletion
            IndexDocument? variantDoc = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(variantDoc, Is.Not.Null, "Variant document should be in cache before language deletion");

            IndexDocument? invariantDoc = await IndexDocumentRepository.GetAsync(ChildKey, publish);
            Assert.That(invariantDoc, Is.Not.Null, "Invariant document should be in cache before language deletion");
            invariantFieldsBefore = invariantDoc!.Fields;
        }

        // Delete the language through the service - this triggers the full notification pipeline:
        // LanguageDeletedNotification -> LanguageNotificationHandler -> RemoveFieldsByCultureAsync
        await LanguageService.DeleteAsync("da-DK", Cms.Core.Constants.Security.SuperUserKey);

        // Allow background rebuild to complete
        await Task.Delay(4000);

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // The invariant document should still be in cache with the same fields (not wiped by the
            // language deletion). This is the key assertion - previously, ClearDocumentIndexCache()
            // would have wiped it too, forcing an expensive re-collection from content services.
            IndexDocument? invariantDocAfter = await IndexDocumentRepository.GetAsync(ChildKey, publish);
            Assert.That(invariantDocAfter, Is.Not.Null, "Invariant document cache should be preserved after language deletion");
            Assert.That(invariantDocAfter!.Fields.Length, Is.EqualTo(invariantFieldsBefore.Length), "Invariant document fields should be unchanged");

            // The variant document should be re-created by the rebuild
            IndexDocument? variantDocAfter = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(variantDocAfter, Is.Not.Null, "Variant document should be re-created by rebuild");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RemoveFieldsByCulture_NonMatchingCulture_PreservesDocument(bool publish)
    {
        await CreateVariantContent(publish);

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            IndexDocument? doc = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(doc, Is.Not.Null);
            var originalFieldCount = doc!.Fields.Length;

            await IndexDocumentRepository.RemoveFieldsByCultureAsync(["fr-FR"]);

            IndexDocument? docAfter = await IndexDocumentRepository.GetAsync(RootKey, publish);
            Assert.That(docAfter, Is.Not.Null, "Document should be preserved when no fields match the deleted culture");
            Assert.That(docAfter!.Fields.Length, Is.EqualTo(originalFieldCount));
        }
    }

    private async Task CreateVariantContent(bool publish)
    {
        await PackageMigrationRunner.RunPackageMigrationsIfPendingAsync("Umbraco CMS Search").ConfigureAwait(false);
        Assert.That(RuntimeState.Level, Is.EqualTo(RuntimeLevel.Run));

        ILanguage langDk = new LanguageBuilder()
            .WithCultureInfo("da-DK")
            .WithIsDefault(true)
            .Build();
        ILanguage langJp = new LanguageBuilder()
            .WithCultureInfo("ja-JP")
            .Build();

        await LanguageService.CreateAsync(langDk, Cms.Core.Constants.Security.SuperUserKey);
        await LanguageService.CreateAsync(langJp, Cms.Core.Constants.Security.SuperUserKey);

        IContentType contentType = new ContentTypeBuilder()
            .WithAlias("variantType")
            .WithContentVariation(ContentVariation.Culture)
            .AddPropertyType()
                .WithAlias("title")
                .WithVariations(ContentVariation.Culture)
                .WithDataTypeId(Cms.Core.Constants.DataTypes.Textbox)
                .WithPropertyEditorAlias(Cms.Core.Constants.PropertyEditors.Aliases.TextBox)
                .Done()
            .Build();
        ContentTypeService.Save(contentType);

        Content root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Root EN")
            .WithCultureName("da-DK", "Root DA")
            .WithCultureName("ja-JP", "Root JP")
            .Build();

        root.SetValue("title", "English Title", "en-US");
        root.SetValue("title", "Danish Title", "da-DK");
        root.SetValue("title", "Japanese Title", "ja-JP");

        var indexAlias = GetIndexAlias(publish);
        await WaitForIndexing(indexAlias, () =>
        {
            ContentService.Save(root);
            if (publish)
            {
                ContentService.Publish(root, ["*"]);
            }

            return Task.CompletedTask;
        });
    }

    private async Task CreateInvariantAndVariantContent(bool publish)
    {
        await PackageMigrationRunner.RunPackageMigrationsIfPendingAsync("Umbraco CMS Search").ConfigureAwait(false);
        Assert.That(RuntimeState.Level, Is.EqualTo(RuntimeLevel.Run));

        ILanguage langDk = new LanguageBuilder()
            .WithCultureInfo("da-DK")
            .WithIsDefault(true)
            .Build();

        await LanguageService.CreateAsync(langDk, Cms.Core.Constants.Security.SuperUserKey);

        // Variant content type
        IContentType variantType = new ContentTypeBuilder()
            .WithAlias("variantType")
            .WithContentVariation(ContentVariation.Culture)
            .AddPropertyType()
                .WithAlias("title")
                .WithVariations(ContentVariation.Culture)
                .WithDataTypeId(Cms.Core.Constants.DataTypes.Textbox)
                .WithPropertyEditorAlias(Cms.Core.Constants.PropertyEditors.Aliases.TextBox)
                .Done()
            .Build();
        ContentTypeService.Save(variantType);

        // Invariant content type
        IContentType invariantType = new ContentTypeBuilder()
            .WithAlias("invariantType")
            .WithContentVariation(ContentVariation.Nothing)
            .AddPropertyType()
                .WithAlias("title")
                .WithVariations(ContentVariation.Nothing)
                .WithDataTypeId(Cms.Core.Constants.DataTypes.Textbox)
                .WithPropertyEditorAlias(Cms.Core.Constants.PropertyEditors.Aliases.TextBox)
                .Done()
            .Build();
        ContentTypeService.Save(invariantType);

        var indexAlias = GetIndexAlias(publish);

        // Create variant content
        Content variantRoot = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(variantType)
            .WithCultureName("en-US", "Variant EN")
            .WithCultureName("da-DK", "Variant DA")
            .Build();

        variantRoot.SetValue("title", "English Title", "en-US");
        variantRoot.SetValue("title", "Danish Title", "da-DK");

        await WaitForIndexing(indexAlias, () =>
        {
            ContentService.Save(variantRoot);
            if (publish)
            {
                ContentService.Publish(variantRoot, ["*"]);
            }

            return Task.CompletedTask;
        });

        // Create invariant content
        Content invariantRoot = new ContentBuilder()
            .WithKey(ChildKey)
            .WithContentType(invariantType)
            .Build();

        invariantRoot.Name = "Invariant Content";
        invariantRoot.SetValue("title", "Invariant Title");

        await WaitForIndexing(indexAlias, () =>
        {
            ContentService.Save(invariantRoot);
            if (publish)
            {
                ContentService.Publish(invariantRoot, ["*"]);
            }

            return Task.CompletedTask;
        });
    }
}
