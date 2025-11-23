# You Must Construct Additional ...

## What is this?
This is a RTS build on OpenRA engine. The focus is on insane mass battles, fast development of the game, while keeping required APM (actions-per-minute) as low as possible.

Find more information on [ModDB](https://www.moddb.com/mods/you-must-construct-additional1) or visit the [Discord](https://discord.gg/maGHYC3cVk) to find other players.

## Agent API

This mod includes a **built-in HTTP API** for programmatic game control, enabling AI/ML agents and automation:

```bash
# Start game with agent control (API disabled by default, enable with Server.AgentAPI=true)
./launch-game.sh Launch.Map=<map-uid> Graphics.Mode=Windowed Server.AgentAPI=true

# Get game state
curl http://localhost:8080/api/state

# Control units
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"command":"move","units":[76],"targetCell":{"x":50,"y":84}}'
```

**Full API documentation:** [docs/AGENT_API.md](docs/AGENT_API.md)

**Features:**
- Observe game state (units, buildings, resources, enemies)
- Issue orders (move, attack, build, stop)
- Control local human player or dedicated agent bots
- RESTful JSON API on localhost:8080

## Building and Running

### Prerequisites
- .NET 6.0 SDK or higher
- Linux, macOS, or Windows

### Build
```bash
make
```

### Run
```bash
./launch-game.sh
```

### Test
```bash
make test           # Validate YAML configuration
make check          # Check code style
make check-scripts  # Validate Lua scripts
```

## Plan for future releases

**0.96** - Combined Arms units (vehicles, planes, infantry) replaced with copyright fine material \
**0.97** - Combined Arms buildings replaced with copyright fine material \
**0.98** - **Differentiated Factions** means that the sub-factions play differently and are more than just a little flavor change towards other sub-factions. This is worked on already in 0.96 and 0.97 as well. \
**0.99** - Testing of Autobattle mode and testing of differentiated Factions, especially balance. \
**1.0** - **Autobattle** mode working \
**1.1** - GLA added \
**1.2** - USA added 
