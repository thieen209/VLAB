using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using VLAB.Core.Input;

namespace VLAB.MainMenu
{
    /// <summary>Controls the installed Cardboard provider; mode changes rebuild only the main menu.</summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class VLabMobileVrMode : MonoBehaviour
    {
        private const string CardboardLoaderType = "Google.XR.Cardboard.XRLoader";
        public static VLabMobileVrMode Instance { get; private set; }
        public static bool Enabled => VLabHeadPose.ViewerRequested;
        public bool IsSwitching { get; private set; }
        public string Status { get; private set; } = "Chế độ kính VR khả dụng trên điện thoại Android.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            VLabHeadPose.SetViewerRequested(PlayerPrefs.GetInt(VLabHeadPose.ViewerPreferenceKey, 1) != 0);
            if (Instance == null) new GameObject("VLAB Mobile VR Mode").AddComponent<VLabMobileVrMode>();
#if UNITY_ANDROID && !UNITY_EDITOR
            // XR Management auto-starts before the splash screen. Apply saved off mode before scene rigs exist.
            var manager = XRGeneralSettings.Instance?.Manager;
            if (!Enabled)
            {
                if (Instance.TryStop(manager)) Instance.Status = "Màn hình thường: chạm để chọn, kéo vùng trống để nhìn.";
                else VLabHeadPose.SetViewerRequested(IsCardboardRunning(manager));
            }
            else if (!IsCardboardRunning(manager))
            {
                Instance.TryStop(manager);
                VLabHeadPose.SetViewerRequested(false);
                Instance.Status = "Cardboard chưa sẵn sàng. Đang dùng màn hình thường; có thể thử bật VR trong Cài đặt.";
            }
            else Instance.Status = "Cardboard đang chạy: hiển thị hai mắt và theo dõi đầu từ thiết bị.";
#endif
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public static bool CanSwitch(RuntimePlatform platform, string sceneName) =>
            platform == RuntimePlatform.Android && string.Equals(sceneName, VLABMenuBootstrap.MenuScene, StringComparison.Ordinal);

        public void Request(bool enabled)
        {
            if (IsSwitching) return;
            if (!CanSwitch(Application.platform, SceneManager.GetActiveScene().name))
            {
                Status = Application.platform != RuntimePlatform.Android
                    ? "Chế độ kính VR chỉ hoạt động trên điện thoại Android."
                    : "Hãy về trang chính trước khi đổi chế độ VR để giữ nguyên thí nghiệm.";
                return;
            }
            StartCoroutine(SetEnabled(enabled));
        }

        public IEnumerator SetEnabled(bool enabled)
        {
            if (IsSwitching) yield break;
            if (!CanSwitch(Application.platform, SceneManager.GetActiveScene().name))
            { Status = "Chỉ đổi chế độ VR tại trang chính trên điện thoại Android."; yield break; }
            if (!Application.CanStreamedLevelBeLoaded(VLABMenuBootstrap.MenuScene))
            { Status = "Không tìm thấy trang chính để khởi tạo lại giao diện."; yield break; }
            IsSwitching = true;
            try
            {
                Status = enabled ? "Đang khởi động Cardboard…" : "Đang chuyển về màn hình thường…";
                var manager = XRGeneralSettings.Instance?.Manager;
                if (enabled)
                {
                    if (manager == null) { Status = "Bản dựng chưa có cấu hình XR cho Android."; yield break; }
                    if (manager.activeLoader != null && !IsCardboardLoader(manager))
                    { Status = "Nhà cung cấp XR hiện tại không phải Cardboard; chưa thay đổi chế độ."; yield break; }
                    if (manager.activeLoader == null)
                    {
                        var configured = false;
                        foreach (var loader in manager.activeLoaders)
                            if (loader != null && loader.GetType().FullName == CardboardLoaderType) configured = true;
                        if (!configured) { Status = "Bản dựng chưa bật Cardboard trong cấu hình Android XR."; yield break; }
                        // XR Management requires graphics initialization to finish before manual loader startup.
                        yield return null;
                        var initializer = manager.InitializeLoader();
                        var deadline = Time.realtimeSinceStartup + 10;
                        bool next;
                        do
                        {
                            if (!TryAdvance(initializer, out next)) { TryStop(manager); yield break; }
                            if (!next) break;
                            if (Time.realtimeSinceStartup > deadline)
                            { Status = "Khởi tạo Cardboard quá thời gian. Hãy thử lại."; TryStop(manager); yield break; }
                            yield return initializer.Current;
                        } while (next);
                    }
                    if (!manager.isInitializationComplete || !IsCardboardLoader(manager))
                    { Status = "Không khởi tạo được nhà cung cấp Cardboard."; TryStop(manager); yield break; }
                    if (!TryAction(manager.StartSubsystems)) { TryStop(manager); yield break; }
                    var runningDeadline = Time.realtimeSinceStartup + 3;
                    while (!IsCardboardRunning(manager) && Time.realtimeSinceStartup < runningDeadline) yield return null;
                    if (!IsCardboardRunning(manager))
                    { Status = "Cardboard chưa tạo được hiển thị hai mắt và đầu vào theo dõi đầu."; TryStop(manager); yield break; }
                }
                else if (!TryStop(manager)) yield break;

                VLabHeadPose.SetViewerRequested(enabled);
                PlayerPrefs.SetInt(VLabHeadPose.ViewerPreferenceKey, enabled ? 1 : 0); PlayerPrefs.Save();
                Status = enabled ? "Đã bật Cardboard. Đang mở lại trang chính…" : "Đã tắt VR. Đang mở lại trang chính…";
                // Rig and input modules are scene-owned; a fresh Menu safely removes gaze/pose overrides.
                AsyncOperation reload = null;
                if (!TryAction(() => reload = SceneManager.LoadSceneAsync(VLABMenuBootstrap.MenuScene, LoadSceneMode.Single))) yield break;
                if (reload != null) yield return reload;
                Status = enabled ? "Cardboard đang chạy: hai mắt và theo dõi đầu." : "Màn hình thường: chạm để chọn, kéo vùng trống để nhìn.";
            }
            finally { IsSwitching = false; }
        }

        private static bool IsCardboardLoader(XRManagerSettings manager) =>
            manager?.activeLoader != null && manager.activeLoader.GetType().FullName == CardboardLoaderType;

        private static bool IsCardboardRunning(XRManagerSettings manager)
        {
            if (!IsCardboardLoader(manager)) return false;
            var display = manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            var input = manager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>();
            return display != null && display.running && input != null && input.running;
        }

        private bool TryStop(XRManagerSettings manager)
        {
            if (manager == null || manager.activeLoader == null) return true;
            if (!IsCardboardLoader(manager)) { Status = "Nhà cung cấp XR hiện tại không phải Cardboard."; return false; }
            if (!manager.isInitializationComplete) { Status = "Cardboard chưa hoàn tất khởi tạo. Hãy thử lại."; return false; }
            return TryAction(() => { manager.StopSubsystems(); manager.DeinitializeLoader(); });
        }

        private bool TryAction(Action action)
        {
            try { action(); return true; }
            catch (Exception error)
            { Status = "Không thể thay đổi chế độ Cardboard. Hãy thử lại hoặc khởi động lại ứng dụng."; Debug.LogException(error); return false; }
        }
        private bool TryAdvance(IEnumerator operation, out bool next)
        {
            try { next = operation.MoveNext(); return true; }
            catch (Exception error)
            { next = false; Status = "Khởi tạo Cardboard không thành công."; Debug.LogException(error); return false; }
        }
    }
}
