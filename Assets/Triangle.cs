using UnityEngine;
public struct Triangle {
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;
    public Vector3 norm() {
        return Vector3.Cross(v2-v1, v3-v1).normalized;
    }
    public Vector3 centroid() {
        return (v1 + v2 + v3) / 3.0f;
    }

    public Bounds getConsBounds() {
        var consBounds = new Bounds();

        Vector3 min = v1;
        Vector3 max = v1;

        min = Vector3.Min(min, v2);
        max = Vector3.Max(max, v2);
        min = Vector3.Min(min, v3);
        max = Vector3.Max(max, v3);

        var boundsCenter = (max + min) * 0.5f;
        var boundsSize = (max - min) * 1.01f;
        consBounds = new Bounds(boundsCenter, boundsSize);
        // Debug.DrawLine(consBounds.min, consBounds.max, Color.red);
        return consBounds;
    }
}