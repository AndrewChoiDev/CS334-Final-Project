using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvexMeshGenerator : MonoBehaviour
{
    public enum ConvexMeshType {Cube, Cylinder, Pyramid}

    public ConvexMeshType meshType;

    [ContextMenu("Set Mesh")]
    public void SetMesh() {
        Vector3[] vertices = {};
        int[] indices = {};
        if (meshType == ConvexMeshType.Cube) {
            var min = Vector3.one * -0.5f;
            var max = -min;
            var disp = max - min;
            vertices = new Vector3[]{
                new Vector3 (0, 0, 0),
                new Vector3 (1, 0, 0),
                new Vector3 (1, 1, 0),
                new Vector3 (0, 1, 0),
                new Vector3 (0, 1, 1),
                new Vector3 (1, 1, 1),
                new Vector3 (1, 0, 1),
                new Vector3 (0, 0, 1)
            };
            for (int i = 0; i < vertices.Length; i++) {
                vertices[i] -= Vector3.one * 0.5f;
            }
            indices = new int[]{
                0, 2, 1, //face front
                0, 3, 2,
                2, 3, 4, //face top
                2, 4, 5,
                1, 2, 5, //face right
                1, 5, 6,
                0, 7, 4, //face left
                0, 4, 3,
                5, 4, 7, //face back
                5, 7, 6,
                0, 6, 7, //face bottom
                0, 1, 6
            };
        }

        GetComponent<MeshFilter>().sharedMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        GetComponent<MeshFilter>().sharedMesh.SetVertices(vertices);

    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
