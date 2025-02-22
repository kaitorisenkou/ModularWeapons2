using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public struct ModularPartEffects {
        public List<StatModifier> statOffsets;
        public List<StatModifier> statFactors;
        public List<StatModifier> equippedStatOffsets;
        public List<MountAdapterClass> additionalAdapters;
        public List<Tool> tools;

        public IEnumerable<(string, Color)> GetStatChangeTexts(CompModularWeapon weapon = null) {
            var builder = new StringBuilder();
            Color color = Color.white;
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
                    builder.Append(": ");
                    builder.Append(tool.label);
                    builder.Append(" (");
                    builder.Append(string.Join(",",tool.capacities.Select(t=>t.label)));
                    builder.Append(") ");
                    builder.Append(tool.power);
                    builder.Append("dmg / ");
                    builder.Append(tool.cooldownTime);
                    builder.Append("sec");
                    yield return (builder.ToString(), color);
                }
            }
        }
    }
}
