#!/bin/bash

set -e

echo "=== OpenRA Game Smoke Test ==="
echo "This test verifies basic game functionality using the Agent API"
echo ""

# Configuration
MAP_UID="all-connected"
API_URL="http://localhost:8080"
GAME_LOG="/tmp/openra-smoke-test.log"
MAX_WAIT_SECONDS=60
API_READY_WAIT=30

# Cleanup function
cleanup() {
    echo ""
    echo "=== Cleanup ==="
    if [ -n "$GAME_PID" ]; then
        echo "Stopping game process (PID: $GAME_PID)..."
        curl -X POST "$API_URL/api/game/kill" 2>/dev/null || true
        sleep 2
        kill -9 $GAME_PID 2>/dev/null || true
    fi

    if [ -f "$GAME_LOG" ]; then
        echo "Game log saved to: $GAME_LOG"
        if [ "$TEST_FAILED" = "true" ]; then
            echo ""
            echo "=== Last 50 lines of game log ==="
            tail -n 50 "$GAME_LOG"
        fi
    fi
}

trap cleanup EXIT

# Start game in background (main menu, with Agent API enabled)
echo "Starting game..."
./launch-game.sh \
    Graphics.Mode=Windowed \
    Server.AgentAPI=true \
    > "$GAME_LOG" 2>&1 &

GAME_PID=$!
echo "Game started with PID: $GAME_PID"

# Wait for API to be ready
echo "Waiting for API to be ready (max ${API_READY_WAIT}s)..."
START_TIME=$(date +%s)
API_READY=false

while [ $(($(date +%s) - START_TIME)) -lt $API_READY_WAIT ]; do
    if curl -s "$API_URL/api/ping" > /dev/null 2>&1; then
        API_READY=true
        echo "✓ API is ready!"
        break
    fi

    # Check if game process is still running
    if ! kill -0 $GAME_PID 2>/dev/null; then
        echo "✗ Game process died during startup"
        TEST_FAILED=true
        exit 1
    fi

    sleep 1
done

if [ "$API_READY" = "false" ]; then
    echo "✗ API did not become ready within ${API_READY_WAIT}s"
    TEST_FAILED=true
    exit 1
fi

# Start a game via API
echo "Starting game on map '$MAP_UID' via API..."
START_RESULT=$(curl -s -X POST "$API_URL/api/game/start" \
    -H "Content-Type: application/json" \
    -d "{\"mapUid\":\"$MAP_UID\",\"gameSpeed\":\"fastest\",\"bots\":{\"bot1\":{\"slot\":\"Multi0\",\"botType\":\"normal\"}}}")

if ! echo "$START_RESULT" | grep -q '"success":true'; then
    echo "✗ Failed to start game via API: $START_RESULT"
    TEST_FAILED=true
    exit 1
fi

echo "✓ Game start initiated via API"

# Wait for game to fully load (units to spawn)
echo "Waiting for game to load and units to spawn (max ${MAX_WAIT_SECONDS}s)..."
START_TIME=$(date +%s)
GAME_LOADED=false

while [ $(($(date +%s) - START_TIME)) -lt $MAX_WAIT_SECONDS ]; do
    # Get game state
    STATE=$(curl -s "$API_URL/api/state" 2>/dev/null || echo "")

    if [ -n "$STATE" ]; then
        # Check if we have units (game has loaded)
        UNIT_COUNT=$(echo "$STATE" | grep -o '"units":\[' | wc -l)
        if [ "$UNIT_COUNT" -gt 0 ]; then
            # Check if units array is not empty
            if ! echo "$STATE" | grep -q '"units":\[\]'; then
                GAME_LOADED=true
                echo "✓ Game loaded successfully!"
                break
            fi
        fi
    fi

    # Check if game process is still running
    if ! kill -0 $GAME_PID 2>/dev/null; then
        echo "✗ Game process crashed during load"
        TEST_FAILED=true
        exit 1
    fi

    sleep 2
done

if [ "$GAME_LOADED" = "false" ]; then
    echo "✗ Game did not load within ${MAX_WAIT_SECONDS}s"
    TEST_FAILED=true
    exit 1
fi

# Get initial game state
echo ""
echo "=== Testing Game State ==="
STATE=$(curl -s "$API_URL/api/state")

# Extract tick count
TICK=$(echo "$STATE" | grep -o '"tick":[0-9]*' | grep -o '[0-9]*')
echo "Current tick: $TICK"

# Count units
UNITS=$(echo "$STATE" | grep -o '"id":[0-9]*' | wc -l)
echo "Units found: $UNITS"

if [ "$UNITS" -eq 0 ]; then
    echo "✗ No units found in game state"
    TEST_FAILED=true
    exit 1
fi

echo "✓ Game state looks valid"

# Test unit movement (verify game loop is working)
echo ""
echo "=== Testing Unit Control ==="

# Get first unit
FIRST_UNIT_ID=$(echo "$STATE" | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*')
UNIT_CELL_X=$(echo "$STATE" | grep -A2 "\"id\":$FIRST_UNIT_ID" | grep -o '"x":[0-9]*' | head -1 | grep -o '[0-9]*')
UNIT_CELL_Y=$(echo "$STATE" | grep -A2 "\"id\":$FIRST_UNIT_ID" | grep -o '"y":[0-9]*' | head -1 | grep -o '[0-9]*')

echo "Moving unit $FIRST_UNIT_ID from ($UNIT_CELL_X,$UNIT_CELL_Y) to ($((UNIT_CELL_X + 5)),$UNIT_CELL_Y)..."

# Issue move order
MOVE_RESULT=$(curl -s -X POST "$API_URL/api/orders" \
    -H "Content-Type: application/json" \
    -d "{\"command\":\"move\",\"units\":[$FIRST_UNIT_ID],\"targetCell\":{\"x\":$((UNIT_CELL_X + 5)),\"y\":$UNIT_CELL_Y},\"queued\":false}")

if ! echo "$MOVE_RESULT" | grep -q '"success":true'; then
    echo "✗ Move order failed: $MOVE_RESULT"
    TEST_FAILED=true
    exit 1
fi

echo "✓ Move order issued successfully"

# Wait for game to process the order
echo "Waiting 5 seconds for game to process order..."
sleep 5

# Verify game is still running (no crashes/deadlocks)
if ! kill -0 $GAME_PID 2>/dev/null; then
    echo "✗ Game crashed after issuing order"
    TEST_FAILED=true
    exit 1
fi

# Get new state to verify game loop is still working
NEW_STATE=$(curl -s "$API_URL/api/state")
NEW_TICK=$(echo "$NEW_STATE" | grep -o '"tick":[0-9]*' | grep -o '[0-9]*')

if [ "$NEW_TICK" -le "$TICK" ]; then
    echo "✗ Game appears to be frozen (tick not advancing: $TICK -> $NEW_TICK)"
    TEST_FAILED=true
    exit 1
fi

echo "✓ Game is still running (tick: $TICK -> $NEW_TICK)"

# All tests passed
echo ""
echo "=== All Smoke Tests Passed! ==="
echo "✓ Game starts without crashing"
echo "✓ Game loads map successfully"
echo "✓ Units spawn correctly"
echo "✓ Commands can be issued"
echo "✓ Game loop processes orders"
echo "✓ No freezes or deadlocks detected"
echo ""

exit 0
