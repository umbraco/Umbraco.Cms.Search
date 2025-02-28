using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Site.NotificationHandlers;

internal sealed class CreateSegmentsNotificationHandler : INotificationHandler<SendingContentNotification>
{
    private readonly IContentTypeService _contentTypeService;

    public CreateSegmentsNotificationHandler(IContentTypeService contentTypeService)
        => _contentTypeService = contentTypeService;

    public void Handle(SendingContentNotification notification)
    {
        var segments = new[]
        {
            new
            {
                Name = "Segment 1",
                Alias = "seg-1"
            },
            new
            {
                Name = "Segment 2",
                Alias = "seg-2"
            }
        };

        void SetSegmentDisplayName(ContentVariantDisplay variant, string segmentAlias)
        {
            var segmentName = segments.FirstOrDefault(s => s.Alias == segmentAlias)?.Name;
            if (segmentName is null)
            {
                return;
            }

            variant.DisplayName = variant.Language is not null ? $"{segmentName} - {variant.Language.Name}" : segmentName;
        }

        if (notification.Content.Id > 0)
        {
            foreach (ContentVariantDisplay variant in notification.Content.Variants.Where(variant => variant.Segment is not null))
            {
                SetSegmentDisplayName(variant, variant.Segment!);
            }

            return;
        }

        // get the content type
        IContentType? contentType = notification.Content.ContentTypeId.HasValue
            ? _contentTypeService.Get(notification.Content.ContentTypeId.Value)
            : null;
        if (contentType == null || contentType.Alias.Contains("Segment") is false)
        {
            return;
        }

        // get all properties of the content type that allow variation by segment
        IEnumerable<string> segmentVariantPropertyAliases = contentType
            .CompositionPropertyTypes
            .Where(p => p.VariesBySegment())
            .Select(p => p.Alias)
            .ToArray();

        // segments are coupled to variants, so make the segment for all variations
        foreach (ContentVariantDisplay variant in notification.Content.Variants.Where(variant => variant.Segment is null))
        {
            foreach (var segment in segments)
            {
                var newVariant = new ContentVariantDisplay
                {
                    AllowedActions = new List<string>(variant.AllowedActions),
                    Language = variant.Language,
                    Segment = segment.Alias,
                    State = ContentSavedState.NotCreated,
                    Tabs = variant.Tabs.Select(x => new Tab<ContentPropertyDisplay>
                    {
                        Alias = x.Alias,
                        IsActive = x.IsActive,
                        Expanded = x.Expanded,
                        Id = x.Id,
                        Key = x.Key,
                        Label = x.Label,
                        Type = x.Type,
                        Properties = x.Properties?.Select(p => new ContentPropertyDisplay
                        {
                            Alias = p.Alias,
                            Config = p.Config,
                            ConfigNullable = p.ConfigNullable,
                            Culture = p.Culture,
                            DataTypeKey = p.DataTypeKey,
                            Description = p.Description,
                            Editor = p.Editor,
                            HideLabel = p.HideLabel,
                            Id = p.Id,
                            IsSensitive = p.IsSensitive,
                            Label = p.Label,
                            LabelOnTop = p.LabelOnTop,
                            PropertyEditor = p.PropertyEditor,
                            Readonly = p.Readonly,
                            Segment = segmentVariantPropertyAliases.Contains(p.Alias) ? segment.Alias : null,
                            SupportsReadOnly = p.SupportsReadOnly,
                            Validation = p.Validation,
                            Value = segmentVariantPropertyAliases.Contains(p.Alias) ? null : p.Value,
                            Variations = p.Variations,
                            View = p.View,
                        }),
                    })
                };

                SetSegmentDisplayName(newVariant, newVariant.Segment);

                notification.Content.Variants = notification.Content.Variants.Append(newVariant);
            }
        }
    }
}
