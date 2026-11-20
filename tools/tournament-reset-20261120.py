"""One-off authorized T012 reset. Run on host from TournamentBot/deploy; never log credentials."""
import copy, datetime, json, os, pathlib, random, subprocess, sys, time, urllib.request, urllib.error

ROOT = pathlib.Path('data')
STATE = ROOT / 'tournament-state.json'
RECOVERY = ROOT / 'recovery' / 'reset-T012-20261120'

def load():
    return json.loads(STATE.read_text())

def plan(state):
    s = copy.deepcopy(state)
    old = s['Tournaments']['T012']
    assert old['Status'] == 'Running', 'T012 is not running'
    assert old['Mode'] == 'OneVsOne' and old['MapSelection'] == 'PlayerChoice' and old['WinsRequired'] == 2
    entrants = old['Entrants'][:]
    assert len(entrants) == len(set(entrants)) == 16
    assert all(str(p) in s['Players'] for p in entrants)
    assert old['MapPool'] and '1' in old['RandomMapByRound']
    now = datetime.datetime.now(datetime.timezone.utc).isoformat()
    new_id = 'T%03d' % s['NextTournamentNumber']
    assert new_id not in s['Tournaments']
    s['NextTournamentNumber'] += 1
    new = copy.deepcopy(old)
    new.update(Id=new_id, Status='Running', RoundNumber=1, CreatedAtUtc=now, StartedAtUtc=now,
        FinishedAtUtc=None, RegistrationAnnouncementMessageId=None, MatchIds=[], SeriesIds=[],
        ProcessedMatchIds=[], ProcessedSeriesIds=[], EliminatedInRound={}, Losses={str(p):0 for p in entrants},
        ChampionDiscordId=None, RunnerUpDiscordId=None, ThirdPlaceDiscordId=None, FourthPlaceDiscordId=None,
        RandomMapByRound={'1':copy.deepcopy(old['RandomMapByRound']['1'])}, MapsByRound={})
    new['MapHistory'] = [new['RandomMapByRound']['1']['Uid']]
    new['MapUid'] = new['RandomMapByRound']['1']['Uid']
    new['MapTitle'] = new['RandomMapByRound']['1']['Title']
    random.SystemRandom().shuffle(entrants)
    new['Entrants'] = entrants
    old['Status'] = 'Cancelled'
    old['FinishedAtUtc'] = now
    retired = []
    for m in s['Matches'].values():
        t = s['Tournaments'].get(m.get('TournamentId'), {})
        if m.get('TournamentId') == 'T012' or (t.get('Status') in ('Completed','Cancelled') and m['Status'] in ('Queued','StartingServer','WaitingForPlayers','Playing')):
            m.update(Status='Cancelled', Port=None, Password='', SpectatorPassword='', ServerRetainUntilUtc=None, AttendanceTimer=None)
            retired.append(m['Id'])
    for series in s['Series'].values():
        if series.get('TournamentId') == 'T012':
            series['Status'] = 'Completed' # Close old map-selection interactions; tournament itself is cancelled.
    for a,b in zip(entrants[::2], entrants[1::2]):
        sid = 'S%04d' % s['NextSeriesNumber']
        assert sid not in s['Series']
        s['NextSeriesNumber'] += 1
        s['Series'][sid] = dict(Id=sid,TournamentId=new_id,PlayerOneDiscordId=a,PlayerTwoDiscordId=b,
            TournamentRound=1,Bracket='Upper bracket',IsThirdPlaceMatch=False,WinsRequired=2,
            Status='AwaitingMapPicks',MapPicks={str(a):[],str(b):[]},Maps=[],Wins={str(a):0,str(b):0},MatchIds=[])
        new['SeriesIds'].append(sid)
    s['Tournaments'][new_id] = new
    assert len(new['SeriesIds']) == 8 and set(new['Entrants']) == set(old['Entrants'])
    assert not any(m['Status'] in ('Queued','StartingServer','WaitingForPlayers','Playing') and
                   s['Tournaments'].get(m.get('TournamentId'),{}).get('Status') in ('Completed','Cancelled')
                   for m in s['Matches'].values())
    return s,new_id,retired

def discord_api(token, method, path, body=None):
    payload = None if body is None else json.dumps(body).encode()
    for _ in range(8):
        req = urllib.request.Request('https://discord.com/api/v10'+path, data=payload, method=method,
            headers={'Authorization':'Bot '+token, 'Content-Type':'application/json','User-Agent':'YMCA-Tournament-Recovery/1.0'})
        try:
            with urllib.request.urlopen(req,timeout=30) as r:
                data = r.read()
                return json.loads(data) if data else {}
        except urllib.error.HTTPError as e:
            if e.code == 429:
                wait=json.loads(e.read()).get('retry_after',2); time.sleep(float(wait)+0.2); continue
            raise RuntimeError('Discord request failed HTTP '+str(e.code)) from None
    raise RuntimeError('Discord rate-limit retry budget exhausted')

mode=sys.argv[1]
if mode == 'prepare':
    assert not RECOVERY.exists(), 'Reset already prepared; refuse duplicate operation'
    before=load()
    plan(before) # Validate before touching the running container.
    top=subprocess.check_output(['docker','top','ymca-tournament-bot','-eo','pid,args'],text=True)
    for m in before['Matches'].values():
        if m.get('TournamentId')=='T012' and m.get('Port'):
            lines=[l for l in top.splitlines() if 'Server.Name=YMCA Tournament '+m['Id']+' ' in l]
            print('BEFORE_CREDENTIAL_CHECK',m['Id'],'process_count',len(lines),'password_matches',bool(lines) and ('Server.Password='+m['Password']+' ') in lines[0], 'stored_join_dms',len(m.get('JoinDmMessageIds',{})))
    RECOVERY.mkdir(parents=True,mode=0o700)
    (RECOVERY/'before-stop.json').write_text(json.dumps(before))
    subprocess.run(['docker-compose','stop','tournament-bot'],check=True)
    before=load()
    (RECOVERY/'quiescent.json').write_text(json.dumps(before))
    s,new_id,retired=plan(before)
    tmp=STATE.with_suffix('.reset.tmp'); tmp.write_text(json.dumps(s,indent=2)); os.chmod(tmp,STATE.stat().st_mode & 0o777)
    os.chown(tmp,STATE.stat().st_uid,STATE.stat().st_gid); os.replace(tmp,STATE)
    (RECOVERY/'new-id.txt').write_text(new_id)
    print('RESET_PREPARED',new_id,'entrants',16,'series',8,'retired',retired)
    subprocess.run(['docker-compose','start','tournament-bot'],check=True)
elif mode == 'invite':
    s=load(); new_id=(RECOVERY/'new-id.txt').read_text(); t=s['Tournaments'][new_id]
    inspect=json.loads(subprocess.check_output(['docker','inspect','ymca-tournament-bot'],text=True))[0]
    env=dict(v.split('=',1) for v in inspect['Config']['Env'] if '=' in v)
    config=json.loads(pathlib.Path('tournament-bot.json').read_text())
    token=env.get('YMCA_TOURNAMENT_DISCORD_TOKEN') or config.get('discordToken')
    assert token
    base=config['joinPage']['publicBaseUrl'].rstrip('/')
    receipt_path=RECOVERY/'invitations.json'
    receipts=json.loads(receipt_path.read_text()) if receipt_path.exists() else {}
    decider=t['RandomMapByRound']['1']['Uid']
    maps=sorted([m for m in t['MapPool'] if len(t['MapPool'])<=1 or m['Uid'].lower()!=decider.lower()],key=lambda m:m['Title'])[:124]
    chunks=[maps[:24]]+[maps[i:i+25] for i in range(24,len(maps),25)]
    failures=[]
    for sid in t['SeriesIds']:
        series=s['Series'][sid]
        components=[]
        for row,chunk in enumerate(chunks):
            options=[{'label':m['Title'][:100],'value':m['Uid']} for m in chunk]
            if row==0: options.insert(0,{'label':"I don't care",'value':'__random__','description':'Let YMCA Overlord choose an available map'})
            components.append({'type':1,'components':[{'type':3,'custom_id':f'series-map:{sid}:{row}', 'placeholder':'Choose your map archetype','min_values':1,'max_values':1,'options':options}]})
        for pid,opp in [(series['PlayerOneDiscordId'],series['PlayerTwoDiscordId']),(series['PlayerTwoDiscordId'],series['PlayerOneDiscordId'])]:
            if str(pid) in receipts: continue
            p=s['Players'][str(pid)]; opponent=s['Players'][str(opp)]['OpenRaName']
            try:
                channel=discord_api(token,'POST','/users/@me/channels',{'recipient_id':str(pid)})['id']
                text=(f"**YMCA World Championship restarted — {new_id}**\nThe previous tournament T012 and its old match links are cancelled. Ignore all earlier match invitations.\n"
                    f"Your opponent: **{opponent}**\nYour player name: **{p['OpenRaName']}**\nRound 1 · BO3 · Series **{sid}**\n"
                    f"Please update to **YMCA v0.97.6**. Choose your map below (or I don't care). Once both players have chosen, the bot sends a NEW game invitation with the current password.\n"
                    f"Bracket: {base}/tournament/{new_id}")
                message=discord_api(token,'POST',f'/channels/{channel}/messages',{'content':text,'components':components,'allowed_mentions':{'parse':[]}})
                receipts[str(pid)]={'channel':channel,'message':message['id'],'series':sid}
                receipt_path.write_text(json.dumps(receipts,indent=2))
                print('INVITATION_DELIVERED',p['OpenRaName'],sid)
            except Exception as e:
                failures.append(p['OpenRaName']);print('INVITATION_FAILED',p['OpenRaName'],str(e))
    print('DELIVERED',len(receipts),'OF',len(t['Entrants']),'FAILURES',failures)
    if failures:sys.exit(1)
else:
    raise SystemExit('Unknown mode')
