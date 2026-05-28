using Hangfire.Console.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.PlexModels.Media;
using Plex.ServerApi.PlexModels.Server.History;
using PlexNotifierr.Core.Config;
using PlexNotifierr.Core.Models;
using PlexNotifierr.TestSupport;
using PlexNotifierr.Worker.Jobs;
using Shouldly;

namespace PlexNotifierr.Worker.Tests;

public sealed class GetUsersHistoryJobTests : IDisposable
{
    private const int UserPlexId = 10;
    private const int ShowRatingKey = 200;

    private readonly SqliteInMemoryDatabase _db = new();
    private readonly IPlexServerClient _serverClient = Substitute.For<IPlexServerClient>();
    private readonly IProgressBarFactory _progressBarFactory = Substitute.For<IProgressBarFactory>();

    public void Dispose() => _db.Dispose();

    private GetUsersHistoryJob CreateJob(PlexNotifierrDbContext context) =>
        new(context, _serverClient, _progressBarFactory,
            Options.Create(new PlexConfig { ServerUrl = "http://plex", AccessToken = "token" }),
            NullLogger<GetUsersHistoryJob>.Instance);

    /// <summary>
    /// A single page of play history (Size &lt; the job's 300 page-limit, so the paging loop stops
    /// after one iteration).
    /// </summary>
    private void GivenHistoryPage(params HistoryMetadata[] items) =>
        _serverClient.GetPlayHistory(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<int?>())
            .Returns(new HistoryMediaContainer { Size = items.Length, HistoryMetadata = items.ToList() });

    private static HistoryMetadata Episode(int grandparentRatingKey) => new()
    {
        Type = "episode",
        GrandParentKey = $"/library/metadata/{grandparentRatingKey}",
    };

    private void GivenShowMetadata(string title, string summary) =>
        _serverClient.GetMediaMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new MediaContainer { Media = [new Metadata { Title = title, Summary = summary }] });

    private void GivenShowPoster(string posterKey) =>
        _serverClient.GetMediaPostersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new MediaContainer { Media = [new Metadata { Key = posterKey }] });

    private void SeedUser(int historyPosition = 0) =>
        _db.Seed(c => c.Users.Add(new User { PlexId = UserPlexId, PlexName = "alice", HistoryPosition = historyPosition }));

    private (int users, int medias, int subs, int historyPosition) Snapshot()
    {
        using var context = _db.CreateContext();
        return (
            context.Users.Count(),
            context.Medias.Count(),
            context.UserSubscriptions.Count(),
            context.Users.Single(u => u.PlexId == UserPlexId).HistoryPosition);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesMediaAndSubscription_ForNewShowInHistory()
    {
        SeedUser();
        GivenHistoryPage(Episode(ShowRatingKey));
        GivenShowMetadata(title: "New Show", summary: "A great show");
        GivenShowPoster(posterKey: "http://poster.jpg");

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        await using var verify = _db.CreateContext();
        var media = verify.Medias.SingleOrDefault(m => m.RatingKey == ShowRatingKey);
        media.ShouldNotBeNull();
        media.Title.ShouldBe("New Show");
        media.Summary.ShouldBe("A great show");
        media.ThumbUrl.ShouldBe("http://poster.jpg");
        verify.UserSubscriptions.Count(s => s.RatingKey == ShowRatingKey).ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_AddsSubscriptionOnly_WhenMediaAlreadyExists()
    {
        SeedUser();
        _db.Seed(c => c.Medias.Add(new Media { RatingKey = ShowRatingKey, Title = "Existing" }));
        GivenHistoryPage(Episode(ShowRatingKey));

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        var snapshot = Snapshot();
        snapshot.medias.ShouldBe(1);           // no new media created
        snapshot.subs.ShouldBe(1);             // subscription added
        snapshot.historyPosition.ShouldBe(1);  // page fully processed (no swallowed exception)
        // existing media short-circuits the metadata lookups
        await _serverClient.DidNotReceive().GetMediaMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotDuplicate_WhenUserAlreadySubscribed()
    {
        SeedUser();
        _db.Seed(c =>
        {
            var user = c.Users.Single(u => u.PlexId == UserPlexId);
            var media = new Media { RatingKey = ShowRatingKey, Title = "Existing" };
            c.Medias.Add(media);
            c.UserSubscriptions.Add(new UserSubscription { Media = media, User = user });
        });
        GivenHistoryPage(Episode(ShowRatingKey));

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        var snapshot = Snapshot();
        snapshot.subs.ShouldBe(1);             // no duplicate subscription
        snapshot.historyPosition.ShouldBe(1);  // page fully processed
        await _serverClient.DidNotReceive().GetMediaMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_StopsUser_WhenHistoryFetchFails_WithoutRetryingSameOffset()
    {
        SeedUser();
        // First page fetch fails; a terminal empty page is queued so a regression to the old
        // swallow-and-retry behavior surfaces as a second call instead of hanging the test.
        _serverClient.GetPlayHistory(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<int?>())
            .Returns(
                _ => Task.FromException<HistoryMediaContainer>(new InvalidOperationException("plex down")),
                _ => Task.FromResult(new HistoryMediaContainer { Size = 0, HistoryMetadata = [] }));

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        await _serverClient.Received(1).GetPlayHistory(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<DateTime?>(), Arg.Any<int?>());
        Snapshot().historyPosition.ShouldBe(0); // cursor left untouched for the next run
    }

    [Fact]
    public async Task ExecuteAsync_SkipsUnresolvableShow_ButProcessesTheRestAndAdvancesPosition()
    {
        SeedUser();
        GivenHistoryPage(Episode(200), Episode(300));
        // Show 200 can no longer be resolved on Plex; show 300 is fine.
        _serverClient.GetMediaMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), "200")
            .Returns(Task.FromException<MediaContainer>(new InvalidOperationException("removed from plex")));
        _serverClient.GetMediaMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), "300")
            .Returns(new MediaContainer { Media = [new Metadata { Title = "Show 300", Summary = "" }] });
        GivenShowPoster("http://poster.jpg");

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        await using var verify = _db.CreateContext();
        verify.Medias.Count().ShouldBe(1);
        verify.Medias.Single().RatingKey.ShouldBe(300);
        verify.UserSubscriptions.Count().ShouldBe(1);
        verify.Users.Single(u => u.PlexId == UserPlexId).HistoryPosition.ShouldBe(2); // whole page processed
    }

    [Fact]
    public async Task ExecuteAsync_IgnoresNonEpisodeHistory_ButStillAdvancesPosition()
    {
        SeedUser();
        GivenHistoryPage(new HistoryMetadata { Type = "movie", GrandParentKey = "/library/metadata/999" });

        await using (var context = _db.CreateContext())
            await CreateJob(context).ExecuteAsync();

        var snapshot = Snapshot();
        snapshot.medias.ShouldBe(0);
        snapshot.subs.ShouldBe(0);
        snapshot.historyPosition.ShouldBe(1);
        await _serverClient.DidNotReceive().GetMediaMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
