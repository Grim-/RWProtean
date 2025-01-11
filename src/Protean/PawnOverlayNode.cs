using UnityEngine;
using Verse;

namespace Protean
{
    public class PawnOverlayNode : PawnRenderNode
    {
        public new PawnOverlayNodeProperties Props => (PawnOverlayNodeProperties)props;

        public PawnOverlayNode(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            string texPath = Props.useBodyTypeVariants ? GetBodyTypeTexPath(pawn) : Props.graphicData.texPath;
            return GraphicDatabase.Get<Graphic_Multi>(
                texPath,
                base.ShaderFor(pawn),
                Props.graphicData.drawSize,
                base.ColorFor(pawn),
                Props.graphicData.colorTwo
            );
        }

        private string GetBodyTypeTexPath(Pawn pawn)
        {
            string basePath = Props.graphicData.texPath;

            // Return base path if pawn or story is null
            if (pawn?.story?.bodyType == null) return basePath + "Male";

            return basePath + $"_{pawn.story.bodyType.defName}";
        }

        public override Color ColorFor(Pawn pawn)
        {
            Gene_Parasite _Parasite = pawn.genes.GetFirstGeneOfType<Gene_Parasite>();

            if (_Parasite != null && _Parasite.SuitColor != default(Color))
            {
                return _Parasite.SuitColor;
            }

            return base.ColorFor(pawn);
        }
    }
}
