using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Kadaya
{
    public partial class Game1
    {
        // [f0, f1, duration, wave(0 saw,1 square,2 sine,3 tri), noise mix, volume, noise lowpass, harmonic]
        static readonly Dictionary<string, float[]> SP = new Dictionary<string, float[]>
        {
            ["slash"] = new float[] { 1000, 180, .20f, 0, .80f, .55f, .30f, 0 },
            ["slash3"] = new float[] { 520, 70, .34f, 0, .65f, .70f, .22f, 0 },
            ["hit"] = new float[] { 190, 45, .16f, 1, .55f, .75f, .45f, 0 },
            ["kill"] = new float[] { 150, 30, .50f, 0, .35f, .80f, .20f, .3f },
            ["jump"] = new float[] { 300, 720, .15f, 2, .05f, .35f, .5f, 0 },
            ["land"] = new float[] { 110, 55, .12f, 2, .70f, .55f, .12f, 0 },
            ["dash"] = new float[] { 1300, 160, .24f, 0, .90f, .50f, .40f, 0 },
            ["kunai"] = new float[] { 1600, 600, .10f, 3, .45f, .30f, .50f, 0 },
            ["ras"] = new float[] { 320, 38, .60f, 0, .45f, .90f, .25f, .2f },
            ["clone"] = new float[] { 500, 1600, .32f, 2, .20f, .40f, .5f, .5f },
            ["ult"] = new float[] { 90, 28, 1.3f, 0, .55f, 1.0f, .15f, .4f },
            ["hurt"] = new float[] { 240, 70, .30f, 1, .40f, .70f, .35f, 0 },
            ["check"] = new float[] { 880, 880, .90f, 2, 0, .50f, .5f, .6f },
            ["win"] = new float[] { 330, 990, 1.4f, 2, 0, .55f, .5f, .5f },
            ["tele"] = new float[] { 760, 760, .09f, 1, .10f, .22f, .5f, 0 },
            ["arrow"] = new float[] { 1900, 520, .16f, 3, .50f, .40f, .55f, 0 },
            ["fall"] = new float[] { 420, 55, .70f, 2, .10f, .60f, .5f, 0 },
            ["boom"] = new float[] { 100, 28, .65f, 0, .50f, 1.0f, .10f, 0 },
            ["soul"] = new float[] { 700, 1400, .25f, 2, 0, .35f, .5f, .5f },
        };
        Dictionary<string, SoundEffect> sfxc = new Dictionary<string, SoundEffect>();
        SoundEffect[] songs = new SoundEffect[3]; SoundEffectInstance musInst;

        static byte[] Pcm(float[] m)
        {
            var b = new byte[m.Length * 2];
            for (int i = 0; i < m.Length; i++) { short s = (short)(Math.Max(-1f, Math.Min(1f, m[i])) * short.MaxValue); b[2 * i] = (byte)(s & 255); b[2 * i + 1] = (byte)(s >> 8); }
            return b;
        }

        SoundEffect Syn(float[] q)
        {
            const int sr = 22050; int n = (int)(q[2] * sr); var m = new float[n]; double ph = 0; float lp = 0; var r = new Random(7);
            for (int i = 0; i < n; i++)
            {
                double u = (double)i / n, f = q[0] * Math.Pow(q[1] / q[0], u); ph += f / sr;
                double tone = q[3] == 0 ? (ph % 1) * 2 - 1 : q[3] == 1 ? ((ph % 1) < .5 ? 1 : -1) : q[3] == 2 ? Math.Sin(ph * 6.2832) : Math.Abs((ph % 1) * 4 - 2) - 1;
                tone += q[7] * Math.Sin(ph * 12.566);
                lp += (float)(((r.NextDouble() * 2 - 1) - lp) * q[6]);
                double env = Math.Min(1, i / (sr * .004)) * Math.Pow(1 - u, 2);
                m[i] = (float)((tone * (1 - q[4]) + lp * 3 * q[4]) * env * q[5] * .8);
            }
            return new SoundEffect(Pcm(m), sr, AudioChannels.Mono);
        }

        void Sfx(string k, float x = float.NaN, float v = 1f)
        {
            try
            {
                if (!sfxc.TryGetValue(k, out var se)) { se = Syn(SP[k]); sfxc[k] = se; }
                float pan = float.IsNaN(x) ? 0 : Cl((x - (cam + W / 2f)) / 700f, -1, 1);
                se.Play(Cl(v * .75f, 0, 1), Rf(-.12f, .12f), pan);
            }
            catch { }
        }

        void PlaySong(int th)
        {
            try
            {
                if (musInst != null) { musInst.Stop(); musInst.Dispose(); musInst = null; }
                if (songs[th] == null) songs[th] = MakeSong(th);
                musInst = songs[th].CreateInstance(); musInst.IsLooped = true; musInst.Volume = .6f; musInst.Play();
            }
            catch { }
        }

        SoundEffect MakeSong(int th)
        {
            const int sr = 22050; float bpm = new[] { 64f, 56f, 82f }[th], beat = 60f / bpm; int beats = 32, n = (int)(beats * beat * sr); float dur = n / (float)sr;
            var dry = new float[n]; var r = new Random(300 + th); float f0 = new[] { 55f, 51.91f, 58.27f }[th]; int[] sc = { 0, 2, 3, 7, 8 };
            float Q(float f) => MathF.Round(f * dur) / dur;
            double d0 = Q(f0), d1 = Q(f0 * 2.004f), d2 = Q(f0 * 1.498f), d3 = Q(f0 * 3.01f), p1 = Q(f0 * 4), p2 = Q(f0 * 4.756f), p3 = Q(f0 * 6);
            float lpw = 0; const double TP = 6.2832;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / sr, am = .65 + .35 * Math.Sin(TP * tt * 2 / dur);
                double d = Math.Sin(TP * d0 * tt) * .5 + Math.Sin(TP * d1 * tt) * .22 + Math.Sin(TP * d2 * tt) * .2 * am + Math.Sin(TP * d3 * tt) * .06;
                double pad = (Math.Sin(TP * p1 * tt) + Math.Sin(TP * p2 * tt) + Math.Sin(TP * p3 * tt)) * .05 * (.5 + .5 * Math.Sin(TP * tt / dur));
                lpw += (float)(((r.NextDouble() * 2 - 1) - lpw) * .02); double sw = Math.Sin(TP * tt * 3 / dur + 1);
                dry[i] = (float)(d * .22 + pad + lpw * 4 * (.4 + .6 * sw * sw) * .35);
            }
            // taiko drums
            for (int b = 0; b < beats; b++)
            {
                int pos = (int)(b * beat * sr), kind = b % 4 == 0 ? 2 : b % 4 == 2 ? 1 : (th > 0 && b % 4 == 3 ? 3 : 0); if (kind == 0) continue;
                float amp = kind == 2 ? .75f : kind == 1 ? .45f : .2f; int len = (int)(.5f * sr); double ph = 0;
                for (int j = 0; j < len; j++)
                {
                    double u = (double)j / len, f = 45 + 80 * Math.Exp(-u * 9) * (kind == 3 ? 1.8 : 1); ph += f / sr;
                    double s = Math.Sin(ph * TP) * Math.Exp(-u * 6) + (r.NextDouble() * 2 - 1) * Math.Exp(-u * 45) * .3;
                    dry[(pos + j) % n] += (float)(s * amp);
                }
            }
            // melody (pentatonic plucks)
            for (int h = 0; h < beats * 2; h++)
            {
                if (r.NextDouble() > (th == 2 ? .45 : .28)) continue;
                double fr = f0 * 8 * Math.Pow(2, sc[r.Next(5)] / 12.0) * (r.Next(3) == 0 ? 2 : 1); int pos = (int)(h * beat * .5f * sr), len = (int)(1.4f * sr); double ph = 0;
                for (int j = 0; j < len; j++)
                {
                    double u = (double)j / len; ph += fr / sr;
                    double s = (Math.Sin(ph * TP) + .4 * Math.Sin(ph * TP * 2) + .2 * Math.Sin(ph * TP * 3)) * Math.Exp(-u * 5) * Math.Min(1, j / 80.0);
                    dry[(pos + j) % n] += (float)(s * .11);
                }
            }
            // echo (circular so the loop is seamless)
            var o = (float[])dry.Clone(); int dl = (int)(beat * .75f * sr);
            for (int pass = 0; pass < 3; pass++) for (int i = 0; i < n; i++) o[i] = dry[i] + .38f * o[(i - dl + n) % n];
            float peak = 0.001f; for (int i = 0; i < n; i++) peak = Math.Max(peak, Math.Abs(o[i]));
            for (int i = 0; i < n; i++) o[i] = MathF.Tanh(o[i] / peak * 1.1f) * .7f;
            return new SoundEffect(Pcm(o), sr, AudioChannels.Mono);
        }
    }
}
