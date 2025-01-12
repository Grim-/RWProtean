using System.Collections.Generic;
using System.Linq;
using Talented;
using Verse;

namespace Protean
{
    public class ParasiteGeneDef : TalentedGeneDef
    {
        public List<StrainDef> PossibleStrains;

        public StrainDef SelectRandomStrain()
        {
            var strains = PossibleStrains;
            if (strains.NullOrEmpty())
            {
                Log.Error($"has no possible strains defined");
                return null;
            }

            float totalWeight = strains.Sum(s => s.rarity);
            float random = Rand.Range(0f, totalWeight);
            float currentSum = 0f;

            foreach (var strain in strains)
            {
                currentSum += strain.rarity;
                if (random <= currentSum)
                {
                    return strain;
                }
            }

            // Fallback in case of floating point imprecision
            return strains.Last();
        }
    }
}
