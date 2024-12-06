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
        private HashSet<UpgradePathDef> selectedPassivePaths = new HashSet<UpgradePathDef>();
        private HashSet<UpgradePathDef> selectedActivePaths = new HashSet<UpgradePathDef>();

        public PassiveTreeHandler passiveTree;
        public ActiveTreeHandler activeTree;

        private const int MaxParasiteLevel = 300;
        private const int MaxBondLevel = 20;
        private const float BaseBond = 100f;
        private const float BondingPerHour = 1f;

        private int parasiteLevel = 1;
        private int bondLevel = 0;
        private float currentLevelBond = 0f;
        private int evolutionPoints = 0;

        public int ParasiteLevel => parasiteLevel;
        public int BondLevel => bondLevel;
        public float CurrentBondProgress => currentLevelBond / MaxBondPerLevel(bondLevel);
        public int EvolutionPoints => evolutionPoints;

        private float MaxBondPerLevel(int level) => (level + 1) * BaseBond;
        public override void PostMake()
        {
            base.PostMake();
            passiveTree = new PassiveTreeHandler(pawn, this, ProteanDefOf.Passive_BasicParasiteTree);
            activeTree = new ActiveTreeHandler(pawn, this, ProteanDefOf.Active_BasicParasiteTree);
        }

        public override void Tick()
        {
            base.Tick();

            if (pawn.IsHashIntervalTick(6000))
            {
                IncreaseBond(BondingPerHour);
            }
        }
        public void OpenPassiveTree()
        {
            var window = new ParasiteTreeUI(this, ProteanDefOf.Passive_BasicParasiteTree, passiveTree, ProteanDefOf.Passive_BasicParasiteTree.displayStrategy);
            Find.WindowStack.Add(window);
        }

        public void OpenActiveTree()
        {
            var window = new ParasiteTreeUI(this, ProteanDefOf.Active_BasicParasiteTree, activeTree, ProteanDefOf.Active_BasicParasiteTree.displayStrategy);
            Find.WindowStack.Add(window);
        }

        private void IncreaseBond(float amount)
        {
            if (bondLevel >= MaxBondLevel) return;
            currentLevelBond += amount;
            float maxBond = MaxBondPerLevel(bondLevel);

            int levelsToGain = 0;
            float remainingBond = currentLevelBond;
            int tempLevel = bondLevel;

            while (remainingBond >= maxBond && tempLevel < MaxBondLevel)
            {
                remainingBond -= maxBond;
                tempLevel++;
                levelsToGain++;
                maxBond = MaxBondPerLevel(tempLevel);
            }

            if (levelsToGain > 0)
            {
                currentLevelBond = remainingBond;
                GainBondLevel(levelsToGain);
            }
        }

        private void GainBondLevel(int levels)
        {
            int oldLevel = bondLevel;
            bondLevel = Math.Min(bondLevel + levels, MaxBondLevel);

            if (passiveTree != null)
            {
                passiveTree.OnLevelUp(bondLevel);
            }
            if (activeTree != null)
            {
                activeTree.AddPoints(levels);
            }

            Messages.Message($"{pawn.Label} gained {levels} bond level{(levels > 1 ? "s" : "")} with their parasite (level = {bondLevel})", MessageTypeDefOf.PositiveEvent);
        }

        public bool CanAffordUpgrade(UpgradeDef upgrade)
        {
            return HasEvolutionPoints(upgrade.pointCost);
        }

        private bool HasEvolutionPoints(int amount)
        {
            return evolutionPoints >= amount;
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
                action = () => IncreaseBond(MaxBondPerLevel(BondLevel) - CurrentBondProgress - 0.1f),

            };
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref passiveTree, "passiveTree");
            Scribe_Deep.Look(ref activeTree, "activeTree");

            Scribe_Collections.Look(ref unlockedUpgrades, "unlockedUpgrades", LookMode.Def);
            Scribe_Values.Look(ref parasiteLevel, "parasiteLevel", 1);
            Scribe_Values.Look(ref bondLevel, "bondLevel", 0);
            Scribe_Values.Look(ref currentLevelBond, "currentLevelBond", 0f);
            Scribe_Values.Look(ref evolutionPoints, "evolutionPoints", 0);
            Scribe_Collections.Look(ref selectedPassivePaths, "selectedPassivePaths", LookMode.Def);
            Scribe_Collections.Look(ref selectedActivePaths, "selectedActivePaths", LookMode.Def);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                activeEffects.Clear();
            }
        }
    }
}
