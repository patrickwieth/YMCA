using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace Ymca.TournamentBot;

public sealed class OpenRaServerPool : IAsyncDisposable
{
    readonly OpenRaServerConfiguration config;
    readonly ReplayMetadataReader replayReader;
    readonly Channel<MatchRecord> queue = Channel.CreateUnbounded<MatchRecord>();
    readonly CancellationTokenSource shutdown = new();
    readonly List<Task> workers = new();

    public event Func<MatchRecord, Task>? ServerStarting;
    public event Func<MatchRecord, Task>? ServerReady;
    public event Func<MatchRecord, ReplayResult, Task>? ResultAvailable;
    public event Func<MatchRecord, string, Task>? ServerFailed;

    public OpenRaServerPool(OpenRaServerConfiguration config, ReplayMetadataReader replayReader)
    {
        this.config = config;
        this.replayReader = replayReader;
    }

    public void Start()
    {
        if (workers.Count != 0)
            return;

        Directory.CreateDirectory(config.MatchDirectory);
        for (var i = 0; i < config.MaxConcurrentServers; i++)
        {
            var port = config.FirstPort + i;
            workers.Add(Task.Run(() => WorkerAsync(port, shutdown.Token)));
        }
    }

    public ValueTask EnqueueAsync(MatchRecord match, CancellationToken cancellationToken = default) =>
        queue.Writer.WriteAsync(match, cancellationToken);

    async Task WorkerAsync(int port, CancellationToken cancellationToken)
    {
        await foreach (var match in queue.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await RunMatchAsync(match, port, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                await InvokeAsync(ServerFailed, match, ex.Message);
            }
        }
    }

    async Task RunMatchAsync(MatchRecord match, int port, CancellationToken cancellationToken)
    {
        match.Port = port;
        match.Password = CreatePassword();
        match.SupportDirectory = Path.Combine(config.MatchDirectory, match.Id);
        Directory.CreateDirectory(match.SupportDirectory);

        var startInfo = CreateServerStartInfo(match);
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("OpenRA.Server could not be started.");
        await InvokeAsync(ServerStarting, match);

        var stdoutPath = Path.Combine(match.SupportDirectory, "process-output.log");
        var stderrPath = Path.Combine(match.SupportDirectory, "process-error.log");
        await using var stdout = new FileStream(stdoutPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using var stderr = new FileStream(stderrPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        var copyOutput = process.StandardOutput.BaseStream.CopyToAsync(stdout, cancellationToken);
        var copyError = process.StandardError.BaseStream.CopyToAsync(stderr, cancellationToken);

        try
        {
            await WaitUntilListeningAsync(process, port, cancellationToken);
            await VerifyInitialMapAsync(process, match, cancellationToken);
            await InvokeAsync(ServerReady, match);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (process.HasExited)
                    throw new InvalidOperationException($"OpenRA.Server exited with code {process.ExitCode}.");

                foreach (var replay in Directory.EnumerateFiles(match.SupportDirectory, "*.orarep", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc))
                {
                    var result = await replayReader.TryReadAsync(replay, cancellationToken);
                    if (result == null)
                        continue;

                    match.ReplayPath = replay;
                    await InvokeAsync(ResultAvailable, match, result);
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(config.ReplayPollSeconds), cancellationToken);
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            await Task.WhenAll(IgnoreCancellation(copyOutput), IgnoreCancellation(copyError));
        }
    }

    ProcessStartInfo CreateServerStartInfo(MatchRecord match)
    {
        var executable = config.UseDotNetHost ? "dotnet" : config.ServerExecutable;
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = ReplayMetadataReader.GetEngineWorkingDirectory(config.ServerExecutable),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (config.UseDotNetHost)
            info.ArgumentList.Add(config.ServerExecutable);

        AddArgument(info, "Game.Mod", config.ModId);
        AddArgument(info, "Engine.EngineDir", config.EngineDirectory);
        AddArgument(info, "Engine.SupportDir", match.SupportDirectory);
        AddArgument(info, "Server.Name", $"YMCA Tournament {match.Id}");
        AddArgument(info, "Server.ListenPort", match.Port!.Value.ToString());
        AddArgument(info, "Server.AdvertiseOnline", config.AdvertiseOnline.ToString());
        AddArgument(info, "Server.Password", match.Password);
        AddArgument(info, "Server.AllowedPlayerNames", $"{match.PlayerOneOpenRaName},{match.PlayerTwoOpenRaName}");
        AddArgument(info, "Server.AutoStartDelaySeconds", "5");
        AddArgument(info, "Server.AutoAssignCompetitiveSpawns", "True");
        AddArgument(info, "Server.LobbyStatusFile", Path.Combine(match.SupportDirectory, "lobby-status.json"));
        AddArgument(info, "Server.RecordReplays", "True");
        AddArgument(info, "Server.RequireAuthentication", "False");
        AddArgument(info, "Server.Map", match.MapUid);
        info.Environment["MOD_SEARCH_PATHS"] = string.Join(',', config.ModSearchPaths.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(config.WorkingDirectory, path.Trim()))));
        return info;
    }

    static void AddArgument(ProcessStartInfo info, string key, string value) =>
        info.ArgumentList.Add($"{key}={value}");

    async Task WaitUntilListeningAsync(Process process, int port, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(config.StartupTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"OpenRA.Server exited during startup with code {process.ExitCode}.");

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port, cancellationToken);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        throw new TimeoutException($"OpenRA.Server did not listen on port {port} within {config.StartupTimeoutSeconds} seconds.");
    }

    async Task VerifyInitialMapAsync(Process process, MatchRecord match, CancellationToken cancellationToken)
    {
        var logPath = Path.Combine(match.SupportDirectory, "Logs", "dedicated-server.log");
        var deadline = DateTime.UtcNow.AddSeconds(config.StartupTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"OpenRA.Server exited during map validation with code {process.ExitCode}.");

            if (File.Exists(logPath))
            {
                string log;
                using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                    log = await reader.ReadToEndAsync();

                var initialMap = log.Split('\n')
                    .Select(line => line.Trim())
                    .FirstOrDefault(line => line.Contains("Initial map:", StringComparison.Ordinal));
                if (initialMap != null)
                {
                    if (!initialMap.EndsWith(match.MapUid, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"OpenRA selected a different map than requested: {initialMap}");
                    return;
                }
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException("OpenRA.Server did not report its initial map.");
    }

    static string CreatePassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    static async Task InvokeAsync(Func<MatchRecord, Task>? callback, MatchRecord match)
    {
        if (callback != null)
            await callback(match);
    }

    static async Task InvokeAsync(Func<MatchRecord, string, Task>? callback, MatchRecord match, string message)
    {
        if (callback != null)
            await callback(match, message);
    }

    static async Task InvokeAsync(Func<MatchRecord, ReplayResult, Task>? callback, MatchRecord match, ReplayResult result)
    {
        if (callback != null)
            await callback(match, result);
    }

    static async Task IgnoreCancellation(Task task)
    {
        try { await task; }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        queue.Writer.TryComplete();
        shutdown.Cancel();
        try { await Task.WhenAll(workers); }
        catch (OperationCanceledException) { }
        shutdown.Dispose();
    }
}
