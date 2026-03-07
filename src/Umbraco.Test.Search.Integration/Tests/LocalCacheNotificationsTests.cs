namespace Umbraco.Test.Search.Integration.Tests;

public class LocalCacheNotificationsTests : CacheNotificationTestBase
{
    protected override bool UseDistributedCache => false;
}
