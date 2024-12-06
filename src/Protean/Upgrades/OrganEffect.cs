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

        public override void Apply(Pawn pawn)
        {
            if (addedParts.Any())
            {
                // If tracked parts are missing, remove them from our list
                addedParts.RemoveAll(part => part == null || !pawn.health.hediffSet.HasHediff(part.def));
            }

            // Only add new parts
            if (!addedParts.Any())
            {
                IEnumerable<BodyPartRecord> targetParts = pawn.health.hediffSet.GetNotMissingParts()
                    .Where(part => part.def == targetOrgan);

                if (isAddition)
                {
                    BodyPartRecord randomParent = targetParts.RandomElement();
                    if (randomParent != null)
                    {
                        var hediff = HediffMaker.MakeHediff(addedOrganHediff, pawn, randomParent);
                        pawn.health.AddHediff(hediff, randomParent);
                        addedParts.Add(hediff);
                    }
                }
                else
                {
                    foreach (BodyPartRecord part in targetParts)
                    {
                        var hediff = HediffMaker.MakeHediff(addedOrganHediff, pawn, part);
                        pawn.health.AddHediff(hediff, part);
                        addedParts.Add(hediff);
                    }
                }
            }
        }

        public override void Remove(Pawn pawn)
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
            Scribe_Defs.Look(ref targetOrgan, "targetOrgan");
            Scribe_Defs.Look(ref addedOrganHediff, "addedOrganHediff");
            Scribe_Values.Look(ref isAddition, "isAddition");
            Scribe_Collections.Look(ref addedParts, "addedParts", LookMode.Reference);
        }
    }
}
