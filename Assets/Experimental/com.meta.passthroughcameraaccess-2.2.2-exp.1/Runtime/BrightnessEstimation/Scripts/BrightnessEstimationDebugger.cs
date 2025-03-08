// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BrightnessEstimationDebugger : MonoBehaviour
{
    [SerializeField] private Text debugger;
    [SerializeField] private UnityEvent onTooDark;
    [SerializeField] private UnityEvent onTooLight;

    [Range(0, 100)][SerializeField] private float minBrightnessLevel = 10;
    [Range(0, 100)][SerializeField] private float maxBrightnessLevel = 50;

    private int _isDark = 2;
    private string _brightnessStatus = "";

    public void OnChangeBrightness(float value)
    {
        if (debugger)
        {
            debugger.text = $"Brightness level: {value} \n\n {_brightnessStatus}";
        }

        if (value <= minBrightnessLevel && _isDark != 2)
        {
            onTooDark?.Invoke();
            _isDark = 2;
        }
        else if (value >= maxBrightnessLevel && _isDark != 1)
        {
            onTooLight?.Invoke();
            _isDark = 1;
        }
    }

    public void TooDark()
    {
        _brightnessStatus = "IS TOO DARK, TURN LIGHTS ON!";
    }

    public void TooLight()
    {
        _brightnessStatus = "TOO BRIGHT, TURN LIGHTS OFF!";
    }
}
