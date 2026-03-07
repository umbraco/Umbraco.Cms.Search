using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class CacheNotificationTestBase : TestBase
{
    private IMediaTypeService MediaTypeService => GetRequiredService<IMediaTypeService>();

    private IMediaService MediaService => GetRequiredService<IMediaService>();

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IMemberTypeService MemberTypeService => GetRequiredService<IMemberTypeService>();

    private IMemberService MemberService => GetRequiredService<IMemberService>();

    private IPublicAccessService PublicAccessService => GetRequiredService<IPublicAccessService>();

    private IMemberGroupService MemberGroupService => GetRequiredService<IMemberGroupService>();

    protected abstract bool UseDistributedCache { get; }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<LocalServerMessenger>();
        services.Configure<ContentCacheNotificationOptions>(options => options.UseDistributedCache = UseDistributedCache);
        services.AddUnique<IServerMessenger, InvocationCountingLocalServerMessenger>();
    }

    [SetUp]
    public void SetupTest() => IndexerAndSearcher.Reset();

    [Test]
    public async Task TestDraftContent()
    {
        IContentType contentType = new ContentTypeBuilder()
            .WithAlias("myContentType")
            .WithAllowAsRoot(true)
            .Build();

        await ContentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);

        Content content = new ContentBuilder()
            .WithContentType(contentType)
            .WithName("Something")
            .Build();
        ContentService.Save(content);

        IReadOnlyList<TestIndexDocument> documents = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Id, Is.EqualTo(content.Key));

        AssertServerMessengerInvocations();
    }

    [Test]
    public async Task TestPublishedContent()
    {
        IContentType contentType = new ContentTypeBuilder()
            .WithAlias("myContentType")
            .WithAllowAsRoot(true)
            .Build();

        await ContentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);

        Content content = new ContentBuilder()
            .WithContentType(contentType)
            .WithName("Something")
            .Build();
        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);

        IReadOnlyList<TestIndexDocument> documents = IndexerAndSearcher.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Id, Is.EqualTo(content.Key));

        AssertServerMessengerInvocations();
    }

    [Test]
    public async Task TestMedia()
    {
        IMediaType mediaType = new MediaTypeBuilder()
            .WithAlias("myMediaType")
            .Build();

        await MediaTypeService.CreateAsync(mediaType, Constants.Security.SuperUserKey);

        Media media = new MediaBuilder()
            .WithMediaType(mediaType)
            .WithName("Something")
            .Build();
        MediaService.Save(media);

        IReadOnlyList<TestIndexDocument> documents = IndexerAndSearcher.Dump(IndexAliases.Media);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Id, Is.EqualTo(media.Key));

        AssertServerMessengerInvocations();
    }

    [Test]
    public async Task TestMember()
    {
        IMemberType memberType = new MemberTypeBuilder()
            .WithAlias("myMemberType")
            .Build();

        await MemberTypeService.CreateAsync(memberType, Constants.Security.SuperUserKey);

        Member member = new MemberBuilder()
            .WithMemberType(memberType)
            .WithName("Something")
            .Build();
        MemberService.Save(member);

        IReadOnlyList<TestIndexDocument> documents = IndexerAndSearcher.Dump(IndexAliases.Member);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Id, Is.EqualTo(member.Key));

        AssertServerMessengerInvocations();
    }

    [Test]
    public async Task TestPublicAccess()
    {
        IContentType contentType = new ContentTypeBuilder()
            .WithAlias("myContentType")
            .WithAllowAsRoot(true)
            .Build();

        await ContentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);

        Content content = new ContentBuilder()
            .WithContentType(contentType)
            .WithName("Something")
            .Build();
        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);

        IMemberGroup memberGroup = (await MemberGroupService.CreateAsync(new MemberGroup { Name = "testGroup" })).Result!;
        await PublicAccessService.CreateAsync(
            new PublicAccessEntrySlim
            {
                ErrorPageId = content.Key,
                LoginPageId = content.Key,
                ContentId = content.Key,
                MemberGroupNames = [memberGroup.Name!],
            });

        IReadOnlyList<TestIndexDocument> documents = IndexerAndSearcher.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(content.Key));
            Assert.That(documents[0].Protection?.AccessIds, Is.EquivalentTo(new[] { memberGroup.Key }));
        });

        AssertServerMessengerInvocations();
    }

    private void AssertServerMessengerInvocations()
    {
        var serverMessenger = (InvocationCountingLocalServerMessenger)GetRequiredService<IServerMessenger>();
        if (UseDistributedCache)
        {
            Assert.That(serverMessenger.QueuedInvocations, Is.GreaterThan(0));
        }
        else
        {
            Assert.That(serverMessenger.QueuedInvocations, Is.Zero);
        }
    }

    private class InvocationCountingLocalServerMessenger : IServerMessenger
    {
        private readonly IServerMessenger _inner;

        public InvocationCountingLocalServerMessenger(LocalServerMessenger inner)
            => _inner = inner;

        public int QueuedInvocations { get; private set; }

        public void Sync() => _inner.Sync();

        public void SendMessages() => _inner.SendMessages();

        public void QueueRefresh<TPayload>(ICacheRefresher refresher, TPayload[] payload)
        {
            RegisterInvocations(refresher);
            _inner.QueueRefresh(refresher, payload);
        }

        public void QueueRefresh<T>(ICacheRefresher refresher, Func<T, int> getNumericId, params T[] instances)
        {
            RegisterInvocations(refresher);
            _inner.QueueRefresh(refresher, getNumericId, instances);
        }

        public void QueueRefresh<T>(ICacheRefresher refresher, Func<T, Guid> getGuidId, params T[] instances)
        {
            RegisterInvocations(refresher);
            _inner.QueueRefresh(refresher, getGuidId, instances);
        }

        public void QueueRemove<T>(ICacheRefresher refresher, Func<T, int> getNumericId, params T[] instances)
        {
            RegisterInvocations(refresher);
            _inner.QueueRefresh(refresher, getNumericId, instances);
        }

        public void QueueRemove(ICacheRefresher refresher, params int[] numericIds)
        {
            RegisterInvocations(refresher);
            _inner.QueueRefresh(refresher, numericIds);
        }

        public void QueueRefresh(ICacheRefresher refresher, params int[] numericIds)
        {
            RegisterInvocations(refresher);
            _inner.QueueRefresh(refresher, numericIds);
        }

        public void QueueRefresh(ICacheRefresher refresher, params Guid[] guidIds)
        {
            RegisterInvocations(refresher);
            _inner.QueueRefresh(refresher, guidIds);
        }

        public void QueueRefreshAll(ICacheRefresher refresher)
        {
            RegisterInvocations(refresher);
            _inner.QueueRefreshAll(refresher);
        }

        private void RegisterInvocations(ICacheRefresher cacheRefresher)
        {
            if (cacheRefresher.GetType().Namespace!.Contains("Umbraco.Cms.Search.Core"))
            {
                QueuedInvocations++;
            }
        }
    }
}
