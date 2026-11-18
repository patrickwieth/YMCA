using System;
using System.IO;
using System.Security.Cryptography;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Network;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.CA.Server
{
	public sealed class TournamentMapDownloadServer : ServerTrait, INotifyServerStart, INotifySyncLobbyInfo
	{
		TournamentMapDownloadInfo download;

		void INotifyServerStart.ServerStarted(S server)
		{
			if (server.Type != ServerType.Dedicated || server.Settings.AllowedPlayerNames.Length == 0 || server.Settings.MapPool.Length != 1) return;
			var receipt = Path.Combine(Platform.SupportDir, TournamentMapDownloadInfo.ReceiptFilename);
			if (!File.Exists(receipt)) return;
			try
			{
				if (new FileInfo(receipt).Length > 400 || !TournamentMapDownloadInfo.TryParse(File.ReadAllText(receipt), out var candidate) ||
					candidate.Uid != server.LobbyInfo.GlobalSettings.Map || candidate.Uid != server.Settings.MapPool[0])
					throw new InvalidDataException("Invalid tournament download receipt.");
				var package = server.ModData.MapCache[candidate.Uid].PackageName;
				if (new FileInfo(package).Length != candidate.Size) throw new InvalidDataException("Tournament map size mismatch.");
				using var sha = SHA256.Create();
				using var stream = File.OpenRead(package);
				var hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
				if (hash != candidate.Sha256) throw new InvalidDataException("Tournament map checksum mismatch.");
				download = candidate;
				Publish(server);
			}
			catch (Exception e) { Log.Write("server", "Tournament map download not advertised: " + e.Message); }
		}

		void INotifySyncLobbyInfo.LobbyInfoSynced(S server) => Publish(server);

		void Publish(S server)
		{
			var settings = server.LobbyInfo.GlobalSettings;
			if (download == null || download.Uid != settings.Map)
			{
				if (settings.LobbyOptions.Remove(TournamentMapDownloadInfo.LobbyOption)) server.SyncLobbyGlobalSettings();
				return;
			}
			var value = download.Encode();
			if (settings.LobbyOptions.TryGetValue(TournamentMapDownloadInfo.LobbyOption, out var current) && current.Value == value) return;
			settings.LobbyOptions[TournamentMapDownloadInfo.LobbyOption] = new Session.LobbyOptionState { Value = value, IsLocked = true };
			server.SyncLobbyGlobalSettings();
		}
	}
}
