using System.IO;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Fragments.Audio
{
    public static class AudioClipStore
    {
        static string ClipsDir(string journalId)
        {
            var path = Path.Combine(Application.persistentDataPath, "audio", journalId);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        public static string SaveClip(string journalId, AudioClip clip)
        {
            var filename = System.Guid.NewGuid().ToString() + ".wav";
            var path = Path.Combine(ClipsDir(journalId), filename);
            SavWav.Save(path, clip);
            return filename;
        }

        public static AudioClip LoadClip(string journalId, string filename)
        {
            var path = Path.Combine(ClipsDir(journalId), filename);
            if (!File.Exists(path)) return null;
            return SavWav.Load(path);
        }
    }
}
