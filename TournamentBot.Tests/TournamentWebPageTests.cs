using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class TournamentWebPageTests
{
    static TournamentState Fixture()
    {
        var state = new TournamentState();
        state.Players[1] = new RegisteredPlayer { OpenRaName = "Alice<script>alert(1)</script>" };
        state.Players[2] = new RegisteredPlayer { OpenRaName = "Bob" };
        state.Tournaments["T001"] = new TournamentRecord { Id = "T001", Name = "Cup <img src=x onerror=alert(1)>",
            Status = TournamentStatus.Running, Format = TournamentFormat.DoubleElimination, Entrants = new() { 1, 2 },
            SeriesIds = new() { "S0001", "S0002" }, RoundNumber = 2, WinsRequired = 2 };
        state.Series["S0001"] = new TournamentSeries { Id = "S0001", TournamentId = "T001", PlayerOneDiscordId = 1, PlayerTwoDiscordId = 2,
            TournamentRound = 1, Status = TournamentSeriesStatus.Completed, WinsRequired = 2, Wins = new() { [1] = 2, [2] = 0 }, MatchIds = new() { "M0001" }, Bracket = "Upper bracket" };
        state.Series["S0002"] = new TournamentSeries { Id = "S0002", TournamentId = "T001", PlayerOneDiscordId = 1, PlayerTwoDiscordId = 2,
            TournamentRound = 2, Status = TournamentSeriesStatus.Playing, WinsRequired = 2, MatchIds = new() { "M0002" }, Bracket = "Final" };
        state.Matches["M0001"] = new MatchRecord { Id = "M0001", TournamentId = "T001", Status = MatchStatus.Completed, MapTitle = "Plains",
            FinalWinnerDiscordId = 1, ReplayPath = "/private/replays/secret.orarep", Password = "PRIVATE_JOIN_SECRET", SpectatorPassword = "PRIVATE_SPECTATOR_SECRET" };
        state.Matches["M0002"] = new MatchRecord { Id = "M0002", TournamentId = "T001", Status = MatchStatus.WaitingForPlayers, MapTitle = "Mountain Valleys",
            AllowSpectators = true, Port = 12400, SpectatorPassword = "PRIVATE_SPECTATOR_SECRET", Password = "PRIVATE_JOIN_SECRET" };
        return state;
    }

    [Test]
    public void WalkoverHighlightsWinnerAndSourceWithoutFabricatingGameWins()
    {
        var state = Fixture();
        state.Series["S0001"].Wins.Clear();
        state.Series["S0001"].MatchIds.Clear();
        state.Series["S0001"].ForfeitWinnerDiscordId = 1;
        var html = TournamentWebPage.Tournament(state, "T001")!;
        Assert.That(html, Does.Contain("class='side winner' data-player='1'"));
        Assert.That(html, Does.Contain("← Winner S0001").And.Contain("← Loser S0001"));
        Assert.That(html, Does.Not.Contain("/replay/M0001"));
        Assert.That(state.Series["S0001"].Wins, Is.Empty);
    }

    [Test]
    public void BracketEscapesNamesShowsRealConnectionsAndNeverLeaksCredentials()
    {
        var html = TournamentWebPage.Tournament(Fixture(), "t001")!;
        Assert.That(html, Does.Contain("Double elimination").And.Contain("Upper bracket").And.Contain("Final"));
        Assert.That(html, Does.Contain("data-from='S0001'"));
        Assert.That(html, Does.Contain("&lt;script&gt;").And.Contain("&lt;img"));
        Assert.That(html, Does.Not.Contain("<img").And.Not.Contain("<script>alert"));
        Assert.That(html, Does.Not.Contain("PRIVATE_").And.Not.Contain("/private/").And.Not.Contain("?player="));
        Assert.That(html, Does.Contain("/replay/M0001").And.Contain("/spectate/M0002"));
        Assert.That(TournamentWebPage.Tournament(Fixture(), "missing"), Is.Null);
    }

    [Test]
    public void PendingResultsDoNotClaimAWinnerAndPrivateOrStartedMatchesHaveNoSpectatorLink()
    {
        var state = Fixture();
        state.Matches["M0001"].Status = MatchStatus.AwaitingConfirmation;
        state.Matches["M0001"].FinalWinnerDiscordId = null;
        state.Matches["M0001"].AutomaticWinnerDiscordId = 1;
        state.Matches["M0002"].Status = MatchStatus.Playing;
        var html = TournamentWebPage.Tournament(state, "T001")!;
        Assert.That(html, Does.Contain("Awaiting result confirmation"));
        Assert.That(html, Does.Not.Contain("Winner:").And.Not.Contain("/replay/M0001").And.Not.Contain("/spectate/M0002"));
        state.Matches["M0002"].Status = MatchStatus.WaitingForPlayers;
        state.Matches["M0002"].AllowSpectators = false;
        Assert.That(TournamentWebPage.Tournament(state, "T001"), Does.Not.Contain("/spectate/M0002"));
    }

    [Test]
    public void TeamsCancelledTournamentsAndEmptyBracketsRender()
    {
        var state = Fixture(); var t = state.Tournaments["T001"];
        t.Mode = TournamentMode.TwoVsTwo; t.Teams[1] = new TournamentTeam { Name = "Team & One" };
        t.Status = TournamentStatus.Cancelled;
        Assert.That(TournamentWebPage.Tournament(state, t.Id), Does.Contain("Team &amp; One").And.Contain("Cancelled"));
        t.SeriesIds.Clear();
        Assert.That(TournamentWebPage.Tournament(state, t.Id), Does.Contain("No series bracket"));
        t.Status = TournamentStatus.Registration;
        Assert.That(TournamentWebPage.Tournament(state, t.Id), Does.Contain("bracket will appear"));
        Assert.That(TournamentWebPage.Index(state), Does.Contain("/tournament/T001").And.Not.Contain("PRIVATE_"));
    }

    [Test]
    public async Task SeparateLanesAndExpandableDetailsPreserveHistoricalStateAndReplayUrls()
    {
        var state = new TournamentState();
        var t = new TournamentRecord { Id = "PREVIEW", Name = "Bracket layout preview — synthetic results", Format = TournamentFormat.DoubleElimination,
            Status = TournamentStatus.Completed, WinsRequired = 2, ChampionDiscordId = 1 };
        state.Tournaments[t.Id] = t;
        for (ulong p = 1; p <= 8; p++)
        {
            state.Players[p] = new RegisteredPlayer { OpenRaName = new[] { "LördenMcFairGenü", "DavidWasLos", "Boris", "Fux", "der Peer", "Todesbrezel", "VivaLaPanik", "yesyoulike" }[p - 1] };
            t.Entrants.Add(p);
        }
        void Add(int round, string lane, ulong one, ulong two)
        {
            var id = "S" + (state.Series.Count + 1).ToString("D4"); var mid = "M" + (state.Series.Count + 1).ToString("D4");
            t.SeriesIds.Add(id);
            state.Series[id] = new TournamentSeries { Id = id, TournamentId = t.Id, TournamentRound = round, Bracket = lane,
                PlayerOneDiscordId = one, PlayerTwoDiscordId = two, WinsRequired = 2, Status = TournamentSeriesStatus.Completed,
                Wins = new() { [one] = 2, [two] = 1 }, MatchIds = new() { mid } };
            state.Matches[mid] = new MatchRecord { Id = mid, TournamentId = t.Id, SeriesGameNumber = 1,
                Status = MatchStatus.Completed, FinalWinnerDiscordId = one, MapTitle = "Tactical Two Sides", ReplayPath = "/private/replay.orarep" };
        }
        Add(1,"Upper bracket",1,2); Add(1,"Upper bracket",3,4); Add(1,"Upper bracket",5,6); Add(1,"Upper bracket",7,8);
        Add(2,"Upper bracket",1,3); Add(2,"Upper bracket",5,7); Add(2,"Lower bracket",2,4); Add(2,"Lower bracket",6,8);
        Add(3,"Upper bracket",1,5); Add(3,"Lower bracket",2,7); Add(3,"Lower bracket",6,3);
        Add(4,"Lower bracket",2,6); Add(5,"Lower bracket",2,5); Add(6,"Final",1,2);
        var before = System.Text.Json.JsonSerializer.Serialize(state);
        var html = TournamentWebPage.Tournament(state,t.Id)!;
        Assert.That(System.Text.Json.JsonSerializer.Serialize(state), Is.EqualTo(before));
        Assert.That(System.Text.RegularExpressions.Regex.Matches(html,"class='bracket-lane'").Count, Is.EqualTo(3));
        Assert.That(html, Does.Contain("<details id='details-S0001'>").And.Contain("href='#series-S0001'"));
        foreach (var mid in state.Matches.Keys) Assert.That(html, Does.Contain("/replay/"+mid));
        Assert.That(html, Does.Not.Contain("/private/"));
        var evidence = Environment.GetEnvironmentVariable("YMCA_WEB_PREVIEW");
        if (!string.IsNullOrEmpty(evidence))
        {
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"large.html"),html);
            await File.WriteAllTextAsync(Path.Combine(evidence,"tournament-assets.js"),TournamentWebPage.Script);
        }
    }

    [Test]
    public async Task RealHttpPagesRefreshFromStateWithoutExposingStateJson()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ymca-web-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var config = new BotConfiguration { StateFile = Path.Combine(directory, "state.json"), JoinPage = new JoinPageConfiguration { ListenUrl = $"http://127.0.0.1:{port}" } };
        var store = new StateStore(config.StateFile);
        await store.UpdateAsync(state => { var fixture = Fixture(); state.Tournaments = fixture.Tournaments; state.Series = fixture.Series; state.Matches = fixture.Matches; state.Players = fixture.Players; });
        await using var pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        await using var pages = new JoinPageServer(config, new TournamentCoordinator(config, store, pool));
        await pages.StartAsync(CancellationToken.None);
        using var http = new HttpClient { BaseAddress = new Uri(config.JoinPage.ListenUrl) };
        var response = await http.GetAsync("/tournament/T001");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.CacheControl!.NoStore, Is.True);
        Assert.That(response.Headers.GetValues("Content-Security-Policy").Single(), Does.Contain("frame-ancestors 'none'"));
        Assert.That(await http.GetStringAsync("/"), Does.Contain("YMCA tournaments"));
        Assert.That(await http.GetStringAsync("/tournament-assets.js"), Does.Contain("10000"));
        await store.UpdateAsync(state => state.Tournaments["T001"].Name = "Updated Cup");
        Assert.That(await http.GetStringAsync("/tournament/T001"), Does.Contain("Updated Cup"));
        Assert.That((await http.GetAsync("/tournament/unknown")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        // Evidence for a separate browser render; only synthetic data is written.
        var evidence = Environment.GetEnvironmentVariable("YMCA_WEB_PREVIEW");
        if (!string.IsNullOrEmpty(evidence))
        {
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "index.html"), await http.GetStringAsync("/tournament/T001"));
            await File.WriteAllTextAsync(Path.Combine(evidence, "tournament-assets.js"), TournamentWebPage.Script);
        }
    }
}
