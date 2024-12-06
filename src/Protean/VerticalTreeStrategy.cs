﻿using System.Collections.Generic;
using UnityEngine;

namespace Protean
{
    public class VerticalTreeStrategy : ITreeDisplayStrategy
    {
        private const float START_NODE_SPACING = 20f; // Space between multiple start nodes

        private readonly bool bottomToTop = true;
        private readonly float horizontalPadding;
        private readonly float verticalPadding;

        public VerticalTreeStrategy()
        {

        }

        public VerticalTreeStrategy(bool startFromBottom = true, float horizontalPadding = 80f, float verticalPadding = 30f)
        {
            bottomToTop = startFromBottom;
            this.horizontalPadding = horizontalPadding;
            this.verticalPadding = verticalPadding;
        }

        public Dictionary<UpgradeTreeNodeDef, Rect> PositionNodes(
            List<UpgradeTreeNodeDef> nodes,
            Rect availableSpace,
            float nodeSize,
            float spacing)
        {
            if (nodes == null || nodes.Count == 0)
                return new Dictionary<UpgradeTreeNodeDef, Rect>();

            // Create a padded workspace
            var paddedSpace = new Rect(
                availableSpace.x + horizontalPadding,
                availableSpace.y + verticalPadding,
                availableSpace.width - (horizontalPadding * 2),
                availableSpace.height - (verticalPadding * 2)
            );

            var nodePositions = new Dictionary<UpgradeTreeNodeDef, Rect>();
            var pathGroups = GroupNodesByPath(nodes);
            var startNodes = FindStartNodes(nodes);

            // Position start nodes from the center
            PositionStartNodes(startNodes, nodePositions, paddedSpace, nodeSize);

            // Position path nodes distributed evenly
            PositionPaths(pathGroups, nodePositions, paddedSpace, nodeSize, spacing);

            return nodePositions;
        }

        private void PositionStartNodes(
            List<UpgradeTreeNodeDef> startNodes,
            Dictionary<UpgradeTreeNodeDef, Rect> nodePositions,
            Rect paddedSpace,
            float nodeSize)
        {
            if (startNodes.Count == 0) return;

            float startY = bottomToTop ? paddedSpace.height : paddedSpace.y;
            float centerX = paddedSpace.x + (paddedSpace.width / 2);

            if (startNodes.Count == 1)
            {
                // Single start node goes in the center
                nodePositions[startNodes[0]] = new Rect(
                    centerX - (nodeSize / 2),
                    startY,
                    nodeSize,
                    nodeSize);
            }
            else
            {
                // Multiple start nodes spread from center
                float totalWidth = (startNodes.Count * nodeSize) + ((startNodes.Count - 1) * START_NODE_SPACING);
                float startX = centerX - (totalWidth / 2);

                for (int i = 0; i < startNodes.Count; i++)
                {
                    float x = startX + (i * (nodeSize + START_NODE_SPACING));
                    nodePositions[startNodes[i]] = new Rect(x, startY, nodeSize, nodeSize);
                }
            }
        }


        private void PositionPaths(
            Dictionary<string, List<UpgradeTreeNodeDef>> pathGroups,
            Dictionary<UpgradeTreeNodeDef, Rect> nodePositions,
            Rect paddedSpace,
            float nodeSize,
            float spacing)
        {
            int pathCount = pathGroups.Count;
            if (pathCount == 0) return;

            float pathSpacing = pathCount > 1 ? paddedSpace.width / (pathCount - 1) : 0;

            int maxPathLength = GetMaxPathLength(pathGroups);
            float startY = bottomToTop ? paddedSpace.height : paddedSpace.y;
            float verticalStep = paddedSpace.height / (maxPathLength + 1);

            int pathIndex = 0;
            foreach (var path in pathGroups)
            {
                float pathX;
                if (pathCount == 1)
                {
                    // Single path goes in the center
                    pathX = paddedSpace.x + (paddedSpace.width / 2);
                }
                else
                {
                    // Multiple paths are distributed evenly
                    pathX = paddedSpace.x + (pathIndex * pathSpacing);
                }

                // Center the nodes on the path
                pathX -= nodeSize / 2;

                var orderedNodes = OrderByConnections(path.Value);
                PositionNodesInPath(
                    orderedNodes,
                    nodePositions,
                    pathX,
                    startY,
                    verticalStep,
                    nodeSize);

                pathIndex++;
            }
        }

        private void PositionNodesInPath(
            List<UpgradeTreeNodeDef> pathNodes,
            Dictionary<UpgradeTreeNodeDef, Rect> nodePositions,
            float pathX,
            float startY,
            float verticalStep,
            float nodeSize)
        {
            for (int i = 0; i < pathNodes.Count; i++)
            {
                float y = bottomToTop
                    ? startY - ((i + 1) * verticalStep)
                    : startY + ((i + 1) * verticalStep);

                nodePositions[pathNodes[i]] = new Rect(pathX, y, nodeSize, nodeSize);
            }
        }

        private List<UpgradeTreeNodeDef> FindStartNodes(List<UpgradeTreeNodeDef> nodes)
        {
            return nodes.FindAll(n => n.type == UpgradeTreeNodeDef.NodeType.Start);
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

        private List<UpgradeTreeNodeDef> OrderByConnections(List<UpgradeTreeNodeDef> nodes)
        {
            var ordered = new List<UpgradeTreeNodeDef>();
            var processed = new HashSet<UpgradeTreeNodeDef>();

            var rootNodes = FindRootNodes(nodes);

            foreach (var node in rootNodes)
            {
                ProcessNode(node, processed, ordered);
            }

            foreach (var node in nodes)
            {
                if (!ordered.Contains(node))
                {
                    ordered.Add(node);
                }
            }

            return ordered;
        }

        private HashSet<UpgradeTreeNodeDef> FindRootNodes(List<UpgradeTreeNodeDef> nodes)
        {
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

            var rootNodes = new HashSet<UpgradeTreeNodeDef>();
            foreach (var node in nodes)
            {
                if (!targetNodes.Contains(node))
                {
                    rootNodes.Add(node);
                }
            }

            return rootNodes;
        }

        private static void ProcessNode(
            UpgradeTreeNodeDef node,
            HashSet<UpgradeTreeNodeDef> processed,
            List<UpgradeTreeNodeDef> ordered)
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
