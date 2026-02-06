using System;
using UnityEngine;

namespace CR.OpenClaw
{
    #region Base Response Models

    [Serializable, AgentRes]
    public class APIResponse
    {
        public bool success;
        public object data;
        public APIError error;
        public string timestamp;

        public static APIResponse Success(object data = null)
        {
            return new APIResponse
            {
                success = true,
                data = data,
                error = null,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        public static APIResponse Error(string code, string message, string details = null)
        {
            return new APIResponse
            {
                success = false,
                data = null,
                error = new APIError { code = code, message = message, details = details },
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }
    }

    [Serializable, AgentRes]
    public class APIError
    {
        public string code;
        public string message;
        public string details;
    }

    #endregion

    #region Common Data Types

    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    #endregion

    #region System Endpoints 
    [Serializable, AgentRes]
    public class HealthResponse
    {
        public string status;
        public string serverVersion;
        public int port;
        public string uptime;
    }

    [Serializable, AgentRes]
    public class GameStatusResponse
    {
        public bool isPlaying;
        public string sceneName;
        public float gameTime;
        public bool playerExists;
        public string worldAxisUp = "y";
        public string worldAxisForward = "z";
        public string worldAxisRight = "x";
    }

    #endregion
}
