namespace Ymca.TournamentBot;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configPath = args.Length > 0 ? args[0] : "tournament-bot.json";
        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };

        try
        {
            var config = BotConfiguration.Load(configPath);
            var store = new StateStore(config.StateFile);
            var replayReader = new ReplayMetadataReader(config.Server);
            await using var serverPool = new OpenRaServerPool(config.Server, replayReader);
            var coordinator = new TournamentCoordinator(config, store, serverPool);
            await using var joinPage = new JoinPageServer(config, coordinator);
            await using var discord = new DiscordTournamentBot(config, coordinator, joinPage);

            await joinPage.StartAsync(shutdown.Token);
            Console.WriteLine($"YMCA Tournament Bot starting with {config.Server.MaxConcurrentServers} server slots.");
            await discord.RunAsync(shutdown.Token);
            return 0;
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
