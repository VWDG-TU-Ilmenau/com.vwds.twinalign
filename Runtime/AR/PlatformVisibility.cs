using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MetaReal.CrossPlatform
{
    public class PlatformVisibility : MonoBehaviour
    {
        public PlatformSpecific Platform;
        // Start is called before the first frame update
        void Awake()
        {
            if((Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer) && Platform == PlatformSpecific.VR)
            {
                gameObject.SetActive(false);
            }
            else
            {
                if((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) && Platform == PlatformSpecific.AR)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
    
    public enum PlatformSpecific 
    {
        AR,
        VR
    }
}

