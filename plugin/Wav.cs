using System;
using System.Text;
using UnityEngine;

namespace FluidLove.AlbumCases
{
    // Minimal 16-bit PCM WAV reader -> AudioClip
    internal static class Wav
    {
        public static AudioClip Load(string name, byte[] d)
        {
            if (Encoding.ASCII.GetString(d, 0, 4) != "RIFF") throw new Exception(name + " is not a WAV");
            int channels = 1, rate = 44100, bits = 16, pos = 12, dataStart = -1, dataLen = 0;
            while (pos + 8 <= d.Length)
            {
                string id = Encoding.ASCII.GetString(d, pos, 4);
                int len = BitConverter.ToInt32(d, pos + 4);
                if (id == "fmt ") { channels = BitConverter.ToInt16(d, pos + 10); rate = BitConverter.ToInt32(d, pos + 12); bits = BitConverter.ToInt16(d, pos + 22); }
                else if (id == "data") { dataStart = pos + 8; dataLen = Math.Min(len, d.Length - dataStart); break; }
                pos += 8 + len + (len & 1);
            }
            if (dataStart < 0 || bits != 16) throw new Exception(name + ": need 16-bit PCM");
            int n = dataLen / 2;
            var samples = new float[n];
            for (int i = 0; i < n; i++) samples[i] = BitConverter.ToInt16(d, dataStart + i * 2) / 32768f;
            var clip = AudioClip.Create(name.Replace(".wav", ""), n / channels, channels, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
