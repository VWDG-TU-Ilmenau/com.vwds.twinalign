using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace vwds.TwinAlign
{
    public class AnchorInstance : MonoBehaviour
    {
        public TMP_Text AnchorTextObject;
        public string AnchorNameId;

        // Start is called before the first frame update
        void Start()
        {
            AnchorInstanceSystem.Instance.AddAnchorInstanceToList(this);
        }

        public void SetAnchorName(string anchorName)
        {
            AnchorTextObject.text = anchorName;
        }
    }
}