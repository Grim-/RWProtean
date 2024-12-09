using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Protean
{
    /// <summary>
    /// A tree display strategy that arranges nodes vertically, supporting both top-to-bottom and bottom-to-top layouts.
    /// Handles branching paths while maintaining proper spacing and alignment between nodes.
    /// </summary>
    /// <remarks>
    /// The strategy positions nodes according to these rules:
    /// - Start nodes are positioned at the bottom (or top) in a centered row
    /// - Child nodes are arranged in levels above (or below) their parents
    /// - Nodes at the same depth are aligned horizontally
    /// - Branches are distributed based on their subtree size to optimize space usage
    /// - All nodes respect the specified padding and margins
    /// 
    /// Key features:
    /// - Supports multiple start nodes with automatic spacing
    /// - Handles branching paths from any node
    /// - Maintains consistent vertical spacing between levels
    /// - Automatically adjusts horizontal distribution based on branch complexity
    /// - Respects specified padding while maximizing space usage
    /// </remarks>
    public class VerticalTreeStrategy : ITreeDisplayStrategy
    {
        private float START_NODE_SPACING = 20f;
        private float DEFAULT_VERTICAL_SPACING = 100f;
        private bool bottomToTop = true;
        private float verticalSpacing = 100f;
        private float horizontalPadding = 80f;
        private float verticalPadding = 30f;

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

            var nodePositions = new Dictionary<UpgradeTreeNodeDef, Rect>();
            var startNodes = FindStartNodes(nodes);
            var layoutTree = CreateLayoutTree(nodes);

            int maxDepth = FindMaxDepth(layoutTree);
            float verticalSpacing = Mathf.Min(
                DEFAULT_VERTICAL_SPACING,
                (availableSpace.height - (verticalPadding * 2) - nodeSize) / (maxDepth + 1)
            );

            // Position start nodes with original wide spread
            float startY = bottomToTop ?
                availableSpace.y + availableSpace.height - verticalPadding - nodeSize :
                availableSpace.y + verticalPadding;

            float effectiveWidth = availableSpace.width - (horizontalPadding * 2);
            if (startNodes.Count == 1)
            {
                float centerX = availableSpace.x + (availableSpace.width / 2);
                nodePositions[startNodes[0]] = new Rect(
                    centerX - (nodeSize / 2),
                    startY,
                    nodeSize,
                    nodeSize);
            }
            else if (startNodes.Count > 1)
            {
                float totalNodesWidth = nodeSize * startNodes.Count;
                float remainingSpace = effectiveWidth - totalNodesWidth;
                float startSpacing = remainingSpace / (startNodes.Count - 1);

                for (int i = 0; i < startNodes.Count; i++)
                {
                    float x = availableSpace.x + horizontalPadding + (i * (nodeSize + startSpacing));
                    nodePositions[startNodes[i]] = new Rect(x, startY, nodeSize, nodeSize);
                }
            }

            // Build parent-child relationships and find max children per level
            var children = new Dictionary<UpgradeTreeNodeDef, List<UpgradeTreeNodeDef>>();
            var maxChildrenPerLevel = new Dictionary<int, int>();
            foreach (var node in nodes)
            {
                if (node.connections != null)
                {
                    children[node] = new List<UpgradeTreeNodeDef>(node.connections);

                    // Track max children per level for spread calculation
                    if (children[node].Count > 1)
                    {
                        int nodeLevel = GetNodeLevel(node, children);
                        int childLevel = nodeLevel + 1;
                        if (!maxChildrenPerLevel.ContainsKey(childLevel))
                        {
                            maxChildrenPerLevel[childLevel] = 0;
                        }
                        maxChildrenPerLevel[childLevel] = Mathf.Max(maxChildrenPerLevel[childLevel], children[node].Count);
                    }
                }
            }

            // Position remaining nodes level by level
            var levelNodes = new Dictionary<int, List<LayoutNode>>();
            CollectNodesByLevel(layoutTree, levelNodes, 0);

            for (int level = 0; level <= maxDepth; level++)
            {
                if (!levelNodes.ContainsKey(level)) continue;

                var nodesInLevel = levelNodes[level]
                    .Where(n => n.node != null && n.node.type != NodeType.Start)
                    .ToList();

                if (nodesInLevel.Count == 0) continue;

                float y = bottomToTop ?
                    startY - (level * verticalSpacing) :
                    startY + (level * verticalSpacing);

                // Find all nodes in this level that need positioning
                var nodesToPosition = new HashSet<UpgradeTreeNodeDef>();
                foreach (var layoutNode in nodesInLevel)
                {
                    nodesToPosition.Add(layoutNode.node);
                }

                // Handle branching for non-start nodes with adaptive spread
                foreach (var node in nodes.Where(n => n.type != NodeType.Start))
                {
                    if (!children.ContainsKey(node) || children[node].Count <= 1) continue;

                    var childrenInThisLevel = children[node]
                        .Where(child => nodesToPosition.Contains(child))
                        .ToList();

                    if (childrenInThisLevel.Count > 0)
                    {
                        var parentPos = nodePositions[node];
                        float branchWidth = effectiveWidth * 0.8f;

                        if (childrenInThisLevel.Count == 1)
                        {
                            nodePositions[childrenInThisLevel[0]] = new Rect(
                                parentPos.x,
                                y,
                                nodeSize,
                                nodeSize);
                        }
                        else
                        {
                            float leftX = Mathf.Max(availableSpace.x + horizontalPadding,
                                                  parentPos.center.x - (branchWidth / 2));
                            float rightX = Mathf.Min(availableSpace.x + availableSpace.width - horizontalPadding - nodeSize,
                                                   parentPos.center.x + (branchWidth / 2) - nodeSize);

                            float branchSpacing = (rightX - leftX) / (childrenInThisLevel.Count - 1);
                            for (int i = 0; i < childrenInThisLevel.Count; i++)
                            {
                                float x = leftX + (i * branchSpacing);
                                nodePositions[childrenInThisLevel[i]] = new Rect(x, y, nodeSize, nodeSize);
                            }
                        }

                        foreach (var child in childrenInThisLevel)
                        {
                            nodesToPosition.Remove(child);
                        }
                    }
                }

                // Position any remaining nodes evenly
                var remainingNodes = nodesInLevel
                    .Where(n => nodesToPosition.Contains(n.node))
                    .Select(n => n.node)
                    .ToList();

                if (remainingNodes.Count > 0)
                {
                    float totalNodesWidth = nodeSize * remainingNodes.Count;
                    float remainingSpace = effectiveWidth - totalNodesWidth;
                    float levelSpacing = remainingNodes.Count > 1 ? remainingSpace / (remainingNodes.Count - 1) : 0;

                    for (int i = 0; i < remainingNodes.Count; i++)
                    {
                        float x = availableSpace.x + horizontalPadding + (i * (nodeSize + levelSpacing));
                        nodePositions[remainingNodes[i]] = new Rect(x, y, nodeSize, nodeSize);
                    }
                }
            }

            return nodePositions;
        }
        private int GetNodeLevel(UpgradeTreeNodeDef node, Dictionary<UpgradeTreeNodeDef, List<UpgradeTreeNodeDef>> children)
        {
            int level = 0;
            var visited = new HashSet<UpgradeTreeNodeDef>();

            void FindLevel(UpgradeTreeNodeDef current, int currentLevel)
            {
                if (visited.Contains(current)) return;
                visited.Add(current);
                level = Mathf.Max(level, currentLevel);

                if (children.ContainsKey(current))
                {
                    foreach (var child in children[current])
                    {
                        FindLevel(child, currentLevel + 1);
                    }
                }
            }

            FindLevel(node, 0);
            return level;
        }

        private class LayoutNode
        {
            public UpgradeTreeNodeDef node;
            public List<LayoutNode> children = new List<LayoutNode>();
            public int depth;
            public float horizontalWeight;
        }

        private LayoutNode CreateLayoutTree(List<UpgradeTreeNodeDef> nodes)
        {
            var nodeMap = new Dictionary<UpgradeTreeNodeDef, LayoutNode>();
            var rootNodes = FindRootNodes(nodes);

            // Create LayoutNodes for all nodes
            foreach (var node in nodes)
            {
                nodeMap[node] = new LayoutNode { node = node };
            }

            // Build the tree structure
            foreach (var node in nodes)
            {
                if (node.connections != null)
                {
                    foreach (var connection in node.connections)
                    {
                        nodeMap[node].children.Add(nodeMap[connection]);
                    }
                }
            }

            // Calculate depths and weights
            foreach (var rootNode in rootNodes)
            {
                CalculateDepthAndWeight(nodeMap[rootNode], 0);
            }

            // Create a virtual root for multiple start nodes
            var virtualRoot = new LayoutNode();
            foreach (var rootNode in rootNodes)
            {
                virtualRoot.children.Add(nodeMap[rootNode]);
            }

            return virtualRoot;
        }

        private void CalculateDepthAndWeight(LayoutNode layoutNode, int currentDepth)
        {
            layoutNode.depth = currentDepth;

            if (layoutNode.children.Count == 0)
            {
                layoutNode.horizontalWeight = 1;
                return;
            }

            float totalWeight = 0;
            foreach (var child in layoutNode.children)
            {
                CalculateDepthAndWeight(child, currentDepth + 1);
                totalWeight += child.horizontalWeight;
            }

            layoutNode.horizontalWeight = Mathf.Max(1, totalWeight);
        }

        private int FindMaxDepth(LayoutNode root)
        {
            int maxDepth = 0;
            void TraverseDepth(LayoutNode node, int depth)
            {
                maxDepth = Mathf.Max(maxDepth, depth);
                foreach (var child in node.children)
                {
                    TraverseDepth(child, depth + 1);
                }
            }
            TraverseDepth(root, 0);
            return maxDepth;
        }

        private void CollectNodesByLevel(LayoutNode node, Dictionary<int, List<LayoutNode>> levelNodes, int depth)
        {
            if (!levelNodes.ContainsKey(depth))
            {
                levelNodes[depth] = new List<LayoutNode>();
            }
            if (node.node != null) // Don't add virtual root
            {
                levelNodes[depth].Add(node);
            }
            foreach (var child in node.children)
            {
                CollectNodesByLevel(child, levelNodes, depth + 1);
            }
        }

        private List<UpgradeTreeNodeDef> FindStartNodes(List<UpgradeTreeNodeDef> nodes) =>
            nodes.FindAll(n => n.type == NodeType.Start);

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

        public void DrawToolBar(Rect toolbarRect)
        {
            float currentX = toolbarRect.x + 10f;
            float buttonWidth = 120f;
            float sliderWidth = 100f;
            float labelWidth = 60f;
            float rowHeight = 24f;
            float padding = 10f;

            // Direction toggle
            if (Widgets.ButtonText(new Rect(currentX, toolbarRect.y + 5f, buttonWidth, rowHeight),
                bottomToTop ? "Bottom to Top" : "Top to Bottom"))
            {
                bottomToTop = !bottomToTop;
            }
            currentX += buttonWidth + padding;

            // Vertical Spacing
            Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "V Space:");
            currentX += labelWidth;
            float newVerticalSpacing = Widgets.HorizontalSlider(
                new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
                verticalSpacing, 50f, 200f, true);
            if (newVerticalSpacing != verticalSpacing)
            {
                verticalSpacing = newVerticalSpacing;
            }
            currentX += sliderWidth + padding;

            // Start Node Spacing
            Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "Start:");
            currentX += labelWidth;
            float newStartSpacing = Widgets.HorizontalSlider(
                new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
                START_NODE_SPACING, 10f, 100f, true);
            if (newStartSpacing != START_NODE_SPACING)
            {
                START_NODE_SPACING = newStartSpacing;
            }
            currentX += sliderWidth + padding;

            // Horizontal Padding
            Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "H Pad:");
            currentX += labelWidth;
            float newHPadding = Widgets.HorizontalSlider(
                new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
                horizontalPadding, 20f, 150f, true);
            if (newHPadding != horizontalPadding)
            {
                horizontalPadding = newHPadding;
            }
            currentX += sliderWidth + padding;

            // Vertical Padding
            Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "V Pad:");
            currentX += labelWidth;
            float newVPadding = Widgets.HorizontalSlider(
                new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
                verticalPadding, 10f, 100f, true);
            if (newVPadding != verticalPadding)
            {
                verticalPadding = newVPadding;
            }
        }
    }


    /// <summary>
    /// A variation of the vertical tree strategy that introduces small random variations in node positions
    /// while maintaining the overall vertical structure and connection readability.
    /// </summary>
    //public class RandomizedVerticalTreeStrategy : ITreeDisplayStrategy
    //{
    //    private float START_NODE_SPACING = 20f;
    //    private float DEFAULT_VERTICAL_SPACING = 100f;
    //    private bool bottomToTop = true;
    //    private float verticalSpacing = 100f;
    //    private float horizontalPadding = 80f;
    //    private float verticalPadding = 30f;

    //    // Randomization parameters
    //    private float horizontalVariation = 15f;
    //    private float verticalVariation = 10f;
    //    private System.Random random;

    //    public RandomizedVerticalTreeStrategy()
    //    {
    //        random = new System.Random(Find.TickManager?.TicksGame ?? 0);
    //    }

    //    public RandomizedVerticalTreeStrategy(bool startFromBottom = true, float horizontalPadding = 80f, float verticalPadding = 30f)
    //    {
    //        bottomToTop = startFromBottom;
    //        this.horizontalPadding = horizontalPadding;
    //        this.verticalPadding = verticalPadding;
    //        random = new System.Random(Find.TickManager?.TicksGame ?? 0);
    //    }

    //    private Vector2 AddRandomVariation(Vector2 position)
    //    {
    //        float randX = (float)(random.NextDouble() * 2 - 1) * horizontalVariation;
    //        float randY = (float)(random.NextDouble() * 2 - 1) * verticalVariation;
    //        return new Vector2(position.x + randX, position.y + randY);
    //    }

    //    public Dictionary<UpgradeTreeNodeDef, Rect> PositionNodes(
    //        List<UpgradeTreeNodeDef> nodes,
    //        Rect availableSpace,
    //        float nodeSize,
    //        float spacing)
    //    {
    //        if (nodes == null || nodes.Count == 0)
    //            return new Dictionary<UpgradeTreeNodeDef, Rect>();

    //        var nodePositions = new Dictionary<UpgradeTreeNodeDef, Rect>();
    //        var startNodes = FindStartNodes(nodes);
    //        var layoutTree = CreateLayoutTree(nodes);

    //        int maxDepth = FindMaxDepth(layoutTree);
    //        float verticalSpacing = Mathf.Min(
    //            DEFAULT_VERTICAL_SPACING,
    //            (availableSpace.height - (verticalPadding * 2) - nodeSize) / (maxDepth + 1)
    //        );

    //        // Position start nodes with original wide spread but slight randomization
    //        float startY = bottomToTop ?
    //            availableSpace.y + availableSpace.height - verticalPadding - nodeSize :
    //            availableSpace.y + verticalPadding;

    //        float effectiveWidth = availableSpace.width - (horizontalPadding * 2);
    //        if (startNodes.Count == 1)
    //        {
    //            float centerX = availableSpace.x + (availableSpace.width / 2);
    //            var randomPos = AddRandomVariation(new Vector2(
    //                centerX - (nodeSize / 2),
    //                startY
    //            ));
    //            nodePositions[startNodes[0]] = new Rect(
    //                randomPos.x,
    //                randomPos.y,
    //                nodeSize,
    //                nodeSize);
    //        }
    //        else if (startNodes.Count > 1)
    //        {
    //            float totalNodesWidth = nodeSize * startNodes.Count;
    //            float remainingSpace = effectiveWidth - totalNodesWidth;
    //            float startSpacing = remainingSpace / (startNodes.Count - 1);

    //            for (int i = 0; i < startNodes.Count; i++)
    //            {
    //                float baseX = availableSpace.x + horizontalPadding + (i * (nodeSize + startSpacing));
    //                var randomPos = AddRandomVariation(new Vector2(baseX, startY));
    //                nodePositions[startNodes[i]] = new Rect(randomPos.x, randomPos.y, nodeSize, nodeSize);
    //            }
    //        }

    //        // Build parent-child relationships and find max children per level
    //        var children = new Dictionary<UpgradeTreeNodeDef, List<UpgradeTreeNodeDef>>();
    //        var maxChildrenPerLevel = new Dictionary<int, int>();
    //        foreach (var node in nodes)
    //        {
    //            if (node.connections != null)
    //            {
    //                children[node] = new List<UpgradeTreeNodeDef>(node.connections);

    //                if (children[node].Count > 1)
    //                {
    //                    int nodeLevel = GetNodeLevel(node, children);
    //                    int childLevel = nodeLevel + 1;
    //                    if (!maxChildrenPerLevel.ContainsKey(childLevel))
    //                    {
    //                        maxChildrenPerLevel[childLevel] = 0;
    //                    }
    //                    maxChildrenPerLevel[childLevel] = Mathf.Max(maxChildrenPerLevel[childLevel], children[node].Count);
    //                }
    //            }
    //        }

    //        // Position remaining nodes level by level with randomization
    //        var levelNodes = new Dictionary<int, List<LayoutNode>>();
    //        CollectNodesByLevel(layoutTree, levelNodes, 0);

    //        for (int level = 0; level <= maxDepth; level++)
    //        {
    //            if (!levelNodes.ContainsKey(level)) continue;

    //            var nodesInLevel = levelNodes[level]
    //                .Where(n => n.node != null && n.node.type != NodeType.Start)
    //                .ToList();

    //            if (nodesInLevel.Count == 0) continue;

    //            float baseY = bottomToTop ?
    //                startY - (level * verticalSpacing) :
    //                startY + (level * verticalSpacing);

    //            var nodesToPosition = new HashSet<UpgradeTreeNodeDef>();
    //            foreach (var layoutNode in nodesInLevel)
    //            {
    //                nodesToPosition.Add(layoutNode.node);
    //            }

    //            // Handle branching for non-start nodes
    //            foreach (var node in nodes.Where(n => n.type != NodeType.Start))
    //            {
    //                if (!children.ContainsKey(node) || children[node].Count <= 1) continue;

    //                var childrenInThisLevel = children[node]
    //                    .Where(child => nodesToPosition.Contains(child))
    //                    .ToList();

    //                if (childrenInThisLevel.Count > 0)
    //                {
    //                    var parentPos = nodePositions[node];
    //                    float branchWidth = effectiveWidth * 0.8f;

    //                    if (childrenInThisLevel.Count == 1)
    //                    {
    //                        var randomPos = AddRandomVariation(new Vector2(
    //                            parentPos.x,
    //                            baseY
    //                        ));
    //                        nodePositions[childrenInThisLevel[0]] = new Rect(
    //                            randomPos.x,
    //                            randomPos.y,
    //                            nodeSize,
    //                            nodeSize);
    //                    }
    //                    else
    //                    {
    //                        float leftX = Mathf.Max(availableSpace.x + horizontalPadding,
    //                                              parentPos.center.x - (branchWidth / 2));
    //                        float rightX = Mathf.Min(availableSpace.x + availableSpace.width - horizontalPadding - nodeSize,
    //                                               parentPos.center.x + (branchWidth / 2) - nodeSize);

    //                        float branchSpacing = (rightX - leftX) / (childrenInThisLevel.Count - 1);
    //                        for (int i = 0; i < childrenInThisLevel.Count; i++)
    //                        {
    //                            float baseX = leftX + (i * branchSpacing);
    //                            var randomPos = AddRandomVariation(new Vector2(baseX, baseY));
    //                            nodePositions[childrenInThisLevel[i]] = new Rect(
    //                                randomPos.x,
    //                                randomPos.y,
    //                                nodeSize,
    //                                nodeSize);
    //                        }
    //                    }

    //                    foreach (var child in childrenInThisLevel)
    //                    {
    //                        nodesToPosition.Remove(child);
    //                    }
    //                }
    //            }

    //            // Position remaining nodes with randomization
    //            var remainingNodes = nodesInLevel
    //                .Where(n => nodesToPosition.Contains(n.node))
    //                .Select(n => n.node)
    //                .ToList();

    //            if (remainingNodes.Count > 0)
    //            {
    //                float totalNodesWidth = nodeSize * remainingNodes.Count;
    //                float remainingSpace = effectiveWidth - totalNodesWidth;
    //                float levelSpacing = remainingNodes.Count > 1 ? remainingSpace / (remainingNodes.Count - 1) : 0;

    //                for (int i = 0; i < remainingNodes.Count; i++)
    //                {
    //                    float baseX = availableSpace.x + horizontalPadding + (i * (nodeSize + levelSpacing));
    //                    var randomPos = AddRandomVariation(new Vector2(baseX, baseY));
    //                    nodePositions[remainingNodes[i]] = new Rect(randomPos.x, randomPos.y, nodeSize, nodeSize);
    //                }
    //            }
    //        }

    //        return nodePositions;
    //    }

    //    // All helper methods from VerticalTreeStrategy remain exactly the same
    //    private int GetNodeLevel(UpgradeTreeNodeDef node, Dictionary<UpgradeTreeNodeDef, List<UpgradeTreeNodeDef>> children)
    //    {
    //        int level = 0;
    //        var visited = new HashSet<UpgradeTreeNodeDef>();

    //        void FindLevel(UpgradeTreeNodeDef current, int currentLevel)
    //        {
    //            if (visited.Contains(current)) return;
    //            visited.Add(current);
    //            level = Mathf.Max(level, currentLevel);

    //            if (children.ContainsKey(current))
    //            {
    //                foreach (var child in children[current])
    //                {
    //                    FindLevel(child, currentLevel + 1);
    //                }
    //            }
    //        }

    //        FindLevel(node, 0);
    //        return level;
    //    }

    //    private class LayoutNode
    //    {
    //        public UpgradeTreeNodeDef node;
    //        public List<LayoutNode> children = new List<LayoutNode>();
    //        public int depth;
    //        public float horizontalWeight;
    //    }

    //    private LayoutNode CreateLayoutTree(List<UpgradeTreeNodeDef> nodes)
    //    {
    //        var nodeMap = new Dictionary<UpgradeTreeNodeDef, LayoutNode>();
    //        var rootNodes = FindRootNodes(nodes);

    //        foreach (var node in nodes)
    //        {
    //            nodeMap[node] = new LayoutNode { node = node };
    //        }

    //        foreach (var node in nodes)
    //        {
    //            if (node.connections != null)
    //            {
    //                foreach (var connection in node.connections)
    //                {
    //                    nodeMap[node].children.Add(nodeMap[connection]);
    //                }
    //            }
    //        }

    //        foreach (var rootNode in rootNodes)
    //        {
    //            CalculateDepthAndWeight(nodeMap[rootNode], 0);
    //        }

    //        var virtualRoot = new LayoutNode();
    //        foreach (var rootNode in rootNodes)
    //        {
    //            virtualRoot.children.Add(nodeMap[rootNode]);
    //        }

    //        return virtualRoot;
    //    }

    //    private void CalculateDepthAndWeight(LayoutNode layoutNode, int currentDepth)
    //    {
    //        layoutNode.depth = currentDepth;

    //        if (layoutNode.children.Count == 0)
    //        {
    //            layoutNode.horizontalWeight = 1;
    //            return;
    //        }

    //        float totalWeight = 0;
    //        foreach (var child in layoutNode.children)
    //        {
    //            CalculateDepthAndWeight(child, currentDepth + 1);
    //            totalWeight += child.horizontalWeight;
    //        }

    //        layoutNode.horizontalWeight = Mathf.Max(1, totalWeight);
    //    }

    //    private int FindMaxDepth(LayoutNode root)
    //    {
    //        int maxDepth = 0;
    //        void TraverseDepth(LayoutNode node, int depth)
    //        {
    //            maxDepth = Mathf.Max(maxDepth, depth);
    //            foreach (var child in node.children)
    //            {
    //                TraverseDepth(child, depth + 1);
    //            }
    //        }
    //        TraverseDepth(root, 0);
    //        return maxDepth;
    //    }

    //    private void CollectNodesByLevel(LayoutNode node, Dictionary<int, List<LayoutNode>> levelNodes, int depth)
    //    {
    //        if (!levelNodes.ContainsKey(depth))
    //        {
    //            levelNodes[depth] = new List<LayoutNode>();
    //        }
    //        if (node.node != null)
    //        {
    //            levelNodes[depth].Add(node);
    //        }
    //        foreach (var child in node.children)
    //        {
    //            CollectNodesByLevel(child, levelNodes, depth + 1);
    //        }
    //    }

    //    private List<UpgradeTreeNodeDef> FindStartNodes(List<UpgradeTreeNodeDef> nodes) =>
    //        nodes.FindAll(n => n.type == NodeType.Start);

    //    private HashSet<UpgradeTreeNodeDef> FindRootNodes(List<UpgradeTreeNodeDef> nodes)
    //    {
    //        var targetNodes = new HashSet<UpgradeTreeNodeDef>();
    //        foreach (var node in nodes)
    //        {
    //            if (node.connections != null)
    //            {
    //                foreach (var conn in node.connections)
    //                {
    //                    targetNodes.Add(conn);
    //                }
    //            }
    //        }

    //        var rootNodes = new HashSet<UpgradeTreeNodeDef>();
    //        foreach (var node in nodes)
    //        {
    //            if (!targetNodes.Contains(node))
    //            {
    //                rootNodes.Add(node);
    //            }
    //        }

    //        return rootNodes;
    //    }

    //    public void DrawToolBar(Rect toolbarRect)
    //    {
    //        float currentX = toolbarRect.x + 10f;
    //        float buttonWidth = 120f;
    //        float sliderWidth = 100f;
    //        float labelWidth = 60f;
    //        float rowHeight = 24f;
    //        float padding = 10f;

    //        if (Widgets.ButtonText(new Rect(currentX, toolbarRect.y + 5f, buttonWidth, rowHeight),
    //            bottomToTop ? "Bottom to Top" : "Top to Bottom"))
    //        {
    //            bottomToTop = !bottomToTop;
    //        }
    //        currentX += buttonWidth + padding;

    //        Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "V Space:");
    //        currentX += labelWidth;
    //        float newVerticalSpacing = Widgets.HorizontalSlider(
    //            new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
    //            verticalSpacing, 50f, 200f, true);
    //        if (newVerticalSpacing != verticalSpacing)
    //        {
    //            verticalSpacing = newVerticalSpacing;
    //        }
    //        currentX += sliderWidth + padding;

    //        Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "H Var:");
    //        currentX += labelWidth;
    //        float newHVar = Widgets.HorizontalSlider(
    //            new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
    //            horizontalVariation, 0f, 50f, true);
    //        if (newHVar != horizontalVariation)
    //        {
    //            horizontalVariation, 0f, 50f, true);
    //            if (newHVar != horizontalVariation)
    //            {
    //                horizontalVariation = newHVar;
    //            }
    //            currentX += sliderWidth + padding;

    //            Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "V Var:");
    //            currentX += labelWidth;
    //            float newVVar = Widgets.HorizontalSlider(
    //                new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
    //                verticalVariation, 0f, 30f, true);
    //            if (newVVar != verticalVariation)
    //            {
    //                verticalVariation = newVVar;
    //            }
    //            currentX += sliderWidth + padding;

    //            Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "H Pad:");
    //            currentX += labelWidth;
    //            float newHPadding = Widgets.HorizontalSlider(
    //                new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
    //                horizontalPadding, 20f, 150f, true);
    //            if (newHPadding != horizontalPadding)
    //            {
    //                horizontalPadding = newHPadding;
    //            }
    //            currentX += sliderWidth + padding;

    //            Widgets.Label(new Rect(currentX, toolbarRect.y + 5f, labelWidth, rowHeight), "V Pad:");
    //            currentX += labelWidth;
    //            float newVPadding = Widgets.HorizontalSlider(
    //                new Rect(currentX, toolbarRect.y + 5f, sliderWidth, rowHeight),
    //                verticalPadding, 10f, 100f, true);
    //            if (newVPadding != verticalPadding)
    //            {
    //                verticalPadding = newVPadding;
    //            }
    //        }
    //    }
    //}
}
