using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Protean
{
    public class UpgradeTreeDef : Def
    {
        public List<UpgradeTreeNodeDef> nodes;
        public IntVec2 dimensions;
        public Type handlerClass;
        public UpgradeTreeSkinDef skin;
        public List<UpgradePathDef> availablePaths;
        public TreeDisplayStrategyDef displayStrategy;

        public List<UpgradeTreeNodeDef> GetAllNodes()
        {
            if (nodes.NullOrEmpty())
                return new List<UpgradeTreeNodeDef>();

            var allNodes = new HashSet<UpgradeTreeNodeDef>();
            var toProcess = new Queue<UpgradeTreeNodeDef>();
            toProcess.Enqueue(nodes[0]);

            while (toProcess.Count > 0)
            {
                var node = toProcess.Dequeue();
                if (allNodes.Add(node))
                {
                    if (!node.connections.NullOrEmpty())
                    {
                        foreach (var connected in node.connections)
                        {
                            toProcess.Enqueue(connected);
                        }
                    }

                    if (node.IsBranchNode && !node.branchPaths.NullOrEmpty())
                    {
                        foreach (var branch in node.branchPaths)
                        {
                            foreach (var branchNode in branch.nodes)
                            {
                                toProcess.Enqueue(branchNode);
                            }
                        }
                    }
                }
            }
            return allNodes.ToList();
        }

        public UpgradeTreeSkinDef GetSkin()
        {
            if (this.skin == null)
            {
                return DefDatabase<UpgradeTreeSkinDef>.GetNamed("DefaultTreeSkin");
            }

            return skin;
        }
    }
}
