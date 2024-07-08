using Photon.Deterministic;

namespace Quantum
{
    public unsafe class LerpSystem : SystemMainThreadFilter<LerpSystem.Filter>
    {
        public delegate void LerpEvent(Frame f, ref Filter filter);

        public static LerpEvent OnLerpComplete;

        public struct Filter
        {
            public EntityRef Entity;
            public Lerp* Lerp;
            public Transform3D* Transform3D;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            var t = FPMath.InverseLerpUnclamped(filter.Lerp->StartTime, filter.Lerp->EndTime, f.Number * f.DeltaTime);

            var position = FPVector3.Lerp(filter.Lerp->StartPosition, filter.Lerp->EndPosition, t);
            filter.Transform3D->Position = position;

            if (t >= 1)
            {
                OnLerpComplete?.Invoke(f, ref filter);
                f.Remove<Lerp>(filter.Entity);
            }
        }

        public static void StartLerpRoutine(Frame f, EntityRef entity, FPVector3 start, FPVector3 end, FP duration)
        {
            var startTime = f.DeltaTime * f.Number;

            Lerp lerp;
            lerp = new Lerp()
            {
                StartPosition = start,
                EndPosition = end,
                StartTime = startTime,
                EndTime = startTime + duration,
            };
            f.Add(entity, lerp);
        }
    }
}