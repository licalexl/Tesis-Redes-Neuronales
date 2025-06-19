using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic
{
    public Vector2 startPoint;
    public Vector2 endPoint;
    public float thickness = 2f;

    public void SetPositions(Vector2 start, Vector2 end)
    {
        startPoint = start;
        endPoint = end;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Vector2 direction = (endPoint - startPoint).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;

        Vector2 v1 = startPoint + normal;
        Vector2 v2 = startPoint - normal;
        Vector2 v3 = endPoint - normal;
        Vector2 v4 = endPoint + normal;

        vh.AddVert(v1, color, Vector2.zero);
        vh.AddVert(v2, color, Vector2.zero);
        vh.AddVert(v3, color, Vector2.zero);
        vh.AddVert(v4, color, Vector2.zero);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
