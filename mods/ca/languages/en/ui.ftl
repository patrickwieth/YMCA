## Ui strings


## MapOptions, MissionBrowserLogic

slowest = Slowest
slower = Slower
fast = Fast
faster = Faster
fastest = Fastest
map-bots-disabled = Bots have been disabled on this map.
unknown-server-command = Unknown server command: { $command }
lobby-disconnected = { $player } has left.
observer-disconnected = { $player } (Spectator) has disconnected.
you-were-kicked = You have been kicked from the server.
kicked = { $admin } kicked { $player } from the server.
admin-kick = Only the host can kick players.
kick-none = No-one in that slot.
no-kick-game-started = Only spectators can be kicked after the game has started.
admin-lobby-info = Only the host can set lobby info.
invalid-lobby-info = Invalid lobby info sent.
master-server-connected = Master server communication established.
master-server-error = "Master server communication failed."

## PlayerMessageTracker

chat-temp-disabled =
    { $remaining ->
        [one] Chat is disabled. Please try again in { $remaining } second.
       *[other] Chat is disabled. Please try again in { $remaining } seconds.
    }
chat = Chat
kick-title = Kick { $player }?
kick-prompt = They will not be able to rejoin this game.
kick-accept = Kick

## LobbyLogic, InGameChatLogic

chat-disabled = Chat Disabled
chat-availability =
    { $seconds ->
        [one] Chat available in { $seconds } second...
       *[other] Chat available in { $seconds } seconds...
    }

## KickClientLogic

kick-client = Kick { $player }?

## KickSpectatorsLogic

kick-spectators =
    { $count ->
        [one] Are you sure you want to kick one spectator?
       *[other] Are you sure you want to kick { $count } spectators?
    }
map-size-huge = (Huge)
map-size-large = (Large)
map-size-medium = (Medium)
map-size-small = (Small)
map-deletion-failed = Failed to delete map '{ $map }'. See the debug.log file for details.

## ServerCreationLogic

internet-server-nat-A = Internet Server (UPnP/NAT-PMP
internet-server-nat-B-enabled = Enabled
internet-server-nat-B-not-supported = Not Supported
internet-server-nat-B-disabled = Disabled
internet-server-nat-C = ):
local-server = Local Server:
server-creation-failed-prompt = Could not listen on port { $port }
server-creation-failed-port-used = Check if the port is already being used.
server-creation-failed-error = Error is: "{ $message }" ({ $code })
server-creation-failed-title = Server Creation Failed
server-creation-failed-cancel = Back
no-server-selected = No Server Selected
map-status-searching = Searching...
map-classification-unknown = Unknown Map
server-shutting-down = Server shutting down
unknown-server-state = Unknown server state
fast-charge = toggles almost instant support power charging.

### hotkeys-hotkeys

hotkeys-hotkeys =
    .description = Building Tab
    .description-2 = Defense Tab
    .description-3 = Infantry Tab
    .description-4 = Vehicle Tab
    .description-5 = Aircraft Tab
    .description-6 = Naval Tab
    .description-7 = Commander Tree
    .description-8 = Power-down mode
promotions =
    .lobby-description = Configure the number of commander points available each match.



productionpalette-sidebar-production-palette.ready = Ready
productionpalette-sidebar-production-palette.hold = On Hold

commander-tree-promotion-oil-pumps =
    .description = Unlocks Oil Pumps and Oil Platforms for sustained income.
    .tooltipextras-strengths = Strengths: \u0007 Unlocks Oil Pump and Oil Platform economy structures.
    .tooltipextras-weaknesses = Weaknesses: \u0007 Requires additional power and protection for vulnerable economy structures.
    .tooltipextras-attributes = Attributes: \u0007 Grants Supply Truck cash deliveries to teammates.

