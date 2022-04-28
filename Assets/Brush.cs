   using UnityEngine; 
   using UnityEngine.Profiling;
   using System.Collections.Generic;
   using System.Linq;
    
    public class Brush {
        public List<Vector3> vertices;
        public Bounds consBounds;
        public TriBVH triBVH;
        public void CreateTriBVH() {
            Profiler.BeginSample("Create BVH");
            // this.triBVH = TriBVH.constructBVH(this.makeTriangleList());
            this.triBVH = new TriBVH(this.makeTriangleList());
            Profiler.EndSample();
        }
        /*
        public BSPSys triBSP;
        public void CreateBSP() {
            this.triBSP = new BSPSys(new Triangle(){v1=vertices[0], v2=vertices[1], v3=vertices[2]});

            for (int i = 3; i < this.vertices.Count; i += 3) {
                this.triBSP.AddTriangle(new Triangle(){v1=vertices[i+0], v2=vertices[i+1], v3=vertices[i+2]});
            }
        }
        */

        public void SetVerticesFromTris(List<Triangle> tris) {
            vertices = new List<Vector3>();
            foreach (var tri in tris)
            {
                vertices.Add(tri.v1);
                vertices.Add(tri.v2);
                vertices.Add(tri.v3);
            }
        }


        public List<Triangle> GetTriIntersections(Triangle otherTri) {
            var intersects = new List<Triangle>();
            var bvhTris = new List<Triangle>();
            this.triBVH.GetPotentialTriBoundsIntersects(otherTri.getConsBounds(), bvhTris);
            foreach (var bvhTri in bvhTris)
            {
                var result = MyMath.TriangleIntersectionTest(otherTri, bvhTri);
                if (result) {
                    intersects.Add(bvhTri);
                }
            }
            return intersects;
            /*
            for (int i = 0; i < vertices.Count; i += 3)
            {
                var thisTri = new Triangle(){v1=vertices[i+0], v2=vertices[i+1], v3=vertices[i+2]};

                var consTest = otherTri.getConsBounds().Intersects(thisTri.getConsBounds());
                if (consTest == false) {
                    continue;
                }
                
                var result = MyMath.TriangleIntersectionTest(otherTri, thisTri);

                if (result) {
                    intersects.Add(thisTri);
                }
            }

            return intersects;
            */
        }

        public List<Triangle> makeTriangleList() {
            var outList = new List<Triangle>();
            for (int i = 0; i < vertices.Count; i += 3)
            {
                var thisTri = new Triangle(){v1=vertices[i+0], v2=vertices[i+1], v3=vertices[i+2]};
                outList.Add(thisTri);
            }   
            return outList;
        }

        public (List<Triangle>, List<Triangle>) SplitMesh(List<Triangle> baseTris) {
            Profiler.BeginSample("SplitMesh");
            var validTriangles = new List<Triangle>();
            var intersectedTriangles = new List<Triangle>();
            var intersectedTrianglesIntersects = new List<List<Triangle>>();
            for (int i = 0; i < baseTris.Count; i++)
            {
                var result = this.GetTriIntersections(baseTris[i]);
                if (result.Count > 0) {
                    intersectedTriangles.Add(baseTris[i]);
                    intersectedTrianglesIntersects.Add(result);
                } else {
                    validTriangles.Add(baseTris[i]);
                }
            }
            var insideTriangles = new List<Triangle>();
            var outsideTriangles = new List<Triangle>();

            Profiler.BeginSample("Valid Triangles In Mesh Test");
            foreach (var tri in validTriangles)
            {
                var centroidTest = MyMath.isPointInMesh(tri.centroid(), this);

                if (centroidTest == -1.0f) {
                    outsideTriangles.Add(tri);
                } else {
                    insideTriangles.Add(tri);
                }
            }
            Profiler.EndSample();

            var splitTriangles = new List<Triangle>();
            for (int i = 0; i < intersectedTriangles.Count; i++)
            {
                var resultTris = MyMath.DivideTrianglesByOtherTris(intersectedTriangles[i], intersectedTrianglesIntersects[i]);
                splitTriangles = splitTriangles.Concat(resultTris).ToList();
            }

            Profiler.BeginSample("Split Triangles In Mesh Test");
            foreach (var tri in splitTriangles)
            {
                var centroidTest = MyMath.isPointInMesh(tri.centroid(), this);

                if (centroidTest == -1.0f) {
                    outsideTriangles.Add(tri);
                } else {
                    insideTriangles.Add(tri);
                }
            }
            Profiler.EndSample();

            Profiler.EndSample();
            return (insideTriangles, outsideTriangles);
        }
    }