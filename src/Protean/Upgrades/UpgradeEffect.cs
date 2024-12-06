using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Protean
{
    public abstract class UpgradeEffect : IExposable
    {
        public abstract void Apply(Pawn pawn);
        public abstract void Remove(Pawn pawn);
        public abstract void ExposeData();
    }
}
