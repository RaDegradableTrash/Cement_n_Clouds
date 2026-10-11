using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Voyage.Exploration;
using Voyage.Lighting;

namespace Voyage.Dialogue
{
    // One foreground owner. Added to the demo NPC by ExplorationSession.
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(NpcActor))]
    public sealed class CoBluesDialogue : MonoBehaviour
    {
        public static CoBluesDialogue Foreground { get; private set; }
        public static int ConsumedEscapeFrame { get; private set; } = -1;
        public static bool BlocksPause => ConsumedEscapeFrame == Time.frameCount;
        public static bool InputModal { get; private set; }
        public static bool UsesPointer => false;
        public static bool ConsumesMovement => Foreground != null && Foreground.Held;
        public static bool ConsumesScroll => Foreground != null && Foreground.Held;
        bool Held => Keyboard.current != null && Keyboard.current.tabKey.isPressed || Gamepad.current != null && Gamepad.current.leftShoulder.isPressed;
        bool wasHeld, holdArmed, requireRelease;
        string heldSet;
        public int SelectedSlot => selected;
        public ConversationRuntime Runtime { get; private set; }
        public Key actionKey = Key.E;
        public int fontSize = 20;
        public bool ExternalInputBlocked { get; set; }
        public string SavePath => Path.Combine(Application.persistentDataPath, "coblues-dialogue-v1.json");
        ExplorerPlayer player;
        NpcActor actor;
        string observedSet;
        float phaseTime;
        bool noticeReady = true;
        float noticeSeconds, idleClock, nextGreetingAt, nextManualAt;
        string npcSpeech, playerSpeech, spokenSet, spokenChoice;
        float npcSpeechLeft, playerSpeechLeft;
        float npcSpeechElapsed, playerSpeechElapsed;
        int spokenBeats;
        const float CharactersPerSecond = 22;
        Texture2D chatIcon, speechTail;
        public bool CanRespond => Runtime != null && Runtime.Session.phase == ConversationPhase.AwaitingResponse && Runtime.Session.slots.Exists(s => string.IsNullOrEmpty(s.disabledReason)) && Context().canHear;
        double ApproachRange => Runtime.Session.phase==ConversationPhase.Suspended?ConversationRuntime.ConversationDistance:ConversationRuntime.StartDistance;
        public bool ChatAvailable => !Blocked && Time.timeScale > 0 && player != null && player.isActiveAndEnabled && (CanRespond || Foreground == null && idleClock >= nextManualAt && Context().canHear && Context().distance <= ApproachRange);
        public string VisibleNpcSpeech => npcSpeechLeft > 0 ? Revealed(npcSpeech,npcSpeechElapsed) : null;
        public string VisiblePlayerSpeech => playerSpeechLeft > 0 ? Revealed(playerSpeech,playerSpeechElapsed) : null;

        int selected;

        Font chineseFont;
        GUIStyle lineStyle, choiceStyle, smallStyle;
        GameObject paintPickup, tentPatch;
        const string PickupCommand = "cobalt.demo.paint-pickup.1";

        void Awake()
        {
            actor = GetComponent<NpcActor>(); actor.displayName = "钴蓝"; actor.prompt = "交谈";
            var asset = Resources.Load<TextAsset>("Dialogue/CoBlues");
            if (asset == null) { Debug.LogError("Missing CoBlues dialogue content", this); enabled = false; return; }
            Runtime = new ConversationRuntime(JsonUtility.FromJson<DialogueContent>(asset.text));
            chatIcon = CreateChatIcon();
            speechTail = CreateSpeechTail();
            chineseFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei", "Arial Unicode MS" }, 22);

        }
        public void Initialize(ExplorerPlayer explorer)
        {
            player = explorer;
            // Explicit prototype props: the one paint tube belongs to the player, no invented shop economy.
            paintPickup = MakeProp("旅行物资：蓝色颜料 ×1", transform.position + transform.right * 3, new Vector3(.18f,.4f,.18f), new Color(.1f,.3f,.8f), PrimitiveType.Cylinder);
            tentPatch = MakeProp("帐篷布", transform.position - transform.right * 1.2f, new Vector3(1.2f,.08f,1.6f), new Color(.63f,.57f,.4f), PrimitiveType.Cube);
        }
        GameObject MakeProp(string label, Vector3 position, Vector3 scale, Color color, PrimitiveType primitive)
        {
            var prop = GameObject.CreatePrimitive(primitive); prop.name = label;
            prop.transform.position = position + Vector3.up * scale.y * .5f; prop.transform.localScale = scale;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = color;
            prop.GetComponent<Renderer>().material = material;
            return prop;
        }
        public bool Begin(ExplorerPlayer explorer)
        {
            if (Foreground != null && Foreground != this) return false;
            player = explorer;
            if (!Runtime.BeginNearby(Context())) return false;
            Foreground = this; noticeReady = false; noticeSeconds = 0; ArmBarrier(); return true;
        }
        public DialogueContext Context()
        {
            if (player == null || !player.isActiveAndEnabled) return default;
            Vector3 eye = player.transform.position + Vector3.up;
            float distance = Vector3.Distance(eye, actor.Anchor);
            bool visible = true;
            foreach (var hit in Physics.RaycastAll(eye, (actor.Anchor - eye).normalized, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(player.transform) && !hit.transform.IsChildOf(transform)) { visible = false; break; }
            return new DialogueContext { npcAvailable = isActiveAndEnabled && actor.interactable, distance = distance,
                canHear = visible && distance <= ConversationRuntime.ConversationDistance, canHandOver = visible && distance <= 2.5f, canCooperate = NearTent(),
                isStationary = Vector3.ProjectOnPlane(player.Velocity,Vector3.up).sqrMagnitude < .04f };
        }
        bool DeviceReady => Keyboard.current != null || Gamepad.current != null;
        bool Blocked => ExternalInputBlocked || !Application.isFocused || !DeviceReady || VoyageCommandConsole.IsOpen || VoyageCommandConsole.ConsumedInputThisFrame;
        bool Press(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        bool PadCancel => Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        void ArmBarrier() { holdArmed = false; heldSet = null; requireRelease = Held; phaseTime = 0; }
        void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;
        void OnApplicationFocus(bool focused) { if (!focused) ArmBarrier(); }
        void OnApplicationPause(bool paused) { if (paused) ArmBarrier(); }
        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if ((device is Keyboard || device is Gamepad) &&
                (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed || change == InputDeviceChange.Disabled))
                ArmBarrier();
        }
        void Update()
        {
            if (Runtime == null) return;
            if (player == null && ExplorationSession.Instance != null) player = ExplorationSession.Instance.Explorer;
            var context = Context();
            var clock = DayNightSystem.Instance;
            bool blocked = Blocked || Time.timeScale <= 0;
            double worldDelta = clock != null && clock.advanceTime && !Blocked ? Time.deltaTime * clock.timeScale * 24 / clock.CycleDurationSeconds : 0;
            if (!blocked)
            {
                idleClock += Time.unscaledDeltaTime;
                npcSpeechLeft -= Time.unscaledDeltaTime; playerSpeechLeft -= Time.unscaledDeltaTime;
                npcSpeechElapsed += Time.unscaledDeltaTime; playerSpeechElapsed += Time.unscaledDeltaTime;
                if (context.distance > ConversationRuntime.ConversationDistance || !context.canHear) noticeReady = true;
            }
            bool hadFocus = Runtime.OwnsFocus;
            var previousCooperation = Runtime.State.cooperation;
            Runtime.Tick(worldDelta, blocked ? 0 : Time.unscaledDeltaTime, context);
            if (previousCooperation == CooperationPhase.Active && Runtime.State.cooperation == CooperationPhase.Completed && !Runtime.OwnsFocus)
                SpeakNpc(Runtime.Content.Find("tent.done").text);
            if (hadFocus && !Runtime.OwnsFocus) { nextGreetingAt = idleClock + 15; nextManualAt = idleClock + 5; }
            if (player == null || !player.isActiveAndEnabled)
            {
                if (Runtime.OwnsFocus) Runtime.Close("离开步行交谈");
                Runtime.CancelCooperation(); ReleaseFocus(); ArmBarrier(); wasHeld = Held; return;
            }
            if (blocked) { ArmBarrier(); wasHeld = Held; return; }
            bool held = Held;
            bool pressed = held && !wasHeld;
            bool released = !held && wasHeld;
            wasHeld = held;
            if (!held) requireRelease = false;
            if (!Runtime.OwnsFocus) { ReleaseFocus(); holdArmed = false; }
            if (Foreground == this && (Press(Key.Escape) || PadCancel))
            {
                ConsumedEscapeFrame = Time.frameCount;
                if (Runtime.Content.Find(Runtime.Session.nodeId)?.endAfterSeconds > 0) { Runtime.Close("结束交谈"); ReleaseFocus(); nextManualAt = idleClock + 5; nextGreetingAt = idleClock + 15; }
                else Runtime.EndNaturally();
                ArmBarrier(); return;
            }
            if (paintPickup != null) paintPickup.SetActive(!Runtime.State.completedCommands.Contains(PickupCommand));


            bool canObserve = Runtime.CanNoticeWhileIdle && Runtime.ApproachEntry().StartsWith("observe.");
            if (Foreground == null && (noticeReady || canObserve) && idleClock >= nextGreetingAt && context.canHear && context.distance <= ApproachRange)
            {
                noticeSeconds += Time.unscaledDeltaTime;
                if (noticeSeconds >= .65f) Begin(player);
            }
            else noticeSeconds = 0;
            if (Foreground == null && ChatAvailable && pressed && !requireRelease)
            {
                if (Begin(player)) { requireRelease = false; holdArmed = true; heldSet = Runtime.Session.responseSetId; }
            }
            if (Press(actionKey) || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)) Act(context);
            if (Foreground != this) return;
            if (observedSet != Runtime.Session.responseSetId)
            {
                observedSet = Runtime.Session.responseSetId; selected = 0; phaseTime = 0;
                if (heldSet != observedSet) { holdArmed = false; requireRelease = held; }
            }
            phaseTime += Time.unscaledDeltaTime;
            if (Runtime.Session.phase == ConversationPhase.Resolving)
            {
                if (spokenChoice != Runtime.Session.pendingCommandId)
                {
                    spokenChoice = Runtime.Session.pendingCommandId;
                    Feedback(Runtime.CurrentLine);
                    npcSpeechLeft = Mathf.Min(npcSpeechLeft,.6f);
                }
                if (phaseTime >= Mathf.Clamp((Runtime.CurrentLine?.Length ?? 0) * .06f,.9f,2f)) { Runtime.DeliverPlayerSpeech(context); phaseTime = 0; }
                return;
            }
            if (Runtime.Session.phase == ConversationPhase.Presenting)
            {
                if (phaseTime > .35f && Runtime.DeliverNpcLine(context))
                {
                    if (Runtime.Session.lineDelivered && spokenSet != Runtime.Session.responseSetId)
                    { spokenSet = Runtime.Session.responseSetId; SpeakNpc(Runtime.CurrentLine); }
                }
                // Releasing before the responses arrive cannot submit later.
                if (released) holdArmed = false;
                return;
            }
            if (Runtime.Session.phase != ConversationPhase.AwaitingResponse) return;
            if (pressed && !requireRelease) { holdArmed = true; heldSet = Runtime.Session.responseSetId; }
            if (held && holdArmed)
            {
                float wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0;
                var pad = Gamepad.current;
                if (wheel < 0 || Press(Key.DownArrow) || Press(Key.S) || pad != null && pad.dpad.down.wasPressedThisFrame) StepSelection(1);
                else if (wheel > 0 || Press(Key.UpArrow) || Press(Key.W) || pad != null && pad.dpad.up.wasPressedThisFrame) StepSelection(-1);
            }
            if (released && holdArmed)
            {
                holdArmed = false;
                if (heldSet == Runtime.Session.responseSetId && Runtime.Select(Runtime.CommandForSlot(selected), context, true))
                    phaseTime = 0;
            }
        }
        void StepSelection(int direction)
        {
            int count = Runtime.Session.slots.Count;
            for (int step = 1; step <= count; step++)
            {
                int candidate = (selected + direction * step + count * 2) % count;
                if (string.IsNullOrEmpty(Runtime.Session.slots[candidate].disabledReason)) { selected = candidate; return; }
            }
        }
        void Act(DialogueContext context)
        {
            if (Runtime.State.cooperation == CooperationPhase.Active) { Runtime.CancelCooperation(); Feedback("已松开帐篷布。"); return; }
            if (NearPickup())
            {
                if (Runtime.CollectPaint(PickupCommand)) { paintPickup.SetActive(false); Feedback("拿起旅行物资：蓝色颜料 ×1。"); }
                return;
            }
            if (NearTent() && Runtime.State.cooperation != CooperationPhase.Completed)
            { if (Runtime.StartCooperation(context)) Feedback("正在踩住帐篷布。"); return; }
            if (Runtime.GivePaint(Guid.NewGuid().ToString("N"), context))
            {
                Feedback("已递给钴蓝：蓝色颜料 ×1。");
                if (!Runtime.OwnsFocus)
                {
                    string thanks = Runtime.Content.Find("paint.thanks").text;
                    Runtime.AddHistory("钴蓝：" + thanks); SpeakNpc(thanks);
                }
            }
            
        }
        bool NearPickup() => paintPickup != null && paintPickup.activeSelf && Vector3.Distance(player.transform.position, paintPickup.transform.position) < 1.5f;
        bool NearTent() => tentPatch != null && Vector3.Distance(player.transform.position, tentPatch.transform.position) < 1.1f;
        void Feedback(string text)
        {
            playerSpeech = text; playerSpeechElapsed=0;playerSpeechLeft = ReadingSeconds(text);
            if (Foreground == this && player != null && player.Velocity.sqrMagnitude < .04f)
                player.GetComponent<CharacterLocomotionAnimator>()?.PlayGesture(text.Contains("？") ? "OfferOneHand" : "HandOnChest", 1.5f);
        }
        static float ReadingSeconds(string text) => Mathf.Clamp(2 + (text?.Length ?? 0) * .13f,4,10)+(text?.Length??0)/CharactersPerSecond;
        void SpeakNpc(string text)
        {
            npcSpeech = text; npcSpeechElapsed=0;npcSpeechLeft = ReadingSeconds(text);
            // Give longer beats a gesture, leaving alternate lines quiet so
            // conversation is not a repeating arm-animation metronome.
            if (++spokenBeats % 2 == 1)
                GetComponent<CharacterLocomotionAnimator>()?.PlayGesture(text != null && text.Contains("谢") ? "HandOnChest" : "OfferOneHand", Mathf.Clamp((text?.Length ?? 0) * .09f, 1.2f, 2.7f));
        }
        void LateUpdate()
        {
            if (Foreground != this || Blocked || player == null || !Context().canHear) return;
            Vector3 direction = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 100 * Time.deltaTime);
            if (Held && player.IsGrounded && !player.ControlLocked && direction.sqrMagnitude > .01f)
                player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, Quaternion.LookRotation(-direction), 120 * Time.deltaTime);
        }
        static string Revealed(string text,float elapsed)
        {
            if(string.IsNullOrEmpty(text))return text;
            var boundaries=System.Globalization.StringInfo.ParseCombiningCharacters(text);
            int count = 0;
            for (; count < boundaries.Length; count++)
            {
                char c = text[boundaries[count]];
                elapsed -= 1 / CharactersPerSecond;
                if ("，、；：".IndexOf(c) >= 0) elapsed -= .09f;
                if ("。！？…".IndexOf(c) >= 0) elapsed -= .18f;
                if (elapsed < 0) break;
            }
            return count>=boundaries.Length?text:count==0?string.Empty:text.Substring(0,boundaries[count]);
        }
        void ReleaseFocus() { if (Foreground == this) Foreground = null; }
        void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            ArmBarrier();
            InputModal = false; Runtime?.Close("对话对象不可用"); Runtime?.CancelCooperation(); ReleaseFocus();
        }
        void OnDestroy()
        {
            foreach (var prop in new[] { paintPickup, tentPatch }) if (prop != null) { Destroy(prop.GetComponent<Renderer>().sharedMaterial); Destroy(prop); }
            if (chineseFont != null) Destroy(chineseFont);
            if (chatIcon != null) Destroy(chatIcon);
            if (speechTail != null) Destroy(speechTail);
        }

        void OnGUI()
        {
            if (Runtime == null || player == null || !player.isActiveAndEnabled || Blocked || Time.timeScale <= 0) return;
            if (lineStyle == null)
            {
                lineStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, font = chineseFont, fontSize = fontSize };
                choiceStyle = new GUIStyle(lineStyle);
                smallStyle = new GUIStyle(lineStyle) { fontSize = 16 };
            }
            // Reference coordinates keep the list legible in small Game views without clipping.
            var matrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            var white = new Color(.96f,.94f,.86f);
            var gold = new Color(1f,.78f,.33f);
            if (Foreground == this)
            {

                if (Runtime.Session.phase == ConversationPhase.AwaitingResponse)
                {
                    float width = Mathf.Min(480,w * .34f), x = w - width - w * .05f;
                    float totalHeight = 0;
                    foreach (var slot in Runtime.Session.slots) totalHeight += ChoiceHeight(slot.text,width) + 12;
                    float y = Mathf.Max(48,Mathf.Min(h * .40f,h - 148 - totalHeight - 24));
                    if (Held)
                    {
                        for (int i = 0; i < Runtime.Session.slots.Count; i++)
                        {
                            var slot = Runtime.Session.slots[i];
                            bool expired = !string.IsNullOrEmpty(slot.disabledReason);
                            float height = ChoiceHeight(slot.text,width);
                            bool highlighted = holdArmed && i == selected;
                            var color = expired ? new Color(.64f,.63f,.59f,.65f) : highlighted ? gold : white;
                            if (highlighted) DrawText(new Rect(x-24,y,24,32),"›",choiceStyle,color);
                            DrawChoice(new Rect(x,y,width,height),slot.text,color,expired);
                            y += height + 12;
                        }
                    }
                    else foreach (var slot in Runtime.Session.slots)
                        y += ChoiceHeight(slot.text,width)+12;
                    if (Runtime.ResponseDuration > 0)
                    {
                        float remaining = 1 - Mathf.Clamp01((float)(Runtime.Session.responseElapsedSeconds / Runtime.ResponseDuration));
                        Bar(new Rect(x,y+12,width,4),new Color(1,1,1,.25f));
                        Bar(new Rect(x,y+12,width*remaining,4),remaining <= .34f ? new Color(1,.49f,.3f) : gold);
                    }
                }
            }
            if (npcSpeechLeft > 0 && Context().canHear)
                DrawHeadSpeech(transform,VisibleNpcSpeech,npcSpeechLeft,scale,w,lineStyle,white);
            if (playerSpeechLeft > 0)
                DrawHeadSpeech(player.transform,VisiblePlayerSpeech,playerSpeechLeft,scale,w,lineStyle,white);
            if (ChatAvailable && !Held && HeadPoint(player.transform,scale,out Vector2 bubble))
            {
                if (playerSpeechLeft > 0) bubble.y -= SpeechSize(VisiblePlayerSpeech,w,lineStyle).y+24;
                Color old = GUI.color;
                GUI.color = new Color(1,1,1,.85f + Mathf.Sin(Time.unscaledTime*2)*.1f);
                GUI.DrawTexture(new Rect(bubble.x-23,bubble.y-42,46,34),chatIcon);
                GUI.color = old;
            }            GUI.matrix = matrix;
        }
        bool HeadPoint(Transform subject,float scale,out Vector2 point)
        {
            point = default;
            var camera = Camera.main;
            if (camera == null) return false;
            var body = subject.GetComponent<Collider>();
            float top = body != null ? body.bounds.max.y : subject.position.y + 2;
            Vector3 screen = camera.WorldToScreenPoint(new Vector3(subject.position.x,top+.25f,subject.position.z));
            if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return false;
            point = new Vector2(screen.x/scale,(Screen.height-screen.y)/scale);
            return true;
        }
        void DrawHeadSpeech(Transform subject,string text,float remaining,float scale,float viewWidth,GUIStyle style,Color color)
        {
            if (text==null || !HeadPoint(subject,scale,out Vector2 point)) return;
            var content=new GUIContent(Spaced(text));
            Vector2 size=SpeechSize(text,viewWidth,style);
            float width=size.x,height=size.y;
            float alpha = Mathf.Clamp01(remaining/.65f);
            var body=new Rect(point.x-width*.5f,point.y-height-20,width,height);
            var fill=new Color(.97f,.95f,.88f,.95f*alpha);
            GUI.DrawTexture(body,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,fill,0,10);
            Color old=GUI.color;GUI.color=fill;
            // Body and tail share the character's exact horizontal anchor.
            GUI.DrawTexture(new Rect(point.x-9,body.yMax-1,18,13),speechTail);
            GUI.color=old;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor=new Color(.15f,.17f,.18f,alpha);
            GUI.Label(new Rect(body.x+14,body.y+9,width-28,height-18),content,style);
            style.alignment = TextAnchor.UpperLeft;
        }
        static Vector2 SpeechSize(string visibleText,float viewWidth,GUIStyle style)
        {
            var content=new GUIContent(Spaced(visibleText??string.Empty));
            float width=Mathf.Clamp(style.CalcSize(content).x+28,140,Mathf.Max(140,Mathf.Min(420,viewWidth*.5f)));
            return new Vector2(width,Mathf.Max(44,style.CalcHeight(content,width-28)+18));
        }
        static Texture2D CreateSpeechTail()
        {
            const int width=36,height=26;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Centered speech tail",filterMode=FilterMode.Bilinear};
            var pixels=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                if(Mathf.Abs(x-(width-1)*.5f)<=(y+.5f)*width/(2f*height))pixels[y*width+x]=Color.white;
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        static Texture2D CreateChatIcon()
        {
            // A small outlined speech bubble with three dots; independent of font glyph coverage.
            const int width = 128, height = 96;
            var texture = new Texture2D(width,height,TextureFormat.RGBA32,false) { name = "Dialogue availability bubble", filterMode = FilterMode.Bilinear };
            var pixels = new Color[width*height];
            for (int y=0;y<height;y++) for (int x=0;x<width;x++)
            {
                float ellipse = Mathf.Pow((x-63.5f)/57f,2)+Mathf.Pow((y-55)/34f,2);
                bool tail = y>=8 && y<36 && Mathf.Abs(x-63.5f)<=(y-8)*.65f;
                if (ellipse>1 && !tail) continue;
                bool inner = Mathf.Pow((x-63.5f)/52f,2)+Mathf.Pow((y-55)/29f,2)<1 || y>=16 && y<36 && Mathf.Abs(x-63.5f)<=(y-16)*.55f;
                Color color = inner ? new Color(.97f,.95f,.87f,.95f) : new Color(.15f,.16f,.16f,.9f);
                for (int dot=0;dot<3;dot++) if ((new Vector2(x-(42.5f+dot*21),y-55)).sqrMagnitude<25) color=new Color(.18f,.2f,.2f,1);
                pixels[y*width+x]=color;
            }
            texture.SetPixels(pixels); texture.Apply(false,true); return texture;
        }
        static string Spaced(string text)
        {
            if(string.IsNullOrEmpty(text))return text;
            var elements=System.Globalization.StringInfo.GetTextElementEnumerator(text);
            var result=new System.Text.StringBuilder();
            while(elements.MoveNext()){if(result.Length>0)result.Append('\u200A');result.Append(elements.GetTextElement());}
            return result.ToString();
        }
        float ChoiceHeight(string text,float width) => Mathf.Max(32,choiceStyle.CalcHeight(new GUIContent(Spaced(text)),width)+12);
        void DrawChoice(Rect rect,string text,Color color,bool expired)
        {
            DrawText(rect,text,choiceStyle,color);
            if (!expired || Event.current.type != EventType.Repaint) return;
            // Use the text renderer's actual wrapping, including Chinese and punctuation.
            text=Spaced(text);
            var content = new GUIContent(text);
            Vector2 start = choiceStyle.GetCursorPixelPosition(rect,content,0);
            Vector2 previous = start;
            for (int i = 1; i <= text.Length; i++)
            {
                Vector2 cursor = choiceStyle.GetCursorPixelPosition(rect,content,i);
                if (cursor.y > previous.y + 1)
                {
                    Bar(new Rect(start.x,start.y+choiceStyle.lineHeight*.52f,rect.xMax-start.x,1),color);
                    start = new Vector2(rect.x + choiceStyle.padding.left,cursor.y);
                }
                previous = cursor;
            }
            Bar(new Rect(start.x,start.y+choiceStyle.lineHeight*.52f,Mathf.Max(0,previous.x-start.x),1),color);
        }
        static void Bar(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color = old;
        }
        static void DrawText(Rect rect,string text,GUIStyle style,Color color)
        {
            text=Spaced(text);
            style.normal.textColor = new Color(.08f,.07f,.05f,.85f * color.a);
            GUI.Label(new Rect(rect.x+2,rect.y+2,rect.width,rect.height),text,style);
            GUI.Label(new Rect(rect.x-1,rect.y+1,rect.width,rect.height),text,style);
            style.normal.textColor = color; GUI.Label(rect,text,style);
        }
        public void Save()
        {
            try { DialogueSaveStore.Save(SavePath,Runtime.State);Feedback("已保存钴蓝的对话、物资与约定。"); }
            catch(Exception e) { Feedback("保存失败，原存档已保留。");Debug.LogException(e,this); }
        }
        public void Load()
        {
            try
            {
                var loaded=DialogueSaveStore.Load(SavePath,Runtime.Content);
                Runtime=loaded;ReleaseFocus();ArmBarrier();
                if(DayNightSystem.Instance!=null)DayNightSystem.Instance.SetTime((float)(Runtime.State.worldHours%24));
                Feedback("已读取。");
            }
            catch(Exception e) { Feedback("读取失败，当前状态和原存档均已保留。");Debug.LogException(e,this); }
        }
    }
}
