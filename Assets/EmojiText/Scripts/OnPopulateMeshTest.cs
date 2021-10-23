using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnPopulateMeshTest : Graphic
{
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear();
        List<UIVertex> verts = new List<UIVertex>();

        UIVertex vert0 = new UIVertex();
        vert0.position = new Vector3(0, 0);
        vert0.color = Color.blue;
        vert0.uv0 = Vector2.zero;
        verts.Add(vert0);

        UIVertex vert1 = new UIVertex();
        vert1.position = new Vector3(0, 100);
        vert1.color = Color.blue;
        vert1.uv0 = Vector2.zero;
        verts.Add(vert1);

        UIVertex vert2 = new UIVertex();
        vert2.position = new Vector3(100, 100);
        vert2.color = Color.blue;
        vert2.uv0 = Vector2.zero;
        verts.Add(vert2);

        UIVertex vert3 = new UIVertex();
        vert3.position = new Vector3(100, 0);
        vert3.color = Color.blue;
        vert3.uv0 = Vector2.zero;
        verts.Add(vert3);

        List<int> indices = new List<int>() { 0, 1, 2, 2, 3, 0 };
        toFill.AddUIVertexStream(verts, indices);
    }
}
