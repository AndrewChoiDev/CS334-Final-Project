using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class NaiveCSGSystem : MonoBehaviour
{
    [SerializeField] Transform top;
    [SerializeField] Transform farPoint;
    [Range(0, 100)]
    [SerializeField] private int debugValue;
    [SerializeField] private bool dynamic;

    public static int debugIndex;

    private BrushHierarchy brushHierarchy;

    public static void DebugDrawTri(Triangle inTri, Color color) {
        Debug.DrawLine(inTri.v1, inTri.v2, color);
        Debug.DrawLine(inTri.v2, inTri.v3, color);
        Debug.DrawLine(inTri.v3, inTri.v1, color);
    }
    // Update is called once per frame

    public static bool notableFlag = true;

    void Update()
    {
        if (dynamic == false) {
            return;
        }

        Construct();

    }
    [ContextMenu("construct")]
    public void Construct() {
        debugIndex = this.debugValue;

        var meshFilters = top.GetComponentsInChildren<MeshFilter>();
        // var brushes = GetBrushes(meshFilters);
        // foreach (var brush in brushes)
        // {   
            // brush.CreateBSP();
        // }
        brushHierarchy = new BrushHierarchy(this.top);

        brushHierarchy.ConstructBrush();
        var vertexList = brushHierarchy.brush.vertices;
        var normList = new List<Vector3>();
        for (int i = 0; i < vertexList.Count; i += 3)
        {
            var norm = Vector3.Cross(vertexList[i+1]-vertexList[i], vertexList[i+2]-vertexList[i]).normalized;
            normList.Add(norm);
            normList.Add(norm);
            normList.Add(norm);           
        }
        var compMesh = new Mesh();
        compMesh.SetVertices(vertexList);
        compMesh.SetNormals(normList);
        var indices = new List<int>();
        for (int i = 0; i < vertexList.Count; i++)
        {
            indices.Add(i);
        }
        compMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        GetComponent<MeshFilter>().sharedMesh = compMesh;
    }

    public static Mesh MakeAggregateMesh(List<Brush> brushes) {
        var aggregateVertices = new List<Vector3>();
        var aggregateIndices = new List<int>();
        foreach (var brush in brushes)
        {
            foreach (var vertex in brush.vertices)
            {
                aggregateVertices.Add(vertex);
                aggregateIndices.Add(aggregateIndices.Count);
            }
        }

        var aggregateMesh = new Mesh();
        aggregateMesh.SetVertices(aggregateVertices);
        aggregateMesh.SetIndices(aggregateIndices.ToArray(), MeshTopology.Triangles, 0);
        return aggregateMesh;
    }

    



    public static List<Brush> GetBrushes(MeshFilter[] meshFilters) {
        var brushes = new List<Brush>();
        foreach (var meshFilter in meshFilters)
        {
            var brush = new Brush();

            // explode mesh
            var rawVertices = meshFilter.sharedMesh.vertices.Select(vertex => meshFilter.transform.TransformPoint(vertex)).ToArray();
            var rawIndices = meshFilter.sharedMesh.GetIndices(0);
            var outVertices = new List<Vector3>();
            foreach (var index in rawIndices)
            {
                outVertices.Add(rawVertices[index]);
            }

            brush.vertices = outVertices;

            Vector3 min = brush.vertices[0];
            Vector3 max = brush.vertices[0];
            System.Array.ForEach(brush.vertices.ToArray(), (v) => {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            });
            var boundsCenter = (max + min) * 0.5f;
            var boundsSize = (max - min) * 1.05f;
            brush.consBounds = new Bounds(boundsCenter, boundsSize);


            brushes.Add(brush);
        }
        return brushes;
    }
}
