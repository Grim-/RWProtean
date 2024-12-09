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
            TryUnlockNextUpgrade(this.treeDef.nodes[0], true);
        }

        public ActiveTreeHandler()
        {
        }

        private void UnlockBasicNode()
        {
            var basicNode = treeDef.GetAllNodes().FirstOrDefault(n => n.type != NodeType.Start);
            if (basicNode != null)
            {
                TryUnlockNextUpgrade(basicNode);
            }
        }

        protected override UnlockResult ValidateTypeSpecificRules(UpgradeTreeNodeDef node)
        {
            if (node.type != NodeType.Start && !HasSelectedAPath())
            {
                return UnlockResult.Failed(UpgradeUnlockError.ExclusivePath,"Must select a path first");
            }

            int currentProgress = GetNodeProgress(node);
            if (currentProgress >= node.upgrades.Count)
                return UnlockResult.Failed(UpgradeUnlockError.AlreadyUnlocked, "Already unlocked all upgrades in this node");

            if (availablePoints < node.upgrades[currentProgress].pointCost)
            {
                return UnlockResult.Failed(UpgradeUnlockError.InsufficientPoints,
                    string.Format("Requires {0} points", node.upgrades[currentProgress].pointCost));
            }

            return UnlockResult.Succeeded();
        }

        public void AddPoints(int points)
        {
            this.availablePoints += points;
        }

        public int GetAvailablePoints()
        {
            return availablePoints;
        }

        public UnlockResult TryUnlockNode(UpgradeTreeNodeDef node)
        {
            UnlockResult result = CanUnlockNode(node);
            if (!result.Success)
            {
                return result;
            }

            int currentProgress = GetNodeProgress(node);
            if (currentProgress >= node.upgrades.Count)
            {
                return UnlockResult.Failed(UpgradeUnlockError.AlreadyUnlocked, "Node is already fully unlocked");
            }

            int cost = node.upgrades[currentProgress].pointCost;
            if (availablePoints < cost)
            {
                return UnlockResult.Failed(UpgradeUnlockError.InsufficientPoints, $"Requires {cost} points");
            }

            this.availablePoints -= cost;
            UnlockResult unlockResult = TryUnlockNextUpgrade(node);

            if (!unlockResult.Success)
            {
                // Refund points if unlock failed
                this.availablePoints += cost;
            }

            return unlockResult;
        }

        public bool HasSufficientPointsForNode(UpgradeTreeNodeDef node)
        {
            if (node == null || node.upgrades.NullOrEmpty()) return false;
            if (node.type != NodeType.Start && !HasSelectedAPath()) return false;

            int currentProgress = GetNodeProgress(node);
            if (currentProgress >= node.upgrades.Count) return false;

            return availablePoints >= node.upgrades[currentProgress].pointCost;
        }

        public override void OnPathSelected(UpgradePathDef path)
        {

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref availablePoints, "availablePoints", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (unlockedNodes.Count == 0)
                {
                    UnlockStartNode();
                    UnlockBasicNode();
                }
            }
        }

        public int GetNextUpgradeCost(UpgradeTreeNodeDef node)
        {
            int currentProgress = GetNodeProgress(node);
            if (currentProgress >= node.upgrades.Count)
                return 0;

            return node.upgrades[currentProgress].pointCost;
        }

        public int GetTotalPointsSpent()
        {
            int total = 0;
            foreach (var node in treeDef.GetAllNodes())
            {
                int progress = GetNodeProgress(node);
                for (int i = 0; i < progress && i < node.upgrades.Count; i++)
                {
                    total += node.upgrades[i].pointCost;
                }
            }
            return total;
        }

        public bool HasAvailableUpgrades()
        {
            return treeDef.GetAllNodes().Any(node =>
                CanUnlockNode(node).Success &&
                HasSufficientPointsForNode(node));
        }
    }
}
