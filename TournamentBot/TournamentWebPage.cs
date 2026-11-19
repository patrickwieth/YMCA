using System.Net;
using System.Text;

namespace Ymca.TournamentBot;

public static class TournamentWebPage
{
    static string E(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? "");
    static string Status(object value) => value.ToString() switch
    {
        "AwaitingMapPicks" => "Waiting for map choices", "WaitingForPlayers" => "Waiting for players",
        "StartingServer" => "Starting server", "AwaitingConfirmation" => "Awaiting result confirmation",
        "RematchRequested" => "Rematch requested", _ => value.ToString() ?? "Unknown"
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
        body.Append("<div class='tree-scroll'><div class='tree'><svg class='connections' aria-hidden='true'></svg><div class='rounds'>");
        foreach (var round in series.GroupBy(s => s.TournamentRound))
        {
            body.Append($"<section class='round'><h2>Round {round.Key}</h2>");
            if (t.MapsByRound.TryGetValue(round.Key, out var maps))
                body.Append("<p class='round-maps'>" + string.Join(" → ", maps.Select((m, i) => $"{i + 1}. {E(m.Title)}")) + "</p>");
            foreach (var s in round.OrderBy(s => Bracket(t, s, series)).ThenBy(s => s.Id))
            {
                var sources = new[] { s.PlayerOneDiscordId, s.PlayerTwoDiscordId }.Select(player => series
                    .LastOrDefault(prior => prior.TournamentRound < s.TournamentRound &&
                        (prior.PlayerOneDiscordId == player || prior.PlayerTwoDiscordId == player))?.Id)
                    .Where(source => source != null).Distinct();
                var status = t.Status == TournamentStatus.Cancelled && s.Status != TournamentSeriesStatus.Completed ? "Cancelled" : Status(s.Status);
                body.Append($"<article class='card series' id='series-{E(s.Id)}' data-from='{E(string.Join(",", sources))}'><div class='badge'>{E(Bracket(t, s, series))} · {E(s.Id)}</div>");
                foreach (var player in new[] { s.PlayerOneDiscordId, s.PlayerTwoDiscordId })
                {
                    var wins = s.Wins.GetValueOrDefault(player);
                    body.Append($"<div class='side {(wins >= s.WinsRequired ? "winner" : "")}'><span>{E(Name(player))}</span><strong>{wins}</strong></div>");
                }
                body.Append($"<p class='muted'>{E(status)}</p><ol class='games'>");
                foreach (var mid in s.MatchIds)
                {
                    if (!state.Matches.TryGetValue(mid, out var m) || m.TournamentId != t.Id) continue;
                    body.Append($"<li>Game {m.SeriesGameNumber}: {E(m.MapTitle)}<br><small>{E(m.Id)} · {E(Status(m.Status))}");
                    if (m.FinalWinnerDiscordId is ulong winner) body.Append($" · Winner: {E(Name(winner))}");
                    if (m.ParentMatchId != null) body.Append(" · Rematch");
                    if (m.Status == MatchStatus.WaitingForPlayers && m.AllowSpectators && m.Port != null && !string.IsNullOrEmpty(m.SpectatorPassword))
                        body.Append($" · <a href='/spectate/{E(Uri.EscapeDataString(m.Id))}'>Spectate</a>");
                    if (m.Status == MatchStatus.Completed && !string.IsNullOrEmpty(m.ReplayPath))
                        body.Append($" · <a href='/replay/{E(Uri.EscapeDataString(m.Id))}'>Replay</a>");
                    body.Append("</small></li>");
                }
                body.Append("</ol></article>");
            }
            body.Append("</section>");
        }
        body.Append("</div></div></div>");
        if (series.Count == 0)
            body.Append(t.Status == TournamentStatus.Registration
                ? "<p>The bracket will appear when the tournament starts.</p>"
                : "<p>No series bracket was recorded for this tournament.</p>");
        body.Append("<h2>Entrants</h2><ul class='entrants'>");
        foreach (var player in t.Entrants) body.Append($"<li>{E(Name(player))}</li>");
        body.Append("</ul>");
        return Shell(t.Name + " — YMCA tournament", body.ToString());
    }

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
:root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#121721;color:#e8edf5;font:16px system-ui,sans-serif}main,footer{max-width:1500px;margin:auto;padding:24px}a{color:#8ebeff;text-decoration:none}a:hover{text-decoration:underline}h1{margin:18px 0;font-size:clamp(26px,4vw,40px)}h2{font-size:21px}.muted,small,footer{color:#aab8ce}.listing{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:18px}.card{display:block;background:#202b3d;border:1px solid #40516a;border-radius:10px;padding:16px;margin:0 0 22px;color:inherit;box-shadow:0 5px 15px #0003}.badge{color:#aac5ed;font-size:13px;margin-bottom:14px}.side{display:flex;justify-content:space-between;gap:16px;padding:9px 0;border-bottom:1px solid #40516a;overflow-wrap:anywhere}.winner{color:#7feaaf}.games{padding-left:19px}.games li{margin:12px 0;overflow-wrap:anywhere}.tree-scroll{overflow:auto;padding:12px 0}.tree{position:relative;width:max-content;min-width:100%}.rounds{position:relative;display:flex;gap:64px;align-items:flex-start}.round{width:300px;flex:0 0 300px}.round-maps{min-height:48px;font-size:14px;color:#aab8ce}.connections{position:absolute;inset:0;width:100%;height:100%;pointer-events:none;overflow:visible}.connections path{stroke:#56729a;stroke-width:2;fill:none}.entrants{columns:3;line-height:1.8}@media(max-width:600px){main,footer{padding:16px}.entrants{columns:1}.round{width:270px;flex-basis:270px}}";

    public const string Script = @"
function draw(){const tree=document.querySelector('.tree');if(!tree)return;const svg=tree.querySelector('svg');svg.replaceChildren();const base=tree.getBoundingClientRect();document.querySelectorAll('.series').forEach(card=>{(card.dataset.from||'').split(',').filter(Boolean).forEach(id=>{const from=document.getElementById('series-'+id);if(!from)return;const a=from.getBoundingClientRect(),b=card.getBoundingClientRect();const x=a.right-base.left,y=a.top+a.height/2-base.top,u=b.left-base.left,v=b.top+b.height/2-base.top;const path=document.createElementNS('http://www.w3.org/2000/svg','path');path.setAttribute('d',`M ${x} ${y} C ${x+30} ${y}, ${u-30} ${v}, ${u} ${v}`);svg.append(path);});});}
let loading=false;async function refresh(){if(loading||document.hidden)return;loading=true;try{const response=await fetch(location.pathname,{cache:'no-store'});if(!response.ok)throw Error();const page=new DOMParser().parseFromString(await response.text(),'text/html');const next=page.getElementById('content');if(!next)throw Error();const scroll=document.querySelector('.tree-scroll')?.scrollLeft||0;document.getElementById('content').replaceWith(next);const tree=document.querySelector('.tree-scroll');if(tree)tree.scrollLeft=scroll;draw();document.getElementById('refresh-status').textContent='Updated '+new Date().toLocaleTimeString();}catch{document.getElementById('refresh-status').textContent='Update failed — retrying shortly';}finally{loading=false;}}
draw();window.addEventListener('resize',draw);setInterval(refresh,10000);document.addEventListener('visibilitychange',()=>{if(!document.hidden)refresh();});";
}
