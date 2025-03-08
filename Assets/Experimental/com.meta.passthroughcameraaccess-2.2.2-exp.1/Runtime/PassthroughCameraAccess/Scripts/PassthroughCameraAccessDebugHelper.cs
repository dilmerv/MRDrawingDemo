// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Meta.XR.PassthroughCamera
{
    public static class PassthroughCameraAccessDebugHelper
    {
        public enum DebugTypeEnum
        {
            ERROR,
            LOG,
            WARNING
        }

        public enum DebuglevelEnum
        {
            ALL,
            NONE,
            ONLY_ERROR,
            ONLY_LOG,
            ONLY_WARNING

        }

        public static DebuglevelEnum debugLevel = DebuglevelEnum.ALL;

        /// <summary>
        /// Send debug information to Unity console based on DebugType and DebugLevel
        /// </summary>
        /// <param name="mType"></param>
        /// <param name="message"></param>
        public static void DebugMessage(DebugTypeEnum mType, string message)
        {
            switch (mType)
            {
                case DebugTypeEnum.ERROR:
                    if (debugLevel == DebuglevelEnum.ALL || debugLevel == DebuglevelEnum.ONLY_ERROR)
                    {
                        Debug.LogError(message);
                    }
                    break;
                case DebugTypeEnum.LOG:
                    if (debugLevel == DebuglevelEnum.ALL || debugLevel == DebuglevelEnum.ONLY_LOG)
                    {
                        Debug.Log(message);
                    }
                    break;
                case DebugTypeEnum.WARNING:
                    if (debugLevel == DebuglevelEnum.ALL || debugLevel == DebuglevelEnum.ONLY_WARNING)
                    {
                        Debug.LogWarning(message);
                    }
                    break;
            }
        }
    }
}
