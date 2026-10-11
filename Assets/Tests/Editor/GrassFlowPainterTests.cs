using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Voyage.Tests.Editor
{
    public sealed class GrassFlowPainterTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static Type Find(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) { var type = assembly.GetType(name); if (type != null) return type; }
            throw new Exception(name);
        }
        static object Get(object value, string field) => value.GetType().GetField(field, Flags).GetValue(value);
        static void Set(object value, string field, object data) => value.GetType().GetField(field, Flags).SetValue(value, data);
        static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method, Flags).Invoke(value, args);
        static Mesh Surface(float offset = 0)
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(offset, 0, 0), new Vector3(offset, 0, 16), new Vector3(offset + 16, 0, 0), new Vector3(offset + 16, 0, 16) };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 }; mesh.RecalculateBounds(); return mesh;
        }
        static ScriptableObject Patch(Mesh mesh, float offset = 0, float fill = 1) => (ScriptableObject)Find("GrassFlow.Editor.GrassFlowPainterWindow").GetMethod("CreatePatch").Invoke(null, new object[] { mesh, new Bounds(new Vector3(offset + 8, 0, 8), new Vector3(16, 1, 16)), fill });
        static void DeletePatch(ScriptableObject patch)
        {
            Object.DestroyImmediate((Object)Get(patch, "surface")); Object.DestroyImmediate((Object)Get(patch, "density")); Object.DestroyImmediate(patch);
        }

        static ScriptableObject streamedResult;
        [Test]
        public void AssigningAuthoredPatchDoesNotTransferOwnershipToTile()
        {
            var patch = Resources.Load<ScriptableObject>("GrassFlow/Tiles/Grass_-1_-1");
            Assert.That(patch, Is.Not.Null);
            var host = new GameObject("Grass ownership test");
            host.SetActive(false);
            try
            {
                var tile = host.AddComponent(Find("Voyage.TerrainSystem.TerrainTileRuntime"));
                Call(tile, "AssignGrassPatch", patch);
                Assert.That(Get(tile, "generatedPatch"), Is.Null,
                    "Borrowed patches must not be destroyed when their tile unloads.");
                Object.DestroyImmediate(host);
                Assert.That(patch != null, Is.True);
                Assert.That((Object)Get(patch, "surface") != null, Is.True);
                Assert.That((Object)Get(patch, "density") != null, Is.True);
            }
            finally { if (host != null) Object.DestroyImmediate(host); }
        }
        static void ReceiveStreamed<T>(T value) { streamedResult = value as ScriptableObject; }
        [Test]
        public void UnpaintedFarTileBuildsGrassFromTransformedMesh()
        {
            var host = new GameObject("Far tile surface"); var mesh = Surface(); streamedResult = null;
            try
            {
                host.transform.position = new Vector3(2048, 25, 2048);
                var filter = host.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
                var patchType = Find("GrassFlow.GrassFlowPatch");
                var method = typeof(GrassFlowPainterTests).GetMethod("ReceiveStreamed", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(patchType);
                var callback = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(patchType), method);
                var routine = (System.Collections.IEnumerator)Find("Voyage.TerrainSystem.StreamedGrassSurface").GetMethod("Build").Invoke(null,
                    new object[] { new[] { filter }, new Bounds(new Vector3(2056,25,2056), new Vector3(16,10,16)), callback });
                while(routine.MoveNext()) { }
                Assert.That(streamedResult, Is.Not.Null);
                var surface = (Texture2D)Get(streamedResult,"surface");
                Assert.That(surface.GetPixel(64,64).g, Is.EqualTo(1));
                var bounds = (Bounds)Get(streamedResult,"bounds");
                Assert.That(bounds.min.y+surface.GetPixel(64,64).r*bounds.size.y,Is.EqualTo(25).Within(.01));
                Assert.That(((Texture2D)Get(streamedResult,"density")).GetPixel(64,64).r, Is.GreaterThan(.5f));
            }
            finally { if(streamedResult!=null) DeletePatch(streamedResult); Object.DestroyImmediate(host); Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void RuntimeGrassHeightComesFromOwningTileMeshNotOverheadCollider()
        {
            var host = new GameObject("Runtime grass terrain");
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = Surface();
            streamedResult = null;
            try
            {
                host.transform.position = new Vector3(2048, 25, 2048);
                var filter = host.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                blocker.transform.position = new Vector3(2056, 45, 2056);
                blocker.transform.localScale = new Vector3(3, 1, 3);

                var patchType = Find("GrassFlow.GrassFlowPatch");
                var callbackMethod = typeof(GrassFlowPainterTests)
                    .GetMethod("ReceiveStreamed", BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(patchType);
                var callback = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(patchType), callbackMethod);
                var builder = Find("Voyage.TerrainSystem.StreamedGrassSurface");
                var routine = (System.Collections.IEnumerator)builder
                    .GetMethod("BuildRuntimeSampled", Flags)
                    .Invoke(null, new object[]
                    {
                        new[] { filter },
                        new Bounds(new Vector3(2056, 25, 2056), new Vector3(16, 10, 16)),
                        callback
                    });
                while (routine.MoveNext()) { }

                Assert.That(streamedResult, Is.Not.Null);
                var surface = (Texture2D)Get(streamedResult, "surface");
                var patchBounds = (Bounds)Get(streamedResult, "bounds");
                float sampledWorldHeight = patchBounds.min.y + surface.GetPixel(32, 32).r * patchBounds.size.y;
                Assert.That(sampledWorldHeight, Is.EqualTo(25f).Within(.05f),
                    "Grass roots must follow their terrain mesh, even when another collider is directly above it.");
            }
            finally
            {
                if (streamedResult != null) DeletePatch(streamedResult);
                Object.DestroyImmediate(blocker);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void WholeTerrainOverviewSharesMeshesAndSurvivesChangingPaintArea()
        {
            var window = ScriptableObject.CreateInstance(Find("GrassFlow.Editor.GrassFlowPainterWindow"));
            var prefab = Resources.Load<GameObject>("TerrainSystem/Horizon/TerrainHorizon");
            Assert.That(prefab, Is.Not.Null);
            var sourceMeshes = prefab.GetComponentsInChildren<MeshFilter>();
            var originalMaterial = prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            try
            {
                Call(window, "ShowAllTerrain");
                var root = (GameObject)Get(window, "terrainOverview");
                var previewMeshes = root.GetComponentsInChildren<MeshFilter>();
                Assert.That(previewMeshes.Length, Is.EqualTo(sourceMeshes.Length));
                Assert.That(previewMeshes.Length, Is.GreaterThan(1));
                for (int i = 0; i < sourceMeshes.Length; i++)
                    Assert.That(previewMeshes[i].sharedMesh, Is.SameAs(sourceMeshes[i].sharedMesh));
                Assert.That(root.GetComponentsInChildren<Collider>().Length, Is.EqualTo(sourceMeshes.Length), "Every overview region must be directly paintable.");
                Physics.SyncTransforms();
                var pickMesh = previewMeshes[0];
                var vertices = pickMesh.sharedMesh.vertices;
                var triangles = pickMesh.sharedMesh.triangles;
                Vector3 point = pickMesh.transform.TransformPoint((vertices[triangles[0]] + vertices[triangles[1]] + vertices[triangles[2]]) / 3f);
                Assert.That(pickMesh.GetComponent<Collider>().Raycast(new Ray(point + Vector3.up * 10000f, Vector3.down), out _, 20000f),
                    Is.True, "The hidden editor overview must accept brush rays, not merely display a collider component.");
                Assert.That(Get(window, "overviewWeights"), Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<MonoBehaviour>(), Is.Empty);
                Assert.That(root.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
                var bounds = (Bounds)Get(window, "overviewBounds");
                Assert.That(bounds.size.x, Is.GreaterThan(6000f));
                Assert.That(bounds.size.z, Is.GreaterThan(6000f));
                Call(window, "ShowAllTerrain");
                Assert.That(Get(window, "terrainOverview"), Is.SameAs(root), "Repeated clicks must not duplicate the whole map.");
                Call(window, "ClearPatchPreview");
                Assert.That(root != null, Is.True, "Changing the editable area must keep the whole map visible.");
                Assert.That(ShaderUtil.ShaderHasError(Shader.Find("Hidden/Voyage/TerrainOverview")), Is.False);
                Call(window, "ClearPreview");
                Assert.That(root == null, Is.True);
                Assert.That(prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial, Is.SameAs(originalMaterial));
                foreach (var filter in sourceMeshes) Assert.That(filter.sharedMesh != null, Is.True);
            }
            finally { Object.DestroyImmediate(window); }
        }

        [Test]
        public void DeferredRoadBrushDoesNotLoadTilesAndAllowsGrassRepainting()
        {
            var window = ScriptableObject.CreateInstance(Find("GrassFlow.Editor.GrassFlowPainterWindow"));
            try
            {
                var weights = new Texture2D(64,64,TextureFormat.RGBA32,false,true);
                var roads = new Texture2D(64,64,TextureFormat.RGBA32,false,true);
                var white = new Color[4096]; for (int i=0;i<white.Length;i++) white[i]=Color.white;
                weights.SetPixels(white); roads.SetPixels(new Color[4096]);
                Set(window,"overviewWeights",weights); Set(window,"overviewRoads",roads);
                Set(window,"overviewBounds",new Bounds(new Vector3(16,0,16),new Vector3(32,1,32)));
                Set(window,"radius",6f); Set(window,"opacity",1f); Set(window,"hardness",1f);
                var pending=(System.Collections.IList)Get(Get(window,"session"),"strokes"); pending.Clear();
                string[] before=AssetDatabase.FindAssets("t:GrassFlowPatch");
                Set(window,"roadTool",true);
                Call(window,"QueueStroke",new Vector3(8,0,16),new Vector3(24,0,16),1f);
                Assert.That(roads.GetPixel(32,32).r,Is.GreaterThan(.99f));
                Assert.That(weights.GetPixel(32,32).r,Is.LessThan(.01f));
                Set(window,"roadTool",false);
                Call(window,"QueueStroke",new Vector3(16,0,16),new Vector3(16,0,16),1f);
                Assert.That(weights.GetPixel(32,32).r,Is.GreaterThan(.99f),"Grass can be painted back onto a road.");
                Assert.That(roads.GetPixel(32,32).r,Is.GreaterThan(.99f),"Repainting grass must retain the dirt surface.");
                Assert.That(Get(window,"terrainIndex"),Is.Null,"No terrain source should be opened during painting.");
                Assert.That(((System.Collections.IDictionary)Get(window,"worldPatches")).Count,Is.Zero);
                Assert.That(AssetDatabase.FindAssets("t:GrassFlowPatch"),Is.EquivalentTo(before));
                Assert.That(pending.Count,Is.EqualTo(2));
                Set(window,"roadTool",true);
                Call(window,"QueueStroke",new Vector3(16,0,16),new Vector3(16,0,16),0f);
                Assert.That(roads.GetPixel(32,32).r,Is.LessThan(.01f));
                Assert.That(weights.GetPixel(32,32).r,Is.GreaterThan(.99f),"Road erasing leaves grass edits untouched.");
                pending.Clear();
            }
            finally { Object.DestroyImmediate(window); }
        }

        [Test]
        public void LastDraftGestureCanBeRemovedAndRestoredWithoutLosingHistory()
        {
            var window=ScriptableObject.CreateInstance(Find("GrassFlow.Editor.GrassFlowPainterWindow"));
            try
            {
                var session=Get(window,"session");
                var strokes=(System.Collections.IList)Get(session,"strokes"); strokes.Clear();
                var removed=(System.Collections.IList)Get(session,"removed"); removed.Clear();
                for(int i=0;i<3;i++)
                {
                    var stroke=Activator.CreateInstance(Find("GrassFlow.Editor.GrassFlowPainterWindow+PaintStroke"));
                    Set(stroke,"gesture",i==0 ? 1 : 2); strokes.Add(stroke);
                }
                Call(window,"UndoLastEdit");
                Assert.That(strokes.Count,Is.EqualTo(1),"One drag may contain multiple mouse events.");
                Assert.That(removed.Count,Is.EqualTo(1));
                Assert.That(((System.Collections.IList)Get(removed[0],"strokes")).Count,Is.EqualTo(2));
                Call(window,"RedoLastEdit"); Assert.That(strokes.Count,Is.EqualTo(3));
                strokes.Clear(); removed.Clear();
            }
            finally { Object.DestroyImmediate(window); }
        }

        [Test]
        public void AppliedHistoryRestoresBeforeAndAfterAndKeepsTheRecord()
        {
            var type=Find("GrassFlow.Editor.TerrainPaintHistory");
            string path="Assets/PaintHistoryTest-"+Guid.NewGuid().ToString("N")+".txt";
            const string index="Library/VoyageTerrainPaintHistory/index.json";
            string previous=File.Exists(index) ? File.ReadAllText(index) : null;
            object batch=type.GetMethod("Begin").Invoke(null,new object[]{"test"});
            string id=(string)Get(batch,"id");
            try
            {
                // Isolate this test's history from the user's catalog.
                Directory.CreateDirectory(Path.GetDirectoryName(index)); File.WriteAllText(index,"{\"batches\":[]}");
                File.WriteAllText(path,"before"); AssetDatabase.ImportAsset(path);
                type.GetMethod("CaptureBefore").Invoke(null,new[]{batch,(object)path});
                File.WriteAllText(path,"after"); AssetDatabase.ImportAsset(path);
                type.GetMethod("Commit").Invoke(null,new[]{batch});
                Assert.That(type.GetMethod("UndoLast").Invoke(null,null),Is.EqualTo(true));
                Assert.That(File.ReadAllText(path),Is.EqualTo("before"));
                var entries=(System.Collections.IList)type.GetProperty("Entries").GetValue(null);
                Assert.That(entries.Count,Is.EqualTo(1)); Assert.That(Get(entries[0],"active"),Is.EqualTo(false));
                Assert.That(type.GetMethod("RedoLast").Invoke(null,null),Is.EqualTo(true));
                Assert.That(File.ReadAllText(path),Is.EqualTo("after"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                if(previous==null) File.Delete(index); else File.WriteAllText(index,previous);
                string folder="Library/VoyageTerrainPaintHistory/"+id;
                foreach(string file in Directory.GetFiles(folder)) File.Delete(file);
                Directory.Delete(folder);
            }
        }

        [Test]
        public void AppliedRoadClearsGrassButLaterGrassStrokeWins()
        {
            var window=ScriptableObject.CreateInstance(Find("GrassFlow.Editor.GrassFlowPainterWindow"));
            var index=ScriptableObject.CreateInstance(Find("Voyage.TerrainSystem.TerrainTileIndex"));
            var settings=ScriptableObject.CreateInstance(Find("Voyage.TerrainSystem.TerrainChunkSettings"));
            var mesh=Surface(); var patch=Patch(mesh);
            try
            {
                Set(settings,"tileSize",16f); Set(index,"settings",settings); Set(window,"terrainIndex",index);
                object record=Activator.CreateInstance(Find("Voyage.TerrainSystem.TerrainTileRecord"));
                Set(record,"coordinate",Vector2Int.zero); ((System.Collections.IList)Get(index,"tiles")).Add(record);
                ((System.Collections.IDictionary)Get(window,"worldPatches")).Add(Vector2Int.zero,patch);
                object stroke=Activator.CreateInstance(Find("GrassFlow.Editor.GrassFlowPainterWindow+PaintStroke"));
                Set(stroke,"from",new Vector3(8,0,8)); Set(stroke,"to",new Vector3(8,0,8));
                Set(stroke,"radius",4f); Set(stroke,"strength",1f); Set(stroke,"hardness",1f); Set(stroke,"target",1f); Set(stroke,"road",true);
                Call(window,"ApplyGrassEdit",stroke);
                var density=(Texture2D)Get(patch,"density");
                Assert.That(density.GetPixel(128,128).r,Is.LessThan(.01f));
                Set(stroke,"road",false); Call(window,"ApplyGrassEdit",stroke);
                Assert.That(density.GetPixel(128,128).r,Is.GreaterThan(.99f));
                Set(stroke,"road",true); Set(stroke,"target",0f); Call(window,"ApplyGrassEdit",stroke);
                Assert.That(density.GetPixel(128,128).r,Is.GreaterThan(.99f));
            }
            finally
            {
                Undo.ClearUndo((Object)Get(patch,"density")); Object.DestroyImmediate(window);
                Object.DestroyImmediate(index); Object.DestroyImmediate(settings); DeletePatch(patch); Object.DestroyImmediate(mesh);
            }
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator RuntimeTerrainShaderDisplaysRoadMaskAsBrownDirt()
        {
            var shader=Shader.Find("Voyage/Terrain/Stylized");
            bool asyncCompilation=ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation=false;
            var oldPipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            var oldQualityPipeline=QualitySettings.renderPipeline;
            var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
            var mesh=Surface(); var material=new Material(shader);
            ShaderUtil.CompilePass(material,0,true);
            mesh.RecalculateNormals();
            var map=ScriptableObject.CreateInstance(Find("Voyage.TerrainSystem.TerrainRoadMap"));
            var mask=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32);
            var image=new Texture2D(64,64,TextureFormat.RGBA32,false,true);
            var previous=RenderTexture.active;
            var terrainObject=new GameObject("Road surface render test");
            terrainObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            terrainObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            var cameraObject=new GameObject("Road surface test camera"); var camera=cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(8,20,8),Quaternion.LookRotation(Vector3.down,Vector3.forward));
            camera.orthographic=true; camera.orthographicSize=8; camera.nearClipPlane=.1f; camera.farClipPlane=40;
            camera.cullingMask=~0; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.magenta; camera.targetTexture=target;
            var lightObject=new GameObject("Road test sunlight"); var light=lightObject.AddComponent<Light>();
            light.type=LightType.Directional; light.color=Color.white; light.intensity=1; light.transform.rotation=Quaternion.Euler(90,0,0);
            Vector4 oldView=Shader.GetGlobalVector("_VoyageTerrainView");
            Color oldColor=Shader.GetGlobalColor("_VoyageGrassEnvironmentColor"); float oldLight=Shader.GetGlobalFloat("_VoyageGrassEnvironmentLight");
            try
            {
                mask.SetPixels(new[] {Color.white,Color.white,Color.white,Color.white}); mask.Apply(false);
                Set(map,"mask",mask); Set(map,"bounds",new Vector4(0,0,16,16)); Call(map,"Bind",map);
                Shader.SetGlobalVector("_VoyageTerrainView",Vector4.zero);
                Shader.SetGlobalColor("_VoyageGrassEnvironmentColor",Color.white); Shader.SetGlobalFloat("_VoyageGrassEnvironmentLight",1);
                yield return null; // Allow the new MeshRenderer to enter Unity's culling scene.
                target.Create();
                Color Render()
                {
                    camera.Render(); RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32);
                }
                Shader.SetGlobalFloat("_VoyageRoadEnabled",0); Color ground=Render();
                Shader.SetGlobalFloat("_VoyageRoadEnabled",1); Color road=Render();
                Directory.CreateDirectory("Logs/GrassFlowValidation"); File.WriteAllBytes("Logs/GrassFlowValidation/road-runtime.png",image.EncodeToPNG());
                Assert.That(road.r,Is.GreaterThan(ground.r+.003f),"Dirt must visibly change the runtime terrain shader.");
                Assert.That(road.r,Is.GreaterThan(road.g)); Assert.That(road.g,Is.GreaterThan(road.b));
                Assert.That(ShaderUtil.ShaderHasError(shader),Is.False);
            }
            finally
            {
                Call(map,"Reload"); Shader.SetGlobalVector("_VoyageTerrainView",oldView);
                Shader.SetGlobalColor("_VoyageGrassEnvironmentColor",oldColor); Shader.SetGlobalFloat("_VoyageGrassEnvironmentLight",oldLight);
                camera.targetTexture=null; RenderTexture.active=previous; Object.DestroyImmediate(target); Object.DestroyImmediate(image);
                Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(terrainObject); Object.DestroyImmediate(lightObject);
                Object.DestroyImmediate(map); Object.DestroyImmediate(mask); Object.DestroyImmediate(material); Object.DestroyImmediate(mesh);
                ShaderUtil.allowAsyncCompilation=asyncCompilation;
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=oldPipeline; QualitySettings.renderPipeline=oldQualityPipeline;
            }
        }

        [Test]
        public void TerrainEditingViewKeepsZoomPivotAboveMountains()
        {
            var window = ScriptableObject.CreateInstance(Find("GrassFlow.Editor.GrassFlowPainterWindow"));
            var view = ScriptableObject.CreateInstance<SceneView>();
            try
            {
                Set(window, "overviewBounds", new Bounds(new Vector3(0, 500, 0), new Vector3(8000, 1200, 8000)));
                foreach (float size in new[] { 5000f, 256f, 2f })
                {
                    Call(window, "SetTerrainView", view, new Vector3(123, -100, 456), size);
                    Assert.That(view.orthographic, Is.True);
                    Assert.That(view.pivot.y, Is.GreaterThan(1100f));
                    Assert.That(view.pivot.x, Is.EqualTo(123f));
                    Assert.That(view.pivot.z, Is.EqualTo(456f));
                    Assert.That(Vector3.Dot(view.rotation * Vector3.forward, Vector3.down), Is.GreaterThan(.999f));
                    Assert.That(view.size, Is.EqualTo(size).Within(.01f));
                    Assert.That(view.cameraSettings.dynamicClip, Is.False);
                    Assert.That(view.cameraSettings.farClip, Is.GreaterThan(1200f));
                }
            }
            finally { Object.DestroyImmediate(view); Object.DestroyImmediate(window); }
        }

        [Test]
        public void WorldBrushPaintsAcrossUnloadedTilesAndUndoRestoresWeights()
        {
            var window = ScriptableObject.CreateInstance(Find("GrassFlow.Editor.GrassFlowPainterWindow"));
            var index = ScriptableObject.CreateInstance(Find("Voyage.TerrainSystem.TerrainTileIndex"));
            var settings = ScriptableObject.CreateInstance(Find("Voyage.TerrainSystem.TerrainChunkSettings"));
            Mesh meshA = Surface(), meshB = Surface(16);
            ScriptableObject a = Patch(meshA), b = Patch(meshB,16);
            var map = new Texture2D(64,32,TextureFormat.RGBA32,false,true);
            try
            {
                Set(settings,"tileSize",16f); Set(index,"settings",settings); Set(window,"terrainIndex",index);
                var records = (System.Collections.IList)Get(index,"tiles");
                var cache = (System.Collections.IDictionary)Get(window,"worldPatches");
                for(int i=0;i<2;i++)
                {
                    object record=Activator.CreateInstance(Find("Voyage.TerrainSystem.TerrainTileRecord"));
                    Set(record,"coordinate",new Vector2Int(i,0)); records.Add(record);
                    cache.Add(new Vector2Int(i,0),i==0?a:b);
                }
                Set(window,"radius",8f); Set(window,"opacity",1f); Set(window,"hardness",1f);
                Set(window,"overviewWeights",map);
                Set(window,"overviewBounds",new Bounds(new Vector3(16,0,8),new Vector3(32,1,16)));
                Assert.That(((System.Collections.IList)Get(window,"patches")).Count, Is.Zero, "No manual 3x3 load is needed.");
                Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
                Call(window,"PaintWorldStroke",new Vector3(15,0,8),new Vector3(17,0,8),0f);
                Assert.That(((Texture2D)Get(a,"density")).GetPixel(240,128).r,Is.LessThan(.01f));
                Assert.That(((Texture2D)Get(b,"density")).GetPixel(16,128).r,Is.LessThan(.01f));
                Assert.That(map.GetPixel(32,16).r,Is.LessThan(.01f));
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); Undo.PerformUndo();
                Call(window,"Refresh");
                Assert.That(((Texture2D)Get(a,"density")).GetPixel(240,128).r,Is.GreaterThan(.99f));
                Assert.That(((Texture2D)Get(b,"density")).GetPixel(16,128).r,Is.GreaterThan(.99f));
                Assert.That(map.GetPixel(32,16).r,Is.GreaterThan(.99f));
            }
            finally
            {
                Undo.ClearUndo((Object)Get(a,"density")); Undo.ClearUndo((Object)Get(b,"density"));
                Object.DestroyImmediate(window); Object.DestroyImmediate(index); Object.DestroyImmediate(settings);
                DeletePatch(a); DeletePatch(b); Object.DestroyImmediate(meshA); Object.DestroyImmediate(meshB);
            }
        }

        [Test]
        public void DensityPreviewRendersRedForDenseAndGreenForSparseWithoutChangingMesh()
        {
            var mesh = Surface(); var originalVertices = mesh.vertices; var originalTriangles = mesh.triangles;
            var patch = Patch(mesh); var density = (Texture2D)Get(patch, "density");
            var shader = Shader.Find("Hidden/Voyage/GrassDensityPreview");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            var commands = new UnityEngine.Rendering.CommandBuffer();
            try
            {
                material.SetTexture("_DensityMap", density);
                material.SetTexture("_SurfaceMap", (Texture2D)Get(patch, "surface"));
                material.SetVector("_PatchBounds", new Vector4(0, 0, 16, 16));
                target.Create();
                Matrix4x4 view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(new Vector3(8,20,8), Quaternion.LookRotation(Vector3.down, Vector3.forward), Vector3.one).inverse;
                commands.SetRenderTarget(target);
                commands.SetViewProjectionMatrices(view, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-8,8,-8,8,.1f,40f), true));
                commands.ClearRenderTarget(true, true, Color.black);
                commands.DrawMesh(mesh, Matrix4x4.identity, material);
                Color Render()
                {
                    UnityEngine.Graphics.ExecuteCommandBuffer(commands);
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply();
                    return image.GetPixel(32,32);
                }
                Color dense = Render();
                Assert.That(dense.r, Is.GreaterThan(.9f)); Assert.That(dense.g, Is.LessThan(.1f));
                Call(patch, "Paint", new Vector3(8,0,8), 32f, 1f, 0f, 1f);
                Color sparse = Render();
                Assert.That(sparse.g, Is.GreaterThan(.9f)); Assert.That(sparse.r, Is.LessThan(.1f));
                CollectionAssert.AreEqual(originalVertices, mesh.vertices);
                CollectionAssert.AreEqual(originalTriangles, mesh.triangles);
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            }
            finally
            {
                RenderTexture.active = previous; commands.Release();
                Object.DestroyImmediate(material); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
                DeletePatch(patch); Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void BrushErasesAddsAndUndoRestoresTexture()
        {
            var mesh = Surface(); var patch = Patch(mesh); var density = (Texture2D)Get(patch, "density");
            try
            {
                Undo.RegisterCompleteObjectUndo(density, "Grass test");
                Call(patch, "Paint", new Vector3(8, 0, 8), 4f, 1f, 0f, .8f);
                Assert.That(density.GetPixel(128, 128).r, Is.LessThan(.01f));
                Assert.That(density.GetPixel(0, 0).r, Is.GreaterThan(.99f));
                Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
                Assert.That(density.GetPixel(128, 128).r, Is.GreaterThan(.99f));
                Call(patch, "Paint", new Vector3(8, 0, 8), 4f, 1f, 0f, .8f);
                Call(patch, "Paint", new Vector3(8, 0, 8), 4f, 1f, .5f, .8f);
                Assert.That(density.GetPixel(128, 128).r, Is.EqualTo(.5f).Within(.01f));
            }
            finally { Undo.ClearUndo(density); DeletePatch(patch); Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void BrushCrossesTileBoundarySymmetrically()
        {
            var a = Surface(); var b = Surface(16); var first = Patch(a); var second = Patch(b, 16);
            try
            {
                foreach (var patch in new[] { first, second }) Call(patch, "Paint", new Vector3(16, 0, 8), 4f, 1f, 0f, .5f);
                float left = ((Texture2D)Get(first, "density")).GetPixel(250, 128).r;
                float right = ((Texture2D)Get(second, "density")).GetPixel(5, 128).r;
                Assert.That(left, Is.LessThan(.1f)); Assert.That(left, Is.EqualTo(right).Within(.01f));
            }
            finally { DeletePatch(first); DeletePatch(second); Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }

        [Test]
        public void PaintCannotCreateGrassOutsideMeshCoverage()
        {
            var mesh = Surface(); mesh.triangles = new[] { 0, 1, 2 }; var patch = Patch(mesh, 0, 0);
            try
            {
                Call(patch, "Paint", new Vector3(8, 0, 8), 32f, 1f, 1f, 1f);
                var density = (Texture2D)Get(patch, "density");
                Assert.That(density.GetPixel(230, 230).r, Is.LessThan(.01f));
                Assert.That(density.GetPixel(20, 20).r, Is.GreaterThan(.99f));
            }
            finally { DeletePatch(patch); Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void StreamedTileUsesSavedPatchAndSkipsLegacyGeneration()
        {
            var go = new GameObject("Painted terrain streaming test");
            var settings = ScriptableObject.CreateInstance(Find("Voyage.TerrainSystem.TerrainChunkSettings"));
            try
            {
                go.SetActive(false);
                var oldGrass = go.AddComponent(Find("Voyage.TerrainSystem.InteractiveGrassTile"));
                var tile = go.AddComponent(Find("Voyage.TerrainSystem.TerrainTileRuntime"));
                Set(settings, "usePaintedGrass", true); Set(tile, "settings", settings); Set(tile, "coordinate", new Vector2Int(-1, -1));
                Call(tile, "InitializeGrass");
                Assert.That(((Behaviour)oldGrass).enabled, Is.False);
                Assert.That(go.GetComponent(Find("GrassFlow.GrassFlowRenderer")), Is.Not.Null);
                Assert.That(tile.GetType().GetProperty("GrassBuildFinished").GetValue(tile), Is.True);
                Assert.That(tile.GetType().GetProperty("NeedsGrassInitialization").GetValue(tile), Is.False);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(settings); }
        }

        [Test]
        public void GpuRenderingRespondsToSavedDensityAndDayNight()
        {
            var mesh = Surface(); var patch = Patch(mesh);
            var host = new GameObject("GrassFlow GPU test"); var cameraObject = new GameObject("GrassFlow camera");
            var camera = cameraObject.AddComponent<Camera>(); var target = new RenderTexture(640, 360, 24);
            var image = new Texture2D(640, 360, TextureFormat.RGB24, false);
            var pressure = new Texture2D(2, 2, TextureFormat.RGBAFloat, false, true);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Set(patch, "bladesPerRow", 64);
                var component = host.AddComponent(Find("GrassFlow.GrassFlowRenderer")); Set(component, "patch", patch);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.transform.SetPositionAndRotation(new Vector3(8, 3, -3), Quaternion.LookRotation(new Vector3(0, -1, 4)));
                camera.targetTexture = target;
                Shader.SetGlobalVector("_VoyageGrassInteractionWorld", Vector4.zero);
                Shader.SetGlobalTexture("_VoyageGrassInteraction", Texture2D.blackTexture);
                Shader.SetGlobalTexture("_VoyageGrassPermanentInteraction", Texture2D.blackTexture);
                Shader.SetGlobalColor("_VoyageGrassEnvironmentColor", Color.white); Shader.SetGlobalFloat("_VoyageGrassEnvironmentLight", 1);
                Directory.CreateDirectory("Logs/GrassFlowValidation");
                float day = Capture(camera, target, image);
                File.WriteAllBytes("Logs/GrassFlowValidation/painted-day.png", image.EncodeToPNG());
                Assert.That(day, Is.GreaterThan(.002f), "Painted grass must actually render through URP.");
                Assert.That(ShaderUtil.ShaderHasError(Resources.Load<Shader>("GrassFlow/GrassShader")), Is.False);
                Set(patch, "windStrength", 0f); Call(patch, "Changed");
                Capture(camera, target, image); var upright = image.GetPixels();
                // Pressure-only contact (no velocity vector) must still flatten stationary grass.
                pressure.SetPixels(new[] { Color.blue, Color.blue, Color.blue, Color.blue }); pressure.Apply();
                Shader.SetGlobalTexture("_VoyageGrassInteraction", pressure);
                Shader.SetGlobalTexture("_VoyageGrassPermanentInteraction", Texture2D.blackTexture);
                Shader.SetGlobalVector("_VoyageGrassInteractionWorld", new Vector4(8, 8, 32, 0));
                Capture(camera, target, image);
                Assert.That(PixelDifference(upright, image.GetPixels()), Is.GreaterThan(.005f), "Stationary contact pressure must deform the actual rendered blades.");
                File.WriteAllBytes("Logs/GrassFlowValidation/pressed-meadow.png", image.EncodeToPNG());
                Shader.SetGlobalVector("_VoyageGrassInteractionWorld", Vector4.zero);
                Capture(camera, target, image);
                Assert.That(PixelDifference(upright, image.GetPixels()), Is.LessThan(.001f), "Clearing the contact field must restore the grass.");
                Set(patch, "windStrength", .6f); Call(patch, "Changed");
                Capture(camera, target, image);
                Assert.That(PixelDifference(upright, image.GetPixels()), Is.GreaterThan(.001f), "Wind must change blade silhouettes.");
                Shader.SetGlobalColor("_VoyageGrassEnvironmentColor", new Color(.46f, .54f, .72f)); Shader.SetGlobalFloat("_VoyageGrassEnvironmentLight", .48f);
                float night = Capture(camera, target, image);
                Assert.That(night, Is.LessThan(day * .75f));
                File.WriteAllBytes("Logs/GrassFlowValidation/painted-night.png", image.EncodeToPNG());
                Call(patch, "Paint", new Vector3(8, 0, 8), 32f, 1f, 0f, 1f);
                float erased = Capture(camera, target, image);
                Assert.That(erased, Is.LessThan(day * .01f), "Erasing must remove indirect draw instances.");
                File.WriteAllBytes("Logs/GrassFlowValidation/erased.png", image.EncodeToPNG());
                // Turning renderer off/on must safely recreate camera buffers.
                ((Behaviour)component).enabled = false; ((Behaviour)component).enabled = true;
                Call(patch, "Paint", new Vector3(8, 0, 8), 32f, 1f, 1f, 1f);
                Assert.That(Capture(camera, target, image), Is.GreaterThan(.001f));
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(host); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
                Object.DestroyImmediate(pressure);
                Shader.SetGlobalTexture("_VoyageGrassInteraction", null);
                Shader.SetGlobalTexture("_VoyageGrassPermanentInteraction", null);
                Shader.SetGlobalVector("_VoyageGrassInteractionWorld", Vector4.zero);
                DeletePatch(patch); Object.DestroyImmediate(mesh);
                Shader.SetGlobalColor("_VoyageGrassEnvironmentColor", Color.white); Shader.SetGlobalFloat("_VoyageGrassEnvironmentLight", 1);
            }
        }

        static float Capture(Camera camera, RenderTexture target, Texture2D image)
        {
            camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            float sum = 0; foreach (var pixel in image.GetPixels()) sum += pixel.grayscale; return sum / (target.width * target.height);
        }
        static float PixelDifference(Color[] a, Color[] b)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++) sum += Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b);
            return sum/(a.Length*3);
        }

        [Test]
        public void PersistentHorizonContainsOnlyLowDetailGeometryBeyondStreamingRadius()
        {
            var horizon=Resources.Load<GameObject>("TerrainSystem/Horizon/TerrainHorizon");
            Assert.That(horizon,Is.Not.Null);
            Assert.That(horizon.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(horizon.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
            var renderers=horizon.GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(renderers.Length,Is.GreaterThan(1));
            bool distant=false; long triangles=0;
            foreach(var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterial.GetFloat("_Horizon"),Is.EqualTo(1));
                Assert.That(renderer.shadowCastingMode,Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh,Is.Not.Null); triangles+=mesh.GetIndexCount(0)/3;
                distant|=mesh.bounds.center.sqrMagnitude>6000f*6000f;
            }
            Assert.That(distant,Is.True,"The backdrop must extend beyond the local 1 km streaming bubble.");
            Assert.That(triangles,Is.LessThan(1000000),"The entire backdrop must remain lightweight.");
        }

        [TestCase(false, 12f, false)]
        [TestCase(false, 180f, false)]
        [TestCase(false, 6000f, false)]
        [TestCase(true, 180f, false)]
        [TestCase(false, 180f, true)]
        public void DistantContoursPreserveSilhouettesWithoutStripingContinuousGround(bool continuousGround, float distance, bool foliage)
        {
            var features = new System.Collections.Generic.List<UnityEngine.Object>();
            var states = new System.Collections.Generic.List<bool>();
            foreach(var path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                var entries = new SerializedObject(asset).FindProperty("m_RendererFeatures");
                for(int i=0;i<entries.arraySize;i++)
                {
                    var feature = entries.GetArrayElementAtIndex(i).objectReferenceValue;
                    if(feature!=null && feature.GetType().Name=="DistantContourFeature")
                    { features.Add(feature); states.Add((bool)feature.GetType().GetProperty("isActive").GetValue(feature)); }
                }
            }
            Assert.That(features.Count,Is.GreaterThan(0));
            var host=new GameObject("Contour camera"); var camera=host.AddComponent<Camera>();
            var cube=GameObject.CreatePrimitive(continuousGround ? PrimitiveType.Plane : PrimitiveType.Cube);
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var target=new RenderTexture(320,180,24); var image=new Texture2D(320,180,TextureFormat.RGB24,false);
            var previous=RenderTexture.active; bool fog=RenderSettings.fog;
            var fogMode=RenderSettings.fogMode; var fogColor=RenderSettings.fogColor;
            float fogStart=RenderSettings.fogStartDistance, fogEnd=RenderSettings.fogEndDistance;
            try
            {
                RenderSettings.fog=false; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.gray;
                if(distance>1800)
                {
                    RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear;
                    RenderSettings.fogColor=Color.gray; RenderSettings.fogStartDistance=240; RenderSettings.fogEndDistance=1800;
                }
                camera.farClipPlane=12000; camera.targetTexture=target;
                cube.transform.position=new Vector3(0,0,distance); cube.transform.localScale=Vector3.one*(distance/6);
                if(foliage) { cube.layer=LayerMask.NameToLayer("VoyageGrass"); Assert.That(cube.layer,Is.GreaterThan(0)); }
                if(continuousGround)
                {
                    cube.transform.position=Vector3.zero; cube.transform.localScale=Vector3.one*1000;
                    camera.transform.position=new Vector3(0,20,0);
                }
                material.SetColor("_BaseColor",Color.gray); cube.GetComponent<Renderer>().sharedMaterial=material;
                foreach(var feature in features) feature.GetType().GetMethod("SetActive").Invoke(feature,new object[]{false});
                Capture(camera,target,image); var without=image.GetPixels();
                foreach(var feature in features) feature.GetType().GetMethod("SetActive").Invoke(feature,new object[]{true});
                Capture(camera,target,image);
                if(continuousGround)
                {
                    var withContours=image.GetPixels(); float error=0;
                    for(int y=0;y<image.height/2-4;y++) for(int x=8;x<image.width-8;x++)
                        error+=Mathf.Abs(withContours[y*image.width+x].r-without[y*image.width+x].r);
                    error/=(image.height/2-4)*(image.width-16);
                    File.WriteAllBytes("Logs/GrassFlowValidation/continuous-ground-contours.png",image.EncodeToPNG());
                    Assert.That(error,Is.LessThan(.0002f),"A continuous perspective plane must not acquire horizontal contour bands.");
                }
                else if(foliage) Assert.That(PixelDifference(without,image.GetPixels()),Is.LessThan(.00005f),"Foliage must not produce dark canopy contours.");
                else Assert.That(PixelDifference(without,image.GetPixels()),Is.GreaterThan(.00005f),"Depth contours must visibly separate same-color distant objects from the background.");
                Directory.CreateDirectory("Logs/GrassFlowValidation");
                File.WriteAllBytes($"Logs/GrassFlowValidation/contours-{distance}.png",image.EncodeToPNG());
                Assert.That(ShaderUtil.ShaderHasError(Shader.Find("Hidden/Voyage/DistantContours")),Is.False);
            }
            finally
            {
                for(int i=0;i<features.Count;i++) features[i].GetType().GetMethod("SetActive").Invoke(features[i],new object[]{states[i]});
                RenderSettings.fog=fog; RenderTexture.active=previous; camera.targetTexture=null;
                RenderSettings.fogMode=fogMode; RenderSettings.fogColor=fogColor;
                RenderSettings.fogStartDistance=fogStart; RenderSettings.fogEndDistance=fogEnd;
                Object.DestroyImmediate(cube); Object.DestroyImmediate(host); Object.DestroyImmediate(material); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
            }
        }
    }
}
