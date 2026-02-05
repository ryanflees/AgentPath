using System;
using Newtonsoft.Json;
using UnityEngine;

namespace CR.OpenClaw
{
    /// <summary>
    /// Utility class for building consistent API responses
    /// </summary>
    public static class ResponseBuilder
    {
        /// <summary>
        /// Create a success response with data
        /// </summary>
        public static string CreateSuccessResponse(object data = null)
        {
            var response = APIResponse.Success(data);
            return SerializeResponse(response);
        }

        /// <summary>
        /// Create an error response
        /// </summary>
        public static string CreateErrorResponse(string code, string message, string details = null)
        {
            var response = APIResponse.Error(code, message, details);
            return SerializeResponse(response);
        }

        /// <summary>
        /// Create a standard error response for common scenarios
        /// </summary>
        public static string CreateStandardError(StandardError errorType, string additionalDetails = null)
        {
            switch (errorType)
            {
                case StandardError.PlayerNotFound:
                    return CreateErrorResponse("PLAYER_NOT_FOUND", "Player controller not found in scene", additionalDetails);

                case StandardError.WaypointSystemNotFound:
                    return CreateErrorResponse("WAYPOINT_SYSTEM_NOT_FOUND", "Waypoint container not found in scene", additionalDetails);

                case StandardError.InvalidRequest:
                    return CreateErrorResponse("INVALID_REQUEST", "Request body is invalid or malformed", additionalDetails);

                case StandardError.MissingParameter:
                    return CreateErrorResponse("MISSING_PARAMETER", "Required parameter is missing", additionalDetails);

                case StandardError.CommandFailed:
                    return CreateErrorResponse("COMMAND_FAILED", "Command execution failed", additionalDetails);

                case StandardError.InternalError:
                    return CreateErrorResponse("INTERNAL_ERROR", "An internal server error occurred", additionalDetails);

                case StandardError.NotInPlayMode:
                    return CreateErrorResponse("NOT_IN_PLAY_MODE", "Unity is not in play mode", additionalDetails);

                default:
                    return CreateErrorResponse("UNKNOWN_ERROR", "An unknown error occurred", additionalDetails);
            }
        }

        /// <summary>
        /// Serialize response object to JSON
        /// </summary>
        private static string SerializeResponse(APIResponse response)
        {
            try
            {
                return JsonConvert.SerializeObject(response, Formatting.None);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResponseBuilder] Failed to serialize response: {ex.Message}");
                // Fallback to simple error response
                return "{\"success\":false,\"data\":null,\"error\":{\"code\":\"SERIALIZATION_ERROR\",\"message\":\"Failed to serialize response\",\"details\":null},\"timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\"}";
            }
        }

        /// <summary>
        /// Deserialize request body from JSON
        /// </summary>
        public static T DeserializeRequest<T>(string json) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResponseBuilder] Failed to deserialize request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validate that request body is not null
        /// </summary>
        public static bool ValidateRequest<T>(T request, out string errorResponse) where T : class
        {
            if (request == null)
            {
                errorResponse = CreateStandardError(StandardError.InvalidRequest, "Request body cannot be null");
                return false;
            }

            errorResponse = null;
            return true;
        }
    }

    /// <summary>
    /// Standard error types for consistent error handling
    /// </summary>
    public enum StandardError
    {
        PlayerNotFound,
        WaypointSystemNotFound,
        InvalidRequest,
        MissingParameter,
        CommandFailed,
        InternalError,
        NotInPlayMode
    }
}
