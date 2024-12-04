using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Protean
{
    public abstract class BaseTreeHandler
    {
        protected Dictionary<UpgradeDef, List<UpgradeEffect>> activeEffects =
            new Dictionary<UpgradeDef, List<UpgradeEffect>>();
        protected HashSet<UpgradeDef> unlockedUpgrades = new HashSet<UpgradeDef>();
        protected HashSet<UpgradePathDef> chosenPaths = new HashSet<UpgradePathDef>();

        protected Pawn pawn;
        protected UpgradeTreeDef treeDef;

        public BaseTreeHandler(Pawn pawn, UpgradeTreeDef treeDef)
        {
            this.pawn = pawn;
            this.treeDef = treeDef;
        }

        public virtual bool CanUnlockNode(UpgradeTreeNodeDef node)
        {
            if (node.type == UpgradeTreeNodeDef.NodeType.Start) return true;
            if (node.connections == null || node.connections.Count <= 0) return true;
            if (IsNodeUnlocked(node)) return false;

            if (node.path != null && chosenPaths.Any(p =>
                p.exclusiveWith != null &&
                p.exclusiveWith.Contains(node.path)))
            {
                return false;
            }

            return node.connections.Any(connection => IsNodeUnlocked(connection));
        }

        public virtual bool IsNodeUnlocked(UpgradeTreeNodeDef node)
        {
            return node.upgrade == null || unlockedUpgrades.Contains(node.upgrade);
        }

        protected virtual void UnlockUpgrade(UpgradeDef upgrade)
        {
            if (!activeEffects.ContainsKey(upgrade))
            {
                activeEffects[upgrade] = new List<UpgradeEffect>();
                foreach (var effectDef in upgrade.effects)
                {
                    var effect = effectDef.CreateEffect();
                    effect.Apply(pawn);
                    activeEffects[upgrade].Add(effect);
                }
            }
            unlockedUpgrades.Add(upgrade);
        }

        public virtual void ExposeData()
        {
            Scribe_Defs.Look(ref treeDef, "treeDef");
            Scribe_Collections.Look(ref unlockedUpgrades, "unlockedUpgrades", LookMode.Def);
            Scribe_Collections.Look(ref chosenPaths, "chosenPaths", LookMode.Def);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                activeEffects.Clear();
                foreach (var upgrade in unlockedUpgrades)
                {
                    UnlockUpgrade(upgrade);
                }
            }
        }
    }
}
