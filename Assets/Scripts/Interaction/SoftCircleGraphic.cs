using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class SoftCircleGraphic : MaskableGraphic
{
    [SerializeField] private Color centerColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color edgeColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField, Range(16, 96)] private int segments = 48;

    public Color CenterColor
    {
        get => centerColor;
        set
        {
            centerColor = value;
            SetVerticesDirty();
        }
    }

    public Color EdgeColor
    {
        get => edgeColor;
        set
        {
            edgeColor = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = centerColor * color;
        vertex.position = center;
        vh.AddVert(vertex);

        for (int i = 0; i <= segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vertex.color = edgeColor * color;
            vertex.position = position;
            vh.AddVert(vertex);
        }

        for (int i = 1; i <= segments; i++)
        {
            vh.AddTriangle(0, i, i + 1);
        }
    }
}