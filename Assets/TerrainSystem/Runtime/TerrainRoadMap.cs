using UnityEngine;

namespace Voyage.TerrainSystem
{
    // World-space surface paint is shared by streamed tiles and the distant horizon.
    // Grass is edited separately: a road never permanently forbids grass growth.
    public sealed class TerrainRoadMap : ScriptableObject
    {
        public const string ResourcePath = "TerrainSystem/TerrainRoadMap";
        public Texture2D mask;
        public Vector4 bounds;

        public static void Bind(TerrainRoadMap map)
        {
            Shader.SetGlobalTexture("_VoyageRoadMap", map != null && map.mask != null ? map.mask : Texture2D.blackTexture);
            Shader.SetGlobalVector("_VoyageRoadBounds", map != null ? map.bounds : new Vector4(0, 0, 1, 1));
            Shader.SetGlobalFloat("_VoyageRoadEnabled", map != null && map.mask != null ? 1f : 0f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Reload() => Bind(Resources.Load<TerrainRoadMap>(ResourcePath));

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorReload() => UnityEditor.EditorApplication.delayCall += Reload;
#endif
    }
}
