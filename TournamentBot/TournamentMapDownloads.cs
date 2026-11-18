using OpenRA.Mods.CA.MapGeneration;

namespace Ymca.TournamentBot;

public static class TournamentMapDownloads
{
    public static void WriteReceipt(MatchRecord match, string? publicBaseUrl)
    {
        var path = Path.Combine(match.SupportDirectory, TournamentMapDownloadInfo.ReceiptFilename);
        if (match.Generation == null || string.IsNullOrEmpty(publicBaseUrl))
        {
            if (File.Exists(path)) File.Delete(path);
            return;
        }

        var info = new TournamentMapDownloadInfo(match.MapUid, match.GeneratedMapSha256,
            checked((int)new FileInfo(match.GeneratedMapPath).Length),
            publicBaseUrl.TrimEnd('/') + "/map/" + Uri.EscapeDataString(match.Id) + "/download");
        File.WriteAllText(path, info.Encode());
    }
}
