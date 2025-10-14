#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.AgentAPI
{
	/// <summary>
	/// Global HTTP server that starts with the engine and provides API access
	/// even when no game is running (shell map state).
	/// </summary>
	[Desc("Global HTTP server for agent API access")]
	[TraitLocation(SystemActors.World)]
	public class GlobalAgentServerInfo : TraitInfo
	{
		[Desc("HTTP port for the agent API.")]
		public readonly int Port = 8080;

		[Desc("Enable the agent HTTP server. Can be overridden by Server.AgentAPI=true command line parameter.")]
		public readonly bool Enabled = false;

		public override object Create(ActorInitializer init)
		{
			return new GlobalAgentServer(this);
		}
	}

	public class GlobalAgentServer : INotifyCreated
	{
		static GlobalAgentServer instance;
		static readonly object lockObj = new object();

		readonly GlobalAgentServerInfo info;
		AgentHttpServer httpServer;
		IGameLauncher gameLauncher;
		AgentInterface currentAgent;
		World world;

		public GlobalAgentServer(GlobalAgentServerInfo info)
		{
			this.info = info;
		}

		static bool CheckCommandLineParameter()
		{
			var args = Environment.GetCommandLineArgs();
			foreach (var arg in args)
			{
				if (arg == "Server.AgentAPI=true")
					return true;
			}

			return false;
		}

		void INotifyCreated.Created(Actor self)
		{
			var enabledViaCommandLine = CheckCommandLineParameter();
			if (!info.Enabled && !enabledViaCommandLine)
				return;

			if (enabledViaCommandLine)
				Console.WriteLine("[GlobalAgentServer] Enabled via command line parameter");

			lock (lockObj)
			{
				world = self.World;

				if (instance == null)
				{
					Console.WriteLine($"[GlobalAgentServer] Initializing global agent server");

					gameLauncher = new OpenRAGameLauncher(world, Game.ModData);

					httpServer = new AgentHttpServer(info.Port, null, world, gameLauncher);
					httpServer.Start(null); // No player yet, will be set when agent activates

					Console.WriteLine($"[GlobalAgentServer] HTTP API listening on http://localhost:{info.Port}");
					Console.WriteLine($"[GlobalAgentServer] Available endpoints:");
					Console.WriteLine($"[GlobalAgentServer]   GET  /api/ping        - Check if server is running");
					Console.WriteLine($"[GlobalAgentServer]   POST /api/game/start  - Start a new game");
					Console.WriteLine($"[GlobalAgentServer]   POST /api/game/stop   - Stop current game");
					Console.WriteLine($"[GlobalAgentServer]   POST /api/game/kill   - Kill/exit the game process");
					Console.WriteLine($"[GlobalAgentServer]   GET  /api/game/status - Get game status");
					Console.WriteLine($"[GlobalAgentServer]   GET  /api/state       - Get current game state (requires agent bot)");
					Console.WriteLine($"[GlobalAgentServer]   POST /api/orders      - Issue orders to units (requires agent bot)");

					instance = this;
				}
				else
				{
					// World reference must be updated because Shell Map world differs from active game world
					Console.WriteLine($"[GlobalAgentServer] Reusing existing server instance for new world");
					instance.world = world;
					instance.gameLauncher = new OpenRAGameLauncher(world, Game.ModData);

					// Note: httpServer field will be null for non-primary instances
					httpServer = instance.httpServer;
				}
			}
		}

		/// <summary>
		/// Called by AgentInterface when an agent bot is activated
		/// </summary>
		public void RegisterAgent(AgentInterface agent)
		{
			lock (lockObj)
			{
				if (instance != null)
				{
					instance.currentAgent = agent;
					if (instance.httpServer != null)
						instance.httpServer.SetAgentInterface(agent);

					Console.WriteLine($"[GlobalAgentServer] Agent interface registered for player {agent.Player?.PlayerName}");
				}
			}
		}

		/// <summary>
		/// Called by AgentInterface when an agent bot is deactivated
		/// </summary>
		public void UnregisterAgent(AgentInterface agent)
		{
			lock (lockObj)
			{
				if (instance != null && instance.currentAgent == agent)
				{
					instance.currentAgent = null;
					if (instance.httpServer != null)
						instance.httpServer.SetAgentInterface(null);

					Console.WriteLine($"[GlobalAgentServer] Agent interface unregistered");
				}
			}
		}
	}
}
