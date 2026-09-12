using UnityEngine;

namespace VLAB.ChemistryLab.Interaction
{
    /// <summary>Geometric pouring aid, not permission to skip lesson steps or a fluid simulation.</summary>
    [DisallowMultipleComponent]
    public sealed class LabPourGuide : MonoBehaviour
    {
        private LabLiquidVessel vessel;
        private LineRenderer path, marker;
        private Material material;
        public string Hint { get; private set; }
        public bool Visible => path != null && path.enabled;

        private void Awake()
        {
            vessel = GetComponent<LabLiquidVessel>();
            material = new Material(Shader.Find("Sprites/Default"));
            path = MakeLine("Pour aim path", 2, .0015f);
            marker = MakeLine("Pour landing ring", 33, .003f);
            Hide();
        }

        private LineRenderer MakeLine(string label, int points, float width)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = points;
            line.startWidth = line.endWidth = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void LateUpdate()
        {
            if (!LabPreferences.Current.pourGuide || vessel == null || vessel.Liquid == null || !vessel.IsOpen || vessel.Liquid.VolumeMl <= 0 ||
                (!vessel.IsHeld && vessel.Role != LabVesselRole.Burette)) { Hide(); return; }
            var receiver = vessel.PreviewPour(out var origin, out var destination);
            bool aligned = receiver != null;
            Color color = aligned ? new Color(.2f, 1f, .65f) : new Color(1f, .55f, .1f);
            path.enabled = marker.enabled = true;
            path.startColor = path.endColor = marker.startColor = marker.endColor = color;
            path.SetPosition(0, origin);
            path.SetPosition(1, destination);
            for (int i = 0; i < 33; i++)
            {
                float angle = i * Mathf.PI * 2 / 32;
                marker.SetPosition(i, destination + new Vector3(Mathf.Cos(angle) * .025f, .003f, Mathf.Sin(angle) * .025f));
            }
            Hint = aligned ? "Căn miệng: " + receiver.DisplayName + " (cần đúng bước)" : "RÓT RA NGOÀI — đưa gần miệng bình";
        }

        private void Hide() { if (path != null) path.enabled = false; if (marker != null) marker.enabled = false; Hint = ""; }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
