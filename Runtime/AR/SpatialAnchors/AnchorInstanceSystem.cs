using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace vwds.twinalign
{
    public class AnchorInstanceSystem : MonoBehaviour
    {
        public static AnchorInstanceSystem Instance;
        public List<AnchorInstance> AnchorInstancesList;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
        }
        public void AddAnchorInstanceToList(AnchorInstance anchorInstance)
        {
            if (AnchorInstancesList == null)
                AnchorInstancesList = new List<AnchorInstance>();

            AnchorInstancesList.Add(anchorInstance);
        }

        public AnchorInstance GetAnchorInstance(string anchorName)
        {
            return AnchorInstancesList.Find(a => a.AnchorNameId.ToLower() == anchorName.ToLower());
        }
    }
}