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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;

namespace OpenRA.Mods.CA.Traits.AgentAPI
{
	/// <summary>
	/// Interface for game lifecycle management
	/// </summary>
	public interface IGameLauncher
	{
		/// <summary>Start a new game with the given configuration</summary>
		GameStartResult StartGame(GameStartConfig config);

		/// <summary>Stop the current game</summary>
		void StopGame();

		/// <summary>Get current game status</summary>
		GameStatusInfo GetStatus();
	}

	/// <summary>
	/// OpenRA game launcher implementation
	/// </summary>
	public class OpenRAGameLauncher : IGameLauncher
	{
		readonly World world;
		readonly ModData modData;
		GameStatus currentStatus;
		string currentMapUid;

		public OpenRAGameLauncher(World world, ModData modData)
		{
			this.world = world;
			this.modData = modData;
			currentStatus = GameStatus.NotStarted;
		}

		public GameStartResult StartGame(GameStartConfig config)
		{
			try
			{
				// 1. Validate configuration
				config.Validate();
				ValidateMap(config.MapUid);
				ValidateBots(config.Bots);
				ValidateGameSpeed(config.GameSpeed);

				// 2. Check if game is already running
				if (currentStatus == GameStatus.Running || currentStatus == GameStatus.Starting)
				{
					throw new InvalidOperationException(
						$"Cannot start game: current status is {currentStatus}. Stop the current game first.");
				}

				// 3. Build setup orders
				var orders = BuildSetupOrders(config);

				// 4. Start the game
				currentStatus = GameStatus.Starting;
				currentMapUid = config.MapUid;

				Console.WriteLine($"[GameLauncher] Starting game on map '{config.MapUid}' with {config.Bots.Count} bots");

				// Queue the game start in the UI thread
				// We use Game.RunAfterTick to ensure we're in the game loop
				Game.RunAfterTick(() =>
				{
					try
					{
						Console.WriteLine($"[GameLauncher] Executing game start in UI thread");

						// Find the map in the cache (this is important!)
						var map = modData.MapCache.FirstOrDefault(m => m.Uid == config.MapUid);
						if (map == null)
						{
							// Fallback: try by filename
							map = modData.MapCache.FirstOrDefault(m =>
								System.IO.Path.GetFileNameWithoutExtension(m.Package.Name) == config.MapUid);
						}

						if (map == null)
						{
							throw new InvalidOperationException($"Map not found in cache: {config.MapUid}");
						}

						Console.WriteLine($"[GameLauncher] Found map in cache: {map.Title}");

						OrderManager orderManager = null;

						// Register lobby ready handler BEFORE creating server
						Action lobbyReady = null;
						lobbyReady = () =>
						{
							Game.LobbyInfoChanged -= lobbyReady;
							Console.WriteLine($"[GameLauncher] Lobby ready, sending {orders.Count} setup orders");

							foreach (var order in orders)
							{
								Console.WriteLine($"[GameLauncher] Issuing order: {order.OrderString}");
								orderManager.IssueOrder(order);
							}

							Game.RunAfterDelay(2000, () =>
							{
								currentStatus = GameStatus.Running;
								Console.WriteLine($"[GameLauncher] Game started successfully");
							});
						};

						Game.LobbyInfoChanged += lobbyReady;

						Console.WriteLine($"[GameLauncher] Creating local server for map: {map.Uid}");
						var endpoint = Game.CreateLocalServer(map.Uid);

						Console.WriteLine($"[GameLauncher] Joining server at {endpoint}");
						orderManager = Game.JoinServer(endpoint, "");

						Console.WriteLine($"[GameLauncher] Waiting for lobby to be ready...");
					}
					catch (Exception ex)
					{
						currentStatus = GameStatus.Stopped;
						Console.WriteLine($"[GameLauncher] Failed to start game: {ex}");
						Console.WriteLine($"[GameLauncher] Stack trace: {ex.StackTrace}");
					}
				});

				return new GameStartResult
				{
					Success = true,
					Message = "Game starting",
					MapUid = config.MapUid
				};
			}
			catch (ArgumentException ex)
			{
				Console.WriteLine($"[GameLauncher] Validation error: {ex.Message}");
				throw;
			}
			catch (InvalidOperationException ex)
			{
				Console.WriteLine($"[GameLauncher] Operation error: {ex.Message}");
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[GameLauncher] Unexpected error: {ex.Message}");
				currentStatus = GameStatus.Stopped;
				throw new InvalidOperationException($"Failed to start game: {ex.Message}", ex);
			}
		}

		public void StopGame()
		{
			if (currentStatus == GameStatus.NotStarted || currentStatus == GameStatus.Stopped)
			{
				Console.WriteLine($"[GameLauncher] No game to stop (status: {currentStatus})");
				return;
			}

			Console.WriteLine($"[GameLauncher] Stopping game");
			currentStatus = GameStatus.Stopping;

			Game.RunAfterTick(() =>
			{
				Game.Disconnect();
				currentStatus = GameStatus.Stopped;
				currentMapUid = null;
				Console.WriteLine($"[GameLauncher] Game stopped");
			});
		}

		public GameStatusInfo GetStatus()
		{
			var info = new GameStatusInfo
			{
				Status = currentStatus,
				MapUid = currentMapUid,
				Tick = world?.WorldTick,
				PlayerCount = world?.Players.Count(p => p.Playable) ?? 0
			};

			return info;
		}

		List<Order> BuildSetupOrders(GameStartConfig config)
		{
			var orders = new List<Order>();

			// IMPORTANT: First become a spectator so we don't occupy a player slot
			// This allows bots to take the player slots
			orders.Add(Order.Command("spectate"));
			Console.WriteLine($"[GameLauncher] Becoming spectator to allow bot slots");

			orders.Add(Order.Command($"option gamespeed {config.GameSpeed ?? "default"}"));

			foreach (var botEntry in config.Bots)
			{
				var bot = botEntry.Value;

				// slot_bot <slot> <controller_client_id> <bot_type>
				// controller_client_id should be 0 (local player)
				var slotBotCmd = $"slot_bot {bot.Slot} 0 {bot.BotType}";
				orders.Add(Order.Command(slotBotCmd));
				Console.WriteLine($"[GameLauncher] Adding bot: {slotBotCmd}");

				if (!string.IsNullOrEmpty(bot.Faction))
				{
					var factionCmd = $"slot_faction {bot.Slot} {bot.Faction}";
					orders.Add(Order.Command(factionCmd));
					Console.WriteLine($"[GameLauncher] Setting faction: {factionCmd}");
				}

				if (bot.Team.HasValue)
				{
					var teamCmd = $"slot_team {bot.Slot} {bot.Team.Value}";
					orders.Add(Order.Command(teamCmd));
					Console.WriteLine($"[GameLauncher] Setting team: {teamCmd}");
				}
			}

			orders.Add(Order.Command($"state {Session.ClientState.Ready}"));

			orders.Add(Order.Command("startgame"));

			Console.WriteLine($"[GameLauncher] Built {orders.Count} setup orders");
			return orders;
		}

		void ValidateMap(string mapUid)
		{
			if (string.IsNullOrEmpty(mapUid))
				throw new ArgumentException("MapUid cannot be empty");

			var map = modData.MapCache.FirstOrDefault(m =>
				m.Uid == mapUid ||
				System.IO.Path.GetFileNameWithoutExtension(m.Package.Name) == mapUid);

			if (map == null)
			{
				// List available maps for debugging
				var availableMaps = modData.MapCache.Take(10).Select(m => m.Uid).ToList();
				var mapList = string.Join(", ", availableMaps);
				throw new ArgumentException(
					$"Map '{mapUid}' not found. Available maps (first 10): {mapList}");
			}

			Console.WriteLine($"[GameLauncher] Validated map: {map.Title} ({map.Uid})");
		}

		void ValidateBots(Dictionary<string, BotConfig> bots)
		{
			if (bots == null || bots.Count == 0)
			{
				Console.WriteLine($"[GameLauncher] Warning: No bots configured");
				return;
			}

			// Valid bot types from mod configuration
			var validBotTypes = new[] { "agent", "normal", "easy", "hard", "brutal", "naval" };

			foreach (var botEntry in bots)
			{
				var bot = botEntry.Value;

				if (!validBotTypes.Contains(bot.BotType.ToLowerInvariant()))
				{
					throw new ArgumentException(
						$"Invalid bot type '{bot.BotType}'. Valid types: {string.Join(", ", validBotTypes)}");
				}

				if (!bot.Slot.StartsWith("Multi"))
				{
					Console.WriteLine($"[GameLauncher] Warning: Slot '{bot.Slot}' doesn't follow 'MultiN' pattern");
				}
			}

			Console.WriteLine($"[GameLauncher] Validated {bots.Count} bot configurations");
		}

		void ValidateGameSpeed(string gameSpeed)
		{
			if (string.IsNullOrEmpty(gameSpeed))
				return;

			var validSpeeds = new[] { "slowest", "slower", "default", "fast", "faster", "fastest" };
			if (!validSpeeds.Contains(gameSpeed.ToLowerInvariant()))
			{
				throw new ArgumentException(
					$"Invalid game speed '{gameSpeed}'. Valid speeds: {string.Join(", ", validSpeeds)}");
			}
		}
	}
}
