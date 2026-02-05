using System;
using UnityEngine;

namespace CR.OpenClaw
{
    #region Base Response Models (核心 - 所有服务使用)

    /// <summary>
    /// Standard API response wrapper
    /// 标准API响应包装器 - 所有端点使用此格式
    /// </summary>
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

    /// <summary>
    /// API error structure
    /// API错误结构
    /// </summary>
    [Serializable, AgentRes]
    public class APIError
    {
        public string code;
        public string message;
        public string details;
    }

    #endregion

    #region Common Data Types (通用数据类型 - 多个服务共享)

    /// <summary>
    /// Vector3 data transfer object
    /// Vector3数据传输对象 - 用于JSON序列化
    /// </summary>
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

    #region System Endpoints (系统端点 - 服务器自身状态)

    /// <summary>
    /// Health check response - 服务器健康检查
    /// </summary>
    [Serializable, AgentRes]
    public class HealthResponse
    {
        public string status;
        public string serverVersion;
        public int port;
        public string uptime;
    }

    /// <summary>
    /// Game status response - 游戏运行状态
    /// </summary>
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
