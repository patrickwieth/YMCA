# Agent API Documentation

## Overview

The Agent API provides programmatic control of OpenRA games via HTTP. This enables external AI/ML agents, automation scripts, or tools to:

- Start and control games automatically
- Observe game state (units, buildings, resources)
- Issue orders to units (move, attack, build)
- Control a local human player without bot limitations

The API runs on `localhost:8080` and starts automatically with the game engine.

## Quick Start

### Enabling the Agent API

**IMPORTANT:** The Agent API is disabled by default. Enable it via command-line parameter:

```bash
# Enable Agent API with command-line parameter
./launch-game.sh Server.AgentAPI=true Graphics.Mode=Windowed
```

### Starting a Game with Agent Control

```bash
# Start game directly on a map (local player with agent control)
./launch-game.sh Launch.Map=<map-uid> Graphics.Mode=Windowed Server.AgentAPI=true &

# Wait for API to be ready
sleep 10

# Verify API is running
curl http://localhost:8080/api/ping
```

### Getting Game State

```bash
curl http://localhost:8080/api/state | python3 -m json.tool
```

### Controlling Units

```bash
# Move a unit
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "command": "move",
    "units": [76],
    "targetCell": {"x": 50, "y": 84},
    "queued": false
  }'
```

## API Endpoints

### `GET /api/ping`

Health check to verify the API server is running.

**Response:**
```json
{
  "status": "ok",
  "timestamp": 1234567890
}
```

### `GET /api/game/status`

Get current game status.

**Response:**
```json
{
  "status": "running",
  "tick": 1234,
  "player": "Commander",
  "agentActive": true
}
```

Possible status values:
- `"menu"` - Game is in main menu
- `"running"` - Game is actively running
- `"stopped"` - No game currently active

### `GET /api/state`

Get complete game state including units, buildings, enemies, and resources.

**Requirements:** An agent-controlled player must be active in the game.

**Response:**
```json
{
  "tick": 1234,
  "cash": 20000,
  "resources": 0,
  "powerProvided": 100,
  "powerDrained": 50,
  "units": [
    {
      "id": 76,
      "type": "mcv",
      "position": {"x": 4096, "y": 8192},
      "cell": {"x": 45, "y": 84},
      "health": {"current": 600, "max": 600},
      "isIdle": true,
      "canMove": true,
      "canAttack": false
    }
  ],
  "buildings": [
    {
      "id": 123,
      "type": "fact",
      "cell": {"x": 50, "y": 80},
      "health": {"current": 1000, "max": 1000},
      "isProducing": false
    }
  ],
  "enemies": [
    {
      "id": 456,
      "type": "e1",
      "position": {"x": 6144, "y": 10240},
      "cell": {"x": 60, "y": 100},
      "owner": "Enemy",
      "health": {"current": 100, "max": 100},
      "isBuilding": false
    }
  ]
}
```

**Field Descriptions:**
- `tick` - Current game tick (increments each frame)
- `cash` - Available money
- `resources` - Stored resources (ore/tiberium)
- `powerProvided` / `powerDrained` - Power system stats
- `units[]` - All mobile units owned by the player
- `buildings[]` - All buildings owned by the player
- `enemies[]` - All visible enemy units/buildings

**Unit/Building Fields:**
- `id` - Unique actor ID (use for issuing orders)
- `type` - Actor type name (e.g., "mcv", "e1", "fact")
- `position` - World position in internal coordinates
- `cell` - Map grid cell coordinates (X, Y)
- `health` - Current and maximum HP
- `isIdle` - Whether unit has no current orders
- `canMove` / `canAttack` - Capability flags

### `POST /api/orders`

Issue orders to units.

**Request Body:**

#### Move Order
```json
{
  "command": "move",
  "units": [76, 77, 78],
  "targetCell": {"x": 50, "y": 84},
  "queued": false
}
```

#### Attack-Move Order
```json
{
  "command": "attack",
  "units": [76],
  "targetCell": {"x": 60, "y": 100},
  "queued": false
}
```

#### Attack Target Order
```json
{
  "command": "attack",
  "units": [76],
  "targetActorId": 456,
  "queued": false
}
```

#### Stop Order
```json
{
  "command": "stop",
  "units": [76, 77, 78]
}
```

#### Build Order
```json
{
  "command": "build",
  "buildingType": "fact"
}
```

**Parameters:**
- `command` - Order type: `"move"`, `"attack"`, `"stop"`, or `"build"`
- `units` - Array of actor IDs to issue orders to
- `targetCell` - Target map cell (required for move/attack-move)
- `targetActorId` - Target actor ID (for attack orders)
- `queued` - If `true`, adds order to queue; if `false`, replaces current orders
- `buildingType` - Building type to produce (for build orders)

**Response:**
```json
{
  "success": true,
  "message": "Move order issued to 3 units"
}
```

### `POST /api/game/start`

Start a new game with specified configuration.

**Request Body:**
```json
{
  "mapUid": "50ff8481b93ec70c20ebb08cc298ffeb49d1cb2a",
  "gameSpeed": "fastest",
  "bots": {
    "bot1": {
      "slot": "Multi1",
      "botType": "normal",
      "faction": "allies",
      "team": 1
    }
  }
}
```

**Parameters:**
- `mapUid` - Map UID (from map file or use alias like "all-connected")
- `gameSpeed` - Game speed: `"slowest"`, `"slower"`, `"normal"`, `"faster"`, `"fastest"`
- `bots` - Dictionary of bot configurations (optional)
  - `slot` - Player slot ID (e.g., "Multi0", "Multi1")
  - `botType` - Bot AI type: `"normal"`, `"hard"`, `"brutal"`, `"agent"`
  - `faction` - Faction name (optional)
  - `team` - Team number (optional)

**Response:**
```json
{
  "success": true,
  "message": "Game started successfully",
  "mapUid": "50ff8481b93ec70c20ebb08cc298ffeb49d1cb2a"
}
```

### `POST /api/game/stop`

Stop the current game and return to main menu.

**Response:**
```json
{
  "success": true,
  "message": "Game stopped"
}
```

### `POST /api/game/kill`

Immediately terminate the game process.

**Response:**
```json
{
  "success": true,
  "message": "Game will exit"
}
```

**Note:** This uses `Environment.Exit(0)` for immediate shutdown. Recommended for automated testing cleanup.

## Usage Modes

### Mode 1: Local Player Control (Recommended)

Control a regular human player without bot limitations.

**Advantages:**
- Full player features (normal fog of war, no bot constraints)
- Can play with/against other players or bots
- Realistic environment for AI/ML development

**Setup:**
```bash
# Start game on a map
./launch-game.sh Launch.Map=<map-uid> Graphics.Mode=Windowed &

# Wait for API
sleep 10

# Use API normally
curl http://localhost:8080/api/state
```

### Mode 2: Bot-Based Control

Control a dedicated agent bot (legacy mode).

**Advantages:**
- Can be started via API without manual game launch
- Multiple bots can be controlled simultaneously

**Setup:**
```bash
# Start game
./launch-game.sh Graphics.Mode=Windowed &
sleep 5

# Start game with agent bot via API
curl -X POST http://localhost:8080/api/game/start \
  -H "Content-Type: application/json" \
  -d '{
    "mapUid": "all-connected",
    "gameSpeed": "fastest",
    "bots": {
      "bot1": {
        "slot": "Multi0",
        "botType": "agent"
      }
    }
  }'
```

## Python Example

```python
import requests
import time

# Start game (or use Launch.Map parameter)
API_BASE = "http://localhost:8080"

# Wait for API
while True:
    try:
        response = requests.get(f"{API_BASE}/api/ping", timeout=1)
        if response.ok:
            break
    except:
        pass
    time.sleep(1)

print("API ready!")

# Get game state
state = requests.get(f"{API_BASE}/api/state").json()
print(f"Tick: {state['tick']}, Cash: ${state['cash']}")
print(f"Units: {len(state['units'])}, Buildings: {len(state['buildings'])}")

# Find and move first idle unit
for unit in state['units']:
    if unit['isIdle'] and unit['canMove']:
        target = {
            'x': unit['cell']['x'] + 5,
            'y': unit['cell']['y']
        }

        response = requests.post(f"{API_BASE}/api/orders", json={
            'command': 'move',
            'units': [unit['id']],
            'targetCell': target,
            'queued': False
        })

        print(f"Moved unit {unit['id']} ({unit['type']}) to {target}")
        break

# Wait and verify movement
time.sleep(3)
new_state = requests.get(f"{API_BASE}/api/state").json()
print(f"New position: {new_state['units'][0]['cell']}")
```

## Configuration

### Command-Line Activation (Recommended)

The Agent API is **disabled by default** and should be enabled via command-line:

```bash
./launch-game.sh Server.AgentAPI=true
```

This overrides the YAML configuration and enables both the HTTP server and player interface.

### YAML Configuration (Optional)

You can also enable the API permanently in YAML files:

**World Configuration (`mods/ca/rules/world.yaml`):**

```yaml
World:
  GlobalAgentServer:
    Port: 8080
    Enabled: false  # Set to true to enable by default
```

**Parameters:**
- `Port` - HTTP server port (default: 8080)
- `Enabled` - Enable/disable the HTTP server (default: false)

**Player Configuration (`mods/ca/rules/player.yaml`):**

```yaml
Player:
  AgentInterface:
    Enabled: false  # Set to true to enable by default
    MinOrderQuotientPerTick: 5
```

**Parameters:**
- `Enabled` - Enable/disable agent control for local players (default: false)
- `MinOrderQuotientPerTick` - Order throttling (higher = slower execution, default: 5)

**Note:** Command-line parameter `Server.AgentAPI=true` overrides YAML `Enabled: false`.

## Finding Map UIDs

Map UIDs can be found in several ways:

### Method 1: Map File
```bash
# Map files are in mods/ca/maps/
ls -la mods/ca/maps/*.oramap

# UID is the filename without .oramap extension
```

### Method 2: Alias Names
Some maps have alias names:
- `"all-connected"` - Common test map

### Method 3: Via API (when in lobby)
Start a game manually, then check game status to see the current map UID.

## Troubleshooting

### API Not Responding

Check if game is running:
```bash
pgrep -f dotnet
```

Check OpenRA logs:
```bash
tail -f /tmp/openra.log
```

Restart game:
```bash
killall dotnet
./launch-game.sh Graphics.Mode=Windowed &
```

### "No Agent Active" Error

Make sure:
1. Game is running (not in main menu)
2. Local player is active OR agent bot is configured
3. AgentInterface is enabled in `rules/player.yaml`

### Orders Not Executing

Check:
1. Unit IDs are valid (get from `/api/state`)
2. Units belong to the controlled player
3. Target cells are valid map coordinates
4. Units have the required capabilities (e.g., `canMove`, `canAttack`)

### World Reference Issues

If actors are not found despite appearing in `/api/state`, the game may need to be restarted. This was a known issue that has been fixed in the current version.

## Implementation Details

The Agent API is implemented as a self-contained module in `OpenRA.Mods.CA/Traits/AgentAPI/`.

### Module Components

**Core Traits:**

1. **GlobalAgentServer** (`AgentAPI/GlobalAgentServer.cs`)
   - Singleton HTTP server that starts with the engine
   - Manages agent registration/deregistration
   - Runs even in shell/lobby state

2. **AgentHttpServer** (`AgentAPI/AgentHttpServer.cs`)
   - HTTP request handling and routing
   - JSON serialization/deserialization
   - Order parsing and validation
   - CORS support for web clients

3. **AgentInterface** (`AgentAPI/AgentInterface.cs`)
   - Player-level trait for game state observation
   - Order queue management
   - Auto-detection of local human players
   - Game state collection (units, buildings, enemies)

**Game Management:**

4. **GameLauncher** (`AgentAPI/GameLauncher.cs`)
   - Automated game start implementation
   - Bot configuration and validation

5. **GameStartConfig** (`AgentAPI/GameStartConfig.cs`)
   - Data models for API requests/responses

**Module Documentation:** See `OpenRA.Mods.CA/Traits/AgentAPI/README.md` for architecture details.

## Security Notes

- The API runs on `localhost` only (not exposed to network)
- No authentication is implemented (assumes trusted local access)
- Use in production environments is not recommended without additional security measures

## Further Documentation

- Implementation details: `docs/tasks/automated-bot-control.md`
- Codebase structure: `CLAUDE.md`
- Build instructions: `README.md`
