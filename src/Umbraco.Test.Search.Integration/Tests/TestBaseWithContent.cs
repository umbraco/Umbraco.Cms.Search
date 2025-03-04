﻿using Umbraco.Cms.Core.Models;

namespace Umbraco.Test.Search.Integration.Tests;

public abstract class TestBaseWithContent : TestBase
{
    protected Guid RootKey { get; } = Guid.NewGuid();

    protected Guid ChildKey { get; } = Guid.NewGuid();

    protected Guid GrandchildKey { get; } = Guid.NewGuid();

    protected Guid GreatGrandchildKey { get; } = Guid.NewGuid();

    protected IContent Root() => ContentService.GetById(RootKey) ?? throw new InvalidOperationException("Root was not found");

    protected IContent Child() => ContentService.GetById(ChildKey) ?? throw new InvalidOperationException("Child was not found");

    protected IContent Grandchild() => ContentService.GetById(GrandchildKey) ?? throw new InvalidOperationException("Grandchild was not found");

    protected IContent GreatGrandchild() => ContentService.GetById(GreatGrandchildKey) ?? throw new InvalidOperationException("Great grandchild was not found");
}