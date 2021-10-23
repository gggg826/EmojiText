using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    [Serializable]
    public class EmojiSampleInfo
    {
        public string Name;
        public string SourcePath;
        public int FrameCount;
        public Vector2 UV;
        public float EmojiSize;
    }

    //[CreateAssetMenu]
    public class EmojiAsset : ScriptableObject
    {
        public Material Mat;
        public List<string> Keys = new List<string>();
        public List<EmojiSampleInfo> Values = new List<EmojiSampleInfo>();
        public int TotalFrame;

        protected Dictionary<string, EmojiSampleInfo> m_EmojiSampleDic;
        public Dictionary<string, EmojiSampleInfo> EmojiSampleDic
        {
            get
            {
                if(m_EmojiSampleDic == null)
                {
                    m_EmojiSampleDic = new Dictionary<string, EmojiSampleInfo>();
                    for (int i = 0; i < Keys.Count; i++)
                    {
                        m_EmojiSampleDic.Add(Keys[i], Values[i]);
                    }
                }
                return m_EmojiSampleDic;
            }
            set
            {
                m_EmojiSampleDic = value;
                Keys = new List<string>(m_EmojiSampleDic.Keys);
                Values = new List<EmojiSampleInfo>(m_EmojiSampleDic.Values);
            }
        }
    }
}
