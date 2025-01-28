using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Protean
{
    public class StrainDef : Def
    {
        public string strainName;
        public float rarity = 1f;
        public List<Color> possibleColors;
    }
}
