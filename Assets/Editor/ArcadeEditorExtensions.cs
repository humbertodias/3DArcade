﻿/* MIT License

 * Copyright (c) 2020 Skurdt
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. */

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

[assembly: SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "UnityEditor", Scope = "namespaceanddescendants", Target = "Arcade.ArcadeEditorExtensions")]

namespace Arcade.ArcadeEditorExtensions
{
    internal enum ModelType
    {
        None,
        Arcade,
        Game,
        Prop
    }

    internal static class GlobalSettings
    {
        internal static readonly bool UseParticleViewsFix = true;
        internal static readonly bool UseUnwantedNodeFix  = true;
    }

    internal static class GlobalPaths
    {
        internal const string MODELS_FOLDER        = "Assets/3darcade/models";
        internal const string ARCADEMODELS_FOLDER  = MODELS_FOLDER + "/arcades";
        internal const string GAMEMODELS_FOLDER    = MODELS_FOLDER + "/games";
        internal const string PROPMODELS_FOLDER    = MODELS_FOLDER + "/props";
        internal const string RESOURCES_FOLDER     = "Assets/Resources";
        internal const string ARCADEPREFABS_FOLDER = RESOURCES_FOLDER + "/Arcades";
        internal const string GAMEPREFABS_FOLDER   = RESOURCES_FOLDER + "/Games";
        internal const string PROPPREFABS_FOLDER   = RESOURCES_FOLDER + "/Props";
    }

    internal sealed class ModelAssetPresetImportPerFolderPreprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (assetImporter.importSettingsMissing)
            {
                string assetDirectory = Path.GetDirectoryName(assetPath);
                while (!string.IsNullOrEmpty(assetDirectory))
                {
                    string[] presetGuids = AssetDatabase.FindAssets("t:Preset", new[] { assetDirectory });
                    foreach (string presetGuid in presetGuids)
                    {
                        string presetPath = AssetDatabase.GUIDToAssetPath(presetGuid);
                        if (Path.GetDirectoryName(presetPath) == assetDirectory)
                        {
                            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
                            if (preset.ApplyTo(assetImporter))
                            {
                                return;
                            }
                        }
                    }
                    assetDirectory = Path.GetDirectoryName(assetDirectory);
                }
            }
        }
    }

    internal sealed class ModelAssetProcessor : AssetPostprocessor
    {
        private void OnPostprocessModel(GameObject obj)
        {
            if (GlobalSettings.UseParticleViewsFix)
            {
                Utils.RemoveParticleViews(obj);
            }

            if (GlobalSettings.UseUnwantedNodeFix)
            {
                Utils.FixUnwantedNode(obj);
            }
        }
    }

    internal sealed class ImportAssistantWindow : EditorWindow
    {
        private static string _savedBrowseDir = string.Empty;
        private static string _externalPath   = string.Empty;
        private static ModelType _modelType   = ModelType.Game;

        private static ImportAssistantWindow _window;
        private static bool _closeAfterImport = true;

        [MenuItem("3DArcade/Import a new Model...", false, 10003)]
        private static void ShowWindow()
        {
            _window         = GetWindow<ImportAssistantWindow>("Import Assistant");
            _window.minSize = new Vector2(290f, 120f);
        }

        private void OnGUI()
        {
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Close after import:", GUILayout.Width(110f));
                _closeAfterImport = GUILayout.Toggle(_closeAfterImport, string.Empty, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Model file:", GUILayout.Width(80f));
                if (GUILayout.Button("Browse", GUILayout.Width(60f), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    _externalPath = ShowSelectModelWindow();
                }
                _externalPath = GUILayout.TextField(_externalPath);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Model type:", GUILayout.Width(80f));
                _modelType = (ModelType)EditorGUILayout.EnumPopup(_modelType);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            if (string.IsNullOrEmpty(_externalPath) || !Path.GetExtension(_externalPath).Equals(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox("No valid model selected!", MessageType.Error);
            }
            else if (_modelType == ModelType.None)
            {
                EditorGUILayout.HelpBox("Select a model type other than 'None'!", MessageType.Error);
            }
            else if (_externalPath.Contains(Application.dataPath))
            {
                EditorGUILayout.HelpBox("The model must be located outside of the project!", MessageType.Error);
            }
            else
            {
                if (GUILayout.Button("Import", GUILayout.Height(32f)))
                {
                    string assetName         = Path.GetFileName(_externalPath);
                    string assetNameNoExt    = Path.GetFileNameWithoutExtension(assetName);
                    string destinationFolder = null;
                    switch (_modelType)
                    {
                        case ModelType.Arcade:
                            destinationFolder = $"{GlobalPaths.ARCADEMODELS_FOLDER}/{assetNameNoExt}";
                            break;
                        case ModelType.Game:
                            destinationFolder = $"{GlobalPaths.GAMEMODELS_FOLDER}/{assetNameNoExt}";
                            break;
                        case ModelType.Prop:
                            destinationFolder = $"{GlobalPaths.PROPMODELS_FOLDER}/{assetNameNoExt}";
                            break;
                    }

                    if (!string.IsNullOrEmpty(destinationFolder))
                    {
                        if (!Directory.Exists(destinationFolder))
                        {
                            _ = Directory.CreateDirectory(destinationFolder);
                        }
                        string destinationPath = Path.Combine(destinationFolder, assetName);
                        File.Copy(_externalPath, destinationPath, true);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath);
                        string assetPath = AssetDatabase.GetAssetPath(asset);
                        ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (modelImporter != null)
                        {
                            if (Utils.ExtractTextures(assetPath, modelImporter))
                            {
                                Utils.ExtractMaterials(assetPath);
                            }
                        }
                        else
                        {
                            Debug.LogError("modelImporter is null");
                        }

                        Utils.SaveAsPrefab(asset, _modelType);

                        if (_closeAfterImport)
                        {
                            _window.Close();
                        }
                    }
                }
            }
        }

        private string ShowSelectModelWindow()
        {
            string filePath = EditorUtility.OpenFilePanel("Select FBX", _savedBrowseDir, "fbx");
            _savedBrowseDir = Path.GetDirectoryName(filePath);
            return filePath;
        }
    }

    internal sealed class ContextMenus
    {
        private static readonly Color MARQUEE_EMISSIVE_COLOR = Color.white;
        private static readonly Color MONITOR_EMISSIVE_COLOR = Color.white;

        // ***************
        // Assets
        // ***************
        [MenuItem("Assets/ArcadeEditorExtensions/Setup model", false, 1000)]
        private static void AssetsSetupModel()
        {
            GameObject selectedObj = Selection.activeGameObject;
            Undo.RecordObject(selectedObj, "Setup model");
            string assetPath = AssetDatabase.GetAssetPath(selectedObj);
            ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter != null)
            {
                if (Utils.ExtractTextures(assetPath, modelImporter))
                {
                    Utils.ExtractMaterials(assetPath);
                }
            }
            else
            {
                Debug.LogError("modelImporter is null");
            }

            ModelType modelType = Utils.GetModelType(assetPath);
            Utils.SaveAsPrefab(selectedObj, modelType);
        }

        [MenuItem("Assets/ArcadeEditorExtensions/Set as transparent", false, 1000)]
        private static void AssetsSetAsTransparent()
        {
            Material selectedObj = Selection.activeObject as Material;
            Undo.RecordObject(selectedObj, "Set as transparent");
            Utils.SetupTransparentMaterial(selectedObj);
        }

        [MenuItem("Assets/ArcadeEditorExtensions/Set as emissive", false, 1000)]
        private static void AssetsSetAsEmissive()
        {
            Material selectedObj = Selection.activeObject as Material;
            Undo.RecordObject(selectedObj, "Set as emissive");
            Utils.SetupEmissiveMaterial(selectedObj, Color.white);
        }

        // ***************
        // GameObject
        // ***************
        [MenuItem("GameObject/ArcadeEditorExtensions/Set as marquee", false, 11)]
        private static void GameObjectSetAsMarquee()
        {
            GameObject selectedObj = Selection.activeObject as GameObject;

            Transform parent = selectedObj.transform.parent;
            if (parent != null && parent.childCount >= 2)
            {
                Undo.RegisterFullObjectHierarchyUndo(parent, "Set as marquee");
                selectedObj.transform.SetAsFirstSibling();
            }

            MeshRenderer meshRenderer = selectedObj.GetComponent<MeshRenderer>();
            Undo.RecordObject(meshRenderer.sharedMaterial, "Set as marquee");
            Utils.SetupEmissiveMaterial(meshRenderer.sharedMaterial, MARQUEE_EMISSIVE_COLOR);
        }

        [MenuItem("GameObject/ArcadeEditorExtensions/Set as monitor", false, 12)]
        private static void GameObjectSetAsMonitor()
        {
            GameObject selectedObj = Selection.activeObject as GameObject;

            Transform parent = selectedObj.transform.parent;
            if (parent != null && parent.childCount >= 2)
            {
                Undo.RegisterFullObjectHierarchyUndo(parent, "Set as monitor");
                selectedObj.transform.SetSiblingIndex(1);
            }

            MeshRenderer meshRenderer = selectedObj.GetComponent<MeshRenderer>();
            Undo.RecordObject(meshRenderer.sharedMaterial, "Set as monitor");
            Utils.SetupEmissiveMaterial(meshRenderer.sharedMaterial, MONITOR_EMISSIVE_COLOR);
        }

        [MenuItem("GameObject/ArcadeEditorExtensions/Set as transparent", false, 13)]
        private static void GameObjectSetAsTransparent()
        {
            GameObject selectedObj = Selection.activeObject as GameObject;

            MeshRenderer meshRenderer = selectedObj.GetComponent<MeshRenderer>();
            Undo.RecordObject(meshRenderer.sharedMaterial, "Set as transparent");
            Utils.SetupTransparentMaterial(meshRenderer.sharedMaterial);
        }

        [MenuItem("GameObject/ArcadeEditorExtensions/Set as emissive", false, 13)]
        private static void GameObjectSetAsEmissive()
        {
            GameObject selectedObj = Selection.activeObject as GameObject;

            MeshRenderer meshRenderer = selectedObj.GetComponent<MeshRenderer>();
            Undo.RecordObject(meshRenderer.sharedMaterial, "Set as emissive");
            Utils.SetupEmissiveMaterial(meshRenderer.sharedMaterial, Color.white);
        }

        // ***************
        // Validation
        // ***************
        [MenuItem("Assets/ArcadeEditorExtensions/Setup model", true)]
        private static bool AssetsSetupModelValidation()
        {
            Object selectedObj = Selection.activeObject;
            if (!selectedObj)
            {
                return false;
            }
            string assetPath = AssetDatabase.GetAssetPath(selectedObj);
            return assetPath.StartsWith(GlobalPaths.MODELS_FOLDER, System.StringComparison.OrdinalIgnoreCase)
                && Path.GetExtension(assetPath).Equals(".fbx", System.StringComparison.OrdinalIgnoreCase);
        }

        [MenuItem("Assets/ArcadeEditorExtensions/Set as transparent", true)]
        private static bool AssetsSetAsTransparentValidation()
        {
            Object selectedObj = Selection.activeObject;
            return selectedObj != null
                && selectedObj.GetType() == typeof(Material)
                && !selectedObj.name.Equals("Default-Material");
        }

        [MenuItem("Assets/ArcadeEditorExtensions/Set as emissive", true)]
        private static bool AssetsSetAsEmissiveValidation()
        {
            return AssetsSetAsTransparentValidation();
        }

        [MenuItem("GameObject/ArcadeEditorExtensions/Set as marquee", true)]
        private static bool GameObjectSetAsMarqueeValidation()
        {
            GameObject selectedObj = Selection.activeObject as GameObject;
            return selectedObj != null
                && selectedObj.TryGetComponent(out MeshRenderer meshRenderer)
                && meshRenderer.sharedMaterials.Length == 1
                && !meshRenderer.sharedMaterial.name.Equals("Default-Material");
        }

        [MenuItem("GameObject/ArcadeEditorExtensions/Set as monitor", true)]
        private static bool GameObjectSetAsMonitorValidation()
        {
            return GameObjectSetAsMarqueeValidation();
        }

        [MenuItem("GameObject/ArcadeEditorExtensions/Set as transparent", true)]
        private static bool GameObject3DArcadeSetAsTransparentValidation()
        {
            return GameObjectSetAsMarqueeValidation();
        }

        [MenuItem("GameObject/ArcadeEditorExtensions/Set as emissive", true)]
        private static bool GameObjectSetAsEmissiveValidation()
        {
            return GameObjectSetAsMarqueeValidation();
        }
    }

    internal static class Utils
    {
        internal static ModelType GetModelType(string assetPath)
        {
            ModelType result = ModelType.None;
            if (assetPath.StartsWith(GlobalPaths.ARCADEMODELS_FOLDER, System.StringComparison.OrdinalIgnoreCase))
            {
                result = ModelType.Arcade;
            }
            else if (assetPath.StartsWith(GlobalPaths.GAMEMODELS_FOLDER, System.StringComparison.OrdinalIgnoreCase))
            {
                result = ModelType.Game;
            }
            else if (assetPath.StartsWith(GlobalPaths.PROPMODELS_FOLDER, System.StringComparison.OrdinalIgnoreCase))
            {
                result = ModelType.Prop;
            }
            return result;
        }

        internal static bool ExtractTextures(string assetPath, ModelImporter modelImporter)
        {
            bool result = false;

            string modelPath = Path.GetDirectoryName(assetPath);
            string texturesDirectory = Path.Combine(modelPath, "textures");

            if (modelImporter.ExtractTextures(texturesDirectory))
            {
                AssetDatabase.Refresh(ImportAssetOptions.Default);
                result = true;
                Debug.Log("Extracted textures");
            }
            else
            {
                Debug.LogError("Failed to extract textures");
            }

            return result;
        }

        internal static void ExtractMaterials(string assetPath)
        {
            string modelPath = Path.GetDirectoryName(assetPath);
            string materialsDirectory = Path.Combine(modelPath, "materials");
            _ = Directory.CreateDirectory(materialsDirectory);

            HashSet<string> assetsToReload = new HashSet<string>();
            IEnumerable<Object> materials = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x.GetType() == typeof(Material));
            foreach (Object material in materials)
            {
                Material m = material as Material;
                if (m != null && m.mainTexture != null)
                {
                    m.color = Color.white;
                }
                string newAssetPath = Path.Combine(materialsDirectory, $"{material.name}.mat");
                newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
                string error = AssetDatabase.ExtractAsset(material, newAssetPath);
                if (string.IsNullOrEmpty(error))
                {
                    _ = assetsToReload.Add(assetPath);
                }
            }

            if (assetsToReload.Count > 0)
            {
                foreach (string path in assetsToReload)
                {
                    _ = AssetDatabase.WriteImportSettingsIfDirty(path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                Debug.Log("Extracted materials");
            }
            else
            {
                Debug.LogWarning("Failed to extract materials (Already extracted?)");
            }
        }

        internal static void SaveAsPrefab(GameObject obj, ModelType modelType)
        {
            GameObject tempObj = Object.Instantiate(obj);
            string prefabsFolder = string.Empty;
            switch (modelType)
            {
                case ModelType.Arcade:
                {
                    prefabsFolder = GlobalPaths.ARCADEPREFABS_FOLDER;
                }
                break;
                case ModelType.Game:
                {
                    prefabsFolder = GlobalPaths.GAMEPREFABS_FOLDER;
                    AddBoxCollider(tempObj);
                }
                break;
                case ModelType.Prop:
                {
                    prefabsFolder = GlobalPaths.PROPPREFABS_FOLDER;
                    AddBoxCollider(tempObj);
                }
                break;
                case ModelType.None:
                default:
                    break;
            }

            if (!Directory.Exists(prefabsFolder))
            {
                _ = Directory.CreateDirectory(prefabsFolder);
            }

            GameObject newObj = PrefabUtility.SaveAsPrefabAsset(tempObj, Path.Combine(prefabsFolder, $"{obj.name}.prefab"), out bool success);
            if (success)
            {
                RenameNodes(newObj, modelType);
                Debug.Log($"{modelType} prefab '{obj.name}' created");
                Selection.activeGameObject = newObj;
                EditorGUIUtility.PingObject(newObj);
                _ = AssetDatabase.OpenAsset(newObj);
            }
            else
            {
                Debug.LogError($"{modelType} prefab '{obj.name}' creation failed");
            }

            Object.DestroyImmediate(tempObj);
        }

        internal static void SetupEmissiveMaterial(Material material, Color color)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Texture mainTex = material.GetTexture("_MainTex");
            if (mainTex)
            {
                material.SetTexture("_EmissionMap", material.GetTexture("_MainTex"));
            }
            material.SetColor("_EmissionColor", color);
        }

        internal static void SetupTransparentMaterial(Material material)
        {
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            material.SetOverrideTag("RenderType", "Fade");
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Mode", 3);
            material.SetInt("_ZWrite", 0);
        }

        internal static void RemoveParticleViews(GameObject obj)
        {
            Transform rootNodeTransform = obj.transform;
            if (rootNodeTransform != null)
            {
                Transform[] tempArray = new Transform[rootNodeTransform.childCount];
                for (int i = 0; i < rootNodeTransform.childCount; ++i)
                {
                    tempArray[i] = rootNodeTransform.GetChild(i);
                }

                for (int i = 0; i < tempArray.Length; ++i)
                {
                    if (tempArray[i].gameObject.name.StartsWith("Particle View", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Object.DestroyImmediate(tempArray[i].gameObject);
                    }
                }
            }
        }

        internal static void FixUnwantedNode(GameObject obj)
        {
            Transform rootNodeTransform = obj.transform;
            if (rootNodeTransform != null)
            {
                if (rootNodeTransform.childCount == 1 && rootNodeTransform.GetChild(0).name.Equals(obj.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    Transform unwantedNodeTransform = rootNodeTransform.GetChild(0);

                    rootNodeTransform.rotation = Quaternion.identity;
                    unwantedNodeTransform.rotation = Quaternion.identity;

                    Transform[] tempArray = new Transform[unwantedNodeTransform.childCount];
                    for (int i = 0; i < unwantedNodeTransform.childCount; ++i)
                    {
                        tempArray[i] = unwantedNodeTransform.GetChild(i);
                    }

                    for (int i = 0; i < tempArray.Length; ++i)
                    {
                        tempArray[i].parent = rootNodeTransform;
                    }

                    Object.DestroyImmediate(unwantedNodeTransform.gameObject);
                }
            }
        }

        private static List<GameObject> GetAllChildren(GameObject obj, bool getNestedNodes)
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Transform child in obj.transform)
            {
                GameObject childObj = child.gameObject;
                result.Add(childObj);
                if (getNestedNodes)
                {
                    result.AddRange(GetAllChildren(childObj, getNestedNodes));
                }
            }
            return result;
        }

        private static void RenameNodes(GameObject obj, ModelType modelType)
        {
            List<GameObject> children = GetAllChildren(obj, modelType == ModelType.Arcade);
            for (int i = 0; i < children.Count; ++i)
            {
                GameObject child = children[i];
                if (!child.name.StartsWith(obj.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (modelType != ModelType.Arcade
                        && (!child.name.StartsWith("01", System.StringComparison.Ordinal) && !child.name.Contains(obj.name))
                        && (!child.name.StartsWith("02", System.StringComparison.Ordinal) && !child.name.Contains(obj.name)))
                    {
                        if (i == 0)
                        {
                            child.name = $"01_{obj.name}_{child.name}";
                        }
                        else if (i == 1)
                        {
                            child.name = $"02_{obj.name}_{child.name}";
                        }
                        else
                        {
                            child.name = $"{obj.name}_{child.name}";
                        }
                    }
                    else
                    {
                        child.name = $"{obj.name}_{child.name}";
                    }
                }
            }
        }

        private static void AddBoxCollider(GameObject obj)
        {
            if (obj.GetComponent<MeshCollider>() || obj.GetComponentInChildren<MeshCollider>() || obj.GetComponentInChildren<BoxCollider>())
            {
                return;
            }

            BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = obj.AddComponent<BoxCollider>();
            }

            Transform rootTransform = obj.transform;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            Transform[] childrenTransforms = obj.GetComponentsInChildren<Transform>();
            foreach (Transform childTransform in childrenTransforms)
            {
                Renderer childRenderer = childTransform.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    bounds.Encapsulate(childRenderer.bounds);
                }
                boxCollider.center = bounds.center - rootTransform.position;
                boxCollider.size = bounds.size;
            }
        }
    }
}
