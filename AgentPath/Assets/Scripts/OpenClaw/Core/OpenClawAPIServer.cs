using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace CR.OpenClaw
{
    /// <summary>
    /// OpenClaw API Server - HTTP server for agent communication
    /// </summary>
    public class OpenClawAPIServer : MetaGameSingleton<OpenClawAPIServer>, IEndpointRegistry
    {
        #region Configuration

        [Header("Server Configuration")]
        [SerializeField] private int m_Port = 8091;
        [SerializeField] private bool m_AutoStart = true;
        [SerializeField] private bool m_LogRequests = true;

        #endregion

        #region Private Fields

        private HttpListener m_HttpListener;
        private Thread m_ListenerThread;
        private ServerState m_State = ServerState.Stopped;
        private DateTime m_StartTime;

        // Route dictionaries for extensible routing
        private Dictionary<string, Func<HttpListenerRequest, string>> m_GetRoutes;
        private Dictionary<string, Func<HttpListenerRequest, string>> m_PostRoutes;

        // Service registry
        private List<IAPIService> m_RegisteredServices;

        #endregion

        #region Properties

        //public static OpenClawAPIServer Instance => m_Instance;
        public bool IsRunning => m_State == ServerState.Running;
        public int Port => m_Port;

        #endregion

        #region Unity Lifecycle

        protected override void OnAwake()
        {
            base.OnAwake();
            InitializeRouting();
        }

        private void Start()
        {
            if (m_AutoStart)
            {
                StartServer();
            }
        }

        private void OnDestroy()
        {
            StopServer();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }

        #endregion

        #region Server Lifecycle

        public void StartServer()
        {
            if (m_State != ServerState.Stopped)
            {
                Debug.LogWarning("[OpenClawAPI] Server is already running or starting");
                return;
            }

            m_State = ServerState.Starting;
            m_StartTime = DateTime.UtcNow;

            try
            {
                // Ensure dispatcher exists
                //UnityMainThreadDispatcher.Instance;

                // Create and configure HTTP listener
                m_HttpListener = new HttpListener();
                m_HttpListener.Prefixes.Add($"http://localhost:{m_Port}/");
                m_HttpListener.Prefixes.Add($"http://127.0.0.1:{m_Port}/");
                m_HttpListener.Start();

                // Start listener thread
                m_ListenerThread = new Thread(ListenerThreadLoop)
                {
                    IsBackground = true,
                    Name = "OpenClawAPIListener"
                };
                m_ListenerThread.Start();

                m_State = ServerState.Running;
                Debug.Log($"[OpenClawAPI] Server started on port {m_Port}");
            }
            catch (Exception ex)
            {
                m_State = ServerState.Stopped;
                Debug.LogError($"[OpenClawAPI] Failed to start server: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        public void StopServer()
        {
            if (m_State == ServerState.Stopped)
            {
                return;
            }

            m_State = ServerState.Stopping;
            Debug.Log("[OpenClawAPI] Stopping server...");

            try
            {
                // Stop HTTP listener
                if (m_HttpListener != null && m_HttpListener.IsListening)
                {
                    m_HttpListener.Stop();
                    m_HttpListener.Close();
                }

                // Wait for thread to finish (with timeout)
                if (m_ListenerThread != null && m_ListenerThread.IsAlive)
                {
                    if (!m_ListenerThread.Join(2000))
                    {
                        Debug.LogWarning("[OpenClawAPI] Listener thread did not stop gracefully");
                    }
                }

                m_State = ServerState.Stopped;
                Debug.Log("[OpenClawAPI] Server stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenClawAPI] Error stopping server: {ex.Message}");
                m_State = ServerState.Stopped;
            }
        }

        #endregion

        #region Initialization

        private void InitializeRouting()
        {
            m_GetRoutes = new Dictionary<string, Func<HttpListenerRequest, string>>();
            m_PostRoutes = new Dictionary<string, Func<HttpListenerRequest, string>>();
            m_RegisteredServices = new List<IAPIService>();

            // Register core system endpoints
            RegisterSystemEndpoints();
        }

        private void RegisterSystemEndpoints()
        {
            // System endpoints - these stay in the server
            m_GetRoutes["/api/health"] = HandleHealthCheck;
            m_GetRoutes["/api/status"] = HandleGameStatus;
        }

        /// <summary>
        /// Register a new API service
        /// </summary>
        public void RegisterService(IAPIService service)
        {
            if (service == null)
            {
                Debug.LogWarning("[OpenClawAPI] Cannot register null service");
                return;
            }

            m_RegisteredServices.Add(service);
            service.RegisterEndpoints(this);

            Debug.Log($"[OpenClawAPI] Registered service: {service.GetType().Name}");
        }

        /// <summary>
        /// Unregister a service (removes all its endpoints)
        /// </summary>
        public void UnregisterService(IAPIService service)
        {
            if (service == null) return;

            m_RegisteredServices.Remove(service);
            // Note: We don't remove endpoints as they might be shared
            // If you need to remove endpoints, you'll need to track which service registered which endpoint
        }

        #endregion

        #region IEndpointRegistry Implementation

        /// <summary>
        /// Register a GET endpoint (called by services)
        /// </summary>
        public void RegisterGet(string path, Func<HttpListenerRequest, string> handler)
        {
            if (m_GetRoutes.ContainsKey(path))
            {
                Debug.LogWarning($"[OpenClawAPI] GET endpoint already registered: {path}");
                return;
            }

            m_GetRoutes[path] = handler;
            if (m_LogRequests)
            {
                Debug.Log($"[OpenClawAPI] Registered GET {path}");
            }
        }

        /// <summary>
        /// Register a POST endpoint (called by services)
        /// </summary>
        public void RegisterPost(string path, Func<HttpListenerRequest, string> handler)
        {
            if (m_PostRoutes.ContainsKey(path))
            {
                Debug.LogWarning($"[OpenClawAPI] POST endpoint already registered: {path}");
                return;
            }

            m_PostRoutes[path] = handler;
            if (m_LogRequests)
            {
                Debug.Log($"[OpenClawAPI] Registered POST {path}");
            }
        }

        #endregion

        #region HTTP Listener Thread

        private void ListenerThreadLoop()
        {
            Debug.Log("[OpenClawAPI] Listener thread started");

            while (m_State == ServerState.Running && m_HttpListener != null && m_HttpListener.IsListening)
            {
                try
                {
                    // Wait for incoming request (blocking call)
                    HttpListenerContext context = m_HttpListener.GetContext();

                    // Process request on thread pool to avoid blocking
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Expected when stopping the listener
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OpenClawAPI] Listener error: {ex.Message}");
                }
            }

            Debug.Log("[OpenClawAPI] Listener thread stopped");
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                if (m_LogRequests)
                {
                    Debug.Log($"[OpenClawAPI] {request.HttpMethod} {request.Url.PathAndQuery}");
                }

                // Route the request
                string responseString = RouteRequest(request);

                // Send response
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenClawAPI] Error processing request: {ex.Message}");
                Debug.LogException(ex);

                try
                {
                    string errorResponse = ResponseBuilder.CreateStandardError(StandardError.InternalError, ex.Message);
                    byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                    response.ContentType = "application/json";
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode = 500;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch
                {
                    // Failed to send error response
                }
            }
            finally
            {
                try
                {
                    response.OutputStream.Close();
                }
                catch
                {
                    // Ignore close errors
                }
            }
        }

        private string RouteRequest(HttpListenerRequest request)
        {
            string path = request.Url.AbsolutePath;
            string method = request.HttpMethod;

            // Select appropriate route dictionary
            Dictionary<string, Func<HttpListenerRequest, string>> routes = null;
            if (method == "GET")
            {
                routes = m_GetRoutes;
            }
            else if (method == "POST")
            {
                routes = m_PostRoutes;
            }

            // Find and execute handler
            if (routes != null && routes.TryGetValue(path, out var handler))
            {
                return handler(request);
            }

            // Route not found
            return ResponseBuilder.CreateErrorResponse("ROUTE_NOT_FOUND", $"No handler found for {method} {path}");
        }

        #endregion

        #region System Endpoint Handlers

        private string HandleHealthCheck(HttpListenerRequest request)
        {
            // Health check doesn't need Unity APIs, can run on background thread
            var health = new HealthResponse
            {
                status = "healthy",
                serverVersion = "1.0.0",
                port = m_Port,
                uptime = (DateTime.UtcNow - m_StartTime).ToString(@"hh\:mm\:ss")
            };

            return ResponseBuilder.CreateSuccessResponse(health);
        }

        private string HandleGameStatus(HttpListenerRequest request)
        {
            // Must execute on main thread because it uses Unity APIs
            return ExecuteOnMainThread(() => GetGameStatus());
        }

        private string GetGameStatus()
        {
            int progress1 = UserDataManager.GetProgress(1);
            int progress2 = UserDataManager.GetProgress(2);
            LevelComponent levelComponent = GameObject.FindObjectOfType<LevelComponent>();
            string levelName = "unkown";
            if (levelComponent != null)
            {
                levelName = levelComponent.gameObject.name;
            }
            var status = new GameStatusResponse
            {
                isPlaying = Application.isPlaying,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                gameTime = Time.time,
                playerExists = (GameplayManager.Instance != null && GameplayManager.Instance.m_PlayerController != null), 
                worldAxisUp =  "y",
                worldAxisForward = "z",
                worldAxisRight = "x",
                chapter1Progress =  progress1,
                chapter2Progress =  progress2,
                currentLevel = levelName
            };

            return ResponseBuilder.CreateSuccessResponse(status);
        }

        #endregion

        #region Helper Methods (Public for Services)

        /// <summary>
        /// Read request body from POST request
        /// </summary>
        public string ReadRequestBody(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return null;
            }

            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Execute a function on Unity main thread (thread-safe)
        /// </summary>
        public string ExecuteOnMainThread(Func<string> function)
        {
            string result = null;
            ManualResetEvent completed = new ManualResetEvent(false);

            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                try
                {
                    result = function();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OpenClawAPI] Error executing on main thread: {ex.Message}");
                    result = ResponseBuilder.CreateStandardError(StandardError.InternalError, ex.Message);
                }
                finally
                {
                    completed.Set();
                }
            });

            // Wait for completion with timeout
            if (!completed.WaitOne(200))
            {
                Debug.LogWarning("[OpenClawAPI] Main thread execution timeout");
                return ResponseBuilder.CreateStandardError(StandardError.InternalError, "Request timeout");
            }

            return result;
        }

        #endregion

        #region Server State Enum

        private enum ServerState
        {
            Stopped,
            Starting,
            Running,
            Stopping
        }

        #endregion
    }
}
