using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VLAB.ChemistryLab.Mode;

namespace VLAB.ChemistryLab.Tests
{
    public sealed class PresentationModeCoordinatorTests
    {
        [Test]
        public void Resolver_CommandLineOverridesPersistedAndLaunchChoices()
        {
            VLabModeRequestResolution result = VLabModeRequestResolver.Resolve(
                new[] { "app.exe", "-vlabMode", "simulator" },
                VLabModeRequest.Desktop,
                VLabModeRequest.OpenXRHardware);

            Assert.That(result.Request, Is.EqualTo(VLabModeRequest.XRSimulator));
            Assert.That(result.Source, Is.EqualTo(VLabModeRequestSource.CommandLine));
        }

        [Test]
        public void Resolver_PersistedExplicitChoicePrecedesLaunchMenu()
        {
            VLabModeRequestResolution result = VLabModeRequestResolver.Resolve(
                Array.Empty<string>(), VLabModeRequest.OpenXRHardware, VLabModeRequest.Desktop);

            Assert.That(result.Request, Is.EqualTo(VLabModeRequest.OpenXRHardware));
            Assert.That(result.Source, Is.EqualTo(VLabModeRequestSource.PersistedChoice));
        }

        [Test]
        public void Resolver_InvalidCommandLineFallsBackToDesktopWithReason()
        {
            VLabModeRequestResolution result = VLabModeRequestResolver.Resolve(
                new[] { "-vlabMode", "unknown" }, null, null);

            Assert.That(result.Request, Is.EqualTo(VLabModeRequest.Desktop));
            Assert.That(result.FailureReason, Is.EqualTo(VLabModeFailureReason.InvalidCommandLine));
        }

        [Test]
        public async Task Desktop_DoesNotInitializeXr()
        {
            FakeLoader loader = new FakeLoader(true, Task.FromResult(true));
            VLabModeTransitionResult result = await CreateCoordinator(loader).ActivateAsync(
                VLabModeRequest.Desktop, true, true, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.That(result.Mode, Is.EqualTo(VLabPresentationMode.Desktop));
            Assert.That(loader.InitializeCalls, Is.Zero);
        }

        [Test]
        public async Task Simulator_IsRejectedOutsideEditorOrDevelopmentContext()
        {
            FakeLoader loader = new FakeLoader(true, Task.FromResult(true));
            VLabModeTransitionResult result = await CreateCoordinator(loader).ActivateAsync(
                VLabModeRequest.XRSimulator, false, true, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.That(result.Mode, Is.EqualTo(VLabPresentationMode.Desktop));
            Assert.That(result.FailureReason, Is.EqualTo(VLabModeFailureReason.SimulatorUnavailable));
            Assert.That(loader.InitializeCalls, Is.Zero);
        }

        [Test]
        public async Task Hardware_UsesDesktopFallbackWhenRuntimeIsMissing()
        {
            FakeLoader loader = new FakeLoader(false, Task.FromResult(true));
            VLabModeTransitionResult result = await CreateCoordinator(loader).ActivateAsync(
                VLabModeRequest.OpenXRHardware, true, true, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.That(result.Mode, Is.EqualTo(VLabPresentationMode.Desktop));
            Assert.That(result.FailureReason, Is.EqualTo(VLabModeFailureReason.NoRuntime));
        }

        [Test]
        public async Task Hardware_IsBlockedByMandatoryValidationError()
        {
            FakeLoader loader = new FakeLoader(true, Task.FromResult(true));
            VLabModeTransitionResult result = await CreateCoordinator(loader).ActivateAsync(
                VLabModeRequest.OpenXRHardware, true, false, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.That(result.FailureReason, Is.EqualTo(VLabModeFailureReason.ValidationBlocked));
            Assert.That(loader.InitializeCalls, Is.Zero);
        }

        [Test]
        public async Task Hardware_ActivatesAfterLoaderSuccess()
        {
            FakeLoader loader = new FakeLoader(true, Task.FromResult(true));
            VLabModeTransitionResult result = await CreateCoordinator(loader).ActivateAsync(
                VLabModeRequest.OpenXRHardware, true, true, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.That(result.Mode, Is.EqualTo(VLabPresentationMode.OpenXRHardware));
            Assert.That(result.FailureReason, Is.EqualTo(VLabModeFailureReason.None));
            Assert.That(loader.StartCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task Hardware_ReturnsReasonWhenLoaderFails()
        {
            FakeLoader loader = new FakeLoader(true, Task.FromResult(false));
            VLabModeTransitionResult result = await CreateCoordinator(loader).ActivateAsync(
                VLabModeRequest.OpenXRHardware, true, true, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.That(result.Mode, Is.EqualTo(VLabPresentationMode.Desktop));
            Assert.That(result.FailureReason, Is.EqualTo(VLabModeFailureReason.LoaderInitializationFailed));
            Assert.That(loader.DeinitializeCalls, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public async Task Hardware_TimesOutAndCancelsLoader()
        {
            TaskCompletionSource<bool> neverCompletes = new TaskCompletionSource<bool>();
            FakeLoader loader = new FakeLoader(true, neverCompletes.Task);
            VLabPresentationModeCoordinator coordinator = CreateCoordinator(loader, (_, __) => Task.CompletedTask);

            VLabModeTransitionResult result = await coordinator.ActivateAsync(
                VLabModeRequest.Auto, true, true, TimeSpan.FromMilliseconds(1), CancellationToken.None);

            Assert.That(result.Mode, Is.EqualTo(VLabPresentationMode.Desktop));
            Assert.That(result.FailureReason, Is.EqualTo(VLabModeFailureReason.Timeout));
            Assert.That(loader.LastToken.IsCancellationRequested, Is.True);
            Assert.That(loader.StopCalls, Is.GreaterThanOrEqualTo(2));
            Assert.That(loader.DeinitializeCalls, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public async Task ModeTransition_PreservesSharedExperimentStateReference()
        {
            object sharedState = new object();
            FakeLoader loader = new FakeLoader(true, Task.FromResult(true));
            VLabPresentationSession session = new VLabPresentationSession(sharedState);
            VLabPresentationModeCoordinator coordinator = new VLabPresentationModeCoordinator(session, loader);

            VLabModeTransitionResult result = await coordinator.ActivateAsync(
                VLabModeRequest.OpenXRHardware, true, true, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.That(result.SharedSimulationState, Is.SameAs(sharedState));
            Assert.That(session.SharedSimulationState, Is.SameAs(sharedState));
        }

        private static VLabPresentationModeCoordinator CreateCoordinator(
            FakeLoader loader,
            Func<TimeSpan, CancellationToken, Task> delay = null) =>
            new VLabPresentationModeCoordinator(new VLabPresentationSession(new object()), loader, delay);

        private sealed class FakeLoader : IVLabXrLoaderLifecycle
        {
            private readonly Task<bool> initialization;

            public FakeLoader(bool runtimeAvailable, Task<bool> initialization)
            {
                IsRuntimeAvailable = runtimeAvailable;
                this.initialization = initialization;
            }

            public bool IsRuntimeAvailable { get; }
            public int InitializeCalls { get; private set; }
            public CancellationToken LastToken { get; private set; }
            public int StartCalls { get; private set; }
            public int StopCalls { get; private set; }
            public int DeinitializeCalls { get; private set; }

            public Task<bool> InitializeAsync(CancellationToken cancellationToken)
            {
                InitializeCalls++;
                LastToken = cancellationToken;
                return initialization;
            }

            public void StartSubsystems() => StartCalls++;
            public void StopSubsystems() => StopCalls++;
            public void DeinitializeLoader() => DeinitializeCalls++;
        }
    }
}
