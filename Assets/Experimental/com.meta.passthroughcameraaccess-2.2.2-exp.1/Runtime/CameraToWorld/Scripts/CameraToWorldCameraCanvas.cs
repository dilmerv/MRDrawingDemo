// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;
using UnityEngine.UI;
using Meta.XR.PassthroughCamera;

public class CameraToWorldCameraCanvas : MonoBehaviour
{
    [SerializeField] private PassthroughCameraManager _passthroughCameraManager;
    [SerializeField] private Text _debugText;
    [SerializeField] private RawImage _image;
    private Texture2D _cameraSnapshot;
    private WebCamTexture _webCamTexture;

    void Start()
    {
        PassthroughCameraAccessPermissionHelper.OnPermissionFails += OnPermissionFails;
        _passthroughCameraManager.WebCamTextureReady.AddListener(OnWebCamTextureReady);
    }

    void OnDestroy()
    {
        PassthroughCameraAccessPermissionHelper.OnPermissionFails -= OnPermissionFails;
        _passthroughCameraManager.WebCamTextureReady.RemoveListener(OnWebCamTextureReady);
    }

    public void MakeCameraSnapshot()
    {
        if (_webCamTexture == null)
            return;

        if (_cameraSnapshot == null)
        {
            _cameraSnapshot = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
        }

        // Copy the last available image from WebCamTexture to a separate object
        _cameraSnapshot.SetPixels32(_passthroughCameraManager.WebCamTexture.GetPixels32());
        _cameraSnapshot.Apply();

        _image.texture = _cameraSnapshot;
    }

    public void ResumeStreamingFromCamera()
    {
        _image.texture = _webCamTexture;
    }

    private void OnWebCamTextureReady(WebCamTexture webCamTexture)
    {
        _debugText.text = "WebCamTexture Object ready and playing.";
        _webCamTexture = webCamTexture;
        _image.texture = webCamTexture;
    }

    private void OnPermissionFails()
    {
        _debugText.text = "No permission granted.";
    }
}
