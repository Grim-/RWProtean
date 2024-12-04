using Verse;

namespace Protean
{
    public class PassiveTreeHandler : BaseTreeHandler
    {
        private int currentLevel;

        public PassiveTreeHandler(Pawn pawn, UpgradeTreeDef treeDef) : base(pawn, treeDef)
        {
            currentLevel = 0;
        }

        public override bool CanUnlockNode(UpgradeTreeNodeDef node)
        {
            if (!base.CanUnlockNode(node)) return false;
            return node.upgrade == null || currentLevel >= node.upgrade.parasiteLevelRequired;
        }

        public void OnLevelUp(int newLevel)
        {
            currentLevel = newLevel;
            AutoUnlockAvailableNodes();
        }

        private void AutoUnlockAvailableNodes()
        {
            var allNodes = treeDef.GetAllNodes();
            foreach (var node in allNodes)
            {
                if (CanUnlockNode(node) && !IsNodeUnlocked(node))
                {
                    UnlockUpgrade(node.upgrade);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentLevel, "currentLevel", 0);
        }
    }
}
