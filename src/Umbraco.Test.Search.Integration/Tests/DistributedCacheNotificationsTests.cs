namespace Umbraco.Test.Search.Integration.Tests;

public class DistributedCacheNotificationsTests : CacheNotificationTestBase
{
    protected override bool UseDistributedCache => true;
}
