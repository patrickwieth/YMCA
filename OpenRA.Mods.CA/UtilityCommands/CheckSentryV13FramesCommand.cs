using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-sentry-v13-frames", "Load native32 and retained V13 sheets through the engine sprite loaders.")]
	public sealed class CheckSentryV13FramesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-sentry-v13-frames";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			// Validate trait dependencies for the separate, non-buildable test actor too.
			var prototype = modData.DefaultRules.Actors["sentry.soviet"];
			_ = prototype.TraitsInConstructOrder().ToArray();
			Console.WriteLine("SENTRY.SOVIET: independent actor trait dependencies passed.");
			foreach (var item in new[]
			{
				(File: "sentry-v13-native32.png", Count: 32, Width: 120, Height: 120),
				(File: "sentry-base-native.png", Count: 1, Width: 74, Height: 43),
				(File: "sentry-v13.png", Count: 8, Width: 240, Height: 240)
			})
			{
				var filename = "bits/soviet/buildings/experimental/" + item.File;
				using var stream = modData.DefaultFileSystem.Open(filename);
				var frames = FrameLoader.GetFrames(stream, modData.SpriteLoaders, filename, out _);
				if (frames.Length != item.Count)
					throw new InvalidDataException($"{filename}: engine loaded {frames.Length} frames instead of {item.Count}.");

				foreach (var frame in frames)
					if (frame.Size.Width != item.Width || frame.Size.Height != item.Height ||
						frame.FrameSize != frame.Size || frame.Offset != float2.Zero || frame.Data.Length != item.Width * item.Height * 4)
						throw new InvalidDataException(filename + ": invalid native frame size, offset or pixel data.");

				Console.WriteLine($"{item.File}: {frames.Length} frames, each {item.Width}x{item.Height}, zero embedded offset.");
			}

			for (var i = 0; i < 32; i++)
			{
				var yaw = new WAngle(i * 32);
				var direction = new WVec(-yaw.Sin(), -yaw.Cos(), 0);
				if (OpenRA.Mods.Common.Util.IndexFacing(direction.Yaw, 32) != i)
					throw new InvalidDataException("Unexpected engine facing convention.");
			}
		}
	}
}
