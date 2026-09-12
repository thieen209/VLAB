using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using VLAB.Core.Input;

namespace VLAB.MainMenu
{
    /// <summary>Applies the same comfort preferences to installed XRI locomotion providers.</summary>
    public sealed class VLabNativeComfort : MonoBehaviour
    {
        private ContinuousMoveProvider[] moves = new ContinuousMoveProvider[0];
        private SnapTurnProvider[] snaps = new SnapTurnProvider[0];
        private ContinuousTurnProvider[] smooth = new ContinuousTurnProvider[0];
        private InputManager input;
        private float movementSpeed = float.NaN, snapAngle = float.NaN, turnSpeed = float.NaN;
        private bool appliedMode, modeInitialized, menuLocked;

        public void Configure(Camera camera, InputManager sharedInput = null)
        {
            if (camera == null) return;
            input = sharedInput;
            menuLocked = gameObject.scene.name == VLABMenuBootstrap.MenuScene;
            var origin = camera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            var root = origin != null ? origin.transform : camera.transform.root;
            moves = root.GetComponentsInChildren<ContinuousMoveProvider>(true);
            snaps = root.GetComponentsInChildren<SnapTurnProvider>(true);
            smooth = root.GetComponentsInChildren<ContinuousTurnProvider>(true);
            // Some rigs serialize only one turn style; reuse its mediator and exact action readers.
            if (snaps.Length == 1 && smooth.Length == 0)
            {
                var source = snaps[0];
                var target = source.gameObject.AddComponent<ContinuousTurnProvider>();
                target.enabled = false;
                target.mediator = source.mediator;
                target.transformationPriority = source.transformationPriority;
                target.leftHandTurnInput = source.leftHandTurnInput;
                target.rightHandTurnInput = source.rightHandTurnInput;
                smooth = new[] { target };
            }
            else if (smooth.Length == 1 && snaps.Length == 0)
            {
                var source = smooth[0];
                var target = source.gameObject.AddComponent<SnapTurnProvider>();
                target.enabled = false;
                target.mediator = source.mediator;
                target.transformationPriority = source.transformationPriority;
                target.leftHandTurnInput = source.leftHandTurnInput;
                target.rightHandTurnInput = source.rightHandTurnInput;
                snaps = new[] { target };
            }
            movementSpeed = snapAngle = turnSpeed = float.NaN;
            modeInitialized = false;
            ApplySettings();
        }

        private void Update() => ApplySettings();

        public void ApplySettings()
        {
            var settings = VLabComfortSettings.Current;
            if (movementSpeed != settings.movementSpeed)
            {
                foreach (var provider in moves) if (provider != null) provider.moveSpeed = settings.movementSpeed;
                movementSpeed = settings.movementSpeed;
            }
            if (snapAngle != settings.snapAngle)
            {
                foreach (var provider in snaps) if (provider != null) provider.turnAmount = settings.snapAngle;
                snapAngle = settings.snapAngle;
            }
            if (turnSpeed != settings.turnSpeed)
            {
                foreach (var provider in smooth) if (provider != null) provider.turnSpeed = settings.turnSpeed;
                turnSpeed = settings.turnSpeed;
            }
            if (modeInitialized && appliedMode == settings.smoothTurn) return;
            if (menuLocked || Time.timeScale <= 0 || (input != null && (input.BlockExperimentInput || input.TranslationLocked))) return;
            // Disabled providers may belong to a pause/tutorial lock. Wait for their owner to restore one.
            bool anyEnabled = false;
            foreach (var provider in snaps) anyEnabled |= provider != null && provider.isActiveAndEnabled;
            foreach (var provider in smooth) anyEnabled |= provider != null && provider.isActiveAndEnabled;
            if (!anyEnabled || snaps.Length == 0 || smooth.Length == 0) return;
            if (settings.smoothTurn)
            {
                foreach (var provider in snaps) if (provider != null) provider.enabled = false;
                foreach (var provider in smooth) if (provider != null) provider.enabled = true;
            }
            else
            {
                foreach (var provider in smooth) if (provider != null) provider.enabled = false;
                foreach (var provider in snaps) if (provider != null) provider.enabled = true;
            }
            appliedMode = settings.smoothTurn;
            modeInitialized = true;
        }
    }
}
