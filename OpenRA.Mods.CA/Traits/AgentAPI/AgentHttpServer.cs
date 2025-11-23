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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits.AgentAPI
{
	public class AgentHttpServer
	{
		readonly int port;
		AgentInterface agentInterface;
		World world;
		readonly IGameLauncher gameLauncher;
		HttpListener listener;
		Thread listenerThread;
		Player player;
		bool isRunning;

		public AgentHttpServer(int port, AgentInterface agentInterface, World world, IGameLauncher gameLauncher)
		{
			this.port = port;
			this.agentInterface = agentInterface;
			this.world = world;
			this.gameLauncher = gameLauncher;
		}

		/// <summary>
		/// Dynamically set or update the agent interface when an agent bot is activated
		/// </summary>
		public void SetAgentInterface(AgentInterface agent)
		{
			agentInterface = agent;
			if (agent != null)
			{
				player = agent.Player;
				world = agent.Player.World;
				Console.WriteLine($"[AgentHttpServer] Updated world reference for player {player.PlayerName}");
			}
			else
			{
				player = null;
			}
		}

		public void Start(Player player)
		{
			this.player = player;

			try
			{
				listener = new HttpListener();
				listener.Prefixes.Add($"http://localhost:{port}/");
				listener.Start();
				isRunning = true;

				listenerThread = new Thread(ListenLoop)
				{
					IsBackground = true,
					Name = "AgentHttpServer"
				};
				listenerThread.Start();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[AgentHttpServer] Failed to start HTTP server: {ex.Message}");
			}
		}

		public void Stop()
		{
			isRunning = false;
			listener?.Stop();
			listener?.Close();
		}

		void ListenLoop()
		{
			while (isRunning)
			{
				try
				{
					var context = listener.GetContext();
					ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
				}
				catch (HttpListenerException)
				{
					break;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[AgentHttpServer] Error in listener loop: {ex.Message}");
				}
			}
		}

		void HandleRequest(HttpListenerContext context)
		{
			try
			{
				var request = context.Request;
				var response = context.Response;

				response.AddHeader("Access-Control-Allow-Origin", "*");
				response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
				response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

				if (request.HttpMethod == "OPTIONS")
				{
					response.StatusCode = 200;
					response.Close();
					return;
				}

				var path = request.Url.AbsolutePath;

				if (path == "/api/state" && request.HttpMethod == "GET")
				{
					HandleGetState(response);
				}
				else if (path == "/api/orders" && request.HttpMethod == "POST")
				{
					HandlePostOrders(request, response);
				}
				else if (path == "/api/ping" && request.HttpMethod == "GET")
				{
					HandlePing(response);
				}
				else if (path == "/api/game/start" && request.HttpMethod == "POST")
				{
					HandleGameStart(request, response);
				}
				else if (path == "/api/game/stop" && request.HttpMethod == "POST")
				{
					HandleGameStop(response);
				}
				else if (path == "/api/game/status" && request.HttpMethod == "GET")
				{
					HandleGameStatus(response);
				}
				else if (path == "/api/game/kill" && request.HttpMethod == "POST")
				{
					HandleGameKill(response);
				}
				else
				{
					response.StatusCode = 404;
					WriteJson(response, new { error = "Endpoint not found" });
				}

				response.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[AgentHttpServer] Error handling request: {ex.Message}");
				try
				{
					context.Response.StatusCode = 500;
					WriteJson(context.Response, new { error = ex.Message });
					context.Response.Close();
				}
				catch { }
			}
		}

		void HandleGetState(HttpListenerResponse response)
		{
			if (agentInterface == null)
			{
				response.StatusCode = 409;
				WriteJson(response, new { error = "No agent bot is currently active. Start a game with an 'agent' bot first." });
				return;
			}

			var state = agentInterface.GetGameState();
			response.StatusCode = 200;
			WriteJson(response, state);
		}

		void HandlePostOrders(HttpListenerRequest request, HttpListenerResponse response)
		{
			if (agentInterface == null)
			{
				response.StatusCode = 409;
				WriteJson(response, new { error = "No agent bot is currently active. Start a game with an 'agent' bot first." });
				return;
			}

			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				var body = reader.ReadToEnd();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var orderRequest = JsonSerializer.Deserialize<OrderRequest>(body, options);

				if (orderRequest == null)
				{
					response.StatusCode = 400;
					WriteJson(response, new { error = "Invalid request body" });
					return;
				}

				var order = ParseOrder(orderRequest);
				if (order != null)
				{
					Game.RunAfterTick(() => agentInterface.QueueOrderFromExternal(order));
					response.StatusCode = 200;
					WriteJson(response, new { success = true, message = "Order queued" });
				}
				else
				{
					response.StatusCode = 400;
					WriteJson(response, new { error = "Failed to parse order" });
				}
			}
		}

		void HandlePing(HttpListenerResponse response)
		{
			response.StatusCode = 200;
			WriteJson(response, new
			{
				status = "running",
				tick = world?.WorldTick ?? 0,
				player = player?.PlayerName ?? "No agent active",
				agentActive = agentInterface != null
			});
		}

		Order ParseOrder(OrderRequest orderRequest)
		{
			try
			{
				var command = orderRequest.Command?.ToLowerInvariant();

				switch (command)
				{
					case "move":
						return ParseMoveOrder(orderRequest);

					case "attack":
						return ParseAttackOrder(orderRequest);

					case "stop":
						return ParseStopOrder(orderRequest);

					case "build":
						return ParseBuildOrder(orderRequest);

					default:
						Console.WriteLine($"[AgentHttpServer] Unknown command: {command}");
						return null;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[AgentHttpServer] Error parsing order: {ex.Message}");
				return null;
			}
		}

		Order ParseMoveOrder(OrderRequest orderRequest)
		{
		Console.WriteLine($"[ParseMoveOrder] Called with Units: {orderRequest.Units?.Length ?? 0}, TargetCell: {orderRequest.TargetCell?.X},{orderRequest.TargetCell?.Y}");
			if (orderRequest.Units == null || orderRequest.Units.Length == 0)
				return null;

			if (orderRequest.TargetCell == null)
				return null;

			var cell = new CPos(orderRequest.TargetCell.X, orderRequest.TargetCell.Y);
			var actors = GetActorsByIds(orderRequest.Units);

			if (actors.Count == 0)
				return null;

			// For simplicity, we'll create an order for the first actor
			// In a production system, you'd want to handle multiple units
			var actor = actors.First();
			return new Order("Move", actor, Target.FromCell(world, cell), orderRequest.Queued);
		}

		Order ParseAttackOrder(OrderRequest orderRequest)
		{
			if (orderRequest.Units == null || orderRequest.Units.Length == 0)
				return null;

			var actors = GetActorsByIds(orderRequest.Units);
			if (actors.Count == 0)
				return null;

			var actor = actors.First();

			if (orderRequest.TargetId.HasValue)
			{
				var targetActor = world.GetActorById(orderRequest.TargetId.Value);
				if (targetActor != null)
					return new Order("Attack", actor, Target.FromActor(targetActor), orderRequest.Queued);
			}

			if (orderRequest.TargetCell != null)
			{
				var cell = new CPos(orderRequest.TargetCell.X, orderRequest.TargetCell.Y);
				return new Order("AttackMove", actor, Target.FromCell(world, cell), orderRequest.Queued);
			}

			return null;
		}

		Order ParseStopOrder(OrderRequest orderRequest)
		{
			if (orderRequest.Units == null || orderRequest.Units.Length == 0)
				return null;

			var actors = GetActorsByIds(orderRequest.Units);
			if (actors.Count == 0)
				return null;

			var actor = actors.First();
			return new Order("Stop", actor, orderRequest.Queued);
		}

		Order ParseBuildOrder(OrderRequest orderRequest)
		{
			Console.WriteLine($"[ParseBuildOrder] Called with BuildingType: {orderRequest.BuildingType ?? "null"}");

			if (string.IsNullOrEmpty(orderRequest.BuildingType))
			{
				Console.WriteLine($"[ParseBuildOrder] BuildingType is null or empty");
				return null;
			}

			var allActors = world.Actors.Where(a => a.Owner == player).ToList();
			Console.WriteLine($"[ParseBuildOrder] Player has {allActors.Count} actors");

			var producers = allActors.Where(a => a.Info.HasTraitInfo<ProductionInfo>()).ToList();
			Console.WriteLine($"[ParseBuildOrder] Found {producers.Count} producers");

			foreach (var p in producers)
			{
				Console.WriteLine($"[ParseBuildOrder] Producer: {p.Info.Name} (ID: {p.ActorID})");
			}

			var producer = producers.FirstOrDefault();
			if (producer == null)
			{
				Console.WriteLine($"[ParseBuildOrder] No producer found");
				return null;
			}

			Console.WriteLine($"[ParseBuildOrder] Using producer {producer.Info.Name} to build {orderRequest.BuildingType}");
			return Order.StartProduction(producer, orderRequest.BuildingType, 1);
		}

		List<Actor> GetActorsByIds(uint[] ids)
		{
		Console.WriteLine($"[GetActorsByIds] Called with {ids.Length} IDs: {string.Join(",", ids)}");
		Console.WriteLine($"[GetActorsByIds] Current player: {player?.PlayerName ?? "null"}");
		Console.WriteLine($"[GetActorsByIds] Total actors in world: {world.Actors.Count()}");

		var playerActors = world.Actors.Where(a => a.Owner == player && !a.IsDead).ToList();
		Console.WriteLine($"[GetActorsByIds] Player has {playerActors.Count} actors");
		foreach (var pa in playerActors.Take(5))
		{
			Console.WriteLine($"[GetActorsByIds]   Actor ID {pa.ActorID}: {pa.Info.Name}");
		}
			var actors = new List<Actor>();

			foreach (var id in ids)
			{
				var actor = world.Actors.FirstOrDefault(a => a.ActorID == id);
				Console.WriteLine($"[GetActorsByIds] Looking for ID {id}: actor={(actor != null ? "found" : "null")}, owner={(actor?.Owner?.PlayerName ?? "null")}, matches={(actor != null && actor.Owner == player ? "YES" : "NO")}");
				if (actor != null && actor.Owner == player && !actor.IsDead)
					actors.Add(actor);
			}

		Console.WriteLine($"[GetActorsByIds] Returning {actors.Count} actors");
			return actors;
		}

		void HandleGameStart(HttpListenerRequest request, HttpListenerResponse response)
		{
			try
			{
				using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
				{
					var body = reader.ReadToEnd();
					Console.WriteLine($"[AgentHttpServer] Received game start request: {body}");

					var options = new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					};
					var config = JsonSerializer.Deserialize<GameStartConfig>(body, options);

					if (config == null)
					{
						response.StatusCode = 400;
						WriteJson(response, new { error = "Invalid request body" });
						return;
					}

					var result = gameLauncher.StartGame(config);

					response.StatusCode = 200;
					WriteJson(response, result);
				}
			}
			catch (ArgumentException ex)
			{
				// Validation error - 400 Bad Request
				response.StatusCode = 400;
				WriteJson(response, new { error = ex.Message });
			}
			catch (InvalidOperationException ex)
			{
				// Conflict (e.g., game already running) - 409 Conflict
				response.StatusCode = 409;
				WriteJson(response, new { error = ex.Message });
			}
			catch (Exception ex)
			{
				// Internal error - 500
				response.StatusCode = 500;
				WriteJson(response, new { error = "Internal server error", details = ex.Message });
				Console.WriteLine($"[AgentHttpServer] Error starting game: {ex}");
			}
		}

		void HandleGameStop(HttpListenerResponse response)
		{
			try
			{
				gameLauncher.StopGame();
				response.StatusCode = 200;
				WriteJson(response, new { success = true, message = "Game stopping" });
			}
			catch (Exception ex)
			{
				response.StatusCode = 500;
				WriteJson(response, new { error = "Failed to stop game", details = ex.Message });
				Console.WriteLine($"[AgentHttpServer] Error stopping game: {ex}");
			}
		}

		void HandleGameStatus(HttpListenerResponse response)
		{
			try
			{
				var status = gameLauncher.GetStatus();
				response.StatusCode = 200;
				WriteJson(response, status);
			}
			catch (Exception ex)
			{
				response.StatusCode = 500;
				WriteJson(response, new { error = "Failed to get status", details = ex.Message });
				Console.WriteLine($"[AgentHttpServer] Error getting game status: {ex}");
			}
		}

		void HandleGameKill(HttpListenerResponse response)
		{
			try
			{
				Console.WriteLine($"[AgentHttpServer] Received kill request, shutting down...");
				response.StatusCode = 200;
				WriteJson(response, new { success = true, message = "Game shutting down" });

				// Exit immediately - don't wait for response to close
				Console.WriteLine($"[AgentHttpServer] Exiting game now...");
				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				response.StatusCode = 500;
				WriteJson(response, new { error = "Failed to kill game", details = ex.Message });
				Console.WriteLine($"[AgentHttpServer] Error killing game: {ex}");
			}
		}

		void WriteJson(HttpListenerResponse response, object data)
		{
			response.ContentType = "application/json";
			var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});
			var buffer = Encoding.UTF8.GetBytes(json);
			response.ContentLength64 = buffer.Length;
			response.OutputStream.Write(buffer, 0, buffer.Length);
		}
	}

	// Request models
	public class OrderRequest
	{
		public string Command { get; set; }
		public uint[] Units { get; set; }
		public CellRequest TargetCell { get; set; }
		public uint? TargetId { get; set; }
		public bool Queued { get; set; }
		public string BuildingType { get; set; }
	}

	public class CellRequest
	{
		public int X { get; set; }
		public int Y { get; set; }
	}
}
