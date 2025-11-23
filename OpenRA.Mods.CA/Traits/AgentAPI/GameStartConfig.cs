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

namespace OpenRA.Mods.CA.Traits.AgentAPI
{
	/// <summary>
	/// Configuration for starting a new game via API
	/// </summary>
	public class GameStartConfig
	{
		/// <summary>Map UID or filename to load</summary>
		public string MapUid { get; set; }

		/// <summary>Game speed: slowest, slower, default, fast, faster, fastest</summary>
		public string GameSpeed { get; set; }

		/// <summary>Bot configurations keyed by a unique identifier</summary>
		public Dictionary<string, BotConfig> Bots { get; set; }

		public GameStartConfig()
		{
			GameSpeed = "default";
			Bots = new Dictionary<string, BotConfig>();
		}

		public void Validate()
		{
			if (string.IsNullOrEmpty(MapUid))
				throw new ArgumentException("MapUid is required");

			if (Bots == null)
				Bots = new Dictionary<string, BotConfig>();

			foreach (var bot in Bots.Values)
				bot.Validate();
		}
	}

	/// <summary>
	/// Configuration for a single bot player
	/// </summary>
	public class BotConfig
	{
		/// <summary>Player slot (e.g., Multi0, Multi1, Multi2)</summary>
		public string Slot { get; set; }

		/// <summary>Bot type (e.g., agent, normal, easy, hard, brutal)</summary>
		public string BotType { get; set; }

		/// <summary>Faction name (optional, e.g., allies, soviets)</summary>
		public string Faction { get; set; }

		/// <summary>Team number (optional, e.g., 1, 2, 3)</summary>
		public int? Team { get; set; }

		public void Validate()
		{
			if (string.IsNullOrEmpty(Slot))
				throw new ArgumentException("Bot Slot is required");

			if (string.IsNullOrEmpty(BotType))
				throw new ArgumentException("Bot BotType is required");
		}
	}

	/// <summary>
	/// Result of game start operation
	/// </summary>
	public class GameStartResult
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string MapUid { get; set; }
	}

	/// <summary>
	/// Current game status
	/// </summary>
	public enum GameStatus
	{
		NotStarted,
		Starting,
		Running,
		Stopping,
		Stopped
	}

	/// <summary>
	/// Game status information
	/// </summary>
	public class GameStatusInfo
	{
		public GameStatus Status { get; set; }
		public string MapUid { get; set; }
		public int? Tick { get; set; }
		public int PlayerCount { get; set; }
	}
}
