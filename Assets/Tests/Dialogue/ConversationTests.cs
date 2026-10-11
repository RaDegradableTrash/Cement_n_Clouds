using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Voyage.Dialogue.Tests
{
    public sealed class ConversationTests
    {
        ConversationRuntime runtime;
        DialogueContext near;
        [SetUp] public void Setup()
        {
            runtime = new ConversationRuntime(JsonUtility.FromJson<DialogueContent>(Resources.Load<TextAsset>("Dialogue/CoBlues").text));
            near = DialogueContext.Nearby;
        }
        void Start(string node) { runtime.Close("test"); Assert.That(runtime.Begin(near,node),Is.True); Assert.That(runtime.DeliverNpcLine(near),Is.True); }
        void Say(string id)
        {
            int slot = runtime.Session.slots.FindIndex(s=>s.choiceId==id);
            Assert.That(slot,Is.GreaterThanOrEqualTo(0));
            Assert.That(runtime.Select(runtime.CommandForSlot(slot),near,true),Is.True);
            if(runtime.Session.phase==ConversationPhase.Resolving)Assert.That(runtime.DeliverPlayerSpeech(near),Is.True);
            if(runtime.Session.phase==ConversationPhase.Presenting)Assert.That(runtime.DeliverNpcLine(near),Is.True);
        }
        ConversationRuntime RoundTrip() => new ConversationRuntime(runtime.Content,JsonUtility.FromJson<DialogueSnapshot>(JsonUtility.ToJson(runtime.State)));
        [Test] public void ContentIsValidAndAllBranchesHaveStableIds() { Assert.That(runtime.Content.Validate(),Is.Empty); Assert.That(runtime.Content.nodes.Length,Is.GreaterThanOrEqualTo(15)); }
        [Test] public void MalformedConditionCannotUnlockContent()
        {
            runtime.Content.nodes[0].conditions=new[]{new DialogueCondition{op="fact",id="misspelled",value="false"}};
            Assert.That(runtime.Content.Validate(),Is.Not.Empty);Assert.Throws<ArgumentException>(()=>new ConversationRuntime(runtime.Content));
        }
        [Test] public void OfferDoesNotBecomePromise() { Start("paint");Say("paint.offer");Assert.That(runtime.State.promises,Is.Empty);Assert.That(runtime.State.memories.Single().behavior,Is.EqualTo("offered_to_look")); }
        [Test] public void PromiseNeedsBothSemanticDeliveries()
        {
            Start("paint.offer");var cmd=runtime.CommandForSlot(1);
            Assert.That(runtime.Select(cmd,near,true),Is.True);Assert.That(runtime.State.promises,Is.Empty);
            runtime.DeliverPlayerSpeech(near);Assert.That(runtime.State.promises.Single().status,Is.EqualTo(PromiseStatus.Proposed));
            runtime.DeliverNpcLine(near);Assert.That(runtime.ActivePromise.status,Is.EqualTo(PromiseStatus.Active));
            Assert.That(runtime.ActivePromise.deadline,Is.EqualTo(48));
        }
        [Test] public void SpeechLostBeforePerceptionDoesNotCreatePromise()
        {
            Start("paint.offer");runtime.Select(runtime.CommandForSlot(1),near,true);
            var far=near;far.canHear=false;
            Assert.That(runtime.DeliverPlayerSpeech(far),Is.False);Assert.That(runtime.State.promises,Is.Empty);
        }
        [Test] public void NotHearingShopDoesNotUnlockKnowledge()
        {
            Start("paint");runtime.Select(runtime.CommandForSlot(2),near,true);runtime.DeliverPlayerSpeech(near);
            var far=near;far.canHear=false;runtime.DeliverNpcLine(far);
            Assert.That(runtime.State.knownFacts,Is.Empty);
        }
        [Test] public void InvalidatedChoiceKeepsItsSlotAndCannotCommit()
        {
            Start("paint.offer");var command=runtime.CommandForSlot(1);string id=runtime.Session.slots[1].choiceId;
            runtime.ResolvePaintNeed();Assert.That(runtime.Select(command,near,true),Is.False);
            Assert.That(runtime.Session.slots[1].choiceId,Is.EqualTo(id));Assert.That(runtime.Session.slots[1].disabledReason,Is.Not.Empty);
        }
        [Test] public void ReleaseBarrierAndResponseVersionRejectOldInputs()
        {
            Start("paint");var command=runtime.CommandForSlot(2);
            Assert.That(runtime.Select(command,near,false),Is.False);
            Assert.That(runtime.Select(command,near,true),Is.True);
            Assert.That(runtime.Select(command,near,true),Is.False);
            runtime.DeliverPlayerSpeech(near);runtime.DeliverNpcLine(near);
            command.commandId="new-input";Assert.That(runtime.Select(command,near,true),Is.False);
        }
        [Test] public void TopicSelectionHasNoTimeoutOrNegativeEvidence()
        {
            Start("topics");string group=runtime.Session.responseSetId;
            runtime.Tick(0,120,near);Assert.That(runtime.Session.responseSetId,Is.EqualTo(group));
            Assert.That(runtime.State.memories,Is.Empty);Assert.That(runtime.Session.phase,Is.EqualTo(ConversationPhase.AwaitingResponse));
        }
        [Test] public void FullResponseSetLastsThirtySixSecondsWithoutStageExpiry()
        {
            Start("first");var ids=runtime.Session.slots.Select(s=>s.choiceId).ToArray();
            foreach(var seconds in new[]{6,6,6,17})
            {
                runtime.Tick(0,seconds,near);
                Assert.That(runtime.Session.slots.Select(s=>s.choiceId),Is.EqualTo(ids));
                Assert.That(runtime.Session.slots.All(s=>string.IsNullOrEmpty(s.disabledReason)),Is.True);
            }
            Assert.That(runtime.Session.responseElapsedSeconds,Is.EqualTo(35));
            runtime.Tick(0,1,near);Assert.That(runtime.Session.nodeId,Is.EqualTo("silence.painting"));
            Assert.That(runtime.State.observations.Single(o=>o.signal=="quiet").count,Is.EqualTo(1));
            runtime.DeliverNpcLine(near);runtime.Tick(0,4,near);
            Assert.That(runtime.OwnsFocus,Is.False);Assert.That(runtime.State.promises,Is.Empty);
        }
        [Test] public void EveryResponseGroupDefaultsToSilentAndHasShortLines()
        {
            foreach(var node in runtime.Content.nodes)
            {
                Assert.That(node.text.Length,Is.LessThanOrEqualTo(38),node.id);
                if(node.choices.Length==0) continue;
                Assert.That(node.choices[0].speechAct,Is.EqualTo("silence"),node.id);
                Assert.That(node.choices[0].next,Is.EqualTo(node.silenceNext),node.id);
                Assert.That(node.choices.All(c=>c.text.Length<=24 && c.expiresAfterSeconds==0),Is.True,node.id);
                Assert.That(node.responseSeconds,Is.EqualTo(node.id=="topics"?0:36),node.id);
            }
        }
        [Test] public void DefaultReleaseDoesNotSubmitOrResetTheResponseWindow()
        {
            Start("paint.offer");var cmd=runtime.CommandForSlot(0);string group=runtime.Session.responseSetId;
            runtime.Tick(0,7,near);
            Assert.That(runtime.Select(cmd,near,true),Is.False);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("paint.offer"));
            Assert.That(runtime.Session.phase,Is.EqualTo(ConversationPhase.AwaitingResponse));
            Assert.That(runtime.Session.responseSetId,Is.EqualTo(group));
            Assert.That(runtime.Session.responseElapsedSeconds,Is.EqualTo(7));
            Assert.That(runtime.State.history.Any(s=>s.StartsWith("你：")),Is.False);
            Assert.That(runtime.State.events.Any(e=>e.type=="UtteranceDelivered"),Is.False);
            Assert.That(runtime.State.promises,Is.Empty);
            Assert.That(runtime.State.observations.Any(o=>o.signal=="quiet"),Is.False);
            runtime.Tick(0,3,near);Assert.That(runtime.Select(cmd,near,true),Is.False);
            Assert.That(runtime.Session.responseElapsedSeconds,Is.EqualTo(10));
            Say("offer.promise");Assert.That(runtime.ActivePromise,Is.Not.Null);
        }
        [Test] public void SpokenReplyCanStillBeSentAtThirtyFiveSeconds()
        {
            Start("first");runtime.Tick(0,35,near);Say("first.money");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("money"));
        }
        [Test] public void RepeatedUnansweredWindowsProduceTentativeQuietObservation()
        {
            Start("first");runtime.Tick(0,36,near);runtime.Close("leave");
            Assert.That(runtime.ObservationEntry(),Is.Null);
            Start("first");runtime.Tick(0,36,near);runtime.Close("leave");
            Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe.quiet"));
            runtime.BeginNearby(near);runtime.DeliverNpcLine(near);Say("quiet.think");
            Assert.That(runtime.State.statedIntent,Is.EqualTo("self.thinking"));
            Assert.That(runtime.Session.nodeId,Is.EqualTo("company.thinking"));
        }
        [Test] public void ObservationRequiresPerceivedSpeechAndSurvivesSave()
        {
            Start("first");var cmd=runtime.CommandForSlot(1);runtime.Select(cmd,near,true);
            Assert.That(runtime.State.observations.Any(o=>o.signal=="speech"),Is.False);
            runtime.DeliverPlayerSpeech(near);
            Assert.That(runtime.State.observations.Single(o=>o.signal=="question").count,Is.EqualTo(1));
            runtime=RoundTrip();
            Assert.That(runtime.State.observations.Single(o=>o.signal=="speech").count,Is.EqualTo(1));
        }
        [Test] public void NewSpeechSupersedesAnUnspokenQuietGuess()
        {
            Start("first");runtime.Tick(0,36,near);Start("first");runtime.Tick(0,36,near);
            Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe.quiet"));
            Start("first");Say("first.money");
            Assert.That(runtime.ObservationEntry(),Is.Null);
        }
        [Test] public void CloseStationaryCompanyIsObservedButWalkingAndDistanceAreNot()
        {
            runtime.State.met=true;var moving=near;moving.isStationary=false;
            runtime.Tick(0,50,moving);Assert.That(runtime.ObservationEntry(),Is.Null);
            var far=near;far.canHear=false;runtime.Tick(0,50,far);Assert.That(runtime.ObservationEntry(),Is.Null);
            runtime.Tick(0,44,near);Assert.That(runtime.ObservationEntry(),Is.Null);
            runtime.Tick(0,1,near);Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe.company"));
            Assert.That(runtime.CanNoticeWhileIdle,Is.True);
            runtime.BeginNearby(near);runtime.DeliverNpcLine(near);
            Assert.That(runtime.ObservationEntry(),Is.Null);
        }
        [Test] public void LongResponseWaitDoesNotCountAsIdleCompany()
        {
            Start("first");runtime.Tick(0,35,near);
            Assert.That(runtime.State.observations.Any(o=>o.signal=="company"),Is.False);
        }
        [Test] public void RepeatedVisitsAndQuickDeparturesAreDifferentEvidence()
        {
            for(int i=0;i<4;i++){runtime.BeginNearby(near);runtime.DeliverNpcLine(near);runtime.Close("leave");}
            Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe.return"));
            Setup();Start("first");runtime.EndNaturally(true);Start("first");runtime.EndNaturally(true);
            Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe.hasty"));
        }
        [Test] public void HelpfulObservationRequiresBothRealActionsNotOffers()
        {
            Start("first");Say("first.watch");runtime.Close("test");
            runtime.CollectPaint("one");runtime.GivePaint("give-one",near);
            runtime.CollectPaint("two");runtime.GivePaint("give-two",near);
            Assert.That(runtime.ObservationEntry(),Is.Null);
            runtime.StartCooperation(near);runtime.Tick(0,5,near);
            Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe.help"));
        }
        [Test] public void QuestionSubjectsCreateDifferentObservations()
        {
            Start("first");Say("first.watch");Say("watch.painting");Say("painting.joke");
            Start("painting.detail");Say("detail.money");
            Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe.art"));
            Setup();Start("first");Say("first.money");Say("money.use");Say("use.paint");Say("paint.shop");
            Start("topics");Say("topics.camp");
            Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe.practical"));
        }
        [Test] public void AllObservationRoutesHaveEvidenceThresholdsAndCooldown()
        {
            foreach(var pair in new[]{("quiet",2),("art",3),("practical",3),("care",2),("question",4),("speech",6),("return",3),("company",1),("hasty",2)})
            {
                Setup();runtime.State.met=true;
                runtime.State.observations.Add(new DialogueObservation{signal=pair.Item1,count=pair.Item2-1,lastTime=12});
                Assert.That(runtime.ObservationEntry(),Is.Null,pair.Item1);
                runtime.State.observations[0].count++;
                Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe."+pair.Item1));
                runtime.BeginNearby(near);runtime.DeliverNpcLine(near);runtime.Close("done");
                runtime.Tick(0,91,default);
                Assert.That(runtime.ObservationEntry(),Is.Null,"Must not repeat without new evidence: "+pair.Item1);
                runtime.State.observations[0].count+=pair.Item2;runtime.State.worldHours+=25;
                Assert.That(runtime.ObservationEntry(),Is.Null,"Old evidence must expire: "+pair.Item1);
            }
        }
        [Test] public void ExplicitIntentsAreRememberedCorrectableAndDoNotNagWhileIdle()
        {
            foreach(var item in new[]{("observe.quiet","quiet.yes","self.quiet"),("observe.return","return.chat","self.chat"),
                ("observe.return","return.bored","self.bored"),("observe.company","company.yes","self.rest"),
                ("observe.return","return.no","self.passing"),("observe.quiet","quiet.think","self.thinking")})
            {
                Setup();runtime.State.met=true;Start(item.Item1);Say(item.Item2);runtime.Close("leave");runtime=RoundTrip();
                Assert.That(runtime.State.statedIntent,Is.EqualTo(item.Item3));runtime.Tick(0,91,default);
                Assert.That(runtime.ObservationEntry(),Is.EqualTo("observe."+item.Item3));
                Assert.That(runtime.CanNoticeWhileIdle,Is.False);
            }
            Start("observe.self.quiet");Say("squiet.change");
            Assert.That(runtime.State.statedIntent,Is.EqualTo("self.chat"));
        }
        [Test] public void QuietRequestSuppressesUninvitedCompanyQuestions()
        {
            runtime.State.met=true;Start("observe.quiet");Say("quiet.yes");runtime.Close("quiet");
            runtime.Tick(0,100,near);Assert.That(runtime.CanNoticeWhileIdle,Is.False);
        }
        [Test] public void BoredomIsAskedAboutAndLeadsToAConcreteActivity()
        {
            Start("observe.company");Say("company.boredchoice");Say("bored.do");Say("activity.stone");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("company.edge"));
            Assert.That(runtime.Session.slots,Is.Empty);
            Assert.That(runtime.State.statedIntent,Is.EqualTo("self.bored"));
        }
        [Test] public void LaterConversationsVaryWordingWithoutChangingTheResponseMeaning()
        {
            Start("first");string first=runtime.CurrentLine;var choices=runtime.Session.slots.Select(s=>s.choiceId).ToArray();
            Start("first");string second=runtime.CurrentLine;
            Assert.That(second,Is.Not.EqualTo(first));
            Assert.That(runtime.Session.slots.Select(s=>s.choiceId),Is.EqualTo(choices));
            Assert.That(runtime.Content.Find("first").textVariants,Does.Contain(second));
            Assert.That(runtime.State.history.Last(),Is.EqualTo(runtime.Content.speaker+"："+second));
        }
        [Test] public void UndeliveredVariantsAreNotConsumedAndSaveResumeKeepsTheExactLine()
        {
            runtime.Begin(near,"first");string pending=runtime.CurrentLine;runtime.Close("interrupted");
            Start("first");Assert.That(runtime.CurrentLine,Is.EqualTo(pending));
            Start("first");string second=runtime.CurrentLine;runtime.Suspend("away");runtime=RoundTrip();
            runtime.Begin(near);runtime.DeliverNpcLine(near);
            Assert.That(runtime.CurrentLine,Is.EqualTo(second));
            Assert.That(runtime.State.lineVisits.Single(v=>v.nodeId=="first").count,Is.EqualTo(2));
        }
        [Test] public void RedirectedAcceptanceDoesNotKeepAnUnrelatedCachedLine()
        {
            Start("paint.offer");Say("offer.promise");runtime=RoundTrip();runtime.Begin(near);runtime.DeliverNpcLine(near);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("topics"));
            Assert.That(runtime.CurrentLine,Is.EqualTo(runtime.Content.Find("topics").text));
        }
        [Test] public void CasualRepliesDoNotAssertAPlayerIntent()
        {
            Start("observe.company");Say("company.waves");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("company.waves"));
            Assert.That(runtime.State.statedIntent,Is.Null.Or.Empty);
            Start("observe.art");Say("art.blue");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("company.blue"));
            Assert.That(runtime.State.statedIntent,Is.Null.Or.Empty);
        }
        [Test] public void ApproachUsesDeliveredTopicInsteadOfArbitraryVariation()
        {
            Assert.That(runtime.BeginNearby(near),Is.True);runtime.DeliverNpcLine(near);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("first"));runtime.Close("leave");
            Assert.That(runtime.BeginNearby(near),Is.True);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("greet.return"));runtime.Close("leave");
            runtime.BeginNearby(near);Assert.That(runtime.Session.nodeId,Is.EqualTo("greet.return"));
            runtime.Close("leave");runtime.CollectPaint("test");runtime.BeginNearby(near);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("greet.paint"));
            Assert.That(runtime.State.promises,Is.Empty);
        }
        [Test] public void NightAndMorningHaveDifferentOpenings()
        {
            runtime.State.met=true;runtime.State.worldHours=22;
            runtime.BeginNearby(near);Assert.That(runtime.Session.nodeId,Is.EqualTo("greet.night"));
            runtime.Close("test");runtime.State.worldHours=30;runtime.BeginNearby(near);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("greet.morning"));
        }
        [Test] public void NotebookReturnPersistsAcrossSaveAndDoesNotInventPaintingContext()
        {
            runtime.State.met=true;runtime.State.lastApproachHours=runtime.State.worldHours;
            Start("notebook.notes");runtime.Close("leave");runtime=RoundTrip();
            Assert.That(runtime.BeginNearby(near),Is.True);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("greet.notebook"));
        }
        [Test] public void UndeliveredLineDoesNotReplaceTheRememberedTopic()
        {
            Start("notebook.notes");runtime.Close("leave");runtime.Begin(near,"fish");
            Assert.That(runtime.State.lastTopicEntry,Is.EqualTo("greet.notebook"));
        }
        [Test] public void FarewellAndSilenceStayInTheCurrentTopic()
        {
            foreach(var topic in new[]{"notebook","night","tent"})
            {
                Start(topic=="notebook"?"notebook.notes":topic=="night"?"night.colour":"tent");
                runtime.EndNaturally();Assert.That(runtime.Session.nodeId,Is.EqualTo("bye."+topic));
                Start(topic=="notebook"?"notebook.notes":topic=="night"?"night.colour":"tent");
                runtime.Tick(0,36,near);Assert.That(runtime.Session.nodeId,Is.EqualTo("silence."+topic));
            }
        }
        [Test] public void HandoverReplacesOldOfferChoicesWithActualReceipt()
        {
            Start("paint.offer");var stale=runtime.CommandForSlot(1);runtime.CollectPaint("test");runtime.GivePaint("give",near);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("paint.thanks"));
            runtime.DeliverNpcLine(near);Assert.That(runtime.Select(stale,near,true),Is.False);
            Assert.That(runtime.Session.slots.Any(s=>s.choiceId=="offer.promise"),Is.False);
            Assert.That(runtime.State.promises,Is.Empty);
        }
        [Test] public void TentCompletionReplacesPendingHelpWithAcknowledgement()
        {
            Start("tent.wait");runtime.StartCooperation(near);runtime.Tick(0,5,near);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("tent.done"));runtime.DeliverNpcLine(near);
            Assert.That(runtime.Session.slots.Any(s=>s.choiceId=="wait.topics"),Is.False);
        }
        [Test] public void RevisedQuestionPathsReachTheirSpecificAnswerAndAnEnding()
        {
            Start("notebook.mess");Say("mess.fish");Assert.That(runtime.Session.nodeId,Is.EqualTo("notebook.notes"));
            Say("notes.sea");Assert.That(runtime.Session.nodeId,Is.EqualTo("notebook.tide"));Assert.That(runtime.Session.slots,Is.Empty);
            Start("fish.catch");Say("catch.paint");Assert.That(runtime.Session.nodeId,Is.EqualTo("fish.sketch"));Assert.That(runtime.Session.slots,Is.Empty);
            Start("night.colour");Say("nightcolour.ask");Assert.That(runtime.Session.nodeId,Is.EqualTo("night.last"));Assert.That(runtime.Session.slots,Is.Empty);
            Start("paint.recall");Assert.That(runtime.Session.nodeId,Is.EqualTo("topics"));
        }
        [Test] public void UnfamiliarSaleQuestionDeclinesDirectlyInsteadOfJumpingToMenu()
        {
            Start("money.shy");Say("shy.sell");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("sale.private"));Assert.That(runtime.Session.slots,Is.Empty);
        }
        [Test] public void AnsweredQuestionsDoNotReappearWithinTheSameConversation()
        {
            Start("first");Say("first.watch");Say("watch.painting");Say("painting.paint");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("paint"));
            Assert.That(runtime.Session.slots.Any(s=>s.choiceId=="paint.more"),Is.False);
            Assert.That(runtime.Session.slots.Any(s=>s.choiceId=="paint.shop"),Is.True);
        }
        [Test] public void SuppliedPaintDoesNotBecomeMissingAgainInMoneyDiscussion()
        {
            runtime.ResolvePaintNeed();Start("money");Say("money.use");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("money.supplied"));
            Assert.That(runtime.Session.slots.Any(s=>s.choiceId=="use.paint"),Is.False);
            Say("supplied.fish");Assert.That(runtime.Session.nodeId,Is.EqualTo("fish"));
        }
        [Test] public void ExhaustedQuestionsLeadToTopicFarewellInsteadOfEmptyMenu()
        {
            runtime.Begin(near,"painting");
            foreach(var choice in runtime.Content.Find("painting").choices)
                runtime.Session.heardNodes.Add(choice.next);
            runtime.DeliverNpcLine(near);
            // Terminal farewells remain available even when every question was answered.
            Assert.That(runtime.Session.slots.All(s=>runtime.Content.Find(runtime.Content.Find("painting").choices.Single(c=>c.id==s.choiceId).next).endAfterSeconds>0),Is.True);
            runtime.Tick(0,37,near);runtime.DeliverNpcLine(near);runtime.Tick(0,5,near);
            Assert.That(runtime.Session.phase,Is.EqualTo(ConversationPhase.Closed));
        }
        [Test] public void PromiseContextDoesNotInventOrSettlePromises()
        {
            runtime.State.met=true;
            Start("paint.offer");Say("offer.promise");runtime.Close("test");
            runtime.BeginNearby(near);Assert.That(runtime.Session.nodeId,Is.EqualTo("greet.promise"));
            runtime.Close("test");runtime.Tick(37,0,near);runtime.BeginNearby(near);
            Assert.That(runtime.Session.nodeId,Is.EqualTo("greet.overdue"));
            Assert.That(runtime.ActivePromise.status,Is.EqualTo(PromiseStatus.Overdue));
        }
        [Test] public void FarewellFinishesWithoutAnExtraChoice()
        {
            Start("first");Say("first.leave");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("bye.painting"));
            Assert.That(runtime.Session.slots,Is.Empty);runtime.Tick(0,4,near);
            Assert.That(runtime.OwnsFocus,Is.False);
        }
        [Test] public void QuietCompanyRecordsListeningOnlyAndEndsNaturally()
        {
            Start("greet.return");Say("return.stay");
            Assert.That(runtime.Session.nodeId,Is.EqualTo("watch.company"));
            Assert.That(runtime.State.memories.Single().behavior,Is.EqualTo("chose_to_listen"));
            runtime.Tick(0,5,near);Assert.That(runtime.OwnsFocus,Is.False);
        }
        [Test] public void ResumeAndRestoreKeepElapsedResponseTime()
        {
            Start("first");runtime.Tick(0,7,near);runtime.Suspend("test");
            var restored=RoundTrip();
            Assert.That(restored.Begin(near),Is.True);restored.DeliverNpcLine(near);
            Assert.That(restored.Session.responseElapsedSeconds,Is.EqualTo(7));
            Assert.That(restored.Session.slots[0].disabledReason,Is.Null);
            restored.Tick(0,0,near);
            Assert.That(restored.Session.responseElapsedSeconds,Is.EqualTo(7));
        }
        [Test] public void DistanceGraceAndResumePreserveEncounter()
        {
            Start("paint");string encounter=runtime.State.encounterId;
            var far=near;far.distance=5;far.canHear=false;
            runtime.Tick(0,1,far);Assert.That(runtime.OwnsFocus,Is.True);
            runtime.Tick(0,.1,near);runtime.Tick(0,1,far);Assert.That(runtime.OwnsFocus,Is.True);
            runtime.Tick(0,2.1,far);Assert.That(runtime.OwnsFocus,Is.False);
            Assert.That(runtime.Begin(near),Is.True);Assert.That(runtime.State.encounterId,Is.EqualTo(encounter));
        }
        [Test] public void ConversationContinuesAcrossTwentyMetresAndResumesWithinThatRange()
        {
            var start=near;start.distance=10;
            Assert.That(runtime.Begin(start),Is.True);runtime.DeliverNpcLine(start);
            var walking=near;walking.distance=16;
            runtime.Tick(0,5,walking);Assert.That(runtime.OwnsFocus,Is.True);
            walking.distance=20;runtime.Tick(0,2,walking);Assert.That(runtime.OwnsFocus,Is.True);
            var far=near;far.distance=21;far.canHear=false;
            runtime.Tick(0,2.9,far);Assert.That(runtime.OwnsFocus,Is.True);
            runtime.Tick(0,.2,far);Assert.That(runtime.Session.phase,Is.EqualTo(ConversationPhase.Suspended));
            double elapsed=runtime.Session.responseElapsedSeconds;
            walking.distance=16;Assert.That(runtime.Begin(walking),Is.True);runtime.DeliverNpcLine(walking);
            Assert.That(runtime.Session.responseElapsedSeconds,Is.EqualTo(elapsed));
            Assert.That(runtime.Select(runtime.CommandForSlot(1),walking,true),Is.True);
        }
        [Test] public void HandoverRevalidatesAndIsIdempotentAcrossLoad()
        {
            runtime.CollectPaint("pickup");var far=near;far.canHandOver=false;
            Assert.That(runtime.GivePaint("give",far),Is.False);Assert.That(runtime.State.playerPaint,Is.EqualTo(1));
            Assert.That(runtime.GivePaint("give",near),Is.True);runtime=RoundTrip();
            Assert.That(runtime.GivePaint("give",near),Is.False);Assert.That(runtime.State.playerPaint,Is.Zero);
            Assert.That(runtime.State.npcPaint,Is.EqualTo(1));Assert.That(runtime.State.memories.Count(m=>m.behavior=="gave_blue_paint"),Is.EqualTo(1));
        }
        [Test] public void ActualHandoverFulfilsPromiseOnce()
        {
            Start("paint.offer");Say("offer.promise");runtime.CollectPaint("pickup");runtime.GivePaint("give",near);
            Assert.That(runtime.State.promises.Single().status,Is.EqualTo(PromiseStatus.Fulfilled));Assert.That(runtime.State.promises.Single().progress,Is.EqualTo(1));
        }
        [Test] public void AutonomousSupplyReleasesPromiseAndChangesReentry()
        {
            Start("paint.offer");Say("offer.promise");runtime.Tick(49,0,near);
            Assert.That(runtime.State.promises.Single().status,Is.EqualTo(PromiseStatus.Released));Assert.That(runtime.State.promises.Single().reason,Is.EqualTo("NeedResolved"));
            runtime.Close("leave");runtime.Begin(near,"paint");Assert.That(runtime.Session.nodeId,Is.EqualTo("paint.resolved"));
        }
        [Test] public void OverdueIsPreservedByLateFulfilment()
        {
            Start("paint.offer");Say("offer.promise");runtime.Tick(37,0,near);
            Assert.That(runtime.ActivePromise.status,Is.EqualTo(PromiseStatus.Overdue));runtime.CollectPaint("pickup");runtime.GivePaint("give",near);
            Assert.That(runtime.State.promises.Single().wasOverdue,Is.True);
        }
        [Test] public void RepeatedOfferWithinEncounterDoesNotFarmEvidence()
        {
            Start("paint");Say("paint.offer");Start("paint");Say("paint.offer");
            Assert.That(runtime.State.memories.Count(m=>m.behavior=="offered_to_look"),Is.EqualTo(1));
        }
        [Test] public void RecallRequiresActualMemoryAndHasCooldown()
        {
            Start("paint.recall");Assert.That(runtime.Session.nodeId,Is.EqualTo("topics"));
            runtime.CollectPaint("pickup");runtime.GivePaint("give",near);Assert.That(runtime.RecallReady,Is.False);
            runtime.Tick(24,0,near);Assert.That(runtime.RecallReady,Is.True);
            Start("paint.recall");Assert.That(runtime.RecallReady,Is.False);runtime.Tick(24,0,near);Assert.That(runtime.RecallReady,Is.True);
        }
        [Test] public void CooperationAndSpeechProceedIndependently()
        {
            Start("tent");Assert.That(runtime.StartCooperation(near),Is.True);Say("tent.no");runtime.Close("leave conversation");
            Assert.That(runtime.State.cooperation,Is.EqualTo(CooperationPhase.Active));runtime.Tick(0,5,near);
            Assert.That(runtime.State.cooperation,Is.EqualTo(CooperationPhase.Completed));Assert.That(runtime.State.memories.Any(m=>m.behavior=="held_tent"),Is.True);
        }
        [Test] public void InterruptedCooperationDoesNotRecordSuccess()
        {
            runtime.StartCooperation(near);runtime.Tick(0,2,near);var far=near;far.canHandOver=false;runtime.Tick(0,1,far);
            Assert.That(runtime.State.cooperation,Is.EqualTo(CooperationPhase.Cancelled));Assert.That(runtime.State.memories,Is.Empty);
        }
        [Test] public void RestoreDoesNotReproposeOrExtendDeadline()
        {
            Start("paint.offer");Say("offer.promise");double deadline=runtime.ActivePromise.deadline;runtime=RoundTrip();
            Assert.That(runtime.ActivePromise.deadline,Is.EqualTo(deadline));Assert.That(runtime.Begin(near),Is.True);runtime.DeliverNpcLine(near);
            Assert.That(runtime.State.promises.Count,Is.EqualTo(1));Assert.That(runtime.ActivePromise.deadline,Is.EqualTo(deadline));
        }
        [Test] public void MissingNodeClosesSessionWithoutDroppingPromise()
        {
            Start("paint.offer");Say("offer.promise");runtime.Session.nodeId="removed";runtime=RoundTrip();
            Assert.That(runtime.Session.phase,Is.EqualTo(ConversationPhase.Closed));Assert.That(runtime.ActivePromise,Is.Not.Null);
        }
        [Test] public void NewerSaveIsRejected()
        {
            runtime.State.schemaVersion=999;Assert.Throws<InvalidOperationException>(()=>RoundTrip());
        }
        [Test] public void SaveBeforeAcceptanceDoesNotCreatePhantomPromiseOnResume()
        {
            Start("paint.offer");runtime.Select(runtime.CommandForSlot(1),near,true);runtime.DeliverPlayerSpeech(near);
            runtime=RoundTrip();Assert.That(runtime.Begin(near),Is.True);runtime.DeliverNpcLine(near);
            Assert.That(runtime.ActivePromise,Is.Null);Assert.That(runtime.Session.nodeId,Is.EqualTo("paint.offer"));
            Assert.That(runtime.State.promises.Single().status,Is.EqualTo(PromiseStatus.Withdrawn));
        }
        [Test] public void FileSaveReplacesAtomicallyAndKeepsPreviousSnapshot()
        {
            string directory=Path.Combine(Application.temporaryCachePath,"DialogueTest-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"dialogue.json");
            var store=Type.GetType("Voyage.Dialogue.DialogueSaveStore, Assembly-CSharp",true);
            try
            {
                runtime.CollectPaint("pickup");store.GetMethod("Save").Invoke(null,new object[]{path,runtime.State});
                runtime.GivePaint("give",near);store.GetMethod("Save").Invoke(null,new object[]{path,runtime.State});
                var loaded=(ConversationRuntime)store.GetMethod("Load").Invoke(null,new object[]{path,runtime.Content});
                Assert.That(loaded.GivePaint("give",near),Is.False);Assert.That(loaded.State.playerPaint,Is.Zero);
                var backup=(ConversationRuntime)store.GetMethod("Load").Invoke(null,new object[]{path+".bak",runtime.Content});
                Assert.That(backup.State.playerPaint,Is.EqualTo(1));Assert.That(File.Exists(path+".tmp"),Is.False);
                File.WriteAllText(path,"{broken");Assert.Throws<System.Reflection.TargetInvocationException>(()=>store.GetMethod("Load").Invoke(null,new object[]{path,runtime.Content}));
                Assert.That(File.ReadAllText(path),Is.EqualTo("{broken"));
            }
            finally { if(Directory.Exists(directory))Directory.Delete(directory,true); }
        }
    }
}
