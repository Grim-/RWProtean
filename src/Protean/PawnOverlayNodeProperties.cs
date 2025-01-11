using UnityEngine;
using Verse;

namespace Protean
{
    public class PawnOverlayNodeProperties : PawnRenderNodeProperties
    {
        public Color overlayColor = Color.white;
        public float overlayAlpha = 1f;
        public Vector3 offset = Vector3.zero;
        public float layerOffset = 0.1f;
        public Vector3 eastOffset = new Vector3(-1, 0, 0);
        public Vector3 westOffset = new Vector3(1, 0, 0);
        public GraphicData graphicData;
        public bool useBodyTypeVariants = false;

        public PawnOverlayNodeProperties()
        {
            this.nodeClass = typeof(PawnOverlayNode);
            this.workerClass = typeof(PawnOverlayNodeWorker);
        }
    }
}
