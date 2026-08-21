using System;
using System.IO;
using UnityEngine;

namespace Fragments.Audio
{
    /// <summary>
    /// Minimal WAV writer/reader (16-bit PCM), based on the common Unity SavWav pattern.
    /// </summary>
    public static class SavWav
    {
        const int HeaderSize = 44;

        public static bool Save(string filepath, AudioClip clip)
        {
            if (clip == null || string.IsNullOrEmpty(filepath)) return false;

            Directory.CreateDirectory(Path.GetDirectoryName(filepath) ?? ".");

            using (var fileStream = CreateEmpty(filepath))
            {
                ConvertAndWrite(fileStream, clip);
                WriteHeader(fileStream, clip);
            }
            return true;
        }

        public static AudioClip Load(string filepath)
        {
            if (!File.Exists(filepath)) return null;

            using (var stream = File.OpenRead(filepath))
            using (var reader = new BinaryReader(stream))
            {
                // RIFF header
                reader.ReadBytes(4); // "RIFF"
                reader.ReadInt32();  // file size
                reader.ReadBytes(4); // "WAVE"

                // fmt chunk
                reader.ReadBytes(4); // "fmt "
                int fmtSize = reader.ReadInt32();
                int audioFormat = reader.ReadInt16();
                int channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // byte rate
                reader.ReadInt16(); // block align
                int bitsPerSample = reader.ReadInt16();
                if (fmtSize > 16)
                    reader.ReadBytes(fmtSize - 16);

                // Find data chunk
                int dataSize = 0;
                while (stream.Position < stream.Length)
                {
                    string id = new string(reader.ReadChars(4));
                    int size = reader.ReadInt32();
                    if (id == "data")
                    {
                        dataSize = size;
                        break;
                    }
                    reader.ReadBytes(size);
                }

                if (dataSize <= 0 || audioFormat != 1 || bitsPerSample != 16)
                    return null;

                int sampleCount = dataSize / (bitsPerSample / 8);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                    samples[i] = reader.ReadInt16() / 32768f;

                int frames = sampleCount / channels;
                var clip = AudioClip.Create(Path.GetFileNameWithoutExtension(filepath),
                    frames, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }
        }

        static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte[] empty = new byte[HeaderSize];
            fileStream.Write(empty, 0, empty.Length);
            return fileStream;
        }

        static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            Int16[] intData = new Int16[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];
            const float rescaleFactor = 32767f;

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                bytesData[i * 2] = byteArr[0];
                bytesData[i * 2 + 1] = byteArr[1];
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        static void WriteHeader(FileStream stream, AudioClip clip)
        {
            int hz = clip.frequency;
            int channels = clip.channels;
            int samples = clip.samples;

            stream.Seek(0, SeekOrigin.Begin);

            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            stream.Write(riff, 0, 4);

            byte[] chunkSize = BitConverter.GetBytes(stream.Length - 8);
            stream.Write(chunkSize, 0, 4);

            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            stream.Write(wave, 0, 4);

            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            stream.Write(fmt, 0, 4);

            byte[] subChunk1 = BitConverter.GetBytes(16);
            stream.Write(subChunk1, 0, 4);

            ushort one = 1;
            byte[] audioFormat = BitConverter.GetBytes(one);
            stream.Write(audioFormat, 0, 2);

            byte[] numChannels = BitConverter.GetBytes((ushort)channels);
            stream.Write(numChannels, 0, 2);

            byte[] sampleRate = BitConverter.GetBytes(hz);
            stream.Write(sampleRate, 0, 4);

            byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
            stream.Write(byteRate, 0, 4);

            ushort blockAlign = (ushort)(channels * 2);
            stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            ushort bps = 16;
            stream.Write(BitConverter.GetBytes(bps), 0, 2);

            byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            stream.Write(dataString, 0, 4);

            byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            stream.Write(subChunk2, 0, 4);
        }
    }
}
