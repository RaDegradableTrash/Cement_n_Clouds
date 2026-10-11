using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Voyage.Dialogue;

public static class DialogueMarkdownTools
{
    public const string Source = "Assets/ObsidianDocs/NPC_CoBlues 钴蓝/CoBlues.playable.md";
    const string Target = "Assets/Resources/Dialogue/CoBlues.json";

    [MenuItem("Voyage/Dialogue/Validate Markdown Round Trip")]
    public static void ValidateRoundTrip()
    {
        var original = JsonUtility.FromJson<DialogueContent>(File.ReadAllText(Target));
        string markdown = DialogueMarkdown.Write(original);
        var restored = DialogueMarkdown.Read(markdown);
        if (JsonUtility.ToJson(original) != JsonUtility.ToJson(restored))
            throw new Exception("Markdown round trip changed dialogue state or text");
        string changed = markdown.Replace("> " + original.nodes[0].text + "\n", "> 测试编辑台词。\n");
        if (DialogueMarkdown.Read(changed).nodes[0].text != "测试编辑台词。")
            throw new Exception("Markdown text editing failed");
        bool rejected = false;
        try { DialogueMarkdown.Read(markdown.Replace("\"entry\": \""+original.entry+"\"", "\"entry\": \"missing.node\"")); }
        catch (FormatException) { rejected = true; }
        if (!rejected) throw new Exception("Broken graph was not rejected");
        original.nodes[0].text = "第一句。\n\n第二句。";
        var withChoice = Array.Find(original.nodes, n => n.choices != null && n.choices.Length > 0);
        if (withChoice == null) throw new Exception("No response available for multiline test");
        withChoice.choices[0].text = "先等等。\n我还有个问题。";
        string multiline = DialogueMarkdown.Write(original);
        if (JsonUtility.ToJson(original) != JsonUtility.ToJson(DialogueMarkdown.Read(multiline)))
            throw new Exception("Multiline dialogue or response lost text");
        if (DialogueMarkdown.Read(multiline.Replace("> \n", ">\n")).nodes[0].text != original.nodes[0].text)
            throw new Exception("Obsidian empty quoted paragraph lost text");
        Directory.CreateDirectory("Logs/DialogueValidation");
        File.WriteAllText("Logs/DialogueValidation/markdown.txt", $"PASS {original.nodes.Length} nodes round trip, text edit, multiline speech/responses, empty quoted paragraphs, invalid graph rejection");
        Debug.Log("PASS Markdown story round trip, text editing and invalid graph rejection");
    }

    [MenuItem("Voyage/Dialogue/Open Markdown Story")]
    public static void Open()
    {
        if (!File.Exists(Source))
        {
            var content = JsonUtility.FromJson<DialogueContent>(File.ReadAllText(Target));
            Directory.CreateDirectory(Path.GetDirectoryName(Source));
            File.WriteAllText(Source, DialogueMarkdown.Write(content), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(Source);
        }
        EditorUtility.RevealInFinder(Source);
    }

    [MenuItem("Voyage/Dialogue/Import Markdown Story")]
    public static void Import()
    {
        try
        {
            var content = DialogueMarkdown.Read(File.ReadAllText(Source));
            var json = JsonUtility.ToJson(content, true);
            if (File.ReadAllText(Target) != json)
            {
                File.WriteAllText(Target, json, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(Target);
            }
            Debug.Log($"Markdown story imported: {content.nodes.Length} nodes. New conversations use this content after entering Play Mode.");
        }
        catch (Exception e) { Debug.LogError("Markdown story was not imported; previous content kept.\n" + e.Message); }
    }
}

public sealed class DialogueMarkdownPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] from)
    {
        if (Array.Exists(imported, p => p == DialogueMarkdownTools.Source))
            EditorApplication.delayCall += DialogueMarkdownTools.Import;
    }
}
