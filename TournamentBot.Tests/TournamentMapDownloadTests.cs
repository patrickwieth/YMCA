using System.Security.Cryptography;
using NUnit.Framework;
using OpenRA.Mods.CA.MapGeneration;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class TournamentMapDownloadTests
{
    [Test]
    public void ReceiptBindsExactPackageAndClearsStaleDataWhenDisabled()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ymca-tournament-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "map.oramap");
            var bytes = new byte[] { 1, 2, 3 }; File.WriteAllBytes(path, bytes);
            var match = new MatchRecord { Id = "M0020", SupportDirectory = directory, GeneratedMapPath = path,
                Generation = new GeneratedMapSpec(), MapUid = new string('a', 40), GeneratedMapSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() };
            TournamentMapDownloads.WriteReceipt(match, TournamentMapDownloadInfo.TrustedOrigin);
            var receipt = Path.Combine(directory, TournamentMapDownloadInfo.ReceiptFilename);
            Assert.That(TournamentMapDownloadInfo.TryParse(File.ReadAllText(receipt), out var info), Is.True);
            Assert.Multiple(() => {
                Assert.That(info.Uid, Is.EqualTo(match.MapUid));
                Assert.That(info.Sha256, Is.EqualTo(match.GeneratedMapSha256));
                Assert.That(info.Size, Is.EqualTo(bytes.Length));
                Assert.That(info.Url.AbsolutePath, Is.EqualTo("/map/M0020/download"));
            });
            TournamentMapDownloads.WriteReceipt(match, null);
            Assert.That(File.Exists(receipt), Is.False);
            TournamentMapDownloads.WriteReceipt(match, TournamentMapDownloadInfo.TrustedOrigin);
            match.Generation = null;
            TournamentMapDownloads.WriteReceipt(match, TournamentMapDownloadInfo.TrustedOrigin);
            Assert.That(File.Exists(receipt), Is.False);
        }
        finally { Directory.Delete(directory, true); }
    }
}
