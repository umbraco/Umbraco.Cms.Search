﻿using Umbraco.Cms.Core.Models;

namespace Umbraco.Test.Search.Integration.Tests;

public partial class InvariantContentStructureTests
{
    [Test]
    public void DraftStructure_YieldsAllDocuments()
    {
        ContentService.Save([Root(), Child(), Grandchild(), GreatGrandchild()]);

        var documents = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Id, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Id, Is.EqualTo(GreatGrandchildKey));

            Assert.That(documents.All(d => d.ObjectType is UmbracoObjectTypes.Document), Is.True);
        });
    }

    [Test]
    public void DraftStructure_YieldsNoPublishedDocuments()
    {
        ContentService.Save([Root(), Child(), Grandchild(), GreatGrandchild()]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(0));
    }

    [Test]
    public void DraftRoot_YieldsOnlyDraftRoot()
    {
        ContentService.Save(Root());

        var documents = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Id, Is.EqualTo(RootKey));
    }

    [Test]
    public void DraftStructure_WithGrandchildInRecycleBin_YieldsAllDocuments()
    {
        ContentService.Save([Root(), Child(), Grandchild(), GreatGrandchild()]);
        
        var result = ContentService.MoveToRecycleBin(Grandchild());
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Grandchild().Trashed, Is.True);
            Assert.That(GreatGrandchild().Trashed, Is.True);
        });

        var documents = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(documents, Has.Count.EqualTo(4));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Id, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Id, Is.EqualTo(GreatGrandchildKey));
        });
    }

    [Test]
    public void DraftStructure_WithGrandchildDeleted_YieldsNothingBelowChild()
    {
        ContentService.Save([Root(), Child(), Grandchild(), GreatGrandchild()]);
        
        var result = ContentService.Delete(Grandchild());
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ContentService.GetById(GreatGrandchildKey), Is.Null);
        });
           
        var documents = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
        });
    }
}