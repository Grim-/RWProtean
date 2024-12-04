using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Protean
{
    public class AbilityEffect : UpgradeEffect
    {
        public List<AbilityDef> abilities;
        private List<Ability> grantedAbilities = new List<Ability>();

        public override void Apply(Pawn pawn)
        {
            if (pawn.abilities == null)
                pawn.abilities = new Pawn_AbilityTracker(pawn);

            foreach (var abilityDef in abilities)
            {
                    pawn.abilities.GainAbility(abilityDef);
                    grantedAbilities.Add(pawn.abilities.GetAbility(abilityDef));
            }
        }

        public override void Remove(Pawn pawn)
        {
            if (pawn.abilities != null)
            {
                foreach (var ability in grantedAbilities)
                {
                    if (ability != null)
                        pawn.abilities.RemoveAbility(ability.def);
                }
            }
            grantedAbilities.Clear();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref abilities, "abilities", LookMode.Def);
            Scribe_Collections.Look(ref grantedAbilities, "grantedAbilities", LookMode.Reference);
        }
    }
}
