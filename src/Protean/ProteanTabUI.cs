﻿using RimWorld;
using UnityEngine;
using Verse;

namespace Protean
{
    public class ProteanTabUI : ITab
    {
        private Gene_Parasite Parasite => (Gene_Parasite)SelPawn.genes.GetGene(ProteanDefOf.Protean_BasicGene);
        public override bool IsVisible => SelPawn?.genes?.GetGene(ProteanDefOf.Protean_BasicGene) != null;
        const float padding = 5f;
        const float buttonWidth = 100f;
        const float levelWidth = 80f;
        const float progressWidth = 100f;

        public ProteanTabUI()
        {
            labelKey = "Parasite";
            size = new Vector2(500, 400);
        }

        protected override void FillTab()
        {
            var rect = new Rect(0, 0, 500, 600);
            var toolbarHeight = 35f;
            var toolbarRect = rect.TopPartPixels(toolbarHeight);

            // Draw toolbar background
            GUI.DrawTexture(toolbarRect, SolidColorMaterials.NewSolidColorTexture(Color.grey));

            DrawToolbar(toolbarRect);
        }

        private void DrawToolbar(Rect rect)
        {


            float curX = padding;

            // Bond Level with bg
            var bondLabelRect = new Rect(curX, rect.y + padding, levelWidth, rect.height - padding * 2);
            Widgets.DrawHighlight(bondLabelRect);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(bondLabelRect, $"Bond: {Parasite.BondLevel}");
            curX += levelWidth + padding;

            // Parasite Level with bg  
            var parasiteLabelRect = new Rect(curX, rect.y + padding, levelWidth, rect.height - padding * 2);
            Widgets.DrawHighlight(parasiteLabelRect);
            Widgets.Label(parasiteLabelRect, $"Level: {Parasite.ParasiteLevel}");
            curX += levelWidth + padding;

            Text.Anchor = TextAnchor.UpperLeft;

            // Bond Progress
            var progressRect = new Rect(curX, rect.y + (rect.height - 18f) / 2f, progressWidth, 18f);
            Widgets.FillableBar(progressRect, Parasite.CurrentBondProgress);
            curX += progressWidth + padding;

            // Tree Buttons
            var passiveTreeRect = new Rect(rect.width - (buttonWidth * 2 + padding * 2), rect.y + padding, buttonWidth, rect.height - padding * 2);
            if (Widgets.ButtonText(passiveTreeRect, "Passive Tree"))
            {
                Parasite.OpenPassiveTree();
            }

            var activeTreeRect = new Rect(rect.width - (buttonWidth + padding), rect.y + padding, buttonWidth, rect.height - padding * 2);
            if (Widgets.ButtonText(activeTreeRect, "Active Tree"))
            {
                Parasite.OpenActiveTree();
            }
        }
    }
}
