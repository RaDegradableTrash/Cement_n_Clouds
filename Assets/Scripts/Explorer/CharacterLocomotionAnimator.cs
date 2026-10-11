using UnityEngine;

namespace Voyage.Exploration
{
    // Run after movement: actual displacement also stops the gait against walls
    // and while dialogue/input blocks the player's movement.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class CharacterLocomotionAnimator : MonoBehaviour
    {
        public Animator animator;
        [Tooltip("Approximate world speeds represented by the authored walk/run strides.")]
        public float referenceWalkSpeed = 1.4f;
        public float referenceRunSpeed = 3.8f;
        static readonly int Locomotion = Animator.StringToHash("Locomotion");
        static readonly int Grounded = Animator.StringToHash("Grounded");
        static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        static readonly int AirTime = Animator.StringToHash("AirTime");
        static readonly int Jump = Animator.StringToHash("Jump");
        static readonly int GaitRate = Animator.StringToHash("GaitRate");
        ExplorerPlayer player;
        Vector3 previousPosition;
        int lastJump;
        float airTime;
        int previousDismount = -1;
        bool gesture;
        float gestureUntil;
        Transform leftFoot, rightFoot;
        int standingJumpVariant;
        public int JumpLead { get; private set; } // -1 left first, +1 right first, 0 together

        public int SelectJumpLead(bool moving)
        {
            if (!moving)
            {
                JumpLead = standingJumpVariant % 3 - 1;
                standingJumpVariant++;
                return JumpLead;
            }
            if (leftFoot == null || rightFoot == null)
                foreach (var bone in animator.GetComponentsInChildren<Transform>())
                {
                    if (bone.name == "Foot.L") leftFoot = bone;
                    if (bone.name == "Foot.R") rightFoot = bone;
                }
            if (leftFoot == null || rightFoot == null) return JumpLead = 0;
            var grounding = GetComponent<CharacterGrounding>();
            bool leftSupport = grounding != null && grounding.LeftPlanted;
            bool rightSupport = grounding != null && grounding.RightPlanted;
            if (leftSupport != rightSupport) return JumpLead = leftSupport ? 1 : -1;
            // Double support: the forward contact pushes off. During the brief
            // flight/landing boundary use the lower foot, then the forward foot.
            float height = leftFoot.position.y - rightFoot.position.y;
            bool pushLeft = !leftSupport && Mathf.Abs(height) > .025f
                ? height < 0 : Vector3.Dot(leftFoot.position-rightFoot.position, transform.forward) >= 0;
            return JumpLead = pushLeft ? 1 : -1;
        }
        int Gait => animator.GetFloat(Locomotion) < .15f ? 0 : animator.GetFloat(Locomotion) < 1.5f ? 1 : 2;
        public bool PlayGesture(string name, float seconds = 2.4f)
        {
            if (animator == null || player != null && (!player.IsGrounded || player.ControlLocked)) return false;
            if (!PlayPhaseTransition(name))
            {
                int state = Animator.StringToHash("NPC_" + name);
                if (!animator.HasState(0, state)) return false;
                animator.CrossFadeInFixedTime(state, .16f);
            }
            gesture = true; gestureUntil = Time.time + seconds; return true;
        }
        bool PlayPhaseTransition(string destination)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Locomotion") || Gait == 0) return false;
            int count = Gait == 1 ? 32 : 24;
            int frame = Mathf.RoundToInt(Mathf.Repeat(state.normalizedTime, 1) * count) % count;
            string name = $"PHASE_{(Gait == 1 ? "Walk" : "Run")}_{frame:00}_To_{destination}";
            int hash = Animator.StringToHash(name);
            if (!animator.HasState(0, hash)) return false;
            animator.ResetTrigger(Jump);
            animator.CrossFadeInFixedTime(hash, .035f, 0, 0);
            return true;
        }

        void Awake() => player = GetComponent<ExplorerPlayer>();

        void OnEnable()
        {
            previousPosition = transform.position;
            if (player == null) player = GetComponent<ExplorerPlayer>();
            lastJump = player != null ? player.JumpSequence : 0;
            airTime = 0;
            previousDismount = -1; gesture = false;
            if (animator != null)
            {
                animator.ResetTrigger(Jump);
                animator.SetFloat(Locomotion, 0);
                animator.SetBool(Grounded, true);
                animator.SetFloat(VerticalSpeed, 0);
                animator.SetFloat(AirTime, 0);
                animator.SetFloat(GaitRate, 1);
                // Reappearing after driving must not resume an old airborne state.
                animator.Play("Locomotion", 0, 0);
            }
        }

        void Update()
        {
            Vector3 displacement = transform.position - previousPosition;
            previousPosition = transform.position;
            if (animator == null || Time.deltaTime <= 0) return;
            float speed = Vector3.ProjectOnPlane(displacement, Vector3.up).magnitude / Time.deltaTime;
            float walk = player != null ? Mathf.Max(.1f, player.moveSpeed) : 4.5f;
            float run = player != null ? Mathf.Max(walk + .1f, player.sprintSpeed) : 8f;
            // Activation/teleporting is not a running step.
            if (speed > run * 3) speed = 0;
            float gait = speed < .05f ? 0 : speed <= walk
                ? speed / walk : 1 + Mathf.InverseLerp(walk, run, speed);
            animator.SetFloat(Locomotion, gait, .035f, Time.deltaTime);
            float authoredSpeed = Mathf.Lerp(referenceWalkSpeed, referenceRunSpeed, Mathf.Clamp01(gait - 1));
            float rate = speed < .05f ? 1 : Mathf.Clamp(speed / Mathf.Max(.1f, authoredSpeed), .65f, 2.2f);
            animator.SetFloat(GaitRate, rate, .035f, Time.deltaTime);
            bool grounded = player == null || player.IsGrounded;
            airTime = grounded ? 0 : airTime + Time.deltaTime;
            animator.SetBool(Grounded, grounded);
            animator.SetFloat(VerticalSpeed, player != null ? player.VerticalSpeed : 0);
            animator.SetFloat(AirTime, airTime);
            if (player != null && player.ControlLocked)
            {
                int phase = player.DismountPhase;
                if (phase != previousDismount)
                {
                    animator.ResetTrigger(Jump);
                    animator.CrossFadeInFixedTime(phase == 0 ? "DismountStart" : phase == 1 ? "DismountAir" : "DismountLand", .035f);
                    previousDismount = phase;
                }
                lastJump = player.JumpSequence; return;
            }
            if (previousDismount >= 0)
            {
                previousDismount = -1; animator.CrossFadeInFixedTime("Locomotion", .08f);
            }
            if (player != null && player.JumpSequence != lastJump)
            {
                lastJump = player.JumpSequence;
                // Only an accepted physics jump triggers takeoff; a midair key
                // press or simply walking over a ledge must not restart it.
                gesture = false;
                SelectJumpLead(speed > .2f);
                foreach (var parameter in animator.parameters)
                    if (parameter.name == "JumpLead") animator.SetFloat("JumpLead", JumpLead);
                if (!PlayPhaseTransition("Jump")) animator.SetTrigger(Jump);
                return;
            }
            int desiredGait = gait < .15f ? 0 : gait < 1.5f ? 1 : 2;
            if (gesture && (desiredGait > 0 || Time.time >= gestureUntil))
            { gesture = false; animator.CrossFadeInFixedTime("Locomotion", .12f); }
            // The locomotion blend tree keeps one continuous stride phase. A
            // finite walk/run transition cannot follow rapid sprint reversals.
            // Reserve authored phase transitions for gestures and takeoff.
        }
    }
}
