using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRA.Mods.CA.MapGeneration
{
	public static class TournamentMapTransfer
	{
		public static async Task<byte[]> DownloadAsync(TournamentMapDownloadInfo info, CancellationToken cancellation,
			Action<int> progress = null, HttpMessageHandler transport = null)
		{
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			timeout.CancelAfter(TimeSpan.FromMinutes(2));
			var token = timeout.Token;
			using var client = new HttpClient(transport ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
			using var response = await client.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
			// Redirects are intentionally not followed, even to another HTTPS host.
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				throw new InvalidDataException("Map download returned HTTP " + (int)response.StatusCode + ".");
			if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value != info.Size)
				throw new InvalidDataException("Map download size differs from the server's announcement.");
			using var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
			using var output = new MemoryStream();
			var buffer = new byte[8192];
			int count;
			while ((count = await input.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
			{
				if (output.Length + count > info.Size) throw new InvalidDataException("Map download exceeds its announced size.");
				output.Write(buffer, 0, count);
				progress?.Invoke((int)(100 * output.Length / info.Size));
			}
			token.ThrowIfCancellationRequested();
			if (output.Length != info.Size) throw new InvalidDataException("Map download was incomplete.");
			var bytes = output.ToArray();
			using var sha = SHA256.Create();
			var actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
			if (actual != info.Sha256) throw new InvalidDataException("Map download checksum differs from the server's announcement.");
			return bytes;
		}
	}
}
