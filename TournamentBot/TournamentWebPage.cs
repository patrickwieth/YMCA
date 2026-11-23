using System.Net;
using System.Text;

namespace Ymca.TournamentBot;

public static class TournamentWebPage
{
	static string E(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? "");
	static string Status(object value) => value.ToString() switch
	{
		"AwaitingMapPicks" => "Waiting for map choices",
		"WaitingForPlayers" => "Waiting for players",
		"StartingServer" => "Starting server",
		"AwaitingConfirmation" => "Awaiting result confirmation",
		"RematchRequested" => "Rematch requested",
		_ => value.ToString() ?? "Unknown"
	};
	static string Link(string id) => "/tournament/" + Uri.EscapeDataString(id);

	public static string Index(TournamentState state)
	{
		var body = new StringBuilder("<h1>YMCA tournaments</h1><p class='muted'>Live brackets, results and replays. Updates every 10 seconds.</p><div class='listing'>");
		foreach (var t in state.Tournaments.Values.OrderByDescending(t => t.CreatedAtUtc).Take(100))
			body.Append($"<a class='card' href='{E(Link(t.Id))}'><h2>{E(t.Name)}</h2><p>{E(t.Id)} · {E(t.Status)} · {t.Entrants.Count} entrants</p></a>");
		if (state.Tournaments.Count == 0) body.Append("<p>No tournaments yet.</p>");
		return Shell("YMCA tournaments", body.Append("</div>").ToString());
	}

	public static string? Tournament(TournamentState state, string id)
	{
		if (!state.Tournaments.TryGetValue(id.Trim().ToUpperInvariant(), out var t)) return null;
		string Name(ulong player) => t.Mode == TournamentMode.TwoVsTwo && t.Teams.TryGetValue(player, out var team)
			? team.Name : state.Players.TryGetValue(player, out var registered) ? registered.OpenRaName : "Unknown player";
		var series = t.SeriesIds.Where(state.Series.ContainsKey).Select(key => state.Series[key])
			.Where(s => s.TournamentId == t.Id).OrderBy(s => s.TournamentRound).ThenBy(s => s.Id).ToList();
		var body = new StringBuilder($"<a href='/tournaments'>← All tournaments</a><h1>{E(t.Name)}</h1>" +
			$"<p>{E(t.Id)} · <strong>{E(t.Status)}</strong> · {(t.Format == TournamentFormat.DoubleElimination ? "Double elimination" : "Single elimination")} · {(t.Mode == TournamentMode.TwoVsTwo ? "2v2" : "1v1")} · BO{t.WinsRequired * 2 - 1}</p>" +
			$"<p>Maps: {(t.MapSelection == TournamentMapSelection.RoundRandom ? "Fixed random sequence per round" : "Player choice with shared round decider")}</p>" +
			"<p class='muted'>Updates every 10 seconds. Results awaiting confirmation are not final. Future pairings appear when the previous round is decided.</p>");
		if (t.ChampionDiscordId is ulong champion) body.Append($"<h2>🏆 {E(Name(champion))}</h2>");
		if (t.RunnerUpDiscordId is ulong second) body.Append($"<p>Second: {E(Name(second))}</p>");
		if (t.ThirdPlaceDiscordId is ulong third) body.Append($"<p>Third: {E(Name(third))}</p>");
		body.Append("<p class='muted'>Hover a player to follow their path. Open a match for maps, results and replays. Source links connect separate brackets and skipped rounds.</p><nav class='bracket-nav' aria-label='Brackets'>");
		var lanes = series.GroupBy(s => Bracket(t, s, series)).OrderBy(g => LaneOrder(g.Key)).ThenBy(g => g.Key).ToList();
		for (var i = 0; i < lanes.Count; i++) body.Append($"<a href='#bracket-{i}'>{E(lanes[i].Key)}</a>");
		body.Append("</nav>");
		for (var laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
		{
			var lane = lanes[laneIndex];
			body.Append($"<section class='bracket-lane' id='bracket-{laneIndex}' aria-label='{E(lane.Key)}'><h2>{E(lane.Key)}</h2><div class='tree-scroll' tabindex='0' role='region' aria-label='{E(lane.Key)} rounds'><div class='tree'><svg class='connections' aria-hidden='true'></svg><div class='rounds'>");
			foreach (var round in lane.GroupBy(s => s.TournamentRound))
			{
				body.Append($"<section class='round'><h2>Round {round.Key}</h2>");
				if (t.MapsByRound.TryGetValue(round.Key, out var maps))
					body.Append($"<details class='round-maps' id='maps-{laneIndex}-{round.Key}'><summary>Round maps</summary><p>" + string.Join(" → ", maps.Select((m, i) => $"{i + 1}. {E(m.Title)}")) + "</p></details>");
				foreach (var s in round.OrderBy(s => s.Id))
				{
					var origins = new[] { s.PlayerOneDiscordId, s.PlayerTwoDiscordId }.ToDictionary(player => player, player => series
						.LastOrDefault(prior => prior.TournamentRound < s.TournamentRound &&
							(prior.PlayerOneDiscordId == player || prior.PlayerTwoDiscordId == player)));
					var sources = origins.Values.Where(source => source != null).Select(source => source!.Id).Distinct();
					var status = t.Status == TournamentStatus.Cancelled && s.Status != TournamentSeriesStatus.Completed ? "Cancelled" : Status(s.Status);
					body.Append($"<article class='card series' id='series-{E(s.Id)}' data-from='{E(string.Join(",", sources))}' data-players='{s.PlayerOneDiscordId},{s.PlayerTwoDiscordId}'><details id='details-{E(s.Id)}'><summary aria-label='Match {E(s.Id)}: {E(Name(s.PlayerOneDiscordId))} versus {E(Name(s.PlayerTwoDiscordId))}'><span class='badge'>{E(s.Id)} · BO{s.WinsRequired * 2 - 1}</span>");
					foreach (var player in new[] { s.PlayerOneDiscordId, s.PlayerTwoDiscordId })
					{
						var wins = s.Wins.GetValueOrDefault(player);
						body.Append($"<span class='side {(Winner(s) == player ? "winner" : "")}' data-player='{player}'><span title='{E(Name(player))}'>{E(Name(player))}</span><strong>{wins}</strong></span>");
					}
					body.Append($"<span class='match-status'>{E(status)}</span><span class='detail-hint'>Match details &amp; replays</span></summary><div class='match-details'>");
					if (s.MapChoiceTimer is { Outcome: "Pending" } choiceTimer)
						body.Append($"<p>Map-choice deadline: {E(choiceTimer.DeadlineUtc.ToString("HH:mm 'UTC'"))}. Missing choices can forfeit this entire matchup.</p>");
					if (s.MapChoiceTimer?.Outcome == "Review")
						body.Append("<p>Map-choice deadline expired: admin review required.</p>");
					if (s.ForfeitWinnerDiscordId is ulong forfeitWinner)
						body.Append($"<p>Walkover: {E(Name(forfeitWinner))} wins the entire series.<br>{E(s.ForfeitReason)}</p>");
					body.Append("<ol class='games'>");
					foreach (var mid in s.MatchIds)
					{
						if (!state.Matches.TryGetValue(mid, out var m) || m.TournamentId != t.Id) continue;
						body.Append($"<li>Game {m.SeriesGameNumber}: {E(m.MapTitle)}<br><small>{E(m.Id)} · {E(Status(m.Status))}");
						if (m.FinalWinnerDiscordId is ulong winner) body.Append($" · Winner: {E(Name(winner))}");
						if (m.ForfeitReason != null) body.Append(" · " + E(m.ForfeitReason));
						if (m.AttendanceTimer is { Outcome: "Pending" } timer)
							body.Append(" · Admin attendance deadline: " + E(timer.DeadlineUtc.ToString("HH:mm 'UTC'")));
						if (m.ParentMatchId != null) body.Append(" · Rematch");
						if (m.Status == MatchStatus.WaitingForPlayers && m.AllowSpectators && m.Port != null && !string.IsNullOrEmpty(m.SpectatorPassword))
							body.Append($" · <a href='/spectate/{E(Uri.EscapeDataString(m.Id))}'>Spectate</a>");
						if (m.Status == MatchStatus.Completed && !string.IsNullOrEmpty(m.ReplayPath))
							body.Append($" · <a href='/replay/{E(Uri.EscapeDataString(m.Id))}'>Replay</a>");
						body.Append("</small></li>");
					}
					body.Append("</ol></div></details><nav class='origins' aria-label='Previous matches'>");
					foreach (var origin in origins.Where(pair => pair.Value != null))
					{
						var prior = origin.Value!;
						var result = Winner(prior) is ulong priorWinner ? priorWinner == origin.Key ? "Winner" : "Loser" : "From";
						body.Append($"<a class='source-link' data-source='{E(prior.Id)}' data-player='{origin.Key}' href='#series-{E(prior.Id)}' title='{E(Name(origin.Key))} · {E(Bracket(t, prior, series))}'>← {result} {E(prior.Id)} · {E(Name(origin.Key))}</a>");
					}
					body.Append("</nav></article>");
				}
				body.Append("</section>");
			}
			body.Append("</div></div></div></section>");
		}
		if (series.Count == 0)
			body.Append(t.Status == TournamentStatus.Registration
				? "<p>The bracket will appear when the tournament starts.</p>"
				: "<p>No series bracket was recorded for this tournament.</p>");
		body.Append("<h2>Entrants</h2><ul class='entrants'>");
		foreach (var player in t.Entrants) body.Append($"<li>{E(Name(player))}</li>");
		body.Append("</ul>");
		return Shell(t.Name + " — YMCA tournament", body.ToString());
	}

	static ulong? Winner(TournamentSeries series) => series.Status != TournamentSeriesStatus.Completed ? null
		: series.ForfeitWinnerDiscordId ?? series.Wins.Where(w => w.Value >= series.WinsRequired).Select(w => (ulong?)w.Key).FirstOrDefault();

	static int LaneOrder(string label) => label switch
	{
		"Upper bracket" => 0,
		"Lower bracket" => 1,
		"Final" => 2,
		"Third place" => 3,
		_ => 4
	};

	static string Bracket(TournamentRecord t, TournamentSeries s, List<TournamentSeries> all)
	{
		if (s.Bracket.Length != 0) return s.Bracket;
		if (s.IsThirdPlaceMatch) return "Third place";
		var remaining = t.Entrants.Count(p => !t.EliminatedInRound.TryGetValue(p, out var round) || round >= s.TournamentRound);
		if (remaining == 2) return "Final";
		if (t.Format == TournamentFormat.DoubleElimination && all.Any(prior => prior.TournamentRound < s.TournamentRound &&
			prior.Status == TournamentSeriesStatus.Completed && prior.Wins.GetValueOrDefault(s.PlayerOneDiscordId) < prior.WinsRequired &&
			(prior.PlayerOneDiscordId == s.PlayerOneDiscordId || prior.PlayerTwoDiscordId == s.PlayerOneDiscordId))) return "Lower bracket";
		return "Upper bracket";
	}

	static string Shell(string title, string body) => "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>" +
		$"<title>{E(title)}</title><style>{Css}</style><script src='/tournament-assets.js' defer></script></head><body><main id='content'>{body}</main>" +
		"<footer>YMCA · <span id='refresh-status' role='status'>Live updates enabled</span> · <a href=''>Refresh now</a></footer></body></html>";

	const string Css = @"
:root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#121721;color:#e8edf5;font:16px system-ui,sans-serif}main,footer{max-width:1500px;margin:auto;padding:24px}a{color:#8ebeff;text-decoration:none}a:hover{text-decoration:underline}h1{margin:18px 0;font-size:clamp(26px,4vw,40px)}h2{font-size:21px}.muted,small,footer{color:#aab8ce}.listing{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:18px}.card{display:block;background:#202b3d;border:1px solid #40516a;border-radius:10px;padding:16px;margin:0 0 22px;color:inherit;box-shadow:0 5px 15px #0003}.badge{color:#aac5ed;font-size:13px;margin-bottom:14px}.side{display:flex;justify-content:space-between;gap:16px;padding:9px 0;border-bottom:1px solid #40516a;overflow-wrap:anywhere}.winner{color:#7feaaf}.games{padding-left:19px}.games li{margin:12px 0;overflow-wrap:anywhere}.tree-scroll{overflow:auto;padding:12px 0}.tree{position:relative;width:max-content;min-width:100%}.rounds{position:relative;display:flex;gap:64px;align-items:flex-start}.round{width:300px;flex:0 0 300px}.round-maps{min-height:48px;font-size:14px;color:#aab8ce}.connections{position:absolute;inset:0;width:100%;height:100%;pointer-events:none;overflow:visible}.connections path{stroke:#56729a;stroke-width:2;fill:none}.bracket-nav{display:flex;gap:12px;flex-wrap:wrap}.bracket-nav a{padding:8px 14px;border:1px solid #40516a;border-radius:8px}.bracket-lane{margin-top:32px;border-top:1px solid #40516a;padding-top:8px;scroll-margin-top:16px}.bracket-lane>h2{color:#a9c9ff}.rounds{gap:72px}.round>h2{font-size:16px;color:#aab8ce}.round-maps{min-height:30px;margin-bottom:16px}.round-maps summary{cursor:pointer}.series{padding:0;overflow:hidden;scroll-margin:24px;transition:border-color .12s,box-shadow .12s}.series summary{cursor:pointer;padding:10px 14px;list-style:none}.series .side{padding:5px 0}.series .badge{font-size:12px}.series summary::-webkit-details-marker{display:none}.series .badge{display:block;margin-bottom:6px}.series .side{font-size:15px}.side>span{white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.side strong{min-width:20px;text-align:right}.match-status{display:block;color:#aab8ce;font-size:12px;margin-top:10px}.detail-hint{display:block;color:#8ebeff;font-size:12px;margin-top:8px}.detail-hint:before{content:'▸ ';}.series details[open] .detail-hint:before{content:'▾ ';}.match-details{padding:0 14px 12px;border-top:1px solid #40516a;font-size:14px}.origins{padding:6px 14px;min-height:12px;border-top:1px solid #34445c}.source-link{display:block;font-size:12px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;line-height:1.8}.series.active,.series:target{border-color:#7feaaf;box-shadow:0 0 0 2px #7feaaf44}.connections path.active{stroke:#7feaaf;stroke-width:3}.tree.highlighting .series:not(.active){opacity:.45}.tree.highlighting path:not(.active){opacity:.2}summary:focus-visible,a:focus-visible,.tree-scroll:focus-visible{outline:2px solid #7feaaf;outline-offset:3px}.entrants{columns:3;line-height:1.8}@media(max-width:600px){main,footer{padding:16px}.entrants{columns:1}.round{width:270px;flex-basis:270px}}";

	public const string Script = TournamentBracketScript.Value;
}
