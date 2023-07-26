using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace vwds.twinalign
{
    public class LocalOrigin : MonoBehaviour
    {
        public static LocalOrigin Instance;
        public GameObject World;
        public AnchorInstance OriginAnchorInstance;
        private Vector3 localOriginPos;
        private Quaternion localOriginRot;
        private void Awake()
        {
            if (Instance != this && Instance != null)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetLocalOrigin(Vector3 pos, Quaternion rot)
        {
            localOriginPos = pos;
            localOriginRot = rot;
        }

        public Vector3 GetLocalOriginPos()
        {
            return localOriginPos;
        }

        public void AddToWorld(GameObject newObject)
        {
            newObject.transform.parent = World.transform;
        }

        public Quaternion GetLocalOriginRot()
        {
            return localOriginRot;
        }

        public void SetOriginAnchorName(string originAnchorName)
        {
            OriginAnchorInstance.SetAnchorName(originAnchorName);
        }
    }
}
