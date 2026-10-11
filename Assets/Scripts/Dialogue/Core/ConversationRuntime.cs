using System;
using System.Linq;

namespace Voyage.Dialogue
{
    // Engine-independent single-threaded command/semantic delivery boundary.
    // The host supplies perception afresh for every delivery, never author intent.
    public sealed class ConversationRuntime
    {
        public const double StartDistance = 10;
        public const double ConversationDistance = 20;
        public const double DepartureGraceSeconds = 3;
        public readonly DialogueContent Content;
        public DialogueSnapshot State { get; private set; }
        public ConversationState Session => State.session;
        public bool OwnsFocus => Session.phase != ConversationPhase.Closed && Session.phase != ConversationPhase.Suspended;
        public double ResponseDuration => Content.Find(Session.nodeId)?.responseSeconds ?? 0;
        public double ChoiceDeadline(string id) => ResponseDuration;
        public static readonly System.Collections.Generic.HashSet<string> SelfSignals = new System.Collections.Generic.HashSet<string>
            { "self.quiet", "self.chat", "self.bored", "self.rest", "self.passing", "self.thinking", "self.clear" };
        static readonly (string signal, int threshold)[] ObservationRules = {
            ("self.quiet",1), ("self.chat",1), ("self.bored",1), ("self.rest",1), ("self.passing",1), ("self.thinking",1),
            ("help",2), ("quiet",2), ("art",3), ("practical",3), ("care",2), ("question",4), ("speech",6),
            ("return",3), ("company",1), ("hasty",2)
        };
        public string ObservationEntry()
        {
            if (!State.met || State.observationCooldownSeconds > 0) return null;
            foreach (var rule in ObservationRules)
            {
                var evidence = State.observations.Find(o=>o.signal==rule.signal);
                if (evidence == null || State.worldHours-evidence.lastTime>24 || evidence.count-evidence.noticedCount<rule.threshold) continue;
                if (rule.signal=="help" && (!RemembersPaint || State.cooperation!=CooperationPhase.Completed)) continue;
                if (rule.signal.StartsWith("self.") && rule.signal != State.statedIntent) continue;
                if (State.worldHours-State.statedIntentHours<24 &&
                    (State.statedIntent=="self.quiet" || State.statedIntent=="self.rest" || State.statedIntent=="self.thinking") &&
                    (rule.signal=="company" || rule.signal=="speech" || rule.signal=="question" || rule.signal=="return")) continue;
                if (State.worldHours-State.statedIntentHours<24 &&
                    (rule.signal=="quiet" && (State.statedIntent=="self.chat" || State.statedIntent=="self.thinking") ||
                     (rule.signal=="speech" || rule.signal=="return") && (State.statedIntent=="self.quiet" || State.statedIntent=="self.passing"))) continue;
                string entry="observe."+rule.signal;
                if (Content.Find(entry)!=null) return entry;
            }
            return null;
        }
        public bool CanNoticeWhileIdle => ObservationEntry()!=null && !ObservationEntry().StartsWith("observe.self.");
        void Observe(string signal, bool oncePerConversation = false)
        {
            if (oncePerConversation && Session.observedSignals.Contains(signal)) return;
            if (oncePerConversation) Session.observedSignals.Add(signal);
            var evidence=State.observations.Find(o=>o.signal==signal);
            if (evidence==null) { evidence=new DialogueObservation{signal=signal};State.observations.Add(evidence); }
            if (State.worldHours-evidence.lastTime>24) { evidence.count=0;evidence.noticedCount=0; }
            evidence.count++;evidence.lastTime=State.worldHours;
        }
        public string CurrentLine => Session.phase == ConversationPhase.Resolving
            ? Choice(Session.pendingChoiceId)?.text : Session.npcLine ?? Content.Find(Session.nodeId)?.text;
        public string Speaker => Session.phase == ConversationPhase.Resolving ? "你" : Content.speaker;
        public PaintPromise ActivePromise => State.promises.LastOrDefault(p => p.status == PromiseStatus.Active || p.status == PromiseStatus.Overdue);
        public int FamiliarEncounters => State.memories.Select(m => m.encounter).Distinct().Count();
        public bool RemembersPaint => State.memories.Any(m => m.behavior == "gave_blue_paint");
        public bool RecallReady => State.memories.Any(m => m.behavior == "gave_blue_paint" && State.worldHours - m.time >= 24 && State.worldHours - m.lastRecalled >= 24);
        static string Id() => Guid.NewGuid().ToString("N");
        double outsideSeconds;
        public string ApproachEntry()
        {
            if (!State.met) return Content.entry;
            if (ActivePromise?.status == PromiseStatus.Overdue) return "greet.overdue";
            if (State.playerPaint > 0 && State.needsPaint) return "greet.paint";
            if (RecallReady) return "paint.recall";
            if (ActivePromise != null) return "greet.promise";
            string observation=ObservationEntry();
            if (observation!=null) return observation;
            if (State.cooperation == CooperationPhase.Completed && State.lastApproach != "greet.tent") return "greet.tent";
            double hour = State.worldHours % 24;
            if (hour >= 20 || hour < 5) return State.lastApproach == "greet.night" ? "greet.lamp" : "greet.night";
            if (hour < 9) return State.lastApproach == "greet.morning" ? "greet.breakfast" : "greet.morning";
            if (State.worldHours - State.lastApproachHours < 1)
            {
                var previous = Content.Find(State.lastTopicEntry);
                if (previous != null && State.worldHours - State.lastTopicHours < 1 && Check(previous.conditions)) return previous.id;
                return "topics";
            }
            string[] everyday = { "greet.water", "greet.notebook", "greet.fish" };
            return everyday[Math.Abs(State.approachCount % everyday.Length)];
        }
        public bool BeginNearby(DialogueContext context)
        {
            bool resuming = Session.phase == ConversationPhase.Suspended;
            string entry = ApproachEntry();
            if (!Begin(context,entry)) return false;
            if (!resuming)
            {
                if (State.met && State.worldHours-State.lastApproachHours<1) Observe("return");
                State.approachCount++; State.lastApproach = entry; State.lastApproachHours = State.worldHours;
            }
            return true;
        }
        public void EndNaturally(bool walkedAway = false)
        {
            if (!OwnsFocus) return;
            if(walkedAway && Session.lineDelivered && Session.spokenReplies==0 && Session.responseElapsedSeconds<8) Observe("hasty",true);
            WithdrawProposals();
            OpenNode(Content.Find(Session.nodeId)?.leaveNext ?? "bye.neutral");
        }

        public ConversationRuntime(DialogueContent content, DialogueSnapshot state = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            var errors = content.Validate();
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            State = state ?? new DialogueSnapshot { contentVersion = content.version };
            if (State.schemaVersion != 1 || State.contentVersion != content.version)
                throw new InvalidOperationException("对话存档版本不兼容，已保留原文件；需要显式迁移。");
            if (State.session == null || State.memories == null || State.promises == null || State.completedCommands == null || State.knownFacts == null || State.history == null || State.events == null)
                throw new InvalidOperationException("对话存档结构不完整，已保留原文件。");
            if (State.playerPaint < 0 || State.npcPaint < 0 || double.IsNaN(State.worldHours) || double.IsInfinity(State.worldHours))
                throw new InvalidOperationException("对话存档数据无效，已保留原文件。");
            if (Session.responseElapsedSeconds < 0 || double.IsNaN(Session.responseElapsedSeconds) || double.IsInfinity(Session.responseElapsedSeconds))
                throw new InvalidOperationException("回应计时数据无效。");
            if (Session.heardNodes == null) Session.heardNodes = new System.Collections.Generic.List<string>();
            if (Session.observedSignals == null) Session.observedSignals = new System.Collections.Generic.List<string>();
            if (State.observations == null) State.observations = new System.Collections.Generic.List<DialogueObservation>();
            if (State.lineVisits == null) State.lineVisits = new System.Collections.Generic.List<DialogueLineVisit>();
            // A pending speech is never replayed on restore; an unaccepted proposal is withdrawn.
            if (Session.phase == ConversationPhase.Resolving) Close("读档后请重新搭话。");
            else if (Session.phase != ConversationPhase.Closed)
            {
                if (Content.Find(Session.nodeId) == null) Close("原话题已更新，请重新搭话。");
                else Suspend("读档后重新接话");
            }
            if (State.cooperation == CooperationPhase.Active) CancelCooperation();
        }

        public bool Begin(DialogueContext context, string entry = null)
        {
            double range=Session.phase==ConversationPhase.Suspended?ConversationDistance:StartDistance;
            if (!context.npcAvailable || !context.canHear || context.distance > range || OwnsFocus) return false;
            if (Session.phase == ConversationPhase.Suspended && Content.Find(Session.nodeId) != null)
            {
                var node = Content.Find(Session.nodeId);
                double elapsed = Session.responseElapsedSeconds;
                string interruptedLine=Session.npcLine;
                OpenNode(Check(node.conditions) ? node.id : node.fallback ?? Content.entry);
                if (Session.nodeId == node.id) { Session.responseElapsedSeconds = elapsed;Session.npcLine=interruptedLine??Session.npcLine; }
                outsideSeconds = 0;
                return true;
            }
            if (string.IsNullOrEmpty(State.encounterId) || State.worldHours - State.encounterStarted >= 6)
            {
                State.encounterId = Id(); State.encounterStarted = State.worldHours;
            }
            State.session = new ConversationState { id = Id() };
            OpenNode(entry ?? (!State.met ? Content.entry : RecallReady ? "paint.recall" : "topics"));
            outsideSeconds = 0;
            return true;
        }

        void OpenNode(string id)
        {
            if (string.IsNullOrEmpty(id)) { Close("告别"); return; }
            var node = Content.Find(id);
            if (node == null) { Close("话题不可用"); return; }
            if (!Check(node.conditions))
            {
                node = Content.Find(node.fallback);
                if (node == null || !Check(node.conditions)) { Close("情境已改变"); return; }
            }
            Session.nodeId = node.id; Session.phase = ConversationPhase.Presenting;
            int visit=State.lineVisits.Find(v=>v.nodeId==node.id)?.count??0;
            int variantCount=node.textVariants?.Length??0;
            int variant=visit%(variantCount+1);
            Session.npcLine=variant==0?node.text:node.textVariants[variant-1];
            Session.responseElapsedSeconds = 0;
            Session.lineDelivered = false; Session.pendingChoiceId = null; Session.pendingCommandId = null;
            Session.slots.Clear(); Session.responseSetId = Id(); Session.version++;
        }

        public bool DeliverNpcLine(DialogueContext context)
        {
            if (Session.phase != ConversationPhase.Presenting || Session.lineDelivered) return false;
            if (!context.npcAvailable || !context.canHear) { Suspend("听不清了，靠近后重新接话。"); return false; }
            var node = Content.Find(Session.nodeId);
            if (!Check(node.conditions)) { OpenNode(node.fallback); return false; }
            string eventId = Record("NpcResponseDelivered", Session.responseSetId);
            AddHistory(Content.speaker + "：" + CurrentLine);
            Session.lineDelivered = true;
            if(!Session.heardNodes.Contains(node.id))
            {
                var visit=State.lineVisits.Find(v=>v.nodeId==node.id);
                if(visit==null) { visit=new DialogueLineVisit{nodeId=node.id};State.lineVisits.Add(visit); }
                visit.count++;
            }
            if (!string.IsNullOrEmpty(node.observation))
            {
                var evidence=State.observations.Find(o=>o.signal==node.observation);
                if(evidence!=null) evidence.noticedCount=evidence.count;
                State.observationCooldownSeconds=90;
            }
            if (!Session.heardNodes.Contains(node.id)) Session.heardNodes.Add(node.id);
            if (!string.IsNullOrEmpty(node.returnEntry))
            { State.lastTopicEntry = node.returnEntry; State.lastTopicHours = State.worldHours; }
            switch (node.effect)
            {
                case "met": State.met = true; break;
                case "know_shop": if (!State.knownFacts.Contains("paint_shop")) State.knownFacts.Add("paint_shop"); break;
                case "accept_paint":
                    var proposed = State.promises.LastOrDefault(p => p.status == PromiseStatus.Proposed);
                    if (proposed != null && State.needsPaint)
                    {
                        proposed.status = PromiseStatus.Active; proposed.acceptanceEvent = eventId;
                        proposed.deadline = (Math.Floor(State.worldHours / 24) + 2) * 24;
                        AddHistory("已约好：明天结束前带给钴蓝蓝色颜料 ×1。");
                    }
                    break;
                case "release_paint": ReleasePromise(eventId, "AgreedRelease"); break;
                case "recall_paint":
                    foreach (var memory in State.memories.Where(m => m.behavior == "gave_blue_paint")) memory.lastRecalled = State.worldHours;
                    break;
            }
            foreach (var choice in node.choices)
                if (Check(choice.conditions) && (choice.speechAct=="silence" || !(Session.heardNodes.Contains(choice.next) && Content.Find(choice.next)?.endAfterSeconds <= 0)))
                    Session.slots.Add(new ResponseSlot { choiceId = choice.id, text = choice.text });
            Session.phase = ConversationPhase.AwaitingResponse;
            UpdateResponseClock(0);
            return true;
        }

        public ResponseCommand CommandForSlot(int index, string commandId = null)
        {
            return new ResponseCommand { commandId = commandId ?? Id(), sessionId = Session.id,
                responseSetId = Session.responseSetId, version = Session.version,
                choiceId = index >= 0 && index < Session.slots.Count ? Session.slots[index].choiceId : null };
        }
        public bool Select(ResponseCommand command, DialogueContext context, bool releaseBarrierPassed)
        {
            if (!releaseBarrierPassed || string.IsNullOrEmpty(command.commandId) || State.completedCommands.Contains(command.commandId) || Session.phase != ConversationPhase.AwaitingResponse ||
                command.sessionId != Session.id || command.responseSetId != Session.responseSetId || command.version != Session.version) return false;
            var slot = Session.slots.Find(s => s.choiceId == command.choiceId);
            var choice = Choice(command.choiceId);
            if (slot == null || choice == null || !string.IsNullOrEmpty(slot.disabledReason) || !context.npcAvailable || !context.canHear) return false;
            if (!Check(choice.conditions)) { slot.disabledReason = "情境已改变，这句话现在不合适。"; return false; }
            if (choice.speechAct == "silence")
            {
                // The default is a neutral resting selection, not a submitted response.
                return false;
            }
            Session.phase = ConversationPhase.Resolving;
            Session.pendingChoiceId = choice.id; Session.pendingCommandId = command.commandId;
            State.completedCommands.Add(command.commandId);
            Record("ChoiceAccepted", command.commandId);
            return true;
        }
        public bool DeliverPlayerSpeech(DialogueContext context)
        {
            if (Session.phase != ConversationPhase.Resolving) return false;
            if (!context.npcAvailable || !context.canHear) { Suspend("话没说完，靠近后重新接话。"); return false; }
            var choice = Choice(Session.pendingChoiceId);
            if (choice == null || !Check(choice.conditions)) { Close("情境已改变"); return false; }
            var eventId = Record("UtteranceDelivered", Session.pendingCommandId);
            AddHistory("你：" + choice.text);
            Record("UtterancePerceived", Session.pendingCommandId);
            Session.spokenReplies++;Observe("speech");
            // A fresh spoken response outweighs an unspoken guess based on older silence.
            var quietEvidence=State.observations.Find(o=>o.signal=="quiet");
            if(quietEvidence!=null) quietEvidence.noticedCount=quietEvidence.count;
            if(choice.text.Contains("？"))
            {
                Observe("question");
                string topic=Content.Find(choice.next)?.topic;
                if(topic=="painting") Observe("art");
                if(topic=="paint" || topic=="tent" || topic=="fish" || topic=="notebook") Observe("practical");
            }
            if(choice.id=="morning.water" || choice.id=="night.sleep" || choice.id=="breakfast.big") Observe("care",true);
            if(!string.IsNullOrEmpty(choice.signal))
            { State.statedIntent=choice.signal;State.statedIntentHours=State.worldHours;Observe(choice.signal,true);State.observationCooldownSeconds=90; }
            switch (choice.speechAct)
            {
                case "offer": Remember(eventId, "offered_to_look", true); break;
                case "ask": Remember(eventId, "asked_about_paint", true); break;
                case "listen": Remember(eventId, "chose_to_listen", true); break;
                case "promise":
                    if (ActivePromise == null && !State.promises.Any(p => p.status == PromiseStatus.Proposed))
                        State.promises.Add(new PaintPromise { id = eventId, status = PromiseStatus.Proposed });
                    break;
            }
            OpenNode(choice.next);
            return true;
        }
        DialogueChoice Choice(string id) => Content.nodes.SelectMany(n => n.choices).FirstOrDefault(c => c.id == id);

        public bool Check(DialogueCondition[] conditions)
        {
            foreach (var c in conditions ?? Array.Empty<DialogueCondition>())
            {
                bool result;
                switch (c.op)
                {
                    case "fact": result = c.id == "needs_paint" ? State.needsPaint : c.id == "tent_done" && State.cooperation == CooperationPhase.Completed; result = result == (c.value == "true"); break;
                    case "knows": result = State.knownFacts.Contains(c.id); break;
                    case "memory": result = State.memories.Any(m => m.behavior == c.id); break;
                    case "promise_absent": result = ActivePromise == null && !State.promises.Any(p => p.status == PromiseStatus.Proposed); break;
                    case "familiar": result = FamiliarEncounters >= 2; break;
                    default: return false;
                }
                if (!result) return false;
            }
            return true;
        }

        public void RevalidateSlots()
        {
            foreach (var slot in Session.slots)
                if (!Check(Choice(slot.choiceId)?.conditions)) slot.disabledReason = "情境已改变，请换个话题或重新搭话。";
        }
        public void Tick(double worldHoursDelta, double interactionSeconds, DialogueContext context)
        {
            State.worldHours += Math.Max(0, worldHoursDelta);
            State.observationCooldownSeconds=Math.Max(0,State.observationCooldownSeconds-Math.Max(0,interactionSeconds));
            if (State.met && !OwnsFocus && Session.phase==ConversationPhase.Closed && context.npcAvailable && context.canHear && context.distance<=3 && context.isStationary)
            {
                State.nearbyIdleSeconds+=Math.Max(0,interactionSeconds);
                if (State.nearbyIdleSeconds>=45) { Observe("company",true);State.nearbyIdleSeconds=0; }
            }
            else State.nearbyIdleSeconds=0;
            if (ActivePromise != null && ActivePromise.deadline < State.worldHours)
            { ActivePromise.status = PromiseStatus.Overdue; ActivePromise.wasOverdue = true; }
            if (State.needsPaint && State.worldHours >= State.nextSelfSupply) ResolvePaintNeed();
            if (OwnsFocus)
            {
                if (!context.npcAvailable) Close("钴蓝已离开。");
                else if (context.distance > ConversationDistance || !context.canHear)
                {
                    outsideSeconds += Math.Max(0, interactionSeconds);
                    if (outsideSeconds >= DepartureGraceSeconds)
                    {
                        if(Session.lineDelivered && Session.spokenReplies==0 && Session.responseElapsedSeconds<8) Observe("hasty",true);
                        Suspend("走远了，靠近后可接着聊。");
                    }
                }
                else outsideSeconds = 0;
                RevalidateSlots();
                if (context.canHear && context.distance <= ConversationDistance) UpdateResponseClock(interactionSeconds);
            }
            if (State.cooperation == CooperationPhase.Active)
            {
                if (!context.canHandOver || !context.canCooperate || !context.npcAvailable) CancelCooperation();
                else
                {
                    State.cooperationSeconds += Math.Max(0, interactionSeconds);
                    if (State.cooperationSeconds >= 5)
                    {
                        State.cooperation = CooperationPhase.Completed;
                        var id = Record("CooperationCompleted", Id()); Remember(id, "held_tent", false);
                        Observe("help");
                        if (OwnsFocus) { WithdrawProposals(); OpenNode("tent.done"); }
                        else AddHistory(Content.speaker + "：" + Content.Find("tent.done").text);
                    }
                }
            }
        }
        void UpdateResponseClock(double seconds)
        {
            if (Session.phase != ConversationPhase.AwaitingResponse) return;
            var node = Content.Find(Session.nodeId);
            if (Session.slots.Count == 0 && node.choices.Length > 0)
            {
                Session.responseElapsedSeconds += Math.Max(0,seconds);
                if (Session.responseElapsedSeconds >= 4) EndNaturally();
                return;
            }
            if (node.endAfterSeconds > 0)
            {
                Session.responseElapsedSeconds += Math.Max(0,seconds);
                if (Session.responseElapsedSeconds >= node.endAfterSeconds) Close("自然结束");
                return;
            }
            if (ResponseDuration <= 0) return;
            Session.responseElapsedSeconds += Math.Max(0, seconds);
            if (Session.responseElapsedSeconds >= ResponseDuration)
            {
                Record("ResponseWindowElapsed",Session.responseSetId);
                Observe("quiet",true);
                if (!string.IsNullOrEmpty(node.silenceNext)) OpenNode(node.silenceNext);
                else Close("没有接话");
            }
        }
        public void ResolvePaintNeed()
        {
            if (!State.needsPaint) return;
            State.needsPaint = false; State.npcPaint++;
            var id = Record("NeedResolved", Id()); ReleasePromise(id, "NeedResolved");
            RevalidateSlots();
        }
        public bool GivePaint(string commandId, DialogueContext context)
        {
            if (string.IsNullOrEmpty(commandId) || State.completedCommands.Contains(commandId) || !context.npcAvailable || !context.canHandOver || State.playerPaint < 1) return false;
            // All eligibility checks precede the synchronous write set. The host only saves between calls.
            State.playerPaint--; State.npcPaint++; State.needsPaint = false;
            State.completedCommands.Add(commandId);
            var id = Record("ItemTransferred", commandId);
            Remember(id, "gave_blue_paint", false);
            Observe("help");
            var promise = ActivePromise;
            if (promise != null) { promise.progress = 1; promise.status = PromiseStatus.Fulfilled; promise.settlementEvent = id; }
            RevalidateSlots();
            // A real handoff replaces obsolete responses about looking for paint.
            if (OwnsFocus) { WithdrawProposals(); OpenNode("paint.thanks"); }
            return true;
        }
        public bool CollectPaint(string commandId)
        {
            if (string.IsNullOrEmpty(commandId) || State.completedCommands.Contains(commandId)) return false;
            State.completedCommands.Add(commandId); State.playerPaint++;
            Record("ItemCollected", commandId); return true;
        }
        public bool StartCooperation(DialogueContext context)
        {
            if (!context.npcAvailable || !context.canHandOver || !context.canCooperate || State.cooperation == CooperationPhase.Completed || State.cooperation == CooperationPhase.Active) return false;
            State.cooperation = CooperationPhase.Active; State.cooperationSeconds = 0;
            Record("CooperationStarted", Id()); return true;
        }
        public void CancelCooperation() { if (State.cooperation == CooperationPhase.Active) { State.cooperation = CooperationPhase.Cancelled; Record("CooperationCancelled", Id()); } }
        public void Suspend(string reason)
        {
            if (Content.Find(Session.nodeId)?.effect == "accept_paint")
            { Session.nodeId = Session.lineDelivered ? "topics" : "paint.offer";Session.npcLine=null; }
            WithdrawProposals(); Session.phase = ConversationPhase.Suspended; Session.reason = reason;
            Session.pendingChoiceId = null; Session.pendingCommandId = null;
        }
        public void Close(string reason)
        {
            WithdrawProposals(); Session.phase = ConversationPhase.Closed; Session.reason = reason;
            Session.pendingChoiceId = null; Session.pendingCommandId = null; Session.slots.Clear(); outsideSeconds = 0;
        }
        void WithdrawProposals() { foreach (var p in State.promises.Where(p => p.status == PromiseStatus.Proposed)) p.status = PromiseStatus.Withdrawn; }
        void ReleasePromise(string eventId, string reason)
        {
            var promise = ActivePromise;
            if (promise == null) return;
            promise.status = PromiseStatus.Released; promise.reason = reason; promise.settlementEvent = eventId;
        }
        string Record(string type, string commandId)
        {
            string id = Id();
            State.events.Add(new DialogueEvent { id = id, commandId = commandId, type = type, actor = type.StartsWith("Npc") ? Content.npcId : "player", target = Content.npcId,
                encounter = State.encounterId, source = "DirectParticipant", time = State.worldHours });
            if (State.events.Count > 160) State.events.RemoveAt(0);
            return id;
        }
        void Remember(string eventId, string behavior, bool weak)
        {
            if (State.memories.Any(m => m.eventId == eventId || (weak && m.encounter == State.encounterId && m.behavior == behavior))) return;
            State.memories.Add(new DialogueMemory { eventId = eventId, behavior = behavior, encounter = State.encounterId, source = "DirectParticipant", time = State.worldHours });
        }
        public void AddHistory(string text)
        {
            State.history.Add(text);
            if (State.history.Count > 80) State.history.RemoveAt(0);
        }
    }
}
