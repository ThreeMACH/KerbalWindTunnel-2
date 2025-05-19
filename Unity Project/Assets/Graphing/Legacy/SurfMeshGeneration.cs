using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegacyGraphing.Extensions
{
    public class EnhancedSubMesh
    {
        public struct Face
        {
            public MeshTopology topology;
            public int numVertices;
            public int[] vertices;
            public Face?[] neighbours;
            public Edge[] edges;
            public int[] edgeIndicies;

            public Face(int[] vertices)
            {
                numVertices = vertices.Length;
                if (numVertices < 3 || numVertices > 4)
                    throw new ArgumentException("Faces are undefined for other than quads or triangles.");
                topology = numVertices == 3 ? MeshTopology.Triangles : MeshTopology.Quads;
                this.vertices = vertices;
                neighbours = new Face?[numVertices];
                edges = new Edge[numVertices];
                edgeIndicies = new int[numVertices];
            }
            public Face(int[] vertices, Edge[] edges, int[] edgeIndicies) : this(vertices)
            {
                for (int i = 0; i < numVertices; i++)
                {
                    this.edges[i] = edges[i];
                    this.edgeIndicies[i] = edgeIndicies[i];
                    neighbours[i] = edges[i].OtherFace(this);
                }
            }

            public void SetEdges(Edge[] edges, int[] edgeIndicies)
            {
                for (int i = 0; i < numVertices; i++)
                {
                    this.edges[i] = edges[i];
                    this.edgeIndicies[i] = edgeIndicies[i];
                }
            }
            public void SetEdges(int[] edgeIndicies, List<Edge> edgeList)
            {
                for (int i = 0; i < numVertices; i++)
                {
                    this.edgeIndicies[i] = edgeIndicies[i];
                    this.edges[i] = edgeList[edgeIndicies[i]];
                }
            }
            public void SetEdges(int[] edgeIndicies, Edge[] edgeList)
            {
                for (int i = 0; i < numVertices; i++)
                {
                    this.edgeIndicies[i] = edgeIndicies[i];
                    this.edges[i] = edgeList[edgeIndicies[i]];
                }
            }

            public void UpdateNeighbours()
            {
                for (int i = 0; i < numVertices; i++)
                {
                    if (edges[i].face1 != this)
                    {
                        neighbours[i] = edges[i].face1;
                        edges[i].face2 = this;
                    }
                    else
                        neighbours[i] = edges[i].face2;
                }
            }

            public static bool operator ==(Face face1, Face face2)
                => face1.vertices == face2.vertices;
            public override bool Equals(object obj)
            {
                if (!(obj is Face))
                    return false;
                return this == (Face)obj;
            }
            public override int GetHashCode()
                => base.GetHashCode();
            public static bool operator !=(Face face1, Face face2)
                => face1.vertices != face2.vertices;
        }
        public struct Edge
        {
            public int vertex1;
            public int vertex2;
            public Face face1;
            public Face? face2;
            public Edge(int vert1, int vert2, Face face1)
            {
                bool swap = vert2 < vert1;
                vertex1 = swap ? vert2 : vert1;
                vertex2 = swap ? vert1 : vert2;
                this.face1 = face1;
                face2 = null;
            }
            public Edge(int vert1, int vert2, Face face1, Face face2) : this(vert1, vert2, face1)
            {
                this.face2 = face2;
            }
            public void UpdateFirstFace(Face face) => face1 = face;
            public void SetOtherFace(Face face) => face2 = face;
            public Face? OtherFace(Face face)
            {
                if (face == face1)
                    return face2;
                if (face == face2)
                    return face1;
                return null;
            }
            public bool Contains(int vertex) =>
                vertex1 == vertex || vertex2 == vertex;
            public override int GetHashCode()
            => vertex1.GetHashCode();
            public override bool Equals(object obj)
            {
                if (obj is Edge edge)
                    return edge.vertex1 == vertex1 && edge.vertex2 == vertex2;
                return false;
            }
        }

        public readonly Mesh mesh;
        public readonly MeshTopology meshTopology;
        public readonly List<Face> faces = new List<Face>();
        public readonly List<Edge> edges = new List<Edge>();
        public readonly List<Vector3> vertices;
        public readonly List<Color> colors;
        public readonly int submesh;

        public EnhancedSubMesh(Mesh mesh, int submesh = 0)
        {
            this.mesh = mesh;
            this.submesh = submesh;
            meshTopology = mesh.GetTopology(submesh);
            vertices = new List<Vector3>(mesh.vertices);
            colors = new List<Color>(mesh.colors);
            int faceStep;
            switch (meshTopology)
            {
                default:
                    throw new ArgumentException("mesh is invalid topology.");
                case MeshTopology.Quads:
                    faceStep = 4;
                    break;
                case MeshTopology.Triangles:
                    faceStep = 3;
                    break;
            }
            int[] indicies = mesh.GetIndices(submesh);
            Edge[] tempEdges = new Edge[faceStep];
            int[] tempEdgeIndicies = new int[faceStep];
            for (int i = 0; i < indicies.Length; i += faceStep)
            {
                int[] faceVertices = new int[faceStep];
                for (int v = 0; v < faceStep; v++)
                    faceVertices[v] = indicies[i + v];
                Face face = new Face(faceVertices);
                for (int v = 0; v < faceStep; v++)
                {
                    tempEdges[v] = new Edge(indicies[i + v], indicies[i + (v + 1) % faceStep], face);
                    if (edges.Contains(tempEdges[v]))
                    {
                        tempEdgeIndicies[v] = edges.IndexOf(tempEdges[v]);
                    }
                    else
                    {
                        tempEdgeIndicies[v] = edges.Count;
                        edges.Add(tempEdges[v]);
                    }
                }
                face.SetEdges(tempEdges, tempEdgeIndicies);
                faces.Add(face);
            }
            for (int i = faces.Count - 1; i >= 0; i--)
                faces[i].UpdateNeighbours();
        }
        public Mesh UpdateMesh()
        {
            int faceStep;
            switch (meshTopology)
            {
                default:
                    throw new ArgumentException("mesh is invalid topology.");
                case MeshTopology.Quads:
                    faceStep = 4;
                    break;
                case MeshTopology.Triangles:
                    faceStep = 3;
                    break;
            }
            mesh.vertices = vertices.ToArray();
            mesh.colors = colors.ToArray();
            int[] indices = new int[faces.Count * faceStep];
            for (int f = 0; f < faces.Count; f++)
            {
                for (int i = 0; i < faceStep; i++)
                    indices[f * faceStep + i] = faces[f].vertices[i];
            }
            mesh.SetIndices(indices, meshTopology, submesh);
            return mesh;
        }
#if DEBUG
        public void DebugDump()
        {
            string s = string.Format("Vertices:\n");
            foreach (var v in vertices)
                s += string.Format("{0}\n", ((Vector2)v).ToString());
            s += string.Format("\nIndices:\n");
            foreach(var f in faces)
            {
                for (int i = 0; i < f.numVertices; i++)
                    s += string.Format("{0}\n", f.vertices[i]);
            }
            Debug.Log(s);
        }
#endif
        public void SubdivideQuadMesh(int level = 1)
        {
            if (meshTopology != MeshTopology.Quads)
            {
                Debug.LogError("Mesh is not made up of quads.");
                return;
            }

            for (int i = 0; i < level; i++)
                SubdivideQuadMesh_Internal();
        }

        private void SubdivideQuadMesh_Internal()
        {
            int edgeCount = edges.Count;
            int vertexCount = vertices.Count;
            bool lerpColors = colors.Count == vertexCount;
            for (int e = 0; e < edgeCount; e++)
            {
                vertices.Add((vertices[edges[e].vertex1] + vertices[edges[e].vertex2]) * 0.5f);     // vertexCount + e
                if (lerpColors)
                    colors.Add(colors[edges[e].vertex1] * 0.5f + colors[edges[e].vertex2] * 0.5f);  // vertexCount + e
                edges.Add(new Edge(vertexCount + e, edges[e].vertex2, edges[e].face1));             // edgeCount + e
                edges[e] = new Edge(edges[e].vertex1, vertexCount + e, edges[e].face1);
            }
            // vertices now has vertexCount + edgeCount elements
            // edges now has 2 * edgeCount elements

            int faceCount = faces.Count;
            int[] tempEdgeIndices = new int[4];
            Face[] subFaces = new Face[4];
            for (int f = 0; f < faceCount; f++)
            {
                Face face = faces[f];

                vertices.Add(
                    (vertices[face.vertices[0]] +
                    vertices[face.vertices[1]] +
                    vertices[face.vertices[2]] +
                    vertices[face.vertices[3]]) * 0.25f);   // vertexCount + edgeCount + f

                for (int i = 0; i < 4; i++)
                {
                    // The face.edgeIndicies[i] references into the fact that the split edge vertices are in the same order as the original edges.
                    subFaces[i] = new Face(new int[] { face.vertices[i], vertexCount + face.edgeIndicies[i], vertexCount + edgeCount + f, vertexCount + face.edgeIndicies[(i + 3) % 4] });
                    edges.Add(new Edge(vertexCount + face.edgeIndicies[i], vertexCount + edgeCount + f, subFaces[i]));  // 2 * edgeCount + 4 * f + i
                }
                for (int i = 0; i < 4; i++)
                {
                    tempEdgeIndices[0] = edges[face.edgeIndicies[i]].Contains(face.vertices[i]) ? face.edgeIndicies[i] : edgeCount + face.edgeIndicies[i];
                    tempEdgeIndices[1] = 2 * edgeCount + 4 * f + i;
                    tempEdgeIndices[2] = 2 * edgeCount + 4 * f + (i + 3) % 4;
                    tempEdgeIndices[3] = edges[face.edgeIndicies[(i + 3) % 4]].Contains(face.vertices[i]) ? face.edgeIndicies[(i + 3) % 4] : edgeCount + face.edgeIndicies[(i + 3) % 4];
                    if (edges[tempEdgeIndices[0]].face1 == face)
                        edges[tempEdgeIndices[0]].UpdateFirstFace(subFaces[i]);
                    if (edges[tempEdgeIndices[3]].face1 == face)
                        edges[tempEdgeIndices[3]].UpdateFirstFace(subFaces[i]);
                    subFaces[i].SetEdges(tempEdgeIndices, edges);
                    if (i == 0)
                        faces[f] = subFaces[i];
                    else
                        faces.Add(subFaces[i]);
                }
            }
            for (int f = 0; f < faces.Count; f++)
                faces[f].UpdateNeighbours();
        }
    }

    public static class MeshExtensions
    {
        public static void QuadToTris4(this Mesh mesh, int submesh = 0)
        {
            if (mesh.GetTopology(submesh) != MeshTopology.Quads)
            {
                Debug.LogError("Mesh is not made up of quads.");
                return;
            }

            Vector3[] vertices = mesh.vertices;

            int[] quadIndices = mesh.GetIndices(submesh);
            int[] triIndices = new int[quadIndices.Length * 3];
            Vector3[] newVertices = new Vector3[vertices.Length + quadIndices.Length / 4];
            for (int i = vertices.Length - 1; i >= 0; i--)
                newVertices[i] = vertices[i];

            int newVertexOffset = vertices.Length - 1;
            for (int quadIndex = 0; quadIndex < quadIndices.Length; quadIndex += 4)
            {
                newVertexOffset++;
                int triIndex = quadIndex * 3;
                newVertices[newVertexOffset] = (
                    vertices[quadIndices[quadIndex]] +
                    vertices[quadIndices[quadIndex + 1]] +
                    vertices[quadIndices[quadIndex + 2]] +
                    vertices[quadIndices[quadIndex + 3]]) * 0.25f;

                triIndices[triIndex] = quadIndices[quadIndex];
                triIndices[triIndex + 1] = quadIndices[quadIndex + 1];
                triIndices[triIndex + 2] = newVertexOffset;
                triIndices[triIndex + 3] = quadIndices[quadIndex + 1];
                triIndices[triIndex + 4] = quadIndices[quadIndex + 2];
                triIndices[triIndex + 5] = newVertexOffset;
                triIndices[triIndex + 6] = quadIndices[quadIndex + 2];
                triIndices[triIndex + 7] = quadIndices[quadIndex + 3];
                triIndices[triIndex + 8] = newVertexOffset;
                triIndices[triIndex + 9] = quadIndices[quadIndex + 3];
                triIndices[triIndex + 10] = quadIndices[quadIndex];
                triIndices[triIndex + 11] = newVertexOffset;
            }

            mesh.SetVertices(newVertices);
            mesh.SetIndices(triIndices, MeshTopology.Triangles, submesh);
        }

        public static void QuadToTris(this Mesh mesh, int submesh = 0)
        {
            if (mesh.GetTopology(submesh) != MeshTopology.Quads)
            {
                Debug.LogError("Mesh is not made up of quads.");
                return;
            }

            Vector3[] vertices = mesh.vertices;

            int[] quadIndices = mesh.GetIndices(submesh);
            int[] triIndices = new int[quadIndices.Length * 6 / 4];

            for (int quadIndex = 0; quadIndex < quadIndices.Length; quadIndex += 4)
            {
                int triIndex = quadIndex * 6 / 4;
                float intermediateValue = (vertices[quadIndices[quadIndex]].z + vertices[quadIndices[quadIndex + 1]].z) * 0.5f;
                bool case1 = Mathf.Abs(vertices[quadIndices[quadIndex + 2]].z - intermediateValue) < Mathf.Abs(vertices[quadIndices[quadIndex + 3]].z - intermediateValue);

                // First triangle of the quad
                triIndices[triIndex] = quadIndices[quadIndex];
                triIndices[triIndex + 1] = quadIndices[quadIndex + 1];
                triIndices[triIndex + 2] = case1 ? quadIndices[quadIndex + 2] : quadIndices[quadIndex + 3];
                // Second triangle of the quad
                triIndices[triIndex + 3] = quadIndices[quadIndex + 2];
                triIndices[triIndex + 4] = quadIndices[quadIndex + 3];
                triIndices[triIndex + 5] = case1 ? quadIndices[quadIndex] : quadIndices[quadIndex + 1];
            }

            mesh.SetIndices(triIndices, MeshTopology.Triangles, submesh);
        }
    }
    public static class SurfMeshGeneration
    {
        public static void ConstructQuadSurfMesh(float[,] values, float xMin, float xMax, float yMin, float yMax, Mesh mesh, bool invertZ = true)
        {
            float[] xValues = new float[values.GetUpperBound(0) + 1];
            float[] yValues = new float[values.GetUpperBound(1) + 1];
            float xStep = (xMax - xMin) / (xValues.Length - 1);
            float yStep = (yMax - yMin) / (yValues.Length - 1);
            for (int i = xValues.Length - 1; i >= 0; i--)
                xValues[i] = xMin + i * xStep;
            for (int j = yValues.Length - 1; j >= 0; j--)
                yValues[j] = yMin + j * yStep;

            ConstructQuadSurfMesh(values, xValues, yValues, mesh, invertZ);
        }

        public static void ConstructQuadSurfMesh(float[,] values, float[] xValues, float[] yValues, Mesh mesh, bool invertZ = true)
        {
            int iMax = values.GetUpperBound(0) + 1;
            int jMax = values.GetUpperBound(1) + 1;
            if (xValues.Length != iMax)
                throw new ArgumentException("Position locations must have the same number of elements as values.", nameof(xValues));
            if (yValues.Length != jMax)
                throw new ArgumentException("Position locations must have the same number of elements as values.", nameof(yValues));

            if (iMax < 1 || jMax < 1)
            {
                mesh.Clear();
                return;
            }

            Vector3[] vertices = new Vector3[values.Length];
            int[] quadIndices = new int[(iMax - 1) * (jMax - 1) * 4];

            for (int j = 0; j < jMax; j++)
            {
                float yValue = yValues[j];
                for (int i = 0; i < iMax; i++)
                {
                    vertices[i + j * iMax] = new Vector3(xValues[i], yValue, values[i, j] * (invertZ ? -1 : 1));
                    if (i > 0 && j > 0)
                    {
                        int quadIndex = ((i - 1) + (j - 1) * (iMax - 1)) * 4;
                        // Quad:
                        quadIndices[quadIndex] = (i - 1) + (j - 1) * iMax;
                        quadIndices[quadIndex + 1] = (i - 1) + j * iMax;
                        quadIndices[quadIndex + 2] = i + j * iMax;
                        quadIndices[quadIndex + 3] = i + (j - 1) * iMax;
                    }
                }
            }

            mesh.Clear();

            if (values.Length > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            else
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetIndices(quadIndices, MeshTopology.Quads, 0);
        }
    }
}