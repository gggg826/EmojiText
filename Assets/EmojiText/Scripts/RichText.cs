using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/RichText", 11)]
    public class RichText : Text
    {
        //private const float ICON_SCALE_OF_DOUBLE_SYMBOLE = 0.7f;
        //public override float preferredWidth => cachedTextGeneratorForLayout.GetPreferredWidth(emojiText, GetGenerationSettings(rectTransform.rect.size)) / pixelsPerUnit;
        //public override float preferredHeight => cachedTextGeneratorForLayout.GetPreferredHeight(emojiText, GetGenerationSettings(rectTransform.rect.size)) / pixelsPerUnit;

        //private string richText => Regex.Replace(text, "\\[[a-z0-9A-Z]+\\]", "%%");
        private string richText => Parse(text);
        private static Dictionary<string, EmojiSampleInfo> m_EmojiIndexDict = null;
        private static Vector2 m_UnderLineUV = Vector2.one * 1.5f;

        class HyperLinkInfo
        {
            public int HyperLinkIndex;//标记数据属于哪个超链接，一段文本中可能存在多组超链接
            public Vector3 MinPos;
            public Vector3 MaxPos;
            public Action ClickEvent;
            public Color RenderCorlor = Color.blue;
        }

        [SerializeField]
        private EmojiAsset m_EmojiData;

        readonly UIVertex[] m_TempVerts = new UIVertex[4];
        private Dictionary<int, RichTagData> m_RichTags;
        private List<Action<string>> m_HyperCallBackList = null;
        private List<HyperLinkInfo> m_HyperLinkList = null;
        private int m_HyperLinkIndex = 0;

        /// <summary>
        /// 缓存表情采样图集信息
        /// </summary>
        protected void CacheEmojiSampleConfig()
        {
            if(m_EmojiData != null)
            {
                m_EmojiIndexDict = m_EmojiData.EmojiSampleDic;
            }
        }

        public override string text
        {
            get => base.text;
            set
            {
                base.text = value;
                ReleaseHyperLinkData();
            }
        }

        protected void ReleaseHyperLinkData()
        {
            if (supportRichText)
            {
                if (m_HyperCallBackList == null)
                {
                    m_HyperCallBackList = new List<Action<string>>();
                }
                m_HyperCallBackList.Clear();
                if (m_HyperLinkList == null)
                {
                    m_HyperLinkList = new List<HyperLinkInfo>();
                }
                m_HyperLinkList.Clear();
                m_HyperLinkIndex = 0;
            }
        }


        protected string Parse(string strText)
        {
            string result = strText;
            if (supportRichText)
            {
                if (m_RichTags == null)
                {
                    m_RichTags = new Dictionary<int, RichTagData>();
                }
                else
                {
                    m_RichTags.Clear();
                }

                int nOffset = 0;
                string str = ReplaceRichText(text);
                MatchCollection matches = Regex.Matches(str, @"<t=([^>]+)?>");
                for (int i = 0; i < matches.Count; i++)
                {
                    RichTagData info = new RichTagData(matches[i].Groups[1].Value, fontSize);
                    info.SetStartIndex(matches[i].Index + nOffset);
                    m_RichTags.Add(info.StartIndex, info);
                    nOffset += info.IndexOffset;
                    result = result.Replace(matches[i].Value, info.PopulateText);
                }
            }
            return result;
        }

        //去掉富文本标记
        private string ReplaceRichText(string StrText)
        {
            string str = Regex.Replace(StrText, @"<color=(.+?)>", "");
            str = str.Replace("</color>", "");
            str = str.Replace("<b>", "");
            str = str.Replace("</b>", "");
            str = str.Replace("<i>", "");
            str = str.Replace("</i>", "");
            str = str.Replace("\n", "");
            str = str.Replace("\t", "");
            str = str.Replace("\r", "");
            str = str.Replace(" ", "");
            return str;
        }


        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (font == null)
            {
                return;
            }
            if (m_EmojiData != null && m_Material == null)
            {
                m_Material = m_EmojiData.Mat;
            }
            if (m_EmojiIndexDict == null)
            {
                CacheEmojiSampleConfig();
            }

            m_DisableFontTextureRebuiltCallback = true;

            Vector2 extents = rectTransform.rect.size;
            var settings = GetGenerationSettings(extents);
            cachedTextGenerator.Populate(richText, settings);

            IList<UIVertex> verts = cachedTextGenerator.verts;
            float unitsPerPixel = 1 / pixelsPerUnit;
            int vertCount = verts.Count;

            if (vertCount <= 0)
            {
                toFill.Clear();
                return;
            }

            Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
            roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
            toFill.Clear();
            if (roundingOffset != Vector2.zero)
            {
                for (int i = 0; i < vertCount; ++i)
                {
                    int tempVertsIndex = i & 3;
                    m_TempVerts[tempVertsIndex] = verts[i];
                    m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
                    m_TempVerts[tempVertsIndex].position.x += roundingOffset.x;
                    m_TempVerts[tempVertsIndex].position.y += roundingOffset.y;
                    if (tempVertsIndex == 3)
                    {
                        toFill.AddUIVertexQuad(m_TempVerts);
                    }
                }
            }
            else
            {
                RichTagData tagData = null;
                for (int i = 0; i < vertCount; ++i)
                {
                    if (i + 3 > vertCount)
                    {
                        break;
                    }

                    int index = i / 4;
                    RichTagType tagType = RichTagType.FONTSYMBOL;
                    if (tagData != null && tagData.Type == RichTagType.HYPERLINK && index <= tagData.EndPoIndex)
                    {
                        tagType = RichTagType.HYPERLINK;
                    }
                    else if (m_RichTags.TryGetValue(index, out tagData))
                    {
                        tagType = tagData.Type;
                    }

                    m_TempVerts[0] = verts[i];
                    m_TempVerts[1] = verts[i + 1];
                    m_TempVerts[2] = verts[i + 2];
                    m_TempVerts[3] = verts[i + 3];

                    switch (tagType)
                    {
                        case RichTagType.FONTSYMBOL:
                            break;
                        case RichTagType.EMOJI:
                            ProcessEmoji(tagData);
                            break;
                        case RichTagType.HYPERLINK:
                            ProcessHyperLink(index, ref tagData, ref toFill);
                            break;
                        default:
                            break;
                    }

                    m_TempVerts[0].position *= unitsPerPixel;
                    m_TempVerts[1].position *= unitsPerPixel;
                    m_TempVerts[2].position *= unitsPerPixel;
                    m_TempVerts[3].position *= unitsPerPixel;
                    toFill.AddUIVertexQuad(m_TempVerts);

                    i += 3;
                }
            }
            if (m_HyperLinkList != null && m_HyperLinkList.Count > 0)
            {
                ProcessUnderLine(toFill);
            }
            m_DisableFontTextureRebuiltCallback = false;
        }

        private void ProcessEmoji(RichTagData tagData)
        {
            if(tagData.strParam == null || tagData.strParam.Length < 2 || string.IsNullOrEmpty(tagData.strParam[1]))
            {
                return;
            }
            float heighOffset = fontSize * 0.1f;
            m_TempVerts[0].position += new Vector3(0, -heighOffset, 0);
            m_TempVerts[1].position += new Vector3(0, -heighOffset, 0);
            m_TempVerts[2].position += new Vector3(0, -heighOffset, 0);
            m_TempVerts[3].position += new Vector3(0, -heighOffset, 0);

            EmojiSampleInfo emoji;
            if (m_EmojiIndexDict.TryGetValue(tagData.strParam[1], out emoji))
            {
                m_TempVerts[3].uv1 = new Vector2(emoji.UV.x, emoji.UV.y);
                m_TempVerts[2].uv1 = new Vector2(emoji.UV.x + emoji.EmojiSize, emoji.UV.y);
                m_TempVerts[1].uv1 = new Vector2(emoji.UV.x + emoji.EmojiSize, emoji.UV.y + emoji.EmojiSize);
                m_TempVerts[0].uv1 = new Vector2(emoji.UV.x, emoji.UV.y + emoji.EmojiSize);
            }
            else
            {
                m_TempVerts[3].uv1 = Vector2.one;
                m_TempVerts[2].uv1 = Vector2.one;
                m_TempVerts[1].uv1 = Vector2.one;
                m_TempVerts[0].uv1 = Vector2.one;
            }
        }

        private void ProcessHyperLink(int index, ref RichTagData tagData, ref VertexHelper toFill)
        {
            if (tagData.strParam == null || tagData.strParam.Length < 3 || string.IsNullOrEmpty(tagData.strParam[2]))
            {
                return;
            }

            if (m_HyperLinkList == null)
            {
                m_HyperLinkList = new List<HyperLinkInfo>();
            }

            //确定点击区域，每两个点确定一个区域。
            //可能会有换行，所以可能会有多个点击区域
            //换行只适用于从左往右的书写顺序
            if (m_HyperLinkList.Count == 0
                || (m_HyperLinkList[m_HyperLinkList.Count - 1].MaxPos.x - m_TempVerts[2].position.x) > 0
                || (m_HyperLinkList[m_HyperLinkList.Count - 1].HyperLinkIndex != m_HyperLinkIndex))
            {
                HyperLinkInfo h = new HyperLinkInfo();

                Color c;
                ColorUtility.TryParseHtmlString($"{tagData.strParam[2]}", out c);
                h.RenderCorlor = c;
                h.MinPos = m_TempVerts[3].position;
                h.MaxPos = m_TempVerts[1].position;
                h.HyperLinkIndex = m_HyperLinkIndex;
                string linkText = tagData.strParam[1];
                h.ClickEvent = () =>
                {
                    Debug.Log($"<color=#00ff00>[HyperLink Clicked]</color> : {linkText}");
                    if (m_HyperCallBackList != null && m_HyperCallBackList[m_HyperLinkIndex] != null)
                    {
                        m_HyperCallBackList[m_HyperLinkIndex](linkText);
                    }
                };
                m_HyperLinkList.Add(h);
            }
            else
            {
                HyperLinkInfo h = m_HyperLinkList[m_HyperLinkList.Count - 1];
                h.MinPos.x = Mathf.Min(h.MinPos.x, m_TempVerts[3].position.x);
                h.MinPos.y = Mathf.Min(h.MinPos.y, m_TempVerts[3].position.y);
                h.MaxPos.x = Mathf.Max(h.MaxPos.x, m_TempVerts[1].position.x);
                h.MaxPos.y = Mathf.Max(h.MaxPos.y, m_TempVerts[1].position.y);

            }

            //处理至超链接的末尾时，标记++
            if (index == tagData.EndPoIndex)
            {
                m_HyperLinkIndex++;
                tagData = null;
            }
        }

        private void ProcessUnderLine(VertexHelper toFill)
        {
            for (int i = 0; i < m_HyperLinkList.Count; i++)
            {
                HyperLinkInfo info = m_HyperLinkList[i];

                float x1 = info.MinPos.x;
                float x2 = info.MaxPos.x;
                float y1 = info.MinPos.y;
                float y2 = (y1 - fontSize * 0.02f);

                m_TempVerts[0].position = new Vector3(x1, y1);
                m_TempVerts[0].color = info.RenderCorlor;
                m_TempVerts[0].uv0 = m_UnderLineUV;

                m_TempVerts[1].position = new Vector3(x2, y1);
                m_TempVerts[1].color = info.RenderCorlor;
                m_TempVerts[1].uv0 = m_UnderLineUV;

                m_TempVerts[2].position = new Vector3(x2, y2);
                m_TempVerts[2].color = info.RenderCorlor;
                m_TempVerts[2].uv0 = m_UnderLineUV;

                m_TempVerts[3].position = new Vector3(x1, y2);
                m_TempVerts[3].color = info.RenderCorlor;
                m_TempVerts[3].uv0 = m_UnderLineUV;


                m_TempVerts[0].position /= pixelsPerUnit;
                m_TempVerts[1].position /= pixelsPerUnit;
                m_TempVerts[2].position /= pixelsPerUnit;
                m_TempVerts[3].position /= pixelsPerUnit;

                toFill.AddUIVertexQuad(m_TempVerts);
            }
        }

        protected void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (!supportRichText)
            {
                return;
            }
            if (m_HyperLinkList == null || m_HyperLinkList.Count == 0)
            {
                return;
            }

            int c = transform.childCount;
            for (int i = 0; i < c; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            transform.DetachChildren();

            for (int i = m_HyperLinkList.Count - 1; i >= 0; i--)
            {
                HyperLinkInfo info = m_HyperLinkList[i];
                if (info == null)
                {
                    continue;
                }
                float w = info.MaxPos.x - info.MinPos.x;
                float h = info.MaxPos.y - info.MinPos.y;
                Vector2 center = new Vector2((info.MaxPos.x + info.MinPos.x) * 0.5f,
                    (info.MaxPos.y + info.MinPos.y) * 0.5f);

                GameObject go = new GameObject("hyperLink" + i);
                go.transform.SetParent(transform);
                RectTransform rectTrans = go.AddComponent<RectTransform>();
                rectTrans.localScale = Vector3.one;
                rectTrans.anchoredPosition3D = center / pixelsPerUnit;
                rectTrans.sizeDelta = new Vector2(w / pixelsPerUnit, h / pixelsPerUnit);

                HyperLinkGraphic graphic = go.AddComponent<HyperLinkGraphic>();
                graphic.SetClickEvent(() =>
                {
                    info.ClickEvent();
                });
                m_HyperLinkList.RemoveAt(i);
            }
        }
    }
}
