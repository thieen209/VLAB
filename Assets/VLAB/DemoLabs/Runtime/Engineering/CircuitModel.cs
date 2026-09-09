using System;
using System.Collections.Generic;

namespace VLAB.DemoLabs
{
    public enum Terminal { Positive, Ground, ResistorA, ResistorB, Anode, Cathode }
    public enum CircuitState { Incomplete, Miswired, Reversed, Safe, Overcurrent, Dim }
    public readonly struct CircuitResult
    {
        public readonly CircuitState State;
        public readonly float Current;
        public bool CanPower => State == CircuitState.Safe || State == CircuitState.Dim || State == CircuitState.Overcurrent;
        public CircuitResult(CircuitState state, float current = 0) { State = state; Current = current; }
    }
    public readonly struct CircuitLink
    {
        public readonly Terminal A, B;
        public CircuitLink(Terminal a, Terminal b) { A = a; B = b; }
    }
    // A deliberately bounded series-circuit validator. Rejects bypasses, shorts and extra wires.
    public static class CircuitModel
    {
        public static CircuitResult Evaluate(int resistance, bool ledPlaced, bool reversed, IReadOnlyList<CircuitLink> wires)
        {
            if (resistance <= 0 || !ledPlaced || wires.Count < 3) return new CircuitResult(CircuitState.Incomplete);
            if (wires.Count != 3) return new CircuitResult(CircuitState.Miswired);
            var parent = new int[6]; for (var i = 0; i < 6; i++) parent[i] = i;
            int Root(int n) { while (parent[n] != n) n = parent[n]; return n; }
            foreach (var wire in wires)
            {
                var a = (int)wire.A; var b = (int)wire.B;
                if (a < 0 || a >= 6 || b < 0 || b >= 6 || a == b || Root(a) == Root(b)) return new CircuitResult(CircuitState.Miswired);
                parent[Root(a)] = Root(b);
            }
            bool Same(Terminal a, Terminal b) => Root((int)a) == Root((int)b);
            bool Match(Terminal r1, Terminal r2, Terminal anode, Terminal cathode) =>
                Same(Terminal.Positive, r1) && Same(r2, anode) && Same(cathode, Terminal.Ground)
                && !Same(Terminal.Positive, Terminal.Ground) && !Same(r1, r2) && !Same(anode, cathode);
            var normal = Match(Terminal.ResistorA, Terminal.ResistorB, Terminal.Anode, Terminal.Cathode)
                || Match(Terminal.ResistorB, Terminal.ResistorA, Terminal.Anode, Terminal.Cathode);
            var backward = Match(Terminal.ResistorA, Terminal.ResistorB, Terminal.Cathode, Terminal.Anode)
                || Match(Terminal.ResistorB, Terminal.ResistorA, Terminal.Cathode, Terminal.Anode);
            if (!normal && !backward) return new CircuitResult(CircuitState.Miswired);
            if (normal == reversed) return new CircuitResult(CircuitState.Reversed);
            var current = 3f / resistance;
            return new CircuitResult(current > .02f ? CircuitState.Overcurrent : current < .005f ? CircuitState.Dim : CircuitState.Safe, current);
        }
    }
}
