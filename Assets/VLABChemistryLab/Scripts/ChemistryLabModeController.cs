using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Management;
using VLAB.ChemistryLab.Mode;

namespace VLAB.ChemistryLab
{
    /// <summary>Switches presentation/input rigs while keeping experiment components alive.</summary>
    public sealed class ChemistryLabModeController : MonoBehaviour
    {
        private const string PersistedModeKey = "VLAB.PresentationMode";

        [Header("Desktop")]
        [SerializeField] private GameObject desktopCamera;
        [SerializeField] private Behaviour desktopInterface;

        [Header("XR")]
        [SerializeField] private GameObject xrOrigin;
        [SerializeField] private Camera xrCamera;
        [SerializeField] private GameObject xrInteractionManager;
        [SerializeField] private GameObject xrInteractionSimulator;

        [Header("Startup")]
        [SerializeField] private VLabModeRequest defaultRequest = VLabModeRequest.Auto;
        [SerializeField, Min(.25f)] private float xrInitializationTimeoutSeconds = 10f;

        private CancellationTokenSource startupCancellation;
        private UnityXrLoaderLifecycle loaderLifecycle;
        private Interaction.LabTeleportArea teleportArea;
        private GameObject suspendedGlobalSimulator;

        private IEnumerator SuspendGlobalSimulator()
        {
            yield return null;
            var simulator=UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator.instance;
            if(simulator!=null && simulator.gameObject!=xrInteractionSimulator && simulator.gameObject.activeSelf)
            { suspendedGlobalSimulator=simulator.gameObject; suspendedGlobalSimulator.SetActive(false); }
        }

        public VLabPresentationMode CurrentMode { get; private set; } = VLabPresentationMode.Desktop;
        public VLabModeFailureReason LastFailureReason { get; private set; }

        public void Configure(
            GameObject desktopCameraObject,
            Behaviour desktopInterfaceComponent,
            GameObject xrOriginObject,
            Camera xrCameraComponent,
            GameObject xrInteractionManagerObject,
            GameObject xrInteractionSimulatorObject)
        {
            desktopCamera = desktopCameraObject;
            desktopInterface = desktopInterfaceComponent;
            xrOrigin = xrOriginObject;
            xrCamera = xrCameraComponent;
            xrInteractionManager = xrInteractionManagerObject;
            xrInteractionSimulator = xrInteractionSimulatorObject;
        }

        public void Configure(
            GameObject desktopCameraObject,
            Behaviour desktopInterfaceComponent,
            GameObject xrOriginObject,
            GameObject xrInteractionManagerObject)
        {
            Configure(
                desktopCameraObject,
                desktopInterfaceComponent,
                xrOriginObject,
                xrOriginObject == null ? null : xrOriginObject.GetComponentInChildren<Camera>(true),
                xrInteractionManagerObject,
                null);
        }

        private void Awake()
        {
            if(Application.isEditor) StartCoroutine(SuspendGlobalSimulator());
            InitializeSafeTeleport();
            ApplyPresentationMode(VLabPresentationMode.Desktop, VLabModeFailureReason.None);
        }

        private void InitializeSafeTeleport()
        {
            if (xrOrigin == null || xrInteractionManager == null) return;
            var provider = xrOrigin.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true);
            foreach (var snap in xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider>(true))
                snap.turnAmount = 30f;
              if (provider == null) return;
              // Decorative tile seams must not block rays above the real floor collider.
              foreach (var decoration in GetComponentsInChildren<Collider>(true))
                  if (decoration.name == "FloorTileLine") decoration.enabled = false;
              foreach (var collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider.name != "Floor") continue;
                teleportArea = collider.GetComponent<Interaction.LabTeleportArea>() ?? collider.gameObject.AddComponent<Interaction.LabTeleportArea>();
                  teleportArea.Configure(collider, xrOrigin.transform);
                  teleportArea.interactionLayers = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Teleport");
                teleportArea.teleportationProvider = provider;
                teleportArea.interactionManager = xrInteractionManager.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
                teleportArea.matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.WorldSpaceUp;
                teleportArea.matchDirectionalInput = false;
                teleportArea.filterSelectionByHitNormal = true;
                teleportArea.upNormalToleranceDegrees = 10f;
                break;
            }
        }

        private async void Start()
        {
            if (VLAB.Core.Input.VLabHeadPose.PhoneViewer)
            {
                ApplyPresentationMode(VLabPresentationMode.Desktop);
                if (desktopInterface != null) desktopInterface.enabled = false;
                return;
            }
            startupCancellation = new CancellationTokenSource();
            loaderLifecycle = new UnityXrLoaderLifecycle(this);

            VLabModeRequestResolution resolution = VLabModeRequestResolver.Resolve(
                Environment.GetCommandLineArgs(), ReadPersistedChoice(), null);
            if (resolution.FailureReason != VLabModeFailureReason.None)
            {
                ApplyPresentationMode(VLabPresentationMode.Desktop, resolution.FailureReason);
                ReportFallback(resolution.FailureReason);
                return;
            }

            VLabModeRequest request = ResolveAutoDefault(resolution.Request);
            VLabPresentationSession session = new VLabPresentationSession(gameObject);
            VLabPresentationModeCoordinator coordinator = new VLabPresentationModeCoordinator(session, loaderLifecycle);
            try
            {
                VLabModeTransitionResult result = await coordinator.ActivateAsync(
                    request,
                    Application.isEditor || Debug.isDebugBuild,
                    true,
                    TimeSpan.FromSeconds(xrInitializationTimeoutSeconds),
                    startupCancellation.Token);
                if (this == null)
                    return;
                ApplyPresentationMode(result.Mode, result.FailureReason);
                if (result.FellBackToDesktop)
                    ReportFallback(result.FailureReason);
            }
            catch (OperationCanceledException)
            {
                if (this != null)
                    ApplyPresentationMode(VLabPresentationMode.Desktop, VLabModeFailureReason.Cancelled);
            }
        }

        private VLabModeRequest ResolveAutoDefault(VLabModeRequest request)
        {
            if (request != VLabModeRequest.Auto)
                return request;
#if UNITY_EDITOR
            return defaultRequest == VLabModeRequest.Auto ? VLabModeRequest.Desktop : defaultRequest;
#else
            return defaultRequest;
#endif
        }

        private static VLabModeRequest? ReadPersistedChoice()
        {
            if (!PlayerPrefs.HasKey(PersistedModeKey))
                return null;
            string value = PlayerPrefs.GetString(PersistedModeKey, string.Empty);
            if (Enum.TryParse(value, true, out VLabModeRequest request) &&
                (request == VLabModeRequest.Desktop || request == VLabModeRequest.OpenXRHardware))
                return request;
            return null;
        }

        public void ApplyPresentationMode(
            VLabPresentationMode mode,
            VLabModeFailureReason failureReason = VLabModeFailureReason.None)
        {
            bool desktop = mode == VLabPresentationMode.Desktop;
            bool simulator = mode == VLabPresentationMode.XRSimulator;
            bool xr = !desktop;
            if (teleportArea != null) teleportArea.enabled = xr;

            SetActive(desktopCamera, desktop);
            if (desktopInterface != null)
                desktopInterface.enabled = desktop;
            SetActive(xrInteractionManager, xr);
            if (xr && xrInteractionManager != null)
            {
                // Bind explicitly before enabling the rig: auto-discovery can retain a disabled Desktop manager.
                var manager = xrInteractionManager.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
                if (manager != null)
                {
                    if (xrOrigin != null)
                    {
                        foreach (var group in xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRInteractionGroup>(true))
                            group.interactionManager = manager;
                        foreach (var interactor in xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true))
                            interactor.interactionManager = manager;
                    }
                    foreach (var interactable in GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>(true))
                        interactable.interactionManager = manager;
                }
            }
            SetActive(xrOrigin, xr);
            SetActive(xrInteractionSimulator, simulator);

            SetCameraState(desktopCamera == null ? null : desktopCamera.GetComponent<Camera>(), desktop);
            SetCameraState(xrCamera, xr);
            CurrentMode = mode;
            LastFailureReason = failureReason;
        }

        /// <summary>Compatibility entry point retained until every caller uses the three-mode API.</summary>
        public void ApplyMode(bool vrEnabled) => ApplyPresentationMode(
            vrEnabled ? VLabPresentationMode.OpenXRHardware : VLabPresentationMode.Desktop);

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static void SetCameraState(Camera camera, bool active)
        {
            if (camera == null)
                return;
            camera.enabled = active;
            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = active;
            camera.gameObject.tag = active ? "MainCamera" : "Untagged";
        }

        private static void ReportFallback(VLabModeFailureReason reason)
        {
            Debug.LogWarning($"[VLAB] XR could not start ({reason}). The lab is running safely in Desktop mode.");
        }

        private void OnDestroy()
        {
            if(suspendedGlobalSimulator!=null)suspendedGlobalSimulator.SetActive(true);
            startupCancellation?.Cancel();
            startupCancellation?.Dispose();
            loaderLifecycle?.StopSubsystems();
            loaderLifecycle?.DeinitializeLoader();
        }

        private sealed class UnityXrLoaderLifecycle : IVLabXrLoaderLifecycle
        {
            private readonly MonoBehaviour host;
            private bool subsystemsStarted;
            private XRManagerSettings Manager => XRGeneralSettings.Instance == null ? null : XRGeneralSettings.Instance.Manager;

            public UnityXrLoaderLifecycle(MonoBehaviour host)
            {
                this.host = host;
            }

            public bool IsRuntimeAvailable => Manager != null && Manager.activeLoaders.Count > 0;

            public Task<bool> InitializeAsync(CancellationToken cancellationToken)
            {
                TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
                host.StartCoroutine(Initialize(completion, cancellationToken));
                return completion.Task;
            }

            private IEnumerator Initialize(TaskCompletionSource<bool> completion, CancellationToken cancellationToken)
            {
                XRManagerSettings manager = Manager;
                if (manager == null)
                {
                    completion.TrySetResult(false);
                    yield break;
                }

                if (manager.activeLoader == null)
                {
                    IEnumerator initialization = manager.InitializeLoader();
                    while (!cancellationToken.IsCancellationRequested && initialization.MoveNext())
                        yield return initialization.Current;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    DeinitializeLoader();
                    completion.TrySetCanceled(cancellationToken);
                    yield break;
                }
                completion.TrySetResult(manager.activeLoader != null);
            }

            public void StartSubsystems()
            {
                XRManagerSettings manager = Manager;
                if (manager == null || manager.activeLoader == null || subsystemsStarted)
                    return;
                manager.StartSubsystems();
                subsystemsStarted = true;
            }

            public void StopSubsystems()
            {
                XRManagerSettings manager = Manager;
                if (manager == null || manager.activeLoader == null || !subsystemsStarted)
                    return;
                manager.StopSubsystems();
                subsystemsStarted = false;
            }

            public void DeinitializeLoader()
            {
                XRManagerSettings manager = Manager;
                if (manager == null || manager.activeLoader == null)
                    return;
                StopSubsystems();
                manager.DeinitializeLoader();
            }
        }
    }
}
