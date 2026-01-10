using System.IO;
using System.Linq;
using OpenRA;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.FileSystem;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
    public sealed class AutoContentPromptLogic : ChromeLogic
    {
        const string ContentModId = "ca-content";

        static bool promptShown;

        [ObjectCreator.UseCtor]
        public AutoContentPromptLogic(Widget widget)
        {
            if (promptShown || Game.SkipContentPrompt)
                return;

            var modData = Game.ModData;
            if (modData == null || modData.Manifest.Id != "ca")
                return;

            if (modData.FileSystemLoader is not ContentInstallerFileSystemLoader installer)
                return;

            var content = LoadContentDefinition();
            if (content == null)
                return;
            if (HasRequiredContent(content))
                return;

            promptShown = true;

            Game.RunAfterTick(() =>
            {
                Game.InitializeMod(installer.ContentInstallerMod, new Arguments());
            });
        }

        static ModContent LoadContentDefinition()
        {
            if (!Game.Mods.TryGetValue(ContentModId, out var manifest))
                return null;

            var hasContent = manifest.Contains<ModContent>();
            if (!hasContent)
            {
                using var oc = new ObjectCreator(manifest, Game.Mods);
                manifest.LoadCustomData(oc);
                hasContent = manifest.Contains<ModContent>();
            }

            return hasContent ? manifest.Get<ModContent>() : null;
        }

        static bool HasRequiredContent(ModContent content)
        {
            var quickDownloadId = content.QuickDownload;

            return content.Packages
                .Where(p => p.Value.Required || (!string.IsNullOrEmpty(quickDownloadId) && p.Value.Identifier == quickDownloadId))
                .All(p => p.Value.TestFiles.All(f => File.Exists(Platform.ResolvePath(f))));
        }
    }
}
