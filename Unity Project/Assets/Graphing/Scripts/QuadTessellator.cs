using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Graphing.Meshing
{
    public class QuadTessellator
    {
        public const int maxTessFactor = 5;
        private readonly List<int> indices = new List<int>();
        public List<int> Indices { get => indices; }
        private readonly List<int> indices_backup = new List<int>();

        private readonly List<VertexMeta> vertices = new List<VertexMeta>();
        public IEnumerable<Vector3> Vertices { get => vertices.Select(v => v.position); }
        private readonly List<Vector3> vertices_backup = new List<Vector3>();

        private readonly List<Quad> quads = new List<Quad>();

        private bool indicesAreQuads;
        public bool IndicesAreQuads { get => indicesAreQuads; }

        public QuadTessellator(Mesh mesh, int submesh = 0) => Setup(mesh, submesh);
        public QuadTessellator(IList<int> indices, IList<Vector3> vertices) => Setup(indices, vertices);

        public IEnumerable<Vector4> Coords { get => vertices.Select(v => v.coords); }
        public IEnumerable<Vector4> Heights { get => vertices.Select(v => v.heights); }

        public void Setup(Mesh mesh, int submesh = 0)
        {
            if (mesh.GetTopology(submesh) != MeshTopology.Quads)
                throw new ArgumentException("Mesh topology must be quads.");
            Setup(mesh.GetIndices(submesh), mesh.vertices);
        }
        private void Setup(IList<int> indices, IList<Vector3> vertices)
        {
            indicesAreQuads = true;
            this.indices.Clear();
            this.indices.AddRange(indices);
            this.vertices.Clear();
            this.vertices.AddRange(vertices.Select(v => new VertexMeta(v)));
            quads.Clear();

            for (int i = 0; i < this.indices.Count; i += 4)
            {
                // This seems to wind the wrong way... But if it works, it works.
                quads.Add(new Quad(vertices, indices[i], indices[i + 3], indices[i + 2], indices[i + 1]));
            }

            indices_backup.Clear();
            indices_backup.AddRange(this.indices);
            vertices_backup.Clear();
            vertices_backup.AddRange(this.vertices.Select(v => v.position));
        }
        public void Reset() => Setup(indices_backup, vertices_backup);

        // The tessellation into tris could be broken out to a separate method, but I can't be bothered right now.
        public void SubdivideForDegeneracy(float scale = 1, float tolerance = 0.015625f)
        {
            if (!indicesAreQuads)
                throw new InvalidOperationException("Cannot subdivide when already using tris.");
            indices.Clear();
            Parallel.For(0, quads.Count,
                () => new LocalData(scale, tolerance),
                TessellateQuad,
                TessPostProcess);
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
            int tessFactor = TessFactor(quad.zDegeneracy, localData.scale, localData.tolerance);
            if (quad.isFlat || tessFactor == 0)
            {
                localData.quads.Add(quad);
                return localData;
            }

            // First subdivide the quad
            if (tessFactor > maxTessFactor)
            {
                Debug.LogWarning("Quad Tessellation - tessFactor too high.");
                tessFactor = maxTessFactor;
            }
            TessQuadInternal(in quad, localData, tessFactor, loopState);

            // Then cross-tessellate them (inside TessPostProcess)
            return localData;
        }

        private static void TessQuadInternal(in Quad quad, LocalData data, int tessFactor, ParallelLoopState loopState = null)
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

            foreach (Quad q in quad.Subdivide(data.newVertices))
                TessQuadInternal(in q, data, tessFactor - 1, loopState);
        }

        private static void RemoveDuplicates(List<VertexMeta> vertices, IList<int> indices)
        {
            Dictionary<VertexMeta, int> vertIndices = new Dictionary<VertexMeta, int>(vertices.Count);
            int[] reIndeces = new int[vertices.Count];
            int vertCount = -1;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (vertIndices.TryGetValue(vertices[i], out reIndeces[i]))
                {
                    vertices[reIndeces[i]] = VertexMeta.Merge(vertices[reIndeces[i]], vertices[i]);
                }
                else
                {
                    vertCount++;
                    reIndeces[i] = vertCount;
                    vertIndices.Add(vertices[i], vertCount);
                    vertices[vertCount] = vertices[i];
                }
            }
            for (int i = vertices.Count - 1; i > vertCount; i--)
                vertices.RemoveAt(i);

            for (int i = indices.Count - 1; i >= 0; i--)
                indices[i] = reIndeces[indices[i]];
        }

        private void TessPostProcess(LocalData data)
        {
            List<Index> localIndices = new List<Index>(4 * data.quads.Count);
            List<VertexMeta> localVertices = data.newVertices.Select(v => new VertexMeta(v)).ToList();
            List<Quad> quads = data.quads;

            // Tessellate to tris
            int newVertCount = data.newVertices.Count;
            List<VertexMeta> tessdVertices = new List<VertexMeta>(quads.Count);
            for (int i = 0; i < quads.Count; i++)
            {
                Quad quad = quads[i];
                if (quad.isFlat)
                {
                    TessQuadToFlatTris(in quad, tessdVertices, localIndices, newVertCount);
                    Vertex firstInQuad = quad.v0;
                    Index firstInQuadIndex = quad.v0.index;
                    if (firstInQuadIndex.isRelative)
                        localVertices[firstInQuadIndex.index] = new VertexMeta(quad.v0, quad.coords, quad.heights);
                    else
                        lock (vertices)
                            vertices[firstInQuadIndex.index] = VertexMeta.Merge(
                                vertices[firstInQuadIndex.index],
                                new VertexMeta(quad.v0, quad.coords, quad.heights));
                }
                else
                    TessQuadToTris(in quad, tessdVertices, localIndices, newVertCount);
            }
            localVertices.AddRange(tessdVertices);

            /// Remove duplicates amoung the new Vertices:
            /// The new vertices are all relative indices,
            /// so we only run RemoveDuplicates on a subset of the index list.
            int[] relativeIndices = localIndices.Where(i => i.isRelative).Select(i => i.index).ToArray();
            RemoveDuplicates(localVertices, relativeIndices);
            for (int i = localIndices.Count - 1, rI = relativeIndices.Length - 1; i >= 0; i--)
            {
                if (localIndices[i].isRelative)
                {
                    localIndices[i] = new Index(relativeIndices[rI], true);
                    rI--;
                }
            }
            
            // Integrate vertices and tris then cull duplicate vertices.
            lock (this)
            {
                int vertCount = vertices.Count;
                vertices.AddRange(localVertices);
                indices.AddRange(localIndices.Select(i => i.AsAbsolute(vertCount).index));
                lock (vertices)
                    RemoveDuplicates(vertices, indices);
            }
        }

        private static void TessQuadToTris(in Quad quad, List<VertexMeta> outputVertices, List<Index> indices, int indexOffset = 0)
        {
            VertexMeta center = new VertexMeta((quad[0].position + quad[1].position + quad[2].position + quad[3].position) / 4, quad.coords, quad.heights);
            Index centerIndex = new Index(outputVertices.Count + indexOffset, true);
            indices.Add(centerIndex);
            indices.Add(quad[0].index);
            indices.Add(quad[1].index);

            indices.Add(centerIndex);
            indices.Add(quad[1].index);
            indices.Add(quad[2].index);

            indices.Add(centerIndex);
            indices.Add(quad[2].index);
            indices.Add(quad[3].index);

            indices.Add(centerIndex);
            indices.Add(quad[3].index);
            indices.Add(quad[0].index);
            outputVertices.Add(center);
        }
        private static void TessQuadToFlatTris(in Quad quad, List<VertexMeta> outputVertices, List<Index> indices, int indexOffset = 0)
        {
            Index dupeIndex = new Index(outputVertices.Count + indexOffset, true);
            indices.Add(quad[0].index);
            indices.Add(quad[1].index);
            indices.Add(quad[2].index);

            VertexMeta dupe = new VertexMeta(quad[2].position, quad.coords, quad.heights, true);
            indices.Add(dupeIndex);
            indices.Add(quad[3].index);
            indices.Add(quad[0].index);
            outputVertices.Add(dupe);
        }

        public readonly struct Quad
        {
            public readonly float zDegeneracy;
            public readonly Vertex v0, v1, v2, v3;
            public readonly bool isFlat;
            public readonly Vector4 coords;
            public readonly Vector4 heights;
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
                }
            }
            public Quad(IList<Vector3> vertices, int i0, int i1, int i2, int i3)
                : this(
                      new Vertex(vertices[i0], new Index(i0, false)),
                      new Vertex(vertices[i1], new Index(i1, false)),
                      new Vertex(vertices[i2], new Index(i2, false)),
                      new Vertex(vertices[i3], new Index(i3, false))
                      )
            { }

            public Quad(Vertex v0, Vertex v1, Vertex v2, Vertex v3)
            {
                this.v0 = v0;
                this.v1 = v1;
                this.v2 = v2;
                this.v3 = v3;
                zDegeneracy = Mathf.Abs(v0.z + v2.z - (v1.z + v3.z)) / 4;
                isFlat = zDegeneracy == 0;

                // Note that this assumes rectangular quads. Any skew will mess with this,
                // but dealing with that would require another Vector4 of data per vertex.
                coords = new Vector4(
                    v0.x, v0.y,
                    v3.x - v0.x,
                    v1.y - v0.y);
                heights = new Vector4(v0.z, v3.z, v1.z, v2.z);
            }
            private Quad(Vertex v0, Vertex v1, Vertex v2, Vertex v3, Vector4 coords, Vector4 heights)
            {
                this.v0 = v0;
                this.v1 = v1;
                this.v2 = v2;
                this.v3 = v3;
                zDegeneracy = Mathf.Abs(v0.z + v2.z - (v1.z + v3.z)) / 4;
                isFlat = zDegeneracy == 0;
                this.coords = coords;
                this.heights = heights;
            }

            public IEnumerable<Quad> Subdivide(List<Vertex> newVertices)
            {
                int localOffset = newVertices.Count;
                Vertex nv0 = new Vertex((v0.position + v1.position + v2.position + v3.position) / 4, new Index(localOffset, true));
                Vertex nv1 = new Vertex((v0.position + v1.position) / 2, new Index(localOffset + 1, true));
                Vertex nv2 = new Vertex((v1.position + v2.position) / 2, new Index(localOffset + 2, true));
                Vertex nv3 = new Vertex((v2.position + v3.position) / 2, new Index(localOffset + 3, true));
                Vertex nv4 = new Vertex((v3.position + v0.position) / 2, new Index(localOffset + 4, true));

                newVertices.Add(nv0);
                newVertices.Add(nv1);
                newVertices.Add(nv2);
                newVertices.Add(nv3);
                newVertices.Add(nv4);

                yield return new Quad(v0, nv1, nv0, nv4, coords, heights);
                yield return new Quad(nv1, v1, nv2, nv0, coords, heights);
                yield return new Quad(nv0, nv2, v2, nv3, coords, heights);
                yield return new Quad(nv4, nv0, nv3, v3, coords, heights);
            }

            public IEnumerable<Vertex> AsVertexSequence()
            {
                yield return v0;
                yield return v1;
                yield return v2;
                yield return v3;
            }
            public IEnumerable<VertexMeta> AsVertexMetaSequence()
            {
                yield return new VertexMeta(v0, coords, heights);
                yield return new VertexMeta(v1, coords, heights);
                yield return new VertexMeta(v2, coords, heights);
                yield return new VertexMeta(v3, coords, heights);
            }
        }

        public readonly struct VertexMeta : IEquatable<VertexMeta>
        {
            public readonly Vector3 position;
            public readonly Vector4 coords;
            public readonly Vector4 heights;
            public readonly bool validVertexData;
            public readonly bool prohibitMerge;
            public VertexMeta(Vertex vertex, bool prohibitMerge = false) : this(vertex.position, prohibitMerge) { }
            public VertexMeta(Vector3 position, bool prohibitMerge = false)
            {
                coords = Vector4.zero;
                heights = Vector4.zero;
                validVertexData = false;
                this.position = position;
                this.prohibitMerge = prohibitMerge;
                validVertexData = false;
            }
            public VertexMeta(Vertex vertex, Vector4 coords, Vector4 heights, bool prohibitMerge = false)
                : this(vertex, prohibitMerge)
            {
                this.coords = coords;
                this.heights = heights;
                validVertexData = true;
            }
            public VertexMeta(Vector3 position, Vector4 coords, Vector4 heights, bool prohibitMerge = false)
                : this(position, prohibitMerge)
            {
                this.coords = coords;
                this.heights = heights;
                validVertexData = true;
            }

            public static VertexMeta Merge(VertexMeta a, VertexMeta b)
            {
                if (!a.CanMergeWith(b))
                    throw new InvalidOperationException("The supplied vertices are ineligible to merge.");

                if (b.validVertexData)
                    return b;
                else
                    return a;
            }

            public bool CanMergeWith(VertexMeta other)
                => Equals(other);

            public bool Equals(VertexMeta other)
            {
                if (prohibitMerge || other.prohibitMerge)
                {
                    return (prohibitMerge && other.prohibitMerge) &&
                        (validVertexData == other.validVertexData) &&
                        (position == other.position) &&
                        (coords == other.coords) &&
                        (heights == other.heights);
                }
                else if (validVertexData && other.validVertexData)
                {
                    return (position == other.position) &&
                        (coords == other.coords) &&
                        (heights == other.heights);
                }
                else
                {
                    return position == other.position;
                }
            }

            public override int GetHashCode()
                => position.GetHashCode();
        }

        public readonly struct Vertex
        {
            public readonly Vector3 position;
            public readonly Index index;
#pragma warning disable IDE1006 // Naming Styles
            public float x { get => position.x; }
            public float y { get => position.y; }
            public float z { get => position.z; }
#pragma warning restore IDE1006 // Naming Styles

            public Vertex(Vector3 position, Index index)
            {
                this.position = position;
                this.index = index;
            }
        }

        public readonly struct Index
        {
            public readonly int index;
            public readonly bool isRelative;
            public Index(int index, bool isRelative)
            {
                this.index = index;
                this.isRelative = isRelative;
            }
            public Index AsAbsolute(int offset) => new Index(isRelative ? index + offset : index, false);
            public Index AsRelative(int offset) => new Index(isRelative ? index - offset : index, true);

            public static explicit operator int(Index index) => index.index;
            public static Index operator +(Index index, int offset) => new Index(index.index + offset, index.isRelative);
            public static Index operator -(Index index, int offset) => new Index(index.index - offset, index.isRelative);
        }

        private class LocalData
        {
            public readonly float scale;
            public readonly float invScale;
            public readonly float tolerance;
            public readonly List<Quad> quads = new List<Quad>();
            public readonly List<Vertex> newVertices = new List<Vertex>();

            public LocalData(float scale, float tolerance)
            {
                this.scale = scale;
                invScale = 1 / scale;
                this.tolerance = tolerance;
            }
        }
    }
}
