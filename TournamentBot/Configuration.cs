using System.Text.Json;

namespace Ymca.TournamentBot;

public sealed class BotConfiguration
{
    public string DiscordToken { get; set; } = "";
    public ulong GuildId { get; set; }
    public ulong AdminRoleId { get; set; }
    public ulong AdminChannelId { get; set; }
    public ulong AnnouncementChannelId { get; set; }
    public string StateFile { get; set; } = "TournamentBot/data/tournament-state.json";
    public OpenRaServerConfiguration Server { get; set; } = new();
    public JoinPageConfiguration JoinPage { get; set; } = new();

    public static BotConfiguration Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}");

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BotConfiguration>(json, JsonOptions.Default)
            ?? throw new InvalidDataException("The bot configuration is empty.");

        config.DiscordToken = Environment.GetEnvironmentVariable("YMCA_TOURNAMENT_DISCORD_TOKEN")
            ?? config.DiscordToken;

        if (string.IsNullOrWhiteSpace(config.DiscordToken))
            throw new InvalidDataException("DiscordToken or YMCA_TOURNAMENT_DISCORD_TOKEN must be configured.");
        if (config.GuildId == 0)
            throw new InvalidDataException("GuildId must be configured.");

        config.StateFile = Path.GetFullPath(config.StateFile);
        config.Server.NormalizeAndValidate();
        config.JoinPage.Validate();
        return config;
    }
}

public sealed class JoinPageConfiguration
{
    public bool Enabled { get; set; } = true;
    public string ListenUrl { get; set; } = "http://127.0.0.1:5080";
    public string PublicBaseUrl { get; set; } = "http://localhost:5080";

    public void Validate()
    {
        if (!Enabled)
            return;
        if (!Uri.TryCreate(ListenUrl, UriKind.Absolute, out _))
            throw new InvalidDataException("JoinPage.ListenUrl must be an absolute URL.");
        if (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out _))
            throw new InvalidDataException("JoinPage.PublicBaseUrl must be an absolute URL.");

        PublicBaseUrl = PublicBaseUrl.TrimEnd('/');
    }
}

public sealed class OpenRaServerConfiguration
{
    public int MaxConcurrentServers { get; set; } = 2;
    public int FirstPort { get; set; } = 1234;
    public string PublicHost { get; set; } = "localhost";
    public string WorkingDirectory { get; set; } = ".";
    public string ServerExecutable { get; set; } = "engine/bin/OpenRA.Server.exe";
    public string UtilityExecutable { get; set; } = "engine/bin/OpenRA.Utility.exe";
    public bool UseDotNetHost { get; set; }
    public string ModId { get; set; } = "ca";
    public string EngineDirectory { get; set; } = "..";
    public string ModSearchPaths { get; set; } = "mods,./mods";
    public string MatchDirectory { get; set; } = "TournamentBot/tournament/matches";
    public bool AdvertiseOnline { get; set; }
    public int StartupTimeoutSeconds { get; set; } = 45;
    public int ReplayPollSeconds { get; set; } = 3;

    public void NormalizeAndValidate()
    {
        if (MaxConcurrentServers < 1)
            throw new InvalidDataException("MaxConcurrentServers must be at least 1.");
        if (FirstPort is < 1 or > 65535 || FirstPort + MaxConcurrentServers - 1 > 65535)
            throw new InvalidDataException("The configured server port range is invalid.");
        if (string.IsNullOrWhiteSpace(PublicHost))
            throw new InvalidDataException("PublicHost must be configured.");

        WorkingDirectory = Path.GetFullPath(WorkingDirectory);
        MatchDirectory = Path.GetFullPath(Path.IsPathRooted(MatchDirectory)
            ? MatchDirectory
            : Path.Combine(WorkingDirectory, MatchDirectory));
        ServerExecutable = ResolveExecutable(ServerExecutable);
        UtilityExecutable = ResolveExecutable(UtilityExecutable);

        if (!File.Exists(ServerExecutable))
            throw new FileNotFoundException($"OpenRA server executable not found: {ServerExecutable}");
        if (!File.Exists(UtilityExecutable))
            throw new FileNotFoundException($"OpenRA utility executable not found: {UtilityExecutable}");
    }

    string ResolveExecutable(string path) => Path.GetFullPath(Path.IsPathRooted(path)
        ? path
        : Path.Combine(WorkingDirectory, path));
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}
