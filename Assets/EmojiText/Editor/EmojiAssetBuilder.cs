
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    public class EmojiAssetBuilder : EditorWindow
    {
        private enum PackType
        {
            Add,
            Rebuild,
        }

        private static EmojiAsset m_EmojiConfig;

        private static readonly Vector2[] AtlasSize = new Vector2[]
        {
            new Vector2(32,32),
            new Vector2(64,64),
            new Vector2(128,128),
            new Vector2(256,256),
            new Vector2(512,512),
            new Vector2(1024,1024),
            new Vector2(2048,2048),
        };

        enum EMOJISIZE
        {
            _32X32 = 32,
            _64X64 = 64,
            _128X128 = 128,
        }

        private static int m_TarEmojiSize = 32;//the size of emoji.
        private static EMOJISIZE CurEmojiSize = EMOJISIZE._32X32;

        [MenuItem("Assets/Rich Text/Create Emoji Asset")]
        public static EmojiAsset CreateEmojiAsset()
        {
            EmojiAsset asset = CreateNewEmojiAsset();
            Selection.activeObject = asset;
            return asset;
        }

        public static EmojiAsset CreateNewEmojiAsset()
        {
            EmojiAsset asset = ScriptableObject.CreateInstance<EmojiAsset>();
            asset.EmojiSampleDic = new Dictionary<string, EmojiSampleInfo>();
            Shader s = Shader.Find("UI/RichText");
            Material m = new Material(s);
            asset.Mat = m;
            AssetDatabase.CreateAsset(m, "Assets/Resources/NewEmojiAsset.mat");
            AssetDatabase.CreateAsset(asset, "Assets/Resources/NewEmojiAsset.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        public static void SetMaterial(Material mat, Texture emojiTex, Texture dataTex, int emojiLineCount)
        {
            Shader s = Shader.Find("UI/RichText");
            mat.shader = s;
            mat.SetTexture("_EmojiTex", emojiTex);
            mat.SetTexture("_EmojiDataTex", dataTex);
            mat.SetInt("_EmojiSize", emojiLineCount);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Rich Text/EmojiAsset Eidtor")]
        public static void OpenEmojiAssetWindow()
        {
            var window = GetWindow<EmojiAssetBuilder>();
            window.titleContent = new GUIContent("EmojiAsset Eidtor");
            window.Show();
        }

        public void OnGUI()
        {
            EditorGUILayout.Space(10);
            m_EmojiConfig = EditorGUILayout.ObjectField("Asset", m_EmojiConfig, typeof(EmojiAsset), false) as EmojiAsset;
            EditorGUILayout.Space(10);
            CurEmojiSize = (EMOJISIZE)EditorGUILayout.EnumPopup("Emoji Size", CurEmojiSize, new GUILayoutOption[0]);
            m_TarEmojiSize = (int)CurEmojiSize;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                BuildEmoji(PackType.Add);
            }
            if (GUILayout.Button("Rebuild"))
            {
                BuildEmoji(PackType.Rebuild);
            }
            GUILayout.EndHorizontal();
        }

        private static void BuildEmoji(PackType type)
        {
            if (m_EmojiConfig == null)
            {
                //m_EmojiConfig = CreateNewEmojiAsset();
                EditorUtility.DisplayDialog("Error", "Please select asset obj!!", "OK");
                return;
            }

            if(type == PackType.Rebuild)
            {
                //m_EmojiConfig.EmojiSampleDic.Clear();
                m_EmojiConfig.EmojiSampleDic = new Dictionary<string, EmojiSampleInfo>();
                m_EmojiConfig.TotalFrame = 0;
            }


            int i = 0;

            Object[] selected = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
            if(selected.Length == 0)
            {
                return;
            }

            Dictionary<string, int> sourceDic = new Dictionary<string, int>();
            for (i = 0; i < selected.Length; i++)
            {
                string filePath = AssetDatabase.GetAssetPath(selected[i]);
                //string tag = filePath.Substring(filePath.LastIndexOf('_'));
                string tag = Regex.Replace(filePath, @"_\d+.png", "");
                tag = tag.Replace(".png", "");
                if (sourceDic.ContainsKey(tag))
                {
                    sourceDic[tag]++;
                }
                else
                {
                    sourceDic.Add(tag, 1);
                }
            }

            int totalFrames = m_EmojiConfig.TotalFrame;
            Dictionary<string, EmojiSampleInfo> config = new Dictionary<string, EmojiSampleInfo>(m_EmojiConfig.EmojiSampleDic);
            foreach (var item in sourceDic)
            {
                string filename = Path.GetFileName(item.Key);
                //增量时，如果原来存在同名的表情，则舍弃修改，保存原有的表情
                if (!config.ContainsKey(filename))
                {
                    EmojiSampleInfo info = new EmojiSampleInfo();
                    info.Name = filename;
                    info.SourcePath = item.Key;
                    info.FrameCount = item.Value;
                    info.UV = Vector2.zero;
                    info.EmojiSize = m_TarEmojiSize;
                    config.Add(filename, info);
                    totalFrames += item.Value;
                }
            }

            Vector2 texSize = ComputeAtlasSize(totalFrames);
            Texture2D newTex = new Texture2D((int)texSize.x, (int)texSize.y, TextureFormat.ARGB32, false);
            Texture2D dataTex = new Texture2D((int)texSize.x / m_TarEmojiSize, (int)texSize.y / m_TarEmojiSize, TextureFormat.ARGB32, false);
            int x = 0;
            int y = 0;
            foreach (var sampleData in config.Values)
            {
                for (int index = 0; index < sampleData.FrameCount; index++)
                {
                    string path = sampleData.SourcePath;
                    if (sampleData.FrameCount == 1)
                    {
                        path += ".png";
                    }
                    else
                    {
                        path += "_" + (index + 1).ToString() + ".png";
                    }

                     Texture2D asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                     if (asset.width != m_TarEmojiSize || asset.height != m_TarEmojiSize)
                     {
                         //目标尺寸小时，两种缩放效果感觉差不多
                         //asset = ScaleTexture2D2(asset, m_TarEmojiSize, m_TarEmojiSize);
                         asset = ScaleTexture2D(path, m_TarEmojiSize, m_TarEmojiSize);
                     }
                    //Texture2D asset = ScaleTexture2D(path, m_TarEmojiSize, m_TarEmojiSize);
                    Color[] colors = asset.GetPixels(0);

                    for (i = 0; i < m_TarEmojiSize; i++)
                    {
                        for (int j = 0; j < m_TarEmojiSize; j++)
                        {
                            newTex.SetPixel(x + i, y + j, colors[i + j * m_TarEmojiSize]);
                        }
                    }
                    //Object.DestroyImmediate(asset);

                    string t = System.Convert.ToString(sampleData.FrameCount - 1, 2);
                    float r = 0, g = 0, b = 0;
                    if (t.Length >= 3)
                    {
                        r = t[2] == '1' ? 0.5f : 0;
                        g = t[1] == '1' ? 0.5f : 0;
                        b = t[0] == '1' ? 0.5f : 0;
                    }
                    else if (t.Length >= 2)
                    {
                        r = t[1] == '1' ? 0.5f : 0;
                        g = t[0] == '1' ? 0.5f : 0;
                    }
                    else
                    {
                        r = t[0] == '1' ? 0.5f : 0;
                    }
                    dataTex.SetPixel(x / m_TarEmojiSize, y / m_TarEmojiSize, new Color(r, g, b, 1));

                    sampleData.UV.x = x * 1.0f / texSize.x;
                    sampleData.UV.y = y * 1.0f / texSize.y;
                    sampleData.EmojiSize = m_TarEmojiSize / texSize.x;

                    x += m_TarEmojiSize;
                    if (x >= texSize.x)
                    {
                        x = 0;
                        y += m_TarEmojiSize;
                    }
                }
            }
            m_EmojiConfig.TotalFrame = totalFrames;
            m_EmojiConfig.EmojiSampleDic = config;

            //储存图片
            string matPath = AssetDatabase.GetAssetPath(m_EmojiConfig.Mat);
            string matFolder = Path.GetDirectoryName(matPath);
            string texSavePath1;
            Texture texOld1 = m_EmojiConfig.Mat.GetTexture("_EmojiTex");
            if(texOld1)
            {
                texSavePath1 = AssetDatabase.GetAssetPath(texOld1);
            }
            else
            {
                texSavePath1 = matFolder + "/EmojiTex.png";
            }
            byte[] bytes1 = newTex.EncodeToPNG();
            File.WriteAllBytes(texSavePath1, bytes1);

            string texSavePath2;
            Texture texOld2 = m_EmojiConfig.Mat.GetTexture("_EmojiDataTex");
            if (texOld2)
            {
                texSavePath2 = AssetDatabase.GetAssetPath(texOld2);
            }
            else
            {
                texSavePath2 = matFolder + "/_EmojiDataTex.png";
            }
            byte[] bytes2 = dataTex.EncodeToPNG();
            File.WriteAllBytes(texSavePath2, bytes2);
            AssetDatabase.Refresh();
            FormatTexture(texSavePath1, texSavePath2);
            AssetDatabase.Refresh();

            texOld1 = AssetDatabase.LoadAssetAtPath(texSavePath1, typeof(Texture)) as Texture;
            texOld2 = AssetDatabase.LoadAssetAtPath(texSavePath2, typeof(Texture)) as Texture;
            SetMaterial(m_EmojiConfig.Mat, texOld1, texOld2, Mathf.RoundToInt(texSize.x / m_TarEmojiSize)); 

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", "Generate Emoji Successful!", "OK");
            AssetDatabase.Refresh();
        }

        private static Vector2 ComputeAtlasSize(int count)
        {
            long total = count * m_TarEmojiSize * m_TarEmojiSize;
            for (int i = 0; i < AtlasSize.Length; i++)
            {
                if (total <= AtlasSize[i].x * AtlasSize[i].y)
                {
                    return AtlasSize[i];
                }
            }
            return Vector2.zero;
        }

        private static void FormatTexture(string texSavePath1, string texSavePath2)
        {
            TextureImporter emojiTex = AssetImporter.GetAtPath(texSavePath1) as TextureImporter;
            emojiTex.textureType = TextureImporterType.Default;
            emojiTex.filterMode = FilterMode.Point;
            emojiTex.mipmapEnabled = false;
            emojiTex.sRGBTexture = true;
            emojiTex.alphaSource = TextureImporterAlphaSource.FromInput;
            emojiTex.textureCompression = TextureImporterCompression.Uncompressed;
            emojiTex.SaveAndReimport();

            TextureImporter emojiData = AssetImporter.GetAtPath(texSavePath2) as TextureImporter;
            emojiData.textureType = TextureImporterType.Default;
            emojiData.filterMode = FilterMode.Point;
            emojiData.mipmapEnabled = false;
            emojiData.sRGBTexture = false;
            emojiData.alphaSource = TextureImporterAlphaSource.None;
            emojiData.textureCompression = TextureImporterCompression.Uncompressed;
            emojiData.SaveAndReimport();
        }

        public static Texture2D ScaleTexture2D(string sourcePath, int newWidth, int newHeight)
        {
            TextureImporter emojiTex = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            emojiTex.isReadable = true;
            emojiTex.SaveAndReimport();

            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);

            Texture2D newTex = new Texture2D(newWidth, newWidth, source.format, false);
            for (int i = 0; i < newHeight; i++)
            {
                for (int j = 0; j < newWidth; j++)
                {
                    Color newColor = source.GetPixelBilinear(j / (float)newWidth, i / (float)newHeight);
                    try
                    {
                        newTex.SetPixel(j, i, newColor);
                    }
                    catch (System.Exception ex)
                    {
                        int a = 0;
                    }
                }
            }
            newTex.Apply();
            emojiTex.isReadable = false;
            emojiTex.SaveAndReimport();
            return newTex;
        }

        public static Texture2D ScaleTexture2D2(Texture2D source, int newWidth, int newHeight)
        {
            source.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            rt.filterMode = FilterMode.Point;
            rt.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.RGBA_DXT5_SRGB;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            var nTex = new Texture2D(newWidth, newHeight);
            nTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            nTex.Apply();
            RenderTexture.active = null;
            return nTex;
        }
    }
}