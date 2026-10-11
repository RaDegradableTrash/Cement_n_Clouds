#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;
using Voyage.Exploration;

// Runs only when explicitly installed by the editor validation menu.
[DefaultExecutionOrder(-50)]
public sealed class CharacterGroundingRuntimeProbe : MonoBehaviour
{
    public ExplorerPlayer player;
    public float Elapsed { get; private set; }
    public bool RequestedSprint { get; private set; }
    bool jumped;
    void Update()
    {
        Elapsed += Time.deltaTime;
        float t = Elapsed;
        bool walking = t > 1 && t < 3, running = t >= 3 && t < 5;
        if(t>=9 && t<13){running=(int)((t-9)/.18f)%2==0;walking=!running;}
        RequestedSprint=running;
        bool jump = t >= 5.5f && !jumped;
        if (jump) jumped = true;
        player.Move(walking || running ? Vector2.up : Vector2.zero, jump, Time.deltaTime, running);
        if (t > 7 && t < 9) player.transform.Rotate(Vector3.up, 70 * Time.deltaTime);
    }
}

[DefaultExecutionOrder(300)]
public sealed class CharacterGroundingRuntimeObserver : MonoBehaviour
{
    public CharacterGroundingRuntimeProbe probe;
    public CharacterGrounding grounding;
    public Animator animator;
    public Transform left, right;
    Vector3 lastLeft, lastRight;
    bool leftWasPlanted, rightWasPlanted, airborneSeen, jumpAnimationSeen, landedAfterJump, turnSeen;
    int samples, contacts, badAirContacts;
    float maxSlip;
    bool done;
    bool previousSprint;
    float switchedAt;
    int sprintSwitches, responseFailures;
    bool responseChecked;
    float worstResponseError;
    void LateUpdate()
    {
        if (done || !grounding.Ready) return;
        samples++;
        if(probe.Elapsed>=9 && probe.Elapsed<13){
            if(probe.RequestedSprint!=previousSprint){
                previousSprint=probe.RequestedSprint;switchedAt=probe.Elapsed;
                responseChecked=false;sprintSwitches++;
            }
            if(!responseChecked && probe.Elapsed-switchedAt>=.10f){
                responseChecked=true;
                float error=Mathf.Abs(animator.GetFloat("Locomotion")-(probe.RequestedSprint?2:1));
                worstResponseError=Mathf.Max(worstResponseError,error);
                if(error>.15f || !animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))responseFailures++;
            }
        }
        var player = probe.player;
        if (!player.IsGrounded && probe.Elapsed > 5.5f)
        {
            airborneSeen = true;
            if (grounding.LeftPlanted || grounding.RightPlanted) badAirContacts++;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("JumpStart") || state.IsName("JumpRise") || state.IsName("JumpAir") || state.IsName("JumpFall")) jumpAnimationSeen = true;
        }
        if (airborneSeen && player.IsGrounded && grounding.LeftPlanted && grounding.RightPlanted) landedAfterJump = true;
        if (probe.Elapsed > 7 && grounding.TurningStep) turnSeen = true;
        if (leftWasPlanted && grounding.LeftPlanted) maxSlip = Mathf.Max(maxSlip, Vector3.Distance(lastLeft, left.position));
        if (rightWasPlanted && grounding.RightPlanted) maxSlip = Mathf.Max(maxSlip, Vector3.Distance(lastRight, right.position));
        if (!leftWasPlanted && grounding.LeftPlanted) contacts++;
        if (!rightWasPlanted && grounding.RightPlanted) contacts++;
        lastLeft = left.position; lastRight = right.position;
        leftWasPlanted = grounding.LeftPlanted; rightWasPlanted = grounding.RightPlanted;
        if (probe.Elapsed < 14) return;
        done = true;
        var report = new StringBuilder();
        report.AppendLine($"samples={samples}, landings={contacts}, supportSlip={maxSlip:F4}, airborneContacts={badAirContacts}");
        report.AppendLine($"airborne={airborneSeen}, jumpAnimation={jumpAnimationSeen}, relanded={landedAfterJump}, turnStep={turnSeen}");
        report.AppendLine($"rapidSprintSwitches={sprintSwitches}, responseFailures={responseFailures}, worstBlendErrorAt100ms={worstResponseError:F3}");
        bool pass = samples > 30 && contacts > 6 && maxSlip < .045f && badAirContacts == 0 && airborneSeen && jumpAnimationSeen && landedAfterJump && turnSeen && sprintSwitches>=20 && responseFailures==0;
        report.AppendLine(pass ? "PASS runtime integration" : "FAIL runtime integration");
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/GroundingRuntime.txt", report.ToString());
        Debug.Log("GROUNDING RUNTIME: " + report);
        UnityEditor.EditorApplication.isPlaying = false;
    }
}
#endif
