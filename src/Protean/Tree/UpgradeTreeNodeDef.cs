using System.Collections.Generic;
using Verse;

namespace Protean
{
    public class UpgradeTreeNodeDef : Def
    {
        public UpgradeDef upgrade;
        public IntVec2 position;
        public List<UpgradeTreeNodeDef> connections;
        public NodeType type = NodeType.Normal;
        public UpgradePathDef path;

        public int ConnectionCount => connections != null ? connections.Count : 0;

        public bool BelongsToUpgradePath => path != null;


        public enum NodeType
        {
            Normal,
            Keystone,
            Start
        }

        public virtual bool CanUnlock(Pawn pawn, BaseTreeHandler handler)
        {
            if (upgrade == null) return true;
            if (type == NodeType.Start) return true;

            return true;
        }

        public bool IsValid()
        {
            return true;
        }
    }
}
