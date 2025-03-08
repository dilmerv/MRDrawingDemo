// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Meta.XR.PassthroughCamera
{
    public class PassthroughCameraManager : MonoBehaviour
    {
#if UNITY_ANDROID
        private const string HORIZONOS_CAMERA_PERMISSION = "horizonos.permission.HEADSET_CAMERA";
#endif

        private static List<string> _requiredPermissions;
        private static List<string> RequiredPermissions => _requiredPermissions ??= ListRequiredPermissions();

        private readonly Dictionary<string, string> deviceNameToCameraIdMap = new();
        private readonly Dictionary<string, string> cameraIdToDeviceNameMap = new();

        public PassthroughCameraEye eye = PassthroughCameraEye.Left;
        [SerializeField] private Vector2Int _requestedResolution;

        private bool _hasPermission;
        private bool _isWebCamTextureReady;
        private WebCamTexture _webcamTexture;

        public bool IsSupported => PassthroughCameraUtils.IsSupported;

        public bool IsPermissionGranted => _hasPermission;

        public bool IsWebCamTextureReady => _isWebCamTextureReady;

        public bool IsCameraPlaying => _webcamTexture != null && _webcamTexture.isPlaying;

        public Vector2Int RequestedResolution
        {
            get => _requestedResolution;
            set
            {
                if (_webcamTexture != null)
                    throw new ApplicationException(
                        "PCA: Cannot change the WebCamTexture resolution once the initialization started");

                _requestedResolution = value;
            }
        }

        public Vector2Int CurrentResolution => new(_webcamTexture.width, _webcamTexture.height);

        /// <summary>
        /// Occurs when the corresponding WebCamTexture object has initialized and is playing
        /// </summary>
        public UnityEvent<WebCamTexture> WebCamTextureReady;

        public WebCamTexture WebCamTexture => _webcamTexture;

        private WebCamTexture CreateWebCamTexture(PassthroughCameraEye cameraEye, WebCamTextureCreateParams createParams)
        {
            string cameraId = PassthroughCameraUtils.GetCameraIdByEye(cameraEye);

            if (!cameraIdToDeviceNameMap.TryGetValue(cameraId, out var deviceName))
                throw new ApplicationException($"PCA: Cannot find device name for camera Id {cameraId}");

            Debug.Log($"PCA: Attempt to create WebCamTexture with params " +
                    $"({deviceName}, {createParams.requestedWidth}, {createParams.requestedHeight})");
            return new WebCamTexture(deviceName, createParams.requestedWidth, createParams.requestedHeight);
        }

        #region Unity events

        private void Awake()
        {
            Debug.Log($"{nameof(PassthroughCameraManager)}.{nameof(Awake)}() was called");
            Assert.AreEqual(1, FindObjectsByType<PassthroughCameraManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                $"PCA: Passthrough Camera: more than one {nameof(PassthroughCameraManager)} component. Only one instance is allowed at a time. Current instance: {name}");
        }

        void OnDestroy()
        {
            Debug.Log($"PCA: {nameof(PassthroughCameraManager)}.{nameof(OnDestroy)}() was called");
            StopAndDisposeAll();
        }

        private void OnEnable()
        {
            Debug.Log($"PCA: {nameof(OnEnable)}() was called");
            if (!IsSupported)
            {
                Debug.Log("PCA: Passthrough Camera functionality is not supported by the current device." +
                          $" Disabling {nameof(PassthroughCameraManager)} object");
                enabled = false;
                return;
            }

            _hasPermission = HasAllPermissionsGranted();
            if (!_hasPermission)
            {
                Debug.LogError(
                    $"PCA: Passthrough Camera requires permission(s) {string.Join(" and ", RequiredPermissions)}. Waiting for them to be granted...");
                return;
            }

            Debug.Log($"PCA: All permissions have been granted");
            StartCoroutine(DelayedCameraInitializationAndPlay());
        }

        private void OnDisable()
        {
            Debug.Log($"PCA: {nameof(OnDisable)}() was called");
            if (_webcamTexture != null && _webcamTexture.isPlaying)
            {
                _webcamTexture.Stop();
            }
        }

        private void Update()
        {
            if (!_hasPermission)
            {
                if (!HasAllPermissionsGranted())
                    return;

                _hasPermission = true;
                StartCoroutine(DelayedCameraInitializationAndPlay());
            }
        }

        private void OnApplicationQuit()
        {
            Debug.Log($"PCA: {nameof(OnApplicationQuit)}() was called");
            StopAndDisposeAll();
        }

        #endregion Unity events

        private IEnumerator DelayedCameraInitializationAndPlay()
        {
            Debug.Log($"PCA: Attempt to initialize the camera");
            PopulateCameraIdDictionaries();

            yield return null;
            InitializeCamera();
            Debug.Log($"PCA: Camera initialized");
            yield return null;
            _webcamTexture.Play();
            Debug.Log($"PCA: Camera started successfully");

            // Fix for Unity build from macOS:
            //   WebCamTexture object requires stop and play it again went you build the app from macOS.
            //   Seems like Unity on macOS is doing something different compared to Unity under Windows OS. (Not confirmed yet)
            yield return null;
            _webcamTexture.Stop();
            yield return null;
            _webcamTexture.Play();
            // --------------------------------

            yield return null;
            yield return null;

            _isWebCamTextureReady = true;
            WebCamTextureReady?.Invoke(_webcamTexture);
        }

        private void InitializeCamera()
        {
            if (WebCamTexture.devices.Length <= 0)
            {
                Debug.LogError(
                    $"Passthrough Camera devices are not found among WebCamTexture.devices");
                return;
            }

            _webcamTexture = CreateWebCamTexture(eye,
                new WebCamTextureCreateParams
                { requestedWidth = _requestedResolution.x, requestedHeight = _requestedResolution.y });
        }

        private static List<string> ListRequiredPermissions()
        {
            List<string> permissions = new();

#if UNITY_ANDROID
            // Unity's WebCamTexture when running on Android always requires the standard CAMERA permission
            permissions.Add(Permission.Camera);

            if (PassthroughCameraUtils.HorizonOSVersion > PassthroughCameraUtils.EarlySupportOsVersion)
            {
                // Starting from v74, Passthrough Camera API requires a separate HEADSET_CAMERA permission
                permissions.Add(HORIZONOS_CAMERA_PERMISSION);
            }
#endif

            return permissions;
        }

        private static bool HasAllPermissionsGranted()
        {
            return RequiredPermissions.All(Permission.HasUserAuthorizedPermission);
        }

        private void PopulateCameraIdDictionaries()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.Log($"PCA: WebCamTexture.devices is empty. Check permissions required to access passthrough camera data");
                return;
            }

            Debug.Log($"PCA: WebCamTexture.devices are {string.Join(", ", WebCamTexture.devices.Select(x => x.name))}");
            Assert.IsTrue(WebCamTexture.devices.Length >= 2);

            // Currently hardcoded map between deviceName and cameraId: the first device corresponds to the Left eye,
            // the second - to the right eye.
            if (PassthroughCameraUtils.HorizonOSVersion == PassthroughCameraUtils.EarlySupportOsVersion)
            {
                // In the early version of PCA the devices in WebCamTexture are listed from right to left.
                // Also, there WebCamTexture.devices contains 4 items instead of 2, and the one with index 2 represents the left eye.
                deviceNameToCameraIdMap[WebCamTexture.devices[0].name] = PassthroughCameraUtils.GetCameraIdByEye(PassthroughCameraEye.Right);
                deviceNameToCameraIdMap[WebCamTexture.devices[2].name] = PassthroughCameraUtils.GetCameraIdByEye(PassthroughCameraEye.Left);
            }
            else
            {
                deviceNameToCameraIdMap[WebCamTexture.devices[0].name] = PassthroughCameraUtils.GetCameraIdByEye(PassthroughCameraEye.Left);
                deviceNameToCameraIdMap[WebCamTexture.devices[1].name] = PassthroughCameraUtils.GetCameraIdByEye(PassthroughCameraEye.Right);
            }

            foreach (var pair in deviceNameToCameraIdMap)
            {
                cameraIdToDeviceNameMap[pair.Value] = pair.Key;
            }
        }

        private void StopAndDisposeAll()
        {
            // Stop WebCamTexture Object when the application quits
            if (_webcamTexture != null && _webcamTexture.isPlaying)
            {
                _webcamTexture.Stop();
            }

            StopAllCoroutines();

            _webcamTexture = null;
        }

        public struct WebCamTextureCreateParams
        {
            public int requestedWidth;
            public int requestedHeight;
            public int requestedFPS; // Not in use at this moment
        }
    }
}
