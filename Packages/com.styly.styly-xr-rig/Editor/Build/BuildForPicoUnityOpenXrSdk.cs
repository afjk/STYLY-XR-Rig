using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using Styly.XRRig.Initialization;
using static Styly.XRRig.SetupSdk.SetupSdkUtils;

namespace Styly.XRRig.Build
{
    /// <summary>
    /// Build script for PICO Unity OpenXR SDK for GameCI
    /// This is a modified version that runs synchronously for CI/CD environments
    /// </summary>
    public class BuildForPicoUnityOpenXrSdk
    {

        /// <summary>
        /// Build method called from GameCI
        /// </summary>
        public static void Build()
        {
            try
            {
                Debug.Log("Starting PICO Unity OpenXR SDK build setup...");
                
                // Install required samples first (XR Interaction Toolkit samples are needed for STYLY XR Rig prefab)
                Debug.Log("Installing required package samples...");
                CISampleInstaller.InstallRequiredSamplesForCI();
                
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
                "com.pico.openxr.feature.passthrough"
            });
            
            // Step 3: Enable Interaction Profiles
            EnableInteractionProfiles(BuildTargetGroup.Android, new string[]
            {
                "com.unity.openxr.feature.input.handinteraction",
                "com.unity.openxr.feature.input.PICO4touch",
                "com.unity.openxr.feature.input.PICO4Ultratouch"
            });
            
            // Step 4: Setup Other Settings
            SetAndroidMinimumApiLevel(AndroidSdkVersions.AndroidApiLevel26);
            ApplyStylyPipelineAsset();
            UseNewInputSystemOnly();
            SetGraphicsAPIs(BuildTarget.Android, new List<GraphicsDeviceType> { GraphicsDeviceType.OpenGLES3 });
            SetRenderMode(OpenXRSettings.RenderMode.MultiPass, BuildTargetGroup.Android);
            
            // Step 5: Fix XR Project Validation Issues
            XRProjectValidationFixAll.FixAllIssues(BuildTargetGroup.Android);
            
            // Step 6: Additional PICO-specific Settings
            // Set isCameraSubsystem to true
            SetFieldValueOfOpenXrFeature(BuildTargetGroup.Android, "com.pico.openxr.feature.passthrough", "isCameraSubsystem", true);
            
            // Configure PICO Hand Tracking (synchronous version for CI)
            ConfigurePicoHandTrackingForCI();
            
            Debug.Log("PICO Unity OpenXR SDK setup for CI completed.");
        }

        /// <summary>
        /// Configure PICO hand tracking synchronously for CI environments
        /// This is based on SetupSdkUtils.ConfigurePicoHandTracking but runs synchronously
        /// </summary>
        private static void ConfigurePicoHandTrackingForCI()
        {
            try
            {
                // Load the PICO project setting asset
                var picoProjectSetting = Resources.Load("PICOProjectSetting");
                if (picoProjectSetting != null)
                {
                    ModifyPicoProjectSettingAssetSync(picoProjectSetting);
                }
                else
                {
                    Debug.LogWarning("PICOProjectSetting asset not found. Hand tracking configuration skipped.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to configure PICO hand tracking: {e.Message}");
            }
        }

        /// <summary>
        /// Modify the PICOProjectSetting asset to enable hand tracking
        /// This is a synchronous version of the SetupSdkUtils method
        /// </summary>
        private static void ModifyPicoProjectSettingAssetSync(object picoProjectSetting)
        {
            try
            {
                var type = picoProjectSetting.GetType();

                // Set isHandTracking to true (handTrackingSupportType defaults to ControllersAndHands)
                const string IsHandTrackingFieldName = "isHandTracking";
                var isHandTrackingField = type.GetField(IsHandTrackingFieldName, BindingFlags.Public | BindingFlags.Instance);
                if (isHandTrackingField != null && isHandTrackingField.FieldType == typeof(bool))
                {
                    isHandTrackingField.SetValue(picoProjectSetting, true);
                    Debug.Log("Enabled PICO Hand Tracking.");
                }
                else
                {
                    Debug.LogWarning($"Could not find field '{IsHandTrackingFieldName}' or it has the wrong type in PICOProjectSetting asset. Hand tracking configuration may have failed.");
                }

                // Save changes
                if (picoProjectSetting is UnityEngine.Object unityObject)
                {
                    EditorUtility.SetDirty(unityObject);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to modify PICO hand tracking settings: {e.Message}");
            }
        }
    }
}
