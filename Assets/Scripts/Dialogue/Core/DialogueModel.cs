using System;
using System.Collections.Generic;

namespace Voyage.Dialogue
{
    public enum ConversationPhase { Closed, Presenting, AwaitingResponse, Resolving, Suspended }
    public enum PromiseStatus { Proposed, Active, Declined, Withdrawn, Fulfilled, Overdue, Released }
    public enum CooperationPhase { None, Active, Completed, Cancelled }

    [Serializable] public sealed class DialogueCondition
    {
        public string op, id, value;
    }
    [Serializable] public sealed class DialogueChoice
    {
        public string id, text, next, speechAct, signal;
        public double expiresAfterSeconds;
        public DialogueCondition[] conditions = Array.Empty<DialogueCondition>();
    }
    [Serializable] public sealed class DialogueNode
    {
        public string id, text, fallback, effect;
        public string silenceNext;
        public string topic, leaveNext, returnEntry, observation;
        public string[] textVariants = Array.Empty<string>();
        public double endAfterSeconds;
        public double responseSeconds;
        public DialogueCondition[] conditions = Array.Empty<DialogueCondition>();
        public DialogueChoice[] choices = Array.Empty<DialogueChoice>();
    }
    [Serializable] public sealed class DialogueContent
    {
        public int version = 1;
        public string npcId, speaker, entry;
        public DialogueNode[] nodes = Array.Empty<DialogueNode>();

        public DialogueNode Find(string id) => Array.Find(nodes, n => n.id == id);
        public List<string> Validate()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            var choiceIds = new HashSet<string>();
            foreach (var n in nodes)
            {
                if (string.IsNullOrWhiteSpace(n.id) || !ids.Add(n.id)) errors.Add("Duplicate/empty node: " + n.id);
                if (string.IsNullOrWhiteSpace(n.text)) errors.Add("Missing line: " + n.id);
                if (double.IsNaN(n.responseSeconds) || double.IsInfinity(n.responseSeconds) || n.responseSeconds < 0) errors.Add("Invalid response duration: " + n.id);
                if (n.choices == null || n.choices.Length > 5) errors.Add("Invalid response group: " + n.id);
                if (n.text?.Length > 38) errors.Add("Line too dense: " + n.id);
                foreach(var line in n.textVariants ?? Array.Empty<string>())
                    if(string.IsNullOrWhiteSpace(line) || line.Length>38) errors.Add("Invalid line variant: "+n.id);
                if (n.choices?.Length > 0 && (n.choices[0].speechAct != "silence" || n.choices[0].next != n.silenceNext)) errors.Add("First response must be contextual silence: " + n.id);
                if (!string.IsNullOrEmpty(n.fallback) && Find(n.fallback) == null) errors.Add("Missing fallback: " + n.id);
                if (!string.IsNullOrEmpty(n.silenceNext) && Find(n.silenceNext) == null) errors.Add("Missing silence successor: " + n.id);
                if (!string.IsNullOrEmpty(n.leaveNext) && (Find(n.leaveNext) == null || Find(n.leaveNext).endAfterSeconds <= 0)) errors.Add("Invalid farewell: " + n.id);
                if (!string.IsNullOrEmpty(n.returnEntry) && Find(n.returnEntry) == null) errors.Add("Missing return entry: " + n.id);
                if (double.IsNaN(n.endAfterSeconds) || double.IsInfinity(n.endAfterSeconds) || n.endAfterSeconds < 0 || n.endAfterSeconds > 0 && n.choices?.Length > 0) errors.Add("Invalid ending: " + n.id);
                ValidateConditions(n.conditions, errors);
                if (!string.IsNullOrEmpty(n.effect) && n.effect != "know_shop" && n.effect != "accept_paint" && n.effect != "release_paint" && n.effect != "recall_paint" && n.effect != "met") errors.Add("Unknown effect: " + n.effect);
                foreach (var c in n.choices ?? Array.Empty<DialogueChoice>())
                {
                    if (string.IsNullOrEmpty(c.id) || !choiceIds.Add(c.id)) errors.Add("Duplicate/empty choice: " + c.id);
                    if (string.IsNullOrEmpty(c.text)) errors.Add("Missing choice text: " + c.id);
                    if (double.IsNaN(c.expiresAfterSeconds) || double.IsInfinity(c.expiresAfterSeconds) || c.expiresAfterSeconds < 0 || c.expiresAfterSeconds > n.responseSeconds) errors.Add("Invalid choice deadline: " + c.id);
                    if (!string.IsNullOrEmpty(c.next) && Find(c.next) == null) errors.Add("Missing successor: " + c.id);
                    ValidateConditions(c.conditions, errors);
                    if (c.text?.Length > 24) errors.Add("Response too dense: " + c.id);
                    if (!string.IsNullOrEmpty(c.signal) && !ConversationRuntime.SelfSignals.Contains(c.signal)) errors.Add("Unknown self report: " + c.id);
                    if (!string.IsNullOrEmpty(c.speechAct) && c.speechAct != "silence" && c.speechAct != "ask" && c.speechAct != "offer" && c.speechAct != "promise" && c.speechAct != "listen" && c.speechAct != "decline") errors.Add("Unknown speech act: " + c.id);
                    if (c.speechAct == "promise" && (Find(c.next)?.effect != "accept_paint" || !c.text.Contains("明天") || !c.text.Contains("一管"))) errors.Add("Promise requires explicit terms and acceptance: " + c.id);
                }
            }
            if (Find(entry) == null) errors.Add("Missing entry");
            return errors;
        }
        static void ValidateConditions(DialogueCondition[] conditions, List<string> errors)
        {
            foreach (var c in conditions ?? Array.Empty<DialogueCondition>())
            {
                if (c.op != "fact" && c.op != "knows" && c.op != "memory" && c.op != "promise_absent" && c.op != "familiar") errors.Add("Unknown condition: " + c.op);
                if (c.op == "fact" && (c.id != "needs_paint" && c.id != "tent_done" || c.value != "true" && c.value != "false")) errors.Add("Invalid fact or boolean value: " + c.id);
                if ((c.op == "knows" || c.op == "memory") && string.IsNullOrWhiteSpace(c.id)) errors.Add("Missing knowledge/memory id");
            }
        }
    }

    [Serializable] public sealed class ResponseSlot
    {
        public string choiceId, text, disabledReason;
    }
    [Serializable] public sealed class ConversationState
    {
        public string id, nodeId, responseSetId, pendingChoiceId, pendingCommandId, reason;
        public int version;
        public ConversationPhase phase;
        public bool lineDelivered;
        public double responseElapsedSeconds;
        public List<ResponseSlot> slots = new List<ResponseSlot>();
        public List<string> heardNodes = new List<string>();
        public List<string> observedSignals = new List<string>();
        public int spokenReplies;
        public string npcLine;
    }
    [Serializable] public sealed class DialogueEvent
    {
        public string id, commandId, type, actor, target, encounter, source;
        public double time;
    }
    [Serializable] public sealed class DialogueMemory
    {
        public string eventId, behavior, encounter, source;
        public double time, lastRecalled = -1000;
    }
    [Serializable] public sealed class PaintPromise
    {
        public string id, acceptanceEvent, settlementEvent, reason;
        public string itemId = "blue_paint";
        public int quantity = 1, progress;
        public double deadline;
        public bool wasOverdue;
        public PromiseStatus status;
    }
    [Serializable] public sealed class DialogueSnapshot
    {
        public int schemaVersion = 1, contentVersion = 1;
        public double worldHours = 12, nextSelfSupply = 60, encounterStarted = -1000;
        public string encounterId;
        public bool needsPaint = true, met;
        public int playerPaint, npcPaint;
        public int approachCount;
        public string lastApproach;
        public string lastTopicEntry;
        public double lastTopicHours = -1000;
        public double lastApproachHours = -1000;
        public List<DialogueObservation> observations = new List<DialogueObservation>();
        public List<DialogueLineVisit> lineVisits = new List<DialogueLineVisit>();
        public string statedIntent;
        public double statedIntentHours, observationCooldownSeconds, nearbyIdleSeconds;
        public CooperationPhase cooperation;
        public double cooperationSeconds;
        public ConversationState session = new ConversationState();
        public List<string> knownFacts = new List<string>();
        public List<string> completedCommands = new List<string>();
        public List<DialogueEvent> events = new List<DialogueEvent>();
        public List<DialogueMemory> memories = new List<DialogueMemory>();
        public List<PaintPromise> promises = new List<PaintPromise>();
        public List<string> history = new List<string>();
    }
    [Serializable] public sealed class DialogueObservation
    {
        public string signal;
        public int count, noticedCount;
        public double lastTime;
    }
    [Serializable] public sealed class DialogueLineVisit
    {
        public string nodeId;
        public int count;
    }
    public struct DialogueContext
    {
        public bool npcAvailable, canHear, canHandOver, canCooperate, isStationary;
        public double distance;
        public static DialogueContext Nearby => new DialogueContext { npcAvailable = true, canHear = true, canHandOver = true, canCooperate = true, isStationary = true, distance = 2 };
    }
    public struct ResponseCommand
    {
        public string commandId, sessionId, responseSetId, choiceId;
        public int version;
    }
}
