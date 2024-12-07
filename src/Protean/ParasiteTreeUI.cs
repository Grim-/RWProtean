using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Protean
{
    public class ParasiteTreeUI : Window
    {
        private readonly Gene_Parasite parasiteGene;
        private readonly UpgradeTreeDef treeDef;
        private readonly BaseTreeHandler treeHandler;
        private Vector2 scrollPosition;
        private readonly List<UpgradeTreeNodeDef> allNodes;
        private readonly ITreeDisplayStrategy displayStrategy;
        private Dictionary<UpgradeTreeNodeDef, Rect> nodePositions;
        private readonly List<UIAnimationState> activeAnimations = new List<UIAnimationState>();
        private readonly UpgradeTreeSkinDef skin;

        public override Vector2 InitialSize => new Vector2(450f, 800f);

        public ParasiteTreeUI(Gene_Parasite parasite, UpgradeTreeDef tree, BaseTreeHandler handler, TreeDisplayStrategyDef displayStrategyDef)
        {
            parasiteGene = parasite;
            treeDef = tree;
            treeHandler = handler;
            allNodes = treeDef.GetAllNodes();
            skin = tree.skin ?? DefDatabase<UpgradeTreeSkinDef>.GetNamed("DefaultTreeSkin");

            displayStrategy = (ITreeDisplayStrategy)Activator.CreateInstance(
                displayStrategyDef.strategyClass);

            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect toolbarRect = new Rect(0f, 0f, inRect.width, skin.toolbarHeight);
            DrawToolbar(toolbarRect);
            Widgets.DrawTextureFitted(inRect, skin.BackgroundTexture, 1f);
            nodePositions = displayStrategy.PositionNodes(allNodes, inRect, skin.nodeSize, skin.nodeSpacing);

            DrawConnections();
            DrawNodes();

            activeAnimations.RemoveAll(a => a.Finished);
            foreach (var anim in activeAnimations)
            {
                anim.Animate();
            }
        }

        private void DrawNodes()
        {
            foreach (var kvp in nodePositions)
            {
                DrawNode(kvp.Key, kvp.Value);
            }
        }

        private void DrawNode(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            UnlockResult canUnlockResult = treeHandler.CanUnlockNode(node);
            bool isUnlocked = treeHandler.IsNodeUnlocked(node);

            HandlePathSelectionClick(node, nodeRect);

            GUI.color = GetNodeColor(node);
            GUI.DrawTexture(nodeRect, skin.NodeTexture);
            GUI.color = Color.white;
            DrawNodeIconBG(node, nodeRect);
            DrawNodeIcon(node, nodeRect);
            //DrawNodeLabel(node, nodeRect);
            //DrawPathLabel(node, nodeRect);

            if (Widgets.ButtonInvisible(nodeRect))
            {
                HandleNodeClick(node, canUnlockResult);
            }

            if (Mouse.IsOver(nodeRect))
            {
                DrawDescription(nodeRect, node);
            }
        }

        private void HandlePathSelectionClick(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            if (node.BelongsToUpgradePath && node.type != UpgradeTreeNodeDef.NodeType.Start && !treeHandler.HasSelectedAPath())
            {
                if (Widgets.ButtonInvisible(nodeRect))
                {
                    if (treeHandler.CanSelectPath(node.path))
                    {
                        UnlockResult result = treeHandler.SelectPath(node.path);
                        if (result.Success)
                        {
                            var predecessors = node.GetPredecessors(treeDef)
                                                 .Where(p => treeHandler.IsNodeUnlocked(p));

                            foreach (var predecessor in predecessors)
                            {
                                activeAnimations.Add(new ConnectionAnimation
                                {
                                    startTime = Time.time,
                                    duration = 0.5f,
                                    startPos = nodePositions[predecessor].center,
                                    endPos = nodePositions[node].center,
                                    color = skin.unlockedNodeColor
                                });
                            }
                            Log.Message($"Selected {node.path.defName} upgrade path.");
                            Messages.Message($"Selected {node.path.defName} upgrade path.", MessageTypeDefOf.NeutralEvent);
                        }
                        else
                        {
                            Messages.Message(result.Message, MessageTypeDefOf.RejectInput);
                        }
                    }
                    else
                    {
                        Messages.Message("Cannot select this path - conflicts with existing selection", MessageTypeDefOf.RejectInput);
                    }
                    return;
                }
            }
        }

        private void HandleNodeClick(UpgradeTreeNodeDef node, UnlockResult canUnlockResult)
        {
            if (canUnlockResult.Success)
            {
                if (treeHandler is ActiveTreeHandler activeHandler)
                {
                    UnlockResult result = activeHandler.TryUnlockNode(node);
                    if (!result.Success)
                    {
                        Messages.Message(result.Message, MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    treeHandler.UnlockUpgrade(node.upgrade);
                }
            }
            else
            {
                Messages.Message(canUnlockResult.Message, MessageTypeDefOf.RejectInput);
            }
        }

        private void DrawPathLabel(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            if (node.BelongsToUpgradePath && !allNodes.Any(n => n != node && n.path == node.path && nodePositions[n].y < nodePositions[node].y))
            {
                Text.Anchor = TextAnchor.LowerCenter;
                Text.Font = skin.labelFont;

                float pathLabelWidth = Text.CalcSize(node.path.defName).x;
                Rect pathLabelRect = new Rect(
                    nodeRect.x + (nodeRect.width / 2) - (pathLabelWidth / 2),
                    nodeRect.y - skin.pathLabelOffset,
                    pathLabelWidth,
                    20f
                );

                Color pathColor = GetPathColor(node.path);
                GUI.color = pathColor;
                Widgets.DrawHighlight(pathLabelRect);
                Widgets.Label(pathLabelRect, node.path.defName);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawNodeLabel(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            if (node.upgrade?.label != null)
            {
                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = skin.labelFont;
                GUI.color = skin.labelColor;

                float labelWidth = Text.CalcSize(node.upgrade.label).x;
                Rect labelRect = new Rect(
                    nodeRect.x + (nodeRect.width / 2) - (labelWidth / 2),
                    nodeRect.yMax + skin.labelOffset,
                    labelWidth,
                    25f
                );

                Widgets.DrawHighlight(labelRect);
                Widgets.Label(labelRect, node.upgrade.label);

                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawNodeIcon(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            Rect iconRect = nodeRect.ContractedBy(8f);


            Texture2D icon = null;

            if (!string.IsNullOrEmpty(node.upgrade?.uiIcon))
            {
                icon = ContentFinder<Texture2D>.Get(node.upgrade.uiIcon);
            }
            else
            {
                icon = ContentFinder<Texture2D>.Get("Things/Building/Misc/DropBeacon");
            }

            if (icon != null)
            {
                Widgets.DrawTextureFitted(iconRect, icon, 1f);
            }
        }

        private void DrawNodeIconBG(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            if (!string.IsNullOrEmpty(node.upgrade?.uiIcon))
            {
                Rect iconRect = nodeRect.ContractedBy(1f);
                Texture2D icon = ContentFinder<Texture2D>.Get(node.upgrade.uiIcon);
                if (icon != null)
                {
                    Widgets.DrawTextureFitted(iconRect, icon, 1f);
                }
            }
        }

        private void DrawToolbar(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            displayStrategy.DrawControls(rect);
        }

        private Color GetPathColor(UpgradePathDef path)
        {
            if (treeHandler.IsPathSelected(path))
                return skin.pathSelectedColor;

            if (treeHandler.CanSelectPath(path))
                return skin.pathAvailableColor;

            return skin.pathExcludedColor;
        }

        private void DrawConnections()
        {
            foreach (var node in allNodes)
            {
                if (node.connections != null)
                {
                    foreach (var connection in node.connections)
                    {
                        if (nodePositions.ContainsKey(node) && nodePositions.ContainsKey(connection))
                        {
                            DrawConnection(node, connection);
                        }
                    }
                }
            }
        }

        private void DrawConnection(UpgradeTreeNodeDef from, UpgradeTreeNodeDef to)
        {
            Vector2 start = nodePositions[from].center;
            Vector2 end = nodePositions[to].center;

            bool isPathActive = treeHandler.IsNodeUnlocked(from) ||
                              treeHandler.IsNodeUnlocked(to);

            Color lineColor;
            if (treeHandler.IsNodeUnlocked(from) && treeHandler.IsNodeUnlocked(to))
            {
                lineColor = skin.unlockedConnectionColor;
            }
            else if (isPathActive)
            {
                lineColor = skin.activeConnectionColor;
            }
            else
            {
                lineColor = skin.inactiveConnectionColor;
            }

            Widgets.DrawLine(start, end, lineColor, skin.connectionThickness);

            if (skin.showConnectionArrows && Vector2.Distance(start, end) > skin.nodeSize)
            {
                DrawConnectionArrow(start, end, lineColor);
            }
        }

        private void DrawConnectionArrow(Vector2 start, Vector2 end, Color color)
        {
            Vector2 direction = (start - end).normalized;
            Vector2 arrowCenter = Vector2.Lerp(start, end, 0.5f);

            Vector2 right = new Vector2(-direction.y, direction.x);
            Vector2 arrowPoint1 = arrowCenter + (direction * skin.arrowSize / 2) + (right * skin.arrowSize / 2);
            Vector2 arrowPoint2 = arrowCenter + (direction * skin.arrowSize / 2) - (right * skin.arrowSize / 2);
            Vector2 arrowBase = arrowCenter - (direction * skin.arrowSize / 2);

            Widgets.DrawLine(arrowBase, arrowPoint1, color, skin.connectionThickness);
            Widgets.DrawLine(arrowBase, arrowPoint2, color, skin.connectionThickness);
        }

        private void DrawDescription(Rect nodeRect, UpgradeTreeNodeDef node)
        {
            bool isUnlocked = treeHandler.IsNodeUnlocked(node);
            UnlockResult canUnlockResult = treeHandler.CanUnlockNode(node);
            string tooltip = node.upgrade?.description ?? "Unknown upgrade";

            tooltip += "\n\n=== Debug Info ===";
            tooltip += $"\nNode: {node.defName}";
            tooltip += $"\nType: {node.type}";
            tooltip += $"\nUnlocked: {isUnlocked}";
            tooltip += $"\nCan Unlock: {canUnlockResult.Success}";

            if (node.BelongsToUpgradePath)
            {
                tooltip += $"\nPath: {node.path.defName}";
                tooltip += $"\nPath Selected: {treeHandler.IsPathSelected(node.path)}";
            }

            tooltip += $"\nConnections: {node.ConnectionCount}";
            if (node.connections != null && node.connections.Any())
            {
                tooltip += "\nConnected to:";
                foreach (var conn in node.connections)
                {
                    tooltip += $"\n - {conn.defName} (Unlocked: {treeHandler.IsNodeUnlocked(conn)})";
                }
            }

            var predecessors = node.GetPredecessors(treeDef).ToList();
            tooltip += $"\nPredecessors: {predecessors.Count}";
            if (predecessors.Any())
            {
                tooltip += "\nPreceded by:";
                foreach (var pred in predecessors)
                {
                    tooltip += $"\n - {pred.defName} (Unlocked: {treeHandler.IsNodeUnlocked(pred)})";
                }
            }

            if (!isUnlocked && !canUnlockResult.Success)
            {
                tooltip += $"\n\nUnlock Error: {canUnlockResult.Message}";
            }

            TooltipHandler.TipRegion(nodeRect, tooltip);
        }

        private Color GetNodeColor(UpgradeTreeNodeDef node)
        {
            bool isUnlocked = treeHandler.IsNodeUnlocked(node);

            if (node.BelongsToUpgradePath)
            {
                if (isUnlocked)
                    return skin.unlockedNodeColor;

                if (treeHandler.IsPathSelected(node.path))
                    return skin.pathSelectedColor;

                return treeHandler.CanSelectPath(node.path) ? skin.pathAvailableColor : skin.pathExcludedColor;
            }

            UnlockResult canUnlockResult = treeHandler.CanUnlockNode(node);
            return isUnlocked ? skin.unlockedNodeColor : (canUnlockResult.Success ? skin.availableNodeColor : skin.lockedNodeColor);
        }
    }
    //public class ParasiteTreeUI : Window
    //{
    //    private readonly Gene_Parasite parasiteGene;
    //    private readonly UpgradeTreeDef treeDef;
    //    private readonly BaseTreeHandler treeHandler;
    //    private Vector2 scrollPosition;
    //    private float NodeSize = 45f;
    //    private float NodeSpacing = 15f;
    //    private float ConnectionThickness = 1.5f;
    //    private Texture2D TreeBackground;
    //    private Color UnlockedColor = new Color(0.2f, 0.8f, 0.2f);
    //    private Color AvailableColor = Color.grey;
    //    private Color LockedColor = Color.red;
    //    private readonly List<UpgradeTreeNodeDef> allNodes;
    //    private readonly ITreeDisplayStrategy displayStrategy;
    //    private Dictionary<UpgradeTreeNodeDef, Rect> nodePositions;
    //    private Color PathSelectedColor = Color.yellow;
    //    private Color PathExcludedColor = Color.red;
    //    private Color PathAvailableColor = Color.white;
    //    private const float TOOLBAR_HEIGHT = 35f;
    //    private readonly UpgradeTreeSkinDef skin;

    //    public override Vector2 InitialSize
    //    {
    //        get
    //        {
    //            return new Vector2(450f, 800f);
    //        }
    //    }

    //    public ParasiteTreeUI(Gene_Parasite parasite, UpgradeTreeDef tree, BaseTreeHandler handler, TreeDisplayStrategyDef displayStrategyDef)
    //    {
    //        parasiteGene = parasite;
    //        treeDef = tree;
    //        treeHandler = handler;
    //        allNodes = treeDef.GetAllNodes();

    //        skin = tree.skin ?? DefDatabase<UpgradeTreeSkinDef>.GetNamed("DefaultTreeSkin");
    //        NodeSize = skin.nodeSize;
    //        NodeSpacing = skin.nodeSpacing;
    //        TreeBackground = skin.BackgroundTexture;

    //        displayStrategy = (ITreeDisplayStrategy)Activator.CreateInstance(
    //            displayStrategyDef.strategyClass);

    //        doCloseButton = false;
    //        doCloseX = true;
    //        forcePause = true;
    //        absorbInputAroundWindow = false;
    //    }
    //    private List<UIAnimationState> activeAnimations = new List<UIAnimationState>();
    //    public override void DoWindowContents(Rect inRect)
    //    {
    //        Rect toolbarRect = new Rect(0f, 0f, inRect.width, TOOLBAR_HEIGHT);
    //        DrawToolbar(toolbarRect);

    //        nodePositions = displayStrategy.PositionNodes(allNodes, inRect, NodeSize, NodeSpacing);

    //        DrawConnections();
    //        DrawNodes();

    //        activeAnimations.RemoveAll(a => a.Finished);

    //        foreach (var anim in activeAnimations)
    //        {
    //            anim.Animate();
    //        }
    //    }

    //    private void DrawNodes()
    //    {
    //        foreach (var kvp in nodePositions)
    //        {
    //            DrawNode(kvp.Key, kvp.Value);
    //        }
    //    }

    //    private void DrawNode(UpgradeTreeNodeDef node, Rect nodeRect)
    //    {
    //        UnlockResult canUnlockResult = treeHandler.CanUnlockNode(node);
    //        bool isUnlocked = treeHandler.IsNodeUnlocked(node);

    //        //should only run if there's not lready a path selected for the tree, otherwise it blocks the unlock
    //        if (node.BelongsToUpgradePath && node.type != UpgradeTreeNodeDef.NodeType.Start && !treeHandler.HasSelectedAPath())
    //        {
    //            if (Widgets.ButtonInvisible(nodeRect))
    //            {
    //                if (treeHandler.CanSelectPath(node.path))
    //                {
    //                    UnlockResult result = treeHandler.SelectPath(node.path);
    //                    if (result.Success)
    //                    {           
    //                        // Find the predecessor node(s) that are already unlocked
    //                        var predecessors = node.GetPredecessors(treeDef)
    //                                             .Where(p => treeHandler.IsNodeUnlocked(p));

    //                        foreach (var predecessor in predecessors)
    //                        {
    //                            activeAnimations.Add(new ConnectionAnimation
    //                            {
    //                                startTime = Time.time,
    //                                duration = 0.5f,
    //                                startPos = nodePositions[predecessor].center,
    //                                endPos = nodePositions[node].center,
    //                                color = UnlockedColor
    //                            });
    //                        }
    //                        Log.Message($"Selected {node.path.defName} upgrade path.");
    //                        Messages.Message($"Selected {node.path.defName} upgrade path.", MessageTypeDefOf.NeutralEvent);
    //                    }
    //                    else
    //                    {
    //                        Messages.Message(result.Message, MessageTypeDefOf.RejectInput);
    //                    }
    //                }
    //                else
    //                {
    //                    Messages.Message("Cannot select this path - conflicts with existing selection", MessageTypeDefOf.RejectInput);
    //                }
    //                return;
    //            }
    //        }

    //        GUI.color = GetNodeColor(node);
    //        GUI.DrawTexture(nodeRect, TreeBackground);

    //        GUI.color = Color.white;

    //        DrawNodeIcon(node, nodeRect);

    //        DrawNodeLabel(node, nodeRect);

    //        DrawPathLabel(node, nodeRect);

    //        if (Widgets.ButtonInvisible(nodeRect))
    //        {
    //            if (canUnlockResult.Success)
    //            {
    //                if (treeHandler is ActiveTreeHandler activeHandler)
    //                {
    //                    UnlockResult result = activeHandler.TryUnlockNode(node);
    //                    if (!result.Success)
    //                    {
    //                        Messages.Message(result.Message, MessageTypeDefOf.RejectInput);
    //                    }
    //                }
    //                else
    //                {
    //                    treeHandler.UnlockUpgrade(node.upgrade);
    //                }
    //            }
    //            else
    //            {
    //                Messages.Message(canUnlockResult.Message, MessageTypeDefOf.RejectInput);
    //            }
    //        }

    //        if (Mouse.IsOver(nodeRect))
    //        {
    //            DrawDescription(nodeRect, node);
    //        }
    //    }

    //    private void DrawPathLabel(UpgradeTreeNodeDef node, Rect nodeRect)
    //    {
    //        // Draw path label above topmost node in path
    //        if (node.BelongsToUpgradePath && !allNodes.Any(n => n != node && n.path == node.path && nodePositions[n].y < nodePositions[node].y))
    //        {
    //            Text.Anchor = TextAnchor.LowerCenter;
    //            Text.Font = GameFont.Small;

    //            float pathLabelWidth = Text.CalcSize(node.path.defName).x;
    //            Rect pathLabelRect = new Rect(
    //                nodeRect.x + (nodeRect.width / 2) - (pathLabelWidth / 2),
    //                nodeRect.y - 25f,
    //                pathLabelWidth,
    //                20f
    //            );

    //            Color pathColor = GetPathColor(node.path);
    //            GUI.color = pathColor;
    //            Widgets.DrawHighlight(pathLabelRect);
    //            Widgets.Label(pathLabelRect, node.path.defName);
    //            GUI.color = Color.white;
    //            Text.Anchor = TextAnchor.UpperLeft;
    //        }
    //    }

    //    private void DrawNodeLabel(UpgradeTreeNodeDef node, Rect nodeRect)
    //    {
    //        if (node.upgrade?.label != null)
    //        {
    //            Text.Anchor = TextAnchor.UpperCenter;
    //            Text.Font = GameFont.Small;

    //            float labelWidth = Text.CalcSize(node.upgrade.label).x;
    //            Rect labelRect = new Rect(
    //                nodeRect.x + (nodeRect.width / 2) - (labelWidth / 2), // Center the label based on its actual width
    //                nodeRect.yMax + 5f,
    //                labelWidth,
    //                25f
    //            );
    //            Widgets.DrawHighlight(labelRect);
    //            Widgets.Label(labelRect, node.upgrade.label);
    //            Text.Anchor = TextAnchor.UpperLeft;
    //        }
    //    }

    //    private void DrawNodeIcon(UpgradeTreeNodeDef node, Rect nodeRect)
    //    {
    //        // Draw icon or node center
    //        if (!string.IsNullOrEmpty(node.upgrade?.uiIcon))
    //        {
    //            Rect iconRect = nodeRect.ContractedBy(8f);
    //            Texture2D icon = ContentFinder<Texture2D>.Get(node.upgrade.uiIcon);
    //            if (icon != null)
    //            {
    //                Widgets.DrawTextureFitted(iconRect, icon, 1f);
    //            }
    //        }
    //    }
    //    private void DrawToolbar(Rect rect)
    //    {
    //        Widgets.DrawMenuSection(rect);
    //        displayStrategy.DrawControls(rect);
    //    }
    //    private Color GetPathColor(UpgradePathDef path)
    //    {
    //        if (treeHandler.IsPathSelected(path))
    //            return PathSelectedColor;

    //        if (treeHandler.CanSelectPath(path))
    //            return PathAvailableColor;

    //        return PathExcludedColor;
    //    }

    //    private void DrawConnections()
    //    {
    //        foreach (var node in allNodes)
    //        {
    //            if (node.connections != null)
    //            {
    //                foreach (var connection in node.connections)
    //                {
    //                    if (nodePositions.ContainsKey(node) && nodePositions.ContainsKey(connection))
    //                    {
    //                        DrawConnection(node, connection);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    private void DrawConnection(UpgradeTreeNodeDef from, UpgradeTreeNodeDef to)
    //    {
    //        Vector2 start = nodePositions[from].center;
    //        Vector2 end = nodePositions[to].center;

    //        bool isPathActive = treeHandler.IsNodeUnlocked(from) ||
    //                          treeHandler.IsNodeUnlocked(to);

    //        Color lineColor;
    //        if (treeHandler.IsNodeUnlocked(from) && treeHandler.IsNodeUnlocked(to))
    //        {
    //            lineColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    //        }
    //        else if (isPathActive)
    //        {
    //            lineColor = new Color(0.8f, 0.8f, 0.2f, 0.6f);
    //        }
    //        else
    //        {
    //            lineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    //        }

    //        Widgets.DrawLine(start, end, lineColor, ConnectionThickness);

    //        if (Vector2.Distance(start, end) > NodeSize)
    //        {
    //            Vector2 direction = (start - end).normalized;
    //            float arrowSize = 10f;
    //            Vector2 arrowCenter = Vector2.Lerp(start, end, 0.5f);

    //            Vector2 right = new Vector2(-direction.y, direction.x);
    //            Vector2 arrowPoint1 = arrowCenter + (direction * arrowSize / 2) + (right * arrowSize / 2);
    //            Vector2 arrowPoint2 = arrowCenter + (direction * arrowSize / 2) - (right * arrowSize / 2);
    //            Vector2 arrowBase = arrowCenter - (direction * arrowSize / 2);

    //            Widgets.DrawLine(arrowBase, arrowPoint1, lineColor, ConnectionThickness);
    //            Widgets.DrawLine(arrowBase, arrowPoint2, lineColor, ConnectionThickness);
    //        }
    //    }

    //    private void DrawDescription(Rect nodeRect, UpgradeTreeNodeDef node)
    //    {
    //        bool isUnlocked = treeHandler.IsNodeUnlocked(node);
    //        UnlockResult canUnlockResult = treeHandler.CanUnlockNode(node);
    //        string tooltip = node.upgrade?.description ?? "Unknown upgrade";

    //        // Build debug info
    //        tooltip += "\n\n=== Debug Info ===";
    //        tooltip += $"\nNode: {node.defName}";
    //        tooltip += $"\nType: {node.type}";
    //        tooltip += $"\nUnlocked: {isUnlocked}";
    //        tooltip += $"\nCan Unlock: {canUnlockResult.Success}";

    //        if (node.BelongsToUpgradePath)
    //        {
    //            tooltip += $"\nPath: {node.path.defName}";
    //            tooltip += $"\nPath Selected: {treeHandler.IsPathSelected(node.path)}";
    //        }

    //        // Connection info
    //        tooltip += $"\nConnections: {node.ConnectionCount}";
    //        if (node.connections != null && node.connections.Any())
    //        {
    //            tooltip += "\nConnected to:";
    //            foreach (var conn in node.connections)
    //            {
    //                tooltip += $"\n - {conn.defName} (Unlocked: {treeHandler.IsNodeUnlocked(conn)})";
    //            }
    //        }

    //        // Predecessor info
    //        var predecessors = node.GetPredecessors(treeDef).ToList();
    //        tooltip += $"\nPredecessors: {predecessors.Count}";
    //        if (predecessors.Any())
    //        {
    //            tooltip += "\nPreceded by:";
    //            foreach (var pred in predecessors)
    //            {
    //                tooltip += $"\n - {pred.defName} (Unlocked: {treeHandler.IsNodeUnlocked(pred)})";
    //            }
    //        }

    //        // Error message if can't unlock
    //        if (!isUnlocked && !canUnlockResult.Success)
    //        {
    //            tooltip += $"\n\nUnlock Error: {canUnlockResult.Message}";
    //        }

    //        TooltipHandler.TipRegion(nodeRect, tooltip);
    //    }

    //    private Color GetNodeColor(UpgradeTreeNodeDef node)
    //    {
    //        bool isUnlocked = treeHandler.IsNodeUnlocked(node);

    //        if (node.BelongsToUpgradePath)
    //        {
    //            if (isUnlocked)
    //                return UnlockedColor;

    //            if (treeHandler.IsPathSelected(node.path))
    //                return PathSelectedColor;

    //            return treeHandler.CanSelectPath(node.path) ? PathAvailableColor : PathExcludedColor;
    //        }

    //        UnlockResult canUnlockResult = treeHandler.CanUnlockNode(node);
    //        return isUnlocked ? UnlockedColor : (canUnlockResult.Success ? AvailableColor : LockedColor);
    //    }
    //}
}
