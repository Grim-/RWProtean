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
        private const float NodeSize = 40f;
        private const float NodeSpacing = 20f;
        private const float ConnectionThickness = 2f;
        private static readonly Texture2D TreeBackground = ContentFinder<Texture2D>.Get("UI/TreePassiveBorder");
        private Color UnlockedColor = new Color(0.2f, 0.8f, 0.2f);
        private Color AvailableColor = Color.grey;
        private Color LockedColor = Color.red;
        private readonly List<UpgradeTreeNodeDef> allNodes;
        private readonly ITreeDisplayStrategy displayStrategy;
        private Dictionary<UpgradeTreeNodeDef, Rect> nodePositions;

        public override Vector2 InitialSize => new Vector2(300f, 600f);

        public ParasiteTreeUI(Gene_Parasite parasite, UpgradeTreeDef tree, BaseTreeHandler handler)
        {
            parasiteGene = parasite;
            treeDef = tree;
            treeHandler = handler;
            allNodes = treeDef.GetAllNodes();
            displayStrategy = new VerticalTreeStrategy(true);
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            nodePositions = displayStrategy.PositionNodes(allNodes, inRect, NodeSize, NodeSpacing);

            DrawConnections();
            DrawNodes();
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
            bool canUnlock = parasiteGene.CanUnlockNode(node);
            bool isUnlocked = parasiteGene.IsNodeUnlocked(node);

            GUI.color = GetNodeColor(node);
            GUI.DrawTexture(nodeRect, TreeBackground);

            GUI.color = Color.white;

            // Draw icon or node center
            if (!string.IsNullOrEmpty(node.upgrade?.uiIcon))
            {
                Rect iconRect = nodeRect.ContractedBy(8f);
                Texture2D icon = ContentFinder<Texture2D>.Get(node.upgrade.uiIcon);
                if (icon != null)
                {
                    Widgets.DrawTextureFitted(iconRect, icon, 1f);
                }
            }

            // Draw label underneath
            if (node.upgrade?.label != null)
            {
                Text.Anchor = TextAnchor.UpperCenter;
                Rect labelRect = new Rect(nodeRect.x - nodeRect.width / 2, nodeRect.yMax + 5f, nodeRect.width * 2f, 25f);
                Widgets.Label(labelRect, node.upgrade.label);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // Draw path name if this is the first node in a path
            if (node.BelongsToUpgradePath && !allNodes.Any(n => n != node && n.path == node.path && nodePositions[n].y < nodePositions[node].y))
            {
                Text.Anchor = TextAnchor.LowerCenter;
                Rect pathLabelRect = new Rect(nodeRect.x - nodeRect.width, nodeRect.y - 25f, nodeRect.width * 2f, 20f);
                GUI.color = Color.yellow;
                Widgets.Label(pathLabelRect, node.path.defName);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            if (Widgets.ButtonInvisible(nodeRect))
            {
                if (canUnlock)
                    parasiteGene.UnlockUpgrade(node.upgrade);
            }

            if (Mouse.IsOver(nodeRect))
            {
                DrawDescription(nodeRect, node);
            }
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

            bool isPathActive = parasiteGene.IsNodeUnlocked(from) ||
                              parasiteGene.IsNodeUnlocked(to);

            Color lineColor;
            if (parasiteGene.IsNodeUnlocked(from) && parasiteGene.IsNodeUnlocked(to))
            {
                lineColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            }
            else if (isPathActive)
            {
                lineColor = new Color(0.8f, 0.8f, 0.2f, 0.6f);
            }
            else
            {
                lineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }

            Widgets.DrawLine(start, end, lineColor, ConnectionThickness);

            if (Vector2.Distance(start, end) > NodeSize)
            {
                Vector2 direction = (start - end).normalized;
                float arrowSize = 10f;
                Vector2 arrowCenter = Vector2.Lerp(start, end, 0.5f);

                Vector2 right = new Vector2(-direction.y, direction.x);
                Vector2 arrowPoint1 = arrowCenter + (direction * arrowSize / 2) + (right * arrowSize / 2);
                Vector2 arrowPoint2 = arrowCenter + (direction * arrowSize / 2) - (right * arrowSize / 2);
                Vector2 arrowBase = arrowCenter - (direction * arrowSize / 2);

                Widgets.DrawLine(arrowBase, arrowPoint1, lineColor, ConnectionThickness);
                Widgets.DrawLine(arrowBase, arrowPoint2, lineColor, ConnectionThickness);
            }
        }

        private void DrawDescription(Rect nodeRect, UpgradeTreeNodeDef node)
        {
            bool isUnlocked = parasiteGene.IsNodeUnlocked(node);
            bool canUnlock = parasiteGene.CanUnlockNode(node);
            string tooltip = node.upgrade?.description ?? "Unknown upgrade";

            if (!isUnlocked && !canUnlock)
            {
                tooltip += "\n\nLocked - Requires previous upgrades";
            }
            TooltipHandler.TipRegion(nodeRect, tooltip);
        }

        private Color GetNodeColor(UpgradeTreeNodeDef node)
        {
            bool isUnlocked = parasiteGene.IsNodeUnlocked(node);
            bool canUnlock = parasiteGene.CanUnlockNode(node);
            return isUnlocked ? UnlockedColor : (canUnlock ? AvailableColor : LockedColor);
        }
    }
}
