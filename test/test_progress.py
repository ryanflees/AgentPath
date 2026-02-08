import requests
import json
import os
import platform

BASE_URL = "http://localhost:8091"

class OpenClawManualTester:
    def __init__(self):
        self.session = requests.Session()

    def clear_screen(self):
        os.system('cls' if platform.system() == 'Windows' else 'clear')

    def invoke(self, endpoint, method="GET", body=None):
        url = f"{BASE_URL}{endpoint}"
        try:
            if method == "POST":
                r = self.session.post(url, json=body, timeout=2.0)
            else:
                r = self.session.get(url, timeout=2.0)
            
            print(f"\n[Status Code]: {r.status_code}")
            return r.json()
        except Exception as e:
            return {"error": str(e)}

    def run(self):
        while True:
            print("\n" + "="*40)
            print("   OPENCLAW API MANUAL TESTER")
            print("="*40)
            print(" 1. Get Game Status (Progress/Scene)")
            print(" 2. Get Task Info (Keys/Doors)")
            print(" 3. Restart Current Level")
            print(" 4. Load Specific Level")
            print(" 5. Back to Main Menu")
            print(" 6. Move Player (0.5, 0.5)")
            print(" 0. Exit")
            print("-" * 40)
            
            choice = input("Enter choice (0-6): ")

            if choice == '1':
                res = self.invoke("/api/status")
                print(json.dumps(res, indent=4))
            
            elif choice == '2':
                res = self.invoke("/api/game/task")
                print(json.dumps(res, indent=4))
            
            elif choice == '3':
                res = self.invoke("/api/player/restart", "POST")
                print(json.dumps(res, indent=4))
            
            elif choice == '4':
                try:
                    ch = int(input("Enter Chapter: "))
                    lv = int(input("Enter Level: "))
                    res = self.invoke("/api/player/level", "POST", {"chapter": ch, "level": lv})
                    print(json.dumps(res, indent=4))
                except ValueError:
                    print("Invalid input. Please enter numbers.")

            elif choice == '5':
                res = self.invoke("/api/player/main", "POST")
                print(json.dumps(res, indent=4))

            elif choice == '6':
                res = self.invoke("/api/player/move", "POST", {"x": 0.5, "y": 0.5})
                print(json.dumps(res, indent=4))

            elif choice == '0':
                print("Exiting...")
                break
            else:
                print("Unknown choice, try again.")

            input("\nPress Enter to continue...")
            self.clear_screen()

if __name__ == "__main__":
    OpenClawManualTester().run()