# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**You Must Construct Additional...** is an RTS mod built on the OpenRA engine, focused on massive battles with low APM requirements. The mod features multiple factions (Allies, Soviet, China, GLA, GDI, NOD, Scrin) with differentiated gameplay and is working towards an Autobattle mode.

**ModDB**: https://www.moddb.com/mods/you-must-construct-additional1
**Discord**: https://discord.gg/maGHYC3cVk

## Build System

**IMPORTANT: This project uses .NET 6+, not Mono.**

### Primary Commands

```bash
# Build the mod (uses .NET 6)
make

# Clean build artifacts
make clean

# Check code style (StyleCop violations)
make check

# Test mod YAML for errors
make test

# Check Lua scripts for syntax errors
make check-scripts

# Set mod version
make version VERSION="custom-version"
```

### Running the Game

```bash
# Launch game
./launch-game.sh

# Launch game in windowed mode (required for agent interface)
./launch-game.sh Graphics.Mode=Windowed

# Launch dedicated server
./launch-dedicated.sh

# Run utility commands
./utility.sh
```

### Important Configuration Files

- **mod.config** - Core mod configuration (MOD_ID, ENGINE_VERSION, packaging settings)
- **mods/ca/mod.yaml** - Main mod manifest (packages, rules, sequences, assemblies)
- **CombinedArms.sln** - Visual Studio solution for custom C# code

## Architecture

### OpenRA SDK Structure

This project uses the OpenRA Mod SDK pattern:

1. **engine/** - OpenRA engine (automatically fetched, read-only dependency)
   - OpenRA.Game - Core game engine
   - OpenRA.Mods.Common - Common mod functionality
   - OpenRA.Mods.Cnc - C&C-specific features

2. **OpenRA.Mods.CA/** - Custom C# code for this mod
   - Extends engine functionality with mod-specific traits
   - Compiled into OpenRA.Mods.CA.dll
   - References engine projects but engine doesn't reference this

3. **mods/ca/** - Game data and content
   - YAML configurations for units, buildings, AI, weapons
   - Maps, sequences, audio, graphics
   - Localization files

### Custom Code Organization (OpenRA.Mods.CA/)

The mod extends OpenRA with custom traits organized by category:

- **Traits/** - Core trait implementations
  - **AgentAPI/** - External HTTP API for programmatic game control (see module README)
    - AgentInterface.cs - Player-level game state observation and order queuing
    - GlobalAgentServer.cs - HTTP server singleton
    - AgentHttpServer.cs - HTTP request handling
    - GameLauncher.cs - Automated game start
    - GameStartConfig.cs - API data models
  - **SupportPowers/** - Custom support powers (airstrikes, reveals, etc.)
  - **BotModules/** - AI behavior (squad management, unit logic)
  - **Player/** - Player-specific traits
  - **Air/, Attack/, Render/, Sound/** - Unit behavior categories
  - **Conditions/, Modifiers/, Multipliers/** - Stat modification systems
  - **World/** - World-level traits

- **Activities/** - Unit action implementations
- **Effects/** - Visual/gameplay effects
- **Graphics/, LoadScreens/** - Rendering
- **Orders/** - Player commands
- **Projectiles/** - Weapon projectile behavior
- **Warheads/** - Weapon damage/effect logic
- **Widgets/** - UI components
- **Scripting/** - Lua scripting support

### Mod Data Organization (mods/ca/)

Game configuration uses YAML files organized by category and faction:

**rules/** - Unit/building/game rules
- **ai/** - AI configurations (easy, normal, hard, brutal, naval, agent)
- **allies/, soviet/, china/, gla/, gdi/, nod/, scrin/** - Faction-specific rules
  - Each faction has: ai.yaml, aircraft.yaml, commander-tree.yaml, defaults.yaml, husks.yaml, infantry.yaml, misc.yaml, structures.yaml, vehicles.yaml, weapons.yaml
- Base files: defaults.yaml, vehicles.yaml, structures.yaml, infantry.yaml, aircraft.yaml, player.yaml, world.yaml, etc.

**sequences/** - Sprite/animation definitions
**weapons/** - Weapon definitions
**audio/** - Sound/music configuration
**chrome/** - UI layouts
**maps/** - Game maps (.oramap files)
**bits/** - Asset folders (graphics/sounds organized by faction and tileset)
**tilesets/** - Terrain definitions

### Faction Structure

The mod features 7+ factions with differentiated gameplay:
- **Allies** (Red Alert style)
- **Soviet** (Red Alert style)
- **China** (Generals style)
- **GLA** (Generals style, in development)
- **GDI** (Tiberium style)
- **NOD** (Tiberium style)
- **Scrin** (Tiberium style)

Each faction has consistent file organization across rules/, sequences/, and bits/ directories.

## Agent Interface System

The mod includes an **external agent interface** for programmatic game control via HTTP API.

**Module Location:** `OpenRA.Mods.CA/Traits/AgentAPI/`
**Module Documentation:** `OpenRA.Mods.CA/Traits/AgentAPI/README.md`
**API Documentation:** `docs/AGENT_API.md`

### Module Overview

The AgentAPI module provides a self-contained HTTP API implementation with the following components:

1. **GlobalAgentServer** - World-level HTTP server singleton
2. **AgentInterface** - Player-level game state observation and order queuing
3. **AgentHttpServer** - HTTP request handling and routing
4. **GameLauncher** - Automated game start functionality
5. **GameStartConfig** - Data models for API requests

The module automatically activates for local human players (no bot configuration needed).

### API Endpoints

- `GET /api/ping` - Health check
- `GET /api/state` - Get current game state (units, buildings, resources)
- `POST /api/orders` - Issue unit commands (move, attack, stop, build)
- `POST /api/game/start` - Start a new game with configuration
- `POST /api/game/stop` - Stop current game
- `POST /api/game/kill` - **Kill/exit the game process immediately** (recommended for cleanup)
- `GET /api/game/status` - Get game status

### Using the Agent API

**IMPORTANT: Always start the game in windowed mode when using the agent interface.**

#### Workflow 1: Direct Map Start (Recommended)

Start the game directly on a specific map. The Agent API activates automatically for the local player.

**IMPORTANT:** Add `Server.AgentAPI=true` to enable the Agent API (disabled by default).

```bash
# Find map UID from map file (e.g., mods/ca/maps/some-map.oramap -> UID is filename without .oramap)
# Or use known UIDs like: 50ff8481b93ec70c20ebb08cc298ffeb49d1cb2a

# Start game on map in background WITH Agent API enabled
./launch-game.sh Launch.Map=50ff8481b93ec70c20ebb08cc298ffeb49d1cb2a Graphics.Mode=Windowed Server.AgentAPI=true >/tmp/openra.log 2>&1 &

# Wait for API to be ready
until curl -s http://localhost:8080/api/ping >/dev/null 2>&1; do
    echo "Waiting for API..."
    sleep 1
done
echo "API ready!"

# Wait a bit more for game to fully load
sleep 5

# Get game state
curl -s http://localhost:8080/api/state | python3 -m json.tool

# Control units (example: move unit 76 to cell 50,84)
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "command": "move",
    "units": [76],
    "targetCell": {"x": 50, "y": 84},
    "queued": false
  }'
```

#### Workflow 2: API-Started Game with Bots

Start the game in main menu, then use the API to create a match with bots.

```bash
# Start game in main menu WITH Agent API enabled
./launch-game.sh Graphics.Mode=Windowed Server.AgentAPI=true >/tmp/openra.log 2>&1 &

# Wait for API
until curl -s http://localhost:8080/api/ping >/dev/null 2>&1; do
    sleep 1
done

# Start a game via API with agent bot
curl -X POST http://localhost:8080/api/game/start \
  -H "Content-Type: application/json" \
  -d '{
    "mapUid": "50ff8481b93ec70c20ebb08cc298ffeb49d1cb2a",
    "gameSpeed": "fastest",
    "bots": {
      "bot1": {
        "slot": "Multi0",
        "botType": "agent"
      },
      "enemy1": {
        "slot": "Multi1",
        "botType": "normal"
      }
    }
  }'

# Wait for game to start and units to spawn
sleep 15

# Now use API as in Workflow 1
```

#### Stopping the Game

```bash
# Recommended: Kill via API (cleanest method)
curl -X POST http://localhost:8080/api/game/kill

# Alternative: Force kill all dotnet processes
killall dotnet
```

The `/api/game/kill` endpoint uses `Environment.Exit(0)` for immediate, clean shutdown.

See **docs/AGENT_API.md** for complete API documentation.

## Development Workflow

### Adding New Units/Buildings

1. Define actor in appropriate rules YAML (e.g., `rules/allies/vehicles.yaml`)
2. Add sprite sequences in `sequences/` directory
3. Add weapon definitions in `weapons/` if needed
4. Place assets in `bits/` organized by faction/tileset
5. Test with `make test` to validate YAML

### Creating Custom Traits

1. Add C# class in `OpenRA.Mods.CA/Traits/` (organized by category)
2. Extend `TraitInfo` for trait definition
3. Implement trait class with game logic
4. Build with `make`
5. Reference trait in YAML rules files
6. Use `make check` to verify code style

### Modifying AI Behavior

- AI configurations: `rules/ai/` (easy, normal, hard, brutal, naval, agent)
- Custom AI logic: `OpenRA.Mods.CA/Traits/BotModules/`
- Faction-specific AI: `rules/{faction}/ai.yaml`

### Working with Maps

- Maps stored in `mods/ca/maps/` as `.oramap` files
- Edit maps using OpenRA's built-in editor
- Launch game and use in-game map editor

## Code Style

- **C# files**: Tab indentation (4 spaces), LF line endings
- **YAML files**: Tab indentation (4 spaces), LF line endings
- Follow OpenRA engine conventions (see engine code for examples)
- Use StyleCop rules: `make check` validates code style

### Comments Policy

**Comments should explain WHY, not WHAT**

- Write self-explanatory code that shows *what* it does
- Use comments only to explain *why* something is done a certain way
- Only comment when it's not obvious from the code itself
- Avoid redundant comments that merely describe what the code does

**Good examples:**
```csharp
// World reference must be updated here because Shell Map world differs from active game world
world = agent.Player.World;

// Use Environment.Exit instead of Game.Exit because Game.Exit doesn't properly terminate in all cases
Environment.Exit(0);
```

**Bad examples:**
```csharp
// Set world to agent's world
world = agent.Player.World;

// Exit the game
Environment.Exit(0);
```

## Common Issues

### Engine Version Mismatch

If engine files are outdated or corrupted:
```bash
rm -rf engine/
make engine
```

### YAML Validation Errors

Always validate YAML after changes:
```bash
make test
```

### Build Issues

Ensure .NET 6+ SDK is installed:
```bash
dotnet --version  # Should be 6.0 or higher
```

## Engine Dependencies

The mod depends on specific OpenRA engine DLLs:
- **OpenRA.Game.dll** - Core engine
- **OpenRA.Mods.Common.dll** - Common mod traits
- **OpenRA.Mods.Cnc.dll** - C&C-specific features (configured in mod.config)

Engine version is pinned via `ENGINE_VERSION` in mod.config and automatically fetched from:
https://github.com/patrickwieth/OpenRA (forked OpenRA engine)

## Packaging

Build installers using the packaging scripts:
```bash
./packaging/package-all.sh
```

Packaging configuration in **mod.config** defines:
- Installer names
- Display names
- Dependencies (CNC DLL, D2K DLL)
- Discord integration
- Platform-specific settings
