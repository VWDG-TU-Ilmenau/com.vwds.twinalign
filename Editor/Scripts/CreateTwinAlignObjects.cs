#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace vwds.twinalign.editor
{
    public class CreateTwinAlignObjects : Editor
    {
        public static GameObject AnchorInstancePrefab;
        public static GameObject AnchorInstanceSystemPrefab;
        public static GameObject LocalOriginPrefab;
        [MenuItem("GameObject/Twin Align/Anchor Instance", false, -1)]
        public static void CreateNewAnchor()
        {
            AnchorInstancePrefab = Resources.Load("Prefabs/Anchor/AnchorInstance") as GameObject;
            GameObject anchorInstance = Instantiate(AnchorInstancePrefab);
        }

        [MenuItem("GameObject/Twin Align/Anchor System", false, -1)]
        public static void CreateNewAnchorSystem()
        {
            AnchorInstanceSystemPrefab = Resources.Load("Prefabs/Anchor/AnchorInstanceSystem") as GameObject;
            GameObject anchorInstance = Instantiate(AnchorInstanceSystemPrefab);
        }

        [MenuItem("GameObject/Twin Align/Local Origin", false, -1)]
        public static void CreateNewLocalOrigin()
        {
            LocalOriginPrefab = Resources.Load("Prefabs/Anchor/LocalOrigin") as GameObject;
            GameObject anchorInstance = Instantiate(LocalOriginPrefab);
        }
    }
}
#endif