using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System;
using UnityEngine.UI;
using UnityEngine.TerrainTools;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using PlasticGui;
using TreeEditor;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.Graphs;
using System.Runtime.InteropServices;

public class TerrainGenerator : EditorWindow
{
    Vector2 scrollPosition;
    public Terrain terrainObject;
    private TerrainData _terrainData;
    
    private Vector2Int _scaleTerrain;
    protected int _heightmapResolution;
    
    #region Folds
    bool terrainParameters = false;
    bool scaleTerrainFold = false;
    bool heightmapParametersFold = false;
    bool heightmapLayersFold = true;
    bool textureLayersFold = false;
    bool treePrototypesFold = false;
    #endregion
    
    #region Heightmap
    private List<HeightMapLayer> heightMapLayers = new List<HeightMapLayer>();
    public float smoothStrength = 0f;
    bool canGenerateAllLayers = true;
    #endregion
    
    #region Texture
    public struct TextureLayer
    {
        public string textureName;
        public bool isRangeTexturing;
        public float minHeight;
        public float maxHeight;
        public bool isPerlin;
        public float fadeDistance;
        public PerlinNoiseSettings perlinNoiseSettings;
        
            
        
        public TextureLayer(string textureName = "")
        {
            this.textureName = textureName;
            this.isRangeTexturing = false;
            this.minHeight = 0;
            this.maxHeight = 0;
            this.fadeDistance = 0;
            this.isPerlin = false;
            this.perlinNoiseSettings = new PerlinNoiseSettings(false);
        }

        public TextureLayer(string textureName = "", bool isRangeTexturing = false,
                            float minHeight = 0, float maxHeight = 0, float fadeDistance = 0,
                            bool isPerlin = false, PerlinNoiseSettings perlinNoiseSettings = new PerlinNoiseSettings())
        {
            this.textureName = textureName;
            this.isRangeTexturing = isRangeTexturing;
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
            this.fadeDistance = fadeDistance;
            this.isPerlin = isPerlin;
            this.perlinNoiseSettings = perlinNoiseSettings;
        }

        public string GetTextureName()
        {
            return textureName;
        }
        
        public void SetTextureName(string textureName)
        {
            this.textureName = textureName;
        }
    }
    // public List<TextureLayer> textureLayers = new List<TextureLayer>();
    public TextureLayer[] textureLayers = new TextureLayer[0];
    public int terrainLayersCount = 0;
    #endregion
    
    #region Trees
    bool[] enableTree;
    float maxSlopeAngle = 90;
    float maxTreeSpawnLimit = 1000;
    float minTreeSpawnLimit = 0;
    float treeMinHeight = 1;
    float treeMaxHeight = 1;
    int treeAmount = 100;
    bool enablePerlinTree = false;
    PerlinNoiseSettings treePerlinSettings = new PerlinNoiseSettings(false);
    #endregion
    
    GUIStyle titleStyle;
    GUIStyle foldoutStyle;
    
    public struct PerlinNoiseSettings
    {
        public bool showNoisePreview;
        public bool invertNoise;
        public float scale;
        public float contrast;
        public float brightness;
        public float offsetY;
        public float offsetX;
        
        public PerlinNoiseSettings(bool showNoisePreview = false, bool invertNoise = false, float perlinScale = 10, float perlinContrast = 1, float brightness = 0, float perlinOffsetX = 0, float perlinOffsetY = 0)
        {
            this.showNoisePreview = showNoisePreview;
            this.invertNoise = invertNoise;
            this.scale = perlinScale;
            this.contrast = perlinContrast;
            this.brightness = brightness;
            this.offsetY = perlinOffsetX;
            this.offsetX = perlinOffsetY;
        }
    }
    
    
    
    
    
    [MenuItem("Tools/Terrain Generator")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TerrainGenerator));
    }
    
    private void OnGUI()
    {
        #region Intro
        titleStyle = new GUIStyle(EditorStyles.helpBox);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.wordWrap = true;
        titleStyle.fontSize = 24;
        titleStyle.fontStyle = FontStyle.Bold;
        
        foldoutStyle = new GUIStyle(EditorStyles.foldout);
        foldoutStyle.fontStyle = FontStyle.Bold;
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        //Справка
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Terrain Generator for Unity3d", titleStyle);
        EditorGUILayout.LabelField("made by student Kharchenko Evgeniy IP-012", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        
        
        EditorGUILayout.Space(10);
        EditorGUI.BeginChangeCheck();
        terrainObject = EditorGUILayout.ObjectField("Terrain", terrainObject, typeof(Terrain), true) as Terrain;
        //Параметры по умолчанию
        if(EditorGUI.EndChangeCheck())
        {
            if(terrainObject)
            {
                _terrainData = terrainObject.terrainData;
                _scaleTerrain.x = (int)_terrainData.size.x;
                _scaleTerrain.y = (int)_terrainData.size.y;
                _heightmapResolution = (int)Math.Log(_terrainData.heightmapResolution, 2);
            }
        }
        
        //Панель информации
        if(terrainObject)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Scale: " + terrainObject.terrainData.size);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Heightmap Resolution: " + terrainObject.terrainData.heightmapResolution);
            EditorGUILayout.LabelField("Alphamap resolution: " + terrainObject.terrainData.alphamapResolution);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.HelpBox("Please, select terrain to edit", MessageType.Warning);
            GUI.enabled = false;
        }
        #endregion
        
        EditorGUILayout.Space(10);
        
        #region Terrain params
        EditorGUILayout.BeginVertical("PopupCurveSwatchBackground");
        terrainParameters = EditorGUILayout.Foldout(terrainParameters, "Terrain Parameters", foldoutStyle);
        EditorGUILayout.EndVertical();
        
        if(terrainParameters && terrainObject)
        {
            EditorGUI.indentLevel++;
            scaleTerrainFold = EditorGUILayout.Foldout(scaleTerrainFold, "Terrain Scale");
            if(scaleTerrainFold)
            {
                _scaleTerrain = EditorGUILayout.Vector2IntField("", _scaleTerrain);
                _scaleTerrain.x = (_scaleTerrain.x < 0) ? 1 : _scaleTerrain.x;
                _scaleTerrain.y = (_scaleTerrain.y < 0) ? 1 : _scaleTerrain.y;
            }
            
            EditorGUILayout.BeginHorizontal();
            _heightmapResolution = EditorGUILayout.IntSlider("Heightmap Resolution", _heightmapResolution, 5, 12);
            EditorGUILayout.LabelField("" + ((int)Mathf.Pow(2f, _heightmapResolution) + 1), EditorStyles.numberField, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(50));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Changing heightmap resolution will reset current heightmap", MessageType.Warning);
            EditorGUI.indentLevel--;
            
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Apply Scale"))
            {
                ApplyTerrainScale();
                Debug.Log("Scale Applied!");
            }
            if(GUILayout.Button("Apply Heightmap Resolution"))
            {
                ApplyHeightMapResolution();
                Debug.Log("Heightmap Resolution Applied!");
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion
        
        GUILayout.Label("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
        
        #region Heightmap params
        EditorGUILayout.BeginVertical("PopupCurveSwatchBackground");
        heightmapParametersFold = EditorGUILayout.Foldout(heightmapParametersFold, "HeightMap Parameters", foldoutStyle);
        EditorGUILayout.EndVertical();
        
        if(heightmapParametersFold && terrainObject)
        {
            //Слои высот
            EditorGUILayout.BeginVertical("Box");
            heightmapLayersFold = EditorGUILayout.Foldout(heightmapLayersFold, "Height Layers");
            EditorGUI.indentLevel++;
            if(heightmapLayersFold)
            {
                for (int i = 0; i < heightMapLayers.Count; i++)
                {
                    heightMapLayers[i].perlinScale = Mathf.Max(heightMapLayers[i].perlinScale, 0);
                    heightMapLayers[i].maxY = (int)_terrainData.size.y;
                    heightMapLayers[i].minHeight = Mathf.Clamp(heightMapLayers[i].minHeight, 0, heightMapLayers[i].maxHeight);
                    heightMapLayers[i].maxHeight = Mathf.Clamp(heightMapLayers[i].maxHeight, 0, _scaleTerrain.y);
                    
                    //Один слой
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    heightMapLayers[i].heightMapLayerFold = EditorGUILayout.Foldout(heightMapLayers[i].heightMapLayerFold, $"Layer {i + 1}");
                    if(heightMapLayers[i].heightMapLayerFold)
                    {
                        heightMapLayers[i].enabled = EditorGUILayout.BeginToggleGroup("Enable Layer", heightMapLayers[i].enabled);
                        if(heightMapLayers[i].GetGenerationType() == "Height Map" && heightMapLayers[i].heightMapTexture == null)
                        {
                            canGenerateAllLayers = false;
                        }
                        else
                        {
                            canGenerateAllLayers = true;
                        }
                        heightMapLayers[i].DrawGUI();
                        
                        if(GUILayout.Button("Generate Layer"))
                        {
                            if(canGenerateAllLayers)
                            {
                                Undo.RegisterCompleteObjectUndo(_terrainData, "Generate heightmap");
                                _terrainData.SetHeights(0, 0, GenerateHeights(i));
                            }
                            else
                            {
                                Debug.LogError("Please, select heightmap texture");
                            }
                        }
                        EditorGUILayout.EndToggleGroup();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            //Add Layer Button
            if (GUILayout.Button("Add Layer"))
            {
                heightMapLayers.Add(new HeightMapLayer());
                Debug.Log("Layer Added");
            }
            
            //Remove Last Layer Button
            if (GUILayout.Button("Remove Last Layer"))
            {
                if (heightMapLayers.Count > 0)
                {
                    heightMapLayers.RemoveAt(heightMapLayers.Count - 1);
                }
                Debug.Log("Layer Removed");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            
            // Smooth
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            smoothStrength = EditorGUILayout.FloatField("Smooth Strength", smoothStrength);
            if(GUILayout.Button("Smooth Terrain"))
            {
                SmoothTerrain();
                Debug.Log("Terrain Smoothed by " + smoothStrength);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(10);
            if(!canGenerateAllLayers)
            {
                GUI.enabled = false;
            }
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Generate all layers"))
            {
                Debug.Log("Generating...");
                GenerateHeightMap();
                Debug.Log("Heightmap Generated!");
            }
            
            if(GUILayout.Button("Reset Heightmap"))
            {
                ResetHeightMap();
                Debug.Log("Heightmap reseted");
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        #endregion
        
        EditorGUILayout.Space(10);  
          
        #region Texturing
        if(terrainObject)
            ApplyTextureLayers();
        
        EditorGUILayout.BeginVertical("PopupCurveSwatchBackground");
        textureLayersFold = EditorGUILayout.Foldout(textureLayersFold, "Texture Parameters", foldoutStyle);
        EditorGUILayout.EndVertical();
        if(textureLayersFold && terrainObject)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUI.indentLevel++;
            int textureLayerIndex;
            for (int i = 0; i < _terrainData.terrainLayers.Length; i++)
            {
                TerrainLayer terrainLayer = _terrainData.terrainLayers[i];
                PerlinNoiseSettings perlinNoiseSettings = new PerlinNoiseSettings(false);
                textureLayerIndex = Array.FindIndex(textureLayers, layer => layer.GetTextureName() == terrainLayer.diffuseTexture.name);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                //Текстура
                GUILayout.Label(terrainLayer.diffuseTexture, GUILayout.Width(64), GUILayout.Height(64));
                EditorGUILayout.BeginVertical();
                
                //Имя текстуры
                GUILayout.Label("Texture: " + terrainLayer.diffuseTexture.name);
                
                //Размер плитки
                terrainLayer.tileSize = EditorGUILayout.Vector2Field("Tile Size: ", terrainLayer.tileSize);
                
                //Отступ плитки
                terrainLayer.tileOffset = EditorGUILayout.Vector2Field("Tile Offset: ", terrainLayer.tileOffset);
                
                //Диапозон
                bool isRangeTexturing = EditorGUILayout.BeginToggleGroup("Range texturing", textureLayers[textureLayerIndex].isRangeTexturing);
                float minHeight = (int)EditorGUILayout.DelayedFloatField("Texture Min Height", textureLayers[textureLayerIndex].minHeight);
                float maxHeight = (int)EditorGUILayout.DelayedFloatField("Texture Max Height", textureLayers[textureLayerIndex].maxHeight);
                EditorGUILayout.MinMaxSlider(ref minHeight, ref maxHeight, 0, _terrainData.size.y);
                minHeight = Mathf.Clamp(minHeight, 0, maxHeight);
                maxHeight = Mathf.Clamp(maxHeight, 0, _terrainData.size.y);
                if(!isRangeTexturing)
                {
                    minHeight = 0;
                    maxHeight = (int)_terrainData.size.y;
                }
                
                //Затухание
                float fadeDistance = EditorGUILayout.DelayedFloatField("Fade Amount", textureLayers[textureLayerIndex].fadeDistance);
                fadeDistance = Mathf.Clamp(fadeDistance, 0, maxHeight-minHeight);
                EditorGUILayout.EndToggleGroup();
                
                bool isPerlin = EditorGUILayout.Toggle("Perlin Noise", textureLayers[textureLayerIndex].isPerlin);                
                if(isPerlin)
                {
                    EditorGUI.indentLevel++;                    
                    textureLayers[textureLayerIndex].perlinNoiseSettings = DrawPerlinNoiseSettings(textureLayers[textureLayerIndex].perlinNoiseSettings);
                }
                textureLayers[textureLayerIndex] = new TextureLayer(textureLayers[textureLayerIndex].GetTextureName(),
                                                                    isRangeTexturing, 
                                                                    minHeight,
                                                                    maxHeight,
                                                                    fadeDistance,
                                                                    isPerlin,
                                                                    textureLayers[textureLayerIndex].perlinNoiseSettings
                                                                    );
                
                
                
                EditorGUILayout.BeginHorizontal();
                //Кнопка тектурирования
                if(GUILayout.Button("Texture"))
                {
                    ApplyTexture(i, textureLayers[textureLayerIndex]);
                    terrainObject.Flush();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            if(GUILayout.Button("Reset Texture"))
            {
                ResetTextures();
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        EditorGUILayout.Space(10);
        
        #region Trees
        
        TreePrototype[] treePrototypes = null;
        if(terrainObject)
        {
            treePrototypes = _terrainData.treePrototypes;
            enableTree = ApplyTreePrototypes();
        }
        EditorGUILayout.BeginVertical("PopupCurveSwatchBackground");
        treePrototypesFold = EditorGUILayout.Foldout(treePrototypesFold, "Tree Parameters", foldoutStyle);
        EditorGUILayout.EndVertical();
        if(treePrototypesFold && terrainObject)
        {
            if(treePrototypes.Length == 0)
            {
                EditorGUILayout.HelpBox("No tree prototypes", MessageType.Info);
            }
            for (int i = 0; i < treePrototypes.Length; i++)
            {
                Rect prefabRect = new Rect(10, 10, 100, 100);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(AssetPreview.GetAssetPreview(treePrototypes[i].prefab), GUILayout.Width(50), GUILayout.Height(50));
                
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Model: " + treePrototypes[i].prefab.name);
                enableTree[i] = EditorGUILayout.Toggle("Include", enableTree[i]);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel++;
            maxSlopeAngle = EditorGUILayout.Slider("Max Slope Angle", maxSlopeAngle, 0.1f, 90);
            minTreeSpawnLimit = (int)Mathf.Clamp(EditorGUILayout.DelayedFloatField("Min Spawn Height", minTreeSpawnLimit), 0, _terrainData.size.y);
            maxTreeSpawnLimit = (int)Mathf.Clamp(EditorGUILayout.DelayedFloatField("Max Spawn Height", maxTreeSpawnLimit), 0, _terrainData.size.y);
            EditorGUILayout.MinMaxSlider(ref minTreeSpawnLimit, ref maxTreeSpawnLimit, 0, _terrainData.size.y);
            treeMinHeight = EditorGUILayout.DelayedFloatField("Tree Min Height", treeMinHeight);
            treeMaxHeight = EditorGUILayout.DelayedFloatField("Tree Max Height", treeMaxHeight);
            EditorGUILayout.MinMaxSlider(ref treeMinHeight, ref treeMaxHeight, 0.1f, 4);
            EditorGUILayout.Space(10);
            
            enablePerlinTree = EditorGUILayout.Toggle("Perlin Noise", enablePerlinTree);
            if (enablePerlinTree)
            {
                EditorGUI.indentLevel++;
                treePerlinSettings = DrawPerlinNoiseSettings(treePerlinSettings);
            }
            
            EditorGUILayout.Space(10);
            EditorGUI.indentLevel--;
            treeAmount = EditorGUILayout.DelayedIntField("Tree Amount", treeAmount);
            if(GUILayout.Button("Place Trees"))
            {
                PlaceTrees();
            }
            if(GUILayout.Button("Delete all Trees"))
            {
                DeleteTrees();
            }
        }
        #endregion
        EditorGUILayout.EndScrollView();
    }
    
    #region GUI functions
    
    PerlinNoiseSettings DrawPerlinNoiseSettings(PerlinNoiseSettings perlinNoiseSettings)
    {
        perlinNoiseSettings.invertNoise = EditorGUILayout.Toggle("Invert Noise", perlinNoiseSettings.invertNoise);
        perlinNoiseSettings.scale = EditorGUILayout.DelayedFloatField("Scale", perlinNoiseSettings.scale);
        perlinNoiseSettings.contrast = EditorGUILayout.Slider("Contrast", perlinNoiseSettings.contrast, 0, 2);
        perlinNoiseSettings.brightness = EditorGUILayout.Slider("Brightness", perlinNoiseSettings.brightness, -1, 1);
        perlinNoiseSettings.offsetX = EditorGUILayout.DelayedFloatField("Offset X", perlinNoiseSettings.offsetX);
        perlinNoiseSettings.offsetY = EditorGUILayout.DelayedFloatField("Offset Y", perlinNoiseSettings.offsetY);
        perlinNoiseSettings.showNoisePreview = EditorGUILayout.Toggle("Show Noise Preview", perlinNoiseSettings.showNoisePreview);
        if(perlinNoiseSettings.showNoisePreview)
        {
            Texture2D perlinTexture = new Texture2D(128, 128);
            perlinTexture.wrapMode = TextureWrapMode.Repeat;
            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 128; y++)
                {
                    float xCoord = (float)x / 128 * perlinNoiseSettings.scale / (_terrainData.heightmapResolution / _terrainData.size.x) + 
                                                    perlinNoiseSettings.offsetY;
                    float yCoord = (float)y / 128 * perlinNoiseSettings.scale / (_terrainData.heightmapResolution / _terrainData.size.x) + 
                                                    perlinNoiseSettings.offsetX;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                    
                    // Примените контраст к значению шума Перлина
                    perlinValue = 0.5f - (0.5f - perlinValue) * Mathf.Pow(perlinNoiseSettings.contrast, 4);
                    //Яркость шума
                    perlinValue = Mathf.Clamp01(perlinValue) + perlinNoiseSettings.brightness;
                    //Инвертировать шум
                    perlinValue = perlinNoiseSettings.invertNoise ? 1-perlinValue : perlinValue;
                    // Определите цвет пикселя на основе значения шума Перлина
                    Color pixelColor = new Color(perlinValue, perlinValue, perlinValue);
                    
                    // Установите цвет пикселя в текстуре
                    perlinTexture.SetPixel(y, x, pixelColor);
                }
            }
            perlinTexture.Apply();
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            GUILayout.Label(perlinTexture);
            GUILayout.EndHorizontal();
        }
        return perlinNoiseSettings;
    }
    
    #endregion
    
    
    #region Generate terrain params
    private void ApplyTerrainScale()
    {
        Undo.RecordObject(_terrainData, "Apply terrain scale");
        float scaleRatioX = _scaleTerrain.x / _terrainData.size.x;
        Vector3 _scaleTerrainV3 = new Vector3(_scaleTerrain.x, _scaleTerrain.y, _scaleTerrain.x);
        _terrainData.size = _scaleTerrainV3;
        terrainObject.Flush();
    }
    
    private void ApplyHeightMapResolution()
    {
        if(_terrainData.heightmapResolution != (int)Mathf.Pow(2f, _heightmapResolution) + 1)
        {
            Undo.RegisterCompleteObjectUndo(_terrainData, "Apply heightmap resolution");
            Vector3 _scaleTerrainV3 = new Vector3(_terrainData.size.x, _terrainData.size.y, _terrainData.size.x);
            _terrainData.heightmapResolution = (int)Mathf.Pow(2f, _heightmapResolution) + 1;
            _terrainData.size = _scaleTerrainV3;
        }
    }
    #endregion
    
    #region Generate heightmap
    private void GenerateHeightMap()
    {
        Undo.RegisterCompleteObjectUndo(_terrainData, "Generate heightmap");
        for (int i = 0; i < heightMapLayers.Count; i++)
        {
            if(heightMapLayers[i].enabled)
            {
                _terrainData.SetHeights(0, 0, GenerateHeights(i));
            }
        }
        
    }
    
    
    float[,] GenerateHeights(int index)
    {
        float[,] heights = new float[(int)_terrainData.heightmapResolution, (int)_terrainData.heightmapResolution];
        float[,] currentHeights = _terrainData.GetHeights(0, 0, _terrainData.heightmapResolution, _terrainData.heightmapResolution);
        
        switch(heightMapLayers[index].GetGenerationType())
        {
            case "Perlin Noise":
                for(int x = 0; x < (int)_terrainData.heightmapResolution; x++)
                {
                    for(int z = 0; z < (int)_terrainData.heightmapResolution; z++)
                    {
                        if(heightMapLayers[index].isGlobal)
                        {
                            heights[x, z] = heightMapLayers[index].minHeight / _terrainData.size.y + 
                                            CalculateHeight(x, z, index) * 
                                            (heightMapLayers[index].maxHeight / _terrainData.size.y - heightMapLayers[index].minHeight / _terrainData.size.y);
                        }
                        else
                        {
                            heights[x, z] = currentHeights[x, z] + CalculateHeight(x, z, index) * heightMapLayers[index].heightOffset;
                        }
                    }
                }
                break;
            case "Height Map":
                Texture2D resizedTexture = ResizeTexture(heightMapLayers[index].heightMapTexture, (int)_terrainData.heightmapResolution, (int)_terrainData.heightmapResolution);
                Color[] pixels = resizedTexture.GetPixels();
                for(int x = 0; x < (int)_terrainData.heightmapResolution; x++)
                {
                    for(int z = 0; z < (int)_terrainData.heightmapResolution; z++)
                    {
                        int x1 = (heightMapLayers[index].flipX) ? (int)_terrainData.heightmapResolution - x - 1 : x;
                        int z1 = (heightMapLayers[index].flipZ) ? (int)_terrainData.heightmapResolution - z - 1 : z;
                        
                        float heightValue = pixels[x1 + z1 * (int)_terrainData.heightmapResolution].grayscale;
                        heights[x, z] = heightMapLayers[index].minHeight / _terrainData.size.y + 
                                        heightValue * 
                                        (heightMapLayers[index].maxHeight / _terrainData.size.y - heightMapLayers[index].minHeight / _terrainData.size.y);
                    }
                }
                break;
        }
        
        return heights;
    }
    
    float CalculateHeight(int x, int z, int index)
    {
        float modifiedScalePerlin = heightMapLayers[index].perlinScale * 256 / Mathf.Pow(2f, _heightmapResolution);
        float xCoord = (float)x / (int)_terrainData.size.x * modifiedScalePerlin - heightMapLayers[index].perlinOffsetX;
        float zCoord = (float)z / (int)_terrainData.size.z * modifiedScalePerlin + heightMapLayers[index].perlinOffsetY;
        
        return Mathf.PerlinNoise(xCoord, zCoord);
    }
    
    Texture2D ResizeTexture(Texture2D origTexture, int newWidth, int newHeight)
    {
        Texture2D resizedTexture = new Texture2D(newWidth, newHeight);
        Color[] resizedPixels = resizedTexture.GetPixels();
        
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                // Color newColor = origTexture.GetPixelBilinear((float)x / newWidth, (float)y / newHeight);
                // resizedTexture.SetPixel(x, y, newColor);
                float u = (float)x / (newWidth - 1);
                float v = (float)y / (newHeight - 1);
                Color newColor = origTexture.GetPixelBilinear(u, v);
                resizedPixels[x + y * newWidth] = newColor;
            }
        }
        resizedTexture.SetPixels(resizedPixels);
        resizedTexture.Apply();
        
        return resizedTexture;
    }
    
    private void ResetHeightMap()
    {
        Undo.RegisterCompleteObjectUndo(_terrainData, "Reset heightmap");
        float[,] heights = new float[(int)_terrainData.heightmapResolution, (int)_terrainData.heightmapResolution];
        for(int x = 0; x < (int)_terrainData.heightmapResolution; x++)
        {
            for(int z = 0; z < (int)_terrainData.heightmapResolution; z++)
            {
                heights[x, z] = 0;
            }
        }
        _terrainData.SetHeights(0, 0, heights);
    }
    
    void SmoothTerrain()
    {
        Undo.RegisterCompleteObjectUndo(_terrainData, "Smooth terrain");
        int terrainWidth = _terrainData.heightmapResolution;
        int terrainHeight = terrainWidth;
        float[,] heights = _terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);

        for (int x = 0; x < terrainWidth; x++)
        {
            for (int y = 0; y < terrainHeight; y++)
            {
                float avgHeight = 0;
                int count = 0;

                for (int offsetX = -5; offsetX <= 5; offsetX++)
                {
                    for (int offsetY = -5; offsetY <= 5; offsetY++)
                    {
                        int neighborX = Mathf.Clamp(x + offsetX, 0, terrainWidth - 1);
                        int neighborY = Mathf.Clamp(y + offsetY, 0, terrainHeight - 1);

                        avgHeight += heights[neighborX, neighborY];
                        count++;
                    }
                }

                avgHeight /= count;
                heights[x, y] = Mathf.Lerp(heights[x, y], avgHeight, smoothStrength/10);
            }
        }

        _terrainData.SetHeights(0, 0, heights);
    }
    #endregion
    
    #region Texturing
    void ApplyTextureLayers()
    {
        TerrainLayer[] terrainLayers = _terrainData.terrainLayers;
        if(textureLayers.Length == 0 && terrainLayers.Length == 0)
        {
            return;
        }
        if(textureLayers.Length == 0)
        {
            textureLayers = new TextureLayer[terrainLayers.Length];
            Array.ForEach(textureLayers, layer =>
            {
                layer = new TextureLayer();
            });
        }
        if(textureLayers.Length < terrainLayers.Length)
        {
            Array.Resize(ref textureLayers, terrainLayers.Length);
        }
        for(int i = 0; i < terrainLayers.Length; i++)
        {
            if(!Array.Exists(textureLayers, layer => layer.GetTextureName() == terrainLayers[i].diffuseTexture.name))
            {
                textureLayers[i] = new TextureLayer(terrainLayers[i].diffuseTexture.name);
            }
        }
    }
    
    void ApplyTexture(int index, TextureLayer layer)
    {
        Undo.RegisterCompleteObjectUndo(_terrainData.alphamapTextures, "Apply Texture");
        
        float[, ,] alphaMaps = _terrainData.GetAlphamaps(0, 0, _terrainData.alphamapWidth, _terrainData.alphamapHeight);
        //Меняем размер террейна ддя корректного наложения текстур
        Vector3 origScale = new Vector3(_terrainData.size.x, _terrainData.size.y, _terrainData.size.x);
        Vector3 modifScale = new Vector3(_terrainData.heightmapResolution, _terrainData.size.y, _terrainData.heightmapResolution);
        _terrainData.size = modifScale;
        
        for (int x = 0; x < _terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < _terrainData.alphamapHeight; y++)
            {
                int terrainX = Mathf.RoundToInt(x * _terrainData.size.x / _terrainData.alphamapResolution);
                int terrainY = Mathf.RoundToInt(y * _terrainData.size.z / _terrainData.alphamapResolution);
                float height = _terrainData.GetHeight(terrainY, terrainX);
                
                float xCoord = x / _terrainData.size.x * layer.perlinNoiseSettings.scale + layer.perlinNoiseSettings.offsetX;
                float yCoord = y / _terrainData.size.z * layer.perlinNoiseSettings.scale - layer.perlinNoiseSettings.offsetY;
                //Основной шум
                float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                //Контраст шума
                perlinValue = 0.5f - (0.5f - perlinValue) * Mathf.Pow(layer.perlinNoiseSettings.contrast, 4);
                //Яркость шума
                perlinValue = Mathf.Clamp01(perlinValue) + layer.perlinNoiseSettings.brightness;
                //Инвертировать шум
                perlinValue = layer.perlinNoiseSettings.invertNoise ? 1-perlinValue : perlinValue;

                float distanceFromBorder = 0;
                float alpha = 0;
                if(layer.isRangeTexturing)
                {
                    if(height >= layer.minHeight && height <= layer.maxHeight)
                    {
                        //Выбираем границу
                        distanceFromBorder = Mathf.Min(Mathf.Pow(layer.maxHeight - height, 1), Mathf.Pow(height - layer.minHeight, 1));
                        //Гиперболическая функция затухания
                        alpha = 1 - 1 / (1 + Mathf.Pow(distanceFromBorder / layer.fadeDistance, 2));
                    }
                    perlinValue *= alpha;
                    perlinValue = Mathf.Clamp01(perlinValue);
                }
                else
                {
                    alpha = 1;
                }
                if(layer.isPerlin)
                {
                    for (int i = 0; i < _terrainData.alphamapLayers; i++)
                    {
                        alphaMaps[x, y, i] = (i == index) ? alphaMaps[x, y, i] + perlinValue : alphaMaps[x, y, i] - perlinValue;
                    }
                }
                else
                {
                    for (int i = 0; i < _terrainData.alphamapLayers; i++)
                    {
                        alphaMaps[x, y, i] = (i == index) ? alphaMaps[x, y, i] + alpha : alphaMaps[x, y, i] - alpha;
                    }
                }
            }
        }
        _terrainData.SetAlphamaps(0, 0, alphaMaps);
        _terrainData.size = origScale;
    }
    
    void ResetTextures()
    {
        if(_terrainData.terrainLayers.Length == 0)
        {
            return;
        }
        Undo.RegisterCompleteObjectUndo(_terrainData.alphamapTextures, "Reset Texture");
        float[, ,] alphaMaps = new float[_terrainData.alphamapWidth, _terrainData.alphamapHeight, _terrainData.alphamapLayers];
        for (int x = 0; x < _terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < _terrainData.alphamapHeight; y++)
            {
                for (int currentLayer = 1; currentLayer < _terrainData.alphamapLayers; currentLayer++)
                {
                    alphaMaps[x, y, currentLayer] = 0f;
                }
                alphaMaps[x, y, 0] = 1f;
            }
        }
        _terrainData.SetAlphamaps(0, 0, alphaMaps);
    }
    #endregion
    
    #region Trees
    
    bool[] ApplyTreePrototypes()
    {
        TreePrototype[] treePrototypes = _terrainData.treePrototypes;
        bool[] newEnableTree;
        if(enableTree == null)
        {
            newEnableTree = Enumerable.Repeat(true, treePrototypes.Length).ToArray();
            return newEnableTree;
        }
        if(enableTree.Length != treePrototypes.Length)
        {
            newEnableTree = Enumerable.Repeat(true, treePrototypes.Length).ToArray();
            int arrayLength = (enableTree.Length < treePrototypes.Length) ? enableTree.Length : treePrototypes.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                newEnableTree[i] = enableTree[i];
            }
            return newEnableTree;
        }
        return enableTree;
    }
    
    void PlaceTrees()
    {
        Undo.RegisterCompleteObjectUndo(_terrainData, "Place Trees");
        int randomPrototypeIndex;
        int treeCount = 0;
        int treeIterations = 0;
        //Если нет деревьев
        if(_terrainData.treePrototypes.Length == 0)
            return;
        //Если все деревья отключены
        if(!Array.Exists(enableTree, element => element == true))
        {
            return;
        }
        while(treeCount < treeAmount)
        {
            treeIterations++;
            
            //Случайные координаты
            float x;
            float z;
            
            for(int _try = 0; _try < 100; _try++)
            {
                x = UnityEngine.Random.Range(0f, _terrainData.size.x);
                z = UnityEngine.Random.Range(0f, _terrainData.size.z);
                Vector3 position = new Vector3(x, 0f, z);
                
                // Получаем высоту террейна в данной точке
                float y = terrainObject.SampleHeight(position);
                //Попытка подбора высоты в нужном диапозоне допустимых высот
                for(int _try2 = 0; _try2 < 200; _try2++)
                {
                    if(minTreeSpawnLimit < y && y < maxTreeSpawnLimit)
                    {
                        break;
                    }
                    x = UnityEngine.Random.Range(0f, _terrainData.size.x);
                    z = UnityEngine.Random.Range(0f, _terrainData.size.z);
                    position = new Vector3(x, 0f, z);
                    y = terrainObject.SampleHeight(position);
                }
                
                //Нормаль поверхности террейна
                Vector3 normal = _terrainData.GetInterpolatedNormal(x / _terrainData.size.x, z / _terrainData.size.z);
                
                float slopeAngle = Vector3.Angle(normal, Vector3.up);
                
                float perlinValue;
                //Вероятность спавна
                float spawnProbability = UnityEngine.Random.Range(0f, 1f);
                if(enablePerlinTree)
                {
                    float xCoord = x / _terrainData.size.x * treePerlinSettings.scale / (_terrainData.heightmapResolution / _terrainData.size.x) + treePerlinSettings.offsetX;
                    float yCoord = z / _terrainData.size.z * treePerlinSettings.scale / (_terrainData.heightmapResolution / _terrainData.size.x) + treePerlinSettings.offsetY;
                    
                    perlinValue = Mathf.PerlinNoise(yCoord, xCoord);
                    perlinValue = 0.5f - (0.5f - perlinValue) * Mathf.Pow(treePerlinSettings.contrast, 4);
                    perlinValue = Mathf.Clamp01(perlinValue) + treePerlinSettings.brightness;
                    perlinValue = treePerlinSettings.invertNoise ? 1-perlinValue : perlinValue;
                }
                else
                {
                    perlinValue = 0;
                }
                //Если полученный угол меньше максимально допустимого и вероятность спавна
                if(slopeAngle < maxSlopeAngle && 1-perlinValue < spawnProbability)
                {
                    float rotationY = UnityEngine.Random.Range(0f, 360f);
                    float scale = UnityEngine.Random.Range(treeMinHeight, treeMaxHeight);
                    randomPrototypeIndex = (int)UnityEngine.Random.Range(0f, _terrainData.treePrototypes.Length);
                    //Попытка подбора случайного индекса дерева
                    for(int _try2 = 0; _try2 < 100; _try2++)
                    {
                        randomPrototypeIndex = (int)UnityEngine.Random.Range(0f, _terrainData.treePrototypes.Length);
                        //Если дерево включенно в генерацию
                        if(enableTree[randomPrototypeIndex] == true)
                            break;
                    }
                    
                    //новый TreeInstance
                    TreeInstance treeInstance = new TreeInstance
                    {
                        position = new Vector3(x / _terrainData.size.x, y / _terrainData.size.y, z / _terrainData.size.z),
                        prototypeIndex = randomPrototypeIndex,
                        widthScale = 1f*scale,
                        heightScale = 1f*scale,
                        color = Color.white,
                        lightmapColor = Color.white,
                        rotation = rotationY
                    };

                    // Добавляем дерево в список деревьев террейна
                    terrainObject.AddTreeInstance(treeInstance);
                    treeCount++;
                    break;
                }
            }
            if(treeIterations > treeAmount*2)
            {
                break;
            }
        }
        // Обновляем террейн, чтобы отобразить добавленные деревья
        terrainObject.Flush();
        Debug.Log(treeCount + " trees placed");
    }
    
    void DeleteTrees()
    {
        Undo.RegisterCompleteObjectUndo(_terrainData, "Delete all Trees");
        TreeInstance[] trees = new TreeInstance[0];
        _terrainData.SetTreeInstances(trees, false);
    }
    #endregion
}


#region HeightmapLayer class
[System.Serializable]
public class HeightMapLayer
{
    #region Variables
    public bool enabled = true;
    public bool heightMapLayerFold = true;
    public int typeIndex = 0;
    public string[] options = new string[] {"Perlin Noise", "Height Map"};
    public Texture2D heightMapTexture;
    public bool isGlobal = false;
    public bool flipZ = false;
    public bool flipX = false;
    public float minHeight = 0;
    public float maxHeight = 10;
    public int maxY = 10;
    public float perlinScale = 10f;
    public float heightOffset = 0.1f;
    public float perlinOffsetX = 0f;
    public float perlinOffsetY = 0f;
    #endregion

    public void DrawGUI()
    {
        typeIndex = EditorGUILayout.Popup(typeIndex, options);
        switch(typeIndex)
        {
            case 0:
                isGlobal = EditorGUILayout.Toggle("Global Heights", isGlobal);
                perlinScale = EditorGUILayout.DelayedFloatField("Perlin Scale", perlinScale);
                if(isGlobal)
                {
                    minHeight = (int)EditorGUILayout.DelayedFloatField("Min Height", minHeight);
                    maxHeight = (int)EditorGUILayout.DelayedFloatField("Max Height", maxHeight);
                    EditorGUILayout.MinMaxSlider(ref minHeight, ref maxHeight, 0, maxY);
                }
                else
                {
                    heightOffset = EditorGUILayout.Slider("Height Offset", heightOffset, -0.2f, 0.2f);
                }
                perlinOffsetX = EditorGUILayout.Slider("X Offset", perlinOffsetX, -20, 20);
                perlinOffsetY = EditorGUILayout.Slider("Z Offset", perlinOffsetY, -20, 20);
                break;
            case 1:
                heightMapTexture = (Texture2D)EditorGUILayout.ObjectField("HeightMap", heightMapTexture, typeof(Texture2D), false);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Texture width will be matched with heightmap resolution", MessageType.Info);
                if(!heightMapTexture)
                {
                    EditorGUILayout.HelpBox("Please, select heightmap texture", MessageType.Warning);
                }
                EditorGUILayout.EndHorizontal();
                flipZ = EditorGUILayout.Toggle("Flip Horizontal Axis", flipZ);
                flipX = EditorGUILayout.Toggle("Flip Vertical Axis", flipX);
                minHeight = (int)EditorGUILayout.DelayedFloatField("Min Height", minHeight);
                maxHeight = (int)EditorGUILayout.DelayedFloatField("Max Height", maxHeight);
                EditorGUILayout.MinMaxSlider(ref minHeight, ref maxHeight, 0, maxY);
                break;
            default:
                break;
        }
    }
    
    public string GetGenerationType()
    {
        return options[typeIndex];
    }
}
#endregion

