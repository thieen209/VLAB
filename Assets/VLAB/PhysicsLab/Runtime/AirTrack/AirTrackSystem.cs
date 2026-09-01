namespace VLAB.PhysicsLab.AirTrack
{
    public readonly struct CollisionVelocityPair
    {
        public CollisionVelocityPair(float firstVelocity, float secondVelocity)
        {
            FirstVelocity = firstVelocity;
            SecondVelocity = secondVelocity;
        }

        public float FirstVelocity { get; }
        public float SecondVelocity { get; }
    }

    public static class MomentumMath
    {
        public static float TotalMomentum(float massA, float velocityA, float massB, float velocityB)
        {
            return massA * velocityA + massB * velocityB;
        }

        public static void ElasticVelocities(float massA, float velocityA, float massB, float velocityB, out float resultA, out float resultB)
        {
            var totalMass = massA + massB;
            if (totalMass <= 0f)
            {
                resultA = velocityA;
                resultB = velocityB;
                return;
            }
            resultA = ((massA - massB) * velocityA + 2f * massB * velocityB) / totalMass;
            resultB = (2f * massA * velocityA + (massB - massA) * velocityB) / totalMass;
        }

        public static CollisionVelocityPair ElasticCollision(float massA, float velocityA, float massB, float velocityB)
        {
            ElasticVelocities(massA, velocityA, massB, velocityB, out var first, out var second);
            return new CollisionVelocityPair(first, second);
        }

        public static float PerfectlyInelasticVelocity(float massA, float velocityA, float massB, float velocityB)
        {
            var totalMass = massA + massB;
            return totalMass > 0f ? TotalMomentum(massA, velocityA, massB, velocityB) / totalMass : 0f;
        }

        public static float ConservationErrorPercent(float momentumBefore, float momentumAfter)
        {
            var reference = System.Math.Abs(momentumBefore);
            return reference > 0.000001f
                ? (float)(System.Math.Abs(momentumAfter - momentumBefore) / reference * 100d)
                : (System.Math.Abs(momentumAfter) < 0.000001f ? 0f : 100f);
        }
    }

    public enum GliderCollisionMode { Elastic, Inelastic }
}
