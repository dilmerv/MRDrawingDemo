using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction;
using UnityEngine;

public class DrawingToolsManager : SingletonNetwork<DrawingToolsManager>
{
    [SerializeField] private ColorMarker[] markers;
    [SerializeField] private CameraSnapshotTool cameraSnapshotTool;
    
    private readonly List<Grabbable> grabComponents = new();
    private int sortingLayerValue;
    
    private void Start()
    {
        foreach (var marker in markers)
        {
            grabComponents.Add(marker.GetComponent<Grabbable>());
        }
        grabComponents.Add(cameraSnapshotTool.GetComponent<Grabbable>());
    }

    public bool IsAnyToolSelected()
    {
        return grabComponents.Any(g => g.SelectingPointsCount > 0);
    }

    public int GetNewSortingLayer()
    {
        return ++sortingLayerValue;
    }
}
