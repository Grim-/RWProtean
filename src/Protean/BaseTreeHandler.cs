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

            // Check if the path is already selected
            if (selectedPaths.Contains(path)) 
                return false;

            return !path.IsPathExclusiveWith(selectedPaths);
        }

        public virtual bool HasSelectedAPath()
        {
            return selectedPaths != null && selectedPaths.Count > 0;
        }

        public virtual UnlockResult SelectPath(UpgradePathDef path)
        {
            if (!CanSelectPath(path))
            {
                return UnlockResult.Failed(UpgradeUnlockError.ExclusivePath, $"Cannot select this path defName={path.defName}");
            }

            selectedPaths.Add(path);
            OnPathSelected(path);
            return UnlockResult.Succeeded();
        }

        protected virtual UnlockResult ValidateUnlock(UpgradeTreeNodeDef node)
        {
            if (node == null)
            {
                return UnlockResult.Failed(UpgradeUnlockError.InvalidNode, "Invalid upgrade node");
            }

            if (IsNodeUnlocked(node))
            {
                return UnlockResult.Failed(UpgradeUnlockError.AlreadyUnlocked, "This upgrade is already unlocked");
            }

            if (node.type == UpgradeTreeNodeDef.NodeType.Start)
            {
                return UnlockResult.Succeeded();
            }

            if (node.type != UpgradeTreeNodeDef.NodeType.Start)
            {
                bool hasUnlockedPredecessor = node.GetPredecessors(treeDef).Any(IsNodeUnlocked);
                if (!hasUnlockedPredecessor)
                {
                    return UnlockResult.Failed(UpgradeUnlockError.NoPrecedingNode, "Requires a previous upgrade to be unlocked first");
                }
            }

            if (!ValidateUpgradePath(node.path))
            {
                return UnlockResult.Failed(UpgradeUnlockError.ExclusivePath,
                    "Cannot unlock - This node belongs to a path you haven't selected");
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
                    if (item.type == UpgradeTreeNodeDef.NodeType.Start && !IsNodeUnlocked(item))
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
        public abstract bool ValidateUpgradePath(UpgradePathDef path);

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
