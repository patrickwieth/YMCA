# Sprite Sentry machine gun — installed locally

SENTRY.SOVIET now uses its own SentryMG rather than the Flame Tower's
FireballLauncher (initial MG implementation used M60mg.Heavy). Five-round bursts,
5-tick shot gaps,30-tick reload,2000 damage per round:10000/50 ticks equals the
Guard Tower's six1000-damage warheads every30 ticks. Armor multipliers now match
GTChainGun too (110/75/100/30/60/10/5). Existing M60 and Guard weapons are unchanged.
MG42 sound, bullet impacts and the Sentry's4-cell MG range are retained; Guard
range remains6. Equal nominal sustained DPS is not identical practical performance
against moving targets, short exposures or overkill.
A five-frame gunfire2.shp muzzle flash atScale0.5 is added to the native sprite
sequence. The actor remains the separate non-buildable test/editor Sentry; the
normal FTUR, build menus, original artwork and test-map selection are unchanged.

Barrel registration comes from read-only background Blender projection of the
six bore centers in the frozen native32 render scene. LocalOffset1094,0,655,
CameraPitch85, original attachment0,0,112. Existing body/turret offsets and pixels
are untouched. All32 directions match the projected bore center within0.335px.
The main test map continues to place only the sprite Sentry, not the voxel study.

Runtime build and --check-sentry-v13-frames load the MG muzzle sprite, verify actor
dependencies, unchanged FTUR weapon and all32 muzzle positions. Native Sentry
regressions updated for explicitly authorized weapon/flash changes. Live firing
and subjective sound/flash review remain pending. Restart test-gebaeude.cmd.
No commit/push/deployment.
