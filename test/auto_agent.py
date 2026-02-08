import requests
import time
import math
import sys

BASE_URL = "http://localhost:8091"

def invoke(endpoint, method="GET", body=None):
    try:
        url = f"{BASE_URL}{endpoint}"
        if method == "POST":
            resp = requests.post(url, json=body, timeout=1.0)
        else:
            resp = requests.get(url, timeout=1.0)
        if resp.status_code == 200:
            return resp.json()
    except Exception as e:
        print(f"Error invoking {endpoint}: {e}")
    return None

def get_distance(p1, p2):
    return math.sqrt((p1['x'] - p2['x'])**2 + (p1['y'] - p2['y'])**2)

def get_closest(curr_pos, items):
    if not items:
        return None
    closest = None
    min_dist = float('inf')
    for item in items:
        dist = get_distance(curr_pos, item)
        if dist < min_dist:
            min_dist = dist
            closest = item
    return closest

def run_agent():
    print("Starting Auto Agent...")
    
    # Check if we need to restart or ensure we are playing
    status = invoke("/api/status")
    if status and status.get("success"):
        data = status["data"]
        if not data["isPlaying"] or data["sceneName"] != "Gameplay":
            print("Not in Gameplay. Attempting to start/load...")
            # Ideally manual intervention or load command, but let's assume we are in game as per prompt.
    
    while True:
        try:
            # 1. Get State
            player_pos_resp = invoke("/api/player/position")
            task_resp = invoke("/api/game/task")
            
            if not player_pos_resp or not task_resp:
                print("Failed to get game state. Retrying...")
                time.sleep(1)
                continue
                
            player_pos = player_pos_resp["data"]["position"]
            task_data = task_resp["data"]
            
            if task_data.get("isCompleted", False):
                print("Level Complete!")
                break

            keys = task_data.get("keysPositions", [])
            doors = task_data.get("doorsPositions", [])
            exit_pos = task_data.get("targetPosition")
            key_count = task_data.get("keysObtained", 0)

            # Determine Target
            target = None
            action = "move"
            target_type = "none"

            # Logic:
            # 1. If keys exist -> Go to nearest key
            # 2. If doors exist -> Go to nearest door (assuming we picked up key, or we just go there and try)
            # 3. Else -> Go to exit
            
            # Refined Logic:
            # If we see a key, we probably need it.
            if keys:
                target = get_closest(player_pos, keys)
                target_type = "key"
            elif doors:
                target = get_closest(player_pos, doors)
                target_type = "door"
            else:
                target = exit_pos
                target_type = "exit"

            if not target:
                print("No target found? Moving randomly to explore...")
                # Simple blind exploration if stuck
                invoke("/api/player/move", "POST", {"x": 1, "y": 0})
                time.sleep(0.5)
                continue

            # Calculate Distance
            dist = get_distance(player_pos, target)
            print(f"Goal: {target_type} | Dist: {dist:.2f} | Pos: ({player_pos['x']:.1f}, {player_pos['y']:.1f})")

            # Action Logic
            if target_type in ["key", "door"] and dist < 0.8: # Close enough to interact
                print(f"Interacting with {target_type}...")
                invoke("/api/player/interact", "POST", {})
                time.sleep(0.5) # Wait for interaction
                # Stop moving briefly
                invoke("/api/player/move", "POST", {"x": 0, "y": 0})
            else:
                # Move towards target
                dx = target['x'] - player_pos['x']
                dy = target['y'] - player_pos['y']
                
                # Normalize
                length = math.sqrt(dx*dx + dy*dy)
                if length > 0:
                    dx /= length
                    dy /= length
                
                invoke("/api/player/move", "POST", {"x": dx, "y": dy})
            
            time.sleep(0.1)

        except KeyboardInterrupt:
            break
        except Exception as e:
            print(f"Loop error: {e}")
            time.sleep(1)

if __name__ == "__main__":
    run_agent()
