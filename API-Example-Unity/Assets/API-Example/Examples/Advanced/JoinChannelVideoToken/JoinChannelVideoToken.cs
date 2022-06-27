﻿using UnityEngine;
using UnityEngine.UI;
using Agora.Rtc;
using Agora.Util;
using UnityEngine.Serialization;
using Logger = Agora.Util.Logger;

namespace Agora_RTC_Plugin.API_Example.Examples.Advanced.JoinChannelVideoToken
{
    public class JoinChannelVideoToken : MonoBehaviour
    {
        [FormerlySerializedAs("appIdInput")]
        [SerializeField]
        private AppIdInput _appIdInput;

        [Header("_____________Basic Configuration_____________")]
        [FormerlySerializedAs("APP_ID")]
        [SerializeField]
        private string _appID = "";

        [FormerlySerializedAs("TOKEN")]
        [SerializeField]
        private string _token = "";

        [FormerlySerializedAs("CHANNEL_NAME")]
        [SerializeField]
        private string _channelName = "";

        public Text LogText;
        private Logger Log;
        internal IRtcEngine RtcEngine = null;

        private static string _channelToken = "";
        private static string _tokenBase = "http://localhost:8080";
        private CONNECTION_STATE_TYPE _state = CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED;

        // Use this for initialization
        private void Start()
        {
            LoadAssetData();
            if (CheckAppId())
            {
                InitEngine();
                JoinChannel();
            }
        }

        private void RenewOrJoinToken(string newToken)
        {
            JoinChannelVideoToken._channelToken = newToken;
            if (_state == CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED
                || _state == CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED
                || _state == CONNECTION_STATE_TYPE.CONNECTION_STATE_FAILED
            )
            {
                // If we are not connected yet, connect to the channel as normal
                JoinChannel();
            }
            else
            {
                // If we are already connected, we should just update the token
                UpdateToken();
            }
        }

        // Update is called once per frame
        private void Update()
        {
            PermissionHelper.RequestMicrophontPermission();
            PermissionHelper.RequestCameraPermission();
        }

        private void UpdateToken()
        {
            RtcEngine.RenewToken(JoinChannelVideoToken._channelToken);
        }

        private bool CheckAppId()
        {
            Log = new Logger(LogText);
            return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in API-Example/profile/appIdInput.asset");
        }

        //Show data in AgoraBasicProfile
        [ContextMenu("ShowAgoraBasicProfileData")]
        private void LoadAssetData()
        {
            if (_appIdInput == null) return;
            _appID = _appIdInput.appID;
            _token = _appIdInput.token;
            _channelToken = _appIdInput.token;
            _channelName = _appIdInput.channelName;
        }

        private void InitEngine()
        {
            RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
            UserEventHandler handler = new UserEventHandler(this);
            RtcEngineContext context = new RtcEngineContext(_appID, 0, true,
                CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
                AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT);
            RtcEngine.Initialize(context);
            RtcEngine.InitEventHandler(handler);
        }

        private void JoinChannel()
        {
            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
            RtcEngine.EnableAudio();
            RtcEngine.EnableVideo();
            
            if (_channelToken.Length == 0)
            {
                StartCoroutine(HelperClass.FetchToken(_tokenBase, _channelName, 0, this.RenewOrJoinToken));
                return;
            }

            RtcEngine.JoinChannel(_channelToken, _channelName, "");
        }

        private void OnDestroy()
        {
            Debug.Log("OnDestroy");
            if (RtcEngine == null) return;
            RtcEngine.InitEventHandler(null);
            RtcEngine.LeaveChannel();
            RtcEngine.Dispose();
        }

        internal string GetChannelName()
        {
            return _channelName;
        }

        private void DestroyVideoView(uint uid)
        {
            GameObject go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                Object.Destroy(go);
            }
        }

        private void MakeVideoView(uint uid, string channelId = "")
        {
            GameObject go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                return; // reuse
            }

            // create a GameObject and assign to this new user
            VideoSurface videoSurface = MakeImageSurface(uid.ToString());
            if (!ReferenceEquals(videoSurface, null))
            {
                // configure videoSurface
                if (uid == 0)
                {
                    videoSurface.SetForUser(uid, channelId);
                }
                else
                {
                    videoSurface.SetForUser(uid, channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
                }

                videoSurface.OnTextureSizeModify += (int width, int height) =>
                {
                    float scale = (float)height / (float)width;
                    videoSurface.transform.localScale = new Vector3(-5, 5 * scale, 1);
                    Debug.Log("OnTextureSizeModify: " + width + "  " + height);
                };

                videoSurface.SetEnable(true);
            }
        }

        // VIDEO TYPE 1: 3D Object
        public static VideoSurface MakePlaneSurface(string goName)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            // set up transform
            go.transform.Rotate(-90.0f, 0.0f, 0.0f);
            go.transform.position = Vector3.zero;
            go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

            // configure videoSurface
            var videoSurface = go.AddComponent<VideoSurface>();
            return videoSurface;
        }

        // Video TYPE 2: RawImage
        public static VideoSurface MakeImageSurface(string goName)
        {
            GameObject go = new GameObject();

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            // to be renderered onto
            go.AddComponent<RawImage>();
            // make the object draggable
            go.AddComponent<UIElementDrag>();
            GameObject canvas = GameObject.Find("VideoCanvas");
            if (canvas != null)
            {
                go.transform.parent = canvas.transform;
                Debug.Log("add video view");
            }
            else
            {
                Debug.Log("Canvas is null video view");
            }

            // set up transform
            go.transform.Rotate(0f, 0.0f, 180.0f);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(3f, 4f, 1f);

            // configure videoSurface
            var videoSurface = go.AddComponent<VideoSurface>();
            return videoSurface;
        }

        internal class UserEventHandler : IRtcEngineEventHandler
        {
            private readonly JoinChannelVideoToken _helloVideoTokenAgora;

            internal UserEventHandler(JoinChannelVideoToken helloVideoTokenAgora)
            {
                _helloVideoTokenAgora = helloVideoTokenAgora;
            }

            public override void OnWarning(int warn, string msg)
            {
                _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnWarning warn: {0}, msg: {1}", warn, msg));
            }

            public override void OnError(int err, string msg)
            {
                _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnError err: {0}, msg: {1}", err, msg));
            }

            public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
            {
                _helloVideoTokenAgora.Log.UpdateLog(string.Format("sdk version: ${0}",
                    _helloVideoTokenAgora.RtcEngine.GetVersion()));
                _helloVideoTokenAgora.Log.UpdateLog(
                    string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                        connection.channelId, connection.localUid, elapsed));
                _helloVideoTokenAgora.Log.UpdateLog(string.Format("New Token: {0}",
                    JoinChannelVideoToken._channelToken));
                // HelperClass.FetchToken(tokenBase, channelName, 0, this.RenewOrJoinToken);
                _helloVideoTokenAgora.MakeVideoView(0);
            }

            public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
            {
                _helloVideoTokenAgora.Log.UpdateLog("OnRejoinChannelSuccess");
            }

            public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
            {
                _helloVideoTokenAgora.Log.UpdateLog("OnLeaveChannel");
                _helloVideoTokenAgora.DestroyVideoView(0);
            }

            public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole,
                CLIENT_ROLE_TYPE newRole)
            {
                _helloVideoTokenAgora.Log.UpdateLog("OnClientRoleChanged");
            }

            public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
            {
                _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid,
                    elapsed));
                _helloVideoTokenAgora.MakeVideoView(uid, _helloVideoTokenAgora.GetChannelName());
            }

            public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
            {
                _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
                    (int)reason));
                _helloVideoTokenAgora.DestroyVideoView(uid);
            }

            public override void OnTokenPrivilegeWillExpire(RtcConnection connection, string token)
            {
                _helloVideoTokenAgora.StartCoroutine(HelperClass.FetchToken(_tokenBase,
                    _helloVideoTokenAgora._channelName, 0, _helloVideoTokenAgora.RenewOrJoinToken));
            }

            public override void OnConnectionStateChanged(RtcConnection connection, CONNECTION_STATE_TYPE state,
                CONNECTION_CHANGED_REASON_TYPE reason)
            {
                _helloVideoTokenAgora._state = state;
            }

            public override void OnConnectionLost(RtcConnection connection)
            {
                _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnConnectionLost "));
            }
        }
    }
}
