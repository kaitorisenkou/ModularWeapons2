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

        //public IEnumerable<(string, Color)> GetStatChangeTexts(CompModularWeapon weapon = null) {
        public IEnumerable<(TaggedString,int)> GetStatChangeTexts(CompModularWeapon weapon = null) {
            var builder = new StringBuilder();
            //Color color = Color.white;

            if (verbPropsOffset != null) {
                foreach(var i in verbPropsOffset.GetStatChangeTexts()) {
                    yield return i;
                }
            }
            if (false /*TODO オプションから簡易表示切替*/ && weapon != null && statOffsets!=null && statFactors!=null) {
                var _statFactors = statFactors;
                var weaponDef = weapon.parent.def;
                var doubledStat = statOffsets.Where(t => _statFactors.Any(tt => tt.stat == t.stat));
                foreach(var i in doubledStat) {
                    builder.Append(i.stat.label);
                    builder.Append(" ");
                    float valueSum = weaponDef.statBases.FirstOrFallback(t => t.stat == i.stat, null)?.value ?? 0;
                    valueSum = ((valueSum + i.value) * (1 + (statFactors.FirstOrFallback(t => t.stat == i.stat, null)?.value ?? 0))) - valueSum;
                    valueSum = Mathf.Round(valueSum * 100f) / 100f;
                    float statValue = (i.stat.ToStringStyleUnfinalized.ToString().Contains("Percent") ? valueSum * 100 : valueSum);

                    if (valueSum > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                        builder.Append("<color=\"red\">");
                        //color = Color.red;
                    } else {
                        builder.Append("<color=\"green\">");
                        //color = Color.green;
                    }
                    if (statValue > 0) { builder.Append("+"); }
                    builder.Append(statValue.ToString());
                    builder.Append("</color>");
                    //yield return (builder.ToString(), color);
                    yield return (builder.ToString(),i.stat.displayPriorityInCategory);
                    builder.Clear();

                }
                foreach (var i in statOffsets.Where(t => !doubledStat.Any(tt => tt.stat == t.stat))) {
                    builder.Append(i.stat.label);
                    builder.Append(" ");
                    float statValue = (i.stat.ToStringStyleUnfinalized.ToString().Contains("Percent") ? i.value * 100 : i.value);
                    if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                        //color = Color.red;
                        builder.Append("<color=\"red\">");
                    } else {
                        //color = Color.green;
                        builder.Append("<color=\"green\">");
                    }
                    if (statValue > 0) { builder.Append("+"); }
                    builder.Append(statValue.ToString());
                    builder.Append("</color>");
                    
                    //yield return (builder.ToString(), color);
                    yield return (builder.ToString(), i.stat.displayPriorityInCategory);
                    builder.Clear();
                }
                foreach (var i in statFactors.Where(t => !doubledStat.Any(tt => tt.stat == t.stat))) {
                    builder.Append(i.stat.label);
                    builder.Append(" ");
                    if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                        //color = Color.red;
                        builder.Append("<color=\"red\">");
                    } else {
                        //color = Color.green;
                        builder.Append("<color=\"green\">");
                    }
                    if (i.value > 0) builder.Append("+");
                    builder.Append(i.value.ToStringPercent());
                    builder.Append("</color>");
                    //yield return (builder.ToString(), color);
                    yield return (builder.ToString(), i.stat.displayPriorityInCategory);
                    builder.Clear();
                }
            } else {
                if (statOffsets != null) {
                    foreach (var i in statOffsets) {
                        builder.Append(i.stat.label);
                        builder.Append(" ");
                        float statValue = (i.stat.ToStringStyleUnfinalized.ToString().Contains("Percent") ? i.value * 100 : i.value);
                        if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                            //color = Color.red;
                            builder.Append("<color=\"red\">");
                        } else {
                            //color = Color.green;
                            builder.Append("<color=\"green\">");
                        }
                        if (statValue > 0) { builder.Append("+"); }
                        builder.Append(statValue.ToString());
                        builder.Append("</color>");
                        //yield return (builder.ToString(), color);
                        yield return (builder.ToString(), i.stat.displayPriorityInCategory);
                        builder.Clear();
                    }
                }
                if (statFactors != null) {
                    foreach (var i in statFactors) {
                        builder.Append(i.stat.label);
                        builder.Append(" ");
                        if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                            //color = Color.red;
                            builder.Append("<color=\"red\">");
                        } else {
                            //color = Color.green;
                            builder.Append("<color=\"green\">");
                        }
                        if (i.value > 0) builder.Append("+");
                        builder.Append(i.value.ToStringPercent());
                        builder.Append("</color>");
                        //yield return (builder.ToString(), color);
                        yield return (builder.ToString(), i.stat.displayPriorityInCategory);
                        builder.Clear();
                    }
                }
            }
            if (equippedStatOffsets != null) {
                foreach (var i in equippedStatOffsets) {
                    builder.Append(i.stat.label);
                    builder.Append(" ");
                    if (i.value > 0 ^ !MW2Mod.lessIsBetter.Contains(i.stat.label.CapitalizeFirst())) {
                        //color = Color.red;
                        builder.Append("<color=\"red\">");
                    } else {
                        //color = Color.green;
                        builder.Append("<color=\"green\">");
                    }
                    builder.Append(i.value.ToStringByStyle(i.stat.ToStringStyleUnfinalized, ToStringNumberSense.Offset));
                    builder.Append("</color>");
                    //yield return (builder.ToString(), color);
                    yield return (builder.ToString(), i.stat.displayPriorityInCategory);
                    builder.Clear();
                }
            }
            if (tools != null) {
                //color = Color.white;
                foreach (var tool in tools) {
                    builder.Append("MW2_DescMeleeTool".Translate());
                    builder.Append(tool.label);
                    builder.Append(" (");
                    builder.Append(string.Join(",",tool.capacities.Select(t=>t.label)));
                    builder.Append(") ");
                    builder.Append(string.Format("{0}dmg / {1}sec", tool.power, tool.cooldownTime));
                    //yield return (builder.ToString(), color);
                    yield return (builder.ToString(), StatDefOf.MeleeDPS.displayPriorityInCategory);
                    builder.Clear();
                }
            }

            //アビリティの文章
            if (ability != null) {
                builder.Append("MW2_DescAbility".Translate());
                builder.Append(ability.abilityDef.label);
                //yield return (builder.ToString(), Color.white);
                yield return (builder.ToString(),-10001);
                builder.Clear();
            }

            if(tacDevice!=null && !tacDevice.descriptionString.NullOrEmpty()) {
                builder.Append("<color=\"green\">");
                builder.Append(tacDevice.descriptionString);
                builder.Append("</color>");
                yield return (builder.ToString(),-10000);
                builder.Clear();
            }
        }
    }
}
