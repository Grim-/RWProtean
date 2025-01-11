using UnityEngine;
using Verse;

namespace Protean
{
    public class AnimationWorker_SymbiotePulse : AnimationWorker
    {
        private float pulseAmount = 0.05f;
        private float pulseSpeed = 0.05f; 

        public AnimationWorker_SymbiotePulse(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
            : base(def, pawn, part, node)
        {
        }

        public override Vector3 ScaleAtTick(int tick, PawnDrawParms parms)
        {
            float scaleFactor = 1f + (Mathf.Sin(Find.TickManager.TicksGame * pulseSpeed) * pulseAmount);
            return new Vector3(scaleFactor, 1f, scaleFactor);
        }
    }
}
