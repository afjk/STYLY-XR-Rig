using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using static Styly.XRRig.SetupSdk.SetupSdkUtils;

namespace Styly.XRRig.Build
{
    /// <summary>
    /// Build script for PICO Unity OpenXR SDK for GameCI
    /// This is a modified version that runs synchronously for CI/CD environments
    /// </summary>
    public class BuildForPicoUnityOpenXrSdk
    {
        private static readonly string packageIdentifier = "https://github.com/Pico-Developer/PICO-Unity-OpenXR-SDK.git#release_1.4.0";

        /// <summary>
        /// Build method called from GameCI
        /// </summary>
        public static void Build()
        {
            try
            {
                Debug.Log("Starting PICO Unity OpenXR SDK build setup...");
                
                // Switch to Android build target
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                
                // Setup SDK synchronously for CI
                SetupSdkForCI();
                
                Debug.Log("PICO Unity OpenXR SDK setup completed. Starting build...");
                
                // Configure build settings
                string[] scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
                
                if (scenes.Length == 0)
                {
                    Debug.LogError("No scenes found in build settings!");
                    EditorApplication.Exit(1);
                    return;
                }
                
                string buildPath = "build/Android/PicoOpenXR.apk";
                
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = buildPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };
                
                Debug.Log($"Building to: {buildPath}");
                Debug.Log($"Scenes: {string.Join(", ", scenes)}");
                
                BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                BuildSummary summary = report.summary;
                
                if (summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"Build succeeded: {summary.totalSize} bytes");
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError($"Build failed: {summary.result}");
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Build error: {e.Message}\n{e.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Setup SDK synchronously for CI environments
        /// This is based on SetupSdk_PicoUnityOpenXrSdk.SetUpSdkSettings but runs synchronously
        /// </summary>
        private static void SetupSdkForCI()
        {
            // Step 1: Enable the OpenXR Loader
            EnableXRPlugin(BuildTargetGroup.Android, "UnityEngine.XR.OpenXR.OpenXRLoader");
            
            // Step 2: Enable the XR Feature Set
            EnableXRFeatureSet(BuildTargetGroup.Android, "com.picoxr.openxr.features");
            
            // Step 3: Enable OpenXR Features
            EnableOpenXrFeatures(BuildTargetGroup.Android, new string[]
            {
                "com.unity.openxr.feature.input.handtracking",
                "com.pico.openxr.feature.passthrough"
            });
            
            // Step 4: Enable Interaction Profiles
            EnableInteractionProfiles(BuildTargetGroup.Android, new string[]
            {
                "com.unity.openxr.feature.input.handinteraction",
                "com.unity.openxr.feature.input.PICO4touch",
                "com.unity.openxr.feature.input.PICO4Ultratouch"
            });
            
            // Step 5: Setup Other Settings
            SetAndroidMinimumApiLevel(AndroidSdkVersions.AndroidApiLevel26);
            ApplyStylyPipelineAsset();
            UseNewInputSystemOnly();
            SetGraphicsAPIs(BuildTarget.Android, new List<GraphicsDeviceType> { GraphicsDeviceType.OpenGLES3 });
            SetRenderMode(OpenXRSettings.RenderMode.MultiPass, BuildTargetGroup.Android);
            
            // Step 6: Fix XR Project Validation Issues
            SetupSdk.XRProjectValidationFixAll.FixAllIssues(BuildTargetGroup.Android);
            
            // Step 7: Additional PICO-specific Settings
            // Set isCameraSubsystem to true
            SetFieldValueOfOpenXrFeature(BuildTargetGroup.Android, "com.pico.openxr.feature.passthrough", "isCameraSubsystem", true);
            
            // Configure PICO Hand Tracking
            ConfigurePicoHandTracking();
            
            Debug.Log("PICO Unity OpenXR SDK setup for CI completed.");
        }
    }
}
