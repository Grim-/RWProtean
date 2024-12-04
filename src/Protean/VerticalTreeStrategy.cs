using System.Collections.Generic;
using UnityEngine;

namespace Protean
{
    public class VerticalTreeStrategy : ITreeDisplayStrategy
    {
        private readonly bool bottomToTop;

        public VerticalTreeStrategy(bool startFromBottom = true)
        {
            bottomToTop = startFromBottom;
        }

        public Dictionary<UpgradeTreeNodeDef, Rect> PositionNodes(List<UpgradeTreeNodeDef> nodes, Rect availableSpace, float nodeSize, float spacing)
        {
            var nodePositions = new Dictionary<UpgradeTreeNodeDef, Rect>();
            if (nodes == null || nodes.Count == 0) return nodePositions;

            var pathGroups = GroupNodesByPath(nodes);
            var startNodes = nodes.FindAll(n => n.type == UpgradeTreeNodeDef.NodeType.Start);

            float totalHeight = availableSpace.height - nodeSize;
            float columnWidth = nodeSize + spacing;

            float startY = bottomToTop ? totalHeight : availableSpace.y;
            float centerX = availableSpace.x + (availableSpace.width - (startNodes.Count * columnWidth)) / 2;


            for (int i = 0; i < startNodes.Count; i++)
            {
                var node = startNodes[i];
                float x = centerX + (i * columnWidth);
                nodePositions[node] = new Rect(x, startY, nodeSize, nodeSize);
            }

            float yStep = (totalHeight - nodeSize) / (GetMaxPathLength(pathGroups) + 1);


            float currentX = availableSpace.x;
            foreach (var path in pathGroups)
            {
                var pathNodes = OrderByConnections(path.Value);
                for (int i = 0; i < pathNodes.Count; i++)
                {
                    float y;
                    if (bottomToTop)
                    {
                        y = startY - ((i + 1) * yStep);
                    }
                    else
                    {
                        y = startY + ((i + 1) * yStep);
                    }

                    nodePositions[pathNodes[i]] = new Rect(currentX, y, nodeSize, nodeSize);
                }
                currentX += columnWidth;
            }

            return nodePositions;
        }

        private Dictionary<string, List<UpgradeTreeNodeDef>> GroupNodesByPath(List<UpgradeTreeNodeDef> nodes)
        {
            var groups = new Dictionary<string, List<UpgradeTreeNodeDef>>();
            foreach (var node in nodes.FindAll(n => n.BelongsToUpgradePath))
            {
                string key = node.path.defName;
                if (!groups.ContainsKey(key))
                    groups[key] = new List<UpgradeTreeNodeDef>();
                groups[key].Add(node);
            }
            return groups;
        }

        private int GetMaxPathLength(Dictionary<string, List<UpgradeTreeNodeDef>> pathGroups)
        {
            int max = 0;
            foreach (var group in pathGroups.Values)
            {
                max = Mathf.Max(max, group.Count);
            }
            return max;
        }

        public List<UpgradeTreeNodeDef> OrderByConnections(List<UpgradeTreeNodeDef> nodes)
        {
            var ordered = new List<UpgradeTreeNodeDef>();
            var processed = new HashSet<UpgradeTreeNodeDef>();

            // Find root nodes (nodes that aren't targets of any connections)
            var targetNodes = new HashSet<UpgradeTreeNodeDef>();
            foreach (var node in nodes)
            {
                if (node.connections != null)
                {
                    foreach (var conn in node.connections)
                    {
                        targetNodes.Add(conn);
                    }
                }
            }

            // Start with nodes that aren't targets
            foreach (var node in nodes)
            {
                if (!targetNodes.Contains(node))
                {
                    ProcessNode(node, processed, ordered);
                }
            }

            // Add any remaining nodes
            foreach (var node in nodes)
            {
                if (!ordered.Contains(node))
                {
                    ordered.Add(node);
                }
            }

            return ordered;
        }

        private static void ProcessNode(UpgradeTreeNodeDef node, HashSet<UpgradeTreeNodeDef> processed, List<UpgradeTreeNodeDef> ordered)
        {
            if (processed.Contains(node)) return;

            processed.Add(node);
            ordered.Add(node);

            if (node.connections == null) return;

            foreach (var connection in node.connections)
            {
                ProcessNode(connection, processed, ordered);
            }
        }

    }
}
