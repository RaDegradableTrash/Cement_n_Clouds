using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GrassFlow.Editor
{
    // Persistent before/after snapshots. Only explicit Apply creates a batch;
    // recording brush input never reads terrain assets or writes asset files.
    public static class TerrainPaintHistory
    {
        const string Root = "Library/VoyageTerrainPaintHistory";
        [Serializable] public class FileState { public string path, slot; public bool before, after; }
        [Serializable] public class Batch
        {
            public string id, label, strokes;
            public bool active = true, redoable;
            public List<FileState> files = new List<FileState>();
        }
        [Serializable] class Catalog { public List<Batch> batches = new List<Batch>(); }
        static Catalog Read() => File.Exists(Root + "/index.json") ? JsonUtility.FromJson<Catalog>(File.ReadAllText(Root + "/index.json")) : new Catalog();
        static void Save(Catalog data) { Directory.CreateDirectory(Root); File.WriteAllText(Root + "/index.json", JsonUtility.ToJson(data)); }
        public static List<Batch> Entries => Read().batches;
        public static Batch Begin(string strokes) => new Batch { id = Guid.NewGuid().ToString("N"), label = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), strokes = strokes };
        static string CheckedAsset(string path)
        {
            string full = Path.GetFullPath(path).Replace('\\','/');
            string assets = Path.GetFullPath("Assets").Replace('\\','/') + "/";
            if (!full.StartsWith(assets, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Paint history path must stay inside Assets.");
            return full;
        }
        static string Snapshot(Batch batch, FileState file, string side) => Root + "/" + batch.id + "/" + file.slot + "." + side;
        public static void CaptureBefore(Batch batch, string path)
        {
            if (batch == null || batch.files.Exists(f => f.path == path)) return;
            CheckedAsset(path);
            // Include unsaved asset edits (for example palette changes) in the baseline.
            if (File.Exists(path))
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) AssetDatabase.SaveAssetIfDirty(asset);
            var file = new FileState { path = path, slot = batch.files.Count.ToString(), before = File.Exists(path) };
            Directory.CreateDirectory(Root + "/" + batch.id);
            if (file.before) File.Copy(path, Snapshot(batch,file,"before"), true);
            if (File.Exists(path+".meta")) File.Copy(path+".meta", Snapshot(batch,file,"meta"), true);
            batch.files.Add(file);
        }
        public static void Commit(Batch batch)
        {
            foreach (var file in batch.files)
            {
                file.after = File.Exists(file.path);
                if (file.after) File.Copy(file.path,Snapshot(batch,file,"after"),true);
                if (File.Exists(file.path+".meta")) File.Copy(file.path+".meta",Snapshot(batch,file,"meta"),true);
            }
            var data=Read();
            foreach (var old in data.batches) old.redoable=false; // Retain abandoned branches as records, never silently replay them.
            data.batches.Add(batch); Save(data);
        }
        public static bool CanUndo => Entries.Exists(b => b.active);
        public static bool CanRedo => Entries.Exists(b => b.redoable);
        public static bool UndoLast()
        {
            var data=Read(); var batch=data.batches.FindLast(b => b.active);
            if (batch==null) return false;
            Restore(batch,false); batch.active=false; batch.redoable=true; Save(data); return true;
        }
        public static bool RedoLast()
        {
            var data=Read(); var batch=data.batches.Find(b => b.redoable);
            if (batch==null) return false;
            Restore(batch,true); batch.active=true; batch.redoable=false; Save(data); return true;
        }
        static void Restore(Batch batch, bool after)
        {
            // Validate every source before changing any asset.
            foreach (var file in batch.files)
            {
                CheckedAsset(file.path);
                if ((after ? file.after : file.before) && !File.Exists(Snapshot(batch,file,after ? "after" : "before")))
                    throw new IOException("Paint history snapshot is missing: " + file.path);
            }
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var file in batch.files)
                {
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(file.path)) Undo.ClearUndo(asset);
                    if (!(after ? file.after : file.before)) { AssetDatabase.DeleteAsset(file.path); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(file.path));
                    File.Copy(Snapshot(batch,file,after ? "after" : "before"),file.path,true);
                    string meta=Snapshot(batch,file,"meta");
                    if (File.Exists(meta)) File.Copy(meta,file.path+".meta",true);
                    AssetDatabase.ImportAsset(file.path,ImportAssetOptions.ForceUpdate);
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            Voyage.TerrainSystem.TerrainRoadMap.Reload();
        }
        public static void ClearAllRecords()
        {
            string path=Path.GetFullPath(Root), expected=Path.Combine(Path.GetFullPath("Library"),"VoyageTerrainPaintHistory");
            if (!string.Equals(path,expected,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid history path.");
            if (Directory.Exists(path)) Directory.Delete(path,true);
        }
    }
}
