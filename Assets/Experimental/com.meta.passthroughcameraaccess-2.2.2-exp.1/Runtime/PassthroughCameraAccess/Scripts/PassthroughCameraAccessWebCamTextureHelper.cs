// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Meta.XR.PassthroughCamera
{
    using DH = PassthroughCameraAccessDebugHelper;
    using PH = PassthroughCameraAccessPermissionHelper;

    public class PassthroughCameraAccessWebCamTextureHelper : MonoBehaviour
    {
        [Header("Passhtrough Camera Manager ref")]
        [SerializeField] private PassthroughCameraManager _passthroughCameraManager;

        [Header("Debug Configuration")]
        [SerializeField] private DH.DebuglevelEnum debugLevel = DH.DebuglevelEnum.ALL;

        private Texture2D _texture2D;
        private bool _webCamTextureReady = false;

        public bool IsWebCamTextureReady => _webCamTextureReady;

        #region Unity functions
        void Start()
        {
            // Attach a callback when the WebCamTexture is ready to use.
            _passthroughCameraManager.WebCamTextureReady.AddListener(CallbackWebCamTextureReady);

            // Set debug level
            DH.debugLevel = debugLevel;

            if (_passthroughCameraManager == null)
            {
                DH.DebugMessage(DH.DebugTypeEnum.ERROR, $"PCA: {nameof(_passthroughCameraManager)} field is not specified in the component {nameof(PassthroughCameraAccessWebCamTextureHelper)}");
                enabled = false;
                return;
            }
            // Request camera permission
#if UNITY_ANDROID
            StartCoroutine(PH.AskCameraPermission());
#endif
        }

        void OnDestroy()
        {
            // Remove callback
            _passthroughCameraManager.WebCamTextureReady.RemoveListener(CallbackWebCamTextureReady);
            // Disable access to WebCamTexture
            _webCamTextureReady = false;
            // Stop any coroutine that is running
            StopAllCoroutines();
        }
        #endregion

        #region PassthroughManager functions
        /// <summary>
        /// Activate WebCamTexture helper class when the PassthoughManager created the WebCamtexture Object.
        /// </summary>
        public void CallbackWebCamTextureReady(WebCamTexture webCamTexture)
        {
            // Create a new texture2D object to store CPU WebCamTexture data
            _texture2D = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            _webCamTextureReady = true;
            DH.DebugMessage(DH.DebugTypeEnum.LOG, $"PCA: WebCamTexture Object created.");
        }

        /// <summary>
        /// Try to get WebCamTexture CPU data and the current timestamp.
        /// </summary>
        public bool TryGetCpuWebCamTextureData(out Texture2D texture, out double timestamp)
        {
            if (_webCamTextureReady)
            {
                if (_passthroughCameraManager.IsCameraPlaying)
                {
                    if (_passthroughCameraManager.WebCamTexture.didUpdateThisFrame)
                    {
                        timestamp = OVRPlugin.GetTimeInSeconds();
                        _texture2D.SetPixels32(_passthroughCameraManager.WebCamTexture.GetPixels32());
                        _texture2D.Apply();

                        texture = _texture2D;

                        return true;
                    }
                }
            }

            // Return false if WebCamTexture is not ready or not playing or not updated
            timestamp = 0;
            texture = null;
            return false;
        }

        /// <summary>
        /// Try to get WebCamTexture GPU data and the current timestamp.
        /// </summary>
        public bool TryGetGpuWebCamTextureData(out WebCamTexture webCamTex, out double timestamp)
        {
            if (!_webCamTextureReady)
            {
                timestamp = 0;
                webCamTex = null;
                return false;
            }

            timestamp = OVRPlugin.GetTimeInSeconds();
            webCamTex = _passthroughCameraManager.WebCamTexture;
            return true;
        }
        #endregion
    }
}
