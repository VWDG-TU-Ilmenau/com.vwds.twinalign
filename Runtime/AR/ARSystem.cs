using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.ARFoundation;

namespace vwds.twinalign
{
    public class ARSystem : MonoBehaviour
    {
        public static ARSystem Instance;
        public ARInputManager ArInputManager;

        internal void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
        {
            Debug.Log($"{permissionName} PermissionDeniedAndDontAskAgain");
        }

        internal void PermissionCallbacks_PermissionGranted(string permissionName)
        {
            Debug.Log($"{permissionName} PermissionCallbacks_PermissionGranted");
        }

        internal void PermissionCallbacks_PermissionDenied(string permissionName)
        {
            Debug.Log($"{permissionName} PermissionCallbacks_PermissionDenied");
        }

        private void Awake()
        {
            if (Instance != this && Instance != null)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            ArInputManager.enabled = true;
            //This stops the screen from sleeping
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;

            //requestion microphone
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                // The user authorized use of the microphone.
            }
            else
            {
                bool useCallbacks = false;
                if (!useCallbacks)
                {
                    // We do not have permission to use the microphone.
                    // Ask for permission or proceed without the functionality enabled.
                    Permission.RequestUserPermission(Permission.Microphone);
                }
                else
                {
                    var callbacks = new PermissionCallbacks();
                    callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
                    callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
                    callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
                    Permission.RequestUserPermission(Permission.Microphone, callbacks);
                }
            }
        }
    }
}