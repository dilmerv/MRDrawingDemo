using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.XR;
using UnityEngine;
using UnityEngine.Events;

public class DrawingBoardPanelPlacement : SingletonNetwork<DrawingBoardPanelPlacement>
{
    [SerializeField] private EnvironmentRaycastManager raycastManager;
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private Transform raycastAnchor;
    [SerializeField] private OVRInput.RawButton grabButton = OVRInput.RawButton.RIndexTrigger | OVRInput.RawButton.RHandTrigger;
    [SerializeField] private OVRInput.RawAxis2D moveAxis = OVRInput.RawAxis2D.RThumbstick;
    [SerializeField] private OVRInput.RawButton selectionToggleButton = OVRInput.RawButton.B;
    [SerializeField] private Transform panel;
    [SerializeField] private GameObject panelGlow;
    [SerializeField] private LineRenderer raycastVisualizationLine;
    [SerializeField] private Transform raycastVisualizationNormal;
    
    public UnityEvent onPanelGrabbedStarted = new ();
    public UnityEvent onPanelGrabbedEnded = new ();
    
    private readonly RollingAverage rollingAverageFilter = new ();
    private Pose? targetPose;
    private Vector3 positionVelocity;
    private float rotationVelocity;
    private bool isGrabbing;
    private float distanceFromController;
    private Pose? environmentPose;
    private EnvironmentRaycastHitStatus currentEnvHitStatus;
    private OVRSpatialAnchor spatialAnchor;
    private bool toggleSelectionActive;

    private IEnumerator Start()
    {
        // Wait until headset starts tracking
        enabled = false;
        while (!OVRPlugin.userPresent || !OVRManager.isHmdPresent)
        {
            yield return null;
        }
        yield return null;
        enabled = true;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsHost)
        {
            // Create the OVRSpatialAnchor and make it a parent of the panel.
            // This will prevent the panel front drifting after headset lock/unlock.
            spatialAnchor = new GameObject(nameof(OVRSpatialAnchor)).AddComponent<OVRSpatialAnchor>();
            spatialAnchor.transform.SetPositionAndRotation(panel.position, panel.rotation);
            panel.SetParent(spatialAnchor.transform);
        }
    }
    
    private void Update()
    {
        if(!IsPanelSelectionAllowed())
        {
            raycastVisualizationLine.enabled = false;
            raycastVisualizationNormal.gameObject.SetActive(false);
            return;
        }
            
        if (!Application.isFocused)
        {
            isGrabbing = false;
            targetPose = null;
            return;
        }

        VisualizeRaycast();
        
        if (isGrabbing)
        {
            UpdateTargetPose();
            if (OVRInput.GetUp(grabButton))
            {
                panelGlow.SetActive(false);
                isGrabbing = false;
                environmentPose = null;

                // If the existing OVRSpatialAnchor if further than 3 meters away from the current panel position, delete it and create a new one:
                // https://developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-best-practices#tips-for-using-spatial-anchors
                if (panel.localPosition.magnitude > 3f)
                {
                    spatialAnchor.EraseAnchorAsync();
                    DestroyImmediate(spatialAnchor);

                    var parent = panel.parent;
                    panel.SetParent(null);
                    parent.SetPositionAndRotation(panel.position, panel.rotation);
                    spatialAnchor = parent.gameObject.AddComponent<OVRSpatialAnchor>();
                    panel.SetParent(parent);
                }
                
                onPanelGrabbedEnded?.Invoke();
            }
        }
        else
        {
            // Detect grab gesture and update grab indicator
            bool didHitPanel = Physics.Raycast(GetRaycastRay(), out var hit) && hit.transform == panel;
            panelGlow.SetActive(didHitPanel);
            if (didHitPanel && OVRInput.GetDown(grabButton))
            {
                isGrabbing = true;
                distanceFromController = Vector3.Distance(raycastAnchor.position, panel.position);
                onPanelGrabbedStarted?.Invoke();
            }
        }
        AnimatePanelPose();
    }

    private bool IsPanelSelectionAllowed()
    {
        if (OVRInput.GetDown(selectionToggleButton))
        {
            toggleSelectionActive = !toggleSelectionActive;
        }
        
        bool isUsingHands = (OVRInput.GetActiveController() & OVRInput.Controller.Hands) != 0;
        return !(DrawingToolsManager.Instance && DrawingToolsManager.Instance.IsAnyToolSelected()
               || isUsingHands
               || toggleSelectionActive);
    }

    private Ray GetRaycastRay()
    {
        return new Ray(raycastAnchor.position + raycastAnchor.forward * 0.1f, raycastAnchor.forward);
    }

    private void UpdateTargetPose()
    {
        // Animate manual placement position with right thumbstick
        const float moveSpeed = 2.5f;
        distanceFromController += OVRInput.Get(moveAxis).y * moveSpeed * Time.deltaTime;
        distanceFromController = Mathf.Clamp(distanceFromController, 0.3f, float.MaxValue);

        // Try place the panel onto environment
        var newEnvPose = TryGetEnvironmentPose();
        if (newEnvPose.HasValue)
        {
            environmentPose = newEnvPose.Value;
        }
        else if (currentEnvHitStatus == EnvironmentRaycastHitStatus.HitPointOutsideOfCameraFrustum)
        {
            environmentPose = null;
        }
        var manualPlacementPosition = raycastAnchor.position + raycastAnchor.forward * distanceFromController;
        var panelForward = Vector3.ProjectOnPlane(manualPlacementPosition - centerEyeAnchor.position, Vector3.up).normalized;
        var manualPlacementPose = new Pose(manualPlacementPosition, Quaternion.LookRotation(panelForward));
        // If environment pose is available and the panel is closer to it than to the user, place the panel onto environment to create a magnetism effect
        bool chooseEnvPose = environmentPose.HasValue && Vector3.Distance(manualPlacementPose.position, environmentPose.Value.position) / Vector3.Distance(manualPlacementPose.position, centerEyeAnchor.position) < 0.5;
        targetPose = chooseEnvPose ? environmentPose.Value : manualPlacementPose;
    }

    private Pose? TryGetEnvironmentPose()
    {
        var ray = GetRaycastRay();
        if (!raycastManager.Raycast(ray, out var hit) || hit.normalConfidence < 0.5f)
        {
            return null;
        }
        bool isCeiling = Vector3.Dot(hit.normal, Vector3.down) > 0.7f;
        if (isCeiling)
        {
            return null;
        }
        const float sizeTolerance = 0.2f;
        var panelSize = new Vector3(panel.localScale.x, panel.localScale.y, 0f) * (1f - sizeTolerance);
        bool isVerticalSurface = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.3f;
        if (isVerticalSurface)
        {
            // If the surface is vertical, stick the panel to the surface
            if (raycastManager.PlaceBox(ray, panelSize, Vector3.up, out var result))
            {
                // Apply the rolling average filter to smooth the normal
                var smoothedNormal = rollingAverageFilter.UpdateRollingAverage(result.normal);
                return new Pose(result.point, Quaternion.LookRotation(-smoothedNormal, Vector3.up));
            }
        }
        else
        {
            // Position the panel upright and check collisions with environment
            var position = hit.point + Vector3.up * panel.localScale.y * 0.5f;
            var halfExtents = panelSize * 0.5f;
            var forward = Vector3.ProjectOnPlane(position - centerEyeAnchor.position, Vector3.up).normalized;
            var orientation = Quaternion.LookRotation(forward, Vector3.up);
            const float collisionCheckOffset = 0.1f;
            if (!raycastManager.CheckBox(position + Vector3.up * collisionCheckOffset, halfExtents, orientation))
            {
                return new Pose(position, orientation);
            }
        }
        return null;
    }

    private void AnimatePanelPose()
    {
        if (!targetPose.HasValue)
        {
            return;
        }

        const float smoothTime = 0.13f;
        panel.position = Vector3.SmoothDamp(panel.position, targetPose.Value.position, ref positionVelocity, smoothTime);

        float angle = Quaternion.Angle(panel.rotation, targetPose.Value.rotation);
        if (angle > 0f)
        {
            float dampedAngle = Mathf.SmoothDampAngle(angle, 0f, ref rotationVelocity, smoothTime);
            float t = 1f - dampedAngle / angle;
            panel.rotation = Quaternion.SlerpUnclamped(panel.rotation, targetPose.Value.rotation, t);
        }
    }

    private void VisualizeRaycast()
    {
        var ray = GetRaycastRay();
        bool hasHit = RaycastPanelOrEnvironment(ray, out var hit) || hit.status == EnvironmentRaycastHitStatus.HitPointOccluded;
        bool hasNormal = hit.normalConfidence > 0f;
        raycastVisualizationLine.enabled = hasHit;
        raycastVisualizationNormal.gameObject.SetActive(hasHit && hasNormal);
        if (hasHit)
        {
            raycastVisualizationLine.SetPosition(0, ray.origin);
            raycastVisualizationLine.SetPosition(1, hit.point);

            if (hasNormal)
            {
                raycastVisualizationNormal.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
    }

    private bool RaycastPanelOrEnvironment(Ray ray, out EnvironmentRaycastHit envHit)
    {
        if (Physics.Raycast(ray, out var physicsHit) && physicsHit.transform == panel)
        {
            envHit = new EnvironmentRaycastHit
            {
                status = EnvironmentRaycastHitStatus.Hit,
                point = physicsHit.point,
                normal = physicsHit.normal,
                normalConfidence = 1f
            };
            return true;
        }
        bool envHitResult = raycastManager.Raycast(ray, out envHit);
        currentEnvHitStatus = envHit.status;
        return envHitResult;
    }

    private class RollingAverage
    {
        private List<Vector3> _normals;
        private int _currentRollingAverageIndex;

        public Vector3 UpdateRollingAverage(Vector3 current)
        {
            if (_normals == null)
            {
                const int filterSize = 10;
                _normals = Enumerable.Repeat(current, filterSize).ToList();
            }
            _currentRollingAverageIndex++;
            _normals[_currentRollingAverageIndex % _normals.Count] = current;
            Vector3 result = default;
            foreach (var normal in _normals)
            {
                result += normal;
            }
            return result.normalized;
        }
    }
}
