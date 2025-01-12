using HarmonyLib;
using Verse;

namespace Protean
{
    [StaticConstructorOnStartup]
    public static class GraphicsPatches
    {
        static GraphicsPatches()
        {
            var harmony = new Harmony("com.protean.graphicspatches");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(PawnRenderNode_Hair), "GraphicFor")]
        public static class HideHair_Patch
        {
            public static bool Prefix(Pawn pawn, ref Graphic __result)
            {
                if (pawn.health.hediffSet.HasHediff(ProteanDefOf.Protean_SuitActive))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(PawnRenderNode_Beard), "GraphicFor")]
        public static class HideHair_Beard
        {
            public static bool Prefix(Pawn pawn, ref Graphic __result)
            {
                if (pawn.health.hediffSet.HasHediff(ProteanDefOf.Protean_SuitActive))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Head))]
        [HarmonyPatch("CanDrawNow")]
        public class Patch_PawnRenderNodeWorker_Apparel_Head_CanDrawNow
        {
            public static bool Prefix(PawnRenderNode n, PawnDrawParms parms, ref bool __result)
            {
                if (parms.pawn.health.hediffSet.HasHediff(ProteanDefOf.Protean_SuitActive))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Body))]
        [HarmonyPatch("CanDrawNow")]
        public class Patch_PawnRenderNodeWorker_Apparel_Body_CanDrawNow
        {
            public static bool Prefix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
            {
                if (parms.pawn.health.hediffSet.HasHediff(ProteanDefOf.Protean_SuitActive))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PawnRenderNode_Fur), "GraphicFor")]
        public static class HidePawnRenderNode_Fur
        {
            public static bool Prefix(Pawn pawn, ref Graphic __result)
            {
                if (pawn.health.hediffSet.HasHediff(ProteanDefOf.Protean_SuitActive))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }
    
    }
}
