using System.Linq;
using UnityEditor;
using UnityEngine;
using Voyage.Dialogue;

public sealed class DialoguePreviewWindow : EditorWindow
{
    ConversationRuntime preview;
    DialogueContent content;
    Vector2 scroll;
    int nodeIndex;
    bool inspectLive;
    [MenuItem("Voyage/Dialogue/Preview and Inspect")]
    static void Open() => GetWindow<DialoguePreviewWindow>("Dialogue Preview");
    void OnEnable()
    {
        var asset=Resources.Load<TextAsset>("Dialogue/CoBlues");
        if(asset!=null) { content=JsonUtility.FromJson<DialogueContent>(asset.text);preview=new ConversationRuntime(content); }
    }
    void OnGUI()
    {
        if(content==null) { EditorGUILayout.HelpBox("CoBlues content missing",MessageType.Error);return; }
        inspectLive=EditorGUILayout.Toggle("Inspect live scene (read only)",inspectLive);
        var actor=Application.isPlaying?Object.FindAnyObjectByType<CoBluesDialogue>():null;
        var runtime=inspectLive?actor?.Runtime:preview;
        if(runtime==null) { EditorGUILayout.HelpBox("Enter Play Mode to inspect the live NPC.",MessageType.Info);return; }
        scroll=EditorGUILayout.BeginScrollView(scroll);
        if(!inspectLive)
        {
            nodeIndex=EditorGUILayout.Popup("Authored node",nodeIndex,content.nodes.Select(n=>n.id).ToArray());
            if(GUILayout.Button("Preview node")) { preview.Close("Preview");preview.Begin(DialogueContext.Nearby,content.nodes[nodeIndex].id);preview.DeliverNpcLine(DialogueContext.Nearby); }
            if(GUILayout.Button("New world"))preview=new ConversationRuntime(content);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Collect paint"))preview.CollectPaint(System.Guid.NewGuid().ToString("N"));
            if(GUILayout.Button("Give paint"))preview.GivePaint(System.Guid.NewGuid().ToString("N"),DialogueContext.Nearby);
            if(GUILayout.Button("Advance day"))preview.Tick(24,0,DialogueContext.Nearby);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.LabelField("Session",runtime.Session.id??"none");
        EditorGUILayout.LabelField("Phase / focus",runtime.Session.phase+" / "+runtime.OwnsFocus);
        EditorGUILayout.LabelField("Node",runtime.Session.nodeId??"none");
        EditorGUILayout.LabelField("Response set / version",runtime.Session.responseSetId+" / "+runtime.Session.version);
        EditorGUILayout.LabelField("World hours / encounter",runtime.State.worldHours.ToString("F2")+" / "+runtime.State.encounterId);
        EditorGUILayout.HelpBox((runtime.Speaker??"")+"："+(runtime.CurrentLine??"Select a preview node."),MessageType.None);
        for(int i=0;i<runtime.Session.slots.Count;i++)
        {
            var slot=runtime.Session.slots[i];
            EditorGUILayout.LabelField((i+1)+" / "+slot.choiceId,EditorStyles.boldLabel);
            EditorGUILayout.LabelField(slot.text,EditorStyles.wordWrappedLabel);
            if(!string.IsNullOrEmpty(slot.disabledReason))EditorGUILayout.HelpBox(slot.disabledReason,MessageType.Warning);
            if(!inspectLive && GUILayout.Button("Choose / deliver"))
            {
                if(preview.Select(preview.CommandForSlot(i),DialogueContext.Nearby,true))
                { preview.DeliverPlayerSpeech(DialogueContext.Nearby);preview.DeliverNpcLine(DialogueContext.Nearby); }
                GUIUtility.ExitGUI();
            }
        }
        EditorGUILayout.LabelField("Known facts",string.Join(", ",runtime.State.knownFacts));
        EditorGUILayout.LabelField("Player stated intent",runtime.State.statedIntent??"none");
        EditorGUILayout.LabelField("Observation candidate",runtime.ObservationEntry()??"none");
        foreach(var observation in runtime.State.observations)
            EditorGUILayout.LabelField("Observed "+observation.signal,observation.count+" / already noticed "+observation.noticedCount);
        foreach(var promise in runtime.State.promises)EditorGUILayout.LabelField("Promise "+promise.status,promise.progress+"/"+promise.quantity+" due "+promise.deadline+" reason "+promise.reason);
        foreach(var memory in runtime.State.memories)EditorGUILayout.LabelField("Memory "+memory.behavior,memory.source+" @ "+memory.time.ToString("F2"));
        EditorGUILayout.Space();EditorGUILayout.LabelField("Recent event delivery trace",EditorStyles.boldLabel);
        foreach(var entry in runtime.State.events.Skip(System.Math.Max(0,runtime.State.events.Count-20)))
            EditorGUILayout.LabelField(entry.type,entry.source+" / "+entry.commandId);
        EditorGUILayout.EndScrollView();
        if(inspectLive)Repaint();
    }
}
