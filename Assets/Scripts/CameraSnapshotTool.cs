using PassthroughCameraSamples;
using PassthroughCameraSamples.CameraToWorld;
using UnityEngine;

public class CameraSnapshotTool : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager passthroughCameraManager;
    [SerializeField] private CameraToWorldCameraCanvas cameraCanvas;
    private bool snapshotTaken;
    private bool isPassthroughCameraReady;
    
    void Start()
    {
        PassthroughCameraPermissions.OnPermissionSuccess += CallbackPermissionSuccess;
    }
    
    private void CallbackPermissionSuccess()
    {
        isPassthroughCameraReady = true;
    }
    
    private void OnValidate()
    {
        if(passthroughCameraManager == null)
            passthroughCameraManager = FindAnyObjectByType<WebCamTextureManager>();
        if(cameraCanvas == null)
            cameraCanvas = FindAnyObjectByType<CameraToWorldCameraCanvas>();
    }
    
    void Update()
    {
        if (!isPassthroughCameraReady) return;
        
        if ((OVRInput.GetActiveController() & OVRInput.Controller.Touch) != 0)
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                snapshotTaken = !snapshotTaken;
                if (snapshotTaken)
                {
                    // Asking the canvas to make a snapshot before stopping WebCamTexture
                    cameraCanvas.MakeCameraSnapshot();
                    passthroughCameraManager.WebCamTexture.Stop();
                }
                else
                {
                    passthroughCameraManager.WebCamTexture.Play();
                    cameraCanvas.ResumeStreamingFromCamera();
                }
            }
        }
    }
}
