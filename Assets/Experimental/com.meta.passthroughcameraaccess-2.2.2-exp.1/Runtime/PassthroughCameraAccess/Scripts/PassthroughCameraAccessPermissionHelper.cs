// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;

using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Meta.XR.PassthroughCamera
{
    using DH = PassthroughCameraAccessDebugHelper;

    public static class PassthroughCameraAccessPermissionHelper
    {
        const string ANDROID_CAMERA_PERMISSION = "android.permission.CAMERA"; // Required to use WebCamTexture object.
        const string HORIZONOS_CAMERA_PERMISSION = "horizonos.permission.HEADSET_CAMERA"; // Required to access the Passthrough Camera API in Horizon OS v74 and above.

        public delegate void PermissionCheck();
        /// <summary>
        /// The event which fires if the app doesn't have corresponding permissions to access camera.
        /// </summary>
        public static PermissionCheck OnPermissionFails;
        /// <summary>
        /// The event which fires if the camera access permissions are granted.
        /// </summary>
        public static PermissionCheck OnPermissionSuccess;

#if UNITY_ANDROID
        /// <summary>
        /// Request camera permission if the permission is not autorized by the user.
        /// </summary>
        public static IEnumerator AskCameraPermission()
        {
            if (HasAllPermissionsGranted())
            {
                DH.DebugMessage(DH.DebugTypeEnum.LOG, $"PCA: All camera permissions granted.");
                OnPermissionSuccess?.Invoke();
                yield return null;
            }
            else
            {
#if UNITY_2022_3
                // Fix for Unity 2022.3.50f1:
                //   The android camera popup only appears if we wait 2 seconds.
                //   This issue happens if any other permission popup appears before it.
                DH.DebugMessage(DH.DebugTypeEnum.LOG, $"PCA: Waiting to request camera permissions.");
                yield return new WaitForSeconds(2f);
#else
                yield return null;
#endif
                DH.DebugMessage(DH.DebugTypeEnum.LOG, $"PCA: Requesting camera permissions.");

                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += PermissionCallbacksPermissionDenied;
                callbacks.PermissionGranted += PermissionCallbacksPermissionGranted;

                int osVersion = PassthroughCameraUtils.HorizonOSVersion;
                if (osVersion == PassthroughCameraUtils.EarlySupportOsVersion)
                {
                    // For the early version of the Passthrough Camera API request only the android.permission.CAMERA permission.
                    Permission.RequestUserPermission(ANDROID_CAMERA_PERMISSION, callbacks);
                }
                else
                {
                    // For OS v74 and above request both permissions.
                    Permission.RequestUserPermissions(new[] { HORIZONOS_CAMERA_PERMISSION, ANDROID_CAMERA_PERMISSION }, callbacks);
                }
            }
        }

        /// <summary>
        /// Permission Granted callback
        /// </summary>
        /// <param name="permissionName"></param>
        private static void PermissionCallbacksPermissionGranted(string permissionName)
        {
            DH.DebugMessage(DH.DebugTypeEnum.LOG, $"PCA: Permission {permissionName} Granted");

            // Only initialize the WebCamTexture object if both permissions are granted
            if (HasAllPermissionsGranted())
            {
                    OnPermissionSuccess?.Invoke();
            }
        }

        /// <summary>
        /// Permission Denied callback.
        /// </summary>
        /// <param name="permissionName"></param>
        private static void PermissionCallbacksPermissionDenied(string permissionName)
        {
            DH.DebugMessage(DH.DebugTypeEnum.WARNING, $"PCA: Permission {permissionName} Denied");
            OnPermissionFails?.Invoke();
        }

        /// <summary>
        /// Check if both camera permissions are granted by the user.
        /// </summary>
        /// <returns>Return True if both camera permissions are granted</returns>
        private static bool HasAllPermissionsGranted()
        {
            // HORIZONOS_CAMERA_PERMISSION permission is required for v74 and above.
            int osVersion = PassthroughCameraUtils.HorizonOSVersion;
            if (osVersion == PassthroughCameraUtils.EarlySupportOsVersion)
            {
                return Permission.HasUserAuthorizedPermission(ANDROID_CAMERA_PERMISSION);
            }

            return Permission.HasUserAuthorizedPermission(HORIZONOS_CAMERA_PERMISSION)
                   && Permission.HasUserAuthorizedPermission(ANDROID_CAMERA_PERMISSION);
        }
#endif
    }
}
