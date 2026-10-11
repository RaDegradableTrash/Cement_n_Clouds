using System;
using System.IO;
using UnityEngine;

namespace Voyage.Dialogue
{
    public static class DialogueSaveStore
    {
        public static void Save(string path, DialogueSnapshot snapshot)
        {
            string json = JsonUtility.ToJson(snapshot, true);
            if (JsonUtility.FromJson<DialogueSnapshot>(json)?.schemaVersion != 1) throw new InvalidDataException("Invalid snapshot");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
            {
                writer.Write(json); writer.Flush(); stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
        public static ConversationRuntime Load(string path, DialogueContent content)
        {
            // No implicit fallback or overwrite: a bad/newer save remains available for recovery.
            return new ConversationRuntime(content, JsonUtility.FromJson<DialogueSnapshot>(File.ReadAllText(path))
                ?? throw new InvalidDataException("Empty dialogue save"));
        }
    }
}
