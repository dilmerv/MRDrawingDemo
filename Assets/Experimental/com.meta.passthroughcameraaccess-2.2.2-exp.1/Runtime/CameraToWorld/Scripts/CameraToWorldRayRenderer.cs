// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

public class CameraToWorldRayRenderer : MonoBehaviour
{
    public GameObject middleSegment;

    public void RenderMiddleSegment(bool shouldRender)
    {
        middleSegment.SetActive(shouldRender);
    }
}
