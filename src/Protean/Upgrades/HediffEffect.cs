using Verse;

namespace Protean
{
    public class HediffEffect : UpgradeEffect
    {
        public HediffDef hediffDef;
        private Hediff appliedHediff;

        public override void Apply(Pawn pawn)
        {
            if (appliedHediff != null && !pawn.health.hediffSet.HasHediff(hediffDef))
            {
                appliedHediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(appliedHediff);
            }
            // If we have no saved reference at all
            else if (appliedHediff == null)
            {
                appliedHediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(appliedHediff);
            }
        }

        public override void Remove(Pawn pawn)
        {
            if (appliedHediff != null)
            {
                pawn.health.RemoveHediff(appliedHediff);
                appliedHediff = null;
            }
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref hediffDef, "hediffDef");
            Scribe_References.Look(ref appliedHediff, "appliedHediff");
        }
    }
}
