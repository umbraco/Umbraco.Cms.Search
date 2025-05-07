using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Site.Services;

public class SiteSegmentService : ISegmentService
{
    private readonly Segment[] _segments =
    [
        new() { Alias = "seg-1", Name = "Segment 1" },
        new() { Alias = "seg-2", Name = "Segment 2" }
    ];

    public Task<Attempt<PagedModel<Segment>?, SegmentOperationStatus>> GetPagedSegmentsAsync(int skip = 0, int take = 100)
        => Task.FromResult
        (
            Attempt.SucceedWithStatus<PagedModel<Segment>?, SegmentOperationStatus>
            (
                SegmentOperationStatus.Success,
                new PagedModel<Segment> { Total = _segments.Length, Items = _segments.Skip(skip).Take(take) }
            )
        );
}