using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Protean
{
    public class Gene_Parasite : Gene
    {
        private Dictionary<UpgradeDef, List<UpgradeEffect>> activeEffects =
                    new Dictionary<UpgradeDef, List<UpgradeEffect>>();
        private HashSet<UpgradeDef> unlockedUpgrades = new HashSet<UpgradeDef>();
        private HashSet<UpgradePathDef> chosenPaths = new HashSet<UpgradePathDef>();

        private PassiveTreeHandler passiveTree;
        private ActiveTreeHandler activeTree;

        private const int MaxParasiteLevel = 300;
        private const int MaxBondLevel = 20;
        private const float BaseBond = 100f;
        private const float BondingPerHour = 0.1f;

        private int parasiteLevel = 1;
        private int bondLevel = 0;
        private float currentLevelBond = 0f;
        private int evolutionPoints = 0;

        // Properties for external access
        public int ParasiteLevel => parasiteLevel;
        public int BondLevel => bondLevel;
        public float CurrentBondProgress => currentLevelBond / MaxBondPerLevel(bondLevel);
        public int EvolutionPoints => evolutionPoints;

        private float MaxBondPerLevel(int level) => (level + 1) * BaseBond;
        public override void PostMake()
        {
            base.PostMake();
            passiveTree = new PassiveTreeHandler(pawn, ProteanDefOf.BasicParasiteTree);
            activeTree = new ActiveTreeHandler(pawn, ProteanDefOf.BasicParasiteTree);
        }
        public void OpenPassiveTree()
        {
            var window = new ParasiteTreeUI(this, ProteanDefOf.BasicParasiteTree, passiveTree);
            Find.WindowStack.Add(window);
        }

        public void OpenActiveTree()
        {
            var window = new ParasiteTreeUI(this, ProteanDefOf.BasicParasiteTree, activeTree);
            Find.WindowStack.Add(window);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            passiveTree?.ExposeData();
            activeTree?.ExposeData();

            Scribe_Collections.Look(ref unlockedUpgrades, "unlockedUpgrades", LookMode.Def);
            Scribe_Values.Look(ref parasiteLevel, "parasiteLevel", 1);
            Scribe_Values.Look(ref bondLevel, "bondLevel", 0);
            Scribe_Values.Look(ref currentLevelBond, "currentLevelBond", 0f);
            Scribe_Values.Look(ref evolutionPoints, "evolutionPoints", 0);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                activeEffects.Clear();
                foreach (var upgrade in unlockedUpgrades)
                {
                    UnlockUpgrade(upgrade);
                }
            }
        }

        public override void Tick()
        {
            base.Tick();

            // Increase bond every hour (6000 ticks)
            if (pawn.IsHashIntervalTick(6000))
            {
                IncreaseBond(BondingPerHour);
            }
        }

        private void IncreaseBond(float amount)
        {
            if (bondLevel >= MaxBondLevel) return;

            currentLevelBond += amount;
            float maxBond = MaxBondPerLevel(bondLevel);

            while (currentLevelBond >= maxBond && bondLevel < MaxBondLevel)
            {
                currentLevelBond -= maxBond;
                GainBondLevel(1);
                maxBond = MaxBondPerLevel(bondLevel);
            }
        }

        private void GainBondLevel(int levels)
        {
            int oldLevel = bondLevel;
            bondLevel = Math.Min(bondLevel + levels, MaxBondLevel);

            // Update passive tree and grant points for active tree
            passiveTree.OnLevelUp(bondLevel);
            activeTree.AddPoints(levels); // Each bond level grants 1 point
        }

        private void OnBondLevelGained(int newLevel)
        {
            evolutionPoints++;
        }

        public bool CanAffordUpgrade(UpgradeDef upgrade)
        {
            return HasEvolutionPoints(upgrade.pointCost);
        }

        private bool HasEvolutionPoints(int amount)
        {
            return evolutionPoints >= amount;
        }

        public void UnlockUpgrade(UpgradeDef upgrade)
        {
            if (unlockedUpgrades.Contains(upgrade) || !CanAffordUpgrade(upgrade))
                return;

            UseEvolutionPoints(upgrade.pointCost);

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

            Messages.Message($"{pawn.Label} unlocked {upgrade.defName}", MessageTypeDefOf.PositiveEvent);
        }

        private void UseEvolutionPoints(int amount)
        {
            if (HasEvolutionPoints(amount))
            {
                evolutionPoints -= amount;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            //foreach (var item in base.GetGizmos())
            //{
            //    yield return item;
            //}

            yield return new Command_Action
            {
                defaultLabel = "DEV: Increase Bond Level",
                defaultDesc = "Increase Bond Level by 1 (Debug)",
                action = () => GainBondLevel(1),
                
            };

            yield return new Command_Action
            {
                defaultLabel = "DEV: Increase Bond Progress",
                defaultDesc = "Increase Bond Progress to 100%",
                action = () => IncreaseBond(MaxBondPerLevel(BondLevel) - CurrentBondProgress),

            };
        }


        #region Upgrade Tree Node Methods
        //public void UnlockUpgrade(UpgradeDef upgrade)
        //{
        //    if (!activeEffects.ContainsKey(upgrade))
        //    {
        //        activeEffects[upgrade] = new List<UpgradeEffect>();
        //        foreach (var effectDef in upgrade.effects)
        //        {
        //            var effect = effectDef.CreateEffect();
        //            effect.Apply(pawn);
        //            activeEffects[upgrade].Add(effect);
        //        }
        //    }
        //    unlockedUpgrades.Add(upgrade);
        //}

        public bool CanUnlockNode(UpgradeTreeNodeDef node)
        {
            if (node.type == UpgradeTreeNodeDef.NodeType.Start) return true;
            if (node.connections == null || node.connections.Count <= 0) return true;
            if (IsNodeUnlocked(node)) return false;

            if (node.path != null)
            {
                if (chosenPaths.Any(p =>
                    p.exclusiveWith != null &&
                    p.exclusiveWith.Contains(node.path)))
                {
                    return false;
                }
            }

            bool hasUnlockedConnection = node.connections.Any(connection =>
                IsNodeUnlocked(connection));
            bool meetsLevelRequirement = true;
            if (node.upgrade != null)
            {
                meetsLevelRequirement = ParasiteLevel >= node.upgrade.parasiteLevelRequired;
            }

            return hasUnlockedConnection && meetsLevelRequirement;
        }

        public bool IsNodeUnlocked(UpgradeTreeNodeDef node)
        {
            return node.upgrade == null || unlockedUpgrades.Contains(node.upgrade);
        }

        #endregion
    }
}
