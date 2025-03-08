using Meta.XR.PassthroughCamera;
using UnityEngine;

public class CameraSnapshotTool : MonoBehaviour
{
    [SerializeField] private PassthroughCameraManager passthroughCameraManager;
    [SerializeField] private CameraToWorldCameraCanvas cameraCanvas;
    private bool snapshotTaken;
    private bool isPassthroughCameraReady;
    
    void Start()
    {
        PassthroughCameraAccessPermissionHelper.OnPermissionSuccess += CallbackPermissionSuccess;
    }
    
    private void CallbackPermissionSuccess()
    {
        isPassthroughCameraReady = true;
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
