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


        //overlay stuff
        public bool canColorShift = true;
        public int colorShiftTicks = 300;
        public List<Color> possibleShiftColors;

        public bool canPulsate = true;
        public float pulseAmount = 0.05f;
        public float pulseSpeed = 1f;

        public bool lerpAlphaWithHealth = true;
        public FloatRange alphaHealthRange = new FloatRange(0, 1);
    }
}
