using UnityEngine;
using UnityEditor;

namespace Styly.XRRig
{
    public class InstallAdditionalSamples
    {
        [InitializeOnLoadMethod]
        private static void InstallSamples()
        {
            // Skip in batch mode (e.g., when building via CI/CD)
            if (Application.isBatchMode)
            {
                return;
            }

#if USE_POLYSPATIAL
            // Install visionOS sample if PolySpatial is installed
            InstallRequiredSamples.InstallSample("com.unity.xr.interaction.toolkit","visionOS");
#endif
        }
    }
}