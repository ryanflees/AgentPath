import requests
import time
import os
import platform
import threading
import msvcrt

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
                resp = self.session.post(url, json=body, timeout=0.5)
            else:
                resp = self.session.get(url, timeout=0.5)
            if resp.status_code == 200:
                return resp.json()
        except:
            pass
        return None

    def update_loop(self):
        while self.is_running:
            # 聚合请求所有 Service
            status = self.invoke("/api/status")
            pos = self.invoke("/api/player/position")
            task = self.invoke("/api/game/task") # 包含完成状态
            nearby = self.invoke("/api/waypoints/nearby")

            with self.lock:
                self.game_data['status'] = status
                self.game_data['pos'] = pos
                self.game_data['task'] = task
                self.game_data['nearby'] = nearby
            time.sleep(0.4)

    def draw_ui(self):
        os.system('cls' if platform.system() == 'Windows' else 'clear')
        
        with self.lock:
            task_resp = self.game_data.get('task')
            is_done = False
            if task_resp and task_resp.get("success"):
                is_done = task_resp["data"].get("isCompleted", False)

            # --- 顶部横幅 ---
            if is_done:
                print("*" * 60)
                print(" " * 18 + "!!! LEVEL COMPLETE !!!") # 对应 UILevelComplete 逻辑
                print("*" * 60)
            else:
                print("=" * 60)
                print(f"  OPENCLAW MISSION MONITOR - {time.strftime('%H:%M:%S')}")
                print("=" * 60)

            # --- 核心数据 ---
            s_resp = self.game_data.get('status')
            if s_resp and s_resp.get("success"):
                s = s_resp["data"]
                print(f"[SYSTEM ]: Scene: {s['sceneName']} | Player: {'OK' if s['playerExists'] else 'LOST'}")

            p_resp = self.game_data.get('pos')
            if p_resp and p_resp.get("success"):
                p = p_resp["data"]["position"]
                print(f"[PLAYER ]: X: {p['x']:>6.2f} | Y: {p['y']:>6.2f}")

            if task_resp and task_resp.get("success"):
                t = task_resp["data"]
                tp = t.get("targetPosition")
                print(f"[MISSION]: {t['taskDescription']}")
                if tp:
                    # 显示目标坐标 (X, Y)，符合 2D 习惯
                    print(f"[TARGET ]: Pos: (X:{tp['x']:>6.2f}, Y:{tp['y']:>6.2f})")
                print(f"[GOAL   ]: Distance to Exit: {t['distanceToTarget']:.2f}m")

            n_resp = self.game_data.get('nearby')
            if n_resp and n_resp.get("success"):
                wps = n_resp["data"]["waypoints"]
                wp_str = " | ".join([f"#{w['id']}" for w in wps[:3]])
                print(f"[WAYPTS ]: Nearest IDs: {wp_str}")

        print("-" * 60)
        print(" CONTROLS: WASD (Move) | SPACE (Stop) | Q (Quit)")
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
                    if key == 'w': self.invoke("/api/player/move", "POST", {"x": 0, "y": 1})
                    elif key == 's': self.invoke("/api/player/move", "POST", {"x": 0, "y": -1})
                    elif key == 'a': self.invoke("/api/player/move", "POST", {"x": -1, "y": 0})
                    elif key == 'd': self.invoke("/api/player/move", "POST", {"x": 1, "y": 0})
                    elif key == ' ': self.invoke("/api/player/move", "POST", {"x": 0, "y": 0})
                    elif key == 'q': break
                time.sleep(0.1)
        finally:
            self.is_running = False

if __name__ == "__main__":
    OpenClawTerminal().run()