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

        public VerbProperties AffectVerbProps(VerbProperties props) {
            props.warmupTime += this.warmupTime;
            props.range += this.range;
            props.burstShotCount += this.burstShotCount;
            props.ticksBetweenBurstShots += this.ticksBetweenBurstShots;
            if (soundCastOverride != null)
                props.soundCast = soundCastOverride;
            if (projectileOverride != null)
                props.defaultProjectile = projectileOverride;

            return props;
        }

        public IEnumerable<(string, Color)> GetStatChangeTexts() {
            var builder = new StringBuilder();
            Color color = Color.white;

            if (!Mathf.Approximately(warmupTime, 0)) {
                if (warmupTime > 0) {
                    builder.Append("+");
                    color = Color.red;
                } else {
                    color = Color.green;

                }
                builder.Append(warmupTime.ToString("0.##"));
                builder.Append("LetterSecond".Translate());
                builder.Append(" ");
                builder.Append("RangedWarmupTime".Translate());
                yield return (builder.ToString(), color);
            }

            if (!Mathf.Approximately(range, 0)) {
                builder.Clear();
                if (range > 0) {
                    builder.Append("+");
                    color = Color.green;
                } else {
                    color = Color.red;
                }
                builder.Append(range.ToString());
                builder.Append(" ");
                builder.Append("Range".Translate());
                yield return (builder.ToString(), color);
            }

            if (burstShotCount != 0) {
                builder.Clear();
                if (burstShotCount > 0) {
                    builder.Append("+");
                    color = Color.green;
                } else {
                    color = Color.red;
                }
                builder.Append(burstShotCount.ToString());
                builder.Append(" ");
                builder.Append("BurstShotCount".Translate());
                yield return (builder.ToString(), color);
            }

            if (ticksBetweenBurstShots != 0) {
                builder.Clear();
                if (ticksBetweenBurstShots > 0) {
                    builder.Append("+");
                    color = Color.red;
                } else {
                    color = Color.green;
                }
                builder.Append(ticksBetweenBurstShots.ToString());
                builder.Append("ticks ");
                builder.Append("BurstShotFireRate".Translate());
                yield return (builder.ToString(), color);
            }

            if (projectileOverride != null) {
                builder.Clear();
                builder.Append("MW2_ChangeCaliber".Translate());
                builder.Append(projectileOverride.label);
                yield return (builder.ToString(), color);
            }
        }
    }
}
