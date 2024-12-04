using System.Collections.Generic;
using Verse;

namespace Protean
{
    public class UpgradeDef : Def
    {
        public int parasiteLevelRequired;
        public List<UpgradeDef> prerequisites;
        public List<UpgradeEffectDef> effects;
        public string uiIcon;
        public int pointCost = 1;
    }
}
