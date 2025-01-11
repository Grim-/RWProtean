using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Protean
{
    public class PawnOverlayNodeWorker : PawnRenderNodeWorker
    {
        private float animationProgress = 0f;
        private const float AnimationSpeed = 1f / 3000f;

        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            PawnOverlayNode overlayNode = node as PawnOverlayNode;
            if (overlayNode == null || overlayNode.Graphic == null) return;

            Mesh mesh = node.GetMesh(parms);
            if (mesh == null) return;

            Material material = overlayNode.GraphicFor(parms.pawn).MatAt(parms.facing);
            if (material == null) return;


            Vector3 drawLoc;
            Vector3 pivot;
            Quaternion quat;
            Vector3 scale;

            Vector3 offset = this.OffsetFor(node, parms, out pivot);
            node.GetTransform(parms, out drawLoc, out _, out quat, out scale);
            drawLoc += offset;

            if (overlayNode.Props.graphicData != null)
            {
                scale = new Vector3(overlayNode.Props.graphicData.drawSize.x, 1f, overlayNode.Props.graphicData.drawSize.y);
            }

            PawnGraphicDrawRequest request = new PawnGraphicDrawRequest(node, mesh, material);
            request.preDrawnComputedMatrix = Matrix4x4.TRS(drawLoc, quat, scale);
            requests.Add(request);
        }
        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            var matPropBlock = base.GetMaterialPropertyBlock(node, material, parms);
            if (matPropBlock == null) return null;

            var overlayNode = node as PawnOverlayNode;
            if (overlayNode == null) return matPropBlock;

            Gene_Parasite parasite = parms.pawn.genes.GetFirstGeneOfType<Gene_Parasite>();
            Color color = (parasite != null && parasite.SuitColor != default(Color))
                ? parasite.SuitColor
                : overlayNode.Props.overlayColor;

            color.a = overlayNode.Props.overlayAlpha;
            matPropBlock.SetColor(ShaderPropertyIDs.Color, parms.tint * color);

            return matPropBlock;
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 baseOffset = base.OffsetFor(node, parms, out pivot);

            if (node is PawnOverlayNode overlayNode)
            {
                return baseOffset + overlayNode.Props.offset;
            }

            return baseOffset;
        }
        protected override Vector3 PivotFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 basePivot = base.PivotFor(node, parms);
            if (node is PawnOverlayNode overlayNode)
            {
                Vector3 customPivotAdjustment = Vector3.zero;
                return basePivot + customPivotAdjustment;
            }

            return basePivot;
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            //if (node is PawnOverlayNode overlayNode && overlayNode.Props.graphicData != null)
            //{
            //    Vector2 baseSize = overlayNode.Props.graphicData.drawSize;

            //    float pulseAmount = 0.05f;
            //    float pulseSpeed = 1f;
            //    float scaleFactor = 1f + (Mathf.Sin(Time.realtimeSinceStartup * pulseSpeed) * pulseAmount);

            //    return new Vector3(
            //        baseSize.x * scaleFactor,
            //        1f, 
            //        baseSize.y * scaleFactor
            //    );
            //}
            return base.ScaleFor(node, parms);
        }

        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            if (node is PawnOverlayNode overlayNode)
            {
                float baseLayer = base.LayerFor(node, parms);

                return baseLayer + overlayNode.Props.layerOffset;
            }
            return base.LayerFor(node, parms);
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.RotationFor(node, parms);
        }
    }

    public class HediffCompProperties_SuitToggle : HediffCompProperties
    {
        public HediffCompProperties_SuitToggle()
        {
            compClass = typeof(HediffComp_SuitToggle);
        }
    }

    public class HediffComp_SuitToggle : HediffComp
    {

    }
}
