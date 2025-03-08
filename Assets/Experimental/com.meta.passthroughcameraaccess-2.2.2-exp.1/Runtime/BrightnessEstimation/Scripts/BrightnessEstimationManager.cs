// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Add the Passthrough Camera Access namespace
using Meta.XR.PassthroughCamera;

public class BrightnessEstimationManager : MonoBehaviour
{
    // Create a field to attach the reference to the PassthoughCameraAccessWebCamTextureHelper prefab
    [SerializeField] private PassthroughCameraAccessWebCamTextureHelper _webCamTextureHelper;

    [SerializeField] private float refreshTime = 0.1f;
    [SerializeField][Range(1, 100)] private int bufferSize = 10;
    [SerializeField] private UnityEvent<float> onBrightnessChange;

    [SerializeField] private UnityEngine.UI.Text debugger;

    private float _refreshCurrentTime = 0.0f;
    private List<float> _brightnessVals = new List<float>();

    void Start()
    {
        // Attach a callback when the permission fails.
        PassthroughCameraAccessPermissionHelper.OnPermissionFails += CallbackPermissionFails;
        // Attach a callback when the permission is successful.
        PassthroughCameraAccessPermissionHelper.OnPermissionSuccess += CallbackPermissionSuccess;
    }

    void Update()
    {
        // Get the WebCamTexture CPU image
        bool hasWebCamtextureData = _webCamTextureHelper.TryGetCpuWebCamTextureData(out Texture2D texture, out double timestamp);
        // Process WebCamTexture data
        if (!IsWaiting() && hasWebCamtextureData)
        {
            var res = GetRoomAmbientLight(texture) + $"\nTimestamp: {timestamp}";
            Debug.Log(res);
            debugger.text = res;
            onBrightnessChange?.Invoke(GetGlobalBrigthnessLevel());
        }
    }

    void OnDisable()
    {
        // Remove all callback when the object is destroyed
        PassthroughCameraAccessPermissionHelper.OnPermissionFails -= CallbackPermissionFails;
        PassthroughCameraAccessPermissionHelper.OnPermissionSuccess -= CallbackPermissionSuccess;
    }

    /// <summary>
    /// Callback used to print the permission fail message in the debugText UI element.
    /// </summary>
    private void CallbackPermissionFails()
    {
        debugger.text = "No permission granted.";
    }

    /// <summary>
    /// Callback used to print the permission success message in the debugText UI element.
    /// </summary>
    private void CallbackPermissionSuccess()
    {
        debugger.text = "Permission granted.";
    }

    /// <summary>
    /// Estimate the Brightness Level using a Texture2D
    /// </summary>
    /// <param name="tmpTexture"></param>
    /// <returns>String data for debugging purposes</returns>
    private string GetRoomAmbientLight(Texture2D tmpTexture)
    {
        _refreshCurrentTime = refreshTime;
        if (tmpTexture)
        {
            if (tmpTexture.isReadable)
            {
                Color32[] data = tmpTexture.GetPixels32();
                float w = tmpTexture.width;
                float h = tmpTexture.height;

                float colorSum = 0;
                for (int x = 0, len = data.Length; x < len; x++)
                {
                    colorSum += (0.2126f * data[x].r) + (0.7152f * data[x].g) + (0.0722f * data[x].b);
                }
                float brightnessVals = Mathf.Floor(colorSum / (w * h));

                _brightnessVals.Add(brightnessVals);

                if (_brightnessVals.Count > bufferSize)
                {
                    _brightnessVals.RemoveAt(0);
                }

                return $"Current brigthnessLevel: {brightnessVals}\nGlobal value: {GetGlobalBrigthnessLevel()}";
            }
            else
            {
                return "Image not readeable";
            }
        }
        return "No image";
    }

    /// <summary>
    /// Return true if the waiting time is bigger than zero.
    /// </summary>
    /// <returns>True or False</returns>
    private bool IsWaiting()
    {
        _refreshCurrentTime -= Time.deltaTime;
        return (_refreshCurrentTime > 0.0f);
    }

    /// <summary>
    /// Get the average Brightness level based on the buffer size.
    /// </summary>
    /// <returns>Average brightness level (float)</returns>
    private float GetGlobalBrigthnessLevel()
    {
        if (_brightnessVals.Count == 0)
        {
            return -1;
        }

        var sum = 0.0f;
        foreach (float b in _brightnessVals)
        {
            sum += b;
        }
        return (sum / _brightnessVals.Count);
    }
}
