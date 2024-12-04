using Verse;

namespace Protean
{
    public class ActiveTreeHandler : BaseTreeHandler
    {
        private int availablePoints;

        public ActiveTreeHandler(Pawn pawn, UpgradeTreeDef treeDef) : base(pawn, treeDef)
        {
            availablePoints = 0;
        }

        public override bool CanUnlockNode(UpgradeTreeNodeDef node)
        {
            if (!base.CanUnlockNode(node)) return false;
            return node.upgrade == null || availablePoints >= node.upgrade.pointCost;
        }

        public void AddPoints(int points)
        {
            availablePoints += points;
        }

        public bool TryUnlockNode(UpgradeTreeNodeDef node)
        {
            if (!CanUnlockNode(node)) return false;

            availablePoints -= node.upgrade.pointCost;
            UnlockUpgrade(node.upgrade);
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref availablePoints, "availablePoints", 0);
        }
    }
}
