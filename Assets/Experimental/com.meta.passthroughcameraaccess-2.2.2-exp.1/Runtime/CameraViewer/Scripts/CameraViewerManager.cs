// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;
using UnityEngine.UI;

// Add the Passthrough Camera Access namespace
using Meta.XR.PassthroughCamera;

public class CameraViewerManager : MonoBehaviour
{
    // Create a field to attach the reference to the passthroughCameraManager prefab
    [SerializeField] private PassthroughCameraManager _passthroughCameraManager;
    [SerializeField] private Text debugText;
    [SerializeField] private RawImage _image;

    private string _permissionStatus = "";

    void Start()
    {
        // Attach a callback when the permission fails.
        PassthroughCameraAccessPermissionHelper.OnPermissionFails += CallbackPermissionFails;
        // Attach a callback when the permission is successful.
        PassthroughCameraAccessPermissionHelper.OnPermissionSuccess += CallbackPermissionSuccess;
        // Attach a callback when the WebCamTexture is ready.
        _passthroughCameraManager.WebCamTextureReady.AddListener(CallbackWebCamTextureReady);
    }

    void OnDestroy()
    {
        // Remove all callback when the object is destroyed
        PassthroughCameraAccessPermissionHelper.OnPermissionFails -= CallbackPermissionFails;
        PassthroughCameraAccessPermissionHelper.OnPermissionSuccess -= CallbackPermissionSuccess;
        _passthroughCameraManager.WebCamTextureReady.RemoveListener(CallbackWebCamTextureReady);
    }

    /// <summary>
    /// Callback used to  assign the GPU WebCamTexture texture to the RawImage Ui component.
    /// </summary>
    public void CallbackWebCamTextureReady(WebCamTexture webCamTexture)
    {
        debugText.text += "\nWebCamTexture Object ready and playing.";
        // Set WebCamTexture GPU texture to the RawImage Ui element
        _image.texture = webCamTexture;
    }

    /// <summary>
    /// Callback used to print the permission fail message in the debugText UI element.
    /// </summary>
    private void CallbackPermissionFails()
    {
        _permissionStatus = "No permission granted.";
        debugText.text = _permissionStatus;
    }

    /// <summary>
    /// Callback used to print the permission success message in the debugText UI element.
    /// </summary>
    private void CallbackPermissionSuccess()
    {
        _permissionStatus = "Permission granted.";
    }
}
