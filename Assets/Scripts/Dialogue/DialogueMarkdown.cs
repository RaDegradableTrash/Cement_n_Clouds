using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Voyage.Dialogue
{
    // Dialogue text stays ordinary Markdown; fenced metadata preserves branching,
    // conditions and consequences without silently discarding authored state.
    public static class DialogueMarkdown
    {
        public static DialogueContent Read(string markdown)
        {
            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            DialogueContent content = null;
            var nodes = new List<DialogueNode>();
            DialogueNode node = null;
            DialogueChoice choice = null;
            var choices = new List<DialogueChoice>();
            bool hasQuotedText = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("```dialogue-", StringComparison.Ordinal))
                {
                    string kind = line.Substring(12).Trim();
                    var json = new StringBuilder();
                    int start = i + 1;
                    while (++i < lines.Length && lines[i] != "```") json.AppendLine(lines[i]);
                    if (i == lines.Length) throw new FormatException($"Line {start}: unclosed metadata fence");
                    try
                    {
                        if (kind == "content")
                        {
                            if (content != null) throw new FormatException("Duplicate content header");
                            content = JsonUtility.FromJson<DialogueContent>(json.ToString());
                        }
                        else if (kind == "node")
                        {
                            if (node != null) node.choices = choices.ToArray();
                            choices.Clear(); choice = null;
                            node = JsonUtility.FromJson<DialogueNode>(json.ToString()); nodes.Add(node);
                        }
                        else if (kind == "choice")
                        {
                            if (node == null) throw new FormatException("Choice before node");
                            choice = JsonUtility.FromJson<DialogueChoice>(json.ToString()); choices.Add(choice);
                        }
                        else throw new FormatException("Unknown metadata block: " + kind);
                        hasQuotedText = false;
                    }
                    catch (Exception e) { throw new FormatException($"Line {start}: {e.Message}", e); }
                }
                else if (line == ">" || line.StartsWith("> ", StringComparison.Ordinal))
                {
                    if (node == null) throw new FormatException($"Line {i+1}: dialogue before node");
                    string quoted = line.Length > 1 ? line.Substring(2) : string.Empty;
                    if (choice != null) choice.text = hasQuotedText ? choice.text + "\n" + quoted : quoted;
                    else node.text = hasQuotedText ? node.text + "\n" + quoted : quoted;
                    hasQuotedText = true;
                }
            }
            if (content == null) throw new FormatException("Missing dialogue-content header");
            if (node != null) node.choices = choices.ToArray();
            content.nodes = nodes.ToArray();
            var errors = content.Validate();
            if (errors.Count > 0) throw new FormatException(string.Join("\n", errors));
            return content;
        }

        public static string Write(DialogueContent source)
        {
            var content = JsonUtility.FromJson<DialogueContent>(JsonUtility.ToJson(source));
            var nodes = content.nodes; content.nodes = Array.Empty<DialogueNode>();
            var text = new StringBuilder("# 钴蓝 · 可编辑剧情\n\n在 Obsidian 中修改 `> ` 后的台词。每个节点与选项下方的元数据保留条件和分支；可修改 next 指向另一个节点 id。保存后回到 Unity 自动检查并导入，错误会保留原来的有效内容。\n\n");
            void Block(string kind, object value) => text.Append("```dialogue-").Append(kind).Append('\n').Append(JsonUtility.ToJson(value, true)).Append("\n```\n\n");
            void Quote(string value)
            {
                foreach (string line in (value ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
                    text.Append("> ").Append(line).Append('\n');
                text.Append('\n');
            }
            Block("content", content);
            foreach (var n in nodes)
            {
                var choices = n.choices; string line = n.text; n.text = null; n.choices = Array.Empty<DialogueChoice>();
                text.Append("## ").Append(n.id).Append("\n\n"); Block("node", n);
                Quote(line);
                foreach (var c in choices)
                {
                    line = c.text; c.text = null;
                    text.Append("### 回应 · ").Append(c.id).Append("\n\n"); Block("choice", c);
                    Quote(line);
                }
            }
            return text.ToString();
        }
    }
}
