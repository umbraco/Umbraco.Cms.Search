# Reindexing content programmatically

If you ever need to trigger the reindexing of content manually, use the [`IDistributedContentIndexRefresher`](https://github.com/umbraco/Umbraco.Cms.Search/blob/main/src/Umbraco.Cms.Search.Core/Services/ContentIndexing/IDistributedContentIndexRefresher.cs) service.

As the name implies, this service ensures that the conent reindexing happens correctly across all instances in a load balanced setup. This is important, because some search providers (including the [default one](https://github.com/umbraco/Umbraco.Cms.Search/blob/main/docs/examine-provider.md)) explicitly depend on this behavior.

> [!NOTE]
> In Umbraco, content implies either documents, media or members. However, documents are actually named "content", which can be a little confusing.

## How to use

The service allows for reindexing content, media and members. For content, either draft or published state should be targeted explicitly.

Here's a code sample to show the usage of the service:

```csharp
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace My.Site;

public class MyContentReindexer
{
    private readonly IDistributedContentIndexRefresher _distributedContentIndexRefresher;

    public MyContentReindexer(IDistributedContentIndexRefresher distributedContentIndexRefresher)
        => _distributedContentIndexRefresher = distributedContentIndexRefresher;

    public void ReindexContent(IContent content, bool published)
    {
        var contentState = published ? ContentState.Published : ContentState.Draft;
        _distributedContentIndexRefresher.RefreshContent([content], contentState);
    }

    public void ReindexMedia(IMedia media)
        => _distributedContentIndexRefresher.RefreshMedia([media]);

    public void ReindexMember(IMember member)
        => _distributedContentIndexRefresher.RefreshMember([member]);
}
```

## Use reasonably

Content reindexing can be an expensive operation. Do _not_ allow this to be triggered on demand, for example as part of a page rendering or an API. Doing so could lead to excessive resource usage, and ultimately might bring your site down.
