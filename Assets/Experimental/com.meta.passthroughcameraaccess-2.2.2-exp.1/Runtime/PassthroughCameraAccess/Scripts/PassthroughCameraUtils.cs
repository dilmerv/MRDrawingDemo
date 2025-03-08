// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.PassthroughCamera
{
    public static class PassthroughCameraUtils
    {
        // The Horizon OS starts supporting PCA with v72. However, the behaviour of the feature is slightly different
        // in this version compared to v74.
        public const int EarlySupportOsVersion = 72;

        // The only pixel format supported atm
        private const int YUV_420_888 = 0x00000023;

        private static AndroidJavaObject currentActivity;
        private static AndroidJavaObject cameraManager;
        private static bool initialized;
        private static bool? isSupported;
        private static int? horizonOsVersion;

        // Caches
        private static readonly Dictionary<PassthroughCameraEye, string> cameraEyeToCameraIdMap = new();
        private static readonly ConcurrentDictionary<PassthroughCameraEye, List<Vector2Int>> cameraOutputSizes = new();
        private static readonly ConcurrentDictionary<string, AndroidJavaObject> cameraCharacteristicsMap = new();

        /// <summary>
        /// Get the Horizon OS version number on the headset
        /// </summary>
        public static int HorizonOSVersion
        {
            get
            {
                if (!horizonOsVersion.HasValue)
                {
                    var vrosClass = new AndroidJavaClass("vros.os.VrosBuild");
                    horizonOsVersion = vrosClass.CallStatic<int>("getSdkVersion");

                    // 10000 is a special OS built on top of v72 and containing additional fixes to Passthrough Camera API.
                    // But this is still v72.
                    if (horizonOsVersion == 10000)
                    {
                        horizonOsVersion = 72;
                    }
                }

                return horizonOsVersion.Value;
            }
        }

        /// <summary>
        /// Returns true if the current headset supports Passthrough Camera API
        /// </summary>
        public static bool IsSupported
        {
            get
            {
                if (!isSupported.HasValue)
                {
                    var headset = OVRPlugin.GetSystemHeadsetType();
                    return (headset == OVRPlugin.SystemHeadset.Meta_Quest_3 ||
                            headset == OVRPlugin.SystemHeadset.Meta_Quest_3S) &&
                           HorizonOSVersion >= EarlySupportOsVersion;
                }

                return isSupported.Value;
            }
        }

        /// <summary>
        /// Provides a list of resolutions supported by the passthrough camera. Developers should use one of those
        /// when initializing the camera.
        /// </summary>
        /// <param name="cameraEye">The passthrough camera</param>
        public static List<Vector2Int> GetOutputSizes(PassthroughCameraEye cameraEye)
        {
            return cameraOutputSizes.GetOrAdd(cameraEye, _ => GetOutputSizesInternal(cameraEye));
        }

        /// <summary>
        /// Returns the camera intrinsics for a specified passthrough camera. All the intrinsics values are provided
        /// in pixels. The resolution value is the maximum resolution available for the camera.
        /// </summary>
        /// <param name="cameraEye">The passthrough camera</param>
        public static PassthroughCameraIntrinsics GetCameraIntrinsics(PassthroughCameraEye cameraEye)
        {
            AndroidJavaObject cameraCharacteristics = GetCameraCharacteristics(cameraEye);
            float[] intrinsicsArr = GetCameraValueByKey<float[]>(cameraCharacteristics, "LENS_INTRINSIC_CALIBRATION");

            if (HorizonOSVersion == EarlySupportOsVersion)
            {
                // The early API release contains a bug resulting in focal lengths (indices 0 and 1 of camera intrinsics)
                // being returned at half their actual value
                intrinsicsArr[0] *= 2f;
                intrinsicsArr[1] *= 2f;
            }

            List<Vector2Int> outputSizes = GetOutputSizes(cameraEye);
            Assert.IsTrue(outputSizes.Count > 0);

            // Looking for the highest resolution
            Vector2Int maxResolution = outputSizes[0];
            for (int i = 1; i < outputSizes.Count; i++)
            {
                Vector2Int outputSize = outputSizes[i];
                if (outputSize.x * outputSize.y > maxResolution.x * maxResolution.y)
                {
                    maxResolution = outputSize;
                }
            }

            return new PassthroughCameraIntrinsics
            {
                FocalLength = new Vector2(intrinsicsArr[0], intrinsicsArr[1]),
                PrincipalPoint = new Vector2(intrinsicsArr[2], intrinsicsArr[3]),
                Resolution = maxResolution,
                Skew = intrinsicsArr[4]
            };
        }

        /// <summary>
        /// Returns an Android Camera2 API's cameraId associated with the passthrough camera specified in the argument.
        /// </summary>
        /// <param name="cameraEye">The passthrough camera</param>
        /// <exception cref="ApplicationException">Throws an exception if the code was not able to find cameraId</exception>
        public static string GetCameraIdByEye(PassthroughCameraEye cameraEye)
        {
            EnsureInitialized();

            if (!cameraEyeToCameraIdMap.TryGetValue(cameraEye, out string cameraId))
                throw new ApplicationException($"Cannot find cameraId for the eye {cameraEye}");

            return cameraId;
        }

        /// <summary>
        /// Returns the world pose of a passthrough camera, calculated for a specific point in time. The time can be
        /// either in the past or in the future. If the specified time is in the future, the method will project
        /// the camera's pose based on its current motion and other relevant factors.
        /// </summary>
        /// <param name="time">Should be represented in the OpenXR clock. Use methods such as
        /// `OVRPlugin.GetTimeInSeconds()` to obtain a correct time value</param>
        /// <param name="cameraEye">The passthrough camera</param>
        public static OVRPose GetCameraPoseInWorld(double time, PassthroughCameraEye cameraEye)
        {
            string cameraId = GetCameraIdByEye(cameraEye);
            AndroidJavaObject cameraCharacteristics =
                cameraManager.Call<AndroidJavaObject>("getCameraCharacteristics", cameraId);

            // Position of the camera optical center
            float[] cameraTranslation = GetCameraValueByKey<float[]>(cameraCharacteristics, "LENS_POSE_TRANSLATION");
            // From Camera2 documentation: Position is relative to the center of the headset. (0, 0.2, 0) means that
            // the camera is 2 cm higher than the center of the headset. So, (0, 0.2, 0) is the translation
            // from camera space to the headset space.
            Vector3 t_headFromCamera = new Vector3(cameraTranslation[0], cameraTranslation[1], cameraTranslation[2]);

            // The orientation of the camera relative to the sensor coordinate system
            float[] cameraRotation = GetCameraValueByKey<float[]>(cameraCharacteristics, "LENS_POSE_ROTATION");
            // From Camera2 documentation: Represents rotation from the Android sensor coordinate system (head space)
            // to a camera-aligned coordinate system. So, the rotation is from head to camera
            Quaternion q_cameraFromHead =
                new Quaternion(cameraRotation[0], cameraRotation[1], cameraRotation[2], cameraRotation[3]);

            // Android coordinate systems are right-handed, and Unity's are left-handed.
            // Make camera translation and rotation left-handed
            t_headFromCamera = new Vector3(t_headFromCamera[0], t_headFromCamera[1], -t_headFromCamera[2]);
            q_cameraFromHead = new Quaternion(-q_cameraFromHead[0], -q_cameraFromHead[1], q_cameraFromHead[2],
                q_cameraFromHead[3]);

            Quaternion q_headFromCamera = Quaternion.Inverse(q_cameraFromHead);
            OVRPose T_HeadFromCamera = new OVRPose { orientation = q_headFromCamera, position = t_headFromCamera };
            OVRPose T_WorldFromHead = OVRPlugin.GetNodePoseStateAtTime(time, OVRPlugin.Node.Head).Pose.ToOVRPose();
            OVRPose T_WorldFromCamera = T_WorldFromHead * T_HeadFromCamera;

            // Originally the Z axis of the camera points to the viewer, and Y axis looks down.
            // For the convenience we rotate the space around X axis, for Z to look outwards from the headset, and Y axis pointing up.
            T_WorldFromCamera.orientation *= Quaternion.Euler(180, 0, 0);

            return T_WorldFromCamera;
        }

        /// <summary>
        /// Returns a 3D ray in the world space which starts from the passthrough camera origin and passes through the
        /// 2D camera pixel.
        /// </summary>
        /// <param name="time">Should be represented in the OpenXR clock. Use methods such as
        /// `OVRPlugin.GetTimeInSeconds()` to obtain a correct time value</param>
        /// <param name="cameraEye">The passthrough camera</param>
        /// <param name="screenPoint">A 2D point on the camera texture. The point is positioned relative to the
        /// maximum available camera resolution. This resolution can be obtained using <see cref="GetCameraIntrinsics"/>
        /// or <see cref="GetOutputSizes"/> methods.
        /// </param>
        public static Ray ScreenPointToRayInWorld(double time, PassthroughCameraEye cameraEye, Vector2Int screenPoint)
        {
            Ray rayInCamera = ScreenPointToRayInCamera(cameraEye, screenPoint);

            OVRPose cameraPoseInWorld = GetCameraPoseInWorld(time, cameraEye);
            Vector3 rayDirectionInWorld = cameraPoseInWorld.orientation * rayInCamera.direction;
            Vector3 rayOriginInWorld = cameraPoseInWorld.position;

            return new Ray(rayOriginInWorld, rayDirectionInWorld);
        }

        /// <summary>
        /// Returns a 3D ray in the camera space which starts from the passthrough camera origin - which is always
        /// (0, 0, 0) - and passes through the 2D camera pixel.
        /// </summary>
        /// <param name="cameraEye">The passthrough camera</param>
        /// <param name="screenPoint">A 2D point on the camera texture. The point is positioned relative to the
        /// maximum available camera resolution. This resolution can be obtained using <see cref="GetCameraIntrinsics"/>
        /// or <see cref="GetOutputSizes"/> methods.
        /// </param>
        public static Ray ScreenPointToRayInCamera(PassthroughCameraEye cameraEye, Vector2Int screenPoint)
        {
            PassthroughCameraIntrinsics intrinsics;
#if UNITY_ANDROID && !UNITY_EDITOR
            intrinsics = GetCameraIntrinsics(cameraEye);
#else
            // Providing some test values for debug purposes
            intrinsics = new PassthroughCameraIntrinsics
            {
                FocalLength = new Vector2(435, 435),
                PrincipalPoint = new Vector2(640, 480)
            };
#endif

            Vector3 directionInCamera = new Vector3
            {
                x = (screenPoint.x - intrinsics.PrincipalPoint.x) / intrinsics.FocalLength.x,
                y = (screenPoint.y - intrinsics.PrincipalPoint.y) / intrinsics.FocalLength.y,
                z = 1
            };

            return new Ray(Vector3.zero, directionInCamera);
        }

        #region Private methods

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            Debug.Log($"PCA: PassthroughCamera - Initializing...");
            using AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
            cameraManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "camera");
            Assert.IsNotNull(cameraManager, "Camera manager has not been provided by the Android system");

            if (HorizonOSVersion == EarlySupportOsVersion)
            {
                Debug.Log("PCA: PassthroughCamera - This is the early version of Passthrough Camera API");
                // In the early version the camera eye to cameraId mapping is hardcoded.
                cameraEyeToCameraIdMap[PassthroughCameraEye.Right] = "42";
                cameraEyeToCameraIdMap[PassthroughCameraEye.Left] = "44";
                initialized = true;
                return;
            }

            string[] cameraIds = GetCameraIdList();
            Debug.Log($"PCA: PassthroughCamera - cameraId list is {string.Join(", ", cameraIds)}");

            foreach (var cameraId in cameraIds)
            {
                CameraSource? cameraSource = null;
                CameraPosition? cameraPosition = null;

                AndroidJavaObject cameraCharacteristics = GetCameraCharacteristics(cameraId);
                using AndroidJavaObject keysList = cameraCharacteristics.Call<AndroidJavaObject>("getKeys");
                int size = keysList.Call<int>("size");
                for (int i = 0; i < size; i++)
                {
                    using AndroidJavaObject key = keysList.Call<AndroidJavaObject>("get", i);
                    string keyName = key.Call<string>("getName");

                    if (string.Equals(keyName, "com.meta.extra_metadata.camera_source", StringComparison.OrdinalIgnoreCase))
                    {
                        // Both `com.meta.extra_metadata.camera_source` and `com.meta.extra_metadata.camera_source` are
                        // custom camera fields which are stored as arrays of size 1, instead of single values.
                        // We have to read those values correspondingly
                        sbyte[] cameraSourceArr = GetCameraValueByKey<sbyte[]>(cameraCharacteristics, key);
                        if (cameraSourceArr == null || cameraSourceArr.Length != 1)
                            continue;

                        cameraSource = (CameraSource)cameraSourceArr[0];
                    }
                    else if (string.Equals(keyName, "com.meta.extra_metadata.position", StringComparison.OrdinalIgnoreCase))
                    {
                        sbyte[] cameraPositionArr = GetCameraValueByKey<sbyte[]>(cameraCharacteristics, key);
                        if (cameraPositionArr == null || cameraPositionArr.Length != 1)
                            continue;

                        cameraPosition = (CameraPosition)cameraPositionArr[0];
                    }
                }

                if (!cameraSource.HasValue || !cameraPosition.HasValue || cameraSource.Value != CameraSource.Passthrough)
                    continue;

                switch (cameraPosition)
                {
                    case CameraPosition.Left:
                        Debug.Log($"PCA: Found left passthrough cameraId = {cameraId}");
                        cameraEyeToCameraIdMap[PassthroughCameraEye.Left] = cameraId;
                        break;
                    case CameraPosition.Right:
                        Debug.Log($"PCA: Found right passthrough cameraId = {cameraId}");
                        cameraEyeToCameraIdMap[PassthroughCameraEye.Right] = cameraId;
                        break;
                    default:
                        throw new ApplicationException($"Cannot parse Camera Position value {cameraPosition}");
                }
            }

            initialized = true;
        }

        private static string[] GetCameraIdList()
        {
            return cameraManager.Call<string[]>("getCameraIdList");
        }

        private static List<Vector2Int> GetOutputSizesInternal(PassthroughCameraEye cameraEye)
        {
            EnsureInitialized();

            string cameraId = GetCameraIdByEye(cameraEye);
            AndroidJavaObject cameraCharacteristics = GetCameraCharacteristics(cameraId);
            using AndroidJavaObject configurationMap =
                GetCameraValueByKey<AndroidJavaObject>(cameraCharacteristics, "SCALER_STREAM_CONFIGURATION_MAP");
            AndroidJavaObject[] outputSizes = configurationMap.Call<AndroidJavaObject[]>("getOutputSizes", YUV_420_888);

            var result = new List<Vector2Int>();
            foreach (AndroidJavaObject outputSize in outputSizes)
            {
                int width = outputSize.Call<int>("getWidth");
                int height = outputSize.Call<int>("getHeight");
                result.Add(new Vector2Int(width, height));
            }

            foreach (AndroidJavaObject obj in outputSizes)
            {
                obj?.Dispose();
            }

            return result;
        }

        private static AndroidJavaObject GetCameraCharacteristics(string cameraId)
        {
            return cameraCharacteristicsMap.GetOrAdd(cameraId,
                _ => cameraManager.Call<AndroidJavaObject>("getCameraCharacteristics", cameraId));
        }

        private static AndroidJavaObject GetCameraCharacteristics(PassthroughCameraEye eye)
        {
            var cameraId = GetCameraIdByEye(eye);
            return GetCameraCharacteristics(cameraId);
        }

        private static T GetCameraValueByKey<T>(AndroidJavaObject cameraCharacteristics, string keyStr)
        {
            AndroidJavaObject key = cameraCharacteristics.GetStatic<AndroidJavaObject>(keyStr);
            return GetCameraValueByKey<T>(cameraCharacteristics, key);
        }

        private static T GetCameraValueByKey<T>(AndroidJavaObject cameraCharacteristics, AndroidJavaObject key)
        {
            return cameraCharacteristics.Call<T>("get", key);
        }

        private enum CameraSource
        {
            Passthrough = 0
        }

        private enum CameraPosition
        {
            Left = 0,
            Right = 1
        }

        #endregion Private methods
    }
}
