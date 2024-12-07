using UnityEngine;
using Verse;

namespace Protean
{
    public class UpgradeTreeSkinDef : Def
    {
        // Background configuration
        public string backgroundTexturePath;
        public Color? backgroundColor;

        // Node appearance
        public string defaultNodeTexturePath = "UI/TreePassiveBorder";
        public float nodeSize = 45f;
        public float nodeSpacing = 15f;

        // Connection appearance
        public float connectionThickness = 1.5f;
        public bool showConnectionArrows = true;
        public float arrowSize = 10f;

        // Color scheme
        public Color unlockedNodeColor = new Color(0.2f, 0.8f, 0.2f);
        public Color availableNodeColor = Color.grey;
        public Color lockedNodeColor = Color.red;
        public Color pathSelectedColor = Color.yellow;
        public Color pathExcludedColor = Color.red;
        public Color pathAvailableColor = Color.white;

        // Connection colors
        public Color unlockedConnectionColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        public Color activeConnectionColor = new Color(0.8f, 0.8f, 0.2f, 0.6f);
        public Color inactiveConnectionColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        // Text configuration
        public float labelOffset = 5f;
        public float pathLabelOffset = 25f;
        public GameFont labelFont = GameFont.Small;
        public Color labelColor = Color.white;

        // Toolbar configuration
        public float toolbarHeight = 35f;

        private Texture2D cachedBackgroundTexture;
        private Texture2D cachedNodeTexture;

        public Texture2D BackgroundTexture
        {
            get
            {
                if (cachedBackgroundTexture == null && !backgroundTexturePath.NullOrEmpty())
                {
                    cachedBackgroundTexture = ContentFinder<Texture2D>.Get(backgroundTexturePath);
                }
                return cachedBackgroundTexture;
            }
        }

        public Texture2D NodeTexture
        {
            get
            {
                if (cachedNodeTexture == null)
                {
                    cachedNodeTexture = ContentFinder<Texture2D>.Get(defaultNodeTexturePath);
                }
                return cachedNodeTexture;
            }
        }
    }
}
