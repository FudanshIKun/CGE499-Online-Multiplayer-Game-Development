using System;
using System.Collections.Generic;
using UnityEngine;


namespace ToruToru{
    /// <summary>
    /// 
    /// Source Code: https://gist.github.com/ditzel/73f4d1c9028cc3477bb921974f84ed56
    /// </summary>
    internal class Destructible : MonoBehaviour{
        //-----------//
        // INSPECTOR //
        //-----------//
        // PUBLIC PROPERTIES //
        public GameObject Owner { get; private set; }
        // PUBLIC FIELDS //
        public int CutCascades = 1;
        public float ExplodeForce;
        public bool DestroyGameObjectAlongWithMesh;
        
        // PRIVATE FIELDS //
        private bool edgeSet;
        private Vector3 edgeVertex = Vector3.zero;
        private Vector2 edgeUV = Vector2.zero;
        private Plane edgePlane;
        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        private void Awake()
            => Owner = gameObject;

        //---------//
        // METHODS //
        //---------//
        public void DestroyMesh(){
            var originalMesh = GetComponent<MeshFilter>().mesh;
            originalMesh.RecalculateBounds();
            var parts = new List<PartMesh>();
            var subParts = new List<PartMesh>();
            var mainPart = new PartMesh(){
                UV = originalMesh.uv,
                Vertices = originalMesh.vertices,
                Normals = originalMesh.normals,
                Triangles = new int[originalMesh.subMeshCount][],
                Bounds = originalMesh.bounds
            };
            
            for (var i = 0; i < originalMesh.subMeshCount; i++)
                mainPart.Triangles[i] = originalMesh.GetTriangles(i);
            parts.Add(mainPart);
            for (var c = 0; c < CutCascades; c++){
                foreach (var t in parts){
                    var bounds = t.Bounds;
                    bounds.Expand(0.5f);
                    var plane = new Plane(
                        UnityEngine.Random.onUnitSphere, 
                        new Vector3(
                            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                            UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                        )
                    );
                    
                    subParts.Add(GenerateMesh(t, plane, true));
                    subParts.Add(GenerateMesh(t, plane, false));
                }
                parts = new List<PartMesh>(subParts);
                subParts.Clear();
            }

            foreach (var part in parts){
                part.MakeGameObject(this);
                part.GameObject.GetComponent<Rigidbody>().AddForceAtPosition(part.Bounds.center * ExplodeForce, transform.position);
            }
            
            if (DestroyGameObjectAlongWithMesh)
                Destroy(gameObject);
        }
            
        private PartMesh GenerateMesh(PartMesh original, Plane plane, bool left){
            var partMesh = new PartMesh() { };
            var rayA = new Ray();
            var rayB = new Ray();
            for (var i = 0; i < original.Triangles.Length; i++){
                var triangles = original.Triangles[i];
                edgeSet = false;
                for (var j = 0; j < triangles.Length; j += 3){
                    var sideA = plane.GetSide(original.Vertices[triangles[j]]) == left;
                    var sideB = plane.GetSide(original.Vertices[triangles[j + 1]]) == left;
                    var sideC = plane.GetSide(original.Vertices[triangles[j + 2]]) == left;
                    var sideCount = (sideA ? 1 : 0) + (sideB ? 1 : 0) + (sideC ? 1 : 0);
                    switch (sideCount){
                        case 0: continue;
                        case 3:
                            partMesh.AddTriangle(i,
                                original.Vertices[triangles[j]], original.Vertices[triangles[j + 1]],
                                original.Vertices[triangles[j + 2]],
                                original.Normals[triangles[j]], original.Normals[triangles[j + 1]],
                                original.Normals[triangles[j + 2]],
                                original.UV[triangles[j]], original.UV[triangles[j + 1]], original.UV[triangles[j + 2]]
                            );
                            
                            continue;
                    }

                    // CUT POINTS
                    var singleIndex = sideB == sideC ? 0 : sideA == sideC ? 1 : 2;
                    rayA.origin = original.Vertices[triangles[j + singleIndex]];
                    var dirA = original.Vertices[triangles[j + ((singleIndex + 1) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                    rayA.direction = dirA;
                    plane.Raycast(rayA, out var enter1);
                    var valueA = enter1 / dirA.magnitude;

                    rayB.origin = original.Vertices[triangles[j + singleIndex]];
                    var dirB = original.Vertices[triangles[j + ((singleIndex + 2) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                    rayB.direction = dirB;
                    plane.Raycast(rayB, out var enter2);
                    var valueB = enter2 / dirB.magnitude;

                    // Adjust points slightly if they are too close to being coplanar
                    const float tolerance = 0.001f; // Adjust as needed
                    if (Mathf.Approximately(enter1, enter2) || Mathf.Abs(enter1 - enter2) < tolerance){
                        enter1 -= tolerance; // Adjust enter1 slightly
                        enter2 += tolerance; // Adjust enter2 slightly
                    }

                    // FIRST VERTEX == ANCHOR
                    AddEdge(i,
                        partMesh,
                        left ? plane.normal * -1f : plane.normal,
                        rayA.origin + rayA.direction.normalized * enter1,
                        rayB.origin + rayB.direction.normalized * enter2,
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], valueA),
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], valueB)
                    );

                    switch (sideCount){
                        case 1:
                            partMesh.AddTriangle(i,
                                original.Vertices[triangles[j + singleIndex]],
                                rayA.origin + rayA.direction.normalized * enter1,
                                rayB.origin + rayB.direction.normalized * enter2,
                                original.Normals[triangles[j + singleIndex]],
                                Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], valueA),
                                Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], valueB),
                                original.UV[triangles[j + singleIndex]],
                                Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], valueA),
                                Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], valueB)
                            );
                            continue;
                        case 2:
                            partMesh.AddTriangle(i,
                                rayA.origin + rayA.direction.normalized * enter1,
                                original.Vertices[triangles[j + ((singleIndex + 1) % 3)]],
                                original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], valueA),
                                original.Normals[triangles[j + ((singleIndex + 1) % 3)]],
                                original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], valueA),
                                original.UV[triangles[j + ((singleIndex + 1) % 3)]],
                                original.UV[triangles[j + ((singleIndex + 2) % 3)]]
                            );

                            partMesh.AddTriangle(i,
                                rayA.origin + rayA.direction.normalized * enter1,
                                original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                rayB.origin + rayB.direction.normalized * enter2,
                                Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], valueA),
                                original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], valueB),
                                Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], valueA),
                                original.UV[triangles[j + ((singleIndex + 2) % 3)]],
                                Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], valueB)
                            );
                            
                            continue;
                    }
                }
            }

            partMesh.FillArrays();
            return partMesh;
        }

        private void AddEdge(int subMesh, PartMesh partMesh, Vector3 normal, Vector3 vertexA, Vector3 vertexB, Vector2 uvA, Vector2 uvB){
            if (!edgeSet){
                edgeSet = true;
                edgeVertex = vertexA;
                edgeUV = uvA;
            }
            else{
                edgePlane.Set3Points(edgeVertex, vertexA, vertexB);
                partMesh.AddTriangle(
                    subMesh,
                    edgeVertex,
                    edgePlane.GetSide(edgeVertex + normal) ? vertexA : vertexB,
                    edgePlane.GetSide(edgeVertex + normal) ? vertexB : vertexA,
                    normal,
                    normal,
                    normal,
                    edgeUV,
                    uvA,
                    uvB
                );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class PartMesh{
            //-----------//
            // INSPECTOR //
            //-----------//
            private readonly List<Vector3> _Verticies = new();
            private readonly List<Vector3> _Normals = new();
            private readonly List<List<int>> _Triangles = new();
            private readonly List<Vector2> _UVs = new();
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public int[][] Triangles;
            public Vector2[] UV;
            public GameObject GameObject;
            public Bounds Bounds;

            //---------//
            // METHODS //
            //---------//
            public void AddTriangle(int subMesh, Vector3 vertA, Vector3 vertB, Vector3 vertC, Vector3 normalA, Vector3 normalB, Vector3 normalC, Vector2 uvA, Vector2 uvB, Vector2 uvC){
                if (_Triangles.Count - 1 < subMesh)
                    _Triangles.Add(new List<int>());
                _Triangles[subMesh].Add(_Verticies.Count);
                _Verticies.Add(vertA);
                _Triangles[subMesh].Add(_Verticies.Count);
                _Verticies.Add(vertB);
                _Triangles[subMesh].Add(_Verticies.Count);
                _Verticies.Add(vertC);
                _Normals.Add(normalA);
                _Normals.Add(normalB);
                _Normals.Add(normalC);
                _UVs.Add(uvA);
                _UVs.Add(uvB);
                _UVs.Add(uvC);
                Bounds.min = Vector3.Min(Bounds.min, vertA);
                Bounds.min = Vector3.Min(Bounds.min, vertB);
                Bounds.min = Vector3.Min(Bounds.min, vertC);
                Bounds.max = Vector3.Min(Bounds.max, vertA);
                Bounds.max = Vector3.Min(Bounds.max, vertB);
                Bounds.max = Vector3.Min(Bounds.max, vertC);
            }

            public void FillArrays(){
                Vertices = _Verticies.ToArray();
                Normals = _Normals.ToArray();
                UV = _UVs.ToArray();
                Triangles = new int[_Triangles.Count][];
                for (var i = 0; i < _Triangles.Count; i++)
                    Triangles[i] = _Triangles[i].ToArray();
            }

            public void MakeGameObject(Destructible original){
                GameObject = new GameObject($"{original.name}(Part)"){
                    transform ={
                        position = original.transform.position,
                        rotation = original.transform.rotation,
                        localScale = original.transform.localScale
                    }
                };

                var mesh = new Mesh{
                    name = original.GetComponent<MeshFilter>().mesh.name,
                    vertices = Vertices,
                    normals = Normals,
                    uv = UV
                };

                for(var i = 0; i < Triangles.Length; i++)
                    mesh.SetTriangles(Triangles[i], i, true);
                Bounds = mesh.bounds;
                
                var renderer = GameObject.AddComponent<MeshRenderer>();
                renderer.materials = original.GetComponent<MeshRenderer>().materials;

                var filter = GameObject.AddComponent<MeshFilter>();
                filter.mesh = mesh;

                var collider = GameObject.AddComponent<MeshCollider>();
                collider.convex = true;
                
                GameObject.AddComponent<Rigidbody>();
                var component = GameObject.AddComponent<Destructible>();
                component.CutCascades = original.CutCascades;
                component.ExplodeForce = original.ExplodeForce;
                Destroy(component.gameObject, 3f);
            }
        }
    }
}