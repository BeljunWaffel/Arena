#if ENABLE_PLAYFABSERVER_API && ENABLE_PLAYFABAGENT_API
using PlayFab.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PlayFab.Json;
using UnityEngine;
using PlayFab.Networking;

namespace PlayFab
{
    using AgentModels;
    using PlayFab.Networking;
    using System.Text;
    
    #pragma warning disable 414
    public class PlayFabMultiplayerAgentAPI
    {
        private const string GsdkConfigFileEnvVarKey = "GSDK_CONFIG_FILE";

        private static string _baseUrl = string.Empty;

        private static Configuration _config = new Configuration();

        private static ISerializerPlugin _jsonWrapper;

        public delegate void OnShutdownEvent();
        public static event OnShutdownEvent OnShutDown;

        public delegate void OnMaintenanceEvent(DateTime? NextScheduledMaintenanceUtc);
        public static event OnMaintenanceEvent OnMaintenance;

        public delegate void OnAgentCommunicationErrorEvent(string error);
        public static event OnAgentCommunicationErrorEvent OnAgentError;

        public delegate void OnServerActiveEvent();
        public static event OnServerActiveEvent OnServerActive;
        
        public static SessionConfig SessionConfig = new SessionConfig();
        public static HeartbeatRequest CurrentState = new HeartbeatRequest();
        public static Dictionary<string, BackendPlayerInfo> Players = new Dictionary<string, BackendPlayerInfo>();
        public static ErrorStates CurrentErrorState = ErrorStates.Ok;
        public static bool IsProcessing = false;
        public static bool IsDebugging = false;

        public static void Start()
        {
            _jsonWrapper = PlayFab.PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

            string fileName = Environment.GetEnvironmentVariable(GsdkConfigFileEnvVarKey);
            if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                _config = new JsonFileConfiguration(_jsonWrapper, fileName);
            }
            else
            {
                _config = new EnvironmentVariableConfiguration();
            }

            _baseUrl = string.Format("http://{0}/v1/sessionHosts/{1}/heartbeats", _config.HeartbeatEndpoint, _config.ServerId);
            CurrentState.CurrentGameState = GameState.Initializing;
            CurrentErrorState = ErrorStates.Ok;
            CurrentState.CurrentPlayers = new List<ConnectedPlayer>();
            
            if (IsDebugging)
            {
                Debug.Log(_baseUrl);
                Debug.Log(_config.ServerId);
                Debug.Log(_config.LogFolder);
            }

            //Create an agent that can talk on the main-tread and pull on an interval.
            //This is a unity thing, need an object in the scene.
            GameObject agentView = new GameObject("PlayFabAgentView");
            agentView.AddComponent<PlayFabAgentView>();
        }

        public static void ReadyForPlayers()
        {
            CurrentState.CurrentGameState = GameState.StandingBy;
        }
        
        public static void SetState(GameState status)
        {
            CurrentState.CurrentGameState = status;
        }

        public static void AddPlayer(PlayerInfo playerInfo)
        {
            var playerId = playerInfo.PlayFabId;
            if (CurrentState.CurrentPlayers.Find(p => p.PlayerId == playerId) == null)
            {
                CurrentState.CurrentPlayers.Add(new ConnectedPlayer(playerId));
                Players[playerId] = new BackendPlayerInfo(playerInfo);
            }
        }

        public static void RemovePlayer(string playerId)
        {
            var player = CurrentState.CurrentPlayers.Find(p => p.PlayerId == playerId);
            if (player != null)
            {
                CurrentState.CurrentPlayers.Remove(player);
                Players.Remove(playerId);
            }
        }

        public static Configuration GetConfigSettings()
        {
            return new Configuration(_config);
        }

        public static IList<string> GetInitialPlayers()
        {
            return new List<string>(SessionConfig.InitialPlayers);
        }

        internal static void SendHeartBeatRequest()
        {
            var payload = _jsonWrapper.SerializeObject(CurrentState);
            if (string.IsNullOrEmpty(payload)) { return; }
            var payloadBytes = Encoding.ASCII.GetBytes(payload);

            if (IsDebugging)
            {
                Debug.Log(payload);
            }

            PlayFabHttp.SimplePostCall(_baseUrl, payloadBytes, (success) => {
                var json = System.Text.Encoding.UTF8.GetString(success);
                //Debug.Log(json);
                if (string.IsNullOrEmpty(json)) { return;  }
                var hb = _jsonWrapper.DeserializeObject<HeartbeatResponse>(json);
                if(hb != null)
                {
                    ProcessAgentResponse(hb);
                }
                CurrentErrorState = ErrorStates.Ok;
                IsProcessing = false;
            }, (error) => {

                var guid = Guid.NewGuid();
                Debug.LogFormat("CurrentError: {0} - {1}", error, guid.ToString());
                //Exponential backoff for 30 minutes for retries.
                switch (CurrentErrorState)
                {
                    case ErrorStates.Ok:
                        CurrentErrorState = ErrorStates.Retry30S;
                        if (IsDebugging)
                            Debug.Log("Retrying heartbeat in 30s");
                        break;
                    case ErrorStates.Retry30S:
                        CurrentErrorState = ErrorStates.Retry5M;
                        if (IsDebugging)
                            Debug.Log("Retrying heartbeat in 5m");
                        break;
                    case ErrorStates.Retry5M:
                        CurrentErrorState = ErrorStates.Retry10M;
                        if (IsDebugging)
                            Debug.Log("Retrying heartbeat in 10m");
                        break;
                    case ErrorStates.Retry10M:
                        CurrentErrorState = ErrorStates.Retry15M;
                        if (IsDebugging)
                            Debug.Log("Retrying heartbeat in 15m");
                        break;
                    case ErrorStates.Retry15M:
                        CurrentErrorState = ErrorStates.Cancelled;
                        if (IsDebugging)
                            Debug.Log("Agent reconnection cannot be established - cancelling");
                        break;
                }

                if(OnAgentError != null)
                {
                    OnAgentError.Invoke(error);
                }
                IsProcessing = false;
            });
        }

        private static void ProcessAgentResponse(HeartbeatResponse heartBeat)
        {
            SessionConfig.CopyNonNullFields(heartBeat.SessionConfig);

            if(!string.IsNullOrEmpty(heartBeat.NextScheduledMaintenanceUtc))
            {
                DateTime scheduledMaintDate;

                if (DateTime.TryParse(
                    heartBeat.NextScheduledMaintenanceUtc,
                    null,
                    DateTimeStyles.RoundtripKind,
                    out scheduledMaintDate))
                {
                    if (OnMaintenance != null)
                    {
                        OnMaintenance.Invoke(scheduledMaintDate);
                    }
                }
            }

            switch (heartBeat.Operation)
            {
                case GameOperation.Continue:
                    //No Action Required.
                    break;
                case GameOperation.Active:
                    //Transition Server State to Active.
                    CurrentState.CurrentGameState = GameState.Active;
                    if(OnServerActive != null) OnServerActive.Invoke();
                    break;
                case GameOperation.Terminate:
                    if (CurrentState.CurrentGameState == GameState.Terminated) break;
                    //Transition Server to a Termination state.
                    CurrentState.CurrentGameState = GameState.Terminating;
                    if (OnShutDown != null)
                    {
                        OnShutDown.Invoke();
                    }
                    break;
                default:
                    Debug.LogWarning("Unknown operation received" + heartBeat.Operation);
                    break;
            }

            if (IsDebugging)
                Debug.LogFormat("Operation: {0}, Maintenance:{1}, State: {2}", heartBeat.Operation, heartBeat.NextScheduledMaintenanceUtc.ToString(), CurrentState.CurrentGameState);
        }

    }

    public class PlayFabAgentView : MonoBehaviour
    {
        private float _timer = 0f;
        private void LateUpdate()
        {
            if (PlayFabMultiplayerAgentAPI.CurrentState == null) return;
            
            var max = 1f;
            _timer += Time.deltaTime;
            if (PlayFabMultiplayerAgentAPI.CurrentErrorState != ErrorStates.Ok)
            {
                switch (PlayFabMultiplayerAgentAPI.CurrentErrorState)
                {
                    case ErrorStates.Retry30S:
                    case ErrorStates.Retry5M:
                    case ErrorStates.Retry10M:
                    case ErrorStates.Retry15M:
                        max = (float)PlayFabMultiplayerAgentAPI.CurrentErrorState;
                        break;
                    case ErrorStates.Cancelled:
                        max = 1f;
                        break;
                }
            }

            var isTerminating = PlayFabMultiplayerAgentAPI.CurrentState.CurrentGameState == GameState.Terminated ||
                                PlayFabMultiplayerAgentAPI.CurrentState.CurrentGameState == GameState.Terminating;
            var isCancelled = PlayFabMultiplayerAgentAPI.CurrentErrorState == ErrorStates.Cancelled;
            
            if ( !isTerminating && !isCancelled && !PlayFabMultiplayerAgentAPI.IsProcessing && _timer >= max)
            {
                if (PlayFabMultiplayerAgentAPI.IsDebugging)
                {
                    Debug.LogFormat("Timer:{0} - Max:{1}", _timer, max);
                }
                PlayFabMultiplayerAgentAPI.IsProcessing = true;
                _timer = 0f;
                PlayFabMultiplayerAgentAPI.SendHeartBeatRequest();

            }else if (PlayFabMultiplayerAgentAPI.CurrentState.CurrentGameState == GameState.Terminating)
            {
                PlayFabMultiplayerAgentAPI.CurrentState.CurrentGameState = GameState.Terminated;
                PlayFabMultiplayerAgentAPI.IsProcessing = true;
                _timer = 0f;
                PlayFabMultiplayerAgentAPI.SendHeartBeatRequest();
                
            }
        }
    }
}

namespace PlayFab.AgentModels
{
    [Serializable]
    public enum GameState
    {
        Invalid,
        Initializing,
        StandingBy,
        Active,
        Terminating,
        Terminated,
        Quarantined
    }

    [Serializable]
    public enum GameOperation
    {
        Invalid,
        Continue,
        GetManifest,
        Quarantine,
        Active,
        Terminate,
        Operation_Count
    }

    public class BackendPlayerInfo
    {
        public string PlayerId;

        public Vector3 PlayerPosition;

        public Quaternion PlayerRotation;

        public int Health;
        
        public BackendPlayerInfo(PlayerInfo playerInfo)
        {
            UpdateInfo(playerInfo);
        }

        public void UpdateInfo(PlayerInfo playerInfo)
        {
            PlayerId = playerInfo.PlayFabId;
            PlayerPosition = playerInfo.PlayerPosition;
            PlayerRotation = playerInfo.PlayerRotation;
            Health = playerInfo.Health;
        }
    }

    [Serializable]
    public class ConnectedPlayer
    {
        public string PlayerId { get; set; }
        
        public ConnectedPlayer(string playerid)
        {
            PlayerId = playerid;
        }
    }

    [Serializable]
    public class HeartbeatRequest
    {
        public GameState CurrentGameState { get; set; }

        public string CurrentGameHealth { get; set; }

        public List<ConnectedPlayer> CurrentPlayers { get; set; }
    }

    [Serializable]
    public class HeartbeatResponse
    {
        [JsonProperty(PropertyName = "sessionConfig")]
        public SessionConfig SessionConfig { get; set; }

        [JsonProperty(PropertyName = "nextScheduledMaintenanceUtc")]
        public string NextScheduledMaintenanceUtc { get; set; }

        [JsonProperty(PropertyName = "operation")]
        public GameOperation Operation { get; set; }
    }

    [Serializable]
    public class SessionConfig
    {
        [JsonProperty(PropertyName = "sessionId")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "sessionCookie")]
        public string SessionCookie { get; set; }

        [JsonProperty(PropertyName = "initialPlayers")]
        public List<string> InitialPlayers { get; set; }

        public void CopyNonNullFields(SessionConfig other)
        {
            if (other == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(other.SessionId))
            {
                this.SessionId = other.SessionId;
            }

            if (!string.IsNullOrEmpty(other.SessionCookie))
            {
                this.SessionCookie = other.SessionCookie;
            }

            if (other.InitialPlayers != null && other.InitialPlayers.Any())
            {
                this.InitialPlayers = other.InitialPlayers;
            }
        }
    }
    
    [Serializable]
    public enum ErrorStates
    {
        Ok = 0,
        Retry30S = 30,
        Retry5M = 300,
        Retry10M = 600,
        Retry15M = 900,
        Cancelled = -1
    }

    public class Configuration
    {
        public string HeartbeatEndpoint { get; protected set; }
        public string ServerId { get; protected set; }
        public string LogFolder { get; protected set; }
        public string CertificateFolder { get; set; }

        /// <summary>
        /// A folder shared by all the game servers within a VM (to cache user generated content and other data).
        /// </summary>
        public string SharedContentFolder { get; set; }

        public IDictionary<string, string> GameCertificates { get; set; }
        public string TitleId { get; set; }
        public string BuildId { get; set; }
        public string Region { get; set; }
        public IDictionary<string, string> BuildMetadata { get; set; }
        public IDictionary<string, string> GamePorts { get; set; }
        
        protected const string HEARTBEAT_ENDPOINT_ENV_VAR = "HEARTBEAT_ENDPOINT";
        protected const string SERVER_ID_ENV_VAR = "SESSION_HOST_ID";
        protected const string LOG_FOLDER_ENV_VAR = "GSDK_LOG_FOLDER";
        protected const string TITLE_ID_ENV_VAR = "PF_TITLE_ID";
        protected const string BUILD_ID_ENV_VAR = "PF_BUILD_ID";
        protected const string REGION_ENV_VAR = "PF_REGION";
        protected const string SHARED_CONTENT_FOLDER_ENV_VAR = "SHARED_CONTENT_FOLDER";

        public Configuration()
        {
            HeartbeatEndpoint = "localhost:56001";
            GameCertificates = new Dictionary<string, string>();
            BuildMetadata = new Dictionary<string, string>();
            GamePorts = new Dictionary<string, string>();
            TitleId = Environment.GetEnvironmentVariable(TITLE_ID_ENV_VAR);
            BuildId = Environment.GetEnvironmentVariable(BUILD_ID_ENV_VAR);
            Region = Environment.GetEnvironmentVariable(REGION_ENV_VAR);
        }

        public Configuration(Configuration other)
        {
            HeartbeatEndpoint = other.HeartbeatEndpoint;
            ServerId = other.ServerId;
            LogFolder = other.LogFolder;
            CertificateFolder = other.CertificateFolder;
            GameCertificates = new Dictionary<string, string>(other.GameCertificates);
            TitleId = other.TitleId;
            BuildId = other.BuildId;
            Region = other.Region;
            BuildMetadata = new Dictionary<string, string>(other.BuildMetadata);
            GamePorts = new Dictionary<string, string>(other.GamePorts);
        }
    }

    class EnvironmentVariableConfiguration : Configuration
    {
        public EnvironmentVariableConfiguration() : base()
        {
            HeartbeatEndpoint = Environment.GetEnvironmentVariable(HEARTBEAT_ENDPOINT_ENV_VAR);
            ServerId = Environment.GetEnvironmentVariable(SERVER_ID_ENV_VAR);
            LogFolder = Environment.GetEnvironmentVariable(LOG_FOLDER_ENV_VAR);
            SharedContentFolder = Environment.GetEnvironmentVariable(SHARED_CONTENT_FOLDER_ENV_VAR);

            if (string.IsNullOrEmpty(HeartbeatEndpoint) || string.IsNullOrEmpty(ServerId))
            {
                Debug.LogError("Heartbeat endpoint and Server id are required configuration values.");
                Application.Quit();
            }
        }
    }

    class JsonFileConfiguration : Configuration
    {
        public JsonFileConfiguration(ISerializerPlugin jsonSerializer, string fileName) : base()
        {
            try
            {
                using (StreamReader reader = File.OpenText(fileName))
                {
                    JsonGsdkSchema config = jsonSerializer.DeserializeObject<JsonGsdkSchema>(reader.ReadToEnd());

                    HeartbeatEndpoint = config.HeartbeatEndpoint;
                    ServerId = config.SessionHostId;
                    LogFolder = config.LogFolder;
                    SharedContentFolder = config.SharedContentFolder;
                    CertificateFolder = config.CertificateFolder;
                    GameCertificates = config.GameCertificates ?? new Dictionary<string, string>();
                    GamePorts = config.GamePorts ?? new Dictionary<string, string>();
                    BuildMetadata = config.BuildMetadata ?? new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Cannot read configuration file " + fileName);
                Debug.LogException(ex);
                Application.Quit();
            }
        }
    }

    [Serializable]
    public class JsonGsdkSchema
    {
        [JsonProperty(PropertyName = "heartbeatEndpoint")]
        public string HeartbeatEndpoint { get; set; }

        [JsonProperty(PropertyName = "sessionHostId")]
        public string SessionHostId { get; set; }

        [JsonProperty(PropertyName = "logFolder")]
        public string LogFolder { get; set; }

        [JsonProperty(PropertyName = "sharedContentFolder")]
        public string SharedContentFolder { get; set; }

        [JsonProperty(PropertyName = "certificateFolder")]
        public string CertificateFolder { get; set; }

        [JsonProperty(PropertyName = "gameCertificates")]
        public IDictionary<string, string> GameCertificates { get; set; }

        [JsonProperty(PropertyName = "buildMetadata")]
        public IDictionary<string, string> BuildMetadata { get; set; }

        [JsonProperty(PropertyName = "gamePorts")]
        public IDictionary<string, string> GamePorts { get; set; }
    }
}

#endif