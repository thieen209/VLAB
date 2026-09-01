using System.Collections;
using UnityEngine;
using VLAB.PhysicsLab.AirTrack;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Interaction;
using VLAB.PhysicsLab.Measurement;
using VLAB.PhysicsLab.Mechanics;
using VLAB.PhysicsLab.Oscillation;
using VLAB.PhysicsLab.Projectile;

namespace VLAB.PhysicsLab.Education
{
    public sealed class PhysicalStationWiring : MonoBehaviour
    {
        [SerializeField] private string experimentId;
        [SerializeField] private ExperimentPhysicalController controller;
        [SerializeField] private LabSnapController snapController;

        private FrictionBlock frictionBlock;
        private SpringForceMeter forceMeter;
        private bool forceMeterHeld;
        private float forceLinkRestDistance;

        public void Configure(string id, ExperimentPhysicalController physicalController, LabSnapController snap)
        {
            experimentId = id;
            controller = physicalController;
            snapController = snap;
        }

        private void Start()
        {
            snapController?.Refresh();
            ConfigureButtons();
            switch (experimentId)
            {
                case "PHY_01": ConfigurePendulum(); break;
                case "PHY_02": ConfigureProjectile(); break;
                case "PHY_03": ConfigureFriction(); break;
                case "PHY_04": ConfigurePhotogates(); break;
                case "PHY_05": ConfigureSpring(); break;
                case "PHY_06": ConfigureMomentum(); break;
            }
        }

        private void FixedUpdate()
        {
            if (!forceMeterHeld || frictionBlock == null || forceMeter == null)
            {
                return;
            }
            var offset = Vector3.ProjectOnPlane(forceMeter.transform.position - frictionBlock.transform.position, Vector3.up);
            var distance = offset.magnitude;
            var tension = Mathf.Clamp((distance - forceLinkRestDistance) * 22f, 0f, 10f);
            forceMeter.SetTension(tension);
            if (distance > 0.001f)
            {
                frictionBlock.ApplyPullForce(offset.normalized * tension);
            }
        }

        private void ConfigurePendulum()
        {
            var bob = GetComponentInChildren<PendulumBob>(true);
            var anchor = FindDeep(transform, "Pendulum_String_Anchor");
            var grabbable = bob != null ? bob.GetComponent<LabGrabbable>() : null;
            if (bob == null || anchor == null || grabbable == null) return;
            var length = Mathf.Clamp(Vector3.Distance(anchor.position, bob.transform.position), 0.25f, 1.5f);
            bob.Configure(anchor, length, bob.Body.mass);
            grabbable.Released += _ =>
            {
                bob.SetLength(Mathf.Clamp(Vector3.Distance(anchor.position, bob.transform.position), 0.25f, 1.5f));
                bob.Release();
                controller?.PublishPhysicalAction(bob, "Pendulum_Bob", PhysicalActionKind.ParameterAdjusted, bob.Length, "m");
            };
        }

        private void ConfigureProjectile()
        {
            var launcher = GetComponentInChildren<ProjectileLauncher>(true);
            var ball = GetComponentInChildren<ProjectileBall>(true);
            if (launcher == null || ball == null) return;
            var socket = FindDeep(launcher.transform, "ProjectileSocket");
            if (socket != null)
            {
                var point = socket.GetComponent<LabAttachmentPoint>() ?? socket.gameObject.AddComponent<LabAttachmentPoint>();
                point.Configure("projectile", true, true);
                point.Attached += attachment =>
                {
                    var projectile = attachment != null ? attachment.GetComponent<ProjectileBall>() : null;
                    if (projectile != null) launcher.LoadProjectile(projectile);
                };
                launcher.ProjectileLaunched += (_, __) =>
                {
                    if (point.Current != null) point.Detach();
                    controller?.PublishPhysicalAction(launcher, "Projectile_Launcher", PhysicalActionKind.TriggerPressed);
                };
            }
            ball.Landed += landed => controller?.RecordLiveMeasurement(
                landed, "Projectile", "Tầm xa", landed.RangeMetres, "m",
                $"angle={launcher.LaunchAngle:0.##}deg;speed={launcher.InitialVelocity:0.##}m/s");
        }

        private void ConfigureFriction()
        {
            frictionBlock = GetComponentInChildren<FrictionBlock>(true);
            forceMeter = GetComponentInChildren<SpringForceMeter>(true);
            if (frictionBlock == null || forceMeter == null) return;
            forceLinkRestDistance = Vector3.ProjectOnPlane(forceMeter.transform.position - frictionBlock.transform.position, Vector3.up).magnitude;
            var meterGrab = forceMeter.GetComponent<LabGrabbable>();
            if (meterGrab != null)
            {
                meterGrab.Grabbed += _ => forceMeterHeld = true;
                meterGrab.Released += _ => { forceMeterHeld = false; forceMeter.SetTension(0f); };
            }
            frictionBlock.SlidingStarted += force =>
            {
                controller?.PublishPhysicalAction(frictionBlock, "Friction_Block", PhysicalActionKind.SlidingStarted, force, "N");
                if (forceMeterHeld)
                {
                    controller?.RecordLiveMeasurement(
                        frictionBlock,
                        "Friction_Block",
                        "Lực ma sát",
                        force,
                        "N",
                        $"mass={frictionBlock.MassKilograms:0.###}kg;muK={frictionBlock.KineticFriction:0.###};pull={forceMeter.TensionNewtons:0.###}N");
                }
            };
            ConfigureMassMounts(
                frictionBlock.transform,
                frictionBlock.MassKilograms,
                mass => frictionBlock.SetParameters(mass, frictionBlock.StaticFriction, frictionBlock.KineticFriction));
            foreach (var selector in GetComponentsInChildren<FrictionSurfaceSelector>(true))
            {
                selector.Selected += selected => controller?.PublishPhysicalAction(
                    selected,
                    "Friction_Surface",
                    PhysicalActionKind.ParameterAdjusted,
                    selected.KineticCoefficient,
                    "mu");
            }
        }

        private void ConfigurePhotogates()
        {
            var timer = GetComponentInChildren<DigitalTimerMC964>(true);
            var gates = GetComponentsInChildren<PhysicsPhotogate>(true);
            if (timer == null || gates.Length == 0) return;
            timer.SetMode(gates.Length > 1 ? TimerMode.APlusB : TimerMode.A);
            for (var index = 0; index < gates.Length; index++)
            {
                var link = gates[index].gameObject.AddComponent<PhotogateTimerLink>();
                link.Configure(gates[index], timer, index == 0 ? TimerPort.A : TimerPort.B);
            }
            var track = GetComponentInChildren<PhysicsAirTrack>(true);
            foreach (var glider in GetComponentsInChildren<AirTrackGlider>(true))
            {
                glider.Configure(track, glider.MassKilograms);
                var capturedGlider = glider;
                ConfigureMassMounts(glider.transform, glider.MassKilograms, capturedGlider.SetMass);
            }
        }

        private void ConfigureSpring()
        {
            var spring = GetComponentInChildren<PhysicsCoilSpring>(true);
            if (spring == null) return;
            var point = spring.GetComponentInChildren<LabAttachmentPoint>(true);
            if (point == null) return;
            point.Attached += attachment =>
            {
                var body = attachment != null ? attachment.GetComponent<Rigidbody>() : null;
                if (body == null) return;
                body.isKinematic = false;
                body.useGravity = true;
                spring.AttachBody(body, attachment.transform);
            };
            point.Detached += _ => spring.DetachBody();
        }

        private void ConfigureMomentum()
        {
            ConfigurePhotogates();
            foreach (var collision in GetComponentsInChildren<GliderCollisionAttachment>(true))
            {
                var capturedCollision = collision;
                foreach (var point in collision.GetComponentsInChildren<LabAttachmentPoint>(true))
                {
                    if (!string.Equals(point.AcceptedType, "glider-collision", System.StringComparison.OrdinalIgnoreCase)) continue;
                    point.Attached += attachment => capturedCollision.SetMode(
                        attachment != null && attachment.name.IndexOf("Inelastic", System.StringComparison.OrdinalIgnoreCase) >= 0
                            ? GliderCollisionMode.Inelastic
                            : GliderCollisionMode.Elastic);
                    point.Detached += _ => capturedCollision.SetMode(GliderCollisionMode.Elastic);
                }
                collision.CollisionOccurred += (first, second) =>
                {
                    controller?.PublishPhysicalAction(first, "Air_Track_Glider", PhysicalActionKind.CollisionOccurred);
                    StartCoroutine(RecordMomentumAfterPhysics(first, second));
                };
            }
        }

        private static void ConfigureMassMounts(Transform host, float baseMass, System.Action<float> applyMass)
        {
            if (host == null || applyMass == null) return;
            var mounts = host.GetComponentsInChildren<LabAttachmentPoint>(true);
            void Recalculate()
            {
                var total = baseMass;
                foreach (var mount in mounts)
                {
                    if (!string.Equals(mount.AcceptedType, "mass", System.StringComparison.OrdinalIgnoreCase)) continue;
                    var physicalMass = mount.Current != null ? mount.Current.GetComponent<PhysicalMass>() : null;
                    if (physicalMass != null) total += physicalMass.MassKilograms;
                }
                applyMass(total);
            }

            foreach (var mount in mounts)
            {
                if (!string.Equals(mount.AcceptedType, "mass", System.StringComparison.OrdinalIgnoreCase)) continue;
                mount.Attached += _ => Recalculate();
                mount.Detached += _ => Recalculate();
            }
            Recalculate();
        }

        private IEnumerator RecordMomentumAfterPhysics(AirTrackGlider first, AirTrackGlider second)
        {
            yield return new WaitForFixedUpdate();
            var momentum = MomentumMath.TotalMomentum(first.MassKilograms, first.SignedVelocity, second.MassKilograms, second.SignedVelocity);
            controller?.RecordLiveMeasurement(first, "Momentum", "Tổng động lượng", momentum, "kg·m/s", $"m1={first.MassKilograms:0.###};m2={second.MassKilograms:0.###}");
        }

        private void ConfigureButtons()
        {
            foreach (var button in GetComponentsInChildren<InstrumentPushButton>(true))
            {
                button.Pressed += pressed => controller?.PublishPhysicalAction(pressed, pressed.name, PhysicalActionKind.TriggerPressed);
            }
            foreach (var handle in GetComponentsInChildren<LauncherAngleManipulator>(true))
            {
                handle.AngleChanged += angle => controller?.PublishPhysicalAction(handle, "Projectile_Launcher", PhysicalActionKind.ParameterAdjusted, angle, "deg");
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (var index = 0; index < root.childCount; index++)
            {
                var found = FindDeep(root.GetChild(index), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
