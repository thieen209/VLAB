using UnityEngine;

namespace VLAB.DemoLabs
{
    public sealed class VLabWire : MonoBehaviour
    {
        public VLabGrabInteractable EndA, EndB;
        public LineRenderer Line;
        private Vector3 previousA = Vector3.positiveInfinity, previousB;
        private readonly Vector3[] points = new Vector3[20];
        public bool TryGetLink(out CircuitLink link)
        {
            link = default;
            if (EndA.Zone == null || EndB.Zone == null) return false;
            var a = EndA.Zone.GetComponent<VLabConnectionSocket>();
            var b = EndB.Zone.GetComponent<VLabConnectionSocket>();
            if (a == null || b == null) return false;
            link = new CircuitLink(a.Terminal, b.Terminal); return true;
        }
        private void LateUpdate()
        {
            var a = EndA.transform.position; var b = EndB.transform.position;
            if (a == previousA && b == previousB) return;
            previousA = a; previousB = b;
            var lift = Mathf.Min(.27f, Vector3.Distance(a, b) * .35f);
            for (var i = 0; i < points.Length; i++)
            {
                var t = i / (float)(points.Length - 1);
                points[i] = Vector3.Lerp(a, b, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * lift);
            }
            Line.positionCount = points.Length; Line.SetPositions(points);
        }
    }
}
