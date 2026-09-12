using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VLAB.ChemistryLab.Interaction
{
    [DisallowMultipleComponent]
    public sealed class LabLiquidVessel : MonoBehaviour
    {
        private static readonly List<LabLiquidVessel> active = new List<LabLiquidVessel>();
        [SerializeField] private HandsOnTitration station;
        [SerializeField] private LabVesselRole role;
        [SerializeField] private LabReagent reagent;
        [SerializeField] private float capacityMl = 250;
        [SerializeField] private float initialMl;
        [SerializeField] private Transform mouth;
        [SerializeField] private Transform outlet;
        [SerializeField] private Transform liquidVisual;
        [SerializeField] private GameObject cap;
        [SerializeField] private TextMesh readout;
        [SerializeField] private LineRenderer stream;
        [SerializeField] private float mouthRadius = .035f;
        private XRGrabInteractable grab;
        [SerializeField] private Vector3 visualScale;
        [SerializeField] private Vector3 visualPosition;
        private bool open;
        private float nextDrop;
        private float streamUntil;
        private readonly RaycastHit[] hits = new RaycastHit[24];
        public HandsOnTitration Station => station;
        public LabVesselRole Role => role;
        public LabLiquidState Liquid { get; private set; }
        public bool IsOpen => open;
        public bool RinseLoaded { get; set; }
        public Transform Mouth => mouth;
        public bool IsHeld => grab != null && grab.isSelected;
        public string DisplayName => role == LabVesselRole.Pipette ? "Pipette 10 mL" :
            role == LabVesselRole.Flask ? "Bình mẫu" : role == LabVesselRole.Waste ? "Bình thải" :
            role == LabVesselRole.Burette ? "Burette" : reagent == LabReagent.NaOH ? "NaOH 0.100 M" :
            reagent == LabReagent.DilutedVinegar ? "Giấm pha loãng" : "Chỉ thị";
        public void Configure(HandsOnTitration owner, LabVesselRole vesselRole, LabReagent identity,
            float capacity, float initial, Transform opening, Transform tip, Transform liquid,
            GameObject lid, TextMesh label, LineRenderer line, float radius)
        {
            station = owner; role = vesselRole; reagent = identity; capacityMl = capacity; initialMl = initial;
            mouth = opening; outlet = tip; liquidVisual = liquid; cap = lid; readout = label; stream = line; mouthRadius = radius;
            if (liquidVisual != null) { visualScale = liquidVisual.localScale; visualPosition = liquidVisual.localPosition; }
            ResetContents();
        }
        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            // Instantaneous tracking writes the Transform through static colliders.
            // Apply to saved scenes as well as newly generated vessels.
            if (grab != null)
            {
                grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                // Thin glassware needs bounded tracking velocities to avoid rotational oscillation.
                grab.limitLinearVelocity = true;
                grab.maxLinearVelocityDelta = 3f;
                grab.limitAngularVelocity = true;
                grab.maxAngularVelocityDelta = 6f;
                var body = GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    body.maxAngularVelocity = 6f;
                }
            }
            ResetContents();
        }
        private void OnEnable()
        {
            if (GetComponent<LabPourGuide>() == null) gameObject.AddComponent<LabPourGuide>();
            active.Add(this);
            grab = GetComponent<XRGrabInteractable>();
            if (grab != null) grab.activated.AddListener(OnActivated);
        }
        private void OnDisable()
        {
            active.Remove(this);
            if (grab != null) grab.activated.RemoveListener(OnActivated);
            if (stream != null) stream.enabled = false;
        }
        private void OnActivated(ActivateEventArgs _) => Use();
        public void ResetContents()
        {
            Liquid = new LabLiquidState(capacityMl, reagent, initialMl);
            open = cap == null;
            RinseLoaded = false;
            nextDrop = streamUntil = 0;
            if (cap != null) cap.SetActive(true);
            if (stream != null) stream.enabled = false;
            RefreshVisual();
        }
        public void Use()
        {
            if (!IsHeld) return;
            if (cap != null) { open = !open; cap.SetActive(!open); station.Show(open ? "Nắp mở. Nghiêng chai trên miệng bình để rót." : "Đã đóng nắp."); return; }
            if (role != LabVesselRole.Pipette) return;
            if (Liquid.VolumeMl > 0)
            {
                var receiver = ReceiverBelow(outlet.position, .22f);
                if (receiver != null) station.Transfer(this, receiver, Liquid.VolumeMl);
                else station.Show("Đưa đầu pipette vào trên miệng bình tam giác rồi bóp bóng hút.");
                return;
            }
            foreach (var candidate in active)
            {
                if (candidate.station != station || candidate.role != LabVesselRole.Bottle || !candidate.open) continue;
                Vector3 delta = outlet.position - candidate.mouth.position;
                if (Vector3.ProjectOnPlane(delta, Vector3.up).magnitude < candidate.mouthRadius && delta.y < .025f && delta.y > -.16f)
                { station.Transfer(candidate, this, capacityMl); return; }
            }
            station.Show("Nhúng đầu pipette vào chai giấm đã mở nắp, rồi bóp trigger để hút 10 mL.");
        }
        private void Update()
        {
            if (stream != null) stream.enabled = Time.time < streamUntil;
            if (!open || Liquid.VolumeMl <= 0 || role == LabVesselRole.Pipette || role == LabVesselRole.Burette) return;
            float tilt = Vector3.Dot(transform.up, Vector3.up);
            if (tilt > .35f) return;
            if (role == LabVesselRole.Bottle && reagent == LabReagent.Indicator)
            {
                if (Time.time < nextDrop) return;
                nextDrop = Time.time + .4f;
                Pour(.05, outlet.position);
            }
            else Pour(Time.deltaTime * 12f * Mathf.Clamp01((.35f - tilt) / .65f), outlet.position);
        }
        public double Pour(double amount, Vector3 origin)
        {
            if (!open || Liquid == null || Liquid.VolumeMl <= 0) return 0;
            var receiver = ReceiverBelow(origin, .45f);
            double moved = 0;
            if (receiver != null) moved = station.Transfer(this, receiver, amount);
            else station.Spill(this, amount);
            if (stream != null && (moved > 0 || receiver == null))
            {
                stream.enabled = true;
                streamUntil = Time.time + .10f;
                stream.SetPosition(0, origin);
                stream.SetPosition(1, LandingPoint(origin, receiver));
            }
            return moved;
        }

        public LabLiquidVessel PreviewPour(out Vector3 origin, out Vector3 destination)
        {
            origin = outlet != null ? outlet.position : transform.position;
            var receiver = ReceiverBelow(origin, role == LabVesselRole.Pipette ? .22f : .45f);
            destination = LandingPoint(origin, receiver);
            return receiver;
        }

        private Vector3 LandingPoint(Vector3 origin, LabLiquidVessel receiver)
        {
            if (receiver != null) return new Vector3(origin.x, receiver.mouth.position.y, origin.z);
            float distance = 1.5f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (hits[i].collider.GetComponentInParent<LabLiquidVessel>() != this)
                    distance = Mathf.Min(distance, hits[i].distance);
            return origin + Vector3.down * distance;
        }
        public LabLiquidVessel ReceiverBelow(Vector3 origin, float distance)
        {
            LabLiquidVessel closest = null;
            float nearest = distance;
            foreach (var candidate in active)
            {
                if (candidate == this || candidate.station != station || !candidate.open || candidate.role == LabVesselRole.Pipette ||
                    Vector3.Dot(candidate.transform.up, Vector3.up) < .8f) continue;
                Vector3 delta = origin - candidate.mouth.position;
                if (delta.y < 0 || delta.y > nearest || Vector3.ProjectOnPlane(delta, Vector3.up).magnitude > candidate.mouthRadius) continue;
                Vector3 direction = candidate.mouth.position - origin;
                int count = Physics.RaycastNonAlloc(origin, direction.normalized, hits, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = count == hits.Length;
                for (int i = 0; i < count; i++)
                {
                    var owner = hits[i].collider.GetComponentInParent<LabLiquidVessel>();
                    if (owner != this && owner != candidate) { blocked = true; break; }
                }
                if (blocked) continue;
                closest = candidate; nearest = delta.y;
            }
            return closest;
        }
        public void RefreshVisual()
        {
            if (Liquid == null) return;
            if (liquidVisual != null)
            {
                float fraction = (float)(Liquid.VolumeMl / Liquid.CapacityMl);
                liquidVisual.gameObject.SetActive(fraction > 0);
                liquidVisual.localScale = new Vector3(visualScale.x, visualScale.y * fraction, visualScale.z);
                liquidVisual.localPosition = visualPosition - Vector3.up * visualScale.y * (1 - fraction) * .5f;
            }
            if (readout != null) readout.text = Liquid.VolumeMl.ToString("F2") + " / " + capacityMl.ToString("F0") + " mL";
        }
    }
}
