using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Protean
{
    public class Gene_Parasite : Gene
    {
        private PassiveTreeHandler passiveTree;
        private ActiveTreeHandler activeTree;

        private const int MaxParasiteLevel = 300;
        private const int MaxBondLevel = 20;
        private const float BaseBond = 100f;
        private const float BondingPerHour = 1f;

        private float currentBond = 0f;


        private int currentLevel = 1;
        public int CurrentLevel => currentLevel;

        private int bondLevel = 0;
        public int BondLevel => bondLevel;
        public float CurrentBondProgress => currentBond / MaxBondPerLevel(bondLevel);


        private int talentPoints = 0;
        public int TalentPointsAvailable => talentPoints;

        private float MaxBondPerLevel(int level) => (level + 1) * BaseBond;
        public override void PostMake()
        {
            base.PostMake();
            passiveTree = new PassiveTreeHandler(pawn, this, ProteanDefOf.Passive_BasicParasiteTree);
            activeTree = new ActiveTreeHandler(pawn, this, ProteanDefOf.Active_BasicParasiteTree);

            EventManager.OnDamageDealt += EventManager_OnDamageDealt;
            EventManager.OnDamageTaken += EventManager_OnDamageTaken;

            SetSuitColor(Color.red);
        }

        private void EventManager_OnDamageTaken(Pawn arg1, DamageInfo arg2)
        {
            Log.Message($"{arg1.Label} took {arg2.Amount} damage");
        }


        private Color _SuitColor;
        public Color SuitColor => _SuitColor;

        public void SetSuitColor(Color newColor)
        {
            _SuitColor = newColor;
        }

        public override void PostRemove()
        {
            EventManager.OnDamageTaken -= EventManager_OnDamageTaken;
            EventManager.OnDamageDealt -= EventManager_OnDamageDealt;
            base.PostRemove();
        }

        private DamageWorker.DamageResult EventManager_OnDamageDealt(Thing victim, Thing target, DamageInfo arg2, DamageWorker.DamageResult damageResult)
        {
            damageResult.totalDamageDealt += 1000f;


            Log.Message($"{target.Label} dealt {arg2.Amount} damage to {victim.Label}");
            return damageResult;
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

        public void IncreaseBond(float amount)
        {
            if (bondLevel >= MaxBondLevel) return;
            currentBond += amount;
            float maxBond = MaxBondPerLevel(bondLevel);

            int levelsToGain = 0;
            float remainingBond = currentBond;
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
                currentBond = remainingBond;
                GainBondLevel(levelsToGain);
            }
        }

        public void GainBondLevel(int levels)
        {
            int oldLevel = bondLevel;
            int gainedLevels = levels;

            int newLevel = oldLevel + gainedLevels;
            bondLevel = Math.Min(newLevel, MaxBondLevel);

            passiveTree?.OnLevelUp(bondLevel);
            activeTree?.OnLevelUp(levels);
            Messages.Message($"{pawn.Label} gained {levels} bond level{(levels > 1 ? "s" : "")} with their parasite (level = {bondLevel})", MessageTypeDefOf.PositiveEvent);
        }

        public bool CanAffordUpgrade(UpgradeDef upgrade)
        {
            return HasTalentPointsAvailable(upgrade.pointCost);
        }

        public bool HasTalentPointsAvailable(int amount)
        {
            return talentPoints >= amount;
        }

        public void UseTalentPoints(int amount)
        {
            if (HasTalentPointsAvailable(amount))
            {
                talentPoints -= amount;
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

            Scribe_Values.Look(ref currentLevel, "currentLevel", 1);
            Scribe_Values.Look(ref bondLevel, "bondLevel", 0);
            Scribe_Values.Look(ref currentBond, "currentBond", 0f);
            Scribe_Values.Look(ref talentPoints, "talentPoints", 0);
        }
    }
}
