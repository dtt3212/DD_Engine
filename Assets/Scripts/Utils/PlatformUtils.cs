﻿using System.Runtime.InteropServices;
using UnityEngine;

namespace DDEngine.Utils
{
    public class PlatformUtils
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal", EntryPoint="isOnMobile")]
        private static extern bool IsOnMobile();
#endif

        public static bool IsMobile()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return IsOnMobile();
#else
            return Application.isMobilePlatform;
#endif
        }
    }
}
