// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using UnityEngine;
using Meta.XR.PassthroughCamera;
using UnityEngine.UI;

public class CameraToWorldManager : MonoBehaviour
{
    [SerializeField] private PassthroughCameraManager _passthroughCameraManager;
    private PassthroughCameraEye _cameraEye;
    private Vector2Int _cameraResolution;

    public GameObject centerEyeAnchor;
    public GameObject headMarker;
    public GameObject cameraMarker;
    public GameObject rayMarker;

    public CameraToWorldCameraCanvas cameraCanvas;
    public float canvasDistance = 1f;

    public Vector3 headSpaceDebugShift = new(0, -.15f, .4f);
    private GameObject rayGo1, rayGo2, rayGo3, rayGo4;

    private bool _isEverythingReady;
    private bool isDebugOn;
    private bool snapshotTaken;
    private OVRPose snapshotHeadPose;

    void Start()
    {
        if (_passthroughCameraManager == null)
        {
            Debug.LogError($"PCA: {nameof(_passthroughCameraManager)} field is required "
                + $"for the component {nameof(CameraToWorldManager)} to operate properly");
            enabled = false;
            return;
        }

        // Disable the manager here and enable it only when sure the required permissions have been granted
        _passthroughCameraManager.enabled = false;
        PassthroughCameraAccessPermissionHelper.OnPermissionSuccess += CallbackPermissionSuccess;
    }

    private void CallbackPermissionSuccess()
    {
        _cameraEye = _passthroughCameraManager.eye;
        _cameraResolution = PassthroughCameraUtils.GetCameraIntrinsics(_cameraEye).Resolution;
        _passthroughCameraManager.RequestedResolution = _cameraResolution;
        _passthroughCameraManager.enabled = true;

        ScaleCameraCanvas();

        rayGo1 = rayMarker;
        rayGo2 = Instantiate(rayMarker);
        rayGo3 = Instantiate(rayMarker);
        rayGo4 = Instantiate(rayMarker);
        UpdateRaysRendering();
        _isEverythingReady = true;
    }

    void Update()
    {
        if (!_isEverythingReady)
            return;

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            snapshotTaken = !snapshotTaken;
            if (snapshotTaken)
            {
                // Asking the canvas to make a snapshot before stopping WebCamTexture
                cameraCanvas.MakeCameraSnapshot();
                _passthroughCameraManager.WebCamTexture.Stop();
                snapshotHeadPose = centerEyeAnchor.transform.ToOVRPose();
            }
            else
            {
                _passthroughCameraManager.WebCamTexture.Play();
                cameraCanvas.ResumeStreamingFromCamera();
                snapshotHeadPose = OVRPose.identity;
            }

            UpdateRaysRendering();
        }

        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            isDebugOn ^= true;
            Debug.Log($"PCA: SpatialSnapshotManager: DEBUG mode is {(isDebugOn ? "ON" : "OFF")}");
            UpdateRaysRendering();

            if (snapshotTaken)
            {
                // Enable of disable the debug translation of the markers
                TranslateMarkersForDebug(isDebugOn);
            }
        }

        if (!snapshotTaken)
        {
            UpdateMarkerPoses();

            if (isDebugOn)
            {
                // Move the updated markers forward to better see them
                TranslateMarkersForDebug(moveForward: true);
            }
        }

    }

    /// <summary>
    /// Calculate the dimensions of the canvas based on the distance from the camera origin and the camera resolution
    /// </summary>
    private void ScaleCameraCanvas()
    {
        RectTransform cameraCanvasRectTransform = cameraCanvas.GetComponentInChildren<RectTransform>();
        Ray leftSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(_cameraEye, new Vector2Int(0, _cameraResolution.y / 2));
        Ray rightSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(_cameraEye, new Vector2Int(_cameraResolution.x, _cameraResolution.y / 2));
        float horizontalFoVDegrees = Vector3.Angle(leftSidePointInCamera.direction, rightSidePointInCamera.direction);
        double horizontalFoVRadians = horizontalFoVDegrees / 180 * Math.PI;
        double newCanvasWidthInMeters = 2 * canvasDistance * Math.Tan(horizontalFoVRadians / 2);
        float localScale = (float)(newCanvasWidthInMeters / cameraCanvasRectTransform.sizeDelta.x);
        cameraCanvasRectTransform.localScale = new Vector3(localScale, localScale, localScale);
    }

    private void UpdateRaysRendering()
    {
        // Hide rays' middle segments and rendering only their tips
        // when rays' origins are too close to the headset. Otherwise, it looks ugly
        foreach (GameObject rayGo in new[] { rayGo1, rayGo2, rayGo3, rayGo4 })
        {
            rayGo.GetComponent<CameraToWorldRayRenderer>().RenderMiddleSegment(snapshotTaken || isDebugOn);
        }
    }

    private void UpdateMarkerPoses()
    {
        double xrTime = OVRPlugin.GetTimeInSeconds();

        OVRPose headPose = OVRPlugin.GetNodePoseStateAtTime(xrTime, OVRPlugin.Node.Head).Pose.ToOVRPose();
        headMarker.transform.position = headPose.position;
        headMarker.transform.rotation = headPose.orientation;

        OVRPose cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(xrTime, _cameraEye);
        cameraMarker.transform.position = cameraPose.position;
        cameraMarker.transform.rotation = cameraPose.orientation;

        // Position the canvas in front of the camera
        cameraCanvas.transform.position = cameraPose.position + cameraPose.orientation * Vector3.forward * canvasDistance;
        cameraCanvas.transform.rotation = cameraPose.orientation;

        // Position the rays pointing to 4 corners of the canvas / image
        var rays = new[]
        {
            new { rayGo = rayGo1, u = 0, v = 0 },
            new { rayGo = rayGo2, u = 0, v = _cameraResolution.y },
            new { rayGo = rayGo3, u = _cameraResolution.x, v = _cameraResolution.y },
            new { rayGo = rayGo4, u = _cameraResolution.x, v = 0 }
        };

        foreach (var item in rays)
        {
            Ray rayInWorld = PassthroughCameraUtils.ScreenPointToRayInWorld(xrTime, PassthroughCameraEye.Left, new Vector2Int(item.u, item.v));
            item.rayGo.transform.position = rayInWorld.origin;
            item.rayGo.transform.LookAt(rayInWorld.origin + rayInWorld.direction);

            float angleWithCameraForwardDegree =
                Vector3.Angle(item.rayGo.transform.forward, cameraPose.orientation * Vector3.forward);
            // The original size of the ray GameObject along z axis is 0.5f. Hardcoding it here for simplicity
            float zScale = (float)(canvasDistance / Math.Cos(angleWithCameraForwardDegree / 180 * Math.PI) / 0.5);
            item.rayGo.transform.localScale = new Vector3(item.rayGo.transform.localScale.x, item.rayGo.transform.localScale.y, zScale);

            Text label = item.rayGo.GetComponentInChildren<Text>();
            label.text = $"({item.u:F0}, {item.v:F0})";
        }
    }

    private void TranslateMarkersForDebug(bool moveForward)
    {
        var gameObjects = new[]
        {
            headMarker, cameraMarker, cameraCanvas.gameObject, rayGo1, rayGo2, rayGo3, rayGo4
        };

        Quaternion direction = snapshotTaken ? snapshotHeadPose.orientation : centerEyeAnchor.transform.rotation;

        foreach (var go in gameObjects)
        {
            go.transform.position += direction * headSpaceDebugShift * (moveForward ? 1 : -1);
        }
    }
}
