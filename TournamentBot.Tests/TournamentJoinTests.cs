using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class TournamentJoinTests
{
    string directory = "";
    StateStore store = null!;
    OpenRaServerPool pool = null!;
    TournamentCoordinator coordinator = null!;
    TournamentRecord tournament = null!;

    [SetUp]
    public async Task Setup()
    {
        directory = Path.Combine(Path.GetTempPath(), "ymca-tournament-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var config = new BotConfiguration { StateFile = Path.Combine(directory, "state.json"),
            Server = new OpenRaServerConfiguration { MatchDirectory = Path.Combine(directory, "matches") } };
        store = new StateStore(config.StateFile);
        pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        coordinator = new TournamentCoordinator(config, store, pool);
        await coordinator.AddMapAsync("map", "Test", 4);
        for (ulong i = 1; i <= 4; i++) await coordinator.RegisterAsync(i, "Player" + i, "Player" + i);
        tournament = await coordinator.CreateTournamentAsync("Test", TournamentFormat.SingleElimination);
    }

    [TearDown]
    public async Task Cleanup() { await pool.DisposeAsync(); Directory.Delete(directory, true); }

    [Test]
    public async Task RepeatedJoinIsExplicitlyRejectedAndStateIsUnchanged()
    {
        await coordinator.JoinTournamentAsync(tournament.Id, 1);
        var before = File.ReadAllText(Path.Combine(directory, "state.json"));
        var error = Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.JoinTournamentAsync(tournament.Id, 1));
        Assert.That(error!.Message, Does.Contain("already registered"));
        Assert.That(File.ReadAllText(Path.Combine(directory, "state.json")), Is.EqualTo(before));
        Assert.That(tournament.Entrants, Is.EqualTo(new ulong[] { 1 }));
    }

    [Test]
    public async Task ConcurrentRequestsOnlyAcceptOneEntry()
    {
        var outcomes = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(async () =>
        {
            try { await coordinator.JoinTournamentAsync(tournament.Id, 1); return true; }
            catch (InvalidOperationException) { return false; }
        })));
        Assert.That(outcomes.Count(x => x), Is.EqualTo(1));
        Assert.That(tournament.Entrants, Is.EqualTo(new ulong[] { 1 }));
        var reloaded = new StateStore(Path.Combine(directory, "state.json")); await reloaded.LoadAsync();
        Assert.That(await reloaded.ReadAsync(s => s.Tournaments[tournament.Id].Entrants.Count), Is.EqualTo(1));
    }

    [Test]
    public async Task DuplicateModalCannotRenameAnEntrant()
    {
        await coordinator.RegisterAndJoinTournamentAsync(tournament.Id, 1, "First", "CorrectName");
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RegisterAndJoinTournamentAsync(tournament.Id, 1, "Second", "ChangedName"));
        Assert.That((await coordinator.GetPlayerAsync(1))!.OpenRaName, Is.EqualTo("CorrectName"));
        Assert.That(tournament.Entrants.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task InvalidModalDoesNotRegisterOrModifyOtherState()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RegisterAndJoinTournamentAsync("missing", 9, "New", "NewName"));
        Assert.That(await coordinator.GetPlayerAsync(9), Is.Null);
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RegisterAndJoinTournamentAsync(tournament.Id, 9, "New", "Player2"));
        Assert.That(tournament.Entrants, Is.Empty);
        Assert.That(await coordinator.GetPlayerAsync(9), Is.Null);
    }

    [Test]
    public async Task ClosedTournamentModalDoesNotRenameOrRegister()
    {
        await store.UpdateAsync(s => s.Tournaments[tournament.Id].Status = TournamentStatus.Running);
        var before = File.ReadAllText(Path.Combine(directory, "state.json"));
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RegisterAndJoinTournamentAsync(tournament.Id, 1, "Changed", "Changed"));
        Assert.That(File.ReadAllText(Path.Combine(directory, "state.json")), Is.EqualTo(before));
        Assert.That((await coordinator.GetPlayerAsync(1))!.OpenRaName, Is.EqualTo("Player1"));
    }

    [Test]
    public async Task MembershipIsPerTournamentAndExplicitLeaveAllowsRejoin()
    {
        var other = await coordinator.CreateTournamentAsync("Other", TournamentFormat.SingleElimination);
        await coordinator.JoinTournamentAsync(tournament.Id, 1);
        await coordinator.JoinTournamentAsync(other.Id, 1);
        await coordinator.LeaveTournamentAsync(tournament.Id, 1);
        await coordinator.JoinTournamentAsync(tournament.Id, 1);
        Assert.That(tournament.Entrants.Count, Is.EqualTo(1)); Assert.That(other.Entrants.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task TeamsRejectDuplicateMembershipAndRepeatedAcceptance()
    {
        var teams = await coordinator.CreateTournamentAsync("Teams", TournamentFormat.SingleElimination, TournamentMode.TwoVsTwo);
        await coordinator.InviteTeamAsync(teams.Id, 1, 2, "One");
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.InviteTeamAsync(teams.Id, 3, 2, "Two"));
        await coordinator.RespondToTeamInviteAsync(teams.Id, 1, 2, true);
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RespondToTeamInviteAsync(teams.Id, 1, 2, true));
        Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.InviteTeamAsync(teams.Id, 2, 3, "Two"));
        Assert.That(teams.Entrants, Is.EqualTo(new ulong[] { 1 }));
    }
}
