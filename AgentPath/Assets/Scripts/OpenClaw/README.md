# OpenClaw API Service

A clean, flexible, and extensible HTTP API service for Unity FPS games that allows external agents to query game state and send commands at runtime.

## Features

- **Clean Architecture**: Modular design with clear separation of concerns
- **Extensible Routing**: Dictionary-based routing system for easy endpoint addition
- **Thread-Safe**: Uses UnityMainThreadDispatcher for safe Unity API access from background threads
- **Full API Coverage**: Player state queries, movement commands, and waypoint navigation
- **Type-Safe**: Strongly-typed request/response models with Newtonsoft.Json
- **Easy to Extend**: Add new endpoints without modifying core server code

## Installation

1. All files are located in `Assets/Scripts/OpenClaw/`
2. Ensure you have Newtonsoft.Json package installed in Unity
3. Add the `OpenClawAPIServer` component to a GameObject in your scene
4. Configure the port (default: 8091) in the Inspector

## Quick Start

### 1. Add Server to Scene

1. Create an empty GameObject in your scene (e.g., "OpenClawAPIServer")
2. Add the `OpenClawAPIServer` component to it
3. Configure settings in the Inspector:
   - **Port**: 8091 (default)
   - **Auto Start**: Enable to start server automatically on play
   - **Log Requests**: Enable to log all incoming HTTP requests

### 2. Start the Server

The server will start automatically if "Auto Start" is enabled. You can also control it via the Inspector buttons in Play Mode.

### 3. Test the API

Run one of the provided test scripts:

**Python:**
```bash
python test_openclaw_api.py
```

**PowerShell:**
```powershell
.\Test-OpenClawAPI.ps1
```

## API Endpoints

### System Endpoints

- `GET /api/health` - Server health check
- `GET /api/status` - Game state overview

### Player State Endpoints (GET)[test_waypoint_api.py](../../../../backup/test_waypoint_api.py)

- `GET /api/player/position` - Get player position and rotation
- `GET /api/player/state` - Get player state (grounded, crouching, sprinting, etc.)

### Player Command Endpoints (POST)

- `POST /api/player/move` - Send movement input
  ```json
  {"x": 0.0, "y": 0.0, "z": 1.0}
  ```

- `POST /api/player/look` - Control camera rotation
  ```json
  {"yaw": 45.0, "pitch": 0.0}
  ```

- `POST /api/player/jump` - Execute jump
  ```json
  {}
  ```

- `POST /api/player/crouch` - Toggle crouch
  ```json
  {"crouch": true}
  ```

- `POST /api/player/sprint` - Toggle sprint
  ```json
  {"sprint": true}
  ```

### Waypoint Endpoints (GET)

- `GET /api/waypoints/all` - Get all waypoints with connections
- `GET /api/waypoints/nearby?maxDistance=50&maxCount=10` - Get nearby waypoints
- `GET /api/waypoints/nearest?x=0&y=0&z=0` - Get nearest waypoint to position
- `GET /api/waypoints/path?from=1&to=5` - Get path between waypoints (BFS)
- `GET /api/waypoints/in-view?fov=60` - Get waypoints in player's field of view

## Response Format

All endpoints return consistent JSON format:

**Success:**
```json
{
  "success": true,
  "data": { /* endpoint-specific data */ },
  "error": null,
  "timestamp": "2026-02-04T12:34:56Z"
}
```

**Error:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "PLAYER_NOT_FOUND",
    "message": "Player controller not found in scene",
    "details": null
  },
  "timestamp": "2026-02-04T12:34:56Z"
}
```

## Extending the API

### Adding a New Endpoint

1. **Add response model** in `Core/APIModels.cs`:
```csharp
[Serializable]
public class WeaponInfoResponse
{
    public string weaponName;
    public int ammo;
}
```

2. **Add handler method** in `Core/OpenClawAPIServer.cs`:
```csharp
private string HandleGetWeaponInfo(HttpListenerRequest request)
{
    return ExecuteOnMainThread(() => {
        // Your logic here
        var weaponInfo = new WeaponInfoResponse
        {
            weaponName = "Rifle",
            ammo = 30
        };
        return ResponseBuilder.CreateSuccessResponse(weaponInfo);
    });
}
```

3. **Register endpoint** in `InitializeRoutes()` method:
```csharp
m_GetRoutes["/api/player/weapon"] = HandleGetWeaponInfo;
```

That's it! Your new endpoint is now available at `http://localhost:8091/api/player/weapon`

### Adding a New Service

Create a new service class in `Services/` folder:

```csharp
public class MyCustomService
{
    public string GetCustomData()
    {
        // Your logic here
        return ResponseBuilder.CreateSuccessResponse(data);
    }
}
```

Initialize it in `OpenClawAPIServer.InitializeServices()`:
```csharp
private MyCustomService m_CustomService;

private void InitializeServices()
{
    // ... existing services
    m_CustomService = new MyCustomService();
}
```

## Architecture

```
OpenClaw/
├── Core/
│   ├── OpenClawAPIServer.cs         # HTTP server with routing
│   ├── UnityMainThreadDispatcher.cs # Thread-safe dispatcher
│   └── APIModels.cs                 # Request/Response DTOs
├── Services/
│   ├── PlayerStateService.cs        # Query player state
│   ├── PlayerCommandService.cs      # Execute player actions
│   └── WaypointQueryService.cs      # Waypoint queries & pathfinding
├── Utilities/
│   ├── ResponseBuilder.cs           # Consistent response formatting
│   └── RequestValidator.cs          # Input validation
└── Editor/
    └── OpenClawAPIServerEditor.cs   # Inspector UI
```

## Key Design Patterns

- **Singleton Pattern**: Server and dispatcher instances
- **Dictionary-Based Routing**: Extensible endpoint registration
- **Thread-Safe Dispatcher**: Safe Unity API access from background threads
- **Service Layer**: Separation of concerns (state queries vs commands)
- **Consistent Response Format**: All endpoints use same response structure

## Performance Considerations

- **Caching**: Player and waypoint lookups are cached (0.5-1s duration)
- **Thread Pool**: HTTP requests are processed on thread pool to avoid blocking
- **ManualResetEvent**: Efficient synchronization instead of polling
- **BFS Pathfinding**: O(V+E) complexity for waypoint path finding

## Troubleshooting

### Server won't start
- Check if port 8091 is already in use
- Ensure UnityMainThreadDispatcher is in the scene
- Check Unity console for error messages

### Commands not working
- Verify player controller exists in scene
- Check that game is in Play Mode
- Enable "Log Requests" to see incoming requests

### Waypoints not found
- Ensure WaypointContainer exists in scene
- Check that waypoints have valid IDs
- Verify waypoint connections are set up

## Example Usage (Python)

```python
import requests

BASE_URL = "http://localhost:8091"

# Get player position
response = requests.get(f"{BASE_URL}/api/player/position")
data = response.json()
print(f"Player position: {data['data']['position']}")

# Move forward
requests.post(f"{BASE_URL}/api/player/move", json={
    "x": 0.0,
    "y": 0.0,
    "z": 1.0
})

# Get nearby waypoints
response = requests.get(f"{BASE_URL}/api/waypoints/nearby?maxDistance=50&maxCount=5")
waypoints = response.json()['data']['waypoints']
print(f"Found {len(waypoints)} nearby waypoints")
```

## Credits

Built for the AIShooter Unity FPS project. Based on the original OpenClaw implementation with significant improvements in architecture, extensibility, and maintainability.
