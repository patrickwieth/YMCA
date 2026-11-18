using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using NUnit.Framework;
using OpenRA.Mods.CA.MapGeneration;

namespace Modular.Tests;

[TestFixture]
public sealed class TournamentMapDownloadTests
{
    static readonly byte[] Bytes = { 1, 2, 3, 4 };
    const string Url = "https://tournament.crowdcontrol.network/map/M0020/download";
    static TournamentMapDownloadInfo Info(byte[] bytes = null, int? size = null) => new(new string('a', 40),
        Convert.ToHexString(SHA256.HashData(bytes ?? Bytes)), size ?? (bytes ?? Bytes).Length, Url);

    [Test]
    public void DescriptorRoundTrip()
    {
        var expected = Info();
        Assert.That(TournamentMapDownloadInfo.TryParse(expected.Encode(), out var actual), Is.True);
        Assert.That(actual.Encode(), Is.EqualTo(expected.Encode()));
    }

    [TestCase("http://tournament.crowdcontrol.network/map/M0020/download")]
    [TestCase("https://127.0.0.1/map/M0020/download")]
    [TestCase("https://tournament.crowdcontrol.network.attacker.example/map/M0020/download")]
    [TestCase("https://tournament.crowdcontrol.network:8080/map/M0020/download")]
    [TestCase("https://user@tournament.crowdcontrol.network/map/M0020/download")]
    [TestCase("https://tournament.crowdcontrol.network/map/M0020/download?redirect=localhost")]
    [TestCase("https://tournament.crowdcontrol.network/map/M0020/download#fragment")]
    [TestCase("https://tournament.crowdcontrol.network/admin")]
    [TestCase("file:///etc/passwd")]
    public void RejectsUntrustedEndpoints(string url) => Assert.Throws<ArgumentException>(() =>
        new TournamentMapDownloadInfo(new string('a', 40), new string('b', 64), 4, url));

    [TestCase("")]
    [TestCase("2|bad")]
    [TestCase("1|bad|bad|999999999999999999999|url")]
    public void RejectsMalformedWireData(string value) => Assert.That(TournamentMapDownloadInfo.TryParse(value, out _), Is.False);

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(TournamentMapDownloadInfo.MaxBytes + 1)]
    public void RejectsInvalidSize(int size) => Assert.Throws<ArgumentException>(() => Info(size: size));

    [Test]
    public async Task DownloadsExactBytesAndReportsProgress()
    {
        var progress = 0;
        var result = await TournamentMapTransfer.DownloadAsync(Info(), default, x => progress = x,
            new Reply(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Bytes) }));
        Assert.That(result, Is.EqualTo(Bytes)); Assert.That(progress, Is.EqualTo(100));
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Redirect)]
    [TestCase(HttpStatusCode.PartialContent)]
    public void RejectsHttpFailureAndRedirect(HttpStatusCode code) => Assert.ThrowsAsync<InvalidDataException>(async () =>
        await TournamentMapTransfer.DownloadAsync(Info(), default, transport: new Reply(new HttpResponseMessage(code))));

    [Test]
    public void RejectsChecksumMismatch() => Assert.ThrowsAsync<InvalidDataException>(async () =>
        await TournamentMapTransfer.DownloadAsync(Info(), default, transport: new Reply(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new ByteArrayContent(new byte[] { 4, 3, 2, 1 }) })));

    [TestCase(3)]
    [TestCase(5)]
    public void BoundsChunkedBodyAndRejectsTruncation(int length) => Assert.ThrowsAsync<InvalidDataException>(async () =>
        await TournamentMapTransfer.DownloadAsync(Info(), default, transport: new Reply(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new Chunked(new byte[length]) })));

    [Test]
    public void CancellationStopsTransfer()
    {
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () =>
            await TournamentMapTransfer.DownloadAsync(Info(), cancel.Token, transport: new WaitForCancellation()));
    }

    sealed class Reply : HttpMessageHandler
    {
        readonly HttpResponseMessage response;
        public Reply(HttpResponseMessage response) => this.response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Assert.That(request.RequestUri!.AbsoluteUri, Is.EqualTo(Url)); return Task.FromResult(response); }
    }
    sealed class WaitForCancellation : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { await Task.Delay(Timeout.Infinite, token); throw new AssertionException("Cancellation ignored."); }
    }
    sealed class Chunked : HttpContent
    {
        readonly byte[] bytes;
        public Chunked(byte[] bytes) => this.bytes = bytes;
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => stream.WriteAsync(bytes, 0, bytes.Length);
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new MemoryStream(bytes));
    }
}
