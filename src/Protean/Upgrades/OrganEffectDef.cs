using Verse;

namespace Protean
{
    public class OrganEffectDef : UpgradeEffectDef
    {
        public BodyPartDef targetOrgan;
        public HediffDef addedOrganHediff;
        public bool isAddition;

        public override UpgradeEffect CreateEffect()
        {
            return new OrganEffect
            {
                targetOrgan = targetOrgan,
                addedOrganHediff = addedOrganHediff,
                isAddition = isAddition
            };
        }
    }
}
