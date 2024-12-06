using System.Linq;
using Verse;

namespace Protean
{
    public class ActiveTreeHandler : BaseTreeHandler
    {
        private int availablePoints;

        public ActiveTreeHandler(Pawn pawn, Gene_Parasite gene, UpgradeTreeDef treeDef)
            : base(pawn, gene, treeDef)
        {
            this.availablePoints = 0;
        }

        public ActiveTreeHandler()
        {

        }

        protected override UnlockResult ValidateTypeSpecificRules(UpgradeTreeNodeDef node)
        {
            if (availablePoints < node.upgrade.pointCost)
            {
                return UnlockResult.Failed(UpgradeUnlockError.InsufficientPoints,
                    string.Format("Requires {0} points", node.upgrade.pointCost));
            }
            return UnlockResult.Succeeded();
        }

        public void AddPoints(int points)
        {
            this.availablePoints += points;
        }

        public UnlockResult TryUnlockNode(UpgradeTreeNodeDef node)
        {
            UnlockResult result = CanUnlockNode(node);
            if (!result.Success)
            {
                return result;
            }

            this.availablePoints -= node.upgrade.pointCost;
            UnlockNode(node);
            return UnlockResult.Succeeded();
        }

        public override void OnPathSelected(UpgradePathDef path)
        {
            // Active tree doesn't need special handling for path selection
        }

        public override bool ValidateUpgradePath(UpgradePathDef path)
        {
            bool isSelected = IsPathSelected(path);
            //Log.Message($"ActiveTreeHandler.ValidatePath: Path {path?.defName} selected = {isSelected}");
            return isSelected;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref availablePoints, "availablePoints", 0);
        }
    }
}
