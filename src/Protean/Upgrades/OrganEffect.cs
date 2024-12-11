using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Protean
{
    public class OrganEffect : UpgradeEffect
    {
        public BodyPartDef targetOrgan;
        public HediffDef addedOrganHediff;
        public bool isAddition;
        private List<Hediff> addedParts = new List<Hediff>();


        protected override bool IsEffectPresent(Pawn pawn)
        {
            addedParts.RemoveAll(part => part == null || !pawn.health.hediffSet.HasHediff(part.def));
            bool targetOrganExists = pawn.health.hediffSet.hediffs.Any(h => h.def == addedOrganHediff);
            if (isAddition)
            {
                bool AnyAddedPartsExist = addedParts.Any();

                return AnyAddedPartsExist || targetOrganExists;
            }

            var targetParts = pawn.health.hediffSet.GetNotMissingParts()
                .Where(part => part.def == targetOrgan);

            return targetParts.Count() > 0;
        }

        protected override void Apply(Pawn pawn)
        {
            Hediff addedPart = null;

            if (isAddition)
            {
                addedPart = pawn.health.AddHediff(addedOrganHediff);
            }
            else
            {
                BodyPartRecord foundPart = pawn.health.hediffSet.GetNotMissingParts().Where(x => x.def == targetOrgan).FirstOrDefault();
                addedPart = pawn.health.AddHediff(addedOrganHediff, foundPart);
            }
            addedParts.Add(addedPart);
        }

        protected override void Remove(Pawn pawn)
        {
            foreach (var part in addedParts)
            {
                if (part != null)
                    pawn.health.RemoveHediff(part);
            }
            addedParts.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref targetOrgan, "targetOrgan");
            Scribe_Defs.Look(ref addedOrganHediff, "addedOrganHediff");
            Scribe_Values.Look(ref isAddition, "isAddition");
            Scribe_Collections.Look(ref addedParts, "addedParts", LookMode.Reference);
        }
    }
}
