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
    }
}
