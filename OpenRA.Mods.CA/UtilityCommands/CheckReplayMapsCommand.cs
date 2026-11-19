using System;
using System.IO;
using System.IO.Compression;
using OpenRA.FileFormats;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckReplayMapsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-replay-maps";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;

		[Desc("REPLAY OUTPUT", "On a fresh support profile, restore a legacy generated replay and create/verify a portable copy. Never modifies the input.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var mod = utility.ModData;
			mod.MapCache.LoadMaps();
			var replay = ReplayMetadata.Read(args[1]) ?? throw new InvalidDataException("Invalid input replay.");
			if (File.Exists(args[2])) throw new InvalidDataException("Output already exists.");
			if (mod.MapCache[replay.GameInfo.MapUid].Status == MapStatus.Available) throw new InvalidDataException("Test needs an initially missing map.");
			Console.WriteLine("Legacy recipe: " + ReplayMapProvider.ReadLegacyRecipe(args[1], replay.GameInfo.MapUid));
			if (!new ReplayMapProvider().Restore(mod, replay)) throw new InvalidDataException("Legacy restoration failed.");
			Console.WriteLine("PASS legacy recipe restored exact UID: " + replay.GameInfo.MapUid);
			var localMap = mod.MapCache[replay.GameInfo.MapUid];
			var localClass = localMap.Class;
			foreach (var remoteStatus in new[] { MapStatus.Searching, MapStatus.Unavailable, MapStatus.DownloadAvailable })
			{
				localMap.UpdateRemoteSearch(remoteStatus, null, null, _ => throw new InvalidDataException("Stale remote callback ran."));
				if (localMap.Status != MapStatus.Available || localMap.Class != localClass)
					throw new InvalidDataException("A stale repository response replaced an installed map.");
			}
			Console.WriteLine("PASS installed map wins over stale repository responses");
			var archive = ReplayMapSupport.Capture(mod, replay.GameInfo.MapUid) ?? throw new InvalidDataException("Capture failed.");
			ReplayMapProvider.ValidateArchive(archive, replay.GameInfo.MapUid);
			using (var original = File.OpenRead(args[1]))
			using (var output = File.Create(args[2]))
			{
				original.Position = original.Length - 8;
				using var reader = new BinaryReader(original, System.Text.Encoding.UTF8, true);
				var metadataLength = reader.ReadInt32();
				var remaining = original.Length - metadataLength - 16;
				original.Position = 0;
				var buffer = new byte[8192];
				while (remaining > 0)
				{
					var count = original.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
					if (count == 0) throw new EndOfStreamException();
					output.Write(buffer, 0, count); remaining -= count;
				}
				using var writer = new BinaryWriter(output);
				new ReplayMetadata(replay.GameInfo) { EmbeddedMap = archive }.Write(writer);
			}
			var portable = ReplayMetadata.Read(args[2]);
			if (portable?.EmbeddedMap?.Length != archive.Length) throw new InvalidDataException("Archive footer round trip failed.");
			ReplayMapProvider.ValidateArchive(portable.EmbeddedMap, replay.GameInfo.MapUid);
			Console.WriteLine("PASS portable metadata round trip: " + archive.Length + " archive bytes.");

			void Reject(byte[] bytes, string uid, string name)
			{
				try { ReplayMapProvider.ValidateArchive(bytes, uid); }
				catch (Exception e) when (e is InvalidDataException || e is ICSharpCode.SharpZipLib.Zip.ZipException)
				{ Console.WriteLine("PASS rejected " + name); return; }
				throw new InvalidDataException("Accepted invalid archive: " + name);
			}
			Reject(archive, new string('0', 40), "UID mismatch");
			Reject(new byte[] { 1, 2, 3 }, replay.GameInfo.MapUid, "corrupt archive");
			using var unsafeArchive = new MemoryStream();
			using (var zip = new ZipArchive(unsafeArchive, ZipArchiveMode.Create, true)) zip.CreateEntry("../map.yaml");
			Reject(unsafeArchive.ToArray(), replay.GameInfo.MapUid, "path traversal");
			using var duplicateArchive = new MemoryStream();
			using (var zip = new ZipArchive(duplicateArchive, ZipArchiveMode.Create, true))
			{
				zip.CreateEntry("map.yaml"); zip.CreateEntry("MAP.YAML");
			}
			Reject(duplicateArchive.ToArray(), replay.GameInfo.MapUid, "duplicate entries");
			using var oversized = new MemoryStream();
			using (var zip = new ZipArchive(oversized, ZipArchiveMode.Create, true))
			using (var stream = zip.CreateEntry("map.bin", CompressionLevel.Fastest).Open())
			{
				var block = new byte[8192];
				for (var i = 0; i < 8193; i++) stream.Write(block, 0, block.Length);
			}
			Reject(oversized.ToArray(), replay.GameInfo.MapUid, "excessive expansion");
			var corrupt = File.ReadAllBytes(args[2]);
			var temporary = Path.GetTempFileName();
			try
			{
				File.WriteAllBytes(temporary, corrupt);
				File.SetAttributes(temporary, FileAttributes.ReadOnly);
				if (ReplayMetadata.Read(temporary) == null) throw new InvalidDataException("Read-only replay cannot be opened.");
				File.SetAttributes(temporary, FileAttributes.Normal);
				Console.WriteLine("PASS read-only replay metadata");
				Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, corrupt, corrupt.Length - 8, 4);
				File.WriteAllBytes(temporary, corrupt);
				if (ReplayMetadata.Read(temporary) != null) throw new InvalidDataException("Accepted oversized metadata length.");
				Console.WriteLine("PASS rejected invalid metadata length");
				corrupt = File.ReadAllBytes(args[2]);
				Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, corrupt, corrupt.Length - archive.Length - 12, 4);
				File.WriteAllBytes(temporary, corrupt);
				if (ReplayMetadata.Read(temporary) != null) throw new InvalidDataException("Accepted oversized embedded map length.");
				Console.WriteLine("PASS rejected invalid embedded map length");
			}
			finally { File.SetAttributes(temporary, FileAttributes.Normal); File.Delete(temporary); }
		}
	}
}
