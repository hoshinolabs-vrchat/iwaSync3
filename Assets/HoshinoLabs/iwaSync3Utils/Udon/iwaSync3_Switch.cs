using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace HoshinoLabs.Udon
{
    public class iwaSync3_Switch : UdonSharpBehaviour
    {
#if UDONCONSOLE
        UConsole _c;
#endif

        const string _appname = "iwaSync3.Switch";

        [SerializeField]
        IwaSync3 iwaSync3;
        [SerializeField]
        bool interact = true;
        [SerializeField]
        bool global = true;
        [SerializeField]
        bool defaultOn = true;
        [SerializeField]
        GameObject visualOn = null;
        [SerializeField]
        GameObject visualOff = null;

        bool _on;
        
#if UDONCONSOLE
        void InitializeUConsoleIfNeeded()
        {
            if (!GameObject.Find(nameof(UConsole)))
                return;
            _c = GameObject.Find(nameof(UConsole)).GetComponent<UConsole>();
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

            _on = defaultOn;

            enabled = false;
            ValidateView();
        }

        bool IsMaster()
        {
#if UNITY_EDITOR
            return true;
#else
            return Networking.IsMaster;
#endif
        }

        public override void Interact()
        {
            if (!interact)
                return;
            Toggle();
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
                if (global)
                {
                    if (_on)
                        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnOn));
                    else
                        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnOff));
                }
                else
                {
                    if (_on)
                        OnOn();
                    else
                        OnOff();
                }
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
#if UDONCONSOLE
            DebugLog($"`{player.displayName}` has left room.");
#endif
            ValidateView();
        }

        public void On()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a on event.");
#endif
            if (global)
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnOn));
            else
                OnOn();
        }

        public void OnOn()
        {
#if UDONCONSOLE
            DebugLog($"Received a on event.");
#endif
            _on = true;
            ValidateView();
        }

        public void Off()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a off event.");
#endif
            iwaSync3.PowerOff();
            if (global)
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnOff));
            else
                OnOff();
        }

        public void OnOff()
        {
#if UDONCONSOLE
            DebugLog($"Received a off event.");
#endif
            _on = false;
            ValidateView();
        }

        public void Toggle()
        {
#if UDONCONSOLE
            DebugLog($"Trigger a toggle event.");
#endif
            if (_on)
                Off();
            else
                On();
        }

        void ValidateView()
        {
            foreach (Transform x in iwaSync3.transform)
                x.gameObject.SetActive(_on);
            if (visualOn)
                visualOn.SetActive(_on);
            if (visualOff)
                visualOff.SetActive(!_on);
        }
    }
}
