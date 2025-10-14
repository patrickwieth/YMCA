# Agent API Module

This module provides external programmatic control of OpenRA games via HTTP API.

## Components

### Core Traits

- **`AgentInterface.cs`** - Player-level trait that provides game state observation and order queuing
  - Auto-detects local human players
  - Collects game state (units, buildings, resources, enemies)
  - Queues and executes external orders
  - Registers with GlobalAgentServer

- **`GlobalAgentServer.cs`** - World-level trait that manages the HTTP server lifecycle
  - Singleton HTTP server that starts with the engine
  - Runs even in shell/lobby state
  - Manages agent registration/deregistration
  - Configured in `mods/ca/rules/world.yaml`

- **`AgentHttpServer.cs`** - HTTP request handler
  - Routes API endpoints
  - Parses JSON requests
  - Validates and issues orders
  - CORS support for web clients
  - Manages World reference for actor lookups

### Game Management

- **`GameLauncher.cs`** - Automated game start implementation
  - Creates local server
  - Configures bot players
  - Validates map and settings
  - Issues setup orders (slot_bot, slot_faction, startgame)

- **`GameStartConfig.cs`** - Data models for API requests
  - GameStartConfig - Map, speed, bots configuration
  - BotConfig - Bot slot, type, faction, team
  - GameStatusInfo - Game state response
  - Validation logic

## API Endpoints

- `GET /api/ping` - Health check
- `GET /api/game/status` - Current game status
- `GET /api/state` - Complete game state (requires active agent)
- `POST /api/orders` - Issue unit commands (move, attack, stop, build)
- `POST /api/game/start` - Start new game with configuration
- `POST /api/game/stop` - Stop current game
- `POST /api/game/kill` - Exit game process

## Configuration

### Command-Line Activation (Recommended)

The Agent API is **disabled by default**. Enable it via command-line:

```bash
./launch-game.sh Server.AgentAPI=true
```

### YAML Configuration (Optional)

**World Level** (`mods/ca/rules/world.yaml`):
```yaml
World:
  GlobalAgentServer:
    Port: 8080        # HTTP server port
    Enabled: false    # Disabled by default (override with Server.AgentAPI=true)
```

**Player Level** (`mods/ca/rules/player.yaml`):
```yaml
Player:
  AgentInterface:
    Enabled: false                 # Disabled by default (override with Server.AgentAPI=true)
    MinOrderQuotientPerTick: 5     # Order throttling
```

**Note:**
- Command-line parameter `Server.AgentAPI=true` overrides YAML `Enabled: false`
- HTTP server port is configured **only** in `GlobalAgentServer` (world.yaml)

## Usage

The Agent API automatically activates when a local human player is created. No bot configuration needed.

**Example:**
```bash
# Start game on a map
./launch-game.sh Launch.Map=<map-uid> Graphics.Mode=Windowed

# Access API
curl http://localhost:8080/api/state
curl -X POST http://localhost:8080/api/orders -d '{"command":"move",...}'
```

## Architecture

```
┌─────────────────────────────────────┐
│   GlobalAgentServer (World Trait)  │  ← Starts with engine
│   - HTTP Server Singleton           │
│   - Agent Registration              │
└────────────────┬────────────────────┘
                 │
                 │ RegisterAgent()
                 ▼
┌─────────────────────────────────────┐
│   AgentInterface (Player Trait)     │  ← Per local player
│   - Game State Collection           │
│   - Order Queue Management          │
└────────────────┬────────────────────┘
                 │
                 │ GetGameState() / QueueOrder()
                 ▼
┌─────────────────────────────────────┐
│   AgentHttpServer                   │  ← HTTP Request Handler
│   - Endpoint Routing                │
│   - JSON Serialization              │
│   - Order Parsing                   │
└─────────────────────────────────────┘
```

## Thread Safety

- HTTP server runs on background thread
- Orders are queued and issued on game thread via `ITick`
- Order throttling via `MinOrderQuotientPerTick` prevents flooding

## See Also

- **API Documentation:** `/docs/AGENT_API.md`
- **Implementation Notes:** `/docs/tasks/automated-bot-control.md`
- **Project Guide:** `/CLAUDE.md`
