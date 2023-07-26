#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using System.Threading.Tasks;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

namespace vwds.twinalign
{
    public class SpatialAnchorsController : MonoBehaviour
    {
        public GameObject RoomPrefab;
        public GameObject OriginAnchor;
        public static SpatialAnchorsController Instance;
        [SerializeField]
        private SpatialAnchorManager manager;
        public string BaseSharingUrl;
        public Text StatusText;
        public GameObject CloudAnchorPlaceHolder;
        private CloudSpatialAnchorWatcher currentWatcher;
        protected AnchorLocateCriteria anchorLocateCriteria = null;
        private CloudSpatialAnchor currentCloudAnchor;
        private int anchorsLocated;
        private AppState currentAppState;
        private ARSessionOrigin aROrigin;
        private List<CloudSpatialAnchor> anchorsList;
        private GameObject roomInstance;
        private string currentDetectedAnchor;
        private bool isOriginSet;

#if !UNITY_EDITOR
        public AnchorExchanger anchorExchanger = new AnchorExchanger();
#endif
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

            OriginAnchor = FindObjectOfType<LocalOrigin>().gameObject;
        }
        // Start is called before the first frame update
        void Start()
        {
            anchorsList = new List<CloudSpatialAnchor>();

            if (!SanityCheckAccessConfiguration())
            {
                Debug.LogError($"{nameof(SpatialAnchorManager.SpatialAnchorsAccountId)}, {nameof(SpatialAnchorManager.SpatialAnchorsAccountKey)} and {nameof(SpatialAnchorManager.SpatialAnchorsAccountDomain)} must be set on {nameof(SpatialAnchorManager)}");
            }

            if (string.IsNullOrEmpty(BaseSharingUrl))
            {
                Debug.Log("Sharing URL is empty or null");
                return;
            }
            else
            {
                Uri result;
                if (!Uri.TryCreate(BaseSharingUrl, UriKind.Absolute, out result))
                {
                    Debug.Log($"{nameof(BaseSharingUrl)} is not a valid url");
                    return;
                }
                else
                {
                    BaseSharingUrl = $"{result.Scheme}://{result.Host}/api/anchors";
                }
            }

#if !UNITY_EDITOR
            anchorExchanger.WatchKeys(BaseSharingUrl);
#endif

            currentAppState = AppState.CreateSession;

            manager.SessionUpdated += SessionUpdated;
            manager.AnchorLocated += AnchorLocated;
            manager.LocateAnchorsCompleted += LocateAnchorsCompleted;

            anchorLocateCriteria = new AnchorLocateCriteria();
            anchorLocateCriteria.NearAnchor = new NearAnchorCriteria();
            anchorLocateCriteria.Strategy = LocateStrategy.VisualInformation;
            anchorLocateCriteria.BypassCache = true;

            aROrigin = FindObjectOfType<ARSessionOrigin>();

            //Delay initializing locating anchor
            Invoke(nameof(InitializeLocateFlow), 5f);
        }

        /// <summary>
        /// This version only exists for Unity to wire up a button click to.
        /// If calling from code, please use the Async version above.
        /// </summary>
        public async void InitializeLocateFlow()
        {
            try
            {
                if (currentAppState == AppState.CreateSession)
                {
                    string anchorKeyToFind = null;
                    List<string> anchorKeysToFind = new List<string>();
#if !UNITY_EDITOR
                    int idx = 0;
                    bool anchorFound = true;

                    while(anchorFound){

                        anchorKeyToFind = await anchorExchanger.RetrieveAnchorKey(idx);

                        if (anchorKeyToFind != null)
                        {
                            anchorKeysToFind.Add(anchorKeyToFind);
                        }
                        else
                        {
                            break;
                        }

                        idx++;
                    }
#endif
                    if (anchorKeysToFind.Count == 0)
                    {
                        Debug.Log("No Anchors Found!");
                        StatusText.text = "No Anchors Found!";
                    }
                    else
                    {
                        StatusText.text = "Cloud Anchors found" + anchorKeysToFind.Count;
                        anchorLocateCriteria.Identifiers = anchorKeysToFind.ToArray();
                    }
                }

                await AppStateFlow();

            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(SpatialAnchorsController)} - Error in {nameof(InitializeLocateFlow)}: {ex.Message}");
            }
        }
        //UpdatingSession
        public void SessionUpdated(object sender, SessionUpdatedEventArgs args)
        {

        }

        public void LocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            Debug.Log("Located All anchors!");
        }

        public void ResetSession()
        {
            // manager.ResetSessionAsync();
            manager.ResetSessionAsync();
            currentAppState = AppState.CreateSession;
            Invoke(nameof(InitializeLocateFlow), 0f);
        }

        //Locate Spatial Anchors
        public void AnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            switch (args.Status)
            {
                case LocateAnchorStatus.Located:
                    if (currentDetectedAnchor == args.Anchor.AppProperties[@"anchor-name"])
                        return;

                    StatusText.text = "Status : " + args.Anchor.AppProperties[@"anchor-name"] + "Anchor Found";
                    currentDetectedAnchor = args.Anchor.AppProperties[@"anchor-name"];
                    anchorsList.Add(args.Anchor);
                    AdjustWorldPose(args.Anchor);
                    // Go add your anchor to the scene...
                    break;
                case LocateAnchorStatus.AlreadyTracked:
                    // This anchor has already been reported and is being tracked
                    break;
                case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                    // The anchor was deleted or never existed in the first place
                    // Drop it, or show UI to ask user to anchor the content anew
                    break;
                case LocateAnchorStatus.NotLocated:
                    // The anchor hasn't been found given the location data
                    // The user might in the wrong location, or maybe more data will help
                    // Show UI to tell user to keep looking around
                    break;
            }
        }

        private async Task AppStateFlow()
        {
            switch (currentAppState)
            {
                case AppState.CreateSession:
                    currentCloudAnchor = null;
                    currentAppState = AppState.ConfigSession;
                    InitializeLocateFlow();
                    break;
                case AppState.ConfigSession:
                    anchorsLocated = 0;
                    ConfigureSession();
                    currentAppState = AppState.StartSession;
                    InitializeLocateFlow();
                    break;
                case AppState.StartSession:
                    await manager.StartSessionAsync();
                    currentAppState = AppState.LookForAnchor;
                    InitializeLocateFlow();
                    break;
                case AppState.LookForAnchor:
                    currentAppState = AppState.LookingForAnchor;
                    currentWatcher = CreateWatcher();
                    InitializeLocateFlow();
                    break;
                case AppState.LookingForAnchor:
                    break;
            }
            StatusText.text = "Status :" + currentAppState.ToString();
        }

        private void ConfigureSession()
        {
            if (currentAppState == AppState.ConfigSession)
            {
                //anchorsToFind.Add(_anchorKeyToFind);
            }
            {
                // anchorsExpected = anchorsToFind.Count;
                // SetAnchorIdsToLocate(anchorsToFind);
            }
        }
        public void AdjustWorldPose(CloudSpatialAnchor anchor)
        {
            Debug.Log("Found Anchor " + anchor.AppProperties[@"anchor-name"]);

            AnchorInstance anchorInstance = AnchorInstanceSystem.Instance.GetAnchorInstance(anchor.AppProperties[@"anchor-name"]);
            anchorInstance.SetAnchorName(anchor.AppProperties[@"anchor-name"]);

            Quaternion offsetRotation = Quaternion.Euler(0f, -Vector3.SignedAngle(anchor.GetPose().forward, Vector3.forward, Vector3.up), 0f);
            Vector3 angles = new Vector3(0f, anchor.GetPose().rotation.eulerAngles.y, 0f);
            if (!isOriginSet)
            {
                aROrigin.MakeContentAppearAt(OriginAnchor.transform, anchor.GetPose().position, Quaternion.Euler(new Vector3(0f,
                anchor.GetPose().rotation.eulerAngles.y,
                0f)));

                isOriginSet = true;
            }
            else
            {
                aROrigin.MakeContentAppearAt(OriginAnchor.transform, anchor.GetPose().position);
            }

            Vector3 offsetPosition = new Vector3(-anchorInstance.transform.position.x, 0f, -anchorInstance.transform.position.z);

            aROrigin.MakeContentAppearAt(OriginAnchor.transform, offsetPosition);

        }
        public virtual bool SanityCheckAccessConfiguration()
        {
            if (string.IsNullOrWhiteSpace(manager.SpatialAnchorsAccountId)
                || string.IsNullOrWhiteSpace(manager.SpatialAnchorsAccountKey)
                || string.IsNullOrWhiteSpace(manager.SpatialAnchorsAccountDomain))
            {
                return false;
            }

            return true;
        }

        public async void DeleteDetectedAnchor()
        {
            foreach (var anchor in anchorsList)
            {
                await manager.DeleteAnchorAsync(anchor);
            }
        }

        private void Update()
        {
           // Debug.Log("AROrigin Rotation:" + aROrigin.transform.rotation.x + "," + aROrigin.transform.rotation.y + "," + aROrigin.transform.rotation.z);
        }

        protected CloudSpatialAnchorWatcher CreateWatcher()
        {
            if ((manager != null) && (manager.Session != null))
            {

                return manager.Session.CreateWatcher(anchorLocateCriteria);
            }
            else
            {
                return null;
            }
        }

        enum AppState
        {
            CreateSession = 0,
            ConfigSession,
            StartSession,
            LookForAnchor,
            LookingForAnchor,
            AnchorLocated
        }
    }
}

#endif