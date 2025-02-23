﻿using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Protean
{
    public class PawnOverlayNodeWorker : PawnRenderNodeWorker
    {
        private float animationProgress = 0f;
        private const float AnimationSpeed = 1f / 3000f;
        protected int currentTicks = 0;
        protected int colorShiftTicks = 300;
        protected Color currentColor;
        protected Color targetColor;
        private float pulseAmount = 0.05f;
        private float pulseSpeed = 1f;

        private Gene_Parasite Parasite = null;


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
            if (matPropBlock == null) 
                return null;

            var overlayNode = node as PawnOverlayNode;
            if (overlayNode == null) 
                return matPropBlock;

            if (Parasite == null)
            {
                Parasite = parms.pawn.genes.GetFirstGeneOfType<Gene_Parasite>();
            }
        
            if (Parasite == null) 
                return matPropBlock;

            Color baseColor = (Parasite != null && Parasite.SuitColor != default(Color))
                ? Parasite.SuitColor
                : overlayNode.Props.overlayColor;

            Color eyeColor = (Parasite != null && Parasite.EyeColor != default(Color))
                ? Parasite.EyeColor
                : Color.white;


            StrainDef strainDef = Parasite.GetChosenStrain();

            currentTicks++;
            if (currentTicks >= colorShiftTicks)
            {
                currentTicks = 0;
                currentColor = targetColor;

                if (strainDef.possibleShiftColors != null && strainDef.possibleShiftColors.Count > 0)
                {
                    targetColor = strainDef.possibleShiftColors.RandomElement();
                }
                else
                {
                    targetColor = new Color(Rand.Range(0f, 1f), Rand.Range(0f, 1f), Rand.Range(0f, 1f));
                }            
            }

            Color lerpedColor = currentColor;

            if (strainDef.canColorShift)
            {
                float lerpAmount = (float)currentTicks / colorShiftTicks;
                lerpedColor = Color.Lerp(currentColor, targetColor, lerpAmount);
            }

            if (strainDef.canPulsate)
            {
                float pulseAmount = (Mathf.Sin(Find.TickManager.TicksGame / 60f) + 1f) * 0.2f;
                lerpedColor *= (1f + pulseAmount);
            }

            lerpedColor.a = overlayNode.Props.overlayAlpha;

            if (strainDef.lerpAlphaWithHealth)
            {
                float healthPercent = parms.pawn.health.summaryHealth.SummaryHealthPercent;
                lerpedColor.a *= Mathf.Lerp(strainDef.alphaHealthRange.min, strainDef.alphaHealthRange.max, healthPercent);
            }
     
            matPropBlock.SetColor(ShaderPropertyIDs.Color, parms.tint * lerpedColor);
            matPropBlock.SetColor(ShaderPropertyIDs.ColorTwo, parms.tint * eyeColor);

            return matPropBlock;
        }

        protected Color ColorShift(Color lerpedColor, PawnDrawParms parms)
        {
            float lerpAmount = (float)currentTicks / colorShiftTicks;
            lerpedColor = Color.Lerp(currentColor, targetColor, lerpAmount);
            return lerpedColor;
        }

        protected Color GetDirectionColorShift(Color lerpedColor, PawnDrawParms parms)
        {
            float directionMultiplier = parms.facing == Rot4.North ? 1.2f : 1f;
            lerpedColor *= directionMultiplier;
            return lerpedColor;
        }

        protected Color GetHealthColor(Color lerpedColor, PawnDrawParms parms)
        {
            float healthPercent = parms.pawn.health.summaryHealth.SummaryHealthPercent;
            lerpedColor *= Mathf.Lerp(0.5f, 1.2f, healthPercent);
            return lerpedColor;
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
            if (node is PawnOverlayNode overlayNode && overlayNode.Props.graphicData != null)
            {
                Vector2 baseSize = overlayNode.Props.graphicData.drawSize;


                float scaleFactor = 1.1f + (Mathf.Sin(Time.realtimeSinceStartup * pulseSpeed) * pulseAmount);

                return new Vector3(
                    baseSize.x * scaleFactor,
                    1f,
                    baseSize.y * scaleFactor
                );
            }
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
}
