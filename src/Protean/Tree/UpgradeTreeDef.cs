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

        public BaseTreeHandler CreateHandler(Pawn pawn)
        {
            return (BaseTreeHandler)Activator.CreateInstance(handlerClass, new object[] { pawn, this });
        }

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
                if (allNodes.Add(node))  // If this node hasn't been processed yet
                {
                    if (!node.connections.NullOrEmpty())
                    {
                        foreach (var connected in node.connections)
                        {
                            if (connected != null)
                                toProcess.Enqueue(connected);
                        }
                    }
                }
            }
            return allNodes.ToList();
        }
    }
}
