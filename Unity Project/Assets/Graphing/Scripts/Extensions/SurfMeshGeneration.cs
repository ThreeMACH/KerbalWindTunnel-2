using System;
using UnityEngine;

namespace Graphing.Meshing
{
    public static class SurfMeshGeneration
    {
        public static void ConstructQuadSurfMesh(float[,] values, float xMin, float xMax, float yMin, float yMax, Mesh mesh, bool invertZ = false)
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

        public static void ConstructQuadSurfMesh(float[,] values, float[] xValues, float[] yValues, Mesh mesh, bool invertZ = false)
        {
            int iMax = values.GetUpperBound(0) + 1;
            int jMax = values.GetUpperBound(1) + 1;
            if (xValues.Length != iMax)
                throw new ArgumentException("Position locations must have the same number of elements as values.", "xValues");
            if (yValues.Length != jMax)
                throw new ArgumentException("Position locations must have the same number of elements as values.", "yValues");

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
                    vertices[i + j * iMax] = new Vector3(-xValues[i], yValue, values[i, j] * (invertZ ? -1 : 1));
                    if (i > 0 && j > 0)
                    {
                        int quadIndex = ((i - 1) + (j - 1) * (iMax - 1)) * 4;
                        // Quad:
                        quadIndices[quadIndex] = (i - 1) + (j - 1) * iMax;
                        quadIndices[quadIndex + 3] = (i - 1) + j * iMax;
                        quadIndices[quadIndex + 2] = i + j * iMax;
                        quadIndices[quadIndex + 1] = i + (j - 1) * iMax;
                    }
                }
            }

            mesh.Clear();

            if (values.Length > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            else
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetIndices(quadIndices, MeshTopology.Quads, 0);
        }
    }
}