using Verse;

namespace Protean
{
    public abstract class UpgradeEffectDef : Def
    {
        public abstract string Description { get; }
        public abstract UpgradeEffect CreateEffect();
    }
}
