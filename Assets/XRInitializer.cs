using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// Handles the initialization and deinitialization of XR subsystems in Unity.
/// </summary>
/// <remarks>
/// When developing with a Meta Quest headset, this helps to prevent a black screen and the hourglass loading icon in the headset screen on application exit, 
/// and subsequent issues when re-entering play mode.
/// </remarks>
public class XRInitializer : MonoBehaviour
{
    private void Awake()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }

        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
    }

    private void OnDisable()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
    }
}