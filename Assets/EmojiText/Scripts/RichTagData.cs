using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum RichTagType
{
    FONTSYMBOL, //字体
    EMOJI = 1,  //表情和图片
    HYPERLINK = 2,  //超链接
}


public class RichTagData
{
    public RichTagType Type;
    public string PopulateText;//填充文本
    public int StartIndex;//开始的字符Index
    public int EndPoIndex;//结束的字符Index
    public int SymbolLenght;
    public int IndexOffset;//用于计算后一个Tag的起始位置偏移值
    public string[] strParam;

    public RichTagData(string param, int size)
    {
        if(string.IsNullOrEmpty(param))
        {
            return;
        }
        string[] splitArray = param.Split(',');
        if (splitArray == null)
        {
            return;
        }

        strParam = splitArray;
        Type = (RichTagType)int.Parse(strParam[0]);
        switch (Type)
        {
            case RichTagType.EMOJI:
                if(splitArray.Length < 2)
                {
                    return;
                }
                //strParam = splitArray[1];//表情、Icon的名字
                PopulateText = string.Format("<quad Size={0} Width={1}>", size.ToString(), 1);
                SymbolLenght = 1;
                break;
            case RichTagType.HYPERLINK:
                if (splitArray.Length < 3)
                {
                    return;
                }
                //strParam = splitArray[1];//超链接的文本
                PopulateText = string.Format("<color={0}>{1}</color>", splitArray[2], splitArray[1]);
                SymbolLenght = strParam[1].Length;
                break;
        }
        IndexOffset = SymbolLenght - (param.Length + 4);// <t=param> 长度
    }

    public void SetStartIndex(int index)
    {
        StartIndex = index;
        EndPoIndex = StartIndex + SymbolLenght - 1;
    }

    public bool UseQuad()
    {
        return Type != RichTagType.HYPERLINK;
    }
}
