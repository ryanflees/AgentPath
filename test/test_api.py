import requests
import time
import os
import platform
import threading
import msvcrt
import json

BASE_URL = "http://localhost:8091"

class OpenClawTerminal:
    def __init__(self):
        self.session = requests.Session()
        self.game_data = {}
        self.is_running = True
        self.lock = threading.Lock()

    def invoke(self, endpoint, method="GET", body=None):
        try:
            url = f"{BASE_URL}{endpoint}"
            if method == "POST":
                resp = self.session.post(url, json=body, timeout=0.8)
            else:
                resp = self.session.get(url, timeout=0.8)
            if resp.status_code == 200:
                return resp.json()
        except:
            pass
        return None

    def update_loop(self):
        while self.is_running:
            # Syncing with new endpoints and data structures
            status = self.invoke("/api/status")           # Returns scene and progress
            pos = self.invoke("/api/player/position")     # Returns player transform
            task = self.invoke("/api/game/task")          # Returns keys, doors, and objectives
            nearby = self.invoke("/api/waypoints/nearby") # Returns local navigation nodes

            with self.lock:
                self.game_data['status'] = status
                self.game_data['pos'] = pos
                self.game_data['task'] = task
                self.game_data['nearby'] = nearby
            time.sleep(0.4)

    def draw_ui(self):
        os.system('cls' if platform.system() == 'Windows' else 'clear')
        
        with self.lock:
            # --- MISSION STATUS ---
            task_resp = self.game_data.get('task')
            is_done = False
            if task_resp and task_resp.get("success"):
                is_done = task_resp["data"].get("isCompleted", False)

            if is_done:
                print("*" * 60)
                print(" " * 18 + "!!! LEVEL COMPLETE !!!")
                print("*" * 60)
            else:
                print("=" * 60)
                print(f"  OPENCLAW MISSION MONITOR - {time.strftime('%H:%M:%S')}")
                print("=" * 60)

            # --- SYSTEM & PROGRESS ---
            s_resp = self.game_data.get('status')
            if s_resp and s_resp.get("success"):
                s = s_resp["data"]
                print(f"[LEVEL  ]: {s['currentLevel']} (Scene: {s['sceneName']})")
                print(f"[PROGRESS]: Ch1: {s['chapter1Progress']} | Ch2: {s['chapter2Progress']}")

            # --- PLAYER ---
            p_resp = self.game_data.get('pos')
            if p_resp and p_resp.get("success"):
                p = p_resp["data"]["position"]
                print(f"[PLAYER ]: X: {p['x']:>6.2f} | Y: {p['y']:>6.2f}")

            # --- DYNAMIC OBJECTS ---
            if task_resp and task_resp.get("success"):
                t = task_resp["data"]
                print(f"[MISSION]: {t['taskDescription']}")
                
                keys = t.get("keysPositions", [])
                doors = t.get("doorsPositions", [])
                
                if keys:
                    key_list = ", ".join([f"(X:{k['x']:.1f}, Y:{k['y']:.1f})" for k in keys])
                    print(f"[KEYS   ]: Found {len(keys)} @ {key_list}")
                
                if doors:
                    door_list = ", ".join([f"(X:{d['x']:.1f}, Y:{d['y']:.1f})" for d in doors])
                    print(f"[DOORS  ]: Found {len(doors)} @ {door_list}")

            # --- NAVIGATION ---
            n_resp = self.game_data.get('nearby')
            if n_resp and n_resp.get("success"):
                wps = n_resp["data"]["waypoints"]
                wp_str = " | ".join([f"#{w['id']}" for w in wps[:3]])
                print(f"[WAYPTS ]: Nearest IDs: {wp_str}")

        print("-" * 60)
        print(" MOVEMENT: WASD (Move) | SPACE (Stop)")
        print(" COMMANDS: R (Restart) | M (Main Menu) | L (Load 1-1) | Q (Quit)")
        print("=" * 60)

    def run(self):
        t = threading.Thread(target=self.update_loop)
        t.daemon = True
        t.start()
        try:
            while self.is_running:
                self.draw_ui()
                if msvcrt.kbhit():
                    key = msvcrt.getch().decode('utf-8').lower()
                    # Locomotion
                    if key == 'w': self.invoke("/api/player/move", "POST", {"x": 0, "y": 1})
                    elif key == 's': self.invoke("/api/player/move", "POST", {"x": 0, "y": -1})
                    elif key == 'a': self.invoke("/api/player/move", "POST", {"x": -1, "y": 0})
                    elif key == 'd': self.invoke("/api/player/move", "POST", {"x": 1, "y": 0})
                    elif key == ' ': self.invoke("/api/player/move", "POST", {"x": 0, "y": 0})
                    
                    # Management Endpoints
                    elif key == 'r': self.invoke("/api/player/restart", "POST")
                    elif key == 'm': self.invoke("/api/player/main", "POST")
                    elif key == 'l': self.invoke("/api/player/level", "POST", {"chapter": 1, "level": 1})
                    
                    elif key == 'q': break
                time.sleep(0.1)
        finally:
            self.is_running = False

if __name__ == "__main__":
    OpenClawTerminal().run()