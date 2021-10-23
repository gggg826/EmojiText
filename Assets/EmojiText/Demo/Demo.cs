using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    void Start()
    {
        RichText cmp = GetComponent<RichText>();
        cmp.text = @"<color=#008957>支持表情：</color> <t=1,yuancheng>, <t=1,kiss> <t=1,bubinIcon> 和 <color=#00fff0>超链接：</color> <t=2,网址是:www.baidu.com,#003322> <t=2,www.souhu.comwww.souhu.comwww.souhu.com,#001155>";
    }
}
