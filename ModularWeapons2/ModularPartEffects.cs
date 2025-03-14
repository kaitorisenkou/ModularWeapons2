using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ModularWeapons2 {
    public struct ModularPartEffects {
        public List<StatModifier> statOffsets;
        public List<StatModifier> statFactors;
        public List<StatModifier> equippedStatOffsets;
        public VerbPropsOffset verbPropsOffset;
        public List<MountAdapterClass> additionalAdapters;
        public List<Tool> tools;
        public MWAbilityProperties ability;
        public MWTacDevice tacDevice;

        public IEnumerable<(string, Color)> GetStatChangeTexts(CompModularWeapon weapon = null) {
            var builder = new StringBuilder();
            Color color = Color.white;

            if (verbPropsOffset != null) {
                foreach(var i in verbPropsOffset.GetStatChangeTexts()) {
                    yield return i;
                }
            }

            if (statOffsets != null) {
                foreach (var i in statOffsets) {
                    builder.Clear();
                    float statValue = (i.stat.ToStringStyleUnfinalized.ToString().Contains("Percent") ? i.value * 100 : i.value);
                    if (statValue > 0) { builder.Append("+"); }
                    builder.Append(statValue.ToString());
                    builder.Append(" ");
                    builder.Append(i.stat.label);
                    if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                        color = Color.red;
                    } else {
                        color = Color.green;
                    }
                    yield return (builder.ToString(), color);
                }
            }
            if (statFactors != null) {
                foreach (var i in statFactors) {
                    builder.Clear();
                    if (i.value > 0) builder.Append("+");
                    builder.Append(i.value.ToStringPercent());
                    builder.Append(" ");
                    builder.Append(i.stat.label);
                    if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                        color = Color.red;
                    } else {
                        color = Color.green;
                    }
                    yield return (builder.ToString(), color);
                }
            }
            if (equippedStatOffsets != null) {
                foreach (var i in equippedStatOffsets) {
                    builder.Clear();
                    builder.Append(i.value.ToStringByStyle(i.stat.ToStringStyleUnfinalized, ToStringNumberSense.Offset));
                    builder.Append(" ");
                    builder.Append(i.stat.label);
                    if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                        color = Color.red;
                    } else {
                        color = Color.green;
                    }
                    yield return (builder.ToString(), color);
                }
            }
            if (tools != null) {
                color = Color.white;
                foreach (var tool in tools) {
                    builder.Clear();
                    builder.Append("MW2_DescMeleeTool".Translate());
                    builder.Append(tool.label);
                    builder.Append(" (");
                    builder.Append(string.Join(",",tool.capacities.Select(t=>t.label)));
                    builder.Append(") ");
                    builder.Append(string.Format("{0}dmg / {1}sec", tool.power, tool.cooldownTime));
                    yield return (builder.ToString(), color);
                }
            }

            //アビリティの文章
            if (ability != null) {
                builder.Clear();
                builder.Append("MW2_DescAbility".Translate());
                builder.Append(ability.abilityDef.label);
                yield return (builder.ToString(), Color.white);
            }

            if(tacDevice!=null && !tacDevice.descriptionString.NullOrEmpty()) {
                yield return (tacDevice.descriptionString, Color.white);
            }
        }
    }
}
