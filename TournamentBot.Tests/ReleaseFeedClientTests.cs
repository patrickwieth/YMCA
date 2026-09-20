using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class ReleaseFeedClientTests
{
    [Test]
    public void ParsesStableManifestRelease()
    {
        var manifest = "Version: v0.96.20\n"
            + "ReleaseUrl: https://github.com/patrickwieth/YMCA/releases/tag/v0.96.20\n"
            + "WindowsInstaller: https://example.invalid/setup.exe\n";

        var release = ReleaseFeedClient.ParseManifest(manifest);

        Assert.That(release.Version, Is.EqualTo("v0.96.20"));
        Assert.That(release.ReleaseUrl, Is.EqualTo("https://github.com/patrickwieth/YMCA/releases/tag/v0.96.20"));
    }

    [Test]
    public void RejectsIncompleteStableManifest()
    {
        Assert.Throws<InvalidDataException>(() => ReleaseFeedClient.ParseManifest("Version: v0.96.20"));
    }
}
