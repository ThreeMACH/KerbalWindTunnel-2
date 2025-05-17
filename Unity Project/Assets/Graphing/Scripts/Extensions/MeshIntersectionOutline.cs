using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Graphing.Extensions
{
    public static class MeshIntersectionOutline
    {
        public static List<Vector3[]> GetMeshIntersectionOutline(Mesh mesh, Func<Vector3, float> rootFunc)
        {
            Vector3[] vertices = mesh.vertices;
            int[] tris = mesh.triangles;
            List<Vector3> currentLine = new List<Vector3>();
            bool currentLineTerminated = false;
            List<Vector3[]> result = new List<Vector3[]>();
            bool[] checkedTri = new bool[tris.Length / 3];

            void ProcessTri(int triIndex, int inPt1 = -1, int inPt2 = -1)
            {
                if (checkedTri[triIndex])
                    return;

                int inPt1Index = -1, inPt2Index = -1;

                if (inPt1 >= 0 && inPt2 == -1)
                    throw new ArgumentException("Cannot only supply a singe point.", "inPt2");

                if (inPt1 >= 0 && inPt2 >= 0)
                {
                    if (inPt1 == inPt2)
                        throw new ArgumentException("The supplied points are identical.", "inPt1, inPt2");

                    for (int i = 0; i < 3; i++)
                    {
                        if (inPt1 == tris[triIndex * 3 + i])
                            inPt1Index = i;
                        if (inPt2 == tris[triIndex * 3 + i])
                            inPt2Index = i;
                    }
                    if (inPt1Index < 0)
                        throw new ArgumentException("The supplied point is not in the supplied triangle.", "inPt1");
                    if (inPt2Index < 0)
                        throw new ArgumentException("The supplied point is not in the supplied triangle.", "inPt2");

                    if (inPt1Index > inPt2Index)
                    {
                        (inPt1Index, inPt2Index) = (inPt2Index, inPt1Index);
                        (inPt1, inPt2) = (inPt2, inPt1);
                    }
                }

                checkedTri[triIndex] = true;

                float root0 = rootFunc(vertices[tris[triIndex * 3]]);
                float root1 = rootFunc(vertices[tris[triIndex * 3 + 1]]);
                float root2 = rootFunc(vertices[tris[triIndex * 3 + 2]]);
                bool predicate0 = root0 < 0;
                bool predicate1 = root1 < 0;
                bool predicate2 = root2 < 0;

                // COULDDO: Special cases where one or more are actually 0

                // If all are above or below the filter, the filter doesn't cut through any lines on this triangle.
                if (predicate0 == predicate1 && predicate0 == predicate2)
                    return;

                int nextTri;
                // No incoming line, so we start a new line.
                if (inPt1 < 0)
                {
                    if (currentLine.Count > 0)
                        result.Add(currentLine.ToArray());
                    currentLine.Clear();
                    currentLineTerminated = false;
                    inPt1 = tris[triIndex * 3]; inPt1Index = 0;
                    if (predicate0 == predicate1) { inPt2 = tris[triIndex * 3 + 2]; inPt2Index = 2; }
                    else { inPt2 = tris[triIndex * 3 + 1]; inPt2Index = 1; }
                    currentLine.Add(GetLineIntersectPoint(vertices[inPt1], vertices[inPt2], rootFunc));
                    nextTri = GetOtherTriOnLine(mesh, inPt1, inPt2, triIndex);
                    // If there's no other face opposite this one, that means it's the edge of the mesh.
                    if (nextTri == -1)
                        currentLineTerminated = true;
                    else
                        ProcessTri(nextTri, inPt1, inPt2);
                }

                // Common vertex:
                //                                                                                D   B                                A   F                                 E   C
                int outP1Index = inPt1Index == 0 ? (inPt2Index == 1 ? (predicate1 == predicate2 ? 0 : 1) : (predicate1 == predicate2 ? 0 : 2)) : (predicate1 == predicate0 ? 2 : 1);

                // Opposite vertex:
                //                                                   BD  AF   CE
                int outP2Index = inPt1Index == 0 ? (inPt2Index == 1 ? 2 : 1) : 0;

                int outP1 = tris[triIndex * 3 + outP1Index];
                int outP2 = tris[triIndex * 3 + outP2Index];

                Vector3 intersectPoint = GetLineIntersectPoint(vertices[outP1], vertices[outP2], rootFunc);
                int lineVertCount = currentLine.Count;
                if (lineVertCount >= 2)
                {
                    Vector3 lastPointing = currentLine[lineVertCount - 1] - currentLine[lineVertCount - 2];
                    Vector3 currentPointing = intersectPoint - currentLine[lineVertCount - 1];
                    float dot = Vector3.Dot(lastPointing, currentPointing);
                    dot *= dot;
                    float margin = Mathf.Abs(dot - lastPointing.sqrMagnitude * currentPointing.sqrMagnitude) / dot;
                    if (margin < 0.00005f)
                        currentLine[lineVertCount - 1] = intersectPoint;
                    else
                        currentLine.Add(intersectPoint);
                }
                else
                    currentLine.Add(intersectPoint);
                nextTri = GetOtherTriOnLine(mesh, outP1, outP2, triIndex);
                // If there's no other face opposite this one, that means it's the edge of the mesh.
                if (nextTri == -1)
                {
                    // Make it so that every point on the perimeter of the mesh is either the start or end of a line.
                    if (!currentLineTerminated)
                    {
                        currentLine.Reverse();
                        currentLineTerminated = true;
                    }
                    else
                    {
                        currentLineTerminated = false;
                    }
                    return;
                }
                ProcessTri(nextTri, outP1, outP2);
            }

            for (int i = (tris.Length/3) - 1; i >= 0; i--)
            {
                if (checkedTri[i])
                    continue;
                ProcessTri(i);
            }
            if (currentLine.Count > 0)
                result.Add(currentLine.ToArray());
            return result;
        }
        private static Vector3 GetLineIntersectPoint(Vector3 point1, Vector3 point2, Func<Vector3, float> rootFunc)
        {
            float r1 = rootFunc(point1);
            float r2 = rootFunc(point2);
            float f = -r1 / (r2 - r1);
            return Vector3.Lerp(point1, point2, f);
        }
        public static int[] GetTrisOnLine(Mesh mesh, int pt1, int pt2)
        {
            int[] tris = mesh.triangles;
            int triCount = tris.Length;
            int i0 = -1, i1 = -1;
            for (int i = 0; i < triCount; i += 3)
            {
                if ((tris[i] == pt1 && (tris[i + 1] == pt2 || tris[i + 2] == pt2)) ||
                    (tris[i + 1] == pt1 && (tris[i] == pt2 || tris[i + 2] == pt2)) ||
                    (tris[i + 2] == pt1 && (tris[i] == pt2 || tris[i + 1] == pt2)))
                {
                    if (i0 == -1)
                        i0 = i / 3;
                    else
                    {
                        i1 = i / 3;
                        break;
                    }
                }
            }
            if (i0 == -1)
                return new int[0];
            if (i1 == -1)
                return new int[] { i0 };
            return new int[] { i0, i1 };
        }
        public static int GetOtherTriOnLine(Mesh mesh, int pt1, int pt2, int currentTri)
        {
            int[] tris = mesh.triangles;
            int triCount = tris.Length;
            for (int i = 0; i < triCount; i += 3)
            {
                if ((tris[i] == pt1 && (tris[i + 1] == pt2 || tris[i + 2] == pt2)) ||
                    (tris[i + 1] == pt1 && (tris[i] == pt2 || tris[i + 2] == pt2)) ||
                    (tris[i + 2] == pt1 && (tris[i] == pt2 || tris[i + 1] == pt2)))
                {
                    if (i / 3 == currentTri)
                        continue;
                    return i / 3;
                }
            }
            return -1;
        }
    }
}