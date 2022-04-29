using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
public class TriBVH {
    public TriBVH splitA;
    public TriBVH splitB;
    public Bounds consBounds;
    public Triangle tri;

    public static Bounds GetBounds(List<Triangle> inTris) {
        Vector3 min = inTris[0].v1;
        Vector3 max = inTris[0].v1;
        System.Array.ForEach(inTris.ToArray(), (t) => {
            min = Vector3.Min(min, t.v1);
            max = Vector3.Max(max, t.v1);
            min = Vector3.Min(min, t.v2);
            max = Vector3.Max(max, t.v2);
            min = Vector3.Min(min, t.v3);
            max = Vector3.Max(max, t.v3);
        });
        var boundsCenter = (max + min) * 0.5f;
        var boundsSize = (max - min) * 1.01f;
        return new Bounds(boundsCenter, boundsSize);
    }

    // use insertion sort for low triangle counts
    public static void InsertionSort(List<Triangle> tris, int longestAxis) {
        var count = tris.Count;
        for (int i = 1; i < count; i++) {
            var tri = tris[i];
            var continueInner = true;
            for (int j = i - 1; i < count && continueInner; j++) {
                if (tri.centroid()[longestAxis] < tris[j].centroid()[longestAxis]) {
                    tris[j+1] = tris[j];
                    tris[j] = tri;
                    j -= 1;
                } else {
                    continueInner = false;
                }
            }
        }
    }
    private TriBVH() {

    }

    public TriBVH(List<Triangle> inTris, Bounds? inConsBounds = null) {
        Profiler.BeginSample("BVH Get Bounds");
        if (inConsBounds.HasValue) {
            this.consBounds = inConsBounds.Value;

        } else {
            this.consBounds = GetBounds(inTris);
        }
        Profiler.EndSample();

        if (inTris.Count > 1) {
            // sort triangles' centroids along the longest axis of the current AABB
            // triangles before the median are assigned to one TriBVH
            // triangles at the median and after are assigned to other TriBVH
            // calculate maximum and minimums of the new TriBVH AABBs

            // determine longest axis
            var boundsSize = consBounds.size;
            var longestAxis = 0;
            if (boundsSize.y >= boundsSize.x && boundsSize.y >= boundsSize.z) {
                longestAxis = 1;
            } else if (boundsSize.z >= boundsSize.x && boundsSize.z >= boundsSize.y) {
                longestAxis = 2;
            }

            // sort triangles along longest axis
            Profiler.BeginSample("BVH Sort");
            if (inTris.Count < 15) {
                InsertionSort(inTris, longestAxis);
            } else {
                inTris.Sort((t1, t2) => t1.centroid()[longestAxis].CompareTo(t2.centroid()[longestAxis]));
            }
            Profiler.EndSample();
            var iMiddle = inTris.Count / 2;

            // calculate new tribvh aabbs
            var aSplitAxisMax = inTris[0].v1[longestAxis];
            var bSplitAxisMin = inTris[iMiddle].v1[longestAxis];
            var aSplitTris = new List<Triangle>(iMiddle + 1);
            var bSplitTris = new List<Triangle>(iMiddle + 1);
            for (int i = 0; i < iMiddle; i++)
            {
                aSplitAxisMax = Mathf.Max(aSplitAxisMax, inTris[i].v1[longestAxis]);
                aSplitAxisMax = Mathf.Max(aSplitAxisMax, inTris[i].v2[longestAxis]);
                aSplitAxisMax = Mathf.Max(aSplitAxisMax, inTris[i].v3[longestAxis]);

                aSplitTris.Add(inTris[i]);
            }
            for (int i = iMiddle; i < inTris.Count; i++) {
                bSplitAxisMin = Mathf.Min(bSplitAxisMin, inTris[i].v1[longestAxis]);
                bSplitAxisMin = Mathf.Min(bSplitAxisMin, inTris[i].v2[longestAxis]);
                bSplitAxisMin = Mathf.Min(bSplitAxisMin, inTris[i].v3[longestAxis]);

                bSplitTris.Add(inTris[i]);
            }
            var aSplitBoundsMax = this.consBounds.max;
            aSplitBoundsMax[longestAxis] = aSplitAxisMax + 0.0001f;
            var bSplitBoundsMin = this.consBounds.min;
            bSplitBoundsMin[longestAxis] = bSplitAxisMin - 0.0001f;

            var aSplitCenter = (aSplitBoundsMax + this.consBounds.min) * 0.5f;
            var aSplitSizes = (aSplitBoundsMax - this.consBounds.min);
            var aSplitBounds = new Bounds(aSplitCenter, aSplitSizes);

            var bSplitCenter = (bSplitBoundsMin + this.consBounds.max) * 0.5f;
            var bSplitSizes = (this.consBounds.max - bSplitBoundsMin);
            var bSplitBounds = new Bounds(bSplitCenter, bSplitSizes);

            // create new triBVHs
            this.splitA = new TriBVH(aSplitTris, aSplitBounds);
            this.splitB = new TriBVH(bSplitTris, bSplitBounds);
        } else {
            // leaf node case
            this.tri = inTris[0];
        }
    }

    public void GetPotentialTriBoundsIntersects(Bounds bounds, List<Triangle> outList) {
        if (this.consBounds.Intersects(bounds)) {
            if (this.splitA == null) {
                outList.Add(this.tri);
            } else {
                this.splitA.GetPotentialTriBoundsIntersects(bounds, outList);
                this.splitB.GetPotentialTriBoundsIntersects(bounds, outList);
            }
        }
    }

    public void GetPotentialTriRayIntersects(Ray ray, List<Triangle> outList) {
        if (this.consBounds.IntersectRay(ray)) {
            if (this.splitA == null) {
                outList.Add(this.tri);
            } else {
                this.splitA.GetPotentialTriRayIntersects(ray, outList);
                this.splitB.GetPotentialTriRayIntersects(ray, outList);
            }
        }
    }


}