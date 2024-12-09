using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Protean
{
    public abstract class BaseTreeHandler : IExposable
    {
        protected Pawn pawn;
        protected Gene_Parasite gene;
        protected UpgradeTreeDef treeDef;
        protected HashSet<UpgradeDef> unlockedUpgrades;
        protected HashSet<UpgradeTreeNodeDef> unlockedNodes;
        protected Dictionary<UpgradeDef, List<UpgradeEffect>> activeEffects;
        protected HashSet<UpgradePathDef> selectedPaths;

        protected BaseTreeHandler()
        {
        }

        public BaseTreeHandler(Pawn pawn, Gene_Parasite gene, UpgradeTreeDef treeDef)
        {
            this.pawn = pawn;
            this.gene = gene;
            this.treeDef = treeDef;
            this.unlockedUpgrades = new HashSet<UpgradeDef>();
            this.unlockedNodes = new HashSet<UpgradeTreeNodeDef>();
            this.activeEffects = new Dictionary<UpgradeDef, List<UpgradeEffect>>();
            this.selectedPaths = new HashSet<UpgradePathDef>();

            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                UnlockStartNode();
            }
        }

        public bool IsPathSelected(UpgradePathDef path) => selectedPaths.Contains(path);

        public virtual bool CanSelectPath(UpgradePathDef path)
        {
            if (path == null) return false;
            if (selectedPaths.Contains(path)) return false;

            // Get all nodes that belong to this path
            var nodesInPath = treeDef.GetAllNodes().Where(n => n.path == path).ToList();
            if (!nodesInPath.Any()) return false;

            // For each node in this path, check all its predecessors' paths
            foreach (var node in nodesInPath)
            {
                var predecessorPaths = node.GetPredecessors(treeDef)
                    .Where(p => p.path != null)
                    .Select(p => p.path)
                    .Distinct();

                // If any predecessor path is exclusive with our current selections
                // or is itself not selectable, then this path isn't selectable
                foreach (var predPath in predecessorPaths)
                {
                    if (predPath.IsPathExclusiveWith(selectedPaths) ||
                        selectedPaths.Any(sp => sp.IsPathExclusiveWith(predPath)))
                    {
                        return false;
                    }
                }
            }

            // Finally check this path's own exclusivity
            return !path.IsPathExclusiveWith(selectedPaths);
        }

        public virtual bool HasSelectedAPath()
        {
            return selectedPaths != null && selectedPaths.Count > 0;
        }

        public virtual UnlockResult SelectPath(UpgradePathDef path)
        {
            if (!CanSelectPath(path))
                return UnlockResult.Failed(UpgradeUnlockError.ExclusivePath, "Path conflicts with current selections");

            selectedPaths.Add(path);
            OnPathSelected(path);
            return UnlockResult.Succeeded();
        }

        protected virtual UnlockResult ValidateUnlock(UpgradeTreeNodeDef node)
        {
            if (node == null)
                return UnlockResult.Failed(UpgradeUnlockError.InvalidNode, "Invalid node");

            if (IsNodeUnlocked(node))
                return UnlockResult.Failed(UpgradeUnlockError.AlreadyUnlocked, "Already unlocked");

            // Prerequisites check
            if (node.type != NodeType.Start)
            {
                bool hasUnlockedPredecessor = node.GetPredecessors(treeDef).Any(IsNodeUnlocked);
                if (!hasUnlockedPredecessor)
                    return UnlockResult.Failed(UpgradeUnlockError.NoPrecedingNode, "Requires a previous upgrade");
            }

            // For non-branch nodes that belong to a path, that path must be selected
            if (node.type != NodeType.Branch && node.path != null && !IsPathSelected(node.path))
            {
                return UnlockResult.Failed(UpgradeUnlockError.ExclusivePath, "Must select path at branch point first");
            }

            return UnlockResult.Succeeded();
        }

        public bool IsNodeUnlocked(UpgradeTreeNodeDef node)
        {
            return node != null && unlockedNodes.Contains(node);
        }

        public virtual void UnlockStartNode()
        {
            Log.Message($"Unlocking Starting Node...");
            if (this.treeDef.nodes != null)
            {
                foreach (var item in this.treeDef.GetAllNodes())
                {
                    if (item.type == NodeType.Start && !IsNodeUnlocked(item))
                    {
                        UnlockNode(item);
                    }
                }
            }
        }

        public virtual void UnlockUpgrade(UpgradeDef upgrade)
        {
            if (!activeEffects.ContainsKey(upgrade))
            {
                activeEffects[upgrade] = new List<UpgradeEffect>();
                var effects = upgrade.CreateEffects();
                foreach (var effect in effects)
                {
                    effect.Apply(pawn);
                    activeEffects[upgrade].Add(effect);
                }
            }
            unlockedUpgrades.Add(upgrade);
        }

        protected void UnlockNode(UpgradeTreeNodeDef node)
        {
            if (node != null && node.upgrade != null)
            {
                UnlockUpgrade(node.upgrade);
                unlockedNodes.Add(node);
            }
        }


        public virtual UnlockResult CanUnlockNode(UpgradeTreeNodeDef node)
        {
            UnlockResult baseResult = ValidateUnlock(node);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            return ValidateTypeSpecificRules(node);
        }

        protected abstract UnlockResult ValidateTypeSpecificRules(UpgradeTreeNodeDef node);
        public abstract void OnPathSelected(UpgradePathDef path);
        protected virtual bool ValidateUpgradePath(UpgradeTreeNodeDef node)
        {
            // Branch nodes can be used for path selection if their path is available
            if (node.type == NodeType.Branch)
                return node.path != null && CanSelectPath(node.path);

            // Non-branch nodes require their path to be selected already
            if (node.path != null)
                return IsPathSelected(node.path);

            return true;
        }

        public virtual void ExposeData()
        {
            // Save/load basic references
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref gene, "gene");
            Scribe_Defs.Look(ref treeDef, "treeDef");

            Scribe_Collections.Look(ref unlockedUpgrades, "unlockedUpgrades", LookMode.Def);
            Scribe_Collections.Look(ref unlockedNodes, "unlockedNodes", LookMode.Def);
            Scribe_Collections.Look(ref selectedPaths, "selectedPaths", LookMode.Def);

            Scribe_Collections.Look(
                ref activeEffects,
                "activeEffects",
                LookMode.Def,
                LookMode.Deep
            );


            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (unlockedUpgrades != null) unlockedUpgrades = new HashSet<UpgradeDef>(unlockedUpgrades);
                if (unlockedNodes != null) unlockedNodes = new HashSet<UpgradeTreeNodeDef>(unlockedNodes);
                if (selectedPaths != null)
                {
                    // Store the paths first
                    selectedPaths = new HashSet<UpgradePathDef>(selectedPaths);
                    // Then reselect each path to trigger their effects
                    foreach (var path in selectedPaths)
                    {
                        OnPathSelected(path);
                    }
                }
            }
        }
    }
}
