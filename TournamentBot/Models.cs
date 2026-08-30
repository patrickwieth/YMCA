namespace Ymca.TournamentBot;

public enum MatchStatus
{
    Queued,
    StartingServer,
    WaitingForPlayers,
    Playing,
    AwaitingConfirmation,
    Completed,
    RematchRequested,
    Disputed,
    Failed,
    Cancelled
}

public enum PlayerReport
{
    Won,
    Lost,
    Rematch,
    Dispute
}

public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination
}

public enum TournamentStatus
{
    Registration,
    Running,
    Completed,
    Cancelled
}

public sealed class RegisteredPlayer
{
    public ulong DiscordUserId { get; set; }
    public string DiscordDisplayName { get; set; } = "";
    public string OpenRaName { get; set; } = "";
    public DateTime RegisteredAtUtc { get; set; }
}

public sealed class MatchRecord
{
    public string Id { get; set; } = "";
    public ulong PlayerOneDiscordId { get; set; }
    public ulong PlayerTwoDiscordId { get; set; }
    public string PlayerOneOpenRaName { get; set; } = "";
    public string PlayerTwoOpenRaName { get; set; } = "";
    public string MapUid { get; set; } = "";
    public string MapTitle { get; set; } = "";
    public MatchStatus Status { get; set; }
    public int? Port { get; set; }
    public string Password { get; set; } = "";
    public string SupportDirectory { get; set; } = "";
    public string? ReplayPath { get; set; }
    public ulong? AutomaticWinnerDiscordId { get; set; }
    public ulong? FinalWinnerDiscordId { get; set; }
    public Dictionary<ulong, PlayerReport> PlayerReports { get; set; } = new();
    public string? FailureReason { get; set; }
    public string? ParentMatchId { get; set; }
    public string? TournamentId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}

public sealed class TournamentRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public TournamentFormat Format { get; set; }
    public TournamentStatus Status { get; set; }
    public string MapUid { get; set; } = "";
    public string MapTitle { get; set; } = "";
    public List<ulong> Entrants { get; set; } = new();
    public Dictionary<ulong, int> Losses { get; set; } = new();
    public List<string> MatchIds { get; set; } = new();
    public HashSet<string> ProcessedMatchIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public ulong? ChampionDiscordId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}

public sealed class TournamentState
{
    public Dictionary<ulong, RegisteredPlayer> Players { get; set; } = new();
    public Dictionary<string, MatchRecord> Matches { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, TournamentRecord> Tournaments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int NextMatchNumber { get; set; } = 1;
    public int NextTournamentNumber { get; set; } = 1;
}

public sealed record ReplayPlayerResult(string Name, string Outcome, bool IsHuman);

public sealed record ReplayResult(
    string ReplayPath,
    string MapTitle,
    string Version,
    IReadOnlyList<ReplayPlayerResult> Players);
