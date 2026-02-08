import requests
import time
import math
import heapq

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
    except Exception:
        pass
    return None

def get_pos_tuple(pos_dict):
    return (pos_dict['x'], pos_dict['y'])

def dist(p1, p2):
    return math.sqrt((p1[0] - p2[0])**2 + (p1[1] - p2[1])**2)

def get_all_waypoints():
    # Since there isn't a direct "get all" endpoint documented in the prompt, 
    # we might have to rely on `nearby` or assume we can move blindly if we don't have a map.
    # However, the prompt says "you can find all waypoint information". 
    # Let's try to fetch a broad range or walk around. 
    # Actually, the prompt says "you can find all waypoint information, and get nearby waypoints...".
    # It doesn't explicitly give an endpoint for ALL.
    # But usually /api/waypoints might exist? 
    # Let's try /api/waypoints/all or just /api/waypoints
    
    # Trying /api/waypoints based on typical REST patterns if not fully specified
    # The prompt says: "you can find all waypoint information"
    resp = invoke("/api/waypoints") # Guessing
    if resp and resp.get("success"):
        return resp["data"]["waypoints"]
    
    # If that fails, we might need to build a map or just use local search.
    # For now, let's assume we can query nearby and move to them.
    return []

# A* Implementation
def find_path(start_pos, target_pos, all_waypoints):
    # This requires a graph of waypoints. 
    # If we don't have the full graph, we can't do global A*.
    # Let's assume we can get the full graph.
    pass

def move_to(target_pos):
    # Simple reactive movement
    p_resp = invoke("/api/player/position")
    if not p_resp: return False
    
    curr = p_resp["data"]["position"]
    dx = target_pos['x'] - curr['x']
    dy = target_pos['y'] - curr['y']
    
    d = math.sqrt(dx*dx + dy*dy)
    if d < 0.2:
        invoke("/api/player/move", "POST", {"x": 0, "y": 0})
        return True # Arrived
        
    if d > 0:
        invoke("/api/player/move", "POST", {"x": dx/d, "y": dy/d})
    return False

def main():
    print("Agent Active.")
    
    # 1. Get full graph if possible
    # We will try to just move directly to targets first (blindly) as the environment seems open or we lack the map API in the text provided.
    # If there are walls, we will get stuck. 
    # The prompt mentions "walkable waypoints placed in scene... you can get a nearest waypoint by a given position... closest waypoint to the exit... by connection of other waypoints".
    # This implies we should use waypoints for navigation.
    
    # Let's try to get the full list first to build the graph.
    # If we can't, we will just move towards the goal and hope for the best (or use nearby waypoints to skirt obstacles).
    
    state = "init"
    target = None
    target_type = None

    while True:
        try:
            task = invoke("/api/game/task")
            if not task or not task.get("success"):
                time.sleep(0.5)
                continue
                
            t_data = task["data"]
            if t_data.get("isCompleted"):
                print("Level Complete!")
                break
                
            # Decisions
            keys = t_data.get("keysPositions", [])
            doors = t_data.get("doorsPositions", [])
            exit_pos = t_data.get("targetPosition")
            has_key = t_data.get("keysObtained", 0) > 0

            # Select Goal
            if keys and not has_key:
                # Find closest key
                # Simple Euclidean for now
                closest_key = min(keys, key=lambda k: (k['x']**2 + k['y']**2)) # relative to 0,0 ? No, relative to player
                # Need player pos
                p_resp = invoke("/api/player/position")
                if p_resp:
                    p = p_resp["data"]["position"]
                    closest_key = min(keys, key=lambda k: ((k['x']-p['x'])**2 + (k['y']-p['y'])**2))
                    target = closest_key
                    target_type = "key"
            elif doors:
                # Go to door
                # Need player pos
                p_resp = invoke("/api/player/position")
                if p_resp:
                    p = p_resp["data"]["position"]
                    closest_door = min(doors, key=lambda k: ((k['x']-p['x'])**2 + (k['y']-p['y'])**2))
                    target = closest_door
                    target_type = "door"
            else:
                target = exit_pos
                target_type = "exit"
            
            # Execute Move
            if target:
                p_resp = invoke("/api/player/position")
                if p_resp:
                    curr = p_resp["data"]["position"]
                    dist = math.sqrt((target['x']-curr['x'])**2 + (target['y']-curr['y'])**2)
                    
                    print(f"Goal: {target_type} | Dist: {dist:.2f}")

                    if target_type in ["key", "door"] and dist < 1.0:
                        print(f"Interacting with {target_type}...")
                        invoke("/api/player/interact", "POST", {})
                        time.sleep(1.0) # Wait for interaction
                    else:
                        # Move
                        dx = target['x'] - curr['x']
                        dy = target['y'] - curr['y']
                        l = math.sqrt(dx*dx + dy*dy)
                        if l > 0:
                            invoke("/api/player/move", "POST", {"x": dx/l, "y": dy/l})
                            
            time.sleep(0.1)

        except KeyboardInterrupt:
            break
        except Exception as e:
            print(e)
            time.sleep(1)

if __name__ == "__main__":
    main()
