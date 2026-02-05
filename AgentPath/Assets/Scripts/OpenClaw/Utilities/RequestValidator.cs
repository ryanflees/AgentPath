using UnityEngine;

namespace CR.OpenClaw
{
    /// <summary>
    /// Utility class for validating API requests
    /// </summary>
    public static class RequestValidator
    {
        /// <summary>
        // /// Validate move command request
        // /// </summary>
        // public static bool ValidateMoveCommand(MoveCommandRequest request, out string errorMessage)
        // {
        //     if (request == null)
        //     {
        //         errorMessage = "Move command request is null";
        //         return false;
        //     }
        //
        //     // Check for NaN or Infinity
        //     if (float.IsNaN(request.x) || float.IsInfinity(request.x) ||
        //         float.IsNaN(request.y) || float.IsInfinity(request.y))
        //     {
        //         errorMessage = "Move command contains invalid values (NaN or Infinity)";
        //         return false;
        //     }
        //
        //     errorMessage = null;
        //     return true;
        // }
        //
        // /// <summary>
        // /// Validate look command request
        // /// </summary>
        // public static bool ValidateLookCommand(LookCommandRequest request, out string errorMessage)
        // {
        //     if (request == null)
        //     {
        //         errorMessage = "Look command request is null";
        //         return false;
        //     }
        //
        //     // Check for NaN or Infinity
        //     if (float.IsNaN(request.yaw) || float.IsInfinity(request.yaw) ||
        //         float.IsNaN(request.pitch) || float.IsInfinity(request.pitch))
        //     {
        //         errorMessage = "Look command contains invalid values (NaN or Infinity)";
        //         return false;
        //     }
        //
        //     errorMessage = null;
        //     return true;
        // }
        //
        // /// <summary>
        // /// Validate waypoint ID
        // /// </summary>
        // public static bool ValidateWaypointId(int id, out string errorMessage)
        // {
        //     if (id < 0)
        //     {
        //         errorMessage = $"Invalid waypoint ID: {id} (must be >= 0)";
        //         return false;
        //     }
        //
        //     errorMessage = null;
        //     return true;
        // }
        //
        // /// <summary>
        // /// Validate position vector
        // /// </summary>
        // public static bool ValidatePosition(Vector3 position, out string errorMessage)
        // {
        //     if (float.IsNaN(position.x) || float.IsInfinity(position.x) ||
        //         float.IsNaN(position.y) || float.IsInfinity(position.y) ||
        //         float.IsNaN(position.z) || float.IsInfinity(position.z))
        //     {
        //         errorMessage = "Position contains invalid values (NaN or Infinity)";
        //         return false;
        //     }
        //
        //     errorMessage = null;
        //     return true;
        // }
        //
        // /// <summary>
        // /// Validate numeric range
        // /// </summary>
        // public static bool ValidateRange(float value, float min, float max, string paramName, out string errorMessage)
        // {
        //     if (value < min || value > max)
        //     {
        //         errorMessage = $"{paramName} must be between {min} and {max} (got {value})";
        //         return false;
        //     }
        //
        //     errorMessage = null;
        //     return true;
        // }
    }
}
