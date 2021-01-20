using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace HoshinoLabs.Udon
{
    public class IwaSync3 : UdonSharpBehaviour
    {
#if UDONCONSOLE
        UConsole _c;
#endif

        const string _appname = "iwaSync3";
        const string _version = "V3.0e";

        [SerializeField]
        bool masterOnly = false;
        [SerializeField]
        bool allowSeeking = true;
        [SerializeField]
        bool mirrorReflection = false;

        BaseVRCVideoPlayer _player1;
        BaseVRCVideoPlayer _player2;

        GameObject _panel1;
        GameObject _lock1;
        Button _lock1Button;
        Image _lock1Image;
        GameObject _lock2;
        Button _lock2Button;
        Image _lock2Image;
        Text _versionText;
        GameObject _video;
        Button _videoButton;
        Text _videoText;
        GameObject _live;
        Button _liveButton;
        Text _liveText;

        GameObject _panel2;
        GameObject _pause;
        Button _pauseButton;
        Image _pauseImage;
        GameObject _play;
        Button _playButton;
        Image _playImage;
        GameObject _message;
        Text _messageText;
        GameObject _progress;
        Slider _progressSlider;
        GameObject _address;
        VRCUrlInputField _addressInput;
        GameObject _sync;
        GameObject _off;
        Button _offButton;
        Text _offText;

        GameObject _quad;
        GameObject _screen1;
        GameObject _screen1Quad1;
        GameObject _screen1Quad2;
        GameObject _screen2;
        GameObject _screen2Quad1;
        GameObject _screen2Quad2;

        bool _dummyScreen;
        Color _normalColor;
        Color _disabledColor;

        const int _status_off = 0x00000001;
        const int _status_on = 0x00000002;
        const int _status_video = 0x00000010;
        const int _status_live = 0x00000020;
        const int _status_stop = 0x00010000;
        const int _status_fetch = 0x00020000;
        const int _status_play = 0x00040000;
        const int _status_pause = 0x00080000;
        const int _status_error = 0x00100000;
        const int _status_stream = 0x01000000;
        const int _status_core = 0x10000000;

        int _status = _status_off | _status_stop;
        BaseVRCVideoPlayer _player = null;
        bool _progressDrag = false;
        VRCUrl _url = VRCUrl.Empty;
        [UdonSynced]
        VRCUrl _urlSync = VRCUrl.Empty;
        int _serial = 0;
        [UdonSynced]
        int _serialSync = 0;
        float _time = 0f;
        [UdonSynced]
        float _timeSync = 0f;

#if UDONCONSOLE
        void InitializeUConsoleIfNeeded()
        {
            if (!GameObject.Find(nameof(UConsole)))
                return;
            _c = GameObject.Find(nameof(UConsole)).GetComponent<UConsole>();
            _c.AddFilter(_appname, null, "iS3");
        }
#endif

        void DebugLog(object message)
        {
#if UDONCONSOLE
            if (_c)
            {
                _c.LogT(_appname, message);
                return;
            }
#endif
            Debug.Log($"[{_appname}] {message}");
        }

        private void Start()
        {
#if UDONCONSOLE
            InitializeUConsoleIfNeeded();
#endif
            DebugLog($"Started `{_appname} {_version}`.");

            _player1 = (VRCUnityVideoPlayer)GetComponent(typeof(VRCUnityVideoPlayer));
            _player2 = (VRCAVProVideoPlayer)GetComponent(typeof(VRCAVProVideoPlayer));

            _panel1 = transform.Find("Control/Panel").gameObject;
            _lock1 = transform.Find("Control/Panel/Lock").gameObject;
            _lock1Button = transform.Find("Control/Panel/Lock/Button").GetComponent<Button>();
            _lock1Image = transform.Find("Control/Panel/Lock/Button/Image").GetComponent<Image>();
            _lock2 = transform.Find("Control/Panel/UnLock").gameObject;
            _lock2Button = transform.Find("Control/Panel/UnLock/Button").GetComponent<Button>();
            _lock2Image = transform.Find("Control/Panel/UnLock/Button/Image").GetComponent<Image>();
            _versionText = transform.Find("Control/Panel/Text").GetComponent<Text>();
            _versionText.text = $"{_appname} {_version}";
            _video = transform.Find("Control/Panel/Video").gameObject;
            _videoButton = transform.Find("Control/Panel/Video/Button").GetComponent<Button>();
            _videoText = transform.Find("Control/Panel/Video/Button/Text").GetComponent<Text>();
            _live = transform.Find("Control/Panel/Live").gameObject;
            _liveButton = transform.Find("Control/Panel/Live/Button").GetComponent<Button>();
            _liveText = transform.Find("Control/Panel/Live/Button/Text").GetComponent<Text>();

            _panel2 = transform.Find("Control/Panel (1)").gameObject;
            _pause = transform.Find("Control/Panel (1)/Pause").gameObject;
            _pauseButton = transform.Find("Control/Panel (1)/Pause/Button").GetComponent<Button>();
            _pauseImage = transform.Find("Control/Panel (1)/Pause/Button/Image").GetComponent<Image>();
            _play = transform.Find("Control/Panel (1)/Play").gameObject;
            _playButton = transform.Find("Control/Panel (1)/Play/Button").GetComponent<Button>();
            _playImage = transform.Find("Control/Panel (1)/Play/Button/Image").GetComponent<Image>();
            _message = transform.Find("Control/Panel (1)/Message").gameObject;
            _messageText = transform.Find("Control/Panel (1)/Message/Text").GetComponent<Text>();
            _progress = transform.Find("Control/Panel (1)/Message/Progress").gameObject;
            _progressSlider = transform.Find("Control/Panel (1)/Message/Progress").GetComponent<Slider>();
            _address = transform.Find("Control/Panel (1)/Address").gameObject;
            _addressInput = (VRCUrlInputField)transform.Find("Control/Panel (1)/Address").GetComponent(typeof(VRCUrlInputField));
            _sync = transform.Find("Control/Panel (1)/Sync").gameObject;
            _off = transform.Find("Control/Panel (1)/PowerOff").gameObject;
            _offButton = transform.Find("Control/Panel (1)/PowerOff/Button").GetComponentInChildren<Button>();
            _offText = transform.Find("Control/Panel (1)/PowerOff/Button/Text").GetComponentInChildren<Text>();

            _quad = transform.Find("Screen/Quad").gameObject;
            _screen1 = transform.Find("Screen/Video").gameObject;
            _screen1Quad1 = transform.Find("Screen/Video/Quad").gameObject;
            if (mirrorReflection)
                _screen1Quad1.layer = 4;
            _screen1Quad1.transform.position = _screen1Quad1.transform.localPosition + _quad.transform.position;
            _screen1Quad1.transform.rotation = _quad.transform.rotation;
            _screen1Quad1.transform.localScale = _quad.transform.localScale;
            _screen1Quad2 = transform.Find("Screen/Video/Quad (1)").gameObject;
            _screen1Quad2.SetActive(mirrorReflection);
            if (mirrorReflection)
                _screen1Quad2.layer = 18;
            _screen1Quad2.transform.position = _quad.transform.position;
            _screen1Quad2.transform.rotation = _quad.transform.rotation;
            _screen1Quad2.transform.localScale = Vector3.Scale(_quad.transform.localScale, new Vector3(-1f, 1f, 1f));
            _screen2 = transform.Find("Screen/Live").gameObject;
            _screen2Quad1 = transform.Find("Screen/Live/Quad").gameObject;
            if (mirrorReflection)
                _screen2Quad1.layer = 4;
            _screen2Quad1.transform.position = _screen2Quad1.transform.localPosition + _quad.transform.position;
            _screen2Quad1.transform.rotation = _quad.transform.rotation;
            _screen2Quad1.transform.localScale = _quad.transform.localScale;
            _screen2Quad2 = transform.Find("Screen/Live/Quad (1)").gameObject;
            _screen2Quad2.SetActive(mirrorReflection);
            if (mirrorReflection)
                _screen2Quad2.layer = 18;
            _screen2Quad2.transform.position = _quad.transform.position;
            _screen2Quad2.transform.rotation = _quad.transform.rotation;
            _screen2Quad2.transform.localScale = Vector3.Scale(_quad.transform.localScale, new Vector3(-1f, 1f, 1f));

            _dummyScreen = _quad.activeSelf;
            _normalColor = _addressInput.selectionColor;
            _disabledColor = _addressInput.colors.disabledColor;

            OnPowerOff();
        }

        bool IsMaster()
        {
#if UNITY_EDITOR
            return true;
#else
            return Networking.IsMaster;
#endif
        }

        bool IsOwner()
        {
#if UNITY_EDITOR
            return true;
#else
            return Networking.IsOwner(gameObject);
#endif
        }

        void TakeOwnership()
        {
#if UDONCONSOLE
            DebugLog($"Request of ownership.");
#endif
#if !UNITY_EDITOR
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
#endif
        }

        public override void OnOwnershipTransferred()
        {
#if UDONCONSOLE
            DebugLog($"ownership transferred.");
#endif
            ValidateView();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
#if UDONCONSOLE
            DebugLog($"`{player.displayName}` has joined room.");
#endif
            if (IsMaster())
            {
#if UDONCONSOLE
                DebugLog($"I'm room master.");
#endif
                if (masterOnly)
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnLock));
                else
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnUnLock));
            }
            if (IsOwner())
            {
#if UDONCONSOLE
                DebugLog($"I'm room owner.");
#endif
                if (IsStatus(_status_on) && IsStatus(_status_video))
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPowerOnVideo));
                if (IsStatus(_status_on) && IsStatus(_status_live))
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPowerOnLive));
                if (IsStatus(_status_on) && IsStatus(_status_pause))
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPause));
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
#if UDONCONSOLE
            DebugLog($"`{player.displayName}` has left room.");
#endif
            ValidateView();
        }

        public override void OnVideoEnd()
        {
            if (!IsOwner())
                return;
#if UDONCONSOLE
            DebugLog($"The video has reached the end.");
#endif
            PowerOff();
        }

        public override void OnVideoError(VideoError videoError)
        {
#if UDONCONSOLE
            DebugLog($"There was a `{videoError}` error in the video.");
#endif
            _status = _status | _status_error;
            _messageText.text = $"Error:{videoError}";
            ValidateView();
        }

        public override void OnVideoReady()
        {
            if (!_player)
                return;
#if UDONCONSOLE
            DebugLog($"The video is ready.");
#endif
            _status = _status & ~_status_fetch | _status_play;
            if (float.IsInfinity(_player.GetDuration()))
                _status = _status | _status_stream;
            SetElapsedTime(_timeSync);
            _player.Play();
            if (IsStatus(_status_pause))
                _player.Pause();
            ValidateView();
        }

        public override void OnDeserialization()
        {
            if (string.IsNullOrEmpty(_urlSync.Get()))
                return;
            if (_serial != _serialSync)
                LoadURL(_urlSync, _serialSync);
            if (_time != _timeSync)
                SetElapsedTime(_timeSync);
        }

        public void Lock()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a lock event.");
#endif
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnLock));
        }

        public void OnLock()
        {
#if UDONCONSOLE
            DebugLog($"Received a lock event.");
#endif
            masterOnly = true;
            ValidateView();
        }

        public void UnLock()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a unlock event.");
#endif
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnUnLock));
        }

        public void OnUnLock()
        {
#if UDONCONSOLE
            DebugLog($"Received a unlock event.");
#endif
            masterOnly = false;
            ValidateView();
        }

        public void PowerOnVideo()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a video player power-on event.");
#endif
            TakeOwnership();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPowerOnVideo));
        }

        public void OnPowerOnVideo()
        {
#if UDONCONSOLE
            DebugLog($"Received a video player power-on event.");
#endif
            enabled = true;
            _status = _status & ~_status_off | _status_on | _status_video | _status_stop;
            _player = _player1;
            ValidateView();
        }

        public void PowerOnLive()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a live player power-on event.");
#endif
            TakeOwnership();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPowerOnLive));
        }

        public void OnPowerOnLive()
        {
#if UDONCONSOLE
            DebugLog($"Received a live player power-on event.");
#endif
            enabled = true;
            _status = _status & ~_status_off | _status_on | _status_live | _status_stop;
            _player = _player2;
            ValidateView();
        }

        public void PowerOff()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a power-off event.");
#endif
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPowerOff));
        }

        public void OnPowerOff()
        {
#if UDONCONSOLE
            DebugLog($"Received a power-off event.");
#endif
            enabled = false;
            _status = _status_off | _status_stop;
            _player1.Stop();
            _player2.Stop();
            _addressInput.SetUrl(VRCUrl.Empty);
            _player = null;
            _progressDrag = false;
            ValidateView();
        }

        public void OnProgressBeginDrag()
        {
#if UDONCONSOLE
            DebugLog($"Progress has started dragging.");
#endif
            _progressDrag = true;
        }

        public void OnProgressEndDrag()
        {
#if UDONCONSOLE
            DebugLog($"Progress drag is finished.");
#endif
            _progressDrag = false;
        }

        public void OnProgressChanged()
        {
            if (!_progressDrag)
                return;
#if UDONCONSOLE
            DebugLog($"Progress value has changed.");
#endif
            var offset = _player.GetDuration() * _progressSlider.value;
            SetOffsetTime(offset);
            SetElapsedTime(_timeSync);
        }

        void SetElapsedTime(float time)
        {
            if (!_player)
                return;
#if UDONCONSOLE
            DebugLog($"Set the progress time to {(float)Networking.GetServerTimeInSeconds() - time}.");
#endif
            _time = time;
            _player.SetTime((float)Networking.GetServerTimeInSeconds() - time);
            ValidateView();
        }

        public void OnURLChanged()
        {
#if UDONCONSOLE
            DebugLog($"The URL has changed to `{_addressInput.GetUrl().Get()}`. Next serial is {_serialSync + 1}.");
#endif
            _urlSync = _addressInput.GetUrl();
            _serialSync++;
            SetOffsetTime(GetOffsetTime(_urlSync.Get()));
            LoadURL(_urlSync, _serialSync);
        }

        float GetOffsetTime(string url)
        {
            if (url.Contains("youtube.com/watch") || url.Contains("youtu.be/"))
            {
                var t = url.IndexOf("?t=");
                if (t < 0)
                    t = url.IndexOf("&t=");
                if (t < 0)
                    t = url.IndexOf("?start=");
                if (t < 0)
                    t = url.IndexOf("&start=");
                if (t < 0)
                    return 0f;
                return ParseNumber(url.Substring(url.IndexOf("=", t) + 1));
            }

            return 0f;
        }

        float ParseNumber(string s)
        {
            float val;
            if (string.IsNullOrEmpty(s))
                return 0f;
            if (float.TryParse(s, out val))
                return val;
            return ParseNumber(s.Substring(0, s.Length - 1));
        }

        void SetOffsetTime(float offset)
        {
#if UDONCONSOLE
            DebugLog($"Set the offset time to {offset}.");
#endif
            _timeSync = (float)Networking.GetServerTimeInSeconds() - offset;
        }

        void LoadURL(VRCUrl url, int serial)
        {
            if (!_player)
                return;
#if UDONCONSOLE
            DebugLog($"Load the video with the URL `{url.Get()}` and set the serial to {serial}.");
#endif
            _status = _status & ~_status_stop | _status_fetch;
            _url = url;
            _serial = serial;
            _messageText.text = $"Loading";
            _player.Stop();
            _player.LoadURL(url);
            ValidateView();
        }

        bool IsStatus(int val)
        {
            return (_status & val) == val;
        }

        void ValidateView()
        {
            _panel1.SetActive(IsStatus(_status_off));
            _lock1.SetActive(!masterOnly);
            _lock1Button.interactable = IsMaster();
            _lock1Image.color = _lock1Button.interactable ? _normalColor : _disabledColor;
            _lock2.SetActive(masterOnly);
            _lock2Button.interactable = IsMaster();
            _lock2Image.color = _lock2Button.interactable ? _normalColor : _disabledColor;
            _videoButton.interactable = (masterOnly && IsMaster()) || !masterOnly;
            _videoText.color = _videoButton.interactable ? _normalColor : _disabledColor;
            _liveButton.interactable = (masterOnly && IsMaster()) || !masterOnly;
            _liveText.color = _liveButton.interactable ? _normalColor : _disabledColor;
            _panel2.SetActive(IsStatus(_status_on));
            _pause.SetActive((IsStatus(_status_play) && !IsStatus(_status_pause)) && !IsStatus(_status_error));
            _pauseButton.interactable = (masterOnly && IsMaster()) || !masterOnly;
            _pauseImage.color = _pauseButton.interactable ? _normalColor : _disabledColor;
            _play.SetActive((IsStatus(_status_play) && IsStatus(_status_pause)) && !IsStatus(_status_error));
            _playButton.interactable = (masterOnly && IsMaster()) || !masterOnly;
            _playImage.color = _playButton.interactable ? _normalColor : _disabledColor;
            _message.SetActive((IsStatus(_status_fetch) || IsStatus(_status_play) || IsStatus(_status_pause)) || IsStatus(_status_error));
            _progress.SetActive((IsStatus(_status_play) || IsStatus(_status_pause)) && !IsStatus(_status_error) && !IsStatus(_status_stream));
            _progressSlider.interactable = ((masterOnly && IsMaster()) || !masterOnly) && allowSeeking && IsOwner();
            _address.SetActive((!IsStatus(_status_fetch) && !IsStatus(_status_play) && !IsStatus(_status_pause) && IsOwner()) && !IsStatus(_status_error));
            _sync.SetActive((IsStatus(_status_play) || IsStatus(_status_pause)) && !IsStatus(_status_error));
            _offButton.interactable = (masterOnly && IsMaster()) || !masterOnly;
            _offText.color = _offButton.interactable ? _normalColor : _disabledColor;
            _quad.SetActive(IsStatus(_status_play) ? false : _dummyScreen);
            _screen1.SetActive(IsStatus(_status_video) && IsStatus(_status_play));
            _screen2.SetActive(IsStatus(_status_live) && IsStatus(_status_play));
        }

        public void Pause()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a pause event.");
#endif
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPause));
        }

        public void OnPause()
        {
            if (!_player)
                return;
#if UDONCONSOLE
            DebugLog($"Received a pause event.");
#endif
            _status = _status | _status_pause;
            SetOffsetTime(_player.GetTime());
            _player.Pause();
            ValidateView();
        }

        public void Play()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a play event.");
#endif
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPlay));
        }

        public void OnPlay()
        {
            if (!_player)
                return;
#if UDONCONSOLE
            DebugLog($"Received a play event.");
#endif
            _status = _status & ~_status_pause;
            SetOffsetTime(_player.GetTime());
            _player.Play();
            ValidateView();
        }

        public void Sync()
        {
            if (!_player)
                return;
#if UDONCONSOLE
            DebugLog($"Trigger a sync event.");
#endif
            _status = _status & ~_status_play & ~_status_pause | _status_stop;
            LoadURL(_urlSync, _serialSync);
        }

        private void Update()
        {
            if (!_player)
                return;
            if (!_player.IsReady)
                return;
            var time = _player.GetTime();
            _messageText.text = $"{TimeSpan.FromSeconds(time).ToString(@"hh\:mm\:ss")}";
            var duration = _player.GetDuration();
            if (!float.IsInfinity(duration))
            {
                _messageText.text = $"{_messageText.text}/{TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss")}";
                if(!_progressDrag)
                    _progressSlider.value = time / duration;
            }
        }
    }
}
