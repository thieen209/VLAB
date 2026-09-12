using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VLAB.ChemistryLab.Mode
{
    public enum VLabPresentationMode
    {
        Desktop,
        XRSimulator,
        OpenXRHardware
    }

    public enum VLabModeRequest
    {
        Auto,
        Desktop,
        XRSimulator,
        OpenXRHardware
    }

    public enum VLabModeRequestSource
    {
        DefaultAuto,
        LaunchMenu,
        PersistedChoice,
        CommandLine
    }

    public enum VLabModeFailureReason
    {
        None,
        InvalidCommandLine,
        SimulatorUnavailable,
        ValidationBlocked,
        NoRuntime,
        LoaderInitializationFailed,
        Timeout,
        Cancelled
    }

    public readonly struct VLabModeRequestResolution
    {
        public VLabModeRequestResolution(VLabModeRequest request, VLabModeRequestSource source, VLabModeFailureReason failureReason)
        {
            Request = request;
            Source = source;
            FailureReason = failureReason;
        }

        public VLabModeRequest Request { get; }
        public VLabModeRequestSource Source { get; }
        public VLabModeFailureReason FailureReason { get; }
    }

    public static class VLabModeRequestResolver
    {
        public static VLabModeRequestResolution Resolve(
            IReadOnlyList<string> commandLine,
            VLabModeRequest? persistedChoice,
            VLabModeRequest? launchMenuChoice)
        {
            if (commandLine != null)
            {
                for (int index = 0; index < commandLine.Count; index++)
                {
                    if (!string.Equals(commandLine[index], "-vlabMode", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (index + 1 >= commandLine.Count || !TryParse(commandLine[index + 1], out VLabModeRequest request))
                        return new VLabModeRequestResolution(VLabModeRequest.Desktop, VLabModeRequestSource.CommandLine, VLabModeFailureReason.InvalidCommandLine);
                    return new VLabModeRequestResolution(request, VLabModeRequestSource.CommandLine, VLabModeFailureReason.None);
                }
            }

            if (persistedChoice == VLabModeRequest.Desktop || persistedChoice == VLabModeRequest.OpenXRHardware)
                return new VLabModeRequestResolution(persistedChoice.Value, VLabModeRequestSource.PersistedChoice, VLabModeFailureReason.None);
            if (launchMenuChoice.HasValue && launchMenuChoice.Value != VLabModeRequest.Auto)
                return new VLabModeRequestResolution(launchMenuChoice.Value, VLabModeRequestSource.LaunchMenu, VLabModeFailureReason.None);
            return new VLabModeRequestResolution(VLabModeRequest.Auto, VLabModeRequestSource.DefaultAuto, VLabModeFailureReason.None);
        }

        private static bool TryParse(string value, out VLabModeRequest request)
        {
            if (string.Equals(value, "desktop", StringComparison.OrdinalIgnoreCase))
                request = VLabModeRequest.Desktop;
            else if (string.Equals(value, "hardware", StringComparison.OrdinalIgnoreCase))
                request = VLabModeRequest.OpenXRHardware;
            else if (string.Equals(value, "simulator", StringComparison.OrdinalIgnoreCase))
                request = VLabModeRequest.XRSimulator;
            else if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
                request = VLabModeRequest.Auto;
            else
            {
                request = VLabModeRequest.Desktop;
                return false;
            }
            return true;
        }
    }

    public interface IVLabXrLoaderLifecycle
    {
        bool IsRuntimeAvailable { get; }
        Task<bool> InitializeAsync(CancellationToken cancellationToken);
        void StartSubsystems();
        void StopSubsystems();
        void DeinitializeLoader();
    }

    public sealed class VLabPresentationSession
    {
        public VLabPresentationSession(object sharedSimulationState)
        {
            SharedSimulationState = sharedSimulationState ?? throw new ArgumentNullException(nameof(sharedSimulationState));
            CurrentMode = VLabPresentationMode.Desktop;
        }

        public object SharedSimulationState { get; }
        public VLabPresentationMode CurrentMode { get; internal set; }
        public VLabModeFailureReason LastFailureReason { get; internal set; }
    }

    public readonly struct VLabModeTransitionResult
    {
        public VLabModeTransitionResult(VLabPresentationMode mode, VLabModeFailureReason failureReason, object sharedSimulationState)
        {
            Mode = mode;
            FailureReason = failureReason;
            SharedSimulationState = sharedSimulationState;
        }

        public VLabPresentationMode Mode { get; }
        public VLabModeFailureReason FailureReason { get; }
        public object SharedSimulationState { get; }
        public bool FellBackToDesktop => Mode == VLabPresentationMode.Desktop && FailureReason != VLabModeFailureReason.None;
    }

    public sealed class VLabPresentationModeCoordinator
    {
        private readonly IVLabXrLoaderLifecycle loader;
        private readonly Func<TimeSpan, CancellationToken, Task> delay;

        public VLabPresentationModeCoordinator(
            VLabPresentationSession session,
            IVLabXrLoaderLifecycle loader,
            Func<TimeSpan, CancellationToken, Task> delay = null)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            this.delay = delay ?? Task.Delay;
        }

        public VLabPresentationSession Session { get; }

        public async Task<VLabModeTransitionResult> ActivateAsync(
            VLabModeRequest request,
            bool simulatorAllowed,
            bool validationAllowsHardware,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            ShutdownXr();
            Apply(VLabPresentationMode.Desktop, VLabModeFailureReason.None);
            if (request == VLabModeRequest.Desktop)
                return Result();
            if (request == VLabModeRequest.XRSimulator)
            {
                Apply(simulatorAllowed ? VLabPresentationMode.XRSimulator : VLabPresentationMode.Desktop,
                    simulatorAllowed ? VLabModeFailureReason.None : VLabModeFailureReason.SimulatorUnavailable);
                return Result();
            }
            if (!validationAllowsHardware)
            {
                Apply(VLabPresentationMode.Desktop, VLabModeFailureReason.ValidationBlocked);
                return Result();
            }
            if (!loader.IsRuntimeAvailable)
            {
                Apply(VLabPresentationMode.Desktop, VLabModeFailureReason.NoRuntime);
                return Result();
            }
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            using CancellationTokenSource loaderCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task<bool> initializeTask;
            try
            {
                initializeTask = loader.InitializeAsync(loaderCancellation.Token);
            }
            catch
            {
                Apply(VLabPresentationMode.Desktop, VLabModeFailureReason.LoaderInitializationFailed);
                return Result();
            }

            Task timeoutTask = delay(timeout, cancellationToken);
            Task completed;
            try
            {
                completed = await Task.WhenAny(initializeTask, timeoutTask);
            }
            catch (OperationCanceledException)
            {
                Apply(VLabPresentationMode.Desktop, VLabModeFailureReason.Cancelled);
                return Result();
            }

            if (completed == timeoutTask)
            {
                loaderCancellation.Cancel();
                ShutdownXr();
                Apply(VLabPresentationMode.Desktop, cancellationToken.IsCancellationRequested ? VLabModeFailureReason.Cancelled : VLabModeFailureReason.Timeout);
                return Result();
            }

            try
            {
                bool initialized = await initializeTask;
                if (initialized)
                {
                    loader.StartSubsystems();
                    Apply(VLabPresentationMode.OpenXRHardware, VLabModeFailureReason.None);
                }
                else
                {
                    ShutdownXr();
                    Apply(VLabPresentationMode.Desktop, VLabModeFailureReason.LoaderInitializationFailed);
                }
            }
            catch (OperationCanceledException)
            {
                ShutdownXr();
                Apply(VLabPresentationMode.Desktop, VLabModeFailureReason.Cancelled);
            }
            catch
            {
                ShutdownXr();
                Apply(VLabPresentationMode.Desktop, VLabModeFailureReason.LoaderInitializationFailed);
            }
            return Result();
        }

        private void ShutdownXr()
        {
            loader.StopSubsystems();
            loader.DeinitializeLoader();
        }

        private void Apply(VLabPresentationMode mode, VLabModeFailureReason failureReason)
        {
            Session.CurrentMode = mode;
            Session.LastFailureReason = failureReason;
        }

        private VLabModeTransitionResult Result() =>
            new VLabModeTransitionResult(Session.CurrentMode, Session.LastFailureReason, Session.SharedSimulationState);
    }
}
