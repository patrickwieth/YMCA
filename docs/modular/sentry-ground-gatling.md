# Sentry — China ground Gatling, no anti-air

User explicitly requested the same Gatling weapon as the Chinese Gatling Cannon,
without AA. Local `SENTRY.SOVIET` now inherits `^ChinaGatling`, removes its four air
armaments and their spin-up controller, and retains only ground stages
`ChinaMGatt.0G`, `.1G`, `.2G`, `.3G`. The old ordinary `Armament`/SentryMG binding is
removed. No copies/modifications of the Chinese weapon definitions were made.

- Four ground stages, reload12/8/4/2 ticks, existing350 damage/6-cell range and
  Chinese sound/armor tables directly reused.
- Shared China ground spin-up: required shots1,3,5, maximum3 stacks,40-tick cooldown,
  no reset on target switch, revoke all after idle. AttackTurreted selects only
  `secondary`; exactly one ground armament is enabled per stage.
- No AA weapon or AA spin-up controller exists on Sentry. Existing ground auto-target
  filters stay intact; grounded aircraft may still qualify as ground targets.
- Retains Sentry-specific `muzzle-mg` effect and barrel registration, unchanged art,
  pivot,turn speed, health, cost, ground footprint and1×1 bib. Chinese Gatling,
  Guard Tower and stock FTUR flame weapon are untouched.
- Sentry remains the separate test actor (not a new build-menu item).

The previous Sentry muzzle checker also assumed64px horizontally. Actual128×64
projection requires LocalOffset547,0,655 (instead of1094,0,655) and no classic
perspective fudge. This registration change accompanies the new weapon binding;
the same barrel point is used by all four stages. Engine error≤0.356px over32 facings.

`--check-sentry-v13-frames` now validates shared ground weapons, noAir target types,
exclusive stage conditions, spin-up/cooldown values, loaded muzzle/native frame
assets and actual-grid muzzle positions. The old Guard-equal-DPS assertion is
superseded by this explicit user-requested weapon change. Sprite/map/FTUR isolation
regressions pass. Compilation and engine checks ran in an isolated binary directory
so the user's running game was not interrupted; normal launcher builds/installations
refresh after closing the game. Live firing/spin-up review remains pending.

No commit/push/release.
