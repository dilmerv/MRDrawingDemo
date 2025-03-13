using System.Collections.Generic;
using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;

public class ColorMarker : NetworkBehaviour
{
    [SerializeField] private Transform canvas;
    [SerializeField] private float brushRayDistance = 0.1f;
    [SerializeField] private LayerMask layersToInclude;
    [SerializeField] private Grabbable objectGrabbable;
    [SerializeField] private AudioClip drawingAudioClip;
    [SerializeField] private float drawingInterval = 0.025f;
    [SerializeField] private float drawingPointMinDistance = 0.005f;
    [SerializeField] private int cornerAndEndCaps = 10;
    [SerializeField] private Color color = Color.red;
    [SerializeField] private float startLineWidth = 0.01f;
    [SerializeField] private float endLineWidth = 0.02f;
    
    private AudioSource drawingAudioSource;
    private LineRenderer lastLine;
    private readonly List<Vector3> linePositions = new ();
    private float lastDrawingTime;
    private bool drawing;
    
    private NetworkList<Vector3> networkLinePositions;
    
    private void Awake()
    {
        networkLinePositions = new NetworkList<Vector3>();
    }
    
    private void OnValidate()
    {
        var grabbable = GetComponentInParent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = GetComponentInChildren<Grabbable>();
        }
        objectGrabbable = grabbable;
        var drawingCanvas = GameObject.Find("DrawingCanvas").transform;
        canvas = drawingCanvas?.transform;
        layersToInclude = LayerMask.GetMask("DrawingCanvas");
        drawingAudioClip = Resources.Load<AudioClip>("Audio/MarkerDrawing");
    }

    void Start()
    {
        drawingAudioSource = gameObject.AddComponent<AudioSource>();
        drawingAudioSource.loop = true;
        drawingAudioSource.clip = drawingAudioClip;
    }
    
    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * brushRayDistance, Color.green);
        
        if (objectGrabbable.SelectingPointsCount > 0 && 
            Physics.Raycast(ray, out RaycastHit hit, brushRayDistance, layersToInclude))
        {
            Vector3 hitPoint = hit.point;

            var drawPointPosition = canvas.InverseTransformPoint(hitPoint);
            if (Time.time - lastDrawingTime > drawingInterval)
            {
                if (!drawing)
                {
                    drawing = true;
                    drawingAudioSource.Play();
                    
                    if (IsOwner)
                        StartNewLineServerRpc(drawPointPosition, hit.normal);
                }
                else
                {
                    if (IsOwner)
                        AddPointToLineServerRpc(drawPointPosition);
                }
                
                lastDrawingTime = Time.time;
            }
        }
        else
        {
            drawing = false;
            if (drawingAudioSource.isPlaying) drawingAudioSource.Stop();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void StartNewLineServerRpc(Vector3 startPosition, Vector3 normal)
    {
        StartNewLine(startPosition, normal);
        
        networkLinePositions.Clear();
        networkLinePositions.Add(startPosition);
    
        // Notify all clients
        StartNewLineClientRpc(startPosition, normal);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void AddPointToLineServerRpc(Vector3 point)
    {
        if (lastLine != null && (Vector3.Distance(linePositions[^1], point) > drawingPointMinDistance))
        {
            linePositions.Add(point);
            lastLine.positionCount = linePositions.Count;
            lastLine.SetPosition(linePositions.Count - 1, point);
            networkLinePositions.Add(point);
            
            // Sync new point with clients
            AddPointToLineClientRpc(point);
        }
    }

    [ClientRpc]
    private void StartNewLineClientRpc(Vector3 startPosition, Vector3 normal)
    {
        StartNewLine(startPosition, normal);
    }
    
    [ClientRpc]
    private void AddPointToLineClientRpc(Vector3 point)
    {
        linePositions.Add(point);
        lastLine.positionCount = linePositions.Count;
        lastLine.SetPosition(linePositions.Count - 1, point);
    }

    private void StartNewLine(Vector3 startPosition, Vector3 normal)
    {
        GameObject lineObj = new GameObject("Line");
        lastLine = lineObj.AddComponent<LineRenderer>();
        lastLine.material = new Material(Shader.Find("Sprites/Default"));
        lastLine.startColor = color;
        lastLine.endColor = color;
        lastLine.startWidth = startLineWidth;
        lastLine.endWidth = endLineWidth;
        lastLine.alignment = LineAlignment.TransformZ;
        lastLine.positionCount = 1;
        
        // Smooths corners and caps
        lastLine.numCornerVertices = cornerAndEndCaps;  
        lastLine.numCapVertices = cornerAndEndCaps;
        lastLine.useWorldSpace = false;
        lineObj.transform.parent = canvas.transform;
        
        // offset it a bit, so it doesn't go through the canvas
        lineObj.transform.localPosition = new Vector3(0, 0, -0.002f);
        lineObj.transform.rotation = Quaternion.LookRotation(-normal);
        lastLine.sortingOrder = DrawingToolsManager.Instance.GetNewSortingLayer();
        
        linePositions.Clear();
        linePositions.Add(startPosition);
        lastLine.SetPosition(0, startPosition);
    }
}
