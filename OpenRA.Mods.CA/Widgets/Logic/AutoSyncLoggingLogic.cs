using System;
using System.IO;
using OpenRA;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
    public sealed class AutoSyncLoggingLogic : ChromeLogic
    {
        static bool ensured;
        const string SyncLoggingFile = "SyncLogging.yaml";
        static readonly string DefaultContent = string.Join(Environment.NewLine, new[]
        {
            "Enabled: true",
            "FrameCount: 2048",
            "LogInterval: 256",
        }) + Environment.NewLine;

        [ObjectCreator.UseCtor]
        public AutoSyncLoggingLogic(Widget widget)
        {
            if (ensured)
                return;

            ensured = true;

            if (Game.ModData?.Manifest.Id != "ca")
                return;

            EnsureSyncLogging();
        }

        static void EnsureSyncLogging()
        {
            try
            {
                var filePath = Path.Combine(Platform.SupportDir, SyncLoggingFile);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                if (!File.Exists(filePath) || File.ReadAllText(filePath) != DefaultContent)
                    File.WriteAllText(filePath, DefaultContent);
            }
            catch (Exception ex)
            {
                Log.Write("sync", $"Failed to ensure sync logging configuration: {ex}");
            }
        }
    }
}
