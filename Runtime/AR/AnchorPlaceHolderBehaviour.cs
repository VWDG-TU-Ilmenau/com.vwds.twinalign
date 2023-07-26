using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace vwds.twinalign
{
    public class AnchorPlaceHolderBehaviour : MonoBehaviour
    {
        private Camera cam;
        // Start is called before the first frame update
        void Start()
        {
            cam = FindObjectOfType<Camera>();
        }

        // Update is called once per frame
        void Update()
        {
            if (cam == null)
                return;

            transform.LookAt(cam.transform);
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0f, transform.eulerAngles.z);
        }
    }
}