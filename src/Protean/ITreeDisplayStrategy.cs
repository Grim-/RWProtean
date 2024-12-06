using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Protean
{
    public interface ITreeDisplayStrategy
    {
        Dictionary<UpgradeTreeNodeDef, Rect> PositionNodes(List<UpgradeTreeNodeDef> nodes, Rect availableSpace, float nodeSize, float spacing);
    }


    public class TreeDisplayStrategyDef : Def
    {
        public Type strategyClass;
    }
}
