using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace Fragments.Data
{
    public static class JournalStore
    {
        static string Root => Path.Combine(UnityEngine.Application.persistentDataPath, "journals");

        public static void Save(JournalData data)
        {
            Directory.CreateDirectory(Root);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path.Combine(Root, data.id + ".json"), json);
        }

        public static JournalData Load(string id)
        {
            string path = Path.Combine(Root, id + ".json");
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<JournalData>(File.ReadAllText(path));
        }

        public static List<JournalData> LoadAll()
        {
            var list = new List<JournalData>();
            if (!Directory.Exists(Root)) return list;
            foreach (var file in Directory.GetFiles(Root, "*.json"))
            {
                try { list.Add(JsonUtility.FromJson<JournalData>(File.ReadAllText(file))); }
                catch { /* skip corrupt files */ }
            }
            list.Sort((a, b) => b.lastOpenedAt.CompareTo(a.lastOpenedAt));
            return list;
        }

        public static void Delete(string id)
        {
            string path = Path.Combine(Root, id + ".json");
            if (File.Exists(path)) File.Delete(path);
        }
    }
}