using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Voyage.Exploration;

public static class GaitContinuityValidation
{
    [MenuItem("Voyage/Validate Stride Repair")]
    public static void ValidateRepair()
    {
        Run();
        EditorApplication.ExecuteMenuItem("Voyage/Validate Lullaby Character");
        EditorApplication.ExecuteMenuItem("Voyage/Validate Character Revision");
    }
    [MenuItem("Voyage/Validate Gait Continuity")]
    public static void Run()
    {
        var root = new GameObject("Gait continuity validation");
        root.transform.position = new Vector3(4000, 1000, 4000);
        var report = new StringBuilder();
        try
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0, -.5f, 30);
            floor.transform.localScale = new Vector3(20, 1, 100);
            var player = Object.Instantiate(Resources.Load<GameObject>("Prefabs/ExplorerPlayer"), root.transform);
            player.transform.localPosition = Vector3.zero;
            var a = player.GetComponentInChildren<Animator>();
            var g = player.GetComponent<CharacterGrounding>();
            g.Initialize();
            var bones = a.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Thigh.") || t.name.StartsWith("Shin.")).ToArray();
            var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().ToArray();
            Physics.SyncTransforms();
            foreach (string name in new[] { "VOY_Walk", "VOY_Run" })
            {
                g.RestoreAnimationPose(); player.transform.localPosition = Vector3.zero; g.ResetContacts();
                var clip = clips.Single(c => c.name == name);
                float speed = name == "VOY_Walk" ? 4.5f : 8f;
                float rate = speed / (name == "VOY_Walk" ? 2.9f : 5.2f);
                Quaternion[] lastRaw = null, lastSolved = null;
                Vector3[] previousRawVelocity = null, previousSolvedVelocity = null;
                float rawVelocityJump = 0, solvedVelocityJump = 0; string velocityDetail = "";
                float rawMax = 0, solvedMax = 0; int worstFrame = 0; string detail = "";
                for (int i = 0; i < 480; i++)
                {
                    g.RestoreAnimationPose(); player.transform.position += Vector3.forward * speed / 120f;
                    clip.SampleAnimation(a.gameObject, i / 120f * rate % clip.length);
                    var raw = bones.Select(b => b.localRotation).ToArray();
                    g.Evaluate(1f / 120, true, -2);
                    var solved = bones.Select(b => b.localRotation).ToArray();
                    Vector3 Velocity(Quaternion from, Quaternion to)
                    {
                        var q = to * Quaternion.Inverse(from);
                        q.ToAngleAxis(out float angle, out Vector3 axis);
                        if (angle > 180) angle -= 360;
                        return Mathf.Abs(angle) < .001f ? Vector3.zero : axis * angle * 120;
                    }
                    if (lastRaw != null)
                    {
                        var rv = bones.Select((b,j) => Velocity(lastRaw[j], raw[j])).ToArray();
                        var sv = bones.Select((b,j) => Velocity(lastSolved[j], solved[j])).ToArray();
                        if (i > 120 && previousRawVelocity != null)
                            for (int j=0;j<bones.Length;j++)
                            {
                                rawVelocityJump = Mathf.Max(rawVelocityJump, (rv[j]-previousRawVelocity[j]).magnitude);
                                                                float change = (sv[j]-previousSolvedVelocity[j]).magnitude;
                                if (change > solvedVelocityJump)
                                {
                                    solvedVelocityJump = change;
                                                                        var ls = (System.Array)typeof(CharacterGrounding).GetField("legs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(g);
                                    var leg = ls.GetValue(bones[j].name.EndsWith("L") ? 0 : 1);
                                    object F(string n) => leg.GetType().GetField(n).GetValue(leg);
                                    velocityDetail = $"frame={i}, bone={bones[j].name}, velocity={sv[j]:F1}, previous={previousSolvedVelocity[j]:F1}, planted={F("planted")}, contactTime={F("contactTime")}, release={F("releaseTime")}";
                                }
                            }
                        previousRawVelocity=rv; previousSolvedVelocity=sv;
                    }
                    if (i > 120)
                        for (int j = 0; j < bones.Length; j++)
                        {
                            rawMax = Mathf.Max(rawMax, Quaternion.Angle(lastRaw[j], raw[j]));
                            float change = Quaternion.Angle(lastSolved[j], solved[j]);
                            if (change > solvedMax) { solvedMax = change; worstFrame = i; detail = bones[j].name + " planted=" + (bones[j].name.EndsWith("L") ? g.LeftPlanted : g.RightPlanted); }
                        }
                    lastRaw = raw; lastSolved = solved;
                }
                bool continuous = solvedMax < (name == "VOY_Walk" ? 6 : 10) && solvedVelocityJump < (name == "VOY_Walk" ? 180 : 400);
                report.AppendLine($"{name}: max joint change at 120 Hz raw={rawMax:F2}deg, grounded={solvedMax:F2}deg, frame={worstFrame}, {detail}; {(continuous ? "PASS" : "FAIL")}");
                report.AppendLine($"Angular velocity change per sample: raw={rawVelocityJump:F1}, grounded={solvedVelocityJump:F1} deg/s, {velocityDetail}");
            }

        }
        finally { Object.DestroyImmediate(root); }
        File.WriteAllText("Logs/GaitContinuity.txt", report.ToString()); Debug.Log(report.ToString());
    }
}

