using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrefabLightmapData : MonoBehaviour
{
    [System.Serializable]
    public struct RendererInfo
    {
        public Renderer renderer;
        public int lightmapIndex;
        public Vector4 lightmapOffsetScale;
    }

    [System.Serializable]
    public struct LightInfo
    {
        public Light light;
        public int lightmapBaketype;
        public int mixedLightingMode;
    }

    [SerializeField] private RendererInfo[] m_RendererInfo = default;
    [SerializeField] private Texture2D[] m_Lightmaps       = default;
    [SerializeField] private Texture2D[] m_LightmapsDir    = default;
    [SerializeField] private Texture2D[] m_ShadowMasks     = default;
    [SerializeField] private LightInfo[] m_LightInfo       = default;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Init()
    {
        if (m_RendererInfo == null || m_RendererInfo.Length == 0)
        {
            return;
        }

        LightmapData[] lightmaps = LightmapSettings.lightmaps;
        int[] offsetsindexes = new int[m_Lightmaps.Length];
        int counttotal = lightmaps.Length;
        List<LightmapData> combinedLightmaps = new List<LightmapData>();

        for (int i = 0; i < m_Lightmaps.Length; i++)
        {
            bool exists = false;
            for (int j = 0; j < lightmaps.Length; j++)
            {
                if (m_Lightmaps[i] == lightmaps[j].lightmapColor)
                {
                    exists = true;
                    offsetsindexes[i] = j;

                }
            }
            if (!exists)
            {
                offsetsindexes[i] = counttotal;
                LightmapData newlightmapdata = new LightmapData
                {
                    lightmapColor = m_Lightmaps[i],
                    lightmapDir = m_LightmapsDir.Length == m_Lightmaps.Length ? m_LightmapsDir[i] : default,
                    shadowMask = m_ShadowMasks.Length == m_Lightmaps.Length ? m_ShadowMasks[i] : default,
                };

                combinedLightmaps.Add(newlightmapdata);

                counttotal += 1;
            }
        }

        LightmapData[] combinedLightmaps2 = new LightmapData[counttotal];

        lightmaps.CopyTo(combinedLightmaps2, 0);
        combinedLightmaps.ToArray().CopyTo(combinedLightmaps2, lightmaps.Length);

        bool directional = true;

        foreach (Texture2D t in m_LightmapsDir)
        {
            if (t == null)
            {
                directional = false;
                break;
            }
        }

        LightmapSettings.lightmapsMode = (m_LightmapsDir.Length == m_Lightmaps.Length && directional) ? LightmapsMode.CombinedDirectional : LightmapsMode.NonDirectional;
        ApplyRendererInfo(m_RendererInfo, offsetsindexes, m_LightInfo);
        LightmapSettings.lightmaps = combinedLightmaps2;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Init();
    }

    private static void ApplyRendererInfo(RendererInfo[] infos, int[] lightmapOffsetIndex, LightInfo[] lightsInfo)
    {
        for (int i = 0; i < infos.Length; i++)
        {
            RendererInfo info = infos[i];

            info.renderer.lightmapIndex = lightmapOffsetIndex[info.lightmapIndex];
            info.renderer.lightmapScaleOffset = info.lightmapOffsetScale;

            // You have to release shaders.
            Material[] mat = info.renderer.sharedMaterials;
            for (int j = 0; j < mat.Length; j++)
            {
                if (mat[j] != null && Shader.Find(mat[j].shader.name) != null)
                {
                    mat[j].shader = Shader.Find(mat[j].shader.name);
                }
            }
        }

        for (int i = 0; i < lightsInfo.Length; i++)
        {
            LightBakingOutput bakingOutput = new LightBakingOutput
            {
                isBaked = true,
                lightmapBakeType = (LightmapBakeType)lightsInfo[i].lightmapBaketype,
                mixedLightingMode = (MixedLightingMode)lightsInfo[i].mixedLightingMode
            };

            lightsInfo[i].light.bakingOutput = bakingOutput;
        }
    }

#if UNITY_EDITOR
    [MenuItem("3DArcade_r/Bake Prefab Lightmaps", false, 100), SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
    private static void GenerateLightmapInfo()
    {
        if (Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.OnDemand)
        {
            Debug.LogError("ExtractLightmapData requires that you have baked you lightmaps and Auto mode is disabled.");
            return;
        }
        _ = Lightmapping.Bake();

        PrefabLightmapData[] prefabs = FindObjectsOfType<PrefabLightmapData>();

        foreach (PrefabLightmapData instance in prefabs)
        {
            GameObject gameObject = instance.gameObject;
            List<RendererInfo> rendererInfos = new List<RendererInfo>();
            List<Texture2D> lightmaps = new List<Texture2D>();
            List<Texture2D> lightmapsDir = new List<Texture2D>();
            List<Texture2D> shadowMasks = new List<Texture2D>();
            List<LightInfo> lightsInfos = new List<LightInfo>();

            GenerateLightmapInfo(gameObject, rendererInfos, lightmaps, lightmapsDir, shadowMasks, lightsInfos);

            instance.m_RendererInfo = rendererInfos.ToArray();
            instance.m_Lightmaps = lightmaps.ToArray();
            instance.m_LightmapsDir = lightmapsDir.ToArray();
            instance.m_LightInfo = lightsInfos.ToArray();
            instance.m_ShadowMasks = shadowMasks.ToArray();

            GameObject targetPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance.gameObject) as GameObject;
            if (targetPrefab != null)
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(instance.gameObject);
                if (root != null)
                {
                    GameObject rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(instance.gameObject);
                    string rootPath = AssetDatabase.GetAssetPath(rootPrefab);
                    _ = PrefabUtility.UnpackPrefabInstanceAndReturnNewOutermostRoots(root, PrefabUnpackMode.OutermostRoot);
                    try
                    {
                        PrefabUtility.ApplyPrefabInstance(instance.gameObject, InteractionMode.AutomatedAction);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        _ = PrefabUtility.SaveAsPrefabAssetAndConnect(root, rootPath, InteractionMode.AutomatedAction);
                    }
                }
                else
                {
                    PrefabUtility.ApplyPrefabInstance(instance.gameObject, InteractionMode.AutomatedAction);
                }
            }
        }
    }

    private static void GenerateLightmapInfo(GameObject root, List<RendererInfo> rendererInfos, List<Texture2D> lightmaps, List<Texture2D> lightmapsDir, List<Texture2D> shadowMasks, List<LightInfo> lightsInfo)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.lightmapIndex != -1 && renderer.lightmapIndex < 65534)
            {
                RendererInfo info = new RendererInfo
                {
                    renderer = renderer
                };

                if (renderer.lightmapScaleOffset != Vector4.zero)
                {
                    info.lightmapOffsetScale = renderer.lightmapScaleOffset;

                    Texture2D lightmap = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapColor;
                    Texture2D lightmapDir = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapDir;
                    Texture2D shadowMask = LightmapSettings.lightmaps[renderer.lightmapIndex].shadowMask;

                    info.lightmapIndex = lightmaps.IndexOf(lightmap);
                    if (info.lightmapIndex == -1)
                    {
                        info.lightmapIndex = lightmaps.Count;
                        lightmaps.Add(lightmap);
                        lightmapsDir.Add(lightmapDir);
                        shadowMasks.Add(shadowMask);
                    }

                    rendererInfos.Add(info);
                }
            }
        }

        Light[] lights = root.GetComponentsInChildren<Light>(true);

        foreach (Light l in lights)
        {
            LightInfo lightInfo = new LightInfo
            {
                light = l,
                lightmapBaketype = (int)l.lightmapBakeType,
                mixedLightingMode = (int)LightmapEditorSettings.mixedBakeMode
            };
            lightsInfo.Add(lightInfo);
        }
    }
#endif
}