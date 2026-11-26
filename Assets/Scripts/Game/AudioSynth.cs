using System;
using System.Collections.Generic;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// Every sound is synthesised at startup: bells from inharmonic partials, thuds from filtered noise, whooshes
    /// from swept noise. A distant bell tolls over a low water drone the whole time; it falls silent at the end.
    /// </summary>
    public sealed class AudioSynth
    {
        const int Rate = 44100;
        readonly AudioSource sfx, ambience;
        readonly Dictionary<GameEventType, AudioClip> clips = new Dictionary<GameEventType, AudioClip>();
        readonly AudioClip distantToll, step;
        float tollClock = 6f;
        bool silenced;
        public bool Muted;

        public AudioSynth(GameObject host)
        {
            sfx = host.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            ambience = host.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.loop = true;
            ambience.volume = 0.22f;
            ambience.clip = Build("drone", 6f, t =>
            {
                // Whole cycles in 6 s so the loop is seamless.
                float a = Mathf.Sin(t * Mathf.PI * 2f * 41f) * 0.5f + Mathf.Sin(t * Mathf.PI * 2f * 61.5f) * 0.3f;
                return a * (0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f / 3f)) * 0.4f;
            });

            clips[GameEventType.Jump] = Sweep("jump", 0.12f, 300f, 520f, 0.12f);
            clips[GameEventType.Land] = Noise("land", 0.08f, 0.25f, 700f);
            clips[GameEventType.Dash] = Whoosh("dash", 0.22f, 0.32f);
            clips[GameEventType.Swing] = Whoosh("swing", 0.12f, 0.2f);
            clips[GameEventType.Pogo] = Bell("pogo", 0.25f, 880f, 0.18f);
            clips[GameEventType.HitEnemy] = Mix(Noise("thud", 0.12f, 0.45f, 400f), Bell("ping", 0.2f, 1240f, 0.12f));
            clips[GameEventType.EnemyKilled] = Mix(Noise("burst", 0.3f, 0.5f, 900f), Sweep("down", 0.3f, 500f, 120f, 0.15f));
            clips[GameEventType.PlayerHurt] = Mix(Noise("hurt", 0.25f, 0.6f, 300f), Sweep("hurtTone", 0.25f, 260f, 90f, 0.25f));
            clips[GameEventType.HazardHit] = clips[GameEventType.PlayerHurt];
            clips[GameEventType.PlayerDied] = Bell("deathToll", 2.4f, 110f, 0.55f);
            clips[GameEventType.Respawned] = Sweep("respawn", 0.6f, 200f, 640f, 0.12f);
            clips[GameEventType.LampLit] = Chord("lamp", 1.4f, new[] { 392f, 494f, 587f, 784f }, 0.2f);
            clips[GameEventType.Rested] = Chord("rest", 0.8f, new[] { 494f, 587f }, 0.15f);
            clips[GameEventType.WinchPulled] = Mix(Noise("clank", 0.25f, 0.6f, 1200f), Noise("rumble", 1.6f, 0.35f, 60f));
            clips[GameEventType.LungTaken] = Chord("lung", 1.2f, new[] { 523f, 659f, 784f, 1047f }, 0.2f);
            clips[GameEventType.CrabWindup] = Clicks("crabTell", 0.5f, 6, 0.25f);
            clips[GameEventType.CrabLunge] = Mix(Whoosh("lunge", 0.18f, 0.3f), Noise("snap", 0.06f, 0.4f, 2200f));
            clips[GameEventType.JellyWindup] = Sweep("jellyTell", 0.9f, 500f, 1400f, 0.12f);
            clips[GameEventType.JellyPulse] = Bell("jellyRing", 0.9f, 1320f, 0.3f);
            clips[GameEventType.RippleWindup] = Sweep("rippleWindup", 0.8f, 160f, 275f, 0.13f);
            clips[GameEventType.RippleToll] = Bell("rippleToll", 0.8f, 196f, 0.32f);
            clips[GameEventType.BossAwake] = Bell("awake", 3f, 82f, 0.7f);
            clips[GameEventType.BossSlamWindup] = Sweep("rise", 0.8f, 90f, 160f, 0.2f);
            clips[GameEventType.BossSlam] = Mix(Bell("slam", 1.6f, 98f, 0.6f), Noise("slamThud", 0.5f, 0.7f, 120f));
            clips[GameEventType.BossRushWindup] = Sweep("lean", 0.7f, 140f, 70f, 0.2f);
            clips[GameEventType.BossRush] = Whoosh("rush", 0.7f, 0.5f);
            clips[GameEventType.BossWallHit] = Mix(Bell("wall", 1.4f, 123f, 0.55f), Noise("crash", 0.4f, 0.7f, 200f));
            clips[GameEventType.BossRainWarn] = Bell("rainWarn", 1.2f, 659f, 0.25f);
            clips[GameEventType.DripSplash] = Noise("splash", 0.15f, 0.3f, 2600f);
            clips[GameEventType.BossHit] = Mix(Bell("clang", 0.5f, 740f, 0.25f), Noise("clangThud", 0.1f, 0.35f, 500f));
            clips[GameEventType.BossPhase2] = Bell("phase2", 2.4f, 73f, 0.7f);
            clips[GameEventType.BossDefeated] = Bell("silence", 5f, 65f, 0.75f);
            clips[GameEventType.Victory] = Chord("victory", 3f, new[] { 262f, 330f, 392f, 523f, 659f }, 0.22f);
            distantToll = Bell("distant", 4f, 87f, 0.16f);
            step = Noise("step", 0.05f, 0.08f, 900f);
        }

        public void ResetRun()
        {
            silenced = false;
            tollClock = 6f;
        }

        public void Start() { if (!ambience.isPlaying && !Muted) ambience.Play(); }

        public void SetMuted(bool m)
        {
            Muted = m;
            if (m) ambience.Stop(); else ambience.Play();
        }

        public void OnEvent(GameEvent e)
        {
            if (e.Type == GameEventType.BossDefeated) silenced = true;
            if (e.Type == GameEventType.Respawned && !silenced) tollClock = 3f;
            if (Muted) return;
            if (clips.TryGetValue(e.Type, out AudioClip c)) sfx.PlayOneShot(c, 1f);
        }

        /// <summary>The bell that floods the halls tolls in the distance until it is silenced.</summary>
        public void Tick(float dt)
        {
            if (Muted || silenced) return;
            tollClock -= dt;
            if (tollClock <= 0f)
            {
                tollClock = 14f;
                sfx.PlayOneShot(distantToll, 0.9f);
            }
        }

        public void Footstep() { if (!Muted) sfx.PlayOneShot(step, 0.6f); }

        // ---- synthesis ------------------------------------------------------------------------

        static AudioClip Build(string name, float seconds, Func<float, float> f)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            AudioClip c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>A struck bell: inharmonic partials, each decaying at its own rate.</summary>
        static AudioClip Bell(string name, float seconds, float f0, float vol)
        {
            float[] ratio = { 0.5f, 1f, 1.19f, 1.56f, 2f, 2.51f, 2.66f, 3.01f, 4.1f };
            float[] amp = { 0.6f, 1f, 0.55f, 0.45f, 0.4f, 0.25f, 0.2f, 0.15f, 0.1f };
            float[] decay = { 0.7f, 1.2f, 1.6f, 2.2f, 2.6f, 3.4f, 4f, 5f, 6f };
            return Build(name, seconds, t =>
            {
                float s = 0f;
                for (int k = 0; k < ratio.Length; k++)
                    s += Mathf.Sin(t * Mathf.PI * 2f * f0 * ratio[k]) * amp[k] * Mathf.Exp(-t * decay[k] * 3f / seconds);
                return s * vol * 0.35f * Mathf.Min(1f, t * 400f);
            });
        }

        static AudioClip Chord(string name, float seconds, float[] notes, float vol)
        {
            return Build(name, seconds, t =>
            {
                float s = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float start = k * 0.09f;
                    if (t < start) continue;
                    float u = t - start;
                    s += Mathf.Sin(u * Mathf.PI * 2f * notes[k]) * Mathf.Exp(-u * 2.2f) * Mathf.Min(1f, u * 200f);
                }
                return s * vol;
            });
        }

        static AudioClip Sweep(string name, float seconds, float f0, float f1, float vol)
        {
            double phase = 0;
            return Build(name, seconds, t =>
            {
                float u = t / seconds;
                phase += Mathf.Lerp(f0, f1, u) / Rate;
                return Mathf.Sin((float)(phase * Math.PI * 2.0)) * (1f - u) * Mathf.Min(1f, t * 300f) * vol;
            });
        }

        static AudioClip Noise(string name, float seconds, float vol, float cutoff)
        {
            var rng = new System.Random(name.GetHashCode());
            float lp = 0f, k = Mathf.Clamp01(cutoff / 4000f);
            return Build(name, seconds, t =>
            {
                lp += ((float)(rng.NextDouble() * 2.0 - 1.0) - lp) * k;
                return lp * Mathf.Exp(-t * 6f / seconds) * vol * 2f;
            });
        }

        static AudioClip Whoosh(string name, float seconds, float vol)
        {
            var rng = new System.Random(name.GetHashCode());
            float lp = 0f;
            return Build(name, seconds, t =>
            {
                float u = t / seconds;
                float k = 0.05f + 0.4f * Mathf.Sin(u * Mathf.PI);
                lp += ((float)(rng.NextDouble() * 2.0 - 1.0) - lp) * k;
                return lp * Mathf.Sin(u * Mathf.PI) * vol * 2f;
            });
        }

        static AudioClip Clicks(string name, float seconds, int count, float vol)
        {
            return Build(name, seconds, t =>
            {
                float period = seconds / count;
                float u = t % period;
                return Mathf.Sin(u * Mathf.PI * 2f * 2400f) * Mathf.Exp(-u * 120f) * vol;
            });
        }

        static AudioClip Mix(AudioClip a, AudioClip b)
        {
            int n = Mathf.Max(a.samples, b.samples);
            var da = new float[a.samples];
            var db = new float[b.samples];
            a.GetData(da, 0);
            b.GetData(db, 0);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp((i < da.Length ? da[i] : 0f) + (i < db.Length ? db[i] : 0f), -1f, 1f);
            AudioClip c = AudioClip.Create(a.name + "+" + b.name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
