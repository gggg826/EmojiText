using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(RichText), true)]
    [CanEditMultipleObjects]
    public class RichTextEditor : TextEditor
    {
        SerializedProperty m_EmojiData;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EmojiData = serializedObject.FindProperty("m_EmojiData");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.ObjectField(m_EmojiData);
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}
