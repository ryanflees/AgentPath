using UnityEngine;
using UnityEditor;

namespace CR.OpenClaw
{
    [CustomEditor(typeof(OpenClawAPIServer))]
    public class OpenClawAPIServerEditor : Editor
    {
        private SerializedProperty m_PortProperty;
        private SerializedProperty m_AutoStartProperty;
        private SerializedProperty m_LogRequestsProperty;

        private void OnEnable()
        {
            m_PortProperty = serializedObject.FindProperty("m_Port");
            m_AutoStartProperty = serializedObject.FindProperty("m_AutoStart");
            m_LogRequestsProperty = serializedObject.FindProperty("m_LogRequests");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OpenClawAPIServer server = (OpenClawAPIServer)target;

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("OpenClaw API Server", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Server Status
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Server Status", EditorStyles.boldLabel);

            string statusText = server.IsRunning ? "Running" : "Stopped";
            Color statusColor = server.IsRunning ? Color.green : Color.red;

            GUI.color = statusColor;
            EditorGUILayout.LabelField("Status:", statusText);
            GUI.color = Color.white;

            if (server.IsRunning)
            {
                EditorGUILayout.LabelField("Port:", server.Port.ToString());
                EditorGUILayout.LabelField("Base URL:", $"http://localhost:{server.Port}/");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Configuration
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_PortProperty, new GUIContent("Port", "HTTP server port (default: 8091)"));
            EditorGUILayout.PropertyField(m_AutoStartProperty, new GUIContent("Auto Start", "Start server automatically on play"));
            EditorGUILayout.PropertyField(m_LogRequestsProperty, new GUIContent("Log Requests", "Log all incoming HTTP requests"));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Control Buttons
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            if (server.IsRunning)
            {
                if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
                {
                    server.StopServer();
                }
            }
            else
            {
                if (GUILayout.Button("Start Server", GUILayout.Height(30)))
                {
                    server.StartServer();
                }
            }

            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Server controls are only available in Play Mode", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // API Endpoints Reference
            if (server.IsRunning)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("API Endpoints", EditorStyles.boldLabel);

                string baseUrl = $"http://localhost:{server.Port}";

                EditorGUILayout.LabelField("System:", EditorStyles.miniBoldLabel);
                DrawEndpoint("GET", $"{baseUrl}/api/health");
                DrawEndpoint("GET", $"{baseUrl}/api/status");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Player State:", EditorStyles.miniBoldLabel);
                DrawEndpoint("GET", $"{baseUrl}/api/player/position");
                DrawEndpoint("GET", $"{baseUrl}/api/player/state");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Player Commands:", EditorStyles.miniBoldLabel);
                DrawEndpoint("POST", $"{baseUrl}/api/player/move");
                DrawEndpoint("POST", $"{baseUrl}/api/player/look");
                DrawEndpoint("POST", $"{baseUrl}/api/player/jump");
                DrawEndpoint("POST", $"{baseUrl}/api/player/crouch");
                DrawEndpoint("POST", $"{baseUrl}/api/player/sprint");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Waypoints:", EditorStyles.miniBoldLabel);
                DrawEndpoint("GET", $"{baseUrl}/api/waypoints/all");
                DrawEndpoint("GET", $"{baseUrl}/api/waypoints/nearby");
                DrawEndpoint("GET", $"{baseUrl}/api/waypoints/nearest");
                DrawEndpoint("GET", $"{baseUrl}/api/waypoints/path");
                DrawEndpoint("GET", $"{baseUrl}/api/waypoints/in-view");

                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

            // Auto-repaint when playing to update status
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawEndpoint(string method, string url)
        {
            EditorGUILayout.BeginHorizontal();

            Color methodColor = method == "GET" ? new Color(0.3f, 0.7f, 1f) : new Color(1f, 0.6f, 0.3f);
            GUI.color = methodColor;
            GUILayout.Label(method, GUILayout.Width(45));
            GUI.color = Color.white;

            EditorGUILayout.SelectableLabel(url, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            EditorGUILayout.EndHorizontal();
        }
    }
}
