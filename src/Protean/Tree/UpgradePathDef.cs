using System.Collections.Generic;
using Verse;

namespace Protean
{
    public class UpgradePathDef : Def
    {
        public List<UpgradePathDef> exclusiveWith;
        public string pathDescription;
        public string pathUIIcon;
    }
}
