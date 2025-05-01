using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace ModularWeapons2 {
    public class VerbPropsOffset {
        public float warmupTime = 0f;
        public float range = 0f;
        public int burstShotCount = 0;
        public int ticksBetweenBurstShots = 0;
        public SoundDef soundCastOverride = null;
        public ThingDef projectileOverride = null;
        public int priority = 0;

        public VerbProperties AffectVerbProps(VerbProperties props) {
            props.warmupTime += this.warmupTime;
            props.range += this.range;
            props.burstShotCount += this.burstShotCount;
            props.ticksBetweenBurstShots += this.ticksBetweenBurstShots;
            if (soundCastOverride != null) {
                //Log.Message("[MW2]sound override: " + soundCastOverride.defName);
                props.soundCast = soundCastOverride;
            }
            if (projectileOverride != null)
                props.defaultProjectile = projectileOverride;

            return props;
        }

        public IEnumerable<(TaggedString, int)> GetStatChangeTexts() {
            var builder = new StringBuilder();
            //Color color = Color.white;

            if (!Mathf.Approximately(warmupTime, 0)) {
                builder.Append("RangedWarmupTime".Translate());
                builder.Append(" ");
                if (warmupTime > 0) {
                    builder.Append("<color=\"red\">");
                    builder.Append("+");
                    //color = Color.red;
                } else {
                    builder.Append("<color=\"green\">");
                    //color = Color.green;

                }
                builder.Append(warmupTime.ToString("0.##"));
                builder.Append("LetterSecond".Translate());
                builder.Append("</color>");
                //yield return (builder.ToString(), color);
                yield return (builder.ToString(),3555);
                builder.Clear();
            }

            if (!Mathf.Approximately(range, 0)) {
                builder.Append("Range".Translate());
                builder.Append(" ");
                if (range > 0) {
                    builder.Append("<color=\"green\">");
                    builder.Append("+");
                    //color = Color.green;
                } else {
                    builder.Append("<color=\"red\">");
                    //color = Color.red;
                }
                builder.Append(range.ToString());
                builder.Append("</color>");
                //yield return (builder.ToString(), color);
                yield return (builder.ToString(),5390);
                builder.Clear();
            }

            if (burstShotCount != 0) {
                builder.Append("BurstShotCount".Translate());
                builder.Append(" ");
                if (burstShotCount > 0) {
                    builder.Append("<color=\"green\">");
                    builder.Append("+");
                    //color = Color.green;
                } else {
                    builder.Append("<color=\"red\">");
                    //color = Color.red;
                }
                builder.Append(burstShotCount.ToString());
                builder.Append("</color>");
                //yield return (builder.ToString(), color);
                yield return (builder.ToString(),5391);
                builder.Clear();
            }

            if (ticksBetweenBurstShots != 0) {
                builder.Append("BurstShotFireRate".Translate());
                builder.Append(" ");
                if (ticksBetweenBurstShots > 0) {
                    builder.Append("<color=\"red\">");
                    builder.Append("+");
                    //color = Color.red;
                } else {
                    builder.Append("<color=\"green\">");
                    //color = Color.green;
                }
                builder.Append(ticksBetweenBurstShots.ToString());
                builder.Append("ticks</color>");
                //yield return (builder.ToString(), color);
                yield return (builder.ToString(),5392);
                builder.Clear();
            }

            if (projectileOverride != null) {
                builder.Append("MW2_ChangeCaliber".Translate());
                builder.Append(projectileOverride.label);
                //color = Color.white;
                //yield return (builder.ToString(), color);
                yield return (builder.ToString(),5500);
                builder.Clear();
            }
        }
    }
}
