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
            if (displayStrategyDef == null)
            {
                Log.Error($"Display Strategy Def was null");
                return;
            }

            parasiteGene = parasite;
            treeDef = tree;
            treeHandler = handler;
            allNodes = treeDef.GetAllNodes();
            skin = tree.GetSkin();

            displayStrategy = (ITreeDisplayStrategy)Activator.CreateInstance(displayStrategyDef.strategyClass);

            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Draw background to fill the entire content area first
            Widgets.DrawTextureFitted(inRect, skin.BackgroundTexture, 1f);

            // Then draw toolbar
            Rect toolbarRect = new Rect(0f, 0f, inRect.width, skin.toolbarHeight);
            DrawToolbar(toolbarRect);

            // Rest of the window contents
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
            int currentProgress = treeHandler.GetNodeProgress(node);
            bool isFullyUnlocked = treeHandler.IsNodeFullyUnlocked(node);

            GUI.color = treeHandler.GetNodeColor(node, currentProgress);
            GUI.DrawTexture(nodeRect, skin.NodeTexture);
            GUI.color = Color.white;

            if (currentProgress > 0 && !isFullyUnlocked)
            {
                float progressPercentage = (float)currentProgress / node.upgrades.Count;
                Rect progressRect = nodeRect;
                progressRect.height *= progressPercentage;
                progressRect.y = nodeRect.yMax - progressRect.height;

                GUI.color = Color.red;
                GUI.DrawTexture(progressRect, skin.NodeTexture);
                GUI.color = Color.white;
            }

            DrawNodeIconBG(node, nodeRect);
            DrawNodeIcon(node, nodeRect);
            DrawNodeBadge(node, nodeRect);

            HandleNodeClick(nodeRect, node, canUnlockResult);

            if (Mouse.IsOver(nodeRect))
            {
                DrawDescription(nodeRect, node, currentProgress);
            }
        }

        private void DrawNodeBadge(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            int progress = treeHandler.GetNodeProgress(node);
            string label = $"{progress}/{node.upgrades.Count}";

            Vector2 labelSize = Text.CalcSize(label);

            float xPos = nodeRect.x + (nodeRect.width - labelSize.x) / 2; 
            float yPos = nodeRect.y - labelSize.y - 2f;

            Rect badgeRect = new Rect(xPos, yPos, labelSize.x, labelSize.y);

            Widgets.Label(badgeRect, label);
        }

        private void HandleNodeClick(Rect nodeRect, UpgradeTreeNodeDef node, UnlockResult canUnlockResult)
        {
            if (Widgets.ButtonInvisible(nodeRect))
            {
                if (canUnlockResult.Success)
                {
                    // Try to unlock first
                    UnlockResult unlockResult = null;
                    if (treeHandler is ActiveTreeHandler activeHandler)
                    {
                        unlockResult = activeHandler.TryUnlockNode(node);
                        if (!unlockResult.Success)
                        {
                            Messages.Message(unlockResult.Message, MessageTypeDefOf.RejectInput);
                            return;
                        }
                    }
                    else
                    {
                        if (!treeHandler.IsNodeFullyUnlocked(node))
                        {
                            int progress = treeHandler.GetNodeProgress(node);

                            if (node.HasUpgrade(progress + 1))
                            {
                                treeHandler.UnlockUpgrade(node.GetUpgrade(progress + 1));
                            }
                        }
                       
                       
                    }

                    // If unlock succeeded and node has a path that can be selected, do it immediately
                    if (node.BelongsToUpgradePath &&
                        node.path != null &&
                        !treeHandler.IsPathSelected(node.path) &&
                        treeHandler.CanSelectPath(node.path))
                    {
                        UnlockResult pathResult = treeHandler.SelectPath(node.path);
                        if (pathResult.Success)
                        {
                            var predecessors = node.GetPredecessors(treeDef)
                                                 .Where(p => treeHandler.IsNodeFullyUnlocked(p));
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
                    }
                }
                // If can't unlock but has an unselected path that could be selected
                else if (node.BelongsToUpgradePath &&
                        node.path != null &&
                        !treeHandler.IsPathSelected(node.path))
                {
                    if (treeHandler.CanSelectPath(node.path))
                    {
                        UnlockResult result = treeHandler.SelectPath(node.path);
                        if (!result.Success)
                        {
                            Messages.Message(result.Message, MessageTypeDefOf.RejectInput);
                        }
                    }
                    else
                    {
                        Messages.Message("Cannot select this path - conflicts with existing selection", MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message(canUnlockResult.Message, MessageTypeDefOf.RejectInput);
                }
            }
        }

        private void OnUpgradeUnlocked(UpgradeTreeNodeDef node)
        {
            if (treeHandler.IsNodeFullyUnlocked(node))
            {
                HandleFullyUnlockedNode(node);

                var predecessors = node.GetPredecessors(treeDef)
                    .Where(p => treeHandler.IsNodeFullyUnlocked(p));

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
            }
        }

        private void HandleFullyUnlockedNode(UpgradeTreeNodeDef node)
        {
            if (node.BelongsToUpgradePath &&
                node.path != null &&
                !treeHandler.IsPathSelected(node.path) &&
                treeHandler.CanSelectPath(node.path))
            {
                HandlePathSelection(node);
            }
        }
        private void HandlePathSelection(UpgradeTreeNodeDef node)
        {
            if (treeHandler.CanSelectPath(node.path))
            {
                UnlockResult result = treeHandler.SelectPath(node.path);
                if (result.Success)
                {
                    var predecessors = node.GetPredecessors(treeDef)
                        .Where(p => treeHandler.IsNodeFullyUnlocked(p));

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
        }

        private void DrawNodeIcon(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            Rect iconRect = nodeRect.ContractedBy(8f);
            if (!node.upgrades.NullOrEmpty() && !string.IsNullOrEmpty(node.upgrades[0].uiIcon))
            {
                var icon = ContentFinder<Texture2D>.Get(node.upgrades[0].uiIcon);
                if (icon != null)
                {
                    Widgets.DrawTextureFitted(iconRect, icon, 1f);
                }
            }
        }
        private void DrawNodeIconBG(UpgradeTreeNodeDef node, Rect nodeRect)
        {
            //Rect iconRect = nodeRect.ContractedBy(1f);
            //Texture2D icon = skin.NodeBackgroundTexture;
            //if (icon != null)
            //{
            //    Widgets.DrawTextureFitted(iconRect, icon, 1f);
            //}
        }

        private void DrawDescription(Rect nodeRect, UpgradeTreeNodeDef node, int currentProgress)
        {
            bool isFullyUnlocked = treeHandler.IsNodeFullyUnlocked(node);
            UnlockResult canUnlockResult = treeHandler.CanUnlockNode(node);

            string tooltip = "";

            if (currentProgress < node.upgrades.Count)
            {
                var nextUpgrade = node.upgrades[currentProgress];
                tooltip = nextUpgrade?.description ?? "Unknown upgrade";
                tooltip += $"\n\nNext Upgrade: {nextUpgrade?.label ?? "Unknown"}";
            }
            else
            {
                tooltip = "All upgrades unlocked";
            }

            tooltip += $"\n\nProgress: {currentProgress}/{node.upgrades.Count}";

            if (currentProgress > 0)
            {
                tooltip += "\n\nUnlocked Upgrades:";
                for (int i = 0; i < currentProgress; i++)
                {
                    tooltip += $"\n- {node.upgrades[i].label}";
                }
            }

            tooltip += "\n=== Debug Info ===";
            tooltip += $"\nNode: {node.defName}";
            tooltip += $"\nType: {node.type}";
            tooltip += $"\nFully Unlocked: {isFullyUnlocked}";
            tooltip += $"\nCan Unlock Next: {canUnlockResult.Success}";

            if (node.BelongsToUpgradePath)
            {
                tooltip += $"\nPath: {node.path.defName}";
                tooltip += $"\nPath Selected: {treeHandler.IsPathSelected(node.path)}";
            }

            if (!isFullyUnlocked && !canUnlockResult.Success)
            {
                tooltip += $"\n\nUnlock Error: {canUnlockResult.Message}";
            }

            TooltipHandler.TipRegion(nodeRect, tooltip);
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

            Color lineColor = GetPathStatusColor(from, to);
            Widgets.DrawLine(start, end, lineColor, skin.connectionThickness);

            if (skin.showConnectionArrows && Vector2.Distance(start, end) > skin.nodeSize)
            {
                DrawConnectionArrow(start, end, lineColor);
            }
        }


        private Color GetPathStatusColor(UpgradeTreeNodeDef from, UpgradeTreeNodeDef to)
        {
            Color lineColor;
            switch (treeHandler.GetPathStatusBetweenNodes(from, to))
            {
                case BaseTreeHandler.PathStatus.Unlocked:
                    lineColor = skin.unlockedConnectionColor;
                    break;
                case BaseTreeHandler.PathStatus.Active:
                    lineColor = skin.activeConnectionColor;
                    break;
                case BaseTreeHandler.PathStatus.Locked:
                    lineColor = skin.inactiveConnectionColor;
                    break;
                default:
                    lineColor = Color.gray;
                    break;
            }
            return lineColor;
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

        private void DrawToolbar(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            displayStrategy.DrawToolBar(rect);
        }
    }
}
