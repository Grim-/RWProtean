using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Protean
{
    public class PassiveTreeHandler : BaseTreeHandler
    {
        private int currentLevel;
        private int unspentLevels;
        private int availablePoints;
        private bool HasChosenPath => selectedPaths.Any();

        public PassiveTreeHandler()
        {
        }

        public PassiveTreeHandler(Pawn pawn, Gene_Parasite gene, UpgradeTreeDef treeDef)
            : base(pawn, gene, treeDef)
        {
            this.currentLevel = 0;
            this.unspentLevels = 0;
            this.availablePoints = 0;

            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                TryUnlockNextUpgrade(this.treeDef.nodes[0], true);
            }
        }

        protected override UnlockResult ValidateTypeSpecificRules(UpgradeTreeNodeDef node)
        {
            int currentProgress = GetNodeProgress(node);
            if (currentProgress >= node.upgrades.Count)
                return UnlockResult.Failed(UpgradeUnlockError.AlreadyUnlocked, "Already unlocked all upgrades in this node");

            if (availablePoints < node.upgrades[currentProgress].pointCost)
            {
                Log.Message($"Insufficient points for node {node.defName}. Available: {availablePoints}, Required: {node.upgrades[currentProgress].pointCost}");
                return UnlockResult.Failed(UpgradeUnlockError.InsufficientPoints,
                    string.Format("Requires {0} points", node.upgrades[currentProgress].pointCost));
            }
            return UnlockResult.Succeeded();
        }
        private void ForceUnlockNode(UpgradeTreeNodeDef node)
        {
            if (node == null || IsNodeFullyUnlocked(node)) return;

            // Force unlock all upgrades in the node
            while (GetNodeProgress(node) < node.upgrades.Count)
            {
                TryUnlockNextUpgrade(node);
            }
        }
        public UnlockResult TryUnlockNode(UpgradeTreeNodeDef node)
        {
            UnlockResult result = CanUnlockNode(node);
            if (!result.Success)
            {
                return result;
            }

            int currentProgress = GetNodeProgress(node);
            if (currentProgress >= node.upgrades.Count)
            {
                return UnlockResult.Failed(UpgradeUnlockError.AlreadyUnlocked, "Node is already fully unlocked");
            }

            int cost = node.upgrades[currentProgress].pointCost;
            if (availablePoints < cost)
            {
                return UnlockResult.Failed(UpgradeUnlockError.InsufficientPoints, $"Requires {cost} points");
            }

            availablePoints -= cost;
            UnlockResult unlockResult = TryUnlockNextUpgrade(node);

            if (!unlockResult.Success)
            {
                // Refund points if unlock failed
                availablePoints += cost;
            }

            return unlockResult;
        }

        public override void OnPathSelected(UpgradePathDef path)
        {
            Log.Message($"Path selected: {path.defName}, Unspent levels: {unspentLevels}");
            if (unspentLevels > 0)
            {
                availablePoints += unspentLevels;
                unspentLevels = 0;
                AutoUnlockAvailableNodes();
            }
        }

        public void OnLevelUp(int newLevel)
        {
            Log.Message($"Level up from {currentLevel} to {newLevel}. HasChosenPath: {HasChosenPath}");
            int levelsGained = newLevel - currentLevel;
            this.currentLevel = newLevel;

            if (!HasChosenPath)
            {
                unspentLevels += levelsGained;
                Log.Message($"Storing {unspentLevels} unspent levels");
            }
            else
            {
                availablePoints += levelsGained;
                Log.Message($"Adding {levelsGained} points, now have {availablePoints}");
                AutoUnlockAvailableNodes();
            }
        }

        private void AutoUnlockAvailableNodes()
        {
            if (!HasChosenPath)
            {
                Log.Message("No path chosen, skipping auto-unlock");
                return;
            }

            Log.Message("Attempting to auto-unlock available nodes");
            bool unlocked;
            do
            {
                unlocked = false;
                var availableNodes = treeDef.GetAllNodes()
                    .Where(n => !IsNodeFullyUnlocked(n) &&
                               ValidateUpgradePath(n) &&
                               n.type != NodeType.Start);

                foreach (var node in availableNodes)
                {
                    UnlockResult result = CanUnlockNode(node);
                    if (result.Success)
                    {
                        Log.Message($"Auto-unlocking node: {node.defName}");
                        if (TryUnlockNode(node).Success)
                        {
                            unlocked = true;
                            break; // Only unlock one node per iteration
                        }
                    }
                }
            } while (unlocked && availablePoints > 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref currentLevel, "currentLevel", 0);
            Scribe_Values.Look(ref unspentLevels, "unspentLevels", 0);
            Scribe_Values.Look(ref availablePoints, "availablePoints", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (unlockedNodes.Count == 0)
                {
                    UnlockStartNode();
                }

                if (selectedPaths != null && selectedPaths.Any())
                {
                    foreach (var path in selectedPaths.ToList())
                    {
                        OnPathSelected(path);
                    }
                }

                if (HasSelectedAPath())
                {
                    int spentPoints = 0;
                    foreach (var node in treeDef.GetAllNodes())
                    {
                        if (node.type != NodeType.Start)
                        {
                            int progress = GetNodeProgress(node);
                            for (int i = 0; i < progress && i < node.upgrades.Count; i++)
                            {
                                spentPoints += node.upgrades[i].pointCost;
                            }
                        }
                    }
                    availablePoints = currentLevel - spentPoints;

                    AutoUnlockAvailableNodes();
                }
                else if (currentLevel > 0)
                {
                    unspentLevels = currentLevel;
                    availablePoints = 0;
                }
            }
        }
    }
}

