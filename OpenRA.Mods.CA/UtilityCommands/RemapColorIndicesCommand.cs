using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class RemapColorIndicesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--remap-indices";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 4;
		}

		[Desc("SRCSHP DESTSHP OLD_INDEX:NEW_INDEX [OLD_INDEX:NEW_INDEX ...]", 
			"Remap specific color indices in SHP files")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var srcPath = args[1];
			var destPath = args[2];
			
			// Parse color mappings
			var colorMap = new Dictionary<byte, byte>();
			
			for (int i = 3; i < args.Length; i++)
			{
				var mapping = args[i].Split(':');
				if (mapping.Length != 2)
				{
					Console.WriteLine($"Invalid mapping format: {args[i]}. Use OLD_INDEX:NEW_INDEX");
					return;
				}
				
				if (!byte.TryParse(mapping[0], out var oldIndex) || 
					!byte.TryParse(mapping[1], out var newIndex))
				{
					Console.WriteLine($"Invalid index values in: {args[i]}");
					return;
				}
				
				colorMap[oldIndex] = newIndex;
			}
			
			Console.WriteLine($"Remapping indices in {srcPath}:");
			foreach (var kvp in colorMap)
				Console.WriteLine($"  {kvp.Key} -> {kvp.Value}");
			
			// Create backup
			var backupPath = srcPath + ".backup";
			if (!File.Exists(backupPath))
			{
				Console.WriteLine($"Creating backup: {backupPath}");
				File.Copy(srcPath, backupPath);
			}
			
			try 
			{
				using (var srcStream = File.OpenRead(srcPath))
				{
					var sprite = new ShpTDSprite(srcStream);
					
					// Remap pixels in each frame
					var remappedFrames = sprite.Frames.Select(frame =>
					{
						return frame.Data.Select(pixel => 
							colorMap.ContainsKey(pixel) ? colorMap[pixel] : pixel
						).ToArray();
					}).ToArray();
					
					// Write the result
					using (var destStream = File.Create(destPath))
					{
						ShpTDSprite.Write(destStream, sprite.Size, remappedFrames);
					}
				}
				
				Console.WriteLine($"Successfully remapped and saved to: {destPath}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error processing SHP file: {ex.Message}");
				
				// Restore from backup if we were overwriting the original
				if (destPath == srcPath && File.Exists(backupPath))
				{
					Console.WriteLine("Restoring from backup...");
					File.Copy(backupPath, srcPath, true);
				}
			}
		}
	}
}