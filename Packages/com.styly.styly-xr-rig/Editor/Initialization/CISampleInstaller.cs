using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Styly.XRRig.Initialization
{
    /// <summary>
    /// CI/CD-compatible sample installer that works in batch mode.
    /// This class provides synchronous methods to install package samples required for building.
    /// </summary>
    public static class CISampleInstaller
    {
        // Required samples information JSON file name
        private static readonly string RequiredSamplesJson = "required_samples.json";

        /// <summary>
        /// Install all required package samples for CI/CD build.
        /// This method runs synchronously and is designed to work in batch mode.
        /// </summary>
        public static void InstallRequiredSamplesForCI()
        {
            Debug.Log("CI Sample Installer: Starting installation of required samples...");

            try
            {
                // Get the path of this package
                var MyPackageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    System.Reflection.MethodInfo.GetCurrentMethod().DeclaringType.Assembly);
                
                if (MyPackageInfo == null)
                {
                    Debug.LogError("CI Sample Installer: Could not find package info for this assembly.");
                    return;
                }

                string MyPackagePath = MyPackageInfo.resolvedPath;

                // Read the JSON file that contains the list of samples to install
                string RequiredSamplesJsonPath = Path.Combine(MyPackagePath, RequiredSamplesJson);
                
                if (!File.Exists(RequiredSamplesJsonPath))
                {
                    Debug.LogError($"CI Sample Installer: Required samples JSON not found at: {RequiredSamplesJsonPath}");
                    return;
                }

                string jsonData = File.ReadAllText(RequiredSamplesJsonPath);
                SamplePackageInfo packageInfo = JsonUtility.FromJson<SamplePackageInfo>(jsonData);

                if (packageInfo?.samples == null || packageInfo.samples.Length == 0)
                {
                    Debug.LogWarning("CI Sample Installer: No samples found in required_samples.json");
                    return;
                }

                foreach (var sampleToInstall in packageInfo.samples)
                {
                    InstallSampleSync(sampleToInstall.PackageName, sampleToInstall.SampleName);
                }

                // Refresh the asset database to ensure all samples are properly loaded
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("CI Sample Installer: All required samples have been processed.");
            }
            catch (Exception e)
            {
                Debug.LogError($"CI Sample Installer: Error installing samples: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Install a specific sample from a package synchronously.
        /// </summary>
        /// <param name="packageName">Package name containing the sample</param>
        /// <param name="sampleName">Sample name to install</param>
        public static void InstallSampleSync(string packageName, string sampleName)
        {
            Debug.Log($"CI Sample Installer: Processing sample '{sampleName}' from '{packageName}'...");

            try
            {
                var packageInformation = GetPackageInfoSync(packageName);
                if (packageInformation == null)
                {
                    Debug.LogWarning($"CI Sample Installer: Package '{packageName}' not found. Skipping sample '{sampleName}'.");
                    return;
                }

                var samples = UnityEditor.PackageManager.UI.Sample.FindByPackage(packageName, packageInformation.version);
                
                if (samples == null)
                {
                    Debug.LogWarning($"CI Sample Installer: No samples found for package '{packageName}'.");
                    return;
                }

                foreach (var sample in samples)
                {
                    if (sample.displayName == sampleName)
                    {
                        // Check if the latest version is already imported
                        if (sample.isImported && IsLatestSampleImported(sample, packageInformation.version))
                        {
                            Debug.Log($"CI Sample Installer: Sample '{sampleName}' is already installed with the correct version.");
                            return;
                        }

                        // Install or update the sample
                        bool result = sample.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
                        
                        if (result)
                        {
                            Debug.Log($"CI Sample Installer: Successfully installed sample '{sampleName}' from '{packageInformation.displayName}' @ {packageInformation.version}");
                        }
                        else
                        {
                            Debug.LogWarning($"CI Sample Installer: Failed to install sample '{sampleName}'. Import returned false.");
                        }
                        return;
                    }
                }

                Debug.LogWarning($"CI Sample Installer: Sample '{sampleName}' not found in package '{packageName}'.");
            }
            catch (Exception e)
            {
                Debug.LogError($"CI Sample Installer: Error installing sample '{sampleName}' from '{packageName}': {e.Message}");
            }
        }

        /// <summary>
        /// Get package information synchronously.
        /// </summary>
        /// <param name="packageName">Package name to look up</param>
        /// <returns>Package information or null if not found</returns>
        private static UnityEditor.PackageManager.PackageInfo GetPackageInfoSync(string packageName)
        {
            var request = Client.List(true, true);
            
            // Wait for the request to complete
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (request.Status == StatusCode.Success)
            {
                return request.Result.FirstOrDefault(pkg => pkg.name == packageName);
            }

            Debug.LogWarning($"CI Sample Installer: Failed to get package list. Status: {request.Status}");
            return null;
        }

        /// <summary>
        /// Returns true if the sample is imported and its version folder matches the currently installed package version.
        /// </summary>
        private static bool IsLatestSampleImported(UnityEditor.PackageManager.UI.Sample sample, string currentPackageVersion)
        {
            if (!sample.isImported) return false;

            // Expected import path: Assets/Samples/{packageName}/{version}/{sampleName}
            var importedDirInfo = new DirectoryInfo(sample.importPath);
            var versionFolder = importedDirInfo.Parent?.Name ?? string.Empty;
            return versionFolder == currentPackageVersion;
        }

        [Serializable]
        private class SamplePackageInfo
        {
            public Sample[] samples;
        }

        [Serializable]
        private class Sample
        {
            public string PackageName;
            public string SampleName;
        }
    }
}
