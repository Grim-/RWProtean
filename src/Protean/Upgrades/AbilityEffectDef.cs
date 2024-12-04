using RimWorld;
using System.Collections.Generic;

namespace Protean
{
    public class AbilityEffectDef : UpgradeEffectDef
    {
        public List<AbilityDef> abilities;

        public override UpgradeEffect CreateEffect()
        {
            return new AbilityEffect
            {
                abilities = abilities
            };
        }
    }
}
