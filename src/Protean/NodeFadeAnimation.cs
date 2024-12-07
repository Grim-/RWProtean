using UnityEngine;

namespace Protean
{
    public class NodeFadeAnimation : UIAnimationState
    {
        public UpgradeTreeNodeDef node;
        public Color startColor;
        public Color endColor;

        public override bool Finished => Progress >= 1f;

        public override void Animate()
        {
            Color currentColor = Color.Lerp(startColor, endColor, Progress);
            // Draw node with current color
        }
    }
}
