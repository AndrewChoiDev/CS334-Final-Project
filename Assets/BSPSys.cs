using UnityEngine;
using System.Collections.Generic;
// using Triangle = NaiveCSGSystem.Triangle;
public class BSPSys {
    public BSPSys back = null;
    public BSPSys front = null;


    public Triangle tri;

    public BSPSys(Triangle tri) {
        this.tri = tri;
    }

    public void AddBack(Triangle mTri) {
        if (this.back == null) {
            this.back = new BSPSys(mTri);
        } else {
            this.back.AddTriangle(mTri);
        }
    }
    public void AddFront(Triangle mTri) {
        if (this.front == null) {
            this.front = new BSPSys(mTri);
        } else {
            this.front.AddTriangle(mTri);
        }
    }
    public Vector3 GetPlaneNormal() {
        return Vector3.Cross(this.tri.v2-this.tri.v1, this.tri.v3-this.tri.v1).normalized;
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

            var fracTris = MyMath.CutTriangle(mTri, this.tri.v1, thisNormal, testArray);

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

    public static float PointSide(BSPSys bsp, Vector3 point) {
        var thisNormal = bsp.GetPlaneNormal();
        var test = MyMath.PointSideTest(point, bsp.tri.v1, thisNormal);

        if (test < 0) {
            if (bsp.back == null) {
                return -1;
            } else {
                return PointSide(bsp.back, point);
            }
        } else if (test > 0) {
            if (bsp.front == null) {
                Debug.DrawRay(point, Vector3.one * 0.1f, Color.magenta);
                return 1;
            } else {
                return PointSide(bsp.front, point);
            }
        }

        return 0;

    }
    public (List<Triangle>, List<Triangle>) SplitMesh(List<Triangle> triangles) {
        var insideModel = new List<Triangle>();
        var outsideModel = new List<Triangle>();
        foreach (var tri in triangles)
        {
            // NaiveCSGSystem.DebugDrawTri(tri, Color.red);
            var p1 = PointSide(this, tri.v1);
            var p2 = PointSide(this, tri.v2);
            var p3 = PointSide(this, tri.v3);
            if (p1 == 1 || p2 == 1 || p3 == 1) {
            NaiveCSGSystem.DebugDrawTri(tri, Color.red);
                insideModel.Add(tri);
            }
                // outsideModel.Add(tri);
                // NaiveCSGSystem.DebugDrawTri(tri, Color.red);
                // insideModel.Add(tri);

            
        }

        return (insideModel, outsideModel);
    }
}