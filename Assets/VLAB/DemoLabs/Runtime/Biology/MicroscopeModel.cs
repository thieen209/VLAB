using UnityEngine;

namespace VLAB.DemoLabs
{
    public enum CellStructure { None, Nucleus, Wall, Cytoplasm, Vacuole }
    public sealed class MicroscopeModel
    {
        public bool SlidePrepared, Water, Sample, Coverslip, Mounted, Clips;
        public bool CoarseUsed, FineUsed10, Observed10, FineUsed40, Observed40;
        public int Objective;
        public float Coarse, Fine = .5f, Light = .7f;
        public int Identified;
        public bool Prepared => SlidePrepared && Water && Sample && Coverslip;
        public float Focus => Coarse + (Fine - .5f) * .14f;
        public float TargetFocus => Objective == 40 ? .635f : .613f;
        public float FocusError => Mathf.Abs(Focus - TargetFocus);
        public float Clarity => Mathf.Clamp01(1f - FocusError * 12f);
        public bool Sharp => FocusError <= .012f && Light >= .2f;
        public void Observe()
        {
            if (!Mounted || !Clips || !Sharp) return;
            if (Objective == 10 && CoarseUsed && FineUsed10) Observed10 = true;
            if (Objective == 40 && Observed10 && FineUsed40) Observed40 = true;
        }
        public bool Identify(CellStructure structure)
        {
            if (!Observed40 || !Sharp || Identified >= 3) return false;
            var expected = Identified == 0 ? CellStructure.Nucleus : Identified == 1 ? CellStructure.Wall : CellStructure.Cytoplasm;
            if (structure != expected) return false;
            Identified++; return true;
        }
    }
    public static class OnionSpecimen
    {
        public static Vector2 CellGrid(Vector2 uv)
        {
            var y = uv.y * 12 + .075f * Mathf.Sin(uv.x * 26) + .035f * Mathf.Sin(uv.x * 71 + 2);
            var row = Mathf.FloorToInt(y);
            var x = uv.x * 5 + (row % 2 == 0 ? 0 : .48f) + .024f * Mathf.Sin(uv.y * 75) + .028f * Mathf.Sin(uv.y * 31 + 1.7f);
            return new Vector2(x, y);
        }
        public static Vector2 CellCoordinates(Vector2 uv)
        {
            var grid = CellGrid(uv);
            return new Vector2(grid.x - Mathf.Floor(grid.x), grid.y - Mathf.Floor(grid.y));
        }
        public static float Variation(Vector2 uv, float salt = 0)
        {
            var grid = CellGrid(uv);
            return Mathf.Repeat((Mathf.Floor(grid.x) * 13 + Mathf.Floor(grid.y) * 17 + salt) * .137f, 1);
        }
        public static float NucleusDistance(Vector2 uv)
        {
            var p = CellCoordinates(uv);
            var center = new Vector2(.20f + .08f * Variation(uv), .42f + .22f * Variation(uv, 5));
            var radius = new Vector2(.055f + .015f * Variation(uv, 3), .15f + .03f * Variation(uv, 5));
            return new Vector2((p.x - center.x) / radius.x, (p.y - center.y) / radius.y).magnitude;
        }
        public static float VacuoleDistance(Vector2 uv)
        {
            var p = CellCoordinates(uv);
            var q = new Vector2(Mathf.Abs(p.x - .60f), Mathf.Abs(p.y - .51f)) - new Vector2(.25f, .27f);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - .065f;
        }
        public static CellStructure Classify(Vector2 uv)
        {
            var p = CellCoordinates(uv);
            if (Mathf.Min(p.x, 1 - p.x) < .022f || Mathf.Min(p.y, 1 - p.y) < .06f) return CellStructure.Wall;
            if (NucleusDistance(uv) < 1.1f) return CellStructure.Nucleus;
            return VacuoleDistance(uv) > -.008f ? CellStructure.Cytoplasm : CellStructure.Vacuole;
        }
    }
}
