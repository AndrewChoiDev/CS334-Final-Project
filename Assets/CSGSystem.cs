using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class CSGSystem : MonoBehaviour
{
    [SerializeField] Transform top;
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

        brushHierarchy = new BrushHierarchy(this.top);
        brushHierarchy.ConstructBrush();

        // extract vertex, normals, and indices from brush and feed into meshfilter
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
}
