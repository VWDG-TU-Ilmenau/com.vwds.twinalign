#if UNITY_ANDROID || UNITY_IOS

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using System.Threading.Tasks;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using TMPro;

namespace vwds.TwinAlign
{
    public class SpatialAnchorsCreator : MonoBehaviour
    {
        public SpatialAnchorManager CloudManager;
        public GameObject AnchoredObjectPrefab;
        public TextMeshProUGUI StatusText;
        public string BaseSharingUrl;
        public Button PlaceAnchorButton;
        private ARTrackedImageManager aRTrackedImageManager;
        private CreateAnchorState currentCreateAnchorState;
        private bool isAnchorCreationStarted;
        protected CloudSpatialAnchor currentCloudAnchor;
        protected Vector3 anchorPos;
        protected Quaternion anchorRot;

        [SerializeField]
        protected GameObject spawnedObject = null;

        [Header("UI References")]
        public GameObject AnchorDataInputObject;
        public TMP_InputField AnchorNameInput;
        public TMP_InputField AnchorDescriptionInput;

#if !UNITY_EDITOR
        public AnchorExchanger anchorExchanger = new AnchorExchanger();
#endif
        // Start is called before the first frame update
        private void Awake()
        {
            aRTrackedImageManager = FindObjectOfType<ARTrackedImageManager>(true);
        }

        private void OnEnable()
        {
            aRTrackedImageManager.trackedImagesChanged += OnImageChanged;
        }

        private void OnDisable()
        {
            aRTrackedImageManager.trackedImagesChanged -= OnImageChanged;
        }

        public void OnImageChanged(ARTrackedImagesChangedEventArgs args)
        {

            foreach (var trackedImage in args.added)
            {
                ARTrackedImage arTrackedImage = trackedImage.GetComponent<ARTrackedImage>();

                if (arTrackedImage.referenceImage.name == "AnchorMarker")
                {
                    anchorPos = arTrackedImage.gameObject.transform.position;
                    anchorRot = arTrackedImage.gameObject.transform.rotation;
                    PlaceAnchorButton.interactable = true;
                    ShowAnchorDataUI(true);
                    //InitializeCreateFlow();
                }
            }

            foreach (var trackedImage in args.updated)
            {
                if (isAnchorCreationStarted)
                    return;
                    
                ARTrackedImage arTrackedImage = trackedImage.GetComponent<ARTrackedImage>();

                if (arTrackedImage.referenceImage.name == "AnchorMarker")
                {
                    anchorPos = arTrackedImage.gameObject.transform.position;
                    anchorRot = arTrackedImage.gameObject.transform.rotation;
                    UpdateLocalAnchorPosition();
                }
            }
        }

        private void UpdateLocalAnchorPosition()
        {
            if (spawnedObject == null)
                return;

            spawnedObject.transform.position = anchorPos;
            spawnedObject.transform.rotation = anchorRot;
        }
        void Start()
        {

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


            currentCreateAnchorState = CreateAnchorState.CreateSession;

#if !UNITY_EDITOR
            anchorExchanger.WatchKeys(BaseSharingUrl);
#endif
            //Debug.Log("Manager Name: " + manager.name);
        }

        public virtual bool SanityCheckAccessConfiguration()
        {
            if (string.IsNullOrWhiteSpace(CloudManager.SpatialAnchorsAccountId)
                || string.IsNullOrWhiteSpace(CloudManager.SpatialAnchorsAccountKey)
                || string.IsNullOrWhiteSpace(CloudManager.SpatialAnchorsAccountDomain))
            {
                return false;
            }

            return true;
        }

        public async Task InitializeCreateFlowAsync()
        {
            await CreateAnchorFlow();
        }

        /// <summary>
        /// This version only exists for Unity to wire up a button click to.
        /// If calling from code, please use the Async version above.
        /// </summary>
        public async void InitializeCreateFlow()
        {
            try
            {
                await InitializeCreateFlowAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in {nameof(InitializeCreateFlow)}: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawns a new anchored object.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        /// <returns><see cref="GameObject"/>.</returns>
        protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
            // Create the prefab
            spawnedObject = GameObject.Instantiate(AnchoredObjectPrefab, worldPos, worldRot);
            Quaternion rotation = Quaternion.AngleAxis(0, Vector3.up);

            spawnedObject.AddComponent<CloudNativeAnchor>();

            isAnchorCreationStarted = true;

            CloudNativeAnchor cloudNativeAnchor = spawnedObject.GetComponent<CloudNativeAnchor>();
            if (cloudNativeAnchor.CloudAnchor == null)
                cloudNativeAnchor.NativeToCloud();

            return spawnedObject;
        }

        private async Task CreateAnchorFlow()
        {
            Debug.Log(currentCreateAnchorState);

            switch (currentCreateAnchorState)
            {
                case CreateAnchorState.CreateSession:
                    currentCloudAnchor = null;
                    currentCreateAnchorState = CreateAnchorState.ConfigSession;
                    InitializeCreateFlow();
                    break;
                case CreateAnchorState.ConfigSession:
                    currentCreateAnchorState = CreateAnchorState.StartSession;
                    InitializeCreateFlow();
                    break;
                case CreateAnchorState.StartSession:
                    await CloudManager.StartSessionAsync();
                    SpawnNewAnchoredObject(anchorPos, anchorRot);
                    currentCreateAnchorState = CreateAnchorState.CreateLocalAnchor;
                    InitializeCreateFlow();
                    break;
                case CreateAnchorState.CreateLocalAnchor:
                    if (spawnedObject != null)
                    {
                        currentCreateAnchorState = CreateAnchorState.SaveCloudAnchor;
                        InitializeCreateFlow();
                    }
                    break;
                case CreateAnchorState.SaveCloudAnchor:
                    await SaveCurrentObjectAnchorToCloudAsync();
                    currentCreateAnchorState = CreateAnchorState.StopSession;
                    InitializeCreateFlow();
                    break;
                case CreateAnchorState.StopSession:
                    CloudManager.StopSession();
                    CleanupSpawnedObjects();
                    await CloudManager.ResetSessionAsync();
                    currentCreateAnchorState = CreateAnchorState.Complete;
                    InitializeCreateFlow();
                    break;
                case CreateAnchorState.Complete:
                    currentCloudAnchor = null;
                    ShowAnchorDataUI(false);
                    CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + currentCreateAnchorState);
                    break;
            }

            StatusText.text = "Status: " + currentCreateAnchorState;
        }

        /// <summary>
        /// Saves the current object anchor to the cloud.
        /// </summary>
        protected virtual async Task SaveCurrentObjectAnchorToCloudAsync()
        {
            // Get the cloud-native anchor behavior
            CloudNativeAnchor cna = spawnedObject.GetComponent<CloudNativeAnchor>();

            // If the cloud portion of the anchor hasn't been created yet, create it
            if (cna.CloudAnchor == null)
            {
                await cna.NativeToCloud();
            }

            // Get the cloud portion of the anchor
            CloudSpatialAnchor cloudAnchor = cna.CloudAnchor;
            cloudAnchor.AppProperties[@"anchor-name"] = AnchorNameInput.text;
            cloudAnchor.AppProperties[@"anchor-description"] = AnchorDescriptionInput.text;
            // In this sample app we delete the cloud anchor explicitly, but here we show how to set an anchor to expire automatically
            cloudAnchor.Expiration = DateTimeOffset.Now.AddDays(1);

            while (!CloudManager.IsReadyForCreate)
            {
                await Task.Delay(330);
                float createProgress = CloudManager.SessionStatus.RecommendedForCreateProgress;
                StatusText.text = $"Move your device to capture more environment data: {createProgress:0%}";
            }

            bool success = false;

            //feedbackBox.text = "Saving...";

            try
            {
                // Actually save
                await CloudManager.CreateAnchorAsync(cloudAnchor);
                // Store
                currentCloudAnchor = cloudAnchor;

                Debug.Log($"Created a cloud anchor with ID={currentCloudAnchor.Identifier}");

                // Success?
                success = currentCloudAnchor != null;

                if (success)
                {
                    // Await override, which may perform additional tasks
                    // such as storing the key in the AnchorExchanger
                    await OnSaveCloudAnchorSuccessfulAsync();
                }
                else
                {
                    OnSaveCloudAnchorFailed(new Exception("Failed to save, but no exception was thrown."));
                }
            }
            catch (Exception ex)
            {
                OnSaveCloudAnchorFailed(ex);
            }
        }


        /// <summary>
        /// Called when a cloud anchor is saved successfully.
        /// </summary>
        protected async Task OnSaveCloudAnchorSuccessfulAsync()
        {
            // To be overridden.
            long anchorNumber = -1;

#if !UNITY_EDITOR
            anchorNumber = (await anchorExchanger.StoreAnchorKey(currentCloudAnchor.Identifier));
#endif

            Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetPose();
#endif

            currentCreateAnchorState = CreateAnchorState.StopSession;

            Debug.Log($"Created anchor {anchorNumber}. Next: Stop cloud anchor session");
        }

        /// <summary>
        /// Cleans up spawned objects.
        /// </summary>
        protected virtual void CleanupSpawnedObjects()
        {
            if (spawnedObject != null)
            {
                Destroy(spawnedObject);
                spawnedObject = null;
            }
        }

        /// <summary>
        /// Called when a cloud anchor is not saved successfully.
        /// </summary>
        /// <param name="exception">The exception.</param>
        protected virtual void OnSaveCloudAnchorFailed(Exception exception)
        {
            // we will block the next step to show the exception message in the UI.
            Debug.LogException(exception);
            Debug.Log("Failed to save anchor " + exception.ToString());

            //UnityDispatcher.InvokeOnAppThread(() => this.feedbackBox.text = string.Format("Error: {0}", exception.ToString()));
        }

        public void ShowAnchorDataUI(bool isActive)
        {
            AnchorDataInputObject.SetActive(isActive);
        }

        public enum CreateAnchorState
        {
            CreateSession,
            ConfigSession,
            StartSession,
            CreateLocalAnchor,
            SaveCloudAnchor,
            StopSession,
            Complete
        }
    }

}

#endif