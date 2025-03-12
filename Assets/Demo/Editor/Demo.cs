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

using System.IO;
using System.Xml.Linq;
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
    private const string FinalScenePath = "Assets/Scenes/MRDrawingDemo.unity";
    private const string AndroidManifestPath = "Plugins/Android/AndroidManifest.xml";
    private static readonly string[] PermissionsToAppend = { "horizonos.permission.HEADSET_CAMERA"  };

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
        ClearLogConsole();
    }

    [MenuItem("Meta/Demo/Reset to End", false, 3001)]
    private static void ResetToEnd()
    {
        FixThings();
        LoadDemoScene();
        ResetSimulator();
        ResetImmersiveDebugger();
        ClearLogConsole();

    }

    private static void BreakThings()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
        QualitySettings.pixelLightCount = 10;
        PlayerSettings.graphicsJobs = true;
        DeleteAndroidManifest();
    }

    private static void FixThings()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        QualitySettings.pixelLightCount = 1;
        PlayerSettings.graphicsJobs = false;
        GenerateAndroidManifest();
    }

    
    private static void DeleteAndroidManifest()
    {
        // clear out android manifest to trigger the rule
        string androidManifestFile = Path.Combine(Application.dataPath, AndroidManifestPath);
        if (File.Exists(androidManifestFile))
            File.Delete(androidManifestFile);
        if (File.Exists($"{androidManifestFile}.meta"))
            File.Delete($"{androidManifestFile}.meta");
        
        // force unity to refresh assets to reflect deleting of file
        AssetDatabase.Refresh();
    }
    
    private static void GenerateAndroidManifest()
    {
        // re-generate android manifest & let the user know
        OVRManifestPreprocessor.GenerateManifestForSubmission();
        var androidManifestFile = Path.Combine(Application.dataPath, AndroidManifestPath);
        XDocument xmlDoc = XDocument.Load(androidManifestFile);
        XElement manifestElement = xmlDoc.Root;

        foreach (var newPermission in PermissionsToAppend)
        {
            XElement newPermissionElement = new XElement("uses-permission",
                new XAttribute(XNamespace.Get("http://schemas.android.com/apk/res/android") + "name", newPermission)
            );
            manifestElement?.Add(newPermissionElement);
        }
        xmlDoc.Save(androidManifestFile);
        // force unity to refresh assets to reflect new permission(s)
        AssetDatabase.Refresh();
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

    private static void LoadDemoScene()
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
        RuntimeSettings instance = ScriptableObject.CreateInstance<RuntimeSettings>();
        string assetPath = "Assets/Resources/ImmersiveDebuggerSettings.asset";
        AssetDatabase.CreateAsset(instance, assetPath);
        AssetDatabase.SaveAssets();
    }
    
    private static void ClearLogConsole()
    {
        System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        System.Type type = assembly.GetType("UnityEditor.LogEntries");
        System.Reflection.MethodInfo method = type.GetMethod("Clear");
        if (method != null) method.Invoke(new object(), null);
    }
}
