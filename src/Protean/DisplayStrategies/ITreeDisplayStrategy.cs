using System.Collections.Generic;
using UnityEngine;

namespace Protean
{
    public interface ITreeDisplayStrategy
    {
        Dictionary<UpgradeTreeNodeDef, Rect> PositionNodes(List<UpgradeTreeNodeDef> nodes, Rect availableSpace, float nodeSize, float spacing);
        void DrawToolBar(Rect toolbarRect);
    }
}
