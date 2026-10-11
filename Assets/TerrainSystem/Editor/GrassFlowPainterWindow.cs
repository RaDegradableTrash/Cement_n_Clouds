using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Voyage.TerrainSystem;

namespace GrassFlow.Editor
{
    public sealed class GrassFlowPainterWindow : EditorWindow
    {
        const string Folder = "Assets/GrassFlowPaint/Resources/GrassFlow/Tiles";
        readonly List<GameObject> previews = new List<GameObject>();
        readonly List<GrassFlowPatch> patches = new List<GrassFlowPatch>();
        float radius = 12f, opacity = .7f, targetDensity = 1f, hardness = .5f;
        bool showWeights = true;
        bool painting;
        Vector3 lastPoint;
        bool dragging;
        readonly HashSet<GrassFlowPatch> touched = new HashSet<GrassFlowPatch>();
        int undoGroup;
        int brushControl;
        GameObject terrainOverview;
        Material overviewMaterial;
        Bounds overviewBounds;
        Texture2D overviewWeights;
        readonly Dictionary<Vector2Int, GrassFlowPatch> worldPatches = new Dictionary<Vector2Int, GrassFlowPatch>();
        Collider[] overviewColliders = Array.Empty<Collider>();
        TerrainTileIndex terrainIndex;
        bool topDownEditing = true;
        readonly Dictionary<SceneView, Vector3> savedClipSettings = new Dictionary<SceneView, Vector3>();
        const string SessionPath = "Library/VoyageTerrainPaintSession.json";
        const string RoadAssetPath = "Assets/Resources/TerrainSystem/TerrainRoadMap.asset";
        [Serializable] public struct PaintStroke
        {
            public Vector3 from, to;
            public float radius, strength, hardness, target;
            public bool road;
            public int gesture;
        }
        [Serializable] public class RemovedGesture { public List<PaintStroke> strokes = new List<PaintStroke>(); }
        [Serializable] public class PaintSession
        {
            public List<PaintStroke> strokes = new List<PaintStroke>();
            public List<RemovedGesture> removed = new List<RemovedGesture>();
            public int nextGesture;
        }
        [SerializeField] PaintSession session = new PaintSession();
        [SerializeField] bool roadTool;
        Texture2D overviewRoads;
        bool applyingDeferred;
        TerrainPaintHistory.Batch historyBatch;
        bool showHistory;
        const string ResetRequest = "Library/VoyageTerrainPaintReset.request";
        [InitializeOnLoadMethod]
        static void WatchResetRequest() { EditorApplication.update -= CheckResetRequest; EditorApplication.update += CheckResetRequest; }
        static void CheckResetRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(ResetRequest)) return;
            File.Delete(ResetRequest);
            try
            {
                int deleted=0, reset=0;
                foreach (var window in Resources.FindObjectsOfTypeAll<GrassFlowPainterWindow>())
                {
                    window.EndStroke(); window.session=new PaintSession(); window.painting=false;
                    Undo.ClearUndo(window); window.ClearPreview(); window.SaveDraft(); window.Repaint();
                }
                var defaults=new HashSet<string>();
                for(int z=-2;z<=0;z++) for(int x=-2;x<=0;x++) defaults.Add($"Grass_{x}_{z}");
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string guid in AssetDatabase.FindAssets("t:GrassFlowPatch",new[]{Folder}))
                    {
                        string path=AssetDatabase.GUIDToAssetPath(guid);
                        if (!path.StartsWith(Folder+"/",StringComparison.Ordinal) || !Path.GetFileName(path).StartsWith("Grass_",StringComparison.Ordinal)) continue;
                        if (!defaults.Contains(Path.GetFileNameWithoutExtension(path)))
                        {
                            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)) Undo.ClearUndo(asset);
                            if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Cannot remove painted patch: "+path);
                            deleted++; continue;
                        }
                        var patch=AssetDatabase.LoadAssetAtPath<GrassFlowPatch>(path);
                        Undo.ClearUndo(patch.density);
                        var colors=new Color[patch.density.width*patch.density.height];
                        for(int y=0;y<patch.density.height;y++) for(int x=0;x<patch.density.width;x++)
                        {
                            float coverage=patch.surface.GetPixelBilinear((x+.5f)/patch.density.width,(y+.5f)/patch.density.height).g;
                            colors[y*patch.density.width+x]=Color.white*(coverage>.5f ? .78f : 0f);
                        }
                        patch.density.SetPixels(colors); patch.density.Apply(false); patch.Changed();
                        EditorUtility.SetDirty(patch.density); reset++;
                    }
                    foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(RoadAssetPath)) Undo.ClearUndo(asset);
                    if (File.Exists(RoadAssetPath) && !AssetDatabase.DeleteAsset(RoadAssetPath)) throw new IOException("Cannot remove road map.");
                }
                finally { AssetDatabase.StopAssetEditing(); }
                AssetDatabase.SaveAssets(); TerrainPaintHistory.ClearAllRecords();
                File.WriteAllText(SessionPath,JsonUtility.ToJson(new PaintSession()));
                TerrainRoadMap.Reload(); SceneView.RepaintAll();
                File.WriteAllText("Library/VoyageTerrainPaintReset.result",$"OK deleted={deleted} defaultPatches={reset} roadsCleared=true historyCleared=true");
            }
            catch(Exception ex)
            {
                File.WriteAllText("Library/VoyageTerrainPaintReset.result","ERROR "+ex);
                Debug.LogException(ex);
            }
        }

        [MenuItem("Tools/Voyage/Grass Painter")]
        public static void Open() => GetWindow<GrassFlowPainterWindow>("Grass Painter");
        [MenuItem("Tools/Voyage/Terrain Density Painter")]
        public static void OpenDensityPainter() => Open();
        void OnEnable()
        {
            if (session.strokes.Count == 0 && File.Exists(SessionPath)) session = JsonUtility.FromJson<PaintSession>(File.ReadAllText(SessionPath)) ?? new PaintSession();
            SceneView.duringSceneGui += SceneGUI; Undo.undoRedoPerformed += Refresh; EditorApplication.playModeStateChanged += PlayModeChanged;
        }
        void OnDisable() { EndStroke(); SceneView.duringSceneGui -= SceneGUI; Undo.undoRedoPerformed -= Refresh; EditorApplication.playModeStateChanged -= PlayModeChanged; Save(); ClearPreview(); }
        void PlayModeChanged(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingEditMode) { Save(); ClearPreview(); } }

        void OnGUI()
        {
            EditorGUILayout.LabelField("地形绘画 / 草地与道路", EditorStyles.boldLabel);
            bool sceneClouds = Voyage.Rendering.Clouds.VolumetricCloudFeature.SceneViewCloudsEnabled;
            bool requestedClouds = EditorGUILayout.Toggle("Scene 窗口显示云层", sceneClouds);
            if (requestedClouds != sceneClouds) Voyage.Rendering.Clouds.VolumetricCloudFeature.SceneViewCloudsEnabled = requestedClouds;
            EditorGUILayout.HelpBox("绘画只更新预览、记录笔画，不生成地形区块。全部画完后点击“应用全部绘画”统一处理。Shift 擦除，Ctrl+Z 撤销待应用笔画。", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("显示全部地形 / Show entire terrain")) ShowAllTerrain();
                if (terrainOverview != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("框选全图")) FrameEntireTerrain();
                        if (GUILayout.Button("隐藏全图")) { painting = false; ClearOverview(); }
                    }
                    EditorGUILayout.HelpBox("预览立即显示；只有应用时才加载涉及的区块。道路绘制会清草，之后可以切换草地工具重新种草。", MessageType.None);
                    topDownEditing = EditorGUILayout.Toggle("俯视编辑（防止缩放穿地）", topDownEditing);
                    if (GUILayout.Button("返回地表俯视编辑"))
                    {
                        topDownEditing = true;
                        var view = SceneView.lastActiveSceneView;
                        if (view != null) SetTerrainView(view, view.pivot, Mathf.Max(64f, radius * 2f));
                    }
                }
                if (GUILayout.Button("Load / create patches near Scene view")) LoadArea(SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : new Vector3(-24, 0, -24));
                if (GUILayout.Button("Load vehicle spawn area")) LoadArea(new Vector3(-24, 0, -24));
                bool nextRoadTool = GUILayout.Toolbar(roadTool ? 1 : 0, new[] { "草地密度", "道路 · 浅棕泥土" }) == 1;
                if (nextRoadTool != roadTool) { EndStroke(); roadTool = nextRoadTool; UpdatePreview(); SceneView.RepaintAll(); }
                bool requestedPainting = GUILayout.Toggle(painting, "启用绘画笔刷", "Button");
                if (requestedPainting && terrainOverview == null) ShowAllTerrain();
                painting = requestedPainting;
                radius = EditorGUILayout.Slider("Radius (m)", radius, 1, 1024);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("精细 12m")) radius = 12f;
                    if (GUILayout.Button("区域 256m")) radius = 256f;
                    if (GUILayout.Button("大范围 1024m")) radius = 1024f;
                }
                opacity = EditorGUILayout.Slider("Strength", opacity, .01f, 1);
                hardness = EditorGUILayout.Slider("Hardness", hardness, 0, 1);
                using (new EditorGUI.DisabledScope(roadTool))
                {
                targetDensity = EditorGUILayout.Slider("Target density", targetDensity, 0, 1);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("绿色 · 稀疏 (0)")) targetDensity = 0f;
                    if (GUILayout.Button("红色 · 茂密 (1)")) targetDensity = 1f;
                }
                }
                if (roadTool) EditorGUILayout.HelpBox("左键绘制泥土路并清草；Shift 只擦除道路颜色。切换草地密度可在道路上种回草。道路模式显示泥土颜色。", MessageType.None);
                EditorGUI.BeginChangeCheck();
                showWeights = EditorGUILayout.Toggle("显示红绿权重", showWeights);
                if (EditorGUI.EndChangeCheck()) { UpdatePreview(); SceneView.RepaintAll(); }
                Rect legend = GUILayoutUtility.GetRect(1f, 16f, GUILayout.ExpandWidth(true));
                for (int i = 0; i < 64; i++)
                    EditorGUI.DrawRect(new Rect(legend.x + legend.width * i / 64f, legend.y, legend.width / 64f + 1f, legend.height), Color.Lerp(Color.green, Color.red, i / 63f));
                EditorGUILayout.LabelField("0 · 无草 / 稀疏                                      1 · 茂密");
                EditorGUILayout.LabelField(terrainOverview != null
                    ? $"全图绘画已就绪 · 已读取编辑区块：{worldPatches.Count}"
                    : $"局部编辑区块：{patches.Count}");
                EditorGUILayout.LabelField($"待应用笔画片段：{session.strokes.Count}（关闭窗口会保留草稿）");
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(session.strokes.Count == 0 && !TerrainPaintHistory.CanUndo))
                        if (GUILayout.Button("撤销上一次编辑（保留历史）")) UndoLastEdit();
                    using (new EditorGUI.DisabledScope(session.removed.Count == 0 && !TerrainPaintHistory.CanRedo))
                        if (GUILayout.Button("恢复上一次撤销")) RedoLastEdit();
                }
                showHistory = EditorGUILayout.Foldout(showHistory,"编辑历史");
                if (showHistory)
                {
                    EditorGUILayout.LabelField($"待应用：{session.strokes.Count} 段；已撤销笔画：{session.removed.Count} 次");
                    foreach (var batch in TerrainPaintHistory.Entries)
                        EditorGUILayout.LabelField(batch.label + (batch.active ? " · 已应用" : " · 已撤销（记录保留）"));
                }
                using (new EditorGUI.DisabledScope(session.strokes.Count == 0))
                    if (GUILayout.Button("应用全部绘画 / Apply all painting")) ApplyPendingPainting();
                if (GUILayout.Button("关闭预览（保留草稿）")) { Save(); painting = false; ClearPreview(); }
            }
        }

        void Refresh()
        {
            var roads = Resources.Load<TerrainRoadMap>(TerrainRoadMap.ResourcePath);
            if (roads != null && roads.mask != null) roads.mask.Apply(false);
            TerrainRoadMap.Bind(roads);
            var changedPatches = new HashSet<GrassFlowPatch>(patches);
            changedPatches.UnionWith(worldPatches.Values); changedPatches.UnionWith(touched);
            foreach (var patch in changedPatches) if (patch != null) { patch.density.Apply(false); patch.Changed(); }
            SaveDraft();
            if (!applyingDeferred && overviewMaterial != null)
            {
                BuildOverviewWeights();
                SaveDraft(); SceneView.RepaintAll(); Repaint(); return;
            }
            var all = new HashSet<GrassFlowPatch>(patches);
            all.UnionWith(worldPatches.Values);
            foreach (var patch in all) if (patch != null) { patch.density.Apply(false); patch.Changed(); UpdateOverviewPatch(patch); }
            if (overviewWeights != null) overviewWeights.Apply(false);
            SceneView.RepaintAll();
        }
        void UpdatePreview()
        {
            for (int i = 0; i < previews.Count; i++)
            {
                Material material = previews[i].GetComponent<MeshRenderer>().sharedMaterial;
                material.shader = Shader.Find(showWeights ? "Hidden/Voyage/GrassDensityPreview" : "Voyage/Terrain/Stylized");
                material.SetTexture("_DensityMap", patches[i].density);
                material.SetTexture("_SurfaceMap", patches[i].surface);
                Bounds bounds = patches[i].bounds;
                material.SetVector("_PatchBounds", new Vector4(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z));
                previews[i].GetComponent<GrassFlowRenderer>().enabled = !showWeights;
            }
            UpdateOverviewCutout();
        }
        public void ShowAllTerrain()
        {
            if (Application.isPlaying) return;
            if (terrainOverview == null)
            {
                var prefab = Resources.Load<GameObject>("TerrainSystem/Horizon/TerrainHorizon");
                if (prefab == null) throw new InvalidOperationException("全图地形资源缺失，请先执行 Voyage/Terrain/Rebuild distant horizon。");
                var shader = Shader.Find("Hidden/Voyage/TerrainOverview");
                if (shader == null) throw new InvalidOperationException("Terrain overview shader is missing.");
                overviewMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                terrainOverview = Instantiate(prefab);
                terrainOverview.name = "Entire terrain [Editor overview]";
                foreach (Transform child in terrainOverview.GetComponentsInChildren<Transform>(true))
                    child.gameObject.hideFlags = HideFlags.HideAndDontSave;
                bool first = true;
                foreach (var renderer in terrainOverview.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.sharedMaterial = overviewMaterial;
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    if (first) { overviewBounds = renderer.bounds; first = false; }
                    else overviewBounds.Encapsulate(renderer.bounds);
                }
                overviewColliders = terrainOverview.GetComponentsInChildren<Collider>();
                BuildOverviewWeights();
                if (radius < 64f) radius = 256f;
                UpdateOverviewCutout();
            }
            FrameEntireTerrain(); SceneView.RepaintAll(); Repaint();
        }
        void FrameEntireTerrain()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null || terrainOverview == null) return;
            if (topDownEditing)
                SetTerrainView(view, overviewBounds.center, Mathf.Max(overviewBounds.extents.z, overviewBounds.extents.x / Mathf.Max(.1f, view.position.width / Mathf.Max(1f, view.position.height))) * 1.15f);
            else view.Frame(overviewBounds, false);
        }
        void SetTerrainView(SceneView view, Vector3 pivot, float size)
        {
            // Keep the orbit pivot above even the highest peak. Orthographic zoom changes
            // the map scale instead of flying towards a pivot buried inside the mountain.
            pivot.y = overviewBounds.max.y + 10f;
            var clips = view.cameraSettings;
            if (!savedClipSettings.ContainsKey(view))
                savedClipSettings.Add(view, new Vector3(clips.dynamicClip ? 1f : 0f, clips.nearClip, clips.farClip));
            clips.dynamicClip = false;
            clips.nearClip = .1f;
            clips.farClip = Mathf.Max(10000f, Mathf.Clamp(size, 2f, 100000f) * 4f + overviewBounds.size.y + 10000f);
            view.cameraSettings = clips;
            view.orthographic = true;
            view.LookAtDirect(pivot, Quaternion.Euler(90f, 0f, 0f), Mathf.Clamp(size, 2f, 100000f));
        }
        void RestoreViewClipping()
        {
            foreach (var entry in savedClipSettings)
            {
                if (entry.Key == null) continue;
                var clips = entry.Key.cameraSettings;
                clips.dynamicClip = entry.Value.x > .5f;
                clips.nearClip = entry.Value.y;
                clips.farClip = entry.Value.z;
                entry.Key.cameraSettings = clips;
            }
            savedClipSettings.Clear();
        }
        void NavigateTerrainView(SceneView view, Event e)
        {
            if (terrainOverview == null || !topDownEditing || Application.isPlaying) { RestoreViewClipping(); return; }
            if (!view.orthographic || Quaternion.Angle(view.rotation, Quaternion.Euler(90f, 0f, 0f)) > .01f || Mathf.Abs(view.pivot.y - (overviewBounds.max.y + 10f)) > .01f)
                SetTerrainView(view, view.pivot, view.size);
            if (e.type != EventType.ScrollWheel) return;
            float oldSize = view.size;
            float newSize = Mathf.Clamp(oldSize * Mathf.Exp(e.delta.y * .08f), 2f, 100000f);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 pivot = view.pivot;
            var plane = new Plane(Vector3.up, pivot);
            if (plane.Raycast(ray, out float distance))
                pivot = Vector3.LerpUnclamped(ray.GetPoint(distance), pivot, newSize / Mathf.Max(.001f, oldSize));
            SetTerrainView(view, pivot, newSize);
            e.Use();
            view.Repaint();
        }
        void BuildOverviewWeights()
        {
            if (overviewWeights != null) DestroyImmediate(overviewWeights);
            if (overviewRoads != null) DestroyImmediate(overviewRoads);
            overviewWeights = new Texture2D(4096, 4096, TextureFormat.RGBA32, false, true)
            { name = "Grass density overview (temporary)", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[overviewWeights.width * overviewWeights.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(199, 0, 0, 255);
            overviewWeights.SetPixels32(pixels);
            // Read existing authored weights; opening the map never creates thousands of assets.
            if (AssetDatabase.IsValidFolder(Folder))
                foreach (string guid in AssetDatabase.FindAssets("t:GrassFlowPatch", new[] { Folder }))
                    UpdateOverviewPatch(AssetDatabase.LoadAssetAtPath<GrassFlowPatch>(AssetDatabase.GUIDToAssetPath(guid)));
            overviewWeights.Apply(false);
            overviewMaterial.SetTexture("_DensityMap", overviewWeights);
            overviewMaterial.SetVector("_MapBounds", new Vector4(overviewBounds.min.x, overviewBounds.min.z, overviewBounds.size.x, overviewBounds.size.z));
            overviewRoads = new Texture2D(4096, 4096, TextureFormat.RGBA32, false, true)
            { name = "Road preview (temporary)", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var roads = Resources.Load<TerrainRoadMap>(TerrainRoadMap.ResourcePath);
            if (roads != null && roads.mask != null)
            {
                Vector4 bounds = new Vector4(overviewBounds.min.x, overviewBounds.min.z, overviewBounds.size.x, overviewBounds.size.z);
                if (roads.bounds != bounds || roads.mask.width != overviewRoads.width || roads.mask.height != overviewRoads.height)
                    throw new InvalidOperationException("道路地图范围与当前地形不同，请先迁移道路数据，避免覆盖已有道路。");
                overviewRoads.SetPixels32(roads.mask.GetPixels32());
            }
            else overviewRoads.SetPixels32(new Color32[4096 * 4096]);
            foreach (var stroke in session.strokes) PreviewStroke(stroke);
            overviewWeights.Apply(false); overviewRoads.Apply(false);
            overviewMaterial.SetTexture("_RoadMap", overviewRoads);
        }
        void SaveDraft()
        {
            Directory.CreateDirectory("Library");
            File.WriteAllText(SessionPath, JsonUtility.ToJson(session));
        }
        void QueueStroke(Vector3 from, Vector3 to, float target)
        {
            var stroke = new PaintStroke { from = from, to = to, radius = radius, strength = opacity, hardness = hardness, target = target, road = roadTool, gesture = session.nextGesture };
            session.strokes.Add(stroke);
            PreviewStroke(stroke);
            if (!stroke.road || stroke.target > 0f) overviewWeights.Apply(false);
            if (stroke.road) overviewRoads.Apply(false);
        }
        void UndoLastEdit()
        {
            EndStroke();
            if (session.strokes.Count > 0)
            {
                int gesture=session.strokes[session.strokes.Count-1].gesture;
                int start=session.strokes.FindLastIndex(s => s.gesture != gesture)+1;
                var removed=new RemovedGesture { strokes=session.strokes.GetRange(start,session.strokes.Count-start) };
                session.removed.Add(removed); session.strokes.RemoveRange(start,session.strokes.Count-start);
            }
            else
            {
                ClearPatchPreview(); worldPatches.Clear(); touched.Clear();
                TerrainPaintHistory.UndoLast();
            }
            SaveDraft(); Undo.ClearUndo(this);
            if (overviewMaterial != null) BuildOverviewWeights();
            Repaint(); SceneView.RepaintAll();
        }
        void RedoLastEdit()
        {
            EndStroke();
            if (session.removed.Count > 0)
            {
                int last=session.removed.Count-1;
                session.strokes.AddRange(session.removed[last].strokes); session.removed.RemoveAt(last);
            }
            else
            {
                ClearPatchPreview(); worldPatches.Clear(); touched.Clear();
                TerrainPaintHistory.RedoLast();
            }
            SaveDraft(); Undo.ClearUndo(this);
            if (overviewMaterial != null) BuildOverviewWeights();
            Repaint(); SceneView.RepaintAll();
        }
        void PreviewStroke(PaintStroke stroke)
        {
            if (stroke.road)
            {
                PaintAtlas(overviewRoads, stroke, stroke.target);
                if (stroke.target > 0f) PaintAtlas(overviewWeights, stroke, 0f, true);
            }
            else PaintAtlas(overviewWeights, stroke, stroke.target);
        }
        void PaintAtlas(Texture2D texture, PaintStroke stroke, float target, bool clearGrass = false)
        {
            if (texture == null) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(stroke.from, stroke.to) / Mathf.Max(1f, stroke.radius * .25f)));
            Vector3 min = overviewBounds.min, size = overviewBounds.size;
            for (int step = 1; step <= steps; step++)
            {
                Vector3 center = Vector3.Lerp(stroke.from, stroke.to, step / (float)steps);
                int x0 = Mathf.Clamp(Mathf.FloorToInt((center.x-stroke.radius-min.x)/size.x*texture.width),0,texture.width-1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((center.x+stroke.radius-min.x)/size.x*texture.width),0,texture.width-1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt((center.z-stroke.radius-min.z)/size.z*texture.height),0,texture.height-1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt((center.z+stroke.radius-min.z)/size.z*texture.height),0,texture.height-1);
                int width = x1-x0+1, height = y1-y0+1;
                var pixels = texture.GetPixels(x0,y0,width,height);
                for (int y=0;y<height;y++) for (int x=0;x<width;x++)
                {
                    float distance = Vector2.Distance(new Vector2(min.x+(x+x0+.5f)/texture.width*size.x,min.z+(y+y0+.5f)/texture.height*size.z),new Vector2(center.x,center.z))/stroke.radius;
                    if (distance > 1f) continue;
                    float weight = 1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Min(stroke.hardness,.999f),1,distance));
                    float value = Mathf.Lerp(pixels[y*width+x].r,target,clearGrass ? 1f : Mathf.Clamp01(stroke.strength*weight));
                    pixels[y*width+x]=new Color(value,0,0,1);
                }
                texture.SetPixels(x0,y0,width,height,pixels);
            }
        }
        void ApplyPendingPainting()
        {
            EndStroke();
            if (session.strokes.Count == 0) return;
            if (terrainOverview == null) ShowAllTerrain();
            float savedRadius=radius, savedOpacity=opacity, savedHardness=hardness;
            applyingDeferred = true;
            touched.Clear();
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply terrain painting");
            historyBatch=TerrainPaintHistory.Begin(JsonUtility.ToJson(session));
            try
            {
                for (int i=0;i<session.strokes.Count;i++)
                {
                    var stroke=session.strokes[i];
                    if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("统一应用地形绘画", $"笔画 {i+1}/{session.strokes.Count}", i/(float)session.strokes.Count);
                    // Erasing a road restores ground color but never invents grass.
                    ApplyGrassEdit(stroke);
                }
                if (session.strokes.Exists(s => s.road))
                {
                    TerrainPaintHistory.CaptureBefore(historyBatch,RoadAssetPath);
                    var roads=AssetDatabase.LoadAssetAtPath<TerrainRoadMap>(RoadAssetPath);
                    if (roads == null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(RoadAssetPath)); AssetDatabase.Refresh();
                        roads=CreateInstance<TerrainRoadMap>();
                        roads.mask=new Texture2D(overviewRoads.width,overviewRoads.height,TextureFormat.RGBA32,false,true) { name="Road coverage", wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
                        roads.mask.SetPixels32(new Color32[overviewRoads.width*overviewRoads.height]); roads.mask.Apply(false);
                        roads.bounds=new Vector4(overviewBounds.min.x,overviewBounds.min.z,overviewBounds.size.x,overviewBounds.size.z);
                        AssetDatabase.CreateAsset(roads,RoadAssetPath); AssetDatabase.AddObjectToAsset(roads.mask,roads);
                    }
                    Undo.RegisterCompleteObjectUndo(roads.mask,"Apply roads");
                    roads.mask.SetPixels32(overviewRoads.GetPixels32()); roads.mask.Apply(false);
                    EditorUtility.SetDirty(roads.mask); TerrainRoadMap.Bind(roads);
                }
                AssetDatabase.SaveAssets();
                TerrainPaintHistory.Commit(historyBatch);
                // Applied batches use persistent history, not Unity's transient undo stack.
                foreach (var patch in touched) if (patch != null) Undo.ClearUndo(patch.density);
                var appliedRoads=AssetDatabase.LoadAssetAtPath<TerrainRoadMap>(RoadAssetPath);
                if (appliedRoads != null) Undo.ClearUndo(appliedRoads.mask);
                session.strokes.Clear(); SaveDraft(); Undo.ClearUndo(this);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.FlushUndoRecordObjects(); Undo.RevertAllDownToGroup(group);
                throw;
            }
            finally
            {
                radius=savedRadius; opacity=savedOpacity; hardness=savedHardness; applyingDeferred=false;
                historyBatch=null;
                EditorUtility.ClearProgressBar();
            }
            BuildOverviewWeights(); Repaint(); SceneView.RepaintAll();
        }
        void ApplyGrassEdit(PaintStroke stroke)
        {
            if (stroke.road && stroke.target <= 0f) return;
            radius=stroke.radius; opacity=stroke.road ? 1f : stroke.strength; hardness=stroke.road ? 1f : stroke.hardness;
            PaintWorldStroke(stroke.from,stroke.to,stroke.road ? 0f : stroke.target);
        }
        void UpdateOverviewPatch(GrassFlowPatch patch)
        {
            if (overviewWeights == null || patch == null) return;
            Vector3 min = overviewBounds.min, size = overviewBounds.size;
            int w = overviewWeights.width, h = overviewWeights.height;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((patch.bounds.min.x-min.x)/size.x*w),0,w-1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((patch.bounds.max.x-min.x)/size.x*w),0,w-1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((patch.bounds.min.z-min.z)/size.z*h),0,h-1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((patch.bounds.max.z-min.z)/size.z*h),0,h-1);
            for (int y=y0; y<=y1; y++) for (int x=x0; x<=x1; x++)
            {
                float u=(min.x+(x+.5f)/w*size.x-patch.bounds.min.x)/patch.bounds.size.x;
                float v=(min.z+(y+.5f)/h*size.z-patch.bounds.min.z)/patch.bounds.size.z;
                if (u<0 || u>1 || v<0 || v>1) continue;
                overviewWeights.SetPixel(x,y,new Color(patch.density.GetPixelBilinear(u,v).r,0,0,1));
            }
        }
        void PaintWorldStroke(Vector3 from, Vector3 to, float target)
        {
            if (terrainIndex == null) terrainIndex = Resources.Load<TerrainTileIndex>("TerrainSystem/TerrainTileIndex");
            if (terrainIndex == null) throw new InvalidOperationException("Terrain tile index is missing.");
            var settings = terrainIndex.settings;
            Vector2Int min = settings.WorldToTile(Vector3.Min(from,to)-new Vector3(radius,0,radius));
            Vector2Int max = settings.WorldToTile(Vector3.Max(from,to)+new Vector3(radius,0,radius));
            int steps = Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/Mathf.Max(1f,radius*.25f)));
            bool progress = !applyingDeferred && !Application.isBatchMode && (max.x-min.x+1)*(max.y-min.y+1)>9;
            try
            {
                for (int z=min.y; z<=max.y; z++) for (int x=min.x; x<=max.x; x++)
                {
                    var coordinate = new Vector2Int(x,z);
                    if (!terrainIndex.TryGet(coordinate,out var record)) continue;
                    Vector2 a = new Vector2(from.x,from.z), b = new Vector2(to.x,to.z);
                    Bounds tileBounds = settings.GetTileBounds(coordinate);
                    Vector2 center = new Vector2(tileBounds.center.x,tileBounds.center.z);
                    Vector2 delta = b-a;
                    float along = delta.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot(center-a,delta)/delta.sqrMagnitude) : 0f;
                    float reach = radius + new Vector2(tileBounds.extents.x,tileBounds.extents.z).magnitude;
                    if ((center-(a+delta*along)).sqrMagnitude > reach*reach) continue;
                    if (progress) EditorUtility.DisplayProgressBar("绘制草地密度", $"区块 {x}, {z}", (z-min.y)/(float)Mathf.Max(1,max.y-min.y+1));
                    TerrainPaintHistory.CaptureBefore(historyBatch,$"{Folder}/Grass_{x}_{z}.asset");
                    if (!worldPatches.TryGetValue(coordinate,out var patch))
                    {
                        patch = AssetDatabase.LoadAssetAtPath<GrassFlowPatch>($"{Folder}/Grass_{x}_{z}.asset");
                        if (patch == null)
                        {
                            Mesh mesh = ReadWorldMesh(record);
                            if (mesh == null) throw new InvalidOperationException($"无法读取地形区块 {x}, {z}；绘画草稿已保留，请恢复该区块资源后重新应用。");
                            try
                            {
                                if (!AssetDatabase.IsValidFolder(Folder)) { Directory.CreateDirectory(Folder); AssetDatabase.Refresh(); }
                                patch = LoadOrBake(record,settings,mesh);
                            }
                            finally { DestroyImmediate(mesh); }
                        }
                        worldPatches[coordinate] = patch;
                    }
                    if (touched.Add(patch)) Undo.RegisterCompleteObjectUndo(patch.density,"Paint grass");
                    bool changed=false;
                    for (int step=1; step<=steps; step++)
                        changed |= patch.Paint(Vector3.Lerp(from,to,step/(float)steps),radius,opacity,target,hardness);
                    if (changed) { EditorUtility.SetDirty(patch.density); if (!applyingDeferred) UpdateOverviewPatch(patch); }
                }
            }
            finally { if (progress) EditorUtility.ClearProgressBar(); }
            if (!applyingDeferred && overviewWeights != null) overviewWeights.Apply(false);
        }
        void UpdateOverviewCutout()
        {
            foreach (var preview in previews) if (preview != null) preview.SetActive(terrainOverview == null);
            if (overviewMaterial == null) return;
            overviewMaterial.SetFloat("_ShowWeights", showWeights && !roadTool ? 1f : 0f);
        }
        void ClearOverview()
        {
            RestoreViewClipping();
            if (terrainOverview != null) DestroyImmediate(terrainOverview);
            if (overviewMaterial != null) DestroyImmediate(overviewMaterial);
            if (overviewWeights != null) DestroyImmediate(overviewWeights);
            if (overviewRoads != null) DestroyImmediate(overviewRoads);
            overviewRoads = null;
            terrainOverview = null; overviewMaterial = null;
            overviewWeights = null; overviewColliders = Array.Empty<Collider>();
            UpdateOverviewCutout();
            SceneView.RepaintAll();
        }
        void Save() { SaveDraft(); }
        void EndStroke()
        {
            if (!dragging) return;
            dragging = false;
            if (GUIUtility.hotControl == brushControl) GUIUtility.hotControl = 0;
            Undo.CollapseUndoOperations(undoGroup);
            Save();
        }
        void ClearPreview()
        {
            ClearPatchPreview(); ClearOverview();
            worldPatches.Clear(); touched.Clear();
        }
        void ClearPatchPreview()
        {
            EndStroke();
            foreach (var go in previews)
            {
                if (go == null) continue;
                var filter = go.GetComponent<MeshFilter>();
                if (filter != null) DestroyImmediate(filter.sharedMesh);
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null) DestroyImmediate(renderer.sharedMaterial);
                DestroyImmediate(go);
            }
            previews.Clear(); patches.Clear();
            UpdateOverviewCutout();
        }

        public void LoadArea(Vector3 position)
        {
            Save(); ClearPatchPreview();
            var index = Resources.Load<TerrainTileIndex>("TerrainSystem/TerrainTileIndex");
            if (index == null) throw new InvalidOperationException("Terrain tile index is missing.");
            Vector2Int center = index.settings.WorldToTile(position);
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            for (int z = center.y - 1; z <= center.y + 1; z++)
            for (int x = center.x - 1; x <= center.x + 1; x++)
            {
                if (!index.TryGet(new Vector2Int(x, z), out var record)) continue;
                Mesh mesh = ReadWorldMesh(record);
                if (mesh == null) continue;
                var patch = LoadOrBake(record, index.settings, mesh);
                patches.Add(patch);
                var go = new GameObject($"Grass brush preview {x}, {z}") { hideFlags = HideFlags.HideAndDontSave };
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var material = new Material(Shader.Find("Voyage/Terrain/Stylized")) { hideFlags = HideFlags.HideAndDontSave };
                material.SetColor("_BaseColor", new Color(.64f, .42f, .14f));
                material.SetColor("_ShadowColor", new Color(.48f, .33f, .12f));
                material.SetColor("_RidgeColor", new Color(.78f, .56f, .22f));
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                go.AddComponent<GrassFlowRenderer>().patch = patch;
                previews.Add(go);
            }
            UpdatePreview(); AssetDatabase.SaveAssets(); SceneView.RepaintAll(); Repaint();
            foreach (var patch in patches) UpdateOverviewPatch(patch);
            if (overviewWeights != null) overviewWeights.Apply(false);
        }

        public static Mesh ReadWorldMesh(TerrainTileRecord record)
        {
            var prefab = TerrainPrefabStore.LoadInEditor(record.resourcePath);
            if (prefab == null) return null;
            var runtime = prefab.GetComponent<TerrainTileRuntime>();
            var roots = new SerializedObject(runtime).FindProperty("lodRoots");
            var lod = roots.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
            if (lod == null) return null;
            var vertices = new List<Vector3>(); var indices = new List<int>();
            foreach (var filter in lod.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.name.IndexOf("skirt", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                int offset = vertices.Count;
                Matrix4x4 matrix = Matrix4x4.Translate(record.bounds.center) * prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (var vertex in filter.sharedMesh.vertices) vertices.Add(matrix.MultiplyPoint3x4(vertex));
                foreach (int index in filter.sharedMesh.triangles) indices.Add(index + offset);
            }
            var mesh = new Mesh { name = "GrassFlow mesh surface", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds(); mesh.RecalculateNormals(); return mesh;
        }

        public static GrassFlowPatch LoadOrBake(TerrainTileRecord record, TerrainChunkSettings settings, Mesh mesh)
        {
            string path = $"{Folder}/Grass_{record.coordinate.x}_{record.coordinate.y}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GrassFlowPatch>(path);
            if (existing != null) return existing; // Never overwrite authored strokes on reopening.
            var patch = CreatePatch(mesh, settings.GetTileBounds(record.coordinate), .78f);
            patch.name = $"Grass_{record.coordinate.x}_{record.coordinate.y}";
            AssetDatabase.CreateAsset(patch, path);
            AssetDatabase.AddObjectToAsset(patch.surface, patch);
            AssetDatabase.AddObjectToAsset(patch.density, patch);
            EditorUtility.SetDirty(patch); return patch;
        }

        public static GrassFlowPatch CreatePatch(Mesh worldMesh, Bounds tileBounds, float fill)
        {
            const int resolution = 256;
            var patch = CreateInstance<GrassFlowPatch>();
            Vector3 min = tileBounds.min, size = tileBounds.size;
            min.y = worldMesh.bounds.min.y - .01f; size.y = Mathf.Max(.1f, worldMesh.bounds.size.y + .02f);
            patch.bounds = new Bounds(min + size * .5f, size);
            var surface = new Color[resolution * resolution];
            var vertices = worldMesh.vertices; var triangles = worldMesh.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = vertices[triangles[t]], b = vertices[triangles[t + 1]], c = vertices[triangles[t + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (Mathf.Abs(normal.y) < .57f) continue;
                int x0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - min.x) / size.x * resolution), 0, resolution - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - min.x) / size.x * resolution), 0, resolution - 1);
                int z0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.z, Mathf.Min(b.z, c.z)) - min.z) / size.z * resolution), 0, resolution - 1);
                int z1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - min.z) / size.z * resolution), 0, resolution - 1);
                float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(denominator) < .000001f) continue;
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    float px = min.x + (x + .5f) / resolution * size.x, pz = min.z + (z + .5f) / resolution * size.z;
                    float u = ((b.z - c.z) * (px - c.x) + (c.x - b.x) * (pz - c.z)) / denominator;
                    float v = ((c.z - a.z) * (px - c.x) + (a.x - c.x) * (pz - c.z)) / denominator;
                    if (u < -.00001f || v < -.00001f || u + v > 1.00001f) continue;
                    float height = (u * a.y + v * b.y + (1 - u - v) * c.y - min.y) / size.y;
                    int index = z * resolution + x;
                    if (surface[index].g == 0 || height > surface[index].r) surface[index] = new Color(height, 1, 0, 1);
                }
            }
            patch.surface = new Texture2D(resolution, resolution, TextureFormat.RGFloat, false, true) { name = "Mesh height and coverage", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            patch.surface.SetPixels(surface); patch.surface.Apply(false);
            var colors = new Color[surface.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white * (surface[i].g > .5f ? fill : 0f);
            patch.density = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true) { name = "Painted density", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            patch.density.SetPixels(colors); patch.density.Apply(false); return patch;
        }

        void SceneGUI(SceneView view)
        {
            Event e = Event.current;
            NavigateTerrainView(view, e);
            if (dragging && ((e.type == EventType.MouseUp && e.button == 0) || e.type == EventType.Ignore))
            {
                EndStroke();
                if (e.type == EventType.MouseUp) e.Use();
            }
            if (!painting) EndStroke();
            if (!painting || Application.isPlaying || (patches.Count == 0 && terrainOverview == null)) return;
            int control = GUIUtility.GetControlID("VoyageGrassDensityBrush".GetHashCode(), FocusType.Passive);
            if (e.alt || e.button == 1 || e.button == 2) return;
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool hitFound = false; RaycastHit hit = default; float nearest = float.MaxValue;
            foreach (var preview in previews)
                if (preview != null && preview.activeSelf && preview.GetComponent<Collider>().Raycast(ray, out var candidate, 100000) && candidate.distance < nearest)
                { hit = candidate; nearest = candidate.distance; hitFound = true; }
            foreach (var collider in overviewColliders)
                if (collider != null && collider.Raycast(ray, out var candidate, 1000000) && candidate.distance < nearest)
                { hit = candidate; nearest = candidate.distance; hitFound = true; }
            if (hitFound)
            {
                Handles.color = Color.Lerp(Color.green, Color.red, e.shift ? 0f : targetDensity);
                Handles.DrawWireDisc(hit.point, hit.normal, radius);
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    if (!dragging)
                    {
                        Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Paint grass");
                        brushControl = control; GUIUtility.hotControl = control;
                        touched.Clear(); lastPoint = hit.point; dragging = true;
                        session.nextGesture++;
                        Undo.RegisterCompleteObjectUndo(this, "Terrain paint draft");
                    }
                    int steps = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(lastPoint, hit.point) / Mathf.Max(.5f, radius * .2f)), 1, 128);
                    if (terrainOverview != null) QueueStroke(lastPoint, hit.point, e.shift ? 0f : roadTool ? 1f : targetDensity);
                    else foreach (var patch in patches)
                    {
                        if (!touched.Contains(patch)) { Undo.RegisterCompleteObjectUndo(patch.density, "Paint grass"); touched.Add(patch); }
                        for (int step = 1; step <= steps; step++)
                            if (patch.Paint(Vector3.Lerp(lastPoint, hit.point, step / (float)steps), radius, opacity, e.shift ? 0 : targetDensity, hardness)) EditorUtility.SetDirty(patch.density);
                    }
                    lastPoint = hit.point; e.Use(); view.Repaint();
                }
            }
            if (e.type == EventType.MouseMove) view.Repaint();
        }

        // Batch-mode setup, without modifying the currently open scene or prefabs.
        public static void BakeSpawnArea()
        {
            var window = CreateInstance<GrassFlowPainterWindow>();
            try { window.LoadArea(new Vector3(-24, 0, -24)); }
            finally { window.ClearPreview(); DestroyImmediate(window); }
        }
    }
}
