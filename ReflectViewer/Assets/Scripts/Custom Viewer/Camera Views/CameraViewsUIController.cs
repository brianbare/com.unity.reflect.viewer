using System.Collections.Generic;
using System.Linq;
using SharpFlux;
using TMPro;
using Unity.TouchFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Reflect;
using UnityEngine.Reflect.Pipeline;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Reflect.Viewer.Pipeline;

namespace Unity.Reflect.Viewer.UI
{
    /// <summary>
    /// Jump to various camera positions in the model driven by BIM parameter and exported Revit 3D view
    /// </summary>
    [RequireComponent(typeof(DialogWindow))]
    public class CameraViewsUIController : MonoBehaviour, IFilterMetadata
    {
#pragma warning disable CS0649
        [SerializeField, Tooltip("Camera to move")]
        Camera cameraToMove;
        [SerializeField, Tooltip("Dialog button")]
        Button m_DialogButton;
        //[SerializeField]
        //DialogButtonManager m_DialogButtonManager;
        [SerializeField, Tooltip("Reference to the button prefab.")]
        CameraViewListItem cameraViewListItemPrefab;
        [SerializeField, Tooltip("Reference to the list content parent.")]
        Transform m_ParentTransform;
        [SerializeField, Tooltip("Reference to the camera list is empty text field.")]
        TextMeshProUGUI noDataText;
        [SerializeField, Tooltip("Reference to the camera height adjustment slider")]
        NumericInputFieldPropertyControl heightAdjustment;
        [Tooltip("Parameter name to search for in Metadata component.\nIf this parameter is not empty then it will added to the lookup.")]
        [SerializeField] string parameterName = "Camera Location";
#pragma warning restore CS0649

        DialogWindow m_DialogWindow;
        Image m_DialogButtonImage;

        Stack<CameraViewListItem> cameraViewListItemPool = new Stack<CameraViewListItem>();
        List<CameraViewListItem> activeCameraViewListItem = new List<CameraViewListItem>();
        List<CameraViewsInfo> currentCameraViewNames = new List<CameraViewsInfo>();
        Project project;
        Camera m_CameraToMove;
        int cameraHeight;

        //InstructionUI? m_CurrentInstructionUI;
        bool m_ToolbarsEnabled;
        NavigationMode? m_CurrentNavigationMode;
        XRRig xrRig;

        void Awake()
        {
            UIStateManager.stateChanged += OnStateDataChanged;
            UIStateManager.projectStateChanged += OnProjectStateDataChanged;

            m_DialogButtonImage = m_DialogButton.GetComponent<Image>();
            m_DialogWindow = GetComponent<DialogWindow>();
        }

        void Start()
        {
            m_DialogButton.onClick.AddListener(OnDialogButtonClicked);
            heightAdjustment.onIntValueChanged.AddListener(OnSliderControlChange);
        }

        void OnEnable()
        {
            MetadataManager.Instance.Attach(this, parameterName, MetadataManager.Instance.AnyValue);
            MetadataManager.Instance.Attach(this, "Category", "Views");
        }

        void OnDisable()
        {
            MetadataManager.Instance.Detach(this, parameterName);
            MetadataManager.Instance.Detach(this, "Category");
        }

        void CreateFilterListItem(CameraViewsInfo cameraViewItemInfo)
        {
            CameraViewListItem cameraViewListItem;
            if (cameraViewListItemPool.Count > 0)
            {
                cameraViewListItem = cameraViewListItemPool.Pop();
            }
            else
            {
                cameraViewListItem = Instantiate(cameraViewListItemPrefab, m_ParentTransform);
                cameraViewListItem.listItemClicked += OnListItemClicked;
            }

            cameraViewListItem.InitItem(cameraViewItemInfo.nameText, cameraViewItemInfo.location);
            cameraViewListItem.gameObject.SetActive(true);
            cameraViewListItem.transform.SetAsLastSibling();
            activeCameraViewListItem.Add(cameraViewListItem);
        }

        void ClearFilterList()
        {
            foreach (var cameraViewListItem in activeCameraViewListItem)
            {
                cameraViewListItem.gameObject.SetActive(false);
                cameraViewListItemPool.Push(cameraViewListItem);
            }
            activeCameraViewListItem.Clear();
        }

        void OnListItemClicked(string nameKey, Transform location, CameraViewListItem item)
        {
            // Turn off any other camera item highlight
            if (activeCameraViewListItem != null)
            {
                foreach (var _item in activeCameraViewListItem)
                {
                    if (_item != item)
                        _item.TurnOff();
                    else
                        _item.TurnOn();
                }
            }

            SelectToolState();
            SelectCameraToMove();

            var cameraViewInfo = new CameraViewsInfo
            {
                nameText = nameKey,
                location = location,
                distance = UIStateManager.current.stateData.cameraViewsData.heightAdjustment,
                cameraToMove = UIStateManager.current.stateData.cameraViewsData.cameraToMove
            };
            UIStateManager.current.Dispatcher.Dispatch(Payload<ActionTypes>.From(ActionTypes.SelectCameraView, cameraViewInfo));
            // Move the camera
            MoveCamera(cameraViewInfo);
        }

        void SelectToolState()
        {
            ToolState toolState = UIStateManager.current.stateData.toolState;
            toolState.activeTool = ToolType.OrbitTool;
            toolState.orbitType = OrbitType.WorldOrbit;
            UIStateManager.current.Dispatcher.Dispatch(Payload<ActionTypes>.From(ActionTypes.SetToolState, toolState));
        }

        void SelectCameraToMove()
        {
            var data = UIStateManager.current.stateData.cameraViewsData;

            if (m_CurrentNavigationMode == NavigationMode.VR)
            {
                if (xrRig == null)
                    xrRig = FindObjectOfType<XRRig>();

                if (xrRig != null)
                {
                    m_CameraToMove = xrRig.cameraGameObject.GetComponent<Camera>();
                }
                else
                {
                    // Default camera if cannot find it
                    m_CameraToMove = cameraToMove != null ? cameraToMove : Camera.main;
                }
            }
            else
            {
                m_CameraToMove = cameraToMove != null ? cameraToMove : Camera.main;
            }

            data.heightAdjustment = cameraHeight;
            data.cameraToMove = m_CameraToMove;
            UIStateManager.current.Dispatcher.Dispatch(Payload<ActionTypes>.From(ActionTypes.SetCameraInfo, data));
        }

        void OnStateDataChanged(UIStateData data)
        {
            m_DialogButtonImage.enabled = data.activeDialog == DialogType.CameraViews;
            //m_DialogButtonManager.UpdateButton(data);

            if (m_CurrentNavigationMode != data.navigationState.navigationMode)
            {
                m_CurrentNavigationMode = data.navigationState.navigationMode;

                // Initialize our camera height value to 0 as change modes
                if (heightAdjustment != null)
                    heightAdjustment.SetValue(0);
                cameraHeight = 0;
            }
        }

        void OnProjectStateDataChanged(UIProjectStateData data)
        {
            if (data.activeProject != project)
            {
                // New project
                project = data.activeProject;
                data.cameraViewsInfos.Clear();
                data.currentCameraViewInfo = default;
            }

            if (!CompareCameraList(data.cameraViewsInfos, currentCameraViewNames))
            {
                ClearFilterList();
                if (data.cameraViewsInfos.Count == 0)
                {
                    // show no data
                    noDataText.gameObject.SetActive(true);
                }
                else
                {
                    noDataText.gameObject.SetActive(false);
                }
                foreach (var cameraViewsInfo in data.cameraViewsInfos)
                {
                    CreateFilterListItem(cameraViewsInfo);
                }
                currentCameraViewNames = new List<CameraViewsInfo>(data.cameraViewsInfos);
            }
        }

        // Compare if camera lists are not equal by looking at the names in the list
        bool CompareCameraList(List<CameraViewsInfo> a, List<CameraViewsInfo> b)
        {
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].nameText != b[i].nameText)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Move the camera since a new camera view has been selected.
        /// </summary>
        /// <param name="cameraViewInfo">New camera info</param>
        void MoveCamera(CameraViewsInfo cameraViewInfo)
        {
            Vector3 movePosition;
            Quaternion moveRotation;

            if (cameraViewInfo.location.GetComponent<Renderer>() != null)
            {
                // First try to get bounds in world space preferably
                movePosition = cameraViewInfo.location.GetComponent<Renderer>().bounds.center + new Vector3(0, cameraViewInfo.location.GetComponent<Renderer>().bounds.extents.y, 0);
            }
            else if (cameraViewInfo.location.GetComponent<MeshFilter>() != null)
            {
                // Next try to get bounds in local space
                movePosition = cameraViewInfo.location.GetComponent<MeshFilter>().mesh.bounds.center + new Vector3(0, cameraViewInfo.location.GetComponent<MeshFilter>().mesh.bounds.extents.y, 0);
            }
            else
            {
                // Just use the transform
                movePosition = cameraViewInfo.location.position;
            }

            // Move up designated camera height from menu
            movePosition += Vector3.up * (cameraViewInfo.distance / 100f);
            // Get desired rotation
            moveRotation = cameraViewInfo.location.rotation;

            // Check which camera we are using
            if (cameraViewInfo.cameraToMove != null)
            {
                var freeFly = cameraViewInfo.cameraToMove.transform.GetComponent<FreeFlyCamera>();
                if (freeFly != null)
                {
                    // Need to make sure the max pitch angle is 360 so we can rotate to proper rotations
                    var freeFlySettings = freeFly.settings;
                    if (freeFlySettings != null)
                        freeFlySettings.maxPitchAngle = 360;
                    // Set camera position
                    Vector3 offsetPos = movePosition - cameraViewInfo.cameraToMove.transform.position;
                    freeFly.MovePosition(offsetPos, LookAtConstraint.Follow);
                    // Set camera rotation
                    Vector3 offsetRot = (moveRotation * Quaternion.Inverse(cameraViewInfo.cameraToMove.transform.rotation)).eulerAngles;
                    freeFly.Rotate(offsetRot);
                    // Give enough space in pitch angle to be able to look all around after moving to proper rotation
                    if (freeFlySettings != null)
                        freeFlySettings.maxPitchAngle = 540;
                }
                else
                {
                    var xrRig = cameraViewInfo.cameraToMove.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.XRRig>();
                    if (xrRig != null)
                    {
                        Vector3 heightAdjustment = xrRig.rig.transform.up * xrRig.cameraInRigSpaceHeight;
                        movePosition += heightAdjustment;
                        xrRig.MoveCameraToWorldLocation(movePosition);
                        var rotation = moveRotation.eulerAngles.y - xrRig.transform.eulerAngles.y;
                        xrRig.RotateAroundCameraUsingRigUp(rotation);
                    }
                    else
                        cameraViewInfo.cameraToMove.transform.position = movePosition;
                }
            }
        }

        void OnDialogButtonClicked()
        {
            // Turn off any camera item highlight
            if (activeCameraViewListItem != null)
            {
                foreach (var item in activeCameraViewListItem)
                    item.TurnOff();
            }

            var dialogType = m_DialogWindow.open ? DialogType.None : DialogType.CameraViews;
            UIStateManager.current.Dispatcher.Dispatch(Payload<ActionTypes>.From(ActionTypes.OpenDialog, dialogType));
        }

        void OnSliderControlChange(int value)
        {
            var data = UIStateManager.current.stateData.cameraViewsData;
            data.heightAdjustment = value;
            // Cache value in case of state change (e.g. into VR mode)
            cameraHeight = value;
            UIStateManager.current.Dispatcher.Dispatch(Payload<ActionTypes>.From(ActionTypes.SetCameraInfo, data));
        }

        #region IFilterMetadata implementation
        /// <summary>
        /// Metadata filter search is beginning
        /// </summary>
        public void NotifyBeforeSearch()
        { }

        /// <summary>
        /// Metadata filter search is occurring and results are being sent here
        /// </summary>
        /// <param name="reflectObject">GameObject on which the metadata search match is found</param>
        /// <param name="updateType">If this object has been added, updated or removed</param>
        /// <param name="searchParamter">The parameter used for this metadata filter search.
        /// Use this to sort your meatdata matches if this controller class attaches more than one search filter.</param>
        /// <param name="result">The value of the matching parameter found.</param>
        public void NotifyObservers(GameObject reflectObject, StreamEvent updateType, string searchParamter, string result = null)
        {
            if (searchParamter == parameterName && reflectObject != null && !string.IsNullOrEmpty(result))
            {
                var cameraViewInfo = new CameraViewsInfo
                {
                    nameText = result,
                    location = reflectObject.transform,
                    updateType = updateType
                };
                UIStateManager.current.Dispatcher.Dispatch(Payload<ActionTypes>.From(ActionTypes.PrepareCameraView, cameraViewInfo));
            }

            if (searchParamter == "Category" && reflectObject != null && result == "Views")
            {
                string viewLabel = null;
                Metadata metadata = reflectObject.GetComponent<Metadata>();
                if (metadata != null)
                {
                    viewLabel = metadata.GetParameter("View Name");
                }

                if (string.IsNullOrEmpty(viewLabel))
                {
                    POI poi = reflectObject.GetComponent<POI>();
                    if (poi != null)
                    {
                        viewLabel = poi.label;
                    }
                }

                if (!string.IsNullOrEmpty(viewLabel))
                {
                    var cameraViewInfo = new CameraViewsInfo
                    {
                        nameText = viewLabel,
                        location = reflectObject.transform,
                        updateType = updateType
                    };
                    UIStateManager.current.Dispatcher.Dispatch(Payload<ActionTypes>.From(ActionTypes.PrepareCameraView, cameraViewInfo));
                }
            }
        }

        /// <summary>
        /// Metadata filter search is ending
        /// </summary>
        public void NotifyAfterSearch()
        {
            Debug.Log("Finished searching for camera locations................");
        }
        #endregion
    }
}
