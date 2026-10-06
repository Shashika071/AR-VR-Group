using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ReefExplorer.EditorTools
{
    public static class ProceduralAudioFactory
    {
        const string AudioFolder = "Assets/ReefExplorer/Audio";

        public static void EnsureAllClips()
        {
            Directory.CreateDirectory(AudioFolder);
            CreateTone("sfx_scanner_start", 880f, 0.12f, 0.35f);
            CreateTone("sfx_scanner_progress", 660f, 0.05f, 0.2f);
            CreateTone("sfx_scanner_success", 1175f, 0.22f, 0.4f);
            CreateTone("sfx_invalid", 180f, 0.18f, 0.35f);
            CreateTone("sfx_sample_fill", 420f, 0.3f, 0.35f);
            CreateTone("sfx_objective", 740f, 0.25f, 0.4f);
            CreateTone("sfx_mission_success", 523.25f, 0.55f, 0.45f, true);
            CreateNoiseLoop("ambience_underwater", 4f, 0.08f);
            CreateTone("ambience_station_hum", 90f, 2f, 0.12f, false, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static AudioClip Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{name}.wav");
        }

        static void CreateTone(string name, float freq, float seconds, float volume, bool arpeggio = false, bool loopFriendly = false)
        {
            var path = $"{AudioFolder}/{name}.wav";
            if (File.Exists(path))
                return;

            var sampleRate = 44100;
            var samples = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = loopFriendly
                    ? 1f
                    : Mathf.SmoothStep(0f, 1f, t / 0.02f) * Mathf.SmoothStep(0f, 1f, (seconds - t) / 0.05f);
                var f = arpeggio ? freq * (1f + 0.5f * Mathf.Floor(t * 6f) / 6f) : freq;
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * volume * envelope;
            }

            WriteWav(path, data, sampleRate);
        }

        static void CreateNoiseLoop(string name, float seconds, float volume)
        {
            var path = $"{AudioFolder}/{name}.wav";
            if (File.Exists(path))
                return;

            var sampleRate = 44100;
            var samples = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[samples];
            var rand = new System.Random(42);
            float prev = 0f;
            for (var i = 0; i < samples; i++)
            {
                var white = (float)(rand.NextDouble() * 2.0 - 1.0);
                prev = prev * 0.95f + white * 0.05f; // soft rumble
                data[i] = prev * volume;
            }

            WriteWav(path, data, sampleRate);
        }

        static void WriteWav(string path, float[] samples, int sampleRate)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            const short channels = 1;
            const short bitsPerSample = 16;
            var byteRate = sampleRate * channels * bitsPerSample / 8;
            var blockAlign = (short)(channels * bitsPerSample / 8);
            var dataSize = samples.Length * blockAlign;

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (var sample in samples)
            {
                var clamped = Mathf.Clamp(sample, -1f, 1f);
                writer.Write((short)(clamped * short.MaxValue));
            }
        }
    }
}
