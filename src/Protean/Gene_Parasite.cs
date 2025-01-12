using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Talented;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Protean
{
    public class Gene_Parasite : Gene_TalentBase
    {
        private const int MaxParasiteLevel = 300;
        private const int MaxBondLevel = 20;
        private const float BaseBond = 100f;
        private const float BondingPerHour = 1f;

        private float currentBond = 0f;



        private int bondLevel = 0;
        public int BondLevel => bondLevel;
        public float CurrentBondProgress => currentBond / MaxBondPerLevel(bondLevel);


        private Color _SuitColor;
        public Color SuitColor => _SuitColor;
        private float MaxBondPerLevel(int level) => (level + 1) * BaseBond;

        public override void PostMake()
        {
            base.PostMake();
            SetSuitColor(new Color(Rand.Range(0,1f), Rand.Range(0, 1f), Rand.Range(0,1f), 1f ));
        }

        public override void PostRemove()
        {
            base.PostRemove();
        }


        public void SetSuitColor(Color newColor)
        {
            _SuitColor = newColor;
        }

        public override void Tick()
        {
            base.Tick();

            if (pawn.IsHashIntervalTick(6000))
            {
                IncreaseBond(BondingPerHour);
            }
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

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var item in base.GetGizmos())
            {
                yield return item;
            }

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

            Scribe_Values.Look(ref _SuitColor, "suitColor");
        }

        protected override void InitializeTrees()
        {
            passiveTree = new PassiveTreeHandler(pawn, this, TalentedGeneDef.SecondaryTreeDef);
            activeTree = new ActiveTreeHandler(pawn, this, TalentedGeneDef.MainTreeDef);
        }

        public override void OnExperienceGained(float amount, string source)
        {
          
        }

        public override void OnLevelGained(int levels)
        {
           
        }
    }
}
