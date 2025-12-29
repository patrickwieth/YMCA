## Misc strings

normal = Normal

## Server Orders

custom-rules = This map contains custom rules. Game experience may change.
two-humans-required = This server requires at least two human players to start a match.
only-only-host-start-game = Only the host can start the game.
no-start-until-required-slots-full = Unable to start the game until required slots are full.
no-start-without-players = Unable to start the game with no players.
insufficient-enabled-spawnPoints = Unable to start the game until more spawn points are enabled.
malformed-command = Malformed { $command } command
state-unchanged-ready = Cannot change state when marked as ready.
invalid-faction-selected = Invalid faction selected: { $faction }
supported-factions = Supported values: { $factions }
state-unchanged-game-started = Cannot change state when game started. ({ $command })
requires-host = Only the host can do that.
invalid-bot-slot = Can't add bots to a slot with another client.
invalid-bot-type = Invalid bot type.
only-host-change-map = Only the host can change the map.
player-disconnected = { $player } has disconnected.
player-team-disconnected = { $player } (Team { $team }) has disconnected.
unknown-map = Map was not found on server.
searching-map = Searching for map on the Resource Center...
only-host-change-configuration = Only the host can change the configuration.
changed-map = { $player } changed the map to { $map }
value-changed = { $player } changed { $name } to { $value }.
temp-ban = { $admin } temporarily banned { $player } from the server.
only-host-transfer-admin = Only admins can transfer admin to another player.
only-host-move-spectators = Only the host can move players to spectators.
empty-slot = No-one in that slot.
move-spectators = { $admin } moved { $player } to spectators.
nick = { $player } is now known as { $name }.
player-dropped = A player has been dropped after timing out.
connection-problems = { $player } is experiencing connection problems.
timeout = { $player } has been dropped after timing out.
timeout-in =
    { $timeout ->
        [one] { $player } will be dropped in { $timeout } second.
       *[other] { $player } will be dropped in { $timeout } seconds.
    }
error-game-started = The game has already started.
requires-password = Server requires a password.
incorrect-password = Incorrect password.
incompatible-mod = Server is running an incompatible mod.
incompatible-version = Server is running an incompatible version.
incompatible-protocol = Server is running an incompatible protocol.
banned = You have been banned from the server.
temp-banned = You have been temporarily banned from the server.
full = The game is full.
joined = { $player } has joined the game.
new-admin = { $player } is now the admin.
option-locked = { $option } cannot be changed.
invalid-configuration-command = Invalid configuration command.
admin-option = Only the host can set that option.
number-teams = Number of teams could not be parsed: { $raw }
admin-clear-spawn = Only admins can clear spawn points.
spawn-occupied = You cannot occupy the same spawn point as another player.
spawn-locked = The spawn point is locked to another player slot.
player-color-terrain = Color was adjusted to be less similar to the terrain.
player-color-player = Color was adjusted to be less similar to another player.
invalid-player-color = Unable to determine a valid player color. A random color has been selected.
invalid-error-code = Failed to parse error message.
game-offline = Game has not been advertised online.
no-port-forward = Server port is not accessible from the internet.
blacklisted-title = Server name contains a blacklisted word.
requires-forum-account = Server requires players to have an OpenRA forum account.
no-permission = You do not have permission to join this server.
slot-closed = Your slot was closed by the host.

## Server

game-started = Game started

## ActorEditLogic

duplicate-actor-id = Duplicate Actor ID
enter-actor-id = Enter an Actor ID
owner = Owner

## ActorSelectorLogic

type = Type

## CommonSelectorLogic

search-results = Search Results
multiple = Multiple

## SaveMapLogic

unpacked = unpacked
save-map-failed-title = Failed to save map
save-map-failed-prompt = See debug.log for details.
save-map-failed-accept = OK
overwrite-map-failed-title = Warning
overwrite-map-failed-prompt =
    By saving you will overwrite
    an already existing map.
overwrite-map-failed-confirm = Save
overwrite-map-outside-edit-title = Warning
overwrite-map-outside-edit-prompt =
    "The map has been edited from outside the editor.
    By saving you may overwrite progress
overwrite-map-outside-edit-confirm = Save

## GameInfoLogic

objectives = Objectives
briefing = Briefing
options = Options
debug = Debug

## GameInfoObjectivesLogic, GameInfoStatsLogic

in-progress = In progress
accomplished = Accomplished
failed = Failed

## GameInfoStatsLogic

mute = Mute this player
unmute = Unmute this player

## GameInfoStatsLogic

gone = Gone

## GameTimerLogic

paused = Paused
max-speed = Max Speed
speed = { $percentage }% Speed
complete = { $percentage }% complete

## IngameMenuLogic

leave = Leave
abort-mission = Abort Mission
leave-mission-title = Leave Mission
leave-mission-prompt = Leave this game and return to the menu?
leave-mission-accept = Leave
leave-mission-cancel = Stay
restart-button = Restart
restart-mission-title = Restart
restart-mission-prompt = Are you sure you want to restart?
restart-mission-accept = Restart
restart-mission-cancel = Stay
surrender-button = Surrender
surrender-title = Surrender
surrender-prompt = Are you sure you want to surrender?
surrender-accept = Surrender
surrender-cancel = Stay
load-game-button = Load Game
save-game-button = Save Game
music-button = Music
settings-button = Settings
return-to-map = Return to map
resume = Resume
save-map-button = Save Map
error-max-player-title = Error: Max player count exceeded
error-max-player-prompt = There are too many players defined ({ $players }/{ $max }).
error-max-player-accept = Back
exit-map-button = Exit Map Editor
exit-map-editor-title = Exit Map Editor
exit-map-editor-prompt-unsaved = Exit and lose all unsaved changes?
exit-map-editor-prompt-deleted = The map may have been deleted outside the editor.
exit-map-editor-confirm-anyway = Exit anyway
exit-map-editor-confirm = Exit

## IngamePowerBarLogic

## IngamePowerCounterLogic

power-usage = Power Usage: { $usage }/{ $capacity }
infinite-power = Infinite

## IngameSiloBarLogic

## IngameCashCounterLogic

silo-usage = Silo Usage: { $usage }/{ $capacity }

## ObserverShroudSelectorLogic

camera-option-all-players = All Players
camera-option-disable-shroud = Disable Shroud
camera-option-other = Other

## ObserverStatsLogic

information-none = Information: None
basic = Basic
economy = Economy
production = Production
support-powers = Support Powers
combat = Combat
army = Army
earnings-graph = Earnings (graph)
army-graph = Army (graph)

## WorldTooltipLogic

unrevealed-terrain = Unrevealed Terrain

## DownloadPackageLogic

downloading = Downloading { $title }
fetching-mirror-list = Fetching list of mirrors...
downloading-from = Downloading from { $host } { $received } { $suffix }
downloading-from-progress = Downloading from { $host } { $received } / { $total } { $suffix } ({ $progress }%)
unknown-host = unknown host
verifying-archive = Verifying archive...
archive-validation-failed = Archive validation failed
extracting = Extracting...
extracting-entry = Extracting { $entry }
archive-extraction-failed = Archive extraction failed
mirror-selection-failed = Online mirror is not available. Please install from an original disc.

## InstallFromDiscLogic

detecting-drives = Detecting drives
checking-discs = Checking Discs
searching-disc-for = Searching for { $title }
content-package-installation = The following content packages will be installed:
game-discs = Game Discs
digital-installs = Digital Installs
game-content-not-found = Game Content Not Found
alternative-content-sources = Please insert or install one of the following content sources:
installing-content = Installing Content
copying-filename = Copying { $filename }
copying-filename-progress = Copying { $filename } ({ $progress }%)
installation-failed = Installation Failed
check-install-log = Refer to install.log in the logs directory for details.
extracting-filename = Extracting { $filename }
extracting-filename-progress = Extracting { $filename } ({ $progress }%)
cancel = Cancel
retry = Retry

## InstallFromDiscLogic, LobbyLogic

back = Back
# InstallFromDiscLogic, ModContentPromptLogic
continue = Continue

## ModContentLogic

manual-install = Manual Install

## ModContentPromptLogic

quit = Quit

## LobbyLogic

add = Add
remove = Remove
configure-bots = Configure Bots
n-teams = { $count } Teams
humans-vs-bots = Humans vs Bots
free-for-all = Free for all
configure-teams = Configure Teams

## LobbyLogic, CommonSelectorLogic, InGameChatLogic

all = All

## InputSettingsLogic, CommonSelectorLogic

none = None

## LobbyLogic, IngameChatLogic

team = Team

## LobbyOptionsLogic

not-available = Not Available

## LobbyUtils

slot = Slot
open = Open
closed = Closed
bots = Bots
# LobbyUtils, Server
bots-disabled = Bots Disabled

## MapPreviewLogic

connecting = Connecting...
downloading-map = Downloading { $size } kB
downloading-map-progress = Downloading { $size } kB ({ $progress }%)
retry-install = Retry Install
retry-search = Retry Search

## also MapChooserLogic

created-by = Created by { $author }

## SpawnSelectorTooltipLogic

disabled-spawn = Disabled spawn
available-spawn = Available spawn

## DisplaySettingsLogic

close = Close
medium = Medium
far = Far
furthest = Furthest
windowed = Windowed
legacy-fullscreen = Fullscreen (Legacy)
fullscreen = Fullscreen
display = Display { $number }
show-on-damage = Show On Damage
always-show = Always Show
automatic = Automatic
manual = Manual

## DisplaySettingsLogic, InputSettingsLogic

disabled = Disabled

## DisplaySettingsLogic, InputSettingsLogic, IntroductionPromptLogic

classic = Classic
modern = Modern
standard = Standard

## DisplaySettingsLogic, IntroductionPromptLogic

inverted = Inverted
joystick = Joystick
alt = Alt
ctrl = Ctrl
meta = Meta
shift = Shift

## SettingsLogic

settings-save-title = Restart Required
settings-save-prompt =
    Some changes will not be applied until
    the game is restarted.
settings-save-cancel = Continue
restart-title = Restart Now?
restart-prompt =
    Some changes will not be applied until
    the game is restarted. Restart now?
restart-accept = Restart Now
restart-cancel = Restart Later
reset-title = Reset { $panel }
reset-prompt =
    Are you sure you want to reset
    all settings in this panel?
reset-accept = Reset
reset-cancel = Cancel

## AssetBrowserLogic

all-packages = All Packages
length-in-seconds = { $length } sec

## ConnectionLogic

connecting-to-endpoint = Connecting to { $endpoint }...
could-not-connect-to-target = Could not connect to { $target }
unknown-error = Unknown error
password-required = Password Required
connection-failed = Connection Failed
mod-switch-failed = Failed to switch mod.

## GameSaveBrowserLogic

rename-save-title = Rename Save
rename-save-prompt = Enter a new file name:
rename-save-accept = Rename
delete-save-title = Delete selected game save?
delete-save-prompt = Delete '{ $save }'
delete-save-accept = Delete
delete-all-saves-title = Delete all game saves?
delete-all-saves-prompt =
    { $count ->
        [one] Delete { $count } save.
       *[other] Delete { $count } saves.
    }
delete-all-saves-accept = Delete All
save-deletion-failed = Failed to delete save file '{ $savePath }'. See the logs for details.
overwrite-save-title = Overwrite saved game?
overwrite-save-prompt = Overwrite { $file }?
overwrite-save-accept = Overwrite

## MainMenuLogic

loading-news = Loading news
news-retrival-failed = Failed to retrieve news: { $message }
news-parsing-failed = Failed to parse news: { $message }

## MapChooserLogic

all-maps = All Maps
no-matches = No matches
player-players =
    { $players ->
        [one] { $players } Player
       *[other] { $players } Players
    }
delete-map-title = Delete map
delete-map-prompt = Delete the map '{ $title }'?
delete-map-accept = Delete
delete-all-maps-title = Delete maps
delete-all-maps-prompt = Delete all maps on this page?
delete-all-maps-accept = Delete
order-maps-players = Players
order-maps-date = Map Date

## MissionBrowserLogic

no-video-title = Video not installed
no-video-prompt =
    The game videos can be installed from the
    "Manage Content" menu in the mod chooser.
no-video-cancel = Back
cant-play-title = Unable to play video
cant-play-prompt = Something went wrong during video playback.
cant-play-cancel = Back

## MusicPlayerLogic

sound-muted = Audio has been muted in settings.
no-song-playing = No song playing

## MuteHotkeyLogic

audio-muted = Audio muted.
audio-unmuted = Audio unmuted.

## PlayerProfileLogic

loading-player-profile = Loading player profile...
loading-player-profile-failed = Failed to load player profile.

## ReplayBrowserLogic

duration = Duration: { $time }
singleplayer = Singleplayer
multiplayer = Multiplayer
victory = Victory
defeat = Defeat
today = Today
last-week = Last 7 days
last-fortnight = Last 14 days
last-month = Last 30 days
replay-duration-very-short = Under 5 min
replay-duration-short = Short (10 min)
replay-duration-medium = Medium (30 min)
replay-duration-long = Long (60+ min)
rename-replay-title = Rename Replay
rename-replay-prompt = Enter a new file name:
rename-replay-accept = Rename
delete-replay-title = Delete selected replay?
delete-replay-prompt = Delete replay { $replay }?
delete-replay-accept = Delete
delete-all-replays-title = Delete all selected replays?
delete-all-replays-prompt =
    { $count ->
        [one] Delete { $count } replay.
       *[other] Delete { $count } replays.
    }
delete-all-replays-accept = Delete All
replay-deletion-failed = Failed to delete replay file '{ $file }'. See the debug.log file for details.

## ReplayUtils

incompatible-replay-title = Incompatible Replay
incompatible-replay-prompt = Replay metadata could not be read.
incompatible-replay-accept = OK
-incompatible-replay-recorded = It was recorded with
incompatible-replay-unknown-version = { -incompatible-replay-recorded } an unknown version.
incompatible-replay-unknown-mod = { -incompatible-replay-recorded } an unknown mod.
incompatible-replay-unavailable-mod = { -incompatible-replay-recorded } an unavailable mod: { $mod }.
incompatible-replay-incompatible-version =
    { -incompatible-replay-recorded } an incompatible version:
    { $version }.
incompatible-replay-unavailable-map =
    { -incompatible-replay-recorded } an unavailable map:
    { $map }.

## ServerListLogic

players-online =
    { $players ->
        [one] { $players } Player Online
       *[other] { $players } Players Online
    }
search-status-failed = Failed to query server list.
search-status-no-games = No games found. Try changing filters.
players-label =
    { $players ->
        [0] No Players
        [one] One Player
       *[other] { $players } Players
    }
bots-label =
    { $bots ->
        [0] No Bots
        [one] One Bot
       *[other] { $bots } Bots
    }

## ServerListLogic, ReplayBrowserLogic, ObserverShroudSelectorLogic

players = Players

## ServerListLogic, GameInfoStatsLogic

spectators = Spectators
spectators-label =
    { $spectators ->
        [0] No Spectators
        [one] One Spectator
       *[other] { $spectators } Spectators
    }

## ServerlistLogic, GameInfoStatsLogic, ObserverShroudSelectorLogic, SpawnSelectorTooltipLogic, ReplayBrowserLogic

team-number = Team { $team }
no-team = No Team
playing = Playing
waiting = Waiting
n-other-players =
    { $players ->
        [one] One other player
       *[other] { $players } other players
    }
in-progress-for =
    { $minutes ->
        [0] In progress
        [one] In progress for { $minutes } minute.
       *[other] In progress for { $minutes } minutes.
    }
password-protected = Password protected
waiting-for-players = Waiting for players

## Game

saved-screenshot = Saved screenshot { $filename }

## ChatCommands

invalid-command = { $name } is not a valid command.

## DebugVisualizationCommands

combat-geometry-description = toggles combat geometry overlay.
render-geometry-description = toggles render geometry overlay.
screen-map-overlay-description = toggles screen map overlay.
depth-buffer-description = toggles depth buffer overlay.
actor-tags-overlay-description = toggles actor tags overlay.

## DevCommands

cheats-disabled = Cheats are disabled.
invalid-cash-amount = Invalid amount of cash.
toggle-visibility = toggles visibility checks and minimap.
give-cash = gives the default or specified amount of money.
give-cash-all = gives the default or specified amount of money to all players and ai.
instant-building = toggles instant building.
build-anywhere = toggles the ability to build anywhere.
unlimited-power = toggles infinite power.
enable-tech = toggles the ability to build everything.
dev-cheat-all = toggles all cheats and gives you some cash for your trouble.
dev-crash = crashes the game.
levelup-actor = adds a specified number of levels to the selected actors.
player-experience = adds a specified amount of player experience to the owner(s) of selected actors.
power-outage = causes owner(s) of selected actors to have a 5 second power outage.
kill-selected-actors = kills selected actors.
dispose-selected-actors = disposes selected actors.

## HelpCommands

available-commands = Here are the available commands:
no-description = no description available.
help-description = provides useful info about various commands

## PlayerCommands

pause-description = pause or unpause the game
surrender-description = self-destruct everything and lose the game

## DeveloperMode

cheat-used = Cheat used: { $cheat } by { $player }{ $suffix }

## CustomTerrainDebugOverlay

custom-terrain-debug-overlay-description = toggles the custom terrain debug overlay.

## CellTriggerOverlay

cell-trigger-overlay-description = toggles the script triggers overlay.

## ExitsDebugOverlay

exits-debug-overlay-description = Displays exits for factories.

## HierarchicalPathFinderOverlay

hpf-overlay-description = toggles the hierarchical pathfinder overlay.

## PathFinderOverlay

path-debug-description = toggles a visualization of path searching.

## TerrainGeometryOverlay

terrain-geometry-overlay = toggles the terrain geometry overlay.

### aircraft-aurora

aircraft-aurora =
    .buildable-description =
        Supersonic bomber armed with the Mother of all Bombs.
        Strong vs Buildings, Light Vehicles, Infantry
        Weak vs Aircraft

### aircraft-dropship

aircraft-dropship =
    .buildable-description =
        Superheavy cargo lifter plane.
        Brings vehicles to the battlefield.
    .tooltipextras-attributes =
        Attributes: • Transports up to 8 Vehicles
        • Can Paradrop Vehicles (use force fire for drops)
        • Vehicles can be produced into Dropships when fyling over factories

### aircraft-ocar

aircraft-ocar =
    .buildable-description =
        Fast VTOL Vehicle Transporter.
        Unarmed

### aircraft-stealth-fighter

aircraft-stealth-fighter =
    .buildable-description =
        Stealth Ground-Attack Plane.
        Drops laser-guided armor piercing bombs. Strong vs Buildings, Heavy Vehicles
         Weak vs Infantry, Aircraft

### aircraft-stealth-fighter-payload

aircraft-stealth-fighter-payload =
    .buildable-description =
        Stealth Ground-Attack Plane.
        Drops laser-guided armor piercing bombs.
         Strong vs Buildings, Heavy Vehicles
         Weak vs Infantry, Aircraft

### aircraft-tran

aircraft-tran =
    .buildable-description =
        Fast infantry transport helicopter.
        Unarmed

### aircraft-tran-eagle

aircraft-tran-eagle =
    .buildable-description =
        Fast infantry transport helicopter.
        Infantry can fire from weapon ports.
        Is loaded with 4 Rocket Infantry.

### aircraft-yak

aircraft-yak =
    .buildable-description =
        Attack Plane armed with
        dual machine guns.
          Strong vs Infantry, Light armor
          Weak vs Tanks, Aircraft

### civilian-bio

civilian-bio =
    .tooltipdescription-at-ally-description = Provides prerequisite for Bio-Lab units.
    .tooltipdescription-at-other-description = Capture to produce Bio-Lab units.

### civilian-fcom

civilian-fcom =
    .tooltipdescription-at-ally-description = Provides buildable area.
    .tooltipdescription-at-other-description = Capture to give buildable area.

### civilian-hosp

civilian-hosp =
    .tooltipdescription-at-ally-description = Provides infantry with self-healing.
    .tooltipdescription-at-other-description = Capture to enable self-healing for infantry.

### civilian-miss

civilian-miss =
    .tooltipdescription-at-ally-description = Provides range of vision.
    .tooltipdescription-at-other-description = Capture to give visual range.

### civilian-oilb

civilian-oilb =
    .tooltipdescription-at-ally-description = Provides additional funds.
    .tooltipdescription-at-other-description = Capture to receive additional funds.
commander-tree-commander-tree =
    .description-48 =
        Enables to build the Hover MLRS.
        The Hover MLRS improves the MLRS' agility and gives it high explosive rockets.
    .description-49 = Upgrades the Medium Tank with a Point Laser Defense System.
    .description-50 =
        Equips the Medium Tank with Reflector Armor.
        Very resistent vs. Energy Weapons.
    .description-51 = Enables to build the Mobile EMP unit.
    .description-52 = Upgrades the EMP reactor to allow multiple EMP Waves and don't self destruct on use.
    .description-53 = Upgrades the EMP reactor to set off an EMP blast when the vehicle is destroyed.
    .description-54 =
        Upgrades the Titan with EMP shells.
        EMP Shells disable the target on impact.
    .description-55 =
        Upgrades the Titan with a Railgun.
        The Railgun is strong vs. tanks and is an energy based weapon.
    .description-56 = Upgrades the Titan with a Point Laser Defense System to shoot down incoming projectiles.
    .description-57 =
        Equips the Titan with Reflector Armor.
        Very resistent vs. Energy Weapons.
    .description-58 = All heavy tanks are produced with a mini drone attached.
    .description-59 =
        Enables to build the Stealth Fighter.
        It is a cloaked aircraft optimized to take out single heavy armored vehicles.
    .description-60 =
        Doubles the Payload of the Stealth Fighter.
        It allows the Stealth Fighter to take out a second target without resuppyling.
    .description-61 =
        Enables to build the Mammoth MK.II.
        It is the epic walker of Talon.
        Armed with dual railguns and high explosive rockets against ground and air.
    .description-62 =
        Enables to build the Orca Ion Warship.
        It is the epic airship of Eagle.
        Comes with ion drones delivering ion strikes vs. ground
        and heavy missiles vs. air.
    .description-63 =
        Enables to build the Mammoth Armored Reclamation Vehicle.
        It is the epic tanks of Zocom.
        Armed with a triple ion cannon.
        Mines Tiberium and Ores when driving over.
    .description-64 =
        Enables to build the Reinforcements Coordinator.
        It coordinates reinforcements directly to the frontline when deployed.
    .description-65 = Upgrades the Buggy with a Point Laser Defense system to shoot down incoming projectiles.
    .description-66 = Upgrades the Buggy with an Anti-Air Laser to shoot down aircraft.
    .description-67 = Enables to build the SSM Launcher, a high range artillery.
    .description-68 =
        Upgrades the SSM Launcher with chemical missiles.
        The chemical clouds remain for a prolonged time on the impact site dealing damage.
    .description-69 =
        Upgrades the SSM Launcher's missiles to split up in multiple missiles on impact.
        This makes the SSM Launcher more effective against large groups of vehicles.
    .description-70 =
        Upgrades the Stealth Tank with a Armor Piercing Missiles.
        This makes it much more effective vs. heavy tanks and heavy aircraft.
    .description-71 =
        Upgrades the Stealth Tank with a Scrin Weapon.
        Energy based weapons cannot be intercepted by Point Laser Defense.
    .description-72 = Upgrades the Stealth Tank's missiles with a high explosive load.
    .description-73 =
        Equips the Stealth Tank with Reflector Armor.
        Very resistent vs. Energy Weapons.
    .description-74 = Upgrades the Stealth Tank with a Point Laser Defense System to shoot down incoming projectiles.
    .description-75 =
        Upgrades the Recon Bike with a Scrin Weapon.
        Energy based weapons cannot be intercepted by Point Laser Defense.
    .description-76 = Upgrades the Scrin Bike with a Point Laser Defense System to shoot down incoming projectiles.
    .description-77 =
        Equips the Scrin Bike with Reflector Armor.
        Very resistent vs. Energy Weapons.
    .description-78 =
        Upgrades the Recon Bike to shoot more missiles at high range.
        This makes the recon bike a hit and run artillery unit.
        The recon bike loses its ability to fight flying targets.
    .description-79 =
        Equips the Rocket Hail Bike with Reflector Armor.
        Very resistent vs. Energy Weapons.
    .description-80 = Upgrades the Rocket Hail Bike with a Point Laser Defense System to shoot down incoming projectiles.
    .description-81 =
        Upgrades the Recon Bike to shoot explosive missiles.
        This is very strong vs. weakly armored units.
    .description-82 = Upgrades the Explosive Rocket Bike with a Point Laser Defense System to shoot down incoming projectiles.
    .description-83 =
        Equips the Explosive Rocket Bike with Reflector Armor.
        Very resistent vs. Energy Weapons.
    .description-84 =
        Unlocks a small hovering scorpion tank armed with heavy laser.
        Energy based weapons cannot be intercepted by Point Laser Defense.
    .description-85 =
        Enables to build the Beam Cannon.
        It shoots a continuous energy beam at distant targets Strong vs. Vehicles, Weak vs. Aircraft.
    .description-86 = The Subchaser is a small naval vessel specialized in hunting submarines.
    .description-87 =
        The Hunter Submarine is a fast and destructive submerged vessel.
        It lacks armor, but can take out unprotected ships very efficiently.
    .description-88 =
        The Patrol Boat is a versatile rocket vessel.
        It comes with Point-Defense-Laser to stop incoming projectiles.
    .description-89 = The Recon Boat is a fast vessel armed with missiles.
    .description-90 =
        The Sea Scorpion is a flak boat specialized in fighting aircraft.
        It comes with Point-Defense-Laser to stop incoming projectiles.
    .description-91 =
        The Attack Submarine is specialized in fighting other ships.
        It is submerged and cannot be detected by all ships.
    .description-92 =
        The Destroyer is capable of fighting against all threats,
        but is mostly specialized against submarines.
    .description-93 =
        The Frigate brings a heavy energy weapon to the seas.
        This makes it suited best against Point Defense Laser systems.
    .description-94 =
        The Aegis Cruiser fires a barrage of missiles.
        The missiles are best suited against aircraft.
        Has Point Defense Laser system.

### commander-tree-promotion-advanced-infantry-training

commander-tree-promotion-advanced-infantry-training =
    .buildable-description =
        Funds advanced academies so every faction can field elite infantry specialists once the upgrade is complete.
        Unlocks high-tech squads, medics, and commando options across all aligned barracks.
    .description =
        Commits command points to a global infantry doctrine that certifies every barracks to produce late-tier troops.
        This is a prerequisite for most Tier 3 infantry promotions, so purchase it before planning specialized squads.
    .tooltipextras-strengths = Strengths: • Unlocks elite infantry (commandos, medics, specialists) across every aligned barracks.
    .tooltipextras-weaknesses = Weaknesses: • Provides no immediate stat buff—units still cost resources and training time.
    .tooltipextras-attributes = Attributes: • Required before purchasing most Tier 3 infantry promotions in Allied, Soviet, Nod, GDI, and Scrin trees.

### commander-tree-promotion-air-logistics

commander-tree-promotion-air-logistics =
    .buildable-description = Enables to build the Chinook, Carryall and Dropship.
    .description =
        Enables to build the Chinook, Carryall and Dropship.
    .tooltipextras-strengths = Strengths: • Strong with mobility strategies that redeploy units or harvesters by air.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs. opponents who invest heavily into anti-air defenses.
    .tooltipextras-attributes = Attributes: • Grants Chinook, Carryall, and Dropship for ferrying troops, vehicles, and supply crates.

### commander-tree-promotion-battlecruiser

commander-tree-promotion-battlecruiser =
    .buildable-description =
        The Battlecruiser is the ultimate artillery ship.
        Dual 8-inch cannons provide constant barrage,
        while a Point Defense Laser fends of incoming enemy artillery.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Aircraft, Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser
    .description =
        The Battlecruiser is the ultimate artillery ship.
         Dual 8-inch cannons provide constant barrage,
         while a Point Defense Laser fends of incoming enemy artillery.

### commander-tree-promotion-btr

commander-tree-promotion-btr =
    .buildable-description =
        Enables to build the BTR.
          It is a missile based anti-air troop transporter.
        Optimal for taking out heavy air units.
    .description =
        Enables to build the BTR.
          It is a missile based anti-air troop transporter.
        Optimal for taking out heavy air units.

### commander-tree-promotion-carrier

commander-tree-promotion-carrier =
    .buildable-description =
        The Carrier launches small strike aircraft.
        The aircraft are able to fight submarines as well as all ground targets.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Aircraft, Aircraft
    .description =
        The Carrier launches small strike aircraft.
        The aircraft are able to fight submarines as well as all ground targets.
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.

### commander-tree-promotion-flak-track-barrage

commander-tree-promotion-flak-track-barrage =
    .buildable-description =
        Upgrades the Flak Track with fast firing Flak cannons.
        This makes it better vs. Point Laser Defense Systems and groups of units.
    .description =
        Upgrades the Flak Track with fast firing Flak cannons.
          This makes it better vs. Point Laser Defense Systems and groups of units.

### commander-tree-promotion-icbm-submarine

commander-tree-promotion-icbm-submarine =
    .buildable-description = The ICBM Submarine can fire Nukes over a very long distance.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Aircraft, Aircraft, Submarines
    .description = The ICBM Submarine can fire Nukes over a very long distance.

### commander-tree-promotion-missile-submarine

commander-tree-promotion-missile-submarine =
    .buildable-description = The Missile Submarine launches a barrage of missiles with very high range.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Submarines
    .description = The Missile Submarine launches a barrage of missiles with very high range.

### commander-tree-promotion-oil-pumps

commander-tree-promotion-oil-pumps =
    .buildable-description =
        Enables to build Oil Pumps, Oil Platforms, and Supply Trucks.
        Generates slow trickling endless income.
    .description =
        Enables to build Oil Pumps, Oil Platforms, and Supply Trucks.
          Generates slow trickling endless income.
    .tooltipextras-strengths = Strengths: • Strong with long macro games that reward steady passive income.
    .tooltipextras-weaknesses = Weaknesses: • Weak when oil derricks are exposed and easy for raiders to destroy.
    .tooltipextras-attributes = Attributes: • Grants Oil Pumps, Offshore Platforms, and Supply Trucks that generate infinite trickle credits.

### commander-tree-promotion-point-defense-turret

commander-tree-promotion-point-defense-turret =
    .buildable-description =
        Enables to build the Point Laser Defense Turret.
        It shoots down incoming shells and missiles.
    .description =
        Enables to build the Point Laser Defense Turret.
        It shoots down incoming shells and missiles.
    .tooltipextras-strengths = Strengths: • Strong vs. rockets, shells, and aircraft-launched missiles.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs. beam weapons and direct-fire assaults.
    .tooltipextras-attributes = Attributes: • Has a laser battery that needs to recharge from time to time.

### defaults-forceshieldsupportpower

defaults-forceshieldsupportpower =
    .grantexternalconditionpowerca-at-fshield-description =
        Makes selected friendly structures temporarily invulnerable.
        Warning: Causes power failure.
    .grantexternalconditionpowerca-at-fshieldnukular-description =
        Makes selected friendly structures temporarily invulnerable.
        Warning: Causes power failure.

### fakes-domf

fakes-domf =
    .buildable-description = Looks like a Radar Dome.

### fakes-facf

fakes-facf =
    .buildable-description = Looks like a Construction Yard.

### fakes-syrf

fakes-syrf =
    .buildable-description = Looks like a Naval yard.

### fakes-weaf

fakes-weaf =
    .buildable-description = Looks like a War Factory.

### infantry-brute

infantry-brute =
    .buildable-description =
        Genetically engineered hulk.
        Strong vs Vehicles
        Weak vs Aircraft, Defenses

### infantry-dog

infantry-dog =
    .buildable-description =
        Anti-infantry unit.
        Can detect spies and cloaked units.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft

### infantry-engineer

infantry-engineer =
    .buildable-description =
        Unarmed battle field engineer.
        The most important tasks of the engineer are to take over enemy structures and do push ups.

### infantry-leader

infantry-leader =
    .buildable-description =
        Nod Rebel Leader.
        Newly trained infantry enters the game from the closest map edge
         as long as no primary infantry building is set.
         Does not work with water.

### infantry-medic

infantry-medic =
    .buildable-description =
        Heals nearby infantry.
        Unarmed

### infantry-chemwarrior

infantry-chemwarrior =
    .buildable-description =
        Advanced general-purpose infantry.
        Strong vs all Ground Units

### infantry-seal

infantry-seal =
    .buildable-description =
        Elite infantry unit. Armed with
        a SMG and C4.
          Can detect cloaked units.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Aircraft, Vehicles
    .tooltipextras-attributes =
        Attributes: • Can swim
        • C4 Explosives

### infantry-shad-commando

infantry-shad-commando =
    .buildable-description =
        Commando unit with emp grenades, cloaked and with heavy anti-tank mines.
        Strong vs Infantry
        Weak vs Aircraft, Defenses

### player-player

player-player =
    .lobbyprerequisitecheckbox-at-autobattle-description = Enables Autobattle Mode - Checkpoints must be captured to win the game and most units cannot be controlled. THIS IS HIGHLY EXPERIMENTAL! Only works on special maps.
    .lobbyprerequisitecheckbox-at-balancedharvesting-description = Enables dynamic harvester speed to account for the direction of resources relative to refineries.
    .lobbyprerequisitecheckbox-at-forceshield-description = Grants all factions the Force Shield support power ability.
    .lobbyprerequisitecheckbox-at-globalbounty-description = Players receive cash bonuses when killing enemy units.
    .lobbyprerequisitecheckbox-at-globalfactundeploy-description = Allow undeploying Construction Yard.
    .lobbyprerequisitecheckbox-at-nonaval-description = Enable or Disable Naval Units.
    .lobbyprerequisitecheckbox-at-nukularconyard-description = Enables Nukular MCV Mode - The first conyard of each player is Nukular, this means if you lose it, you lose the game and it will explode in a gigantic nuclear detonation. Great for FFA.
    .lobbyprerequisitecheckbox-at-oilp-description = Players can build oil pumps for infinite income scaling.
    .lobbyprerequisitecheckbox-at-radaron-description = If activated players always see their radar.
    .lobbyprerequisitecheckbox-at-revealonfire-description = Units reveal themselves when firing.
    .lobbyprerequisitecheckbox-at-superw-description = Enable or Disable Super Weapons.
    .lobbyprerequisitecheckbox-at-swiss-bank-description = Players can build Swiss Banks to transfer cash to other players steadily.

### scrin-bastion

scrin-bastion =
    .buildable-description =
        Defense platform for Scrin zapping incoming invaders.
        Maximum 1 can be built.

### scrin-deva

scrin-deva =
    .buildable-description =
        Long-range siege warship.
        Strong vs Buildings, Defenses, Infantry
        Weak vs Aircraft, Tanks

### scrin-deva-rift

scrin-deva-rift =
    .buildable-description =
        Long-range anti-tank warship.
        Strong vs masses of units
        Weak vs Aircraft

### scrin-deva-shards

scrin-deva-shards =
    .buildable-description =
        Long-range anti-tank warship.
        Strong vs Tanks
        Weak vs Aircraft

### scrin-grav

scrin-grav =
    .buildable-description = Produces Scrin aircraft.

### scrin-mani

scrin-mani =
    .buildable-description =
        Slows enemy unit movement and rate of fire.
        Requires power to operate.
        Maximum 1 can be built.
        Special Ability: Suppression Field

### scrin-mast

scrin-mast =
    .buildable-description =
        Elite specialist infantry.
        Can mind-control enemy units and defenses.
        Can detect cloaked units.
        Maximum 1 can be trained.
        Strong vs Infantry, Vehicles, Defenses
        Weak vs Aircraft

### scrin-mshp

scrin-mshp =
    .buildable-description =
        Huge craft with powerful beam weapon.
        Can create wormholes.
        Strong vs Buildings, Defenses
        Weak vs Aircraft

### scrin-nerv

scrin-nerv =
    .buildable-description =
        Provides an overview of the battlefield.
        Requires power to operate.

### scrin-port

scrin-port =
    .buildable-description = Produces infantry.

### scrin-proc-scrin

scrin-proc-scrin =
    .buildable-description =
        Processes raw Tiberium, Ore and Gems
        into credits.

### scrin-ptur

scrin-ptur =
    .buildable-description =
        Anti-infantry base defense.
        Can detect cloaked units.
        Strong vs Infantry, Light Armor
        Weak vs Tanks, Aircraft

### scrin-rea2

scrin-rea2 =
    .buildable-description =
        Provides double the power of
        a standard Reactor.

### scrin-reac

scrin-reac =
    .buildable-description = Provides power for other structures.

### scrin-rift

scrin-rift =
    .buildable-description =
        Provides Rift support power.
        Requires power to operate.
        Maximum 1 can be built.
        Special Ability: Rift

### scrin-s1

scrin-s1 =
    .buildable-description =
        General-purpose scouting infantry.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft

### scrin-s2

scrin-s2 =
    .buildable-description =
        Fast assault infantry.
        Strong vs Vehicles, Infantry
        Weak vs Aircraft

### scrin-s3

scrin-s3 =
    .buildable-description =
        Anti-tank/anti-aircraft infantry.
        Strong vs Tanks, Aircraft
        Weak vs Infantry

### scrin-s4

scrin-s4 =
    .buildable-description =
        Heavy assault infantry.
        Can teleport short distances.
        Strong vs Buildings, Defenses, Light Vehicles
        Weak vs Infantry, Aircraft

### scrin-s6

scrin-s6 =
    .buildable-description =
        Captures
        enemy structures.
          Can repair friendly structures & bridges.
          Special Ability: Repair
          Unarmed

### scrin-s7

scrin-s7 =
    .buildable-description =
        General-purpose scouting infantry.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft

### scrin-s8

scrin-s8 =
    .buildable-description =
        General-purpose scouting infantry.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft

### scrin-scol

scrin-scol =
    .buildable-description =
        Advanced base defense.
        Requires power to operate.
        Can detect cloaked units.
        Strong vs Infantry, Vehicles
        Weak vs Aircraft

### scrin-scrt

scrin-scrt =
    .buildable-description = Provides Scrin advanced technologies.

### scrin-sfac

scrin-sfac =
    .buildable-description = Produces structures.

### scrin-shar

scrin-shar =
    .buildable-description =
        Anti-aircraft base defense.
        Requires power to operate.
        Strong vs Aircraft
        Weak vs Ground Units

### scrin-sign

scrin-sign =
    .buildable-description =
        Provides Scrin tier 4 technologies.
        Maximum 1 can be built.
        Cannot be captured.

### scrin-silo-scrin

scrin-silo-scrin =
    .buildable-description = Stores processed Tiberium, Ore and Gems

### scrin-srep

scrin-srep =
    .buildable-description = Repairs vehicles for credits.

### scrin-stmr

scrin-stmr =
    .buildable-description = Durable patrol craft.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Light Armor
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Tanks, Buildings

### scrin-stmr-bomber

scrin-stmr-bomber =
    .buildable-description =
        Dedicated bomber.
        Strong vs Clusters of Vehicles
    .tooltipextras-strengths = Strengths: • Strong vs Tanks, Buildings
    .tooltipextras-weaknesses = Weaknesses: • Surface Anti-Air

### scrin-stmr-hunter

scrin-stmr-hunter =
    .buildable-description = Small hunter anti-air craft.
    .tooltipextras-strengths = Strengths: • Strong vs Aircraft
    .tooltipextras-weaknesses = Weaknesses: • Surface Anti-Air

### scrin-stmr-torp

scrin-stmr-torp =
    .buildable-description = Dedicated torpedo launcher.
    .tooltipextras-strengths = Strengths: • Strong vs Ships, Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Air
    .tooltipextras-attributes = Attributes: • Detects submerged vessels

### scrin-swal

scrin-swal =
    .buildable-description = Stops units and blocks enemy fire. Regenerates over time.

### scrin-wsph

scrin-wsph =
    .buildable-description = Produces vehicles.

### ships-aegis-cruiser

ships-aegis-cruiser =
    .buildable-description =
        Anti-aircraft fleet support vessel.
        Has Point Defense Laser
        Strong vs Aircraft, Projectiles
        Weak vs Submarine, Energy Weapons
    .tooltipextras-strengths = Strengths: • Strong vs Aircraft, Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Submarines, Ships
    .tooltipextras-attributes = Attributes: • Point Defense Laser

### ships-ca

ships-ca =
    .buildable-description =
        Very slow long-range bombardment ship.
        Has Point Defense Laser.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Aircraft, Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser

### ships-carr

ships-carr =
    .buildable-description =
        Carrier that launches a squadron
        of drone aircraft.
          Strong vs Vehicles, Buildings
          Weak vs Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Aircraft, Aircraft

### ships-chasub

ships-chasub =
    .buildable-description =
        Submerged anti-ship unit
        armed with torpedoes.
        Can detect other submarines.
          Strong vs Naval units
          Weak vs Ground units, Aircraft

### ships-chflameboat

ships-chflameboat =
    .buildable-description =
        Light scout & support ship.
        Can detect submarines.

### ships-dd

ships-dd =
    .buildable-description =
        Fast multi-role ship.
        Can detect submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser

### ships-dd2

ships-dd2 =
    .buildable-description =
        Advanced warship armed with
        a powerful railgun.
    .tooltipextras-strengths = Strengths: • Strong vs Point Defense Laser
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Submarines, Aircraft

### ships-isub

ships-isub =
    .buildable-description =
        Submerged unit armed with
        long-range missiles.
          Can detect other submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Aircraft, Aircraft, Submarines

### ships-lst

ships-lst =
    .buildable-description =
        General-purpose naval transport.
        Can carry infantry and tanks.
        Unarmed

### ships-msub

ships-msub =
    .buildable-description =
        Submerged anti-ground siege unit.
        Can detect other submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Sub Ships, Aircraft

### ships-pt2

ships-pt2 =
    .buildable-description =
        Light ship armed with guided missiles.
        Has Point Defense Laser.
    .tooltipextras-strengths = Strengths: • Strong vs Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser

### ships-sb

ships-sb =
    .buildable-description =
        Fast scout boat, armed with
        rockets.
          Can attack Aircraft.
    .tooltipextras-strengths = Strengths: • Strong vs Heavy Ships, Aircraft
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Submarines

### ships-seas

ships-seas =
    .buildable-description =
        Anti-aircraft & support ship.
        Has Point Defense Laser
    .tooltipextras-strengths = Strengths: • Strong vs Aircraft, Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Ships, Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser

### ships-ss

ships-ss =
    .buildable-description =
        Submerged anti-ship unit
        armed with torpedoes.
          Can detect other submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Ships
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Sub Ships, Aircraft

### ships-ss2

ships-ss2 =
    .buildable-description =
        Submerged anti-naval unit
        armed with torpedoes.
    .tooltipextras-strengths = Strengths: • Strong vs Ships
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Aircraft, Anti-Sub Ships

### ships-subchaser

ships-subchaser =
    .buildable-description =
        Fast submarine chaser.
        Can detect submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Ships, Aircraft

### structures-afac

structures-afac =
    .buildable-description = Produces structures.

### structures-afld

structures-afld =
    .buildable-description =
        Produces and reloads aircraft.
        Special Ability: Spy Plane
        Special Ability: Paratroopers

### structures-afld-allies

structures-afld-allies =
    .buildable-description = Produces and reloads aircraft.

### structures-afld-gdi

structures-afld-gdi =
    .buildable-description =
        Produces and reloads aircraft.
        Special Ability: Recon Drone

### structures-agun

structures-agun =
    .buildable-description =
        Anti-aircraft base defense.
        Requires power to operate.
        Strong vs Aircraft
        Weak vs Ground Units

### structures-airs

structures-airs =
    .buildable-description =
        Provides a dropzone
        for vehicle reinforcements

### structures-anypower

structures-anypower =
    .buildable-description = Power Plant

### structures-apwr

structures-apwr =
    .buildable-description =
        Provides double the power of
        a standard Power Plant.

### structures-atek

structures-atek =
    .buildable-description =
        Provides Allied advanced technologies.
        Special Ability: GPS Satellite

### structures-atwr

structures-atwr =
    .buildable-description =
        Advanced base defense.
        Requires power to operate.
        Can detect cloaked units.
        Relay targeting system marks targets so other Advanced Guard Towers can shoot from longer distances.
    .tooltipextras-strengths = Strengths: • Strong vs Vehicles, Slow Aircraft

### structures-barr

structures-barr =
    .buildable-description = Trains infantry.

### structures-bio-nod

structures-bio-nod =
    .buildable-description =
        Replaces basic infantry with cyborgs.
        Maximum 1 can be built.
        Cannot be captured.
        Special Ability: Cyborg Converter

### structures-brik

structures-brik =
    .buildable-description = Stops units and blocks enemy fire.

### structures-chain

structures-chain =
    .buildable-description =
        Stops infantry and light vehicles.
        Can be crushed by tanks.

### structures-cram

structures-cram =
    .buildable-description =
        Anti-aircraft base defense.
        Requires power to operate.
        Can detect cloaked units.
        Strong vs Aircraft
        Weak vs Ground Units

### structures-dome

structures-dome =
    .buildable-description =
        Provides an overview
        of the battlefield.
          Requires power to operate.

### structures-eye

structures-eye =
    .buildable-description =
        Provides radar and Orbital Ion Cannon support power.
        Requires power to operate.
        Maximum 1 can be built.
        Special Ability: Ion Cannon

### structures-fact

structures-fact =
    .buildable-description = Produces structures.

### structures-fenc

structures-fenc =
    .buildable-description =
        Stops infantry and light vehicles.
        Can be crushed by tanks.

### structures-fix

structures-fix =
    .buildable-description = Repairs vehicles.

### structures-ftur

structures-ftur =
    .buildable-description =
        Anti-infantry base defense.
        Can detect cloaked units.
        Strong vs Infantry, Light Armor
        Weak vs Tanks, Aircraft

### structures-gap

structures-gap =
    .buildable-description =
        Obscures the enemy's view with shroud.
        Requires power to operate.

### structures-gtek

structures-gtek =
    .buildable-description = Provides GDI advanced technologies.

### structures-gtwr

structures-gtwr =
    .buildable-description =
        Anti-infantry base defense.
        Can detect cloaked units.
        Strong vs Infantry, Light Armor
        Weak vs Tanks

### structures-gun

structures-gun =
    .buildable-description =
        Basic anti-tank base defense.
        Can detect cloaked units.
        Strong vs Tanks, Vehicles
        Weak vs Infantry

### structures-hand

structures-hand =
    .buildable-description = Trains infantry.

### structures-hbox

structures-hbox =
    .buildable-description =
        Anti-infantry base defense.
        Can detect cloaked units.
        Strong vs Infantry, Light Armor
        Weak vs Tanks, Aircraft

### structures-hpad

structures-hpad =
    .buildable-description =
        Produces and reloads
        helicopters.

### structures-hpad-td

structures-hpad-td =
    .buildable-description =
        Produces and reloads
        helicopters.

### structures-hq

structures-hq =
    .buildable-description =
        Provides an overview of the battlefield.
        Requires power to operate.

### structures-indp

structures-indp =
    .buildable-description =
        Reduces cost of vehicles & aircraft by 25%
        Maximum 1 can be built.
        Cannot be captured.

### structures-infantry-any

structures-infantry-any =
    .buildable-description = Infantry Production

### structures-infantry-human

structures-infantry-human =
    .buildable-description = Infantry Production

### structures-iron

structures-iron =
    .buildable-description =
        Makes a group of units invulnerable
        for a short time.
          Requires power to operate.
          Maximum 1 can be built.
          Special Ability: Iron Curtain

### structures-kenn

structures-kenn =
    .buildable-description = Trains Attack Dogs.

### structures-lasp

structures-lasp =
    .buildable-description =
        Stops infantry and blocks enemy fire.
        Cannot be crushed by tanks.

### structures-ltur

structures-ltur =
    .buildable-description =
        Anti-infantry base defense.
        Can detect cloaked units.
        Strong vs Infantry, Light armor
        Weak vs Tanks, Aircraft

### structures-mslo

structures-mslo =
    .buildable-description =
        Provides an atomic bomb.
        Requires power to operate.
        Maximum 1 can be built.
        Special Ability: Atom Bomb

### structures-mslo-nod

structures-mslo-nod =
    .buildable-description =
        Provides a chemical missile.
        Requires power to operate.
        Maximum 1 can be built.
        Special Ability: Chemical Missile

### structures-nsam

structures-nsam =
    .buildable-description =
        Anti-aircraft base defense.
        Requires power to operate.
        Can detect cloaked units.
        Strong vs Aircraft
        Cannot target Ground Units.

### structures-nuk2

structures-nuk2 =
    .buildable-description =
        Provides double the power of
        a standard Nuclear Power Plant.

### structures-nuke

structures-nuke =
    .buildable-description = Provides power for other structures.

### structures-obli

structures-obli =
    .buildable-description =
        Advanced experimental base defense.
        Requires power to operate.
        Can detect cloaked units.
        Strong vs Vehicles, Infantry
        Weak vs Aircraft

### structures-oil-platform

structures-oil-platform =
    .buildable-description =
        Generates income through oil exploitation.
        Placing many within range circle decreases efficiency.

### structures-oilp

structures-oilp =
    .buildable-description = Gives a steady income from oil drilling.

### structures-orep

structures-orep =
    .buildable-description =
        Refines income from ore, gems & tiberium by 5-10%.
        Provides access to advanced chrono harvester technology.
          Maximum 1 can be built.
          Cannot be captured.

### structures-patr

structures-patr =
    .buildable-description =
        Launches E.M. Pulse Missiles that disable vehicles & structures.
        Requires power to operate.
        Maximum 1 can be built.
        Special Ability: E.M. Pulse Missile

### structures-pbox

structures-pbox =
    .buildable-description =
        Anti-infantry base defense.
        Can detect cloaked units.
        Strong vs Infantry, Light Armor
        Weak vs Tanks, Aircraft

### structures-pdlt

structures-pdlt =
    .buildable-description = Shoots down bullets and missiles with lazers. PEW PEW

### structures-pdox

structures-pdox =
    .buildable-description =
        Teleports a group of units across the
        map for a short time.
          Requires power to operate.
          Maximum 1 can be built.
          Special Ability: Chronoshift

### structures-powr

structures-powr =
    .buildable-description = Provides power for other structures.

### structures-pris

structures-pris =
    .buildable-description =
        Experimental base defense.
        Requires power to operate.
        Can detect cloaked units.
          Strong vs Vehicles, Infantry
          Weak vs Aircraft

### structures-proc

structures-proc =
    .buildable-description =
        Processes raw Tiberium, Ore and Gems
        into credits.

### structures-proc-td

structures-proc-td =
    .buildable-description =
        Processes raw Tiberium, Ore and Gems
        into useable resources

### structures-pyle

structures-pyle =
    .buildable-description = Trains infantry.

### structures-rep

structures-rep =
    .buildable-description = Repairs vehicles.

### structures-repair

structures-repair =
    .buildable-description = Service Depot/Repair Facility

### structures-sam

structures-sam =
    .buildable-description =
        Anti-aircraft base defense.
        Requires power to operate.
        Can detect cloaked units.
        Strong vs Aircraft
        Weak vs Ground Units

### structures-sbag

structures-sbag =
    .buildable-description =
        Stops infantry and light vehicles.
        Can be crushed by tanks.

### structures-sgen

structures-sgen =
    .buildable-description =
        Makes a group of units invisible for a short time.
        Requires power to operate.
        Maximum 1 can be built.
        Special Ability: Tiberium Stealth

### structures-silo

structures-silo =
    .buildable-description =
        Stores excess refined
        Tiberium, Ore and Gems.

### structures-silo-td

structures-silo-td =
    .buildable-description = Stores processed Tiberium, Ore and Gems

### structures-spen

structures-spen =
    .buildable-description =
        Produces and repairs
        submarines and transports.

### structures-spen-nod

structures-spen-nod =
    .buildable-description =
        Produces and repairs
        submarines and transports.

### structures-stek

structures-stek =
    .buildable-description = Provides Soviet advanced technologies.

### structures-structures-tsla

structures-structures-tsla =
    .buildable-description = Tesla Coil

### structures-swiss-bank

structures-swiss-bank =
    .buildable-description =
        Transfers Money to your allies.
        Avoids taxes.

### structures-syrd

structures-syrd =
    .buildable-description =
        Produces and repairs
        ships and transports.

### structures-syrd-gdi

structures-syrd-gdi =
    .buildable-description =
        Produces and repairs ships
        and transports.

### structures-tent

structures-tent =
    .buildable-description = Trains infantry.

### structures-tmpl

structures-tmpl =
    .buildable-description =
        Provides Nod advanced technologies.
        Requires power to operate.
        Special Ability: Cluster Missile

### structures-tpwr

structures-tpwr =
    .buildable-description =
        Provides double the power of
        a standard Power Plant.
          Special Ability: Overload (Toggle)
          Warning: Unstable when damaged and overloaded.

### structures-tsla

structures-tsla =
    .buildable-description =
        Advanced base defense.
        Can be buffed or made work during low power by Shock Troopers.
        Requires power to operate.
        Can detect cloaked units.
        Strong vs Infantry, Vehicles
        Weak vs Aircraft

### structures-ttur

structures-ttur =
    .buildable-description =
        Anti-infantry base defense.
        Can detect cloaked units.
        Strong vs Infantry, Light armor
        Weak vs Tanks, Aircraft

### structures-upgc

structures-upgc =
    .buildable-description =
        Allows the construction of advanced weaponry and upgrades.
        Maximum 1 can be built.
        Cannot be captured.

### structures-upgc-bomb

structures-upgc-bomb =
    .buildable-description =
        Provides an napalm rocket barrage power.
        Units get a firepower bonus of 15%.

### structures-upgc-drop

structures-upgc-drop =
    .buildable-description =
        Provides a dropzone for emergency supplies
        You will receive an universal basic income.

### structures-upgc-hold

structures-upgc-hold =
    .buildable-description =
        Provides the ability to rapidly repair friendly structures.
        Units get an armor bonus of 15%.

### structures-upgc-seek

structures-upgc-seek =
    .buildable-description =
        Provides detailed information on enemy locations.
        Units get a range and speed bonus of 15%.

### structures-vehicles-any

structures-vehicles-any =
    .buildable-description = Vehicle Production

### structures-vehicles-nod

structures-vehicles-nod =
    .buildable-description = Vehicle Production

### structures-vehicles-td

structures-vehicles-td =
    .buildable-description = Vehicle Production

### structures-weap

structures-weap =
    .buildable-description = Produces vehicles.

### structures-weap-td

structures-weap-td =
    .buildable-description = Produces vehicles.

### structures-weat

structures-weat =
    .buildable-description =
        Provides Lightning Storm support power.
        Requires power to operate.
        Maximum 1 can be built.
        Special Ability: Lightning Storm

### vehicles-1tnk

vehicles-1tnk =
    .buildable-description =
        Fast tank, good for scouting.
        Strong vs Light Armor
        Weak vs Infantry, Tanks, Aircraft
        Special Ability: Amphibious

### vehicles-1tnk-pdl

vehicles-1tnk-pdl =
    .buildable-description =
        Fast tank, good for scouting.
        Has Point Laser Defense System
          Strong vs Light Armor
          Weak vs Infantry, Tanks, Aircraft
          Special Ability: Amphibious

### vehicles-1tnk-reflector

vehicles-1tnk-reflector =
    .buildable-description =
        Fast tank, good for scouting.
        Has Reflector Armor, which is very good vs. Energy Weapons.
          Strong vs Light Armor, Energy Weapons
          Weak vs Infantry, Tanks, Aircraft
          Special Ability: Amphibious

### vehicles-2tnk

vehicles-2tnk =
    .buildable-description =
        Allied main battle tank.
        Strong vs Vehicles
        Weak vs Infantry, Aircraft

### vehicles-amcv

vehicles-amcv =
    .buildable-description =
        Deploys into another Construction Yard.
        Unarmed

### vehicles-apc

vehicles-apc =
    .buildable-description =
        Tough infantry transport.
        Strong vs Infantry
        Weak vs Tanks, Aircraft

### vehicles-apc2

vehicles-apc2 =
    .buildable-description =
        Tough infantry transport.
        Strong vs Infantry
        Weak vs Tanks, Aircraft

### vehicles-arty

vehicles-arty =
    .buildable-description =
        Long-range artillery.
        Strong vs Buildings, Infantry
        Weak vs Vehicles, Aircraft

### vehicles-beam-cannon

vehicles-beam-cannon =
    .buildable-description =
        Prototype energy beam weapon.
        Up to 5 Beam Cannons can charge another Beam Cannon up.
        Up to 3 Beam Cannons can help an Obelisk to charge up faster
        Strong vs Vehicles, Infantry
        Weak vs Aircraft

### vehicles-bggy

vehicles-bggy =
    .buildable-description =
        Fast scout & anti-infantry vehicle.
        Can detect spies and cloaked units.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft

### vehicles-bggy-aa

vehicles-bggy-aa =
    .buildable-description =
        Fast scout and anti-aircraft vehicle.
        Can detect cloaked units.
          Strong vs Infantry, Aircraft
          Weak vs Vehicles

### vehicles-bggy-pdl

vehicles-bggy-pdl =
    .buildable-description =
        Fast scout and anti-infantry vehicle.
        Can detect cloaked units and has Point Laser Defense.
          Strong vs Infantry
          Weak vs Energy Weapons

### vehicles-bike

vehicles-bike =
    .buildable-description =
        Fast scout vehicle, armed with
        rockets.
          Can attack Aircraft.
          Strong vs Vehicles, Tanks
          Weak vs Infantry

### vehicles-bike-explosive

vehicles-bike-explosive =
    .buildable-description =
        Fast scout vehicle, armed with
        explosive rockets.
         Can attack Aircraft.
         Strong vs Light Vehicles, Aircraft, Infantry
         Weak vs Heavy Vehicles, PDL

### vehicles-bike-explosive-pdl

vehicles-bike-explosive-pdl =
    .buildable-description =
        Fast scout vehicle, armed with explosive rockets.
        Can attack Aircraft.
        Has Point Laser Defense.
        Strong vs Light Vehicles, Aircraft, Infantry
        Weak vs Heavy Vehicles, Energy Weapons

### vehicles-bike-explosive-reflector

vehicles-bike-explosive-reflector =
    .buildable-description =
        Fast scout vehicle, armed with
        explosive rockets.
         Has Reflector Armor, which is very good vs. Energy Weapons.
         Strong vs Light Vehicles, Aircraft, Infantry, Energy Weapons
         Weak vs Heavy Vehicles, PDL

### vehicles-bike-rockethail

vehicles-bike-rockethail =
    .buildable-description =
        Fast artillery vehicle, armed with
        many rockets.
          Strong vs Vehicles, Tanks
         Weak vs Infantry, Aircraft

### vehicles-bike-rockethail-pdl

vehicles-bike-rockethail-pdl =
    .buildable-description =
        Fast scout vehicle, armed with explosive rockets.
        Can attack Aircraft.
        Has Point Laser Defense.
        Strong vs Vehicles, Projectiles
        Weak vs Infantry, Aircraft, Energy Weapons

### vehicles-bike-rockethail-reflector

vehicles-bike-rockethail-reflector =
    .buildable-description =
        Fast artillery vehicle, armed with
        many rockets.
         Has Reflector Armor, which is very good vs. Energy Weapons.
         Strong vs Vehicles, Energy Weapons
         Weak vs Infantry, Aircraft

### vehicles-bike-scrin

vehicles-bike-scrin =
    .buildable-description =
        Fast scout vehicle, armed with energy balls.
        Strong vs Vehicles, Tanks
        Weak vs Infantry, Aircraft

### vehicles-bike-scrin-pdl

vehicles-bike-scrin-pdl =
    .buildable-description =
        Fast scout vehicle, armed with rockets.
        Can attack Aircraft.
        Has Point Laser Defense.
        Strong vs Vehicles, Tanks
        Weak vs Infantry

### vehicles-bike-scrin-reflector

vehicles-bike-scrin-reflector =
    .buildable-description =
        Fast scout vehicle, armed with energy balls.
        Has Reflector Armor, which is very good vs. Energy Weapons.
        Strong vs Vehicles, Energy Weapons
        Weak vs Infantry, Aircraft

### vehicles-btr

vehicles-btr =
    .buildable-description =
        Tough infantry transport.
        Can attack Aircraft.
        Strong vs Light Armor, Aircraft
        Weak vs Tanks

### vehicles-btr-surveillance

vehicles-btr-surveillance =
    .buildable-description =
        Tough infantry transport.
        Can detect stealth units.
        Is unarmed.

### vehicles-cdrn

vehicles-cdrn =
    .buildable-description =
        Drone armed with Chaos Gas which causes units
        to become frenzied and attack indiscriminately.
          Has weak armor.

### vehicles-coordinator

vehicles-coordinator =
    .buildable-description =
        Coordinates newly built vehicles to be airdropped close to the Coordinator when deployed.
        Airstrips must not be primary production buildings for the Coordinator to work.
        Use the rally point when deployed or units will be dropped onto each other.

### vehicles-cryo

vehicles-cryo =
    .buildable-description =
        Long-range support artillery that slows enemies
        and makes them take more damage.
          Strong vs Infantry, Vehicles, Buildings
          Weak vs Aircraft

### vehicles-disr

vehicles-disr =
    .buildable-description =
        Armored high-tech vehicle with long-range sonic armament.
        Strong vs Infantry, Vehicles, Buildings
        Weak vs Aircraft

### vehicles-gdrn

vehicles-gdrn =
    .buildable-description =
        Fast scout & anti-tank vehicle.
        Can detect spies and cloaked units.
        Strong vs Vehicles
        Weak vs Infantry, Buildings, Defenses, Aircraft

### vehicles-har2

vehicles-har2 =
    .buildable-description =
        Collects Tiberium, Ore and Gems for processing.
        Unarmed

### vehicles-harv

vehicles-harv =
    .buildable-description =
        Collects Ore, Gems and Tiberium for processing.
        Unarmed

### vehicles-harv-chrono

vehicles-harv-chrono =
    .buildable-description =
        Collects Ore, Gems and Tiberium for processing.
        Unarmed
        Special Ability: Teleport

### vehicles-hmmv

vehicles-hmmv =
    .buildable-description =
        Fast scout & anti-infantry vehicle.
        Can detect spies and cloaked units.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft

### vehicles-ifv

vehicles-ifv =
    .buildable-description =
        Adaptable infantry transport.
        Can attack Aircraft.
        Strong vs Light Armor, Aircraft
        Weak vs Infantry, Tanks
        Special Ability: Transform

### vehicles-ifv-ai

vehicles-ifv-ai =
    .buildable-description =
        Adaptable infantry transport.
        Can attack Aircraft.
        Strong vs Infantry, Light Armor
        Weak vs Tanks, Aircraft
        Special Ability: Transform

### vehicles-jeep

vehicles-jeep =
    .buildable-description =
        Fast scout & anti-infantry vehicle.
        Can detect spies and cloaked units.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft

### vehicles-jugg

vehicles-jugg =
    .buildable-description =
        Tough artillery battle-mech.
        Strong vs Infantry, Structures
        Weak vs Aircraft

### vehicles-ltnk

vehicles-ltnk =
    .buildable-description =
        Fast, light tank.
        Strong vs Vehicles
        Weak vs Infantry, Aircraft
        Special Ability: Amphibious

### vehicles-ltnk-laser

vehicles-ltnk-laser =
    .buildable-description =
        Fast, light tank shooting laser.
        Strong vs heavy Tanks
        Weak vs Infantry, Aircraft

### vehicles-mcv

vehicles-mcv =
    .buildable-description =
        Deploys into another Construction Yard.
        Unarmed

### vehicles-mdrn

vehicles-mdrn =
    .buildable-description =
        Hovering drone with machine gun.
        Attaches to certain vehicles to provide support and repairs.
        Strong vs Infantry
        Weak vs Vehicles, Buildings, Aircraft

### vehicles-memp

vehicles-memp =
    .buildable-description =
        Self destructs to disable nearby vehicles & structures.
        Requires power to operate.
        Strong vs Vehicles, Buildings
        Weak vs Infantry, Aircraft

### vehicles-memp-stable

vehicles-memp-stable =
    .buildable-description =
        Releases EMP Wave to disable nearby vehicles & structures.
        Upgraded with Stable Reactor to allow multiple EMP Waves.
          Requires power to operate.
          Strong vs Vehicles, Buildings
          Weak vs Infantry, Aircraft

### vehicles-memp-volatile

vehicles-memp-volatile =
    .buildable-description =
        Self destructs to disable nearby vehicles & structures.
        Upgraded with Volatile Reactor to EMP on destruction.
          Requires power to operate.
          Strong vs Vehicles, Buildings
          Weak vs Infantry, Aircraft

### vehicles-mgg

vehicles-mgg =
    .buildable-description =
        Regenerates the shroud nearby, obscuring the area.
        Unarmed

### vehicles-ssm

vehicles-ssm =
    .buildable-description =
        Long-range incendiary rocket artillery.
        Has weak armor.
        Strong vs Buildings, Infantry
        Weak vs Tanks, Aircraft

### vehicles-ssm-multi

vehicles-ssm-multi =
    .buildable-description =
        Long-range multiple rocket artillery.
        Strong vs Infantry, Vehicles and Buildings

### vehicles-ssm-toxin

vehicles-ssm-toxin =
    .buildable-description =
        Long-range chemical rocket artillery.
        Strong vs Infantry, Vehicles and Buildings

### vehicles-mrj

vehicles-mrj =
    .buildable-description =
        Jams nearby enemy radar systems.
        Unarmed
        Special Ability: Jams Missile-Lock Systems

### vehicles-msg

vehicles-msg =
    .buildable-description =
        Cloaks nearby units and buildings.
        Unarmed

### vehicles-mtnk

vehicles-mtnk =
    .buildable-description =
        Main battle tank.
        Strong vs Vehicles
        Weak vs Infantry, Aircraft

### vehicles-mtnk-drone

vehicles-mtnk-drone =
    .buildable-description =
        Drone battle tank.
        Requires power to operate.
        Strong vs Vehicles
        Weak vs Infantry, Aircraft

### vehicles-mtnk-drone-pdl

vehicles-mtnk-drone-pdl =
    .buildable-description =
        Drone battle tank with Point Laser Defense.
        Requires power to operate.
        Strong vs Vehicles
        Weak vs Infantry, Aircraft

### vehicles-mtnk-drone-reflector

vehicles-mtnk-drone-reflector =
    .buildable-description =
        Drone battle tank with Reflector Armor.
        It is very good vs. Energy Weapons.
         Strong vs Vehicles, Infantry, Aircraft
         Weak vs Anti-Tank Weapons

### vehicles-mtnk-pdl

vehicles-mtnk-pdl =
    .buildable-description =
        Main battle tank with Point Laser Defense.
        Strong vs Vehicles
        Weak vs Infantry, Aircraft

### vehicles-mtnk-reflector

vehicles-mtnk-reflector =
    .buildable-description =
        Main battle tank with Reflector Armor.
        Strong vs Energy Weapons
        Weak vs Infantry, Aircraft

### vehicles-rtnk

vehicles-rtnk =
    .buildable-description =
        Advanced Battle tank that can cloak when stationary.
        Has high explosive shells.
          Strong vs Light Vehicles
          Weak vs Aircraft.

### vehicles-rtnk-firerate

vehicles-rtnk-firerate =
    .buildable-description =
        Advanced Battle tank that can cloak when stationary.
        Strong vs Vehicles
        Weak vs Aircraft, Energy Weapons.

### vehicles-rtnk-pdl

vehicles-rtnk-pdl =
    .buildable-description =
        Advanced Battle tank that can cloak when stationary.
        Has average armor and Point Laser Defense.
          Strong vs Vehicles
          Weak vs Aircraft, Energy Weapons.

### vehicles-rtnk-reflector

vehicles-rtnk-reflector =
    .buildable-description =
        Advanced Battle tank that can cloak when stationary.
        Has Reflector Armor, very good vs. Energy Weapons.
         Strong vs Vehicles, Energy Weapons
         Weak vs Aircraft.

### vehicles-slng

vehicles-slng =
    .buildable-description =
        Fast, lightly armoured anti-air unit.
         Strong vs Aircraft
        Weak vs Infantry, Vehicles, Buildings

### vehicles-spec

vehicles-spec =
    .buildable-description =
        Long-range stealth artillery.
        Strong vs Buildings, Infantry
        Weak vs Vehicles, Aircraft

### vehicles-stnk

vehicles-stnk =
    .buildable-description =
        Medium-range missile tank that can cloak.
        Can attack Aircraft.
        Has weak armor.
        Can be spotted by infantry and defense structures.
        Strong vs Vehicles, Tanks
        Weak vs Infantry.

### vehicles-stnk-ap

vehicles-stnk-ap =
    .buildable-description =
        Medium-range armor piercing missile tank that can cloak.
        Can attack Aircraft.
        Has weak armor.
        Can be spotted by infantry and defense structures.
        Strong vs Heavy Vehicles, Aircraft
        Weak vs Infantry.

### vehicles-stnk-ap-pdl

vehicles-stnk-ap-pdl =
    .buildable-description =
        Medium-range armor piercing missile tank that can cloak.
        Can attack Aircraft.
        Has point laser defense.
        Can be spotted by infantry and defense structures.
        Strong vs Heavy Vehicles, Aircraft, Projectiles
        Weak vs Infantry, Energy Weapons

### vehicles-stnk-ap-reflector

vehicles-stnk-ap-reflector =
    .buildable-description =
        Medium-range armor piercing missile tank that can cloak.
        Can attack Aircraft.
        Has weak reflector armor.
        Can be spotted by infantry and defense structures.
        Strong vs Heavy Vehicles, Aircraft, Energy Weapons
        Weak vs Infantry, Projectiles.

### vehicles-stnk-he

vehicles-stnk-he =
    .buildable-description =
        Medium-range high explosive missile tank that can cloak.
        Can attack Aircraft.
        Has weak armor.
        Can be spotted by infantry and defense structures.
        Strong vs Vehicles, Aircraft
        Weak vs Heavy Tanks.

### vehicles-stnk-he-pdl

vehicles-stnk-he-pdl =
    .buildable-description =
        Medium-range high explosive missile tank that can cloak.
        Can attack Aircraft.
        Has point laser defense.
        Can be spotted by infantry and defense structures.
        Strong vs Vehicles, Aircraft, Projectiles
        Weak vs Heavy Tanks, Energy Weapons

### vehicles-stnk-he-reflector

vehicles-stnk-he-reflector =
    .buildable-description =
        Medium-range high explosive missile tank that can cloak.
        Can attack Aircraft.
        Has weak reflector armor.
        Can be spotted by infantry and defense structures.
        Strong vs Heavy Vehicles, Aircraft, Energy Weapons
        Weak vs Infantry, Projectiles.

### vehicles-stnk-scrin

vehicles-stnk-scrin =
    .buildable-description =
        Cloaked tank that fires Scrin torpedoes.
        Has weak armor.
        Can be spotted by infantry and defense structures.
        Strong vs Vehicles, Infantry
        Weak vs Aircraft, Reflector Armor.

### vehicles-stnk-scrin-pdl

vehicles-stnk-scrin-pdl =
    .buildable-description =
        Cloaked tank that fires Scrin torpedoes.
        Has point laser defense.
        Can be spotted by infantry and defense structures.
        Strong vs Vehicles, Projectiles
        Weak vs Aircraft, Energy Weapons

### vehicles-stnk-scrin-reflector

vehicles-stnk-scrin-reflector =
    .buildable-description =
        Cloaked tank that fires Scrin torpedoes.
        Has weak reflector armor.
        Can be spotted by infantry and defense structures.
        Strong vs Energy Weapons
        Weak vs Aircraft, Reflector Armor.

### vehicles-titn

vehicles-titn =
    .buildable-description =
        Tough combat battle-mech.
         Can attack Aircraft.
         Strong vs Infantry, Tanks
        Weak vs Point Laser Defense

### vehicles-titn-battle

vehicles-titn-battle =
    .buildable-description =
        Tough combat battle-mech with very heavy shells.
         EMP disables hit units.
         Strong vs Tanks, Aircraft
        Weak vs Point Laser Defense

### vehicles-titn-battle-pdl

vehicles-titn-battle-pdl =
    .buildable-description =
        Tough combat battle-mech with very heavy shells.
         EMP disables hit units.
         Has Point Laser Defense System.
         Can attack Aircraft.
         Strong vs Tanks, Aircraft, Projectiles
        Weak vs Energy Weapons

### vehicles-titn-battle-reflector

vehicles-titn-battle-reflector =
    .buildable-description =
        Tough combat battle-mech with very heavy shells.
         EMP disables hit units.
         Has Reflector Armor, which is very good vs. Energy Weapons.
         Strong vs Tanks, Energy Weapons
        Weak vs Point Laser Defense

### vehicles-titn-railgun

vehicles-titn-railgun =
    .buildable-description =
        Tough combat battle-mech with railgun.
        Cannot be intercepted by Point Laser Defense.
        Strong vs Tanks, Weak vs Aircraft, Infantry

### vehicles-titn-railgun-pdl

vehicles-titn-railgun-pdl =
    .buildable-description =
        Tough combat battle-mech with railgun.
         Cannot be intercepted by Point Laser Defense.
         Strong vs Tanks, Projectiles
        Weak vs Aircraft, Infantry

### vehicles-titn-railgun-reflector

vehicles-titn-railgun-reflector =
    .buildable-description =
        Tough combat battle-mech with railgun.
        Has Reflector Armor, which is very good vs. Energy Weapons.
        Strong vs Tanks, Energy Weapons, Weak vs Aircraft, Infantry

### vehicles-tnkd

vehicles-tnkd =
    .buildable-description =
        German tank destroyer.
        Strong vs Heavy Armor
        Weak vs Infantry, Light Armor, Aircraft

### vehicles-tnkd-burstfire

vehicles-tnkd-burstfire =
    .buildable-description =
        German tank destroyer.
        Strong vs Heavy Armor
        Weak vs Infantry, Light Armor, Aircraft

### vehicles-tnkd-pdl

vehicles-tnkd-pdl =
    .buildable-description =
        German tank destroyer.
        Strong vs Heavy Armor
        Weak vs Infantry, Light Armor, Aircraft

### vehicles-tnkd-reflector

vehicles-tnkd-reflector =
    .buildable-description =
        German tank destroyer.
        Has Reflector Armor, very good vs. Energy Weapons.
         Strong vs Heavy Armor, Energy Weapons.
         Weak vs Infantry, Light Armor, Aircraft

### vehicles-truk

vehicles-truk =
    .buildable-description =
        Transports cash to other players.
        Unarmed

### vehicles-wtnk

vehicles-wtnk =
    .buildable-description =
        Prototype microwave weapon.
        Disables vehicles & structures.
        Strong vs Vehicles, Infantry
        Weak vs Aircraft

### vehicles-xo

vehicles-xo =
    .buildable-description =
        Light scout-mech with a short-range laser
        and a machine gun.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Light Armor
    .tooltipextras-weaknesses = Weaknesses: • Cannot attack Aircraft

### world-baseworld

world-baseworld =
    .factionca-at-random-description =
        Random Faction
        A random faction will be chosen when the game starts.
    .factionca-at-randomair-description =
        Random Air Faction
        A random faction, which has strong aircraft, will be chosen when the game starts.
    .factionca-at-randomclassic-description =
        Random Classic Faction
        A random faction from RA or TD will be chosen when the game starts.
    .factionca-at-randomevil-description =
        Random Evil Faction
        A random evil faction will be chosen when the game starts.
        (Keep in mind evil is a question of perspective, if you think Putin is the good guy, this randomness here might confuse you)
    .factionca-at-randomgood-description =
        Random Good Faction
        A random good faction will be chosen when the game starts.
        (Keep in mind good is a question of perspective, if you wonder about China, remember they produce all the cheap sex toys)
    .factionca-at-randominfantry-description =
        Random Infantry Faction
        A random faction, which has strong infantry, will be chosen when the game starts.
    .factionca-at-randomnotchina-description =
        PLEASE GOD NOT CHINA
        A random faction will be chosen when the game starts,
        but please god don't let it be China.
    .factionca-at-randomra-description =
        Random Red Alert Faction
        A random faction from Soviets or Allies will be chosen when the game starts.
    .factionca-at-randomtanks-description =
        Random Tank Faction
        A random faction, which has strong tanks, will be chosen when the game starts.
    .factionca-at-randomtd-description =
        Random Tiberian Dawn Faction
        A random faction from GDI or NOD will be chosen when the game starts.

### world-world

world-world =
    .scriptlobbydropdown-at-promotionpoints-description = Specify the amount of Commander Points
structures-radar =
    .buildable-description = Provides radar coverage of the battlefield.
defaults-pointlaserdefensesystem =
    .tooltipextras-attributes = Attributes: • Point Defense Laser intercepts Projectiles
defaults-pdlinfo =
    .tooltipextras-strengths = Strengths: • Strong vs Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Energy Weapons
    .tooltipextras-attributes = Attributes: • Point Defense Laser intercepts Projectiles
defaults-reflectorarmor =
    .tooltipextras-attributes = Attributes: • Reflector Armor absorbs Energy Weapons
defaults-reflectorinfo =
    .tooltipextras-strengths = Strengths: • Strong vs Energy Weapons
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Projectiles
    .tooltipextras-attributes = Attributes: • Reflector Armor absorbs Energy Weapons
aircraft-heli-fire-rate =
    .tooltipextras-attributes = Attributes: • Higher fire rate
defaults-propagandaspeaker =
    .tooltipextras-attributes = Attributes: • Propaganda Speaker supports nearby infantry
player =
    .description = promotions.lobby-description
promotion-point-defense-turret =
    .description = Enables to build the Point Laser Defense Turret.\n  It shoots down incoming shells and missiles.
promotion-oil-pumps =
	.description = Enables to build Oil Pumps, Oil Platforms, and Supply Trucks.\n  Generates slow trickling endless income.
promotion-air-logistics =
	.description = Enables to build the Chinook, Carryall and Dropship.\n
promotion-advanced-infantry-training =
	.buildable-description =
		Funds advanced academies so every faction can field elite infantry specialists once the upgrade is complete.
		Unlocks high-tech squads, medics, and commando options across all aligned barracks.
	.description =
		Commits command points to a global infantry doctrine that certifies every barracks to produce late-tier troops.
		This is a prerequisite for most Tier 3 infantry promotions, so purchase it before planning specialized squads.
	.tooltipextras-strengths = Strengths: • Unlocks advanced infantry for every faction, including commandos and support teams.
	.tooltipextras-weaknesses = Weaknesses: • Provides no immediate stat buff—units still cost resources and training time.
	.tooltipextras-attributes = Attributes: • Required before purchasing most infantry promotions in Allied, Soviet, Nod, GDI, and Scrin trees.
promotion-yuri =
    .description = Form Unholy Alliance with Yuri and his forces. \n This replaces multiple units and support powers: \n  Lasher Tank replaces Heavy Tank. \n  Yuri replaces Boris. \n  Chaos Drone replaces MAD Tank \n  Genetic Mutation Bomb replaces Parabombs. \n
promotion-flak-track-barrage =
    .description = Upgrades the Flak Track with fast firing Flak cannons. \n  This makes it better vs. Point Laser Defense Systems and groups of units.
promotion-btr =
    .description = Enables to build the BTR.\n  It is a missile based anti-air troop transporter. \nOptimal for taking out heavy air units.
promotion-surveillance-btr =
    .description = Enables to build the Surveillance BTR.\n  It has high visual range, detects stealth units, jams radar and is a troop transporter on top.
promotion-apc-vulcan =
    .description = Upgrades the APC with a Vulcan cannon to attack air units.
promotion-reinforcements-coordinator =
    .description = Enables to build the Reinforcements Coordinator. \n It coordinates reinforcements directly to the frontline when deployed.
promotion-buggy-pdl =
    .description = Upgrades the Buggy with a Point Laser Defense system to shoot down incoming projectiles.
promotion-buggy-aa =
    .description = Upgrades the Buggy with an Anti-Air Laser to shoot down aircraft.
promotion-ssm-launcher =
    .description = Enables to build the SSM Launcher, a high range artillery.
promotion-ssm-toxin-launcher =
    .description = Upgrades the SSM Launcher with chemical missiles.\n  The chemical clouds remain for a prolonged time on the impact site dealing damage.
promotion-ssm-multi-launcher =
    .description = Upgrades the SSM Launcher's missiles to split up in multiple missiles on impact. \n  This makes the SSM Launcher more effective against large groups of vehicles.
promotion-stealth-tank-ap =
    .description = Upgrades the Stealth Tank with a Armor Piercing Missiles. \n  This makes it much more effective vs. heavy tanks and heavy aircraft.
promotion-stealth-tank-scrin =
    .description = Upgrades the Stealth Tank with a Scrin Weapon. \n  Energy based weapons cannot be intercepted by Point Laser Defense.
promotion-stealth-tank-explosive-rockets =
    .description = Upgrades the Stealth Tank's missiles with a high explosive load.
promotion-stealth-tank-reflector =
    .description = Equips the Stealth Tank with Reflector Armor. \n Very resistent vs. Energy Weapons.
promotion-stealth-tank-pdl =
    .description = Upgrades the Stealth Tank with a Point Laser Defense System to shoot down incoming projectiles.
promotion-recon-bike-scrin =
    .description = Upgrades the Recon Bike with a Scrin Weapon.\n  Energy based weapons cannot be intercepted by Point Laser Defense.
promotion-recon-bike-scrin-pdl =
    .description = Upgrades the Scrin Bike with a Point Laser Defense System to shoot down incoming projectiles.
promotion-recon-bike-scrin-reflector =
    .description = Equips the Scrin Bike with Reflector Armor. \n Very resistent vs. Energy Weapons.
promotion-recon-bike-rocket-hail =
    .description = Upgrades the Recon Bike to shoot more missiles at high range. \n  This makes the recon bike a hit and run artillery unit. \n  The recon bike loses its ability to fight flying targets.
promotion-recon-bike-rocket-hail-reflector =
    .description = Equips the Rocket Hail Bike with Reflector Armor. \n Very resistent vs. Energy Weapons.
promotion-recon-bike-rocket-hail-pdl =
    .description = Upgrades the Rocket Hail Bike with a Point Laser Defense System to shoot down incoming projectiles.
promotion-recon-bike-explosive =
    .description = Upgrades the Recon Bike to shoot explosive missiles. \n  This is very strong vs. weakly armored units. \n
promotion-recon-bike-explosive-pdl =
    .description = Upgrades the Explosive Rocket Bike with a Point Laser Defense System to shoot down incoming projectiles.
promotion-recon-bike-explosive-reflector =
    .description = Equips the Explosive Rocket Bike with Reflector Armor. \n Very resistent vs. Energy Weapons.
promotion-scorpion-tank =
    .description = Unlocks a small hovering scorpion tank armed with heavy laser. \n  Energy based weapons cannot be intercepted by Point Laser Defense.
promotion-beam-cannon =
    .description = Enables to build the Beam Cannon. \n It shoots a continuous energy beam at distant targets Strong vs. Vehicles, Weak vs. Aircraft.
promotion-subchaser =
    .description = The Subchaser is a small naval vessel specialized in hunting submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Ships, Aircraft
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
promotion-hunter-submarine =
    .description = The Hunter Submarine is a fast and destructive submerged vessel. \nIt lacks armor, but can take out unprotected ships very efficiently.
    .tooltipextras-strengths = Strengths: • Strong vs Ships
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Aircraft, Anti-Sub Ships
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
promotion-patrol-boat =
    .description = The Patrol Boat is a versatile rocket vessel. \nIt comes with Point-Defense-Laser to stop incoming projectiles.
    .tooltipextras-strengths = Strengths: • Strong vs Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser
promotion-recon-boat =
    .description = The Recon Boat is a fast vessel armed with missiles.
    .tooltipextras-strengths = Strengths: • Strong vs Heavy Ships, Aircraft
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Submarines
promotion-sea-scorpion =
    .description = The Sea Scorpion is a flak boat specialized in fighting aircraft. \nIt comes with Point-Defense-Laser to stop incoming projectiles.
    .tooltipextras-strengths = Strengths: • Strong vs Aircraft, Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Ships, Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser
promotion-attack-submarine =
    .description = The Attack Submarine is specialized in fighting other ships. \nIt is submerged and cannot be detected by all ships.
    .tooltipextras-strengths = Strengths: • Strong vs Ships
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Sub Ships, Aircraft
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
promotion-destroyer =
    .description = The Destroyer is capable of fighting against all threats, \nbut is mostly specialized against submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
promotion-frigate =
    .description = The Frigate brings a heavy energy weapon to the seas. \nThis makes it suited best against Point Defense Laser systems.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Aircraft, Submarines
promotion-aegis-cruiser =
    .description = The Aegis Cruiser fires a barrage of missiles. \nThe missiles are best suited against aircraft.\nHas Point Defense Laser system.
    .tooltipextras-strengths = Strengths: • Strong vs Aircraft, Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Submarines, Ships
    .tooltipextras-attributes = Attributes: • Point Defense Laser
promotion-battlecruiser =
    .description = The Battlecruiser is the ultimate artillery ship. \n Dual 8-inch cannons provide constant barrage, \n while a Point Defense Laser fends of incoming enemy artillery.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Aircraft, Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser
promotion-carrier =
    .description = The Carrier launches small strike aircraft. \nThe aircraft are able to fight submarines as well as all ground targets.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Aircraft, Aircraft
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
promotion-icbm-submarine =
    .description = The ICBM Submarine can fire Nukes over a very long distance.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Aircraft, Aircraft, Submarines
promotion-missile-submarine =
    .description = The Missile Submarine launches a barrage of missiles with very high range.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Submarines
commander-tree-player =
    .description = promotions.lobby-description
commander-tree-promotion-subchaser =
    .description = The Subchaser is a small naval vessel specialized in hunting submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Ships, Aircraft
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
commander-tree-promotion-hunter-submarine =
    .description =
        The Hunter Submarine is a fast and destructive submerged vessel.
        It lacks armor, but can take out unprotected ships very efficiently.
    .tooltipextras-strengths = Strengths: • Strong vs Ships
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Aircraft, Anti-Sub Ships
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
commander-tree-promotion-patrol-boat =
    .description =
        The Patrol Boat is a versatile rocket vessel.
        It comes with Point-Defense-Laser to stop incoming projectiles.
    .tooltipextras-strengths = Strengths: • Strong vs Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser
commander-tree-promotion-recon-boat =
    .description = The Recon Boat is a fast vessel armed with missiles.
    .tooltipextras-strengths = Strengths: • Strong vs Heavy Ships, Aircraft
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Submarines
commander-tree-promotion-sea-scorpion =
    .description =
        The Sea Scorpion is a flak boat specialized in fighting aircraft.
        It comes with Point-Defense-Laser to stop incoming projectiles.
    .tooltipextras-strengths = Strengths: • Strong vs Aircraft, Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Ships, Submarines
    .tooltipextras-attributes = Attributes: • Point Defense Laser
commander-tree-promotion-attack-submarine =
    .description =
        The Attack Submarine is specialized in fighting other ships.
        It is submerged and cannot be detected by all ships.
    .tooltipextras-strengths = Strengths: • Strong vs Ships
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Anti-Sub Ships, Aircraft
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
commander-tree-promotion-destroyer =
    .description =
        The Destroyer is capable of fighting against all threats,
        but is mostly specialized against submarines.
    .tooltipextras-strengths = Strengths: • Strong vs Submarines
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser
    .tooltipextras-attributes = Attributes: • Detects submerged vessles.
commander-tree-promotion-frigate =
    .description =
        The Frigate brings a heavy energy weapon to the seas.
        This makes it suited best against Point Defense Laser systems.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Slow Fleets
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Point Defense Laser, Aircraft, Submarines
commander-tree-promotion-aegis-cruiser =
    .description =
        The Aegis Cruiser fires a barrage of missiles.
        The missiles are best suited against aircraft.
        Has Point Defense Laser system.
    .tooltipextras-strengths = Strengths: • Strong vs Aircraft, Projectiles
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Submarines, Ships
    .tooltipextras-attributes = Attributes: • Point Defense Laser

### infantry-adept

infantry-adept =
    .buildable-description = General-purpose infantry with good scouting abilities.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry
    .tooltipextras-attributes = Attributes: • Equipped with scout equipment

### infantry-black-hand-trooper

infantry-black-hand-trooper =
    .buildable-description = Elite precision flamethrower unit.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Light Vehicles, Buildings

### infantry-cyborg-commando

infantry-cyborg-commando =
    .buildable-description = Tough combat battle-mech.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Vehicles
    .tooltipextras-attributes = Attributes: • Laser sweeps over area

### infantry-cyborgelite

infantry-cyborgelite =
    .buildable-description = Elite cyborg infantry unit armed with plasma cannons.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Vehicles
    .tooltipextras-attributes = Attributes: • Heavy armor infantry

### infantry-cyborgelite-range

infantry-cyborgelite-range =
    .buildable-description =
        Elite cyborg infantry unit armed with a plasma cannon.
        Has upgraded range
        Strong vs Infantry, Vehicles
        Weak vs Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Vehicles with long-range plasma volleys.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Aircraft and focused anti-infantry fire.
    .tooltipextras-attributes = Attributes: • Extended range upgrade for the Cyborg Elite.

### infantry-hack

infantry-hack =
    .buildable-description = The Hacker can take control of defenses and buildings.
    .tooltipextras-strengths = Strengths: • Strong vs Buildings, Defenses
    .tooltipextras-weaknesses = Weaknesses: • Cannot attack Infantry, Aircraft, Vehicles
    .tooltipextras-attributes =
        Attributes: • Can capture enemy structures from range
        • Control lost if the Hacker dies
        • Can swim

### infantry-obelisk-trooper

infantry-obelisk-trooper =
    .buildable-description =
        Elite infantry with a laser weapon.
        Strong vs Infantry, Light Armor
        Weak vs Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Light Vehicles
    .tooltipextras-attributes = Attributes: • Pew pew laser gunz!

### infantry-rocket-cyborg

infantry-rocket-cyborg =
    .buildable-description = Anti-tank cyborg infantry
    .tooltipextras-strengths = Strengths: • Strong vs Heavy Vehicles, Aircraft

### infantry-rocket-trooper

infantry-rocket-trooper =
    .buildable-description = Anti-tank/anti-aircraft infantry.
    .tooltipextras-strengths = Strengths: • Strong vs Heavy Vehicles, Aircraft

### infantry-sab

infantry-sab =
    .buildable-description = Covert infantry. Infiltrates enemy structures and steals technology.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Light Vehicles
    .tooltipextras-attributes =
        Attributes: • Cloaked
        • Infiltrates buildings
        •Detects cloaked units

### infantry-stormtrooper

infantry-stormtrooper =
    .buildable-description = General-purpose cyborg with good scout abilities.
    .tooltipextras-strengths = Strengths: • Strong vs Infantry
    .tooltipextras-attributes = Attributes: • Equipped with scout equipment

### supportpowers-nodsuperweaponsmall

supportpowers-nodsuperweaponsmall =
    .grantexternalconditionpowerca-at-stealthgen-description =
        Makes vehicles and structures temporarily invisible.
        Warning: Harmful to Infantry.

### vehicles-ftnk

vehicles-ftnk =
    .buildable-description =
        Heavily armored flame-throwing vehicle.
        Strong vs Infantry, Buildings, Vehicles
        Weak vs Heavy Tanks, Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, Vehicles with dual flamethrowers.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Heavy Tanks and Aircraft overwhelming its short range.
    .tooltipextras-attributes = Attributes: • Core Flame Tank chassis used by Nod.

### vehicles-ftnk-pdl

vehicles-ftnk-pdl =
    .buildable-description =
        Heavily armored flame-throwing vehicle.
        Has Point Laser Defense
        Strong vs Infantry, Buildings, Vehicles
        Weak vs Heavy Tanks, Aircraft, Energy Weapons
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, Vehicles while Point Laser Defense blocks missiles.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Heavy Tanks, Aircraft, and energy weapons that bypass the PDL.
    .tooltipextras-attributes = Attributes: • Adds Point Laser Defense to the Flame Tank.

### vehicles-ftnk-pdl-chem

vehicles-ftnk-pdl-chem =
    .buildable-description =
        Heavily armored chemical-throwing vehicle.
        Strong vs Infantry, Buildings, Vehicles
        Weak vs Heavy Tanks, Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, Vehicles with corrosive chem spray.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Heavy Tanks and Aircraft outside chemical range.
    .tooltipextras-attributes = Attributes: • Chemical Flame Tank variant coupled with Point Laser Defense.

### vehicles-ftnk-reflector

vehicles-ftnk-reflector =
    .buildable-description =
        Heavily armored flame-throwing vehicle.
        Has Reflector Armor, which is very good vs. Energy Weapons.
        Strong vs Infantry, Buildings, Energy Weapons
        Weak vs Conventional Weapons, Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, and resists Energy Weapons thanks to reflector armor.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs conventional tank shells and Aircraft that stay at range.
    .tooltipextras-attributes = Attributes: • Reflector Armor upgrade for the Flame Tank.

### vehicles-ftnk-reflector-chem

vehicles-ftnk-reflector-chem =
    .buildable-description =
        Heavily armored chemical-throwing vehicle.
        Has Reflector Armor, which is very good vs. Energy Weapons.
        Strong vs Infantry, Buildings, Vehicles, Energy Weapons
        Weak vs Conventional Weapons, Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, Vehicles and shrugs off Energy Weapons.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs conventional artillery and Aircraft which can tag it from afar.
    .tooltipextras-attributes = Attributes: • Chem Tank outfitted with Reflector Armor.

### vehicles-hftk

vehicles-hftk =
    .buildable-description =
        Heavy tank armed with dual short-range flamethrowers.
        Strong vs Infantry, Buildings, Light Armor
        Weak vs Tanks, Defenses, Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, Light Armor with heavy twin flamethrowers.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Tanks, defenses, and Aircraft due to short range.
    .tooltipextras-attributes = Attributes: • Heavy Flame Tank base chassis.

### vehicles-hftk-pdl

vehicles-hftk-pdl =
    .buildable-description =
        Heavily armored flame-throwing vehicle.
        Has Point Laser Defense
        Strong vs Infantry, Buildings, Vehicles
        Weak vs Heavy Tanks, Aircraft, Energy Weapons
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, Vehicles; PDL protects against missiles.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Heavy Tanks, Aircraft, and Energy Weapons that bypass the PDL grid.
    .tooltipextras-attributes = Attributes: • Adds Point Laser Defense to the Heavy Flame Tank.

### vehicles-hftk-pdl-fireball

vehicles-hftk-pdl-fireball =
    .buildable-description =
        Heavily armored fireball-throwing vehicle.
        Strong vs Infantry, Buildings, Vehicles
        Heavy Tanks, Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, Vehicles via long-range fireball payload.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs Heavy Tanks and Aircraft able to kite the slower chassis.
    .tooltipextras-attributes = Attributes: • Fireball launcher plus Point Laser Defense on the Heavy Flame Tank.

### vehicles-hftk-reflector

vehicles-hftk-reflector =
    .buildable-description =
        Heavily armored flame-throwing vehicle.
        Has Reflector Armor, which is very good vs. Energy Weapons.
        Strong vs Infantry, Buildings, Energy Weapons
        Weak vs Conventional Weapons, Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, and energy attackers thanks to reflector armor.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs conventional tank guns and Aircraft bombardment.
    .tooltipextras-attributes = Attributes: • Heavy Flame Tank upgraded with Reflector Armor.

### vehicles-hftk-reflector-fireball

vehicles-hftk-reflector-fireball =
    .buildable-description =
        Heavily armored fireball-throwing vehicle.
        Has Reflector Armor, which is very good vs. Energy Weapons.
        Strong vs Infantry, Buildings, Vehicles, Energy Weapons
        Weak vs Conventional Weapons, Aircraft
    .tooltipextras-strengths = Strengths: • Strong vs Infantry, Buildings, Vehicles while shrugging off energy attacks.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs conventional cannons and Aircraft keeping their distance.
    .tooltipextras-attributes = Attributes: • Fireball launcher plus Reflector Armor on the heavy chassis.

### vehicles-ttrk

vehicles-ttrk =
    .buildable-description = Truck actively armed with a Tiberium bomb. Has very weak armor.
    .tooltipextras-strengths = Strengths: • Strong vs clustered ground targets when the Tiberium bomb detonates.
    .tooltipextras-weaknesses = Weaknesses: • Weak vs any weapons before detonation due to extremely light armor.
    .tooltipextras-attributes = Attributes: • Suicide truck armed with a volatile Tiberium bomb.

