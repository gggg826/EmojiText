using UnityEngine.Events;
using UnityEngine.UI;

public class HyperLinkGraphic : UnityEngine.UI.Graphic
{
    protected Button m_Button;

    protected override void Awake()
    {
        base.Awake();
        m_Button = gameObject.GetComponent<Button>();
        if(m_Button == null)
        {
            m_Button = gameObject.AddComponent<Button>();
        }
    }

    public void SetClickEvent(UnityAction callback)
    {
        if (!m_Button)
        {
            return;
        }
        m_Button.onClick.RemoveAllListeners();
        m_Button.onClick.AddListener(callback);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
    }
}