/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.XR.ImmersiveDebugger;
using Meta.XR.Simulator.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Demo
{
    private const string HasSentConsentEventKey = "OVRTelemetry.HasSentConsentEvent";
    private const string TelemetryEnabledKey = "OVRTelemetry.TelemetryEnabled";
    private const string EmptyScenePath = "Assets/Scenes/Empty.unity";
    private const string FinalScenePath = "Assets/Scenes/Paintball.unity";

    static Demo()
    {
        EditorPrefs.SetBool(HasSentConsentEventKey, true);
        EditorPrefs.SetBool(TelemetryEnabledKey, true);
    }

    [MenuItem("Meta/Demo/Reset to Start", false, 3000)]
    private static void ResetToStart()
    {
        BreakThings();
        LoadAndResetEmptyScene();
        ResetSimulator();
        ResetImmersiveDebugger();
    }

    [MenuItem("Meta/Demo/Reset to End", false, 3001)]
    private static void ResetToEnd()
    {
        FixThings();
        LoadPaintballScene();
        ResetSimulator();
        ResetImmersiveDebugger();
    }

    private static void BreakThings()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
        QualitySettings.pixelLightCount = 10;
        PlayerSettings.graphicsJobs = true;
    }

    private static void FixThings()
    {
        PlayerSettings.graphicsJobs = false;
        QualitySettings.pixelLightCount = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
    }

    private static void LoadAndResetEmptyScene()
    {
        EditorSceneManager.OpenScene(EmptyScenePath, OpenSceneMode.Single);

        // Get active scene
        var activeScene = SceneManager.GetActiveScene();

        // Iterate through all root game objects in the scene and destroy them
        foreach (var obj in activeScene.GetRootGameObjects())
        {
            if (obj.name != "Main Camera" && obj.name != "Directional Light")
            {
                Object.DestroyImmediate(obj);
            }
        }

        // Optionally, save the scene after clearing it
        EditorSceneManager.SaveScene(activeScene);
    }

    private static void LoadPaintballScene()
    {
        EditorSceneManager.OpenScene(FinalScenePath, OpenSceneMode.Single);
    }

    private static void ResetSimulator()
    {
        Enabler.DeactivateSimulator(true);
        EditorPrefs.SetString("com.meta.xr.simulator.   ", "LivingRoom");
        EditorPrefs.SetBool("com.meta.xr.simulator.automaticservers_key", true);
    }

    private static void ResetImmersiveDebugger()
    {
        //TODO fix this
        //RuntimeSettings.Instance.ImmersiveDebuggerEnabled = false;
    }
}
