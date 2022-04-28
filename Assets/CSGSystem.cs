using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class CSGSystem : MonoBehaviour
{
    [SerializeField] Transform top;
    [SerializeField] int debugValue;


    void Start()
    {
        
    }

    public static List<Brush> GetBrushes(MeshFilter[] meshFilters) {
        var brushes = new List<Brush>();
        foreach (var meshFilter in meshFilters)
        {
            var brush = new Brush();

            brush.vertices = meshFilter.sharedMesh.vertices.Select(vertex => meshFilter.transform.TransformPoint(vertex)).ToArray();
            brush.indices = meshFilter.sharedMesh.GetIndices(0);
            brushes.Add(brush);
            Vector3 min = brush.vertices[0];
            Vector3 max = brush.vertices[0];
            System.Array.ForEach(brush.vertices, (v) => {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            });
            var boundsCenter = (max + min) * 0.5f;
            var boundsSize = (max - min) * 1.05f;
            brush.consBounds = new Bounds(boundsCenter, boundsSize);
        }
        return brushes;
    }
    public static List<BrushPair> MakeBrushIntersections(List<Brush> brushes) {
        var pairs = new List<BrushPair>();
        for (int i = 0; i < brushes.Count - 1; i++) {
            for (int j = i + 1; j < brushes.Count; j++) {
                var consIntersect = brushes[i].consBounds.Intersects(brushes[j].consBounds);
                if (consIntersect) {
                    if (brushes[i].ConvexIntersectsTest(brushes[j])) {
                        pairs.Add(new BrushPair(){a = brushes[i], b = brushes[j]});
                    }
                }
            }
        }
        return pairs;
    }

    public static void FindInnerVertices(List<BrushPair> intersects) {
        foreach (var pair in intersects)
        {
            pair.brushAVertsInBrushB = new List<Vector3>();
            foreach (var vert in pair.a.vertices)
            {
                if (pair.b.PointInsideConvexTest(vert)) {
                    pair.brushAVertsInBrushB.Add(vert);
                }
            }
            pair.brushBVertsInBrushA = new List<Vector3>();
            foreach (var vert in pair.b.vertices)
            {
                if (pair.a.PointInsideConvexTest(vert)) {
                    pair.brushBVertsInBrushA.Add(vert);
                }
            }
        }
    }

    public static Mesh MakeAggregateMesh(List<Brush> brushes) {
        var aggregateVertices = new List<Vector3>();
        var aggregateIndices = new List<int>();
        foreach (var brush in brushes)
        {
            var initVertCount = aggregateVertices.Count;
            foreach (var vertex in brush.vertices)
            {
                aggregateVertices.Add(vertex);
            }
            foreach (var index in brush.indices)
            {
                aggregateIndices.Add(index + initVertCount);
            }
        }

        var aggregateMesh = new Mesh();
        aggregateMesh.SetVertices(aggregateVertices);
        aggregateMesh.SetIndices(aggregateIndices.ToArray(), MeshTopology.Triangles, 0);
        return aggregateMesh;
    }

    // Update is called once per frame
    void Update()
    {
        CSGSystem.visualDebug = this.debugValue;
        var meshFilters = top.GetComponentsInChildren<MeshFilter>();
        var brushes = GetBrushes(meshFilters);
        foreach (var brush in brushes)
        {
            brush.VisualizeEdges();
            // brush.VisualizeNormals();
        }
        var intersects = MakeBrushIntersections(brushes);
        FindInnerVertices(intersects);
        foreach (var intersect in intersects)
        {

        }

        GetComponent<MeshFilter>().sharedMesh = MakeAggregateMesh(brushes);
    }

    public static void FindIntersectEdges(List<BrushPair> intersects) {
        foreach (var intersect in intersects)
        {
            var edgesA = intersect.a.GetMeshEdges();
            foreach (var edge in edgesA)
            {
                
            }
            var edgesB = intersect.b.GetMeshEdges();

        }
    }



    public class BrushPair {
        public Brush a;
        public Brush b;

        public List<Vector3> brushAVertsInBrushB;
        public List<Vector3> brushBVertsInBrushA;

        public HashSet<IntersectEdge> intersectBrushAEdges;
        public HashSet<IntersectEdge> intersectBrushBEdges;
    }

    public class IntersectEdge {
        Vector3 start;
        Vector3 end;
        int triIndex;
    }



    public class Brush {
        public Vector3[] vertices;
        public int[] indices;
        public Bounds consBounds;
        public Vector3 GetPlaneNormal(int startIndex) {
            Debug.Assert(startIndex % 3 == 0);
            var a = vertices[this.indices[startIndex]];
            var b = vertices[this.indices[startIndex + 1]];
            var c = vertices[this.indices[startIndex + 2]];
            return Vector3.Cross(b-a, c-a).normalized;
        }

        public void VisualizeNormals() {
            for (int i = 0; i < this.indices.Length; i += 3)
            {
                var center = (vertices[this.indices[i]] + vertices[this.indices[i + 1]] + vertices[this.indices[i + 2]]) / 3.0f;
                var normal = GetPlaneNormal(i);
                Debug.DrawLine(center, center + normal, Color.red);
            }
        }
        public bool PointInsideConvexTest(Vector3 point) {
            for (int i = 0; i < this.indices.Length; i += 3) 
            {
                var normal = GetPlaneNormal(i);
                var thisVert = vertices[this.indices[i]];
                if (Vector3.Dot(point - thisVert, normal) > 0) {
                    return false;
                }
            }
            return true;
        }

        public bool ConvexIntersectsTest(Brush other) {
            for (int j = 0; j < other.vertices.Length; j++) {
                var otherVert = other.vertices[j];
                if (PointInsideConvexTest(otherVert)){ 
                    return true;
                }
            }
            return false;
        }
        public HashSet<(int, int)> GetMeshEdges() {
            var edgeSet = new HashSet<(int, int)>();
            var triEdgeCombos = new (int, int)[]{(0, 1), (0, 2), (1, 2)};
            for (int i = 0; i < this.indices.Length; i += 3)
            {

                for (int j = 0; j < 3; j++) {
                    var edgeIndexA = this.indices[i + triEdgeCombos[j].Item1];
                    var edgeIndexB = this.indices[i + triEdgeCombos[j].Item2];
                    if (edgeIndexA > edgeIndexB) {
                        var a = edgeIndexB;
                        edgeIndexB = edgeIndexA;
                        edgeIndexA = a;
                    }

                    edgeSet.Add((edgeIndexA, edgeIndexB));
                }
            }

            return edgeSet;
        }

        public void OutputVerts() {
            int i = 0;
            if (this.vertices.Length > 30) {
                return;
            }

            foreach (var vert in this.vertices)
            {
                Debug.Log(i + ", " + vert);
                i++;
            }
        }

        public void VisualizeEdges() {
            var set = GetMeshEdges();
            foreach (var item in set)
            {
                Debug.DrawLine(this.vertices[item.Item1], this.vertices[item.Item2], Color.blue);
            }
        }

        // -1 if nothing found
        // otherwise returns tri index
        public void IntersectEdge(Vector3 a, Vector3 b) {
            for (int i = 0; i < this.indices.Length; i += 3) 
            {
                var normal = GetPlaneNormal(i);
                var thisVert = vertices[this.indices[i]];

                // if (Vector3.Dot(point - thisVert, normal) > 0) {
                    // return false;
                // }
            }
        }
    }

    static int visualDebug = 0;
}

