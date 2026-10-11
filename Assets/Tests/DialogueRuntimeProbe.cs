#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Voyage.Dialogue;
using Voyage.Exploration;
using Voyage.Lighting;

namespace Voyage.Tests
{
    // Runs the actual scene adapter and input Update loop, not a second implementation.
    public sealed class DialogueRuntimeProbe : MonoBehaviour
    {
        Keyboard keyboard;
        Gamepad gamepad;
        Mouse mouse;
        CoBluesDialogue dialogue;
        ExplorerPlayer player;
        readonly List<string> results = new List<string>();
        const string DirectoryPath = "Logs/DialogueValidation";
        bool oldClockAdvance;
        IEnumerator Start()
        {
            Directory.CreateDirectory(DirectoryPath);
            var session = ExplorationSession.Instance;
            dialogue = FindAnyObjectByType<CoBluesDialogue>();
            if (session == null || dialogue == null) { Check(false,"Scene has spawned exploration and CoBlues"); Finish(); yield break; }
            if (DayNightSystem.Instance != null) { oldClockAdvance=DayNightSystem.Instance.advanceTime;DayNightSystem.Instance.advanceTime=false;DayNightSystem.Instance.SetTime(12); }
            Check(Application.isFocused,"Game has application focus");
            // Stabilize the vehicle as a test precondition; the authored spawn is on a slope.
            var car=DrivingCore.Instance.Player.GetComponent<Rigidbody>();
            bool gravity=car.useGravity;var constraints=car.constraints;
            car.linearVelocity=Vector3.zero;car.angularVelocity=Vector3.zero;car.useGravity=false;car.constraints=RigidbodyConstraints.FreezeAll;
            yield return new WaitForSecondsRealtime(.4f);
            if (!session.OnFoot) Check(session.ExitVehicle(),"Exit vehicle onto clear terrain (speed="+DrivingCore.Instance.Player.GetComponent<CarControl>().CurrentSpeedKmh+", ground="+session.TryFindExit(out _)+")");
            car.useGravity=gravity;car.constraints=constraints;
            player=session.Explorer;
            if (!player.isActiveAndEnabled) { Finish(); yield break; }
            float exitWait=0;
            while(player.ControlLocked && exitWait<5){yield return null;exitWait+=Time.unscaledDeltaTime;}
            Check(!player.ControlLocked,"Dismount has landed and restored player control");
            if(player.ControlLocked){Finish();yield break;}
            keyboard=InputSystem.AddDevice<Keyboard>();
            mouse=InputSystem.AddDevice<Mouse>();
            MoveTo(dialogue.transform.position+dialogue.transform.forward*2);
            float greetingWait=0;
            while(dialogue.VisibleNpcSpeech==null && greetingWait<3){yield return null;greetingWait+=Time.unscaledDeltaTime;}
            if(dialogue.VisibleNpcSpeech==null){
                Check(false,"Greeting unavailable: distance="+dialogue.Context().distance+", canHear="+dialogue.Context().canHear+", phase="+dialogue.Runtime.Session.phase);
                Finish();yield break;
            }
            string firstPrefix=dialogue.VisibleNpcSpeech;
            Check(firstPrefix!=null && firstPrefix.Length<dialogue.Runtime.CurrentLine.Length,"NPC begins with a partial typewriter line");
            yield return new WaitForSecondsRealtime(.16f);
            Check(dialogue.VisibleNpcSpeech!=null && dialogue.VisibleNpcSpeech.Length>(firstPrefix?.Length??0),"NPC reveals more characters as time advances");
            ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath,"typewriter-npc-early.png"));
            yield return new WaitForSecondsRealtime(.8f);
            Check(dialogue.Runtime.Session.nodeId=="first" && dialogue.CanRespond,"NPC notices approach and speaks without any interaction key");
            Check(!string.IsNullOrEmpty(dialogue.VisibleNpcSpeech),"NPC speech is available for head anchored rendering");
            Check(dialogue.ChatAvailable,"Player chat bubble is available when responses can be sent");
            Check(player.Hint=="","NPC uses no question mark or exclamation mark");
            Check(!player.TryInteract(),"F does not bypass the conversational interaction");
            ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath,"head-greeting.png"));
            yield return new WaitForEndOfFrame();
            MoveTo(dialogue.transform.position+dialogue.transform.forward*15f);
            yield return new WaitForSecondsRealtime(2);
            Check(dialogue.CanRespond && dialogue.Runtime.Session.nodeId=="first","Walking several steps to 15 metres keeps the conversation active");
            MoveTo(dialogue.transform.position+dialogue.transform.forward*2);
            yield return new WaitForSecondsRealtime(.2f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            yield return new WaitForSecondsRealtime(.2f);
            Check(dialogue.Runtime.Session.nodeId=="first","Holding Tab does not submit");
            Check(dialogue.SelectedSlot==0 && dialogue.Runtime.Session.slots[0].text=="...","Default response is silence");
            var selectionPosition = player.transform.position;
            foreach (var key in new[] { Key.S, Key.W, Key.DownArrow, Key.UpArrow })
            {
                int before = dialogue.SelectedSlot;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab,key));
                yield return new WaitForSecondsRealtime(.1f);
                Check(dialogue.SelectedSlot != before,"Tab + " + key + " selects a response");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
                yield return null;
            }
            Check(Vector3.ProjectOnPlane(player.transform.position-selectionPosition,Vector3.up).magnitude < .03f,"Dialogue selection does not move the player");
            InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,-120));
            ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath,"head-options.png"));
            yield return new WaitForSecondsRealtime(.2f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(.25f);
            Check(!string.IsNullOrEmpty(dialogue.VisiblePlayerSpeech) && "这些画不拿去卖吗？".StartsWith(dialogue.VisiblePlayerSpeech) && dialogue.VisiblePlayerSpeech.Length<"这些画不拿去卖吗？".Length,"Player speech reveals a partial typewriter prefix");
            ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath,"head-player.png"));
            yield return new WaitForSecondsRealtime(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath,"typewriter-player-full.png"));
            yield return new WaitForSecondsRealtime(1.6f);
            Check(dialogue.Runtime.Session.nodeId=="money" && dialogue.CanRespond,"NPC replies after the player has spoken");
            yield return Choose(1);
            Check(dialogue.Runtime.Session.nodeId=="money.useful","Conversation follows the selected branch");
            yield return Choose(1);
            Check(dialogue.Runtime.Session.nodeId=="paint","Paint branch remains reachable");
            yield return Choose(3);
            Check(dialogue.Runtime.Session.nodeId=="paint.offer" && dialogue.Runtime.State.promises.Count==0,"Wheel selects offer without inventing a promise");
            yield return Choose(1);
            Check(dialogue.Runtime.ActivePromise!=null,"Spoken acceptance establishes a promise");
            yield return Tap(Key.P,.2f);
            double elapsed=dialogue.Runtime.Session.responseElapsedSeconds;
            yield return new WaitForSecondsRealtime(.3f);
            Check(Time.timeScale==0 && dialogue.Runtime.Session.responseElapsedSeconds==elapsed,"Pause freezes response timing");
            yield return Tap(Key.Escape,.2f);
            yield return Tap(Key.Escape,.7f);
            Check(dialogue.Runtime.Session.nodeId=="bye.promise" && !dialogue.ChatAvailable,"Ending gets a contextual goodbye without new choices");
            ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath,"head-goodbye.png"));
            yield return new WaitForSecondsRealtime(4.3f);
            Check(CoBluesDialogue.Foreground==null,"Goodbye ends automatically");
            yield return new WaitForSecondsRealtime(1);
            Check(CoBluesDialogue.Foreground==null,"Standing nearby does not repeat automatic greetings");
            var pickup=GameObject.Find("旅行物资：蓝色颜料 ×1");
            MoveTo(pickup.transform.position+Vector3.forward*.5f);
            yield return Tap(Key.E,.3f);
            Check(dialogue.Runtime.State.playerPaint==1,"Environmental pickup still works");
            MoveTo(dialogue.transform.position+dialogue.transform.forward*1.8f);
            yield return Tap(Key.E,.3f);
            Check(dialogue.Runtime.State.playerPaint==0 && dialogue.Runtime.State.npcPaint==1,"Actual handover transfers one tube");
            Check(dialogue.Runtime.State.promises.Exists(p=>p.status==PromiseStatus.Fulfilled),"Actual handover fulfils the promise");
            Check(dialogue.VisibleNpcSpeech.Contains("收到了"),"Handover thanks appears as NPC head speech");

            dialogue.Runtime.Close("probe");
            dialogue.Runtime.Begin(dialogue.Context(),"greet.night");
            dialogue.Runtime.Suspend("probe");dialogue.Begin(player);
            yield return new WaitForSecondsRealtime(.7f);
            Check(dialogue.Runtime.Session.nodeId=="greet.night","Alternative night branch can be presented");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            yield return new WaitForSecondsRealtime(.2f);
            dialogue.Runtime.Tick(0,18,dialogue.Context());
            Check(dialogue.Runtime.Session.slots.TrueForAll(s=>string.IsNullOrEmpty(s.disabledReason)) && dialogue.SelectedSlot==0,"All choices remain usable after the old 18-second deadline");
            ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath,"head-full-window.png"));
            string oldPlayerLine=dialogue.VisiblePlayerSpeech;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(.7f);
            Check(dialogue.Runtime.Session.nodeId=="greet.night" && dialogue.CanRespond,"Releasing default only closes the options and retains the response window");
            Check(dialogue.VisiblePlayerSpeech==oldPlayerLine,"Default release never renders a spoken player line");
            double beforeReopen=dialogue.Runtime.Session.responseElapsedSeconds;
            string silenceGroup=dialogue.Runtime.Session.responseSetId;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            yield return new WaitForSecondsRealtime(.2f);
            Check(dialogue.Runtime.Session.responseSetId==silenceGroup && dialogue.Runtime.Session.responseElapsedSeconds>beforeReopen,"Reopening Tab keeps the same group and continues counting down");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(.2f);            dialogue.Runtime.Tick(0,35-dialogue.Runtime.Session.responseElapsedSeconds,dialogue.Context());
            Check(dialogue.Runtime.Session.phase==ConversationPhase.AwaitingResponse && dialogue.Runtime.Session.slots.TrueForAll(s=>string.IsNullOrEmpty(s.disabledReason)),"Entire response set remains available at 35 seconds");
            dialogue.Runtime.Tick(0,1,dialogue.Context());
            yield return new WaitForSecondsRealtime(.7f);
            Check(dialogue.Runtime.Session.nodeId=="silence.night","36-second timeout follows contextual silence");
            yield return new WaitForSecondsRealtime(4.3f);
            dialogue.Runtime.Begin(dialogue.Context(),"topics");dialogue.Runtime.Suspend("probe");dialogue.Begin(player);
            yield return new WaitForSecondsRealtime(.7f);
            string group=dialogue.Runtime.Session.responseSetId;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            yield return new WaitForSecondsRealtime(.2f);
            dialogue.SendMessage("OnApplicationFocus",false);dialogue.SendMessage("OnApplicationFocus",true);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(.4f);
            Check(dialogue.Runtime.Session.responseSetId==group,"Focus loss cancels held response");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            yield return new WaitForSecondsRealtime(.2f);
            InputSystem.RemoveDevice(keyboard);
            yield return new WaitForSecondsRealtime(.4f);
            Check(dialogue.Runtime.Session.responseSetId==group,"Device removal does not send a reply");
            keyboard=InputSystem.AddDevice<Keyboard>();
            gamepad=InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.LeftShoulder));
            yield return new WaitForSecondsRealtime(.2f);
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.DpadDown));
            yield return new WaitForSecondsRealtime(.2f);
            Check(dialogue.SelectedSlot==1,"Held LB and D-pad select a topic");
            InputSystem.QueueStateEvent(gamepad,new GamepadState());
            yield return new WaitForSecondsRealtime(2.7f);
            Check(dialogue.Runtime.Session.nodeId=="paint.resolved","Releasing LB sends selected topic");
            dialogue.Runtime.Close("observation probe");
            dialogue.Runtime.State.observations.Clear();dialogue.Runtime.State.statedIntent=null;
            dialogue.Runtime.State.observationCooldownSeconds=0;
            // Exercise the normal idle adapter: the NPC should notice without a new keypress.
            float observationWait=0;
            while(observationWait<55 && (dialogue.Runtime.Session.nodeId!="observe.company" || !dialogue.CanRespond))
            { yield return new WaitForSecondsRealtime(.25f);observationWait+=.25f; }
            Check(dialogue.Runtime.Session.nodeId=="observe.company" && dialogue.CanRespond,
                "NPC notices sustained quiet company and opens a contextual question (wait="+observationWait+
                ", stationary="+dialogue.Context().isStationary+", distance="+dialogue.Context().distance+
                ", idle="+dialogue.Runtime.State.nearbyIdleSeconds+", candidate="+dialogue.Runtime.ObservationEntry()+")");
            ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath,"head-observation.png"));
            yield return Choose(3);
            Check(dialogue.Runtime.Session.nodeId=="company.bored","Player can explain being bored instead of accepting the NPC guess");
            yield return Choose(1);
            Check(dialogue.Runtime.Session.nodeId=="company.activity","Boredom leads to a concrete shared activity");
            yield return Choose(2);
            Check(dialogue.Runtime.Session.nodeId=="company.edge","NPC responds to the player's specific observation about the painting");
            Finish();
        }        IEnumerator Choose(int steps)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            yield return new WaitForSecondsRealtime(.2f);
            for(int i=0;i<steps;i++)
            {
                InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,-120));
                yield return new WaitForSecondsRealtime(.1f);
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(2.7f);
        }
        IEnumerator Tap(Key key,float after)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
            yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(after);
        }
        IEnumerator Hold(Key key,float duration)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(duration);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.3f);
        }
        IEnumerator Pad(GamepadButton button,float after)
        {
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(button));yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return new WaitForSecondsRealtime(after);
        }
        void MoveTo(Vector3 position)
        {
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;
            player.transform.position=position;controller.enabled=true;Physics.SyncTransforms();
        }
        void Check(bool pass,string description)
        {
            results.Add((pass?"PASS ":"FAIL ")+description);
            File.WriteAllLines(Path.Combine(DirectoryPath,"runtime-probe.txt"),results);
        }
        void Finish()
        {
            results.Add("FINISHED");File.WriteAllLines(Path.Combine(DirectoryPath,"runtime-probe.txt"),results);
            Debug.Log("Dialogue runtime probe finished: "+string.Join("\n",results));
            Destroy(this);
        }
        void OnDestroy()
        {
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(gamepad!=null && gamepad.added)InputSystem.RemoveDevice(gamepad);
            if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);
            if(DayNightSystem.Instance!=null)DayNightSystem.Instance.advanceTime=oldClockAdvance;
        }
    }
}
#endif

