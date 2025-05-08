﻿using Umbraco.Cms.Core.Services;

namespace Umbraco.Test.Search.Integration.Tests.BackOffice;

public partial class ContentSearchServiceTests : BackOfficeTestBase
{
    private IContentSearchService ContentSearchService => GetRequiredService<IContentSearchService>();

    private IMediaSearchService MediaSearchService => GetRequiredService<IMediaSearchService>();
}