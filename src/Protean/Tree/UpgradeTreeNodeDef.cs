using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Protean
{
    public class UpgradeTreeNodeDef : Def
    {
        public List<UpgradeDef> upgrades;
        public IntVec2 position;
        public List<UpgradeTreeNodeDef> connections;
        public NodeType type = NodeType.Normal;
        public UpgradePathDef path;
        public List<BranchPathData> branchPaths;

        public IEnumerable<UpgradeTreeNodeDef> GetPredecessors(UpgradeTreeDef treeDef)
        {
            return treeDef.GetAllNodes().Where(n =>
                n.connections != null && n.connections.Contains(this));
        }

        public bool HasUpgrade(int upgradeIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex > upgrades.Count)
            {
                return false;
            }
            return true;
        }
        public UpgradeDef GetUpgrade(int upgradeIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex > upgrades.Count)
            {
                return null;
            }
            return upgrades[upgradeIndex];
        }

        public int ConnectionCount => connections != null ? connections.Count : 0;
        public bool BelongsToUpgradePath => path != null;
        public bool IsBranchNode => type == NodeType.Branch;
    }
}
