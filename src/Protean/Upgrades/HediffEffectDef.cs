using Verse;

namespace Protean
{
    public class HediffEffectDef : UpgradeEffectDef
    {
        public HediffDef hediffDef;

        public override UpgradeEffect CreateEffect()
        {
            return new HediffEffect
            {
                hediffDef = hediffDef
            };
        }
    }
}
