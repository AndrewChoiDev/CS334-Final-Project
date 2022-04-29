using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;
public static class MyMath {

    public static Vector3 EdgePlaneIntersect(Vector3 edgeA, Vector3 edgeB, Vector3 planeNorm, Vector3 planePos) {
        var planeD = -Vector3.Dot(planeNorm, planePos);
        var t = -(Vector3.Dot(planeNorm, edgeA) + planeD) / (Vector3.Dot(planeNorm, edgeB - edgeA));
        return edgeA + t * (edgeB - edgeA);
    }

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
    public static float zeroToOne(float val) {
        if (val == 0.0f) return 1.0f;
        return val;
    }


    public static bool SemiTriangleIntersectionTest(Triangle a, Triangle b) {
        var testArray = new float[3];
        var bNorm = b.norm();
        testArray[0] = MyMath.PointSideTest(a.v1, b.v1, bNorm);
        testArray[1] = MyMath.PointSideTest(a.v2, b.v1, bNorm);
        testArray[2] = MyMath.PointSideTest(a.v3, b.v1, bNorm);
        if (zeroToOne(testArray[0]) == zeroToOne(testArray[1]) 
            && zeroToOne(testArray[1]) == zeroToOne(testArray[2])) {
            return false;
        }
        if (testArray[0] == 0.0f || testArray[1] == 0.0f || testArray[2] == 0.0f) {
            return false;
        }

        var cutResultOne = CutTriangle(a, b.v1, bNorm, testArray);
        var aCapOne = cutResultOne.Item3.v1;
        var bCapOne = cutResultOne.Item3.v2;

        if (MyMath.pointInTriangleInf(b, aCapOne) || MyMath.pointInTriangleInf(b, bCapOne)) {
            return true;
        }

        return false;
    }

    public static bool TriangleIntersectionTest(Triangle a, Triangle b) {
        Profiler.BeginSample("Triangle Intersection Test");
        var result = SemiTriangleIntersectionTest(a, b) || SemiTriangleIntersectionTest(b, a);
        Profiler.EndSample();
        return result;
    }



    public static float PointPlaneSignedDist(Vector3 point, Vector3 planePos, Vector3 planeNorm) {
        var planeD = -Vector3.Dot(planeNorm, planePos);
        return (Vector3.Dot(planeNorm, point) + planeD) / Mathf.Sqrt(Vector3.Dot(planeNorm, planeNorm));
    }
    public static float PointSideTest(Vector3 point, Vector3 planePos, Vector3 planeNorm) {
        var sd = PointPlaneSignedDist(point, planePos, planeNorm);
        if (Mathf.Abs(sd) < 0.00001f) {
            return 0.0f;
        }
        return Mathf.Sign(sd);
    }
    public static bool pointInTriangleInf(Triangle tri, Vector3 point) {
        var u = Vector3.Cross(tri.v2-tri.v1, point-tri.v1);
        var v = Vector3.Cross(tri.v3-tri.v2, point-tri.v2);
        var w = Vector3.Cross(tri.v1-tri.v3, point-tri.v3);


        var dotCompA = Vector3.Dot(u, v);
        var dotCompB = Vector3.Dot(v, w);
        var dotCompC = Vector3.Dot(w, u);
        if (Mathf.Sign(dotCompA) == Mathf.Sign(dotCompB) 
        && Mathf.Sign(dotCompB)== Mathf.Sign(dotCompC)) {
            return true;
        }

        return false;
    }
public static int Pow(this int bas, int exp)
{
    return Enumerable
          .Repeat(bas, exp)
          .Aggregate(1, (a, b) => a * b);
}

// use this to cut triangles by other triangles in the csg algorithm
    public static List<Triangle> DivideTrianglesByOtherTris(Triangle inTri, List<Triangle> cutTris) {

        Profiler.BeginSample("Divide Triangles");
        var finalTriangles = new List<Triangle>();
        var trisToTest = new List<Triangle>();

        trisToTest.Add(inTri);

        var triIterInput = new List<Triangle>();
        triIterInput.Add(inTri);
        var triOutput = new List<Triangle>();
        for (int m = 0 ; m < cutTris.Count; m++) {
            triOutput = new List<Triangle>();
            var cutTri = cutTris[m];
            for (int i = 0; i < triIterInput.Count; i++)
            {
                var testTri = triIterInput[i];
               var planePos = cutTri.v1;
                var planeNorm = cutTri.norm();
                var testArray = new float[3];
                testArray[0] = MyMath.PointSideTest(testTri.v1, planePos, planeNorm);
                testArray[1] = MyMath.PointSideTest(testTri.v2, planePos, planeNorm);
                testArray[2] = MyMath.PointSideTest(testTri.v3, planePos, planeNorm);
                var trianglesIntersectAtEdge = testArray[0] == 0 || testArray[1] == 0 || testArray[2] == 0;
                if (!trianglesIntersectAtEdge
                    && MyMath.TriangleIntersectionTest(testTri, cutTri)) {

                    var (t1, t2, t3, cTest) = CutTriangle(testTri, planePos, planeNorm, testArray);

                    if (cTest >= 0) {
                        triOutput.Add(t1);
                        triOutput.Add(t2);
                        triOutput.Add(t3);
                    } else {
                        triOutput.Add(t1);
                        triOutput.Add(t2);
                        triOutput.Add(t3);
                    }
                } else {
                    triOutput.Add(testTri);
                }
            }

            triIterInput = triOutput;
        }
        Profiler.EndSample();
        return triOutput;
    }

    // ray direction is defined as (rp-ro).normalized
    public static float RayTriangleIntersect(Triangle tri, Vector3 ro, Vector3 rp) {
        Profiler.BeginSample("RayTriangleIntersect");
        var triEps = 0.0001f;
        var centroid = (tri.v1 + tri.v2 + tri.v3) / 3.0f;
        var toV1Dir = (tri.v1 - centroid).normalized;
        var toV2Dir = (tri.v2 - centroid).normalized;
        var toV3Dir = (tri.v3 - centroid).normalized;
        var widerTri = new Triangle(){v1=tri.v1 + toV1Dir * triEps, v2=tri.v2+toV2Dir*triEps, v3=tri.v3+toV3Dir*triEps};

        if (PointSideTest(ro, widerTri.v1, widerTri.norm()) == 0.0f && pointInTriangleInf(widerTri, ro)) {
            Profiler.EndSample();
            return 0.0f;
        }
        

        var planeNorm = tri.norm();
        var planePos = tri.v1;
        var planeD = -Vector3.Dot(planeNorm, planePos);
        var t = -(Vector3.Dot(planeNorm, ro) + planeD) / (Vector3.Dot(planeNorm, rp - ro));
        if (t < -0.000001f) {
            Profiler.EndSample();
            return -1f;;
        }

        var planeIntersect = ro + t * (rp - ro);

        if (pointInTriangleInf(tri, planeIntersect)) {
            Profiler.EndSample();
            return (planeIntersect - ro).magnitude;
        } else {
            Profiler.EndSample();
            return -1f;
        }



    }

    // use even/odd ray intersection test to see if point is inside mesh
    public static float isPointInMesh(Vector3 point, Brush brush) {
        Profiler.BeginSample("isPointInMesh");
        int interCount = 0;

        var arbitrDir = new Vector3(30.5f, 42.0f, 64.0f).normalized;
        var triangleList = new List<Triangle>();
        brush.triBVH.GetPotentialTriRayIntersects(new Ray(point, arbitrDir), triangleList);


        foreach (var thisTri in triangleList)
        {
            var arbitrPoint = point + arbitrDir;
            var distToPoint = RayTriangleIntersect(thisTri, point, arbitrPoint);
            var epsilon = 0.0001f;
            if (distToPoint > epsilon) {
                interCount += 1;
            } else {
                if (distToPoint > -epsilon && distToPoint <= epsilon) {
                    Profiler.EndSample();
                    return 0.0f;
                }
            }
        }

        if (interCount % 2 == 0) {
            Profiler.EndSample();
            return -1.0f;
        } else {
            Profiler.EndSample();
            return 1.0f;
        }
    }
}