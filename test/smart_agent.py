import requests
import time
import math
import heapq
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
    except:
        pass
    return None

def get_dist(p1, p2):
    return math.sqrt((p1['x'] - p2['x'])**2 + (p1['y'] - p2['y'])**2)

class Pathfinder:
    def __init__(self):
        self.waypoints = {}
        self.graph = {}
        
    def load_waypoints(self):
        resp = invoke("/api/waypoints/all")
        if resp and resp.get("success"):
            wps = resp["data"]["waypoints"]
            for wp in wps:
                self.waypoints[wp['id']] = wp
                self.graph[wp['id']] = wp['connectedIds']
            print(f"Loaded {len(self.waypoints)} waypoints.")
            return True
        return False
        
    def get_closest_waypoint(self, pos):
        closest = None
        min_dist = float('inf')
        for wid, wp in self.waypoints.items():
            d = get_dist(pos, wp['position'])
            if d < min_dist:
                min_dist = d
                closest = wid
        return closest

    def a_star(self, start_id, end_id, exclude_pos=None):
        # exclude_pos is a position (like a door) to avoid? 
        # Actually waypoints already define walkability. 
        # But if a door is at a waypoint, that waypoint might be blocked?
        # The prompt says: "a waypoint occupied by door is inaccessible."
        # So we should check if any waypoint is close to a door and exclude it if we don't have a key.
        
        open_set = []
        heapq.heappush(open_set, (0, start_id))
        came_from = {}
        g_score = {node: float('inf') for node in self.graph}
        g_score[start_id] = 0
        f_score = {node: float('inf') for node in self.graph}
        f_score[start_id] = get_dist(self.waypoints[start_id]['position'], self.waypoints[end_id]['position'])
        
        while open_set:
            current = heapq.heappop(open_set)[1]
            
            if current == end_id:
                return self.reconstruct_path(came_from, current)
            
            for neighbor in self.graph[current]:
                # Distance between nodes
                d = get_dist(self.waypoints[current]['position'], self.waypoints[neighbor]['position'])
                tentative_g = g_score[current] + d
                
                if tentative_g < g_score[neighbor]:
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g
                    f_score[neighbor] = tentative_g + get_dist(self.waypoints[neighbor]['position'], self.waypoints[end_id]['position'])
                    if neighbor not in [i[1] for i in open_set]:
                        heapq.heappush(open_set, (f_score[neighbor], neighbor))
                        
        return None

    def reconstruct_path(self, came_from, current):
        total_path = [current]
        while current in came_from:
            current = came_from[current]
            total_path.append(current)
        return total_path[::-1]

def run_agent():
    pf = Pathfinder()
    if not pf.load_waypoints():
        print("Failed to load waypoints.")
        return

    print("Agent Started with A*.")
    
    current_path = []
    path_index = 0
    
    while True:
        try:
            # 1. State
            status = invoke("/api/status")
            task = invoke("/api/game/task")
            pos_resp = invoke("/api/player/position")
            
            if not (task and pos_resp and task.get("success")):
                time.sleep(0.5)
                continue
                
            t_data = task["data"]
            if t_data.get("isCompleted"):
                print("Level Complete!")
                break
                
            p_pos = pos_resp["data"]["position"]
            keys = t_data.get("keysPositions", [])
            doors = t_data.get("doorsPositions", [])
            exit_pos = t_data.get("targetPosition")
            key_count = t_data.get("keysObtained", 0) # field name from curl output

            # 2. Check Doors (Blocking)
            # The prompt says: "a waypoint occupied by door is inaccessible."
            # We should remove waypoints near doors from the graph if we don't have a key?
            # Or just plan path to door if we have key.
            # Actually, if we use A*, we plan TO the target. 
            
            # Goal Selection
            target_pos = None
            target_type = None
            
            if keys:
                target_pos = keys[0] # Just pick first
                target_type = "key"
            elif doors:
                 # Go to interact range of door
                 target_pos = doors[0]
                 target_type = "door"
            else:
                target_pos = exit_pos
                target_type = "exit"
            
            # 3. Pathfinding
            # Find nearest WP to player and target
            start_wp = pf.get_closest_waypoint(p_pos)
            end_wp = pf.get_closest_waypoint(target_pos)
            
            dist_to_target = get_dist(p_pos, target_pos)
            
            print(f"Goal: {target_type} | Dist: {dist_to_target:.2f} | Keys: {key_count}")

            # Interaction Logic
            if target_type in ["key", "door"] and dist_to_target < 0.8:
                print("Interacting...")
                invoke("/api/player/interact", "POST", {})
                time.sleep(1.0) # Wait for effect
                current_path = [] # Reset path
                continue
            
            # Move Logic
            # If we are close enough to the target (and it's not a waypoint based move but final approach)
            if dist_to_target < 0.5 and target_type == "exit":
                # Just move directly
                dx = target_pos['x'] - p_pos['x']
                dy = target_pos['y'] - p_pos['y']
                invoke("/api/player/move", "POST", {"x": dx*2, "y": dy*2})
                time.sleep(0.1)
                continue

            # Path Execution
            if not current_path or path_index >= len(current_path):
                # Replan
                print("Planning path...")
                path_ids = pf.a_star(start_wp, end_wp)
                if path_ids:
                    current_path = [pf.waypoints[wid]['position'] for wid in path_ids]
                    path_index = 0
                    # If start WP is behind us or we are already there, skip it
                    if len(current_path) > 1 and get_dist(p_pos, current_path[0]) < 0.5:
                        path_index = 1
                else:
                    print("No path found!")
                    # Try blindly moving to target if close
                    dx = target_pos['x'] - p_pos['x']
                    dy = target_pos['y'] - p_pos['y']
                    invoke("/api/player/move", "POST", {"x": dx, "y": dy})
                    time.sleep(0.5)
                    continue

            # Follow Path
            if path_index < len(current_path):
                next_node = current_path[path_index]
                d = get_dist(p_pos, next_node)
                
                if d < 0.4:
                    # Check if stuck
                    if len(current_path) > 1 and path_index > 0:
                        # If we aren't moving closer to next node, we might be stuck
                        pass
                        
                    path_index += 1
                    if path_index >= len(current_path):
                        continue
                    next_node = current_path[path_index]
                
                # Move towards next_node
                dx = next_node['x'] - p_pos['x']
                dy = next_node['y'] - p_pos['y']
                mag = math.sqrt(dx*dx + dy*dy)
                
                # Force stronger movement
                if mag > 0:
                    invoke("/api/player/move", "POST", {"x": dx/mag, "y": dy/mag})
            else:
                 # Reached end of path, but not target?
                 # Should trigger re-plan
                 current_path = []
            
            time.sleep(0.1)

        except KeyboardInterrupt:
            break
        except Exception as e:
            print(f"Error: {e}")
            time.sleep(1)

if __name__ == "__main__":
    run_agent()
