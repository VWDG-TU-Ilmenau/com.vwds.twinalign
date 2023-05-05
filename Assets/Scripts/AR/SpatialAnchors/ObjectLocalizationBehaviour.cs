using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace vwds.TwinAlign
{
    public class ObjectLocalizationBehaviour : MonoBehaviour
    {
        private Vector3 offSetPosition;
        private Vector3 originalPosition;
        private float threshold = 0.1f;
        private float offsetAngle = 0;
        // Start is called before the first frame update
        void Start()
        {
            originalPosition = transform.position;
            offSetPosition = originalPosition + LocalOrigin.Instance.transform.position;
            LocalOrigin.Instance.AddToWorld(transform.root.gameObject);
        }
    }
}
