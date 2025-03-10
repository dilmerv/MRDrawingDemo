using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ColorMarkerStateWatcherStaging : MonoBehaviour
{
    [SerializeField] private Transform drawingBoardContainer;
    [SerializeField] private Transform originalAreaTransform;
    [SerializeField] private float minYPosition;
    [SerializeField] private float frequency;
    
    private Rigidbody markerPhysics;
    private Coroutine watcher;
    private Renderer markerRenderer;
    private bool onHold;
    private Quaternion initialOffset;
    
    void Start()
    {
        markerPhysics = GetComponent<Rigidbody>();
        watcher = StartCoroutine(WatchMarker());
        markerRenderer = GetComponent<Renderer>();
        initialOffset = Quaternion.Inverse(originalAreaTransform.rotation) * transform.rotation;
        
          
        DrawingBoardPanelPlacementStaging.Instance.onPanelGrabbedStarted.AddListener(() =>
        {
            ResetMarker(false);
            onHold = true;
        });
        
        DrawingBoardPanelPlacementStaging.Instance.onPanelGrabbedEnded.AddListener(() =>
        {
            ResetMarker(true);
            onHold = false;
        });
    }
    
    private void ResetMarker(bool visible)
    {
        markerRenderer.enabled = visible;
        if (visible)
        {
            markerPhysics.isKinematic = true;
            transform.SetPositionAndRotation(originalAreaTransform.position, originalAreaTransform.rotation * initialOffset);
            markerPhysics.isKinematic = false;
            markerPhysics.velocity = Vector3.zero;
            markerPhysics.angularVelocity = Vector3.zero;
        }
    }

    private IEnumerator WatchMarker()
    {
        while (true)
        {
            yield return new WaitForSeconds(frequency);
            if (transform.position.y <= minYPosition)
            {
                if(!onHold) 
                    ResetMarker(true);
            }
        }
    }
    
    private void OnDisable()
    {
        if (watcher != null)
        {
            StopCoroutine(watcher);
        }
    }
}
