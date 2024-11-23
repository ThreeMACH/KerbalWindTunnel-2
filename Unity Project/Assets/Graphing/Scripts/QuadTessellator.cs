using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Graphing.Meshing
{
    public class QuadTessellator
    {
        private readonly List<int> indices = new List<int>();
        public List<int> Indices { get => indices; }
        private readonly List<int> indices_backup = new List<int>();

        private readonly List<Vertex> vertices = new List<Vertex>();
        public IEnumerable<Vector3> Vertices { get => vertices.Select(v => v.position); }
        private readonly List<Vector3> vertices_backup = new List<Vector3>();

        private readonly List<Quad> quads = new List<Quad>();

        private bool indicesAreQuads;
        public bool IndicesAreQuads { get => indicesAreQuads; }

        public QuadTessellator(Mesh mesh, int submesh = 0) => Setup(mesh, submesh);
        public QuadTessellator(IEnumerable<int> indices, IEnumerable<Vector3> vertices) => Setup(indices, vertices);

        public IEnumerable<Vector4> Coords { get => vertices.Select(v => v.vertexData.coords); }
        public IEnumerable<Vector4> Heights { get => vertices.Select(v => v.vertexData.heights); }

        public void Setup(Mesh mesh, int submesh = 0)
        {
            if (mesh.GetTopology(submesh) != MeshTopology.Quads)
                throw new ArgumentException("Mesh topology must be quads.");
            Setup(mesh.GetIndices(submesh), mesh.vertices);
        }
        public void Setup(IEnumerable<int> indices, IEnumerable<Vector3> vertices)
        {
            indicesAreQuads = true;
            this.indices.Clear();
            this.indices.AddRange(indices);
            this.vertices.Clear();
            quads.Clear();

            IEnumerator<Vector3> enumerator = vertices.GetEnumerator();
            int index = -1;
            while (enumerator.MoveNext())
            {
                index++;
                this.vertices.Add(new Vertex(enumerator.Current, index, overwriteVertexData: false));
            }

            int[] vertexQuadIndex = new int[this.vertices.Count];
            for (int i = 0; i < vertexQuadIndex.Length; i++)
                vertexQuadIndex[i] = -1;
            for (int i = 0; i < this.indices.Count; i += 4)
            {
                vertexQuadIndex[this.indices[i]] = i;
            }

            for (int i = 0; i < this.vertices.Count; i++)
            {
                int quadIndex = vertexQuadIndex[i];
                if (quadIndex < 0)
                    continue;
                Vector4 coords = new Vector4(
                    this.vertices[i].x, this.vertices[i].y,
                    this.vertices[this.indices[quadIndex + 1]].x - this.vertices[i].x,
                    this.vertices[this.indices[quadIndex + 3]].y - this.vertices[i].y);
                Vector4 heights = new Vector4(this.vertices[i].z, this.vertices[this.indices[quadIndex + 1]].z, this.vertices[this.indices[quadIndex + 3]].z, this.vertices[this.indices[quadIndex + 2]].z);
                Vertex v = this.vertices[i];
                v.vertexData = (coords, heights);
                this.vertices[i] = v;
            }
            for (int i = 0; i < this.indices.Count; i += 4)
            {
                quads.Add(new Quad(this.vertices[this.indices[i]], this.vertices[this.indices[i + 3]], this.vertices[this.indices[i + 2]], this.vertices[this.indices[i + 1]]));
            }
            indices_backup.Clear();
            indices_backup.AddRange(this.indices);
            vertices_backup.Clear();
            vertices_backup.AddRange(this.vertices.Select(v => v.position));
        }
        public void Reset() => Setup(indices_backup, vertices_backup);

        public void SubdivideForDegeneracy(float scale = 1, float tolerance = 0.015625f)
        {
            if (!indicesAreQuads)
                throw new InvalidOperationException("Cannot subdivide when already using tris.");
            indices.Clear();
            Parallel.For(0, quads.Count, () => new LocalData(scale, tolerance), TessellateQuad, TessPostProcess);
            indicesAreQuads = false;
        }

        public static int TessFactor(float degeneracy, float scale, float tolerance)
        {
            degeneracy /= scale;
            if (degeneracy <= tolerance)
                return 0;
            return Mathf.CeilToInt(Mathf.Log(degeneracy / tolerance, 2) / 2);
        }

        private LocalData TessellateQuad(int index, ParallelLoopState loopState, LocalData localData)
        {
            Quad quad = quads[index];
            int tessFactor = TessFactor(quad.ZDegeneracy, localData.scale, localData.tolerance);

            // First subdivide the quad
            if (tessFactor <= 0)
            {
                quad.isFlat = true;
                localData.quads.Add(quad);
                return localData;
            }
            TessQuadInternal(quad, localData, tessFactor, loopState);

            // Then cross-tessellate it (inside TessPostProcess)
            return localData;
        }

        private static void TessQuadInternal(Quad quad, LocalData data, int tessFactor, ParallelLoopState loopState = null)
        {
            if (loopState != null && loopState.ShouldExitCurrentIteration)
                return;

            if (tessFactor <= 0)
            {
                Debug.LogWarning("Quad Tessellation - invalid tessFactor");
                return;
            }
            if (tessFactor == 1)
            {
                data.quads.Add(quad);
                return;
            }

            IEnumerable<Quad> newQuads = quad.Subdivide(data.newVertices);
            foreach (Quad q in newQuads)
                TessQuadInternal(q, data, tessFactor - 1, loopState);
        }

        private static void RemoveNewDuplicates(List<Vertex> vertices, List<Vertex> indices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertex v1 = vertices[i];
                if (v1.prohibitMerge)
                    continue;
                for (int j = vertices.Count - 1; j > i; j--)
                {
                    Vertex v2 = vertices[j];
                    if (v2.prohibitMerge || (!v1.overwriteVertexData && !v2.overwriteVertexData))
                        continue;
                    if (v1.position != v2.position)
                        continue;
                    vertices.RemoveAt(j);
                    for (int k = indices.Count - 1; k >= 0; k--)
                    {
                        if (indices[k].indexIsRelative)
                        {
                            if (indices[k].index == j)
                            {
                                Vertex vi = indices[k];
                                vi.index = i;
                                indices[k] = vi;
                            }
                            else if (indices[k].index > j)
                            {
                                Vertex vi = indices[k];
                                vi.index--;
                                indices[k] = vi;
                            }
                        }
                    }
                    v1.vertexData = v2.overwriteVertexData ? v1.vertexData : v2.vertexData;
                    v1.overwriteVertexData = v1.overwriteVertexData && v2.overwriteVertexData;
                    vertices[i] = v1;
                }
            }
        }
        private static void RemoveDuplicates(List<Vertex> vertices, List<int> indices, int startIndexV = 0, int endIndexV = -1, int startIndexI = 0)
        {
            if (endIndexV < 0)
                endIndexV = vertices.Count;

            for (int i = 0; i < Math.Min(endIndexV, vertices.Count); i++)
            {
                Vertex v1 = vertices[i];
                if (v1.prohibitMerge)
                    continue;
                for (int j = vertices.Count - 1; j > Math.Max(i, startIndexV - 1); j--)
                {
                    Vertex v2 = vertices[j];
                    if (v2.prohibitMerge || (!v1.overwriteVertexData && !v2.overwriteVertexData))
                        continue;
                    if (v1.position != v2.position)
                        continue;
                    vertices.RemoveAt(j);
                    for (int k = indices.Count - 1; k >= startIndexI; k--)
                    {
                        if (indices[k] == j)
                            indices[k] = i;
                        else if (indices[k] > j)
                            indices[k]--;
                    }
                    v1.vertexData = v2.overwriteVertexData ? v1.vertexData : v2.vertexData;
                    v1.overwriteVertexData &= v2.overwriteVertexData;
                    vertices[i] = v1;
                }
            }
        }

        private static IEnumerable<int> GlobalizeIndices(int offset, List<Vertex> indices)
        {
            for (int i = 0; i < indices.Count; i++)
                yield return indices[i].indexIsRelative ? indices[i].index + offset : indices[i].index;
        }

        private void TessPostProcess(LocalData data)
        {
            // Tessellate to tris
            for (int i = 0; i < data.quads.Count; i++)
            {
                if (data.quads[i].isFlat)
                    TessQuadToFlatTris(data.quads[i], data);
                else
                    TessQuadToTris(data.quads[i], data);
            }
            // Pre-cull duplicate vertices.
            RemoveNewDuplicates(data.newVertices, data.newIndices);
            // Integrate vertices and tris then cull duplicate vertices.
            lock (this)
            {
                int vertCount = vertices.Count;
                int indexCount = indices.Count;
                vertices.AddRange(data.newVertices);
                indices.AddRange(GlobalizeIndices(vertCount, data.newIndices));
                RemoveDuplicates(vertices, indices, vertCount, vertCount, indexCount);
            }
        }

        private static void TessQuadToTris(Quad quad, LocalData data)
        {
            Vertex center = new Vertex((quad[0].position + quad[1].position + quad[2].position + quad[3].position) / 4, data.newVertices.Count, true, quad[0].vertexData);
            data.newVertices.Add(center);
            data.newIndices.Add(center);
            data.newIndices.Add(quad[0]);
            data.newIndices.Add(quad[1]);
            data.newIndices.Add(center);
            data.newIndices.Add(quad[1]);
            data.newIndices.Add(quad[2]);
            data.newIndices.Add(center);
            data.newIndices.Add(quad[2]);
            data.newIndices.Add(quad[3]);
            data.newIndices.Add(center);
            data.newIndices.Add(quad[3]);
            data.newIndices.Add(quad[0]);
        }
        private static void TessQuadToFlatTris(Quad quad, LocalData data)
        {
            data.newIndices.Add(quad[0]);
            data.newIndices.Add(quad[1]);
            data.newIndices.Add(quad[2]);
            Vertex dupe = new Vertex(quad[2].position, data.newVertices.Count, true, quad[0].vertexData)
            {
                prohibitMerge = true
            };
            data.newVertices.Add(dupe);
            data.newIndices.Add(dupe);
            data.newIndices.Add(quad[3]);
            data.newIndices.Add(quad[0]);
        }

        public struct Quad
        {
            private Vertex v0, v1, v2, v3;
            public Vertex this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return v0;
                        case 1: return v1;
                        case 2: return v2;
                        case 3: return v3;
                        default: throw new ArgumentOutOfRangeException("Index must be [0, 3].");
                    }
                    /*return index switch
                    {
                        0 => v0,
                        1 => v1,
                        2 => v2,
                        3 => v3,
                        _ => throw new ArgumentOutOfRangeException("Index must be [0, 3]."),
                    };*/
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            v0 = value;
                            return;
                        case 1:
                            v1 = value;
                            return;
                        case 2:
                            v2 = value;
                            return;
                        case 3:
                            v3 = value;
                            return;
                        default:
                            throw new ArgumentOutOfRangeException("Index must be [0, 3].");
                    }
                }
            }
            public Quad(Vertex v0, Vertex v1, Vertex v2, Vertex v3)
            {
                this.v0 = v0;
                this.v1 = v1;
                this.v2 = v2;
                this.v3 = v3;
                isFlat = false;
            }
            public bool isFlat;

            public float ZDegeneracy => Mathf.Abs((v0.z + v2.z) / 2 - (v0.z + v1.z + v2.z + v3.z) / 4);

            public IEnumerable<Quad> Subdivide(List<Vertex> newVertices)
            {
                int offset = newVertices.Count;
                Vertex nv0 = new Vertex((v0.position + v1.position + v2.position + v3.position) / 4, offset, true, v0.vertexData);
                Vertex nv1 = new Vertex((v0.position + v1.position) / 2, offset + 1, true, v0.vertexData);
                Vertex nv2 = new Vertex((v1.position + v2.position) / 2, offset + 2, true, true);
                Vertex nv3 = new Vertex((v2.position + v3.position) / 2, offset + 3, true, true);
                Vertex nv4 = new Vertex((v3.position + v0.position) / 2, offset + 4, true, v0.vertexData);
                newVertices.Add(nv0);
                newVertices.Add(nv1);
                newVertices.Add(nv2);
                newVertices.Add(nv3);
                newVertices.Add(nv4);
                for (int i = 0; i < 4; i++)
                {
                    /*var q = i switch
                    {
                        0 => new Quad(v0, nv1, nv0, nv4),
                        1 => new Quad(nv1, v1, nv2, nv0),
                        2 => new Quad(nv0, nv2, v2, nv3),
                        3 => new Quad(nv4, nv0, nv3, v3),
                        _ => throw new ArgumentOutOfRangeException(),
                    };*/
                    Quad q;
                    switch (i)
                    {
                        case 0: q = new Quad(v0, nv1, nv0, nv4);
                            break;
                        case 1: q = new Quad(nv1, v1, nv2, nv0);
                            break;
                        case 2: q = new Quad(nv0, nv2, v2, nv3);
                            break;
                        case 3: q = new Quad(nv4, nv0, nv3, v3);
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                    //newIndices.AddRange(q.AsVertexSequence());
                    yield return q;
                }
            }

            public IEnumerable<Vertex> AsVertexSequence()
            {
                yield return v0;
                yield return v1;
                yield return v2;
                yield return v3;
            }
        }

        public struct Vertex
        {
            public Vector3 position;
            public int index;
            public bool indexIsRelative;
            public (Vector4 coords, Vector4 heights) vertexData;
            public bool prohibitMerge;
            public bool overwriteVertexData;
#pragma warning disable IDE1006 // Naming Styles
            public float x { get => position.x; set => position.x = value; }
            public float y { get => position.y; set => position.y = value; }
            public float z { get => position.z; set => position.z = value; }
#pragma warning restore IDE1006 // Naming Styles

            public Vertex(Vector3 position, int index, bool indexIsRelative, (Vector4, Vector4) data)
            {
                this.position = position;
                this.index = index;
                this.indexIsRelative = indexIsRelative;
                vertexData = data;
                overwriteVertexData = false;
                prohibitMerge = false;
            }
            public Vertex(Vector3 position, int index, bool indexIsRelative, Vector4 coords, Vector4 heights)
                : this(position, index, indexIsRelative, (coords, heights)) { }
            public Vertex(Vector3 position, int index, Vector4 coords, Vector4 heights)
                : this(position, index, false, coords, heights) { }

            public Vertex(Vector3 position, int index, bool indexIsRelative = false, bool overwriteVertexData = true)
            {
                this.position = position;
                this.index = index;
                this.indexIsRelative = indexIsRelative;
                this.overwriteVertexData = overwriteVertexData;
                vertexData = (Vector4.zero, Vector4.zero);
                prohibitMerge = false;
            }
        }

        public readonly struct LocalData
        {
            public readonly List<Vertex> newIndices;
            public readonly List<Vertex> newVertices;
            public readonly float scale;
            public readonly float invScale;
            public readonly float tolerance;
            public readonly List<Quad> quads;

            public LocalData(float scale, float tolerance)
            {
                newIndices = new List<Vertex>();
                newVertices = new List<Vertex>();
                quads = new List<Quad>();
                this.scale = scale;
                invScale = 1 / scale;
                this.tolerance = tolerance;
            }
        }
    }
}
