using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using static Styly.XRRig.SetupSdk.SetupSdkUtils;

namespace Styly.XRRig.Build
{
    /// <summary>
    /// Build script for Meta OpenXR SDK (Quest) for GameCI
    /// This is a modified version that runs synchronously for CI/CD environments
    /// </summary>
    public class BuildForMetaOpenXrSdk
    {

        /// <summary>
        /// Build method called from GameCI
        /// </summary>
        public static void Build()
        {
            try
            {
                Debug.Log("Starting Meta OpenXR SDK build setup...");
                
                // Switch to Android build target
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                
                // Setup SDK synchronously for CI
                SetupSdkForCI();
                
                Debug.Log("Meta OpenXR SDK setup completed. Starting build...");
                
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
                
                string buildPath = "build/Android/MetaOpenXR.apk";
                
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
        /// This is based on SetupSdk_MetaOpenXrSdk.SetUpSdkSettings but runs synchronously
        /// Note: Some steps like EnableXRFeatureSet may need manual configuration before CI
        /// </summary>
        private static void SetupSdkForCI()
        {
            // Step 1: Enable the OpenXR Loader
            EnableXRPlugin(BuildTargetGroup.Android, "UnityEngine.XR.OpenXR.OpenXRLoader");
            
            // Step 2: Enable OpenXR Features (skip feature set for CI - should be pre-configured)
            EnableOpenXrFeatures(BuildTargetGroup.Android, new string[]
            {
                "com.unity.openxr.feature.input.handtracking",
                "com.unity.openxr.feature.metaquest",
                "com.unity.openxr.feature.arfoundation-meta-anchor",
                "com.unity.openxr.feature.meta-boundary-visibility",
                "com.unity.openxr.feature.arfoundation-meta-bounding-boxes",
                "com.unity.openxr.feature.arfoundation-meta-session",
                "com.unity.openxr.feature.meta-colocation-discovery",
                "com.unity.openxr.feature.arfoundation-meta-camera"
            });
            
            // Step 3: Enable Interaction Profiles
            EnableInteractionProfiles(BuildTargetGroup.Android, new string[]
            {
                "com.unity.openxr.feature.input.handinteraction",
                "com.unity.openxr.feature.input.metaquestplus"
            });
            
            // Step 4: Setup Other Settings
            SetAndroidMinimumApiLevel(AndroidSdkVersions.AndroidApiLevel29);
            ApplyStylyPipelineAsset();
            UseNewInputSystemOnly();
            SetGraphicsAPIs(BuildTarget.Android, new List<GraphicsDeviceType> { GraphicsDeviceType.Vulkan });
            SetRenderMode(OpenXRSettings.RenderMode.MultiPass, BuildTargetGroup.Android);
            
            // Step 5: Fix XR Project Validation Issues
            SetupSdk.XRProjectValidationFixAll.FixAllIssues(BuildTargetGroup.Android);
            
            Debug.Log("Meta OpenXR SDK setup for CI completed.");
        }
    }
}
