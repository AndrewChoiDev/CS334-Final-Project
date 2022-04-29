using UnityEngine;
using System.Linq;
using System.Collections.Generic;
// using Brush = BSPCSGSystem.Brush;
public class BrushHierarchy {
    public Transform top;
    public List<BrushHierarchy> nodeChildren;
    public Brush brush;


    public static List<Triangle> intersection(Brush a, Brush b) {

        var (bInsideA, bOutsideA) = a.SplitMesh(b.makeTriangleList());
        var (aInsideB, aOutsideB) = b.SplitMesh(a.makeTriangleList());
        return bInsideA.Concat(aInsideB).ToList();
    }
    public static List<Triangle> union(Brush a, Brush b) {

        var (bInsideA, bOutsideA) = a.SplitMesh(b.makeTriangleList());
        var (aInsideB, aOutsideB) = b.SplitMesh(a.makeTriangleList());
        return bOutsideA.Concat(aOutsideB).ToList();
    }    
    public static List<Triangle> difference(Brush a, Brush b) {

        var (bInsideA, bOutsideA) = a.SplitMesh(b.makeTriangleList());
        var (aInsideB, aOutsideB) = b.SplitMesh(a.makeTriangleList());

        // reverse order of vertices in each triangle
        // this filps the normals
        for (int i = 0; i < bInsideA.Count; i += 1) {
            var triCopy = bInsideA[i];
            var temp = triCopy.v1;
            triCopy.v1 = triCopy.v3;
            triCopy.v3 = temp;
            bInsideA[i] = triCopy;
        }

        return aOutsideB.Concat(bInsideA).ToList();
    }


    public BrushHierarchy(Transform top) {
        this.brush = null;
        this.top = top;
        if (top.childCount != 0) {
            nodeChildren = new List<BrushHierarchy>();
            for (int i = 0; i < this.top.childCount; i++)
            {
                nodeChildren.Add(new BrushHierarchy(this.top.GetChild(i)));
            }
        }
    }

    public void ConstructBrush() {
        if (this.nodeChildren == null) {
            this.brush = new Brush();
            var meshFilter = this.top.GetComponent<MeshFilter>();

            // explode mesh
            var rawVertices = meshFilter.sharedMesh.vertices.Select(vertex => meshFilter.transform.TransformPoint(vertex)).ToArray();
            var rawIndices = meshFilter.sharedMesh.GetIndices(0);
            var outVertices = new List<Vector3>();
            foreach (var index in rawIndices)
            {
                outVertices.Add(rawVertices[index]);
            }

            this.brush.vertices = outVertices;   

            var triList = this.brush.makeTriangleList();
            foreach (var tri in triList) {
                // NaiveCSGSystem.DebugDrawTri(tri, Color.blue);
            }
            this.brush.CreateTriBVH();
        } else {
            nodeChildren[0].ConstructBrush();
            // this.brush = nodeChildren[0].brush; // beware of this reference type
            this.brush = new Brush();
            this.brush.vertices = new List<Vector3>(nodeChildren[0].brush.vertices);
            this.brush.CreateTriBVH();
            // this.brush = nodeChildren[0].ConstructBrush
            for (int i = 1; i < nodeChildren.Count; i++)
            {
                nodeChildren[i].ConstructBrush();
                var opName = nodeChildren[i].top.tag;
                List<Triangle> tris;
                if (opName == "difference") {
                    tris = difference(this.brush, nodeChildren[i].brush);
                } else if (opName == "intersection") {
                    tris = intersection(this.brush, nodeChildren[i].brush);
                } else {
                    tris = union(this.brush, nodeChildren[i].brush);
                }
                // var tris = union(this.brush, nodeChildren[i].brush);
                this.brush = new Brush();
                this.brush.SetVerticesFromTris(tris);
                this.brush.CreateTriBVH();
            }
        }
    }
}