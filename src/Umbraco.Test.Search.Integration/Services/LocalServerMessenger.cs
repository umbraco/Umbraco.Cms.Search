﻿using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Serialization;
using Umbraco.Cms.Infrastructure.Sync;

namespace Umbraco.Test.Search.Integration.Services;

internal class LocalServerMessenger : ServerMessengerBase
{
    public LocalServerMessenger()
        : base(false, new SystemTextJsonSerializer())
    {
    }

    public override void SendMessages()
    {
    }

    public override void Sync()
    {
    }

    protected override void DeliverRemote(ICacheRefresher refresher, MessageType messageType, IEnumerable<object>? ids = null, string? json = null)
    {
    }
}