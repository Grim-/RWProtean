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
        }

        protected override UnlockResult ValidateTypeSpecificRules(UpgradeTreeNodeDef node)
        {
            if (availablePoints < node.upgrade.pointCost)
            {
                Log.Message($"Insufficient points for node {node.defName}. Available: {availablePoints}, Required: {node.upgrade.pointCost}");
                return UnlockResult.Failed(UpgradeUnlockError.InsufficientPoints,
                    string.Format("Requires {0} points", node.upgrade.pointCost));
            }
            return UnlockResult.Succeeded();
        }

        public UnlockResult TryUnlockNode(UpgradeTreeNodeDef node)
        {
            UnlockResult result = CanUnlockNode(node);
            if (!result.Success)
            {
                return result;
            }

            UnlockNode(node);
            availablePoints -= node.upgrade.pointCost;
            return UnlockResult.Succeeded();
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
                    .Where(n => !IsNodeUnlocked(n) &&
                               ValidateUpgradePath(n) &&
                               n.type != NodeType.Start);

                foreach (var node in availableNodes)
                {
                    UnlockResult result = CanUnlockNode(node);
                    if (result.Success)
                    {
                        Log.Message($"Auto-unlocking node: {node.defName}");
                        TryUnlockNode(node);
                        unlocked = true;
                        break; // Only unlock one node per iteration
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
                // For new games, unlock start node
                if (unlockedNodes.Count == 0)
                {
                    UnlockStartNode();
                }

                // Re-trigger path selection effects for loaded paths
                if (selectedPaths != null && selectedPaths.Any())
                {
                    foreach (var path in selectedPaths.ToList())
                    {
                        OnPathSelected(path);
                    }
                }

                if (HasSelectedAPath())
                {
                    // Recalculate available points based on level and spent points
                    int spentPoints = 0;
                    foreach (var node in unlockedNodes)
                    {
                        if (node.type != NodeType.Start && node.upgrade != null)
                        {
                            spentPoints += node.upgrade.pointCost;
                        }
                    }
                    availablePoints = currentLevel - spentPoints;

                    // Trigger auto-unlock after points are recalculated
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

