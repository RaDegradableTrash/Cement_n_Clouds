using System;
using UnityEngine;

namespace Voyage.Exploration
{
    // Generic-rig post-animation layer. It never moves the controller/collider.
    // Restore our previous correction before Animator evaluates the next pose.
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class CharacterGrounding : MonoBehaviour
    {
        public Animator animator;
        public LayerMask groundMask = Physics.DefaultRaycastLayers;
        [Range(0, 1)] public float weight = 1;
        [Min(0)] public float probeUp = .4f;
        [Min(0)] public float probeDown = .45f;
        [Range(0, 65)] public float maximumSlope = 50;
        [Min(0)] public float pelvisTravel = .22f;
        [Min(0)] public float turnStepHeight = .10f;
        [Min(.1f)] public float turnStepDuration = .24f;
        [Range(5, 70)] public float turnStepAngle = 27;
        [Min(30)] public float standingTurnSpeed = 150;
        [Min(90)] public float movingTurnSpeed = 540;
        public bool drawContacts;

        public bool Ready { get; private set; }
        public bool LeftPlanted => Ready && legs[0].planted && legs[0].settled;
        public bool RightPlanted => Ready && legs[1].planted && legs[1].settled;
        public bool TurningStep => stepping >= 0;
        public Vector3 LeftTarget => Ready ? legs[0].target : transform.position;
        public Vector3 RightTarget => Ready ? legs[1].target : transform.position;

        sealed class Leg
        {
            public Transform thigh, shin, foot;
            public Vector3 rest, target, normal = Vector3.up, animated, releasePoint;
            public float releaseTime, contactTime, previousForward, forwardSpeed, softReach, softReachVelocity;
            public bool sampledForward;
            public Quaternion restRotation, animatedRotation, targetRotation;

            public Vector3 solvedTarget, targetVelocity;
            public bool targetInitialized;
            public float sole, length, lift, previousLift, blend;
            public bool planted, valid, waitForSwing, settled;
            public Transform platform;
            public Vector3 anchor;
            public Quaternion anchorRotation;
        }

        readonly Leg[] legs = { new Leg(), new Leg() };
        readonly RaycastHit[] hits = new RaycastHit[32];
        Transform pelvis;
        ExplorerPlayer player;
        Transform[] corrected;
        Vector3[] positions;
        Quaternion[] rotations;
        bool poseSaved, initializedPosition, wasGrounded;
        Vector3 previousRoot, previousVelocity;
        float pelvisOffset, landingCompression, lastVerticalSpeed;
        float landingStagger;
        int landingLead;
        Vector3 bodyLean, supportShift;
        Vector3 travelDirection;
        int stepping = -1, lastStep = 1;
        float stepTime;
        Vector3 stepStart, stepEnd;
        Quaternion stepStartRotation, stepEndRotation;
        Quaternion modelBindRotation, visualRotation;
        Quaternion Heading => animator.transform.rotation * Quaternion.Inverse(modelBindRotation);
        Vector3 RestPoint(Leg leg) => transform.position + Heading * Vector3.Scale(leg.rest, transform.lossyScale);

        public bool Initialize()
        {
            if (Ready) return true;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) return false;
            modelBindRotation = animator.transform.localRotation;
            var bones = animator.GetComponentsInChildren<Transform>();
            Transform Bone(string name) => Array.Find(bones, t => t.name == name);
            legs[0].foot = Bone("Foot.L"); legs[1].foot = Bone("Foot.R");
            if (legs[0].foot == null || legs[1].foot == null) return false;
            for (int i = 0; i < 2; i++)
            {
                var l = legs[i];
                l.shin = l.foot.parent; l.thigh = l.shin != null ? l.shin.parent : null;
                if (l.thigh == null) return false;
                l.rest = transform.InverseTransformPoint(l.foot.position);
                l.sole = Mathf.Clamp(l.foot.position.y - transform.position.y, .015f, .14f);
                l.restRotation = Quaternion.Inverse(transform.rotation) * l.foot.rotation;
                l.length = Vector3.Distance(l.thigh.position, l.shin.position) + Vector3.Distance(l.shin.position, l.foot.position);
            }
            pelvis = legs[0].thigh.parent;
            if (pelvis == null || !legs[1].thigh.IsChildOf(pelvis)) return false;
            corrected = new[] { animator.transform, pelvis, legs[0].thigh, legs[0].shin, legs[0].foot, legs[1].thigh, legs[1].shin, legs[1].foot };
            positions = new Vector3[corrected.Length]; rotations = new Quaternion[corrected.Length];
            player = GetComponent<ExplorerPlayer>();
            Ready = true;
            ResetContacts();
            return true;
        }

        void Start() => Initialize();
        void OnEnable() => ResetContacts();
        void OnDisable() { RestoreAnimationPose(); ResetContacts(); }
        void Update() => RestoreAnimationPose();
        void LateUpdate()
        {
            if (!Initialize() || Time.deltaTime <= 0 || !animator.enabled || !animator.gameObject.activeInHierarchy) return;
            // NPCs have no controller: the individual foot probes determine support.
            Evaluate(Time.deltaTime, player == null || player.IsGrounded, player != null ? player.VerticalSpeed : 0);
        }

        public void RestoreAnimationPose()
        {
            if (!poseSaved) return;
            for (int i = 0; i < corrected.Length; i++)
                if (corrected[i] != null) corrected[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
            poseSaved = false;
        }

        public void ResetContacts()
        {
            stepping = -1; initializedPosition = false;
            pelvisOffset = landingCompression = landingStagger = 0; previousVelocity = bodyLean = supportShift = Vector3.zero; wasGrounded = false;
            if (animator != null) visualRotation = animator.transform.rotation;
            foreach (var l in legs) { l.planted = l.valid = l.waitForSwing = false; l.blend = 0; l.platform = null; l.releaseTime = 0; l.sampledForward = false; l.contactTime = l.softReach = l.softReachVelocity = 0; l.targetInitialized = false; l.targetVelocity = Vector3.zero; }
        }

        // Explicit delta and physics state also allow deterministic editor validation.
        // Call RestoreAnimationPose, sample the animation, then Evaluate.
        public void Evaluate(float delta, bool grounded, float verticalSpeed)
        {
            if (!Initialize() || delta <= 0) return;
            if (poseSaved) RestoreAnimationPose();
            Vector3 rootDelta = initializedPosition ? transform.position - previousRoot : Vector3.zero;
            bool teleported = rootDelta.magnitude > Mathf.Max(1, 20 * delta);
            if (teleported) ResetContacts();
            Vector3 velocity = teleported ? Vector3.zero : Vector3.ProjectOnPlane(rootDelta / delta, Vector3.up);
            previousRoot = transform.position; initializedPosition = true;
            float speed = velocity.magnitude;
            travelDirection = speed > .01f ? velocity / speed : Heading * Vector3.forward;
            for (int i = 0; i < corrected.Length; i++)
            { positions[i] = corrected[i].localPosition; rotations[i] = corrected[i].localRotation; }
            poseSaved = true;
            visualRotation = Quaternion.RotateTowards(visualRotation, animator.transform.rotation,
                (speed < .18f ? standingTurnSpeed : movingTurnSpeed) * delta);
            animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, visualRotation, weight);
            foreach (var l in legs)
            {
                l.animated = l.foot.position; l.animatedRotation = l.foot.rotation;
                float forward = Vector3.Dot(l.animated - transform.position, Heading * Vector3.forward);
                l.forwardSpeed = l.sampledForward ? (forward - l.previousForward) / delta : 0;
                l.previousForward = forward; l.sampledForward = true;
                l.previousLift = l.lift;
                l.lift = Mathf.Max(0, l.animated.y - transform.position.y - l.sole);
            }
            if (!grounded || weight <= 0)
            {
                // No stale support constraints during takeoff, a fall, or teleport.
                foreach (var l in legs) { l.planted = false; l.blend = 0; l.valid = false; l.waitForSwing = false; l.releaseTime = 0; l.sampledForward = false; l.contactTime = l.softReach = l.softReachVelocity = 0; l.targetInitialized = false; l.targetVelocity = Vector3.zero; }
                stepping = -1; pelvisOffset = 0; landingCompression = 0; supportShift = Vector3.zero;
                previousVelocity = velocity; lastVerticalSpeed = verticalSpeed; wasGrounded = false;
                return;
            }
            if (!wasGrounded && lastVerticalSpeed < -1)
            {
                landingCompression = Mathf.Min(.09f, -lastVerticalSpeed * .012f);
                var driver = GetComponent<CharacterLocomotionAnimator>();
                landingLead = driver != null ? driver.JumpLead : 0;
                landingStagger = landingLead == 0 ? 0 : .14f;
            }
            else landingStagger = Mathf.Max(0, landingStagger - delta);
            wasGrounded = true;
            lastVerticalSpeed = verticalSpeed;
            landingCompression = Mathf.MoveTowards(landingCompression, 0, delta * .35f);
            bool stationary = speed < .18f;
            if (!stationary) stepping = -1;
            for (int i = 0; i < 2; i++) UpdateContact(legs[i], stationary, delta, i);
            if (stationary) UpdateTurnStep(delta);

            foreach (var l in legs)
                if (l.valid && l.blend > .001f)
                {
                    // Contact envelopes keep both endpoints continuous without
                    // delaying the entire swing behind the moving character.
                    l.contactTime = l.planted ? l.contactTime + delta : 0;
                    l.softReach = Mathf.SmoothDamp(l.softReach, stationary ? 0 : l.length * .18f, ref l.softReachVelocity, .08f, Mathf.Infinity, delta);
                    float contact = stationary ? 1 : Mathf.SmoothStep(0, 1, l.contactTime / .055f);
                    var approach = new Vector3(l.animated.x, l.target.y + SoleClearance(l.lift), l.animated.z);
                    var target = l.planted ? Vector3.Lerp(approach, l.target, contact) : l.target;
                    target = Vector3.Lerp(l.animated, target, l.blend * weight);
                    if (!l.targetInitialized || stationary)
                    {
                        l.solvedTarget = target; l.targetVelocity = Vector3.zero; l.targetInitialized = true;
                    }
                    else
                        l.solvedTarget = Vector3.SmoothDamp(l.solvedTarget, target, ref l.targetVelocity, .022f, Mathf.Infinity, delta);
                }
            float lower = 0;
            foreach (var l in legs)
                if (l.valid)
                {
                    if (stationary) lower = Mathf.Min(lower, (l.solvedTarget.y - l.animated.y) * l.blend);
                    // Lower the pelvis before either chain reaches full extension.
                    float horizontal = Vector3.ProjectOnPlane(l.thigh.position - l.solvedTarget, Vector3.up).magnitude;
                    float reach = l.length * .985f;
                    float verticalReach = Mathf.Sqrt(Mathf.Max(.01f, reach * reach - horizontal * horizontal));
                    lower = Mathf.Min(lower, l.solvedTarget.y + verticalReach - l.thigh.position.y);
                }
            float travel = stationary ? pelvisTravel : Mathf.Min(pelvisTravel, .10f);
            float desiredOffset = Mathf.Clamp(Mathf.Min(lower, stationary ? 0 : -.08f) - landingCompression, -travel, travel);
            pelvisOffset = Mathf.Lerp(pelvisOffset, desiredOffset, 1 - Mathf.Exp(-18 * delta));
            pelvis.position += Vector3.up * pelvisOffset * weight;
            Vector3 desiredShift = stepping >= 0
                ? Vector3.ClampMagnitude(Vector3.ProjectOnPlane(legs[1 - stepping].target - pelvis.position, Vector3.up), .035f)
                : Vector3.zero;
            supportShift = Vector3.Lerp(supportShift, desiredShift, 1 - Mathf.Exp(-14 * delta));
            pelvis.position += supportShift * weight;

            // A small acceleration/turn lean, not an arbitrary alternating sway.
            Vector3 acceleration = (velocity - previousVelocity) / Mathf.Max(delta, .001f);
            previousVelocity = velocity;
            Vector3 desiredLean = Vector3.ClampMagnitude(acceleration * .0025f, .065f);
            bodyLean = Vector3.Lerp(bodyLean, desiredLean, 1 - Mathf.Exp(-8 * delta));
            pelvis.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.up + bodyLean * weight) * pelvis.rotation;
            // Keep stance targets reachable, carrying the correction into the
            // pelvis filter so it does not spring back on the following frame.
            float reachCorrection = 0;
            foreach (var l in legs)
                if (l.valid && l.blend > .001f)
                {
                    float horizontal = Vector3.ProjectOnPlane(l.thigh.position - l.solvedTarget, Vector3.up).magnitude;
                    float reach = l.length * .985f;
                    float verticalReach = Mathf.Sqrt(Mathf.Max(.0001f, reach * reach - horizontal * horizontal));
                    reachCorrection = Mathf.Min(reachCorrection, l.solvedTarget.y + verticalReach - l.thigh.position.y);
                }
            reachCorrection = stationary ? Mathf.Max(-pelvisTravel - pelvisOffset, reachCorrection) : 0;
            pelvis.position += Vector3.up * reachCorrection * weight;
            pelvisOffset += reachCorrection;
            foreach (var l in legs)
                if (l.valid && l.blend > .001f)
                {
                    var target = l.solvedTarget;
                    // An animated knee becomes an unstable pole near extension:
                    // its projection crosses the hip/ankle axis and flips the bend.
                    // Keep a consistent forward bend plane throughout the stride.
                    SolveTwoBone(l.thigh, l.shin, l.foot, target, l.thigh.position + Heading * Vector3.forward * l.length, l.softReach);
                    // A captured landing target is not full support until the
                    // animated chain has finished settling onto it.
                    l.settled = Vector3.Distance(l.foot.position, l.target) < .012f;
                    l.foot.rotation = Quaternion.Slerp(l.animatedRotation, l.targetRotation, l.blend * weight);
                }
        }

        // Meet the ground with zero vertical velocity, rather than hard-clamping
        // a descending foot and immediately resetting it below the floor on plant.
        static float SoleClearance(float lift)
        {
            const float cushion = .04f;
            return lift < cushion ? lift * lift / (2 * cushion) : lift - cushion * .5f;
        }

        void UpdateContact(Leg l, bool stationary, float delta, int index)
        {
            // Do not flatten the authored second-foot landing into a two-foot
            // plant when the controller first touches down.
            bool trailingLanding = landingStagger > 0 && (landingLead < 0 ? index == 1 : index == 0);
            Vector3 candidate = stationary && !trailingLanding ? RestPoint(l) : l.animated;
            bool found = Probe(candidate, out var hit);
            bool supportedAnchor = false;
            if (l.planted)
            {
                if (l.platform == null) l.planted = false;
                else
                {
                    l.target = l.platform.TransformPoint(l.anchor);
                    l.targetRotation = l.platform.rotation * l.anchorRotation;
                    // Probe the planted location, not the next animated foothold.
                    // The swing animation may already point across a ledge or step.
                    supportedAnchor = l.platform.gameObject.activeInHierarchy && Probe(l.target, out var anchorHit) && anchorHit.transform == l.platform;
                    float separation = Vector3.ProjectOnPlane(l.target - l.thigh.position, Vector3.up).magnitude;
                    float size = l.length / .57f;
                    float trailing = Vector3.Dot(l.thigh.position - l.target, travelDirection);
                    if (!stationary && (l.lift > .035f * size || trailing > l.length * .35f || separation > l.length * .85f))
                    {
                        l.planted = false; l.waitForSwing = true;
                        l.releasePoint = l.solvedTarget;
                        l.releaseTime = .06f;
                    }
                    // Destroyed/disabled ground must not leave an invisible foothold.
                    if (!supportedAnchor) l.planted = false;
                }
            }
            l.valid = found || l.planted && supportedAnchor;
            if (!l.valid) { l.blend = 0; l.platform = null; return; }
            if (!found) return;
            Vector3 projected = hit.point + hit.normal * l.sole;
            var aligned = Quaternion.FromToRotation(Vector3.up, hit.normal) * (Heading * l.restRotation);
            l.normal = hit.normal;
            if (l.waitForSwing && l.lift > .025f * l.length / .57f) l.waitForSwing = false;
            if (!l.planted && index != stepping)
            {
                float clearance = stationary && !trailingLanding ? 0 : SoleClearance(l.lift);
                l.target = projected + Vector3.up * clearance;
                if (l.releaseTime > 0)
                {
                    // Preserve the last contact at toe-off, then hand it back to
                    // the authored swing over time instead of teleporting the foot.
                    float t = l.releaseTime / .06f;
                    l.target = Vector3.Lerp(l.target, l.releasePoint, t * t * (3 - 2 * t));
                    l.releaseTime = Mathf.Max(0, l.releaseTime - delta);
                }
                l.targetRotation = Quaternion.Slerp(l.animatedRotation, aligned, 1 - Mathf.Clamp01(clearance / .12f));
                bool landing = l.lift < .018f * l.length / .57f && l.lift <= l.previousLift + .002f && l.forwardSpeed <= .1f;
                if (!trailingLanding && (stationary || landing && !l.waitForSwing)) Plant(l, projected, aligned, hit.transform);
            }
            l.blend = Mathf.MoveTowards(l.blend, 1, delta * 12);
        }

        void Plant(Leg l, Vector3 point, Quaternion rotation, Transform platform)
        {
            l.target = point; l.targetRotation = rotation;
            l.platform = platform; l.anchor = platform.InverseTransformPoint(point);
            l.anchorRotation = Quaternion.Inverse(platform.rotation) * rotation;
            l.planted = true; l.waitForSwing = false; l.blend = 1;
            l.releaseTime = 0; l.contactTime = 0;
        }

        void UpdateTurnStep(float delta)
        {
            float YawError(Leg l)
            {
                Vector3 facing = l.targetRotation * Quaternion.Inverse(l.restRotation) * Vector3.forward;
                return Vector3.Angle(Vector3.ProjectOnPlane(facing, Vector3.up), Heading * Vector3.forward);
            }
            if (stepping < 0 && legs[0].planted && legs[1].planted)
            {
                float Error(Leg l) => Vector3.ProjectOnPlane(RestPoint(l) - l.target, Vector3.up).magnitude
                    + YawError(l) / 180f;
                int next = Error(legs[0]) > Error(legs[1]) ? 0 : 1;
                if (Mathf.Abs(Error(legs[0]) - Error(legs[1])) < .025f) next = 1 - lastStep;
                var l = legs[next];
                Vector3 desired = RestPoint(l);
                if ((Vector3.ProjectOnPlane(desired - l.target, Vector3.up).magnitude > .1f ||
                    YawError(l) > turnStepAngle) && Probe(desired, out var hit))
                {
                    stepping = next; stepTime = 0; stepStart = l.target; stepStartRotation = l.targetRotation;
                    stepEnd = hit.point + hit.normal * l.sole;
                    stepEndRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Heading * l.restRotation;
                    l.planted = false;
                }
            }
            if (stepping < 0) return;
            var moving = legs[stepping];
            if (!moving.valid) { stepping = -1; return; }
            stepTime += delta;
            float t = Mathf.Clamp01(stepTime / turnStepDuration);
            float smooth = t * t * (3 - 2 * t);
            moving.target = Vector3.Lerp(stepStart, stepEnd, smooth) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * turnStepHeight);
            moving.targetRotation = Quaternion.Slerp(stepStartRotation, stepEndRotation, smooth);
            if (t >= 1)
            {
                if (Probe(stepEnd, out var hit)) Plant(moving, hit.point + hit.normal * moving.sole, stepEndRotation, hit.transform);
                lastStep = stepping; stepping = -1;
            }
        }

        bool Probe(Vector3 point, out RaycastHit best)
        {
            Vector3 origin = new Vector3(point.x, transform.position.y + probeUp, point.z);
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits, probeUp + probeDown, groundMask, QueryTriggerInteraction.Ignore);
            best = default; float distance = float.PositiveInfinity;
            float minNormal = Mathf.Cos(maximumSlope * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform) || hit.normal.y < minNormal || hit.distance >= distance) continue;
                best = hit; distance = hit.distance;
            }
            return !float.IsPositiveInfinity(distance);
        }

        public static void SolveTwoBone(Transform hip, Transform knee, Transform ankle, Vector3 target, Vector3 pole, float softReach = 0)
        {
            Vector3 origin = hip.position;
            float a = Vector3.Distance(origin, knee.position), b = Vector3.Distance(knee.position, ankle.position);
            if (a < .0001f || b < .0001f) return;
            Vector3 direction = target - origin;
            if (direction.sqrMagnitude < .0000001f) return;
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .0001f, (a + b) * .985f);
            if (softReach > 0)
            {
                float threshold = (a + b) * .985f - softReach;
                float requested = direction.magnitude;
                if (requested > threshold)
                    distance = threshold + softReach * (1 - Mathf.Exp(-(requested - threshold) / softReach));
            }
            direction.Normalize();
            Vector3 bend = Vector3.ProjectOnPlane(pole - origin, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(Vector3.forward, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(Vector3.right, direction);
            bend.Normalize();
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            Vector3 joint = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            hip.rotation = Quaternion.FromToRotation(knee.position - origin, joint - origin) * hip.rotation;
            Vector3 end = origin + direction * distance;
            knee.rotation = Quaternion.FromToRotation(ankle.position - knee.position, end - knee.position) * knee.rotation;
        }

        void OnDrawGizmosSelected()
        {
            if (!drawContacts || !Ready) return;
            foreach (var l in legs)
            {
                Gizmos.color = l.planted ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(l.target, .035f);
                Gizmos.DrawLine(l.target, l.target + l.normal * .2f);
            }
        }
    }
}
