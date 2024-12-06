﻿using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<UpgradeTreeNodeDef> GetPredecessors(UpgradeTreeDef treeDef)
        {
            return treeDef.GetAllNodes().Where(n =>
                             n.connections != null && n.connections.Contains(this));
        }

        public int ConnectionCount => connections != null ? connections.Count : 0;
        public bool BelongsToUpgradePath => path != null;

        public enum NodeType
        {
            Normal,
            Keystone,
            Start
        }
    }
}
