using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Network;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.CA.Server
{
	// No repository upload or arbitrary client-supplied map/rules. Server approves a bounded recipe.
	public sealed class GeneratedMapServer : ServerTrait, INotifyServerStart, IInterpretCommand, INotifySyncLobbyInfo, INotifyServerShutdown, ITick
	{
		Task<MapPlan> pending;
		MapGenerationOptions options;
		Connection requester;
		string previousMap;
		string recipe;
		readonly Stopwatch cooldown = new();
		readonly Dictionary<string, string> recipes = new();
		readonly TemporaryGeneratedMapFiles files = new();

		void INotifyServerStart.ServerStarted(S server)
		{
			if (server.Type == ServerType.Dedicated)
				server.LobbyInfo.GlobalSettings.LobbyOptions[GeneratedMapRecipe.Capability] =
					new Session.LobbyOptionState { Value = server.Settings.MapPool.Length == 0 ? GeneratedMapRecipe.Version : "map-pool", IsLocked = true };
		}

		void INotifyServerShutdown.ServerShutdown(S server)
		{
			foreach (var uid in recipes.Keys) server.ModData.MapCache[uid].DisposePackage();
			files.Cleanup(message => Log.Write("server", message));
		}

		void INotifySyncLobbyInfo.LobbyInfoSynced(S server)
		{
			var settings = server.LobbyInfo.GlobalSettings;
			if (server.State != ServerState.WaitingPlayers || !recipes.TryGetValue(settings.Map, out var selectedRecipe)) return;
			var value = settings.Map + ":" + selectedRecipe;
			if (settings.LobbyOptions.TryGetValue(GeneratedMapRecipe.Selection, out var current) && current.Value == value) return;
			settings.LobbyOptions[GeneratedMapRecipe.Selection] = new Session.LobbyOptionState { Value = value, IsLocked = true };
			server.SyncLobbyGlobalSettings();
		}

		static void Message(S server, Connection conn, string text) => server.SendOrderTo(conn, "Message", text);

		bool IInterpretCommand.InterpretCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (!cmd.StartsWith(GeneratedMapRecipe.Command, StringComparison.Ordinal)) return false;
			if (server.Type != ServerType.Dedicated || server.State != ServerState.WaitingPlayers || client == null || !client.IsAdmin)
			{
				Message(server, conn, "Only the lobby administrator can generate a map before the match starts.");
				return true;
			}

			if (client.IsReady || server.Settings.MapPool.Length != 0)
			{
				Message(server, conn, "Map generation is unavailable while ready or when the server enforces a map pool.");
				return true;
			}

			if (pending != null || cooldown.IsRunning && cooldown.Elapsed.TotalSeconds < 10)
			{
				Message(server, conn, "Please wait before generating another map.");
				return true;
			}

			cooldown.Restart();
			try
			{
				var parts = cmd.Substring(GeneratedMapRecipe.Command.Length).Split(' ');
				if (parts.Length != 2 || parts[0] != server.LobbyInfo.GlobalSettings.Map)
					throw new ArgumentException("The selected map changed. Reopen Change Map and try again.");
				options = GeneratedMapRecipe.Decode(parts[1]);
				recipe = GeneratedMapRecipe.Encode(options);
				previousMap = parts[0];
				requester = conn;
				pending = Task.Run(() => MapPlanGenerator.Generate(options));
				server.SendMessage("Generating a random map. The current lobby stays open until it is ready.");
			}
			catch (Exception e)
			{
				Message(server, conn, "Map generation rejected: " + e.Message);
				Log.Write("server", e);
			}

			return true;
		}

		void ITick.Tick(S server)
		{
			if (pending == null || !pending.IsCompleted) return;
			var completed = pending;
			pending = null;
			try
			{
				// Check again after asynchronous planning: no stale map/host changes, no changes to a running match.
				var plan = completed.GetAwaiter().GetResult();
				if (server.State != ServerState.WaitingPlayers || !server.Conns.Contains(requester) ||
					server.GetClient(requester)?.IsAdmin != true || server.GetClient(requester).IsReady ||
					server.LobbyInfo.GlobalSettings.Map != previousMap)
					return;
				var uid = GeneratedMapInstallation.Install(server.ModData, plan, options, clientCleanup: false);
				files.Register(server.ModData.MapCache[uid].PackageName);
				// Persist with lobby state so existing clients and late joiners reproduce exactly this map.
				recipes[uid] = recipe;
				// QueryMapRepository=false creates an automatic local pool. Extend that
				// pool with the server's own generated map; never bypass an explicit pool.
				if (server.MapPool != null && server.MapPool.Add(uid))
					server.DispatchServerOrdersToClients(Order.FromTargetString("SyncMapPool", FieldSaver.FormatValue(server.MapPool), true));
				var state = new Session.LobbyOptionState { Value = uid + ":" + recipe, IsLocked = true };
				server.LobbyInfo.GlobalSettings.LobbyOptions[GeneratedMapRecipe.Selection] = state;
				server.InterpretCommand("map " + uid, requester);
			}
			catch (Exception e)
			{
				Log.Write("server", e);
				if (server.Conns.Contains(requester)) Message(server, requester, "Map generation failed: " + e.Message);
			}
		}
	}
}
