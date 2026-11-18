#nullable disable
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Shared wire contract with the bot. The server cannot choose arbitrary URLs:
	// automatically fetched packages must come from the official HTTPS endpoint.
	public sealed class TournamentMapDownloadInfo
	{
		public const string LobbyOption = "ymca-map-download";
		public const string ReceiptFilename = "tournament-map-download.txt";
		public const int MaxBytes = 16 * 1024 * 1024;
		public const string TrustedOrigin = "https://tournament.crowdcontrol.network";
		public string Uid { get; }
		public string Sha256 { get; }
		public int Size { get; }
		public Uri Url { get; }

		public TournamentMapDownloadInfo(string uid, string sha256, int size, string url)
		{
			if (uid == null || !Regex.IsMatch(uid, "^[a-fA-F0-9]{40}$") ||
				sha256 == null || !Regex.IsMatch(sha256, "^[a-fA-F0-9]{64}$") || size <= 0 || size > MaxBytes)
				throw new ArgumentException("Invalid tournament map identity or size.");
			if (url == null || url.Length > 256 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
				uri.Scheme != "https" || !uri.IsDefaultPort || uri.Host != "tournament.crowdcontrol.network" ||
				uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
				!Regex.IsMatch(uri.AbsolutePath, "^/map/M[0-9]{1,12}/download$"))
				throw new ArgumentException("Tournament map URL is not an approved HTTPS endpoint.");
			Uid = uid.ToLowerInvariant();
			Sha256 = sha256.ToLowerInvariant();
			Size = size;
			Url = uri;
		}

		public string Encode() => "1|" + Uid + "|" + Sha256 + "|" + Size.ToString(CultureInfo.InvariantCulture) + "|" + Url.AbsoluteUri;

		public static bool TryParse(string value, out TournamentMapDownloadInfo info)
		{
			info = null;
			if (value == null || value.Length > 400) return false;
			var parts = value.Split('|');
			if (parts.Length != 5 || parts[0] != "1" || !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var size)) return false;
			try { info = new TournamentMapDownloadInfo(parts[1], parts[2], size, parts[4]); return true; }
			catch (ArgumentException) { return false; }
		}
	}
}
