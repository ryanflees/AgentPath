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

    def a_star(self, start_id, end_id, blocked_ids=[]):
        open_set = []
        heapq.heappush(open_set, (0, start_id))
        came_from = {}
        
        g_score = {node: float('inf') for node in self.graph}
        g_score[start_id] = 0
        
        f_score = {node: float('inf') for node in self.graph}
        start_pos = self.waypoints[start_id]['position']
        end_pos = self.waypoints[end_id]['position']
        f_score[start_id] = get_dist(start_pos, end_pos)
        
        processed = set()

        while open_set:
            current = heapq.heappop(open_set)[1]
            
            if current == end_id:
                return self.reconstruct_path(came_from, current)
            
            processed.add(current)
            
            for neighbor in self.graph[current]:
                if neighbor in blocked_ids:
                    continue
                    
                d = get_dist(self.waypoints[current]['position'], self.waypoints[neighbor]['position'])
                tentative_g = g_score[current] + d
                
                if tentative_g < g_score[neighbor]:
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g
                    f_score[neighbor] = tentative_g + get_dist(self.waypoints[neighbor]['position'], end_pos)
                    
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
    
    # Restart to ensure fresh state
    print("Restarting level...")
    invoke("/api/player/restart", "POST")
    time.sleep(2.0)
    
    current_path = []
    path_index = 0
    
    # Identify waypoints that are "doors" or blocked initially if needed
    # But we treat door as a target first.
    
    last_pos = None
    stuck_frames = 0
    
    while True:
        try:
            # 1. State
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
            key_count = t_data.get("keysObtained", 0)

            # Stuck detection
            if last_pos:
                moved_dist = get_dist(p_pos, last_pos)
                if moved_dist < 0.01:
                    stuck_frames += 1
                else:
                    stuck_frames = 0
            last_pos = p_pos

            # 2. Strategy
            target_pos = None
            target_type = None
            
            if keys:
                target_pos = keys[0]
                target_type = "key"
            elif doors:
                 target_pos = doors[0]
                 target_type = "door"
            else:
                target_pos = exit_pos
                target_type = "exit"
            
            # Distance check
            dist_to_target = get_dist(p_pos, target_pos)
            # print(f"Goal: {target_type} | Dist: {dist_to_target:.2f} | Keys: {key_count} | Stuck: {stuck_frames}")

            # INTERACTION
            if target_type in ["key", "door"]:
                if dist_to_target < 0.8:
                    print(f"Interacting with {target_type}...")
                    invoke("/api/player/interact", "POST", {})
                    invoke("/api/player/move", "POST", {"x": 0, "y": 0})
                    time.sleep(1.0)
                    current_path = []
                    # Force a small move away/random to unstuck if needed?
                    continue 
                elif dist_to_target < 2.0 and stuck_frames > 5:
                     # We are close but stuck, try direct move aggressively or random wiggle
                     print("Stuck near target, wiggling...")
                     invoke("/api/player/move", "POST", {"x": 0.5, "y": 0.5}) # Wiggle
                     time.sleep(0.2)
                     current_path = [] # force replan
                     continue

            # MOVEMENT
            # If we are stuck, force replan
            if stuck_frames > 10:
                print("Stuck detected! Replanning...")
                current_path = []
                stuck_frames = 0
                # Try a random move to break loose
                invoke("/api/player/move", "POST", {"x": -1, "y": 0})
                time.sleep(0.2)
            
            if not current_path or path_index >= len(current_path):
                print(f"Planning path to {target_type}...")
                start_wp = pf.get_closest_waypoint(p_pos)
                
                # If target is a door/key, we might need to go to a NEIGHBOR of the target waypoint
                # because the item itself might be on a blocking tile (like a door).
                # Especially for doors.
                real_target_wp = pf.get_closest_waypoint(target_pos)
                
                if target_type == "door":
                    # Find a neighbor of the door waypoint that is reachable
                    # We assume we are not AT the door yet.
                    door_wp = pf.waypoints[real_target_wp]
                    best_neighbor = None
                    min_dist_to_player = float('inf')
                    
                    for nid in door_wp['connectedIds']:
                        # Pick neighbor closest to player? Or just any?
                        # Closest to player makes sense to avoid walking through the door.
                        n_pos = pf.waypoints[nid]['position']
                        d = get_dist(p_pos, n_pos)
                        if d < min_dist_to_player:
                            min_dist_to_player = d
                            best_neighbor = nid
                    
                    if best_neighbor is not None:
                        print(f"Adjusting door target from {real_target_wp} to neighbor {best_neighbor}")
                        end_wp = best_neighbor
                    else:
                        end_wp = real_target_wp
                else:
                    end_wp = real_target_wp
                
                print(f"A* from {start_wp} to {end_wp}")
                path_ids = pf.a_star(start_wp, end_wp)
                
                if path_ids:
                    print(f"Path found: {path_ids}")
                    current_path = [pf.waypoints[wid]['position'] for wid in path_ids]
                    path_index = 0
                    if len(current_path) > 1:
                        # Find closest point in path to resume from
                        closest_idx = 0
                        min_d = float('inf')
                        for i, node in enumerate(current_path):
                            d = get_dist(p_pos, node)
                            if d < min_d:
                                min_d = d
                                closest_idx = i
                        
                        path_index = closest_idx
                        # If extremely close to closest_idx, move to next
                        if min_d < 0.5:
                            path_index += 1
                else:
                    print("No path found via A*! Using direct approach.")
                    dx = target_pos['x'] - p_pos['x']
                    dy = target_pos['y'] - p_pos['y']
                    mag = math.sqrt(dx*dx + dy*dy)
                    if mag > 0:
                        invoke("/api/player/move", "POST", {"x": dx/mag, "y": dy/mag})
                    time.sleep(0.5)
                    continue

            # Execute Path
            if path_index < len(current_path):
                next_node = current_path[path_index]
                d = get_dist(p_pos, next_node)
                
                if d < 0.8: 
                    path_index += 1
                    if path_index >= len(current_path):
                         # Final approach
                        dx = target_pos['x'] - p_pos['x']
                        dy = target_pos['y'] - p_pos['y']
                        mag = math.sqrt(dx*dx + dy*dy)
                        if mag > 0:
                            invoke("/api/player/move", "POST", {"x": dx/mag, "y": dy/mag})
                        continue
                    next_node = current_path[path_index]
                
                dx = next_node['x'] - p_pos['x']
                dy = next_node['y'] - p_pos['y']
                mag = math.sqrt(dx*dx + dy*dy)
                
                if mag > 0:
                    invoke("/api/player/move", "POST", {"x": dx/mag, "y": dy/mag})
            
            time.sleep(0.1)

        except KeyboardInterrupt:
            break
        except Exception as e:
            print(f"Error: {e}")
            time.sleep(1)

if __name__ == "__main__":
    run_agent()
