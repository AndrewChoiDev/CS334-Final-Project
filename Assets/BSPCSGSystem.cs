using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class BSPCSGSystem : MonoBehaviour
{
    [SerializeField] Transform top;
    [Range(0, 30)]
    [SerializeField] int debugValue, debugValue2;

    [SerializeField] private Transform plane;

    private BrushHierarchy brushHierarchy;

    static int debugIndex;
    static int debugIndex2;

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

            brushes.Add(brush);
        }
        return brushes;
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



    void Update()
    {
        debugIndex = this.debugValue;
        debugIndex2 = this.debugValue2;
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

    public static Vector3 EdgePlaneIntersect(Vector3 edgeA, Vector3 edgeB, Vector3 planeNorm, Vector3 planePos) {
        var planeD = -Vector3.Dot(planeNorm, planePos);
        var t = -(Vector3.Dot(planeNorm, edgeA) + planeD) / (Vector3.Dot(planeNorm, edgeB - edgeA));
        return edgeA + t * (edgeB - edgeA);
    }


    public class TriBSP {
        public TriBSP back = null;
        public TriBSP front = null;

        public Triangle tri;


        public TriBSP(Triangle tri) {
            this.tri = tri;
        }

        public void AddBack(Triangle mTri) {
            if (this.back == null) {
                this.back = new TriBSP(mTri);
            } else {
                this.back.AddTriangle(mTri);
            }
        }
        public void AddFront(Triangle mTri) {
            if (this.front == null) {
                this.front = new TriBSP(mTri);
            } else {
                this.front.AddTriangle(mTri);
            }
        }
        public void AddTriangle(Triangle mTri) {
            var thisNormal = GetPlaneNormal();
            float[] testArray = new float[3];
            testArray[0] = MyMath.PointSideTest(mTri.v1, this.tri.v1, thisNormal);
            testArray[1] = MyMath.PointSideTest(mTri.v2, this.tri.v1, thisNormal);
            testArray[2] = MyMath.PointSideTest(mTri.v3, this.tri.v1, thisNormal);

            if (testArray[0] <= 0 && testArray[1] <= 0 && testArray[2] <= 0) {
                AddBack(mTri);
            } else if (testArray[0] >= 0 && testArray[1] >= 0 && testArray[2] >= 0) {
                AddFront(mTri);
            } else {

                var fracTris = CutTriangle(mTri, this.tri.v1, thisNormal, testArray);

                if (fracTris.Item4 >= 0) {
                    AddBack(fracTris.Item1);
                    AddBack(fracTris.Item2);
                    AddFront(fracTris.Item3);
                } else {
                    AddFront(fracTris.Item1);
                    AddFront(fracTris.Item2);
                    AddBack(fracTris.Item3);
                }
            }
        }

        static int insertDepth;
        static int insertIndex;

        public static void Insert(TriBSP bsp, Triangle mTri, List<Triangle> insideModel, List<Triangle> outsideModel, List<Triangle> insertModel) {
            insertDepth += 1;
            if (debugIndex == insertDepth && insertIndex == debugIndex2) {
                var mColor = Color.green;
                if (bsp != null) {
                    Debug.DrawLine(bsp.tri.v1, bsp.tri.v2, Color.red);
                    Debug.DrawLine(bsp.tri.v2, bsp.tri.v3, Color.red);
                    Debug.DrawLine(bsp.tri.v3, bsp.tri.v1, Color.red);
                    var normal = bsp.GetPlaneNormal();
                    Debug.DrawRay((bsp.tri.v1 + bsp.tri.v2 + bsp.tri.v3) / 3.0f, normal, Color.green);
                    mColor = Color.blue;
                }
                Debug.DrawLine(mTri.v1, mTri.v2, mColor);
                Debug.DrawLine(mTri.v2, mTri.v3, mColor);
                Debug.DrawLine(mTri.v3, mTri.v1, mColor);
            }
            if (bsp == null) {
                insertModel.Add(mTri);
                insertDepth -=1;
                return;
            }
            var thisNormal = bsp.GetPlaneNormal();
            float[] testArray = new float[3];
            testArray[0] = MyMath.PointSideTest(mTri.v1, bsp.tri.v1, thisNormal);
            testArray[1] = MyMath.PointSideTest(mTri.v2, bsp.tri.v1, thisNormal);
            testArray[2] = MyMath.PointSideTest(mTri.v3, bsp.tri.v1, thisNormal);

            if (testArray[0] <= 0 && testArray[1] <= 0 && testArray[2] <= 0) {
                Insert(bsp.back, mTri, insideModel, outsideModel, insideModel);
            } else if (testArray[0] >= 0 && testArray[1] >= 0 && testArray[2] >= 0) {
                Insert(bsp.front, mTri, insideModel, outsideModel, outsideModel);
            } else {

                var fracTris = CutTriangle(mTri, bsp.tri.v1, thisNormal, testArray);

                if (debugIndex == insertDepth && insertIndex == debugIndex2) {
                Debug.DrawLine(mTri.v3, mTri.v3 + Vector3.back, Color.cyan);
                }

                if (fracTris.Item4 >= 0) {
                    Insert(bsp.back, fracTris.Item1, insideModel, outsideModel, insideModel);
                    Insert(bsp.back, fracTris.Item2, insideModel, outsideModel, insideModel);
                    Insert(bsp.front, fracTris.Item3, insideModel, outsideModel, outsideModel);
                } else {
                    Insert(bsp.front, fracTris.Item1, insideModel, outsideModel, outsideModel);
                    Insert(bsp.front, fracTris.Item2, insideModel, outsideModel, outsideModel);
                    Insert(bsp.back, fracTris.Item3, insideModel, outsideModel, insideModel);
                }
            }           

            insertDepth -= 1;
        }

        public (List<Triangle>, List<Triangle>) SplitMesh(List<Vector3> vertices) {
            var insideModel = new List<Triangle>();
            var outsideModel = new List<Triangle>();
            for (int i = 0; i < vertices.Count; i += 3) {
                var tri = new Triangle(){v1=vertices[i+0], v2=vertices[i+1], v3=vertices[i+2]};
                insertDepth = -1;
                insertIndex = i / 3;
                Insert(this, tri, insideModel, outsideModel, insideModel);
            }

            return (insideModel, outsideModel);
        }       

        // last return is test array for cPos
        public static (Triangle, Triangle, Triangle, float) CutTriangle(Triangle tri, Vector3 planePos, Vector3 planeNormal, float[] testArray) {
            var cPos = tri.v1;
            var aPos = tri.v2;
            var bPos = tri.v3;

            var cTest = testArray[0];

            if (testArray[0] * testArray[1] >= 0.0f) {
                cPos = tri.v3;
                aPos = tri.v1;
                bPos = tri.v2;
                cTest = testArray[2];

            } else if (testArray[0] * testArray[2] >= 0.0f) {
                cPos = tri.v2;
                aPos = tri.v3;
                bPos = tri.v1;
                cTest = testArray[1];
            }

            var aCap = EdgePlaneIntersect(aPos, cPos, planeNormal, planePos);
            var bCap = EdgePlaneIntersect(bPos, cPos, planeNormal, planePos);

            return (
            new Triangle(){v1=aCap, v2=aPos, v3=bPos},
            new Triangle(){v1=bCap, v2=aCap, v3=bPos},
            new Triangle(){v1=aCap, v2=bCap, v3=cPos},
            cTest
            );
        }
        
        public Vector3 GetPlaneNormal() {
            return Vector3.Cross(this.tri.v2-this.tri.v1, this.tri.v3-this.tri.v1).normalized;
        }

        public void preorderTraverse(int depth, List<Triangle> insertModel) {
            insertModel.Add(this.tri);
            if (this.back != null) {
                this.back.preorderTraverse(depth + 1, insertModel);
            }
            if (this.front != null) {
                this.front.preorderTraverse(depth + 1, insertModel);
            }
        }
    }

}
