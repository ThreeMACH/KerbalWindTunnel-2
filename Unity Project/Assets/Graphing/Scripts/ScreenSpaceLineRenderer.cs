using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphing
{
    [ExecuteInEditMode]
    public class ScreenSpaceLineRenderer : MonoBehaviour, IDisposable
    {
        [SerializeField]
        private Vector3[] points;
        [SerializeField]
        private Color[] colors;
        [SerializeField]
        [Range(0, 100)]
        private float width = 5;
        [SerializeField]
        public Material material;

        private NativeArray<Vert> screenSpacePoints;    // [n]
        private NativeArray<Vert> vertices;             // [(n - 1) * 2 * (c + 2)]
        private NativeArray<uint> indices;              // [(n - 1) * (2 * c + 4) * 3]
        private NativeArray<int> numVerts;              // [n - 1]
        private NativeArray<int> numIndices;            // [n - 1]

        /*private readonly List<Vert> screenSpacePositions = new List<Vert>();
        private readonly List<Vert> vertList = new List<Vert>();
        private readonly List<Vector3> vertPositions = new List<Vector3>();
        private readonly List<Color> vertColors = new List<Color>();
        private readonly List<int> indxList = new List<int>();*/

        private Dictionary<Camera, MeshCache> meshCache = new Dictionary<Camera, MeshCache>();

        // Constants for number of segments
        [SerializeField]
        [Range(3, 24)]
        private int capSegments = 12;
        [SerializeField]
        [Range(3, 24)]
        private int maxElbowSegments = 12;

        private int maxSegments = 12;

        private const float invPi = 1 / Mathf.PI;

        public IList<Vector3> Points
        {
            get => points.ToList().AsReadOnly();
            set
            {
                int oldLength = points?.Length ?? 0;
                points = value.ToArray();
                ValidateColors();
                MarkCacheDirty();
                if (oldLength != points.Length)
                    SetupNativeArrays();
            }
        }
        public IList<Color> Colors
        {
            get => colors.ToList().AsReadOnly();
            set
            {
                colors = value.ToArray();
                ValidateColors();
                MarkCacheDirty();
            }
        }
        public float Width
        {
            get => width;
            set
            {
                width = Math.Max(value, 0);
                MarkCacheDirty();
            }
        }
        public int CapSegments
        {
            get => capSegments;
            set
            {
                capSegments = Math.Max(value, 3);
                int oldSegments = maxSegments;
                maxSegments = Math.Max(capSegments, maxElbowSegments);
                MarkCacheDirty();
                if (oldSegments != maxSegments)
                    SetupNativeArrays();
            }
        }
        public int ElbowSegments
        {
            get => maxElbowSegments;
            set
            {
                maxElbowSegments = Math.Max(value, 3);
                int oldSegments = maxSegments;
                maxSegments = Math.Max(capSegments, maxElbowSegments);
                MarkCacheDirty();
                if (oldSegments != maxSegments)
                    SetupNativeArrays();
            }
        }

        private void MarkCacheDirty()
        {
            if (meshCache == null)
                return;
            lock (meshCache)
                foreach (var key in meshCache.Keys)
                    meshCache[key].UpdateWVPMatrix(null, Matrix4x4.zero);
        }

        private void SetupMesh(Camera camera)
        {
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0)
                return;
            BillboardMesh(camera);
        }

        private void BillboardMesh(Camera camera)
        {
            MeshCache cachedResult = GetOrAddMeshCache(camera);

            if (cachedResult == null)
                return;

            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

            if (CacheIsValid(localToWorldMatrix, camera, cachedResult))
            {
                Graphics.DrawMesh(cachedResult.mesh, localToWorldMatrix, material, gameObject.layer, camera);
                return;
            }
            cachedResult.UpdateWVPMatrix(camera, localToWorldMatrix);
            Mesh mesh = cachedResult.mesh;

#if UNITY_EDITOR
        float width = this.width;
        if (camera.name.Equals("Preview Camera"))
            width /= 10;
#endif

            mesh.Clear();

            if (points == null || points.Length <= 1)
                return;

            // Convert the line points to screen space
            //screenSpacePositions.Clear();
            for (int i = 0; i < points.Length; i++)
            {
                //screenSpacePositions.Add(new Vert(points[i], colors[i]).ToScreenSpace(localToWorldMatrix, camera));
                screenSpacePoints[i] = new Vert(points[i], colors[i]).ToScreenSpace(localToWorldMatrix, camera);
            }

            // Create the screen space mesh
            /*vertList.Clear();
            indxList.Clear();
            for (int i = points.Length - 2; i >= 0; i--)
            {
                BillboardLineSegment(width, screenSpacePositions[Math.Max(i - 1, 0)], screenSpacePositions[i], screenSpacePositions[i + 1], screenSpacePositions[Math.Min(i + 2, screenSpacePositions.Count - 1)], vertList, indxList, capSegments, maxElbowSegments);
            }*/
            MeshJob meshJob = new MeshJob(screenSpacePoints, vertices, indices, numVerts, numIndices, width, capSegments, maxElbowSegments);
            meshJob.Schedule(points.Length - 1, 32).Complete();

            // Convert the mesh vertices to back to local space
            //for (int i = vertList.Count - 1; i >= 0; i--)
            //vertList[i] = vertList[i].ToLocalSpace(transform.worldToLocalMatrix, camera);
            uint offset = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                int startIndxs = i * (2 * maxSegments + 4) * 3;
                for (int j = 0; j < numIndices[i]; j++)
                {
                    indices[startIndxs + j] += offset;
                    //indxList.Add((int)indices[startIndxs + j]);
                }

                int startVerts = i * 2 * (maxSegments + 2);
                for (int j = 0; j < numVerts[i]; j++)
                {
                    vertices[startVerts + j] = vertices[startVerts + j].ToLocalSpace(transform.worldToLocalMatrix, camera);
                    //vertList.Add(vertices[startVerts + j]);
                }

                offset += (uint)numVerts[i];
            }

            // Break out Vertex data to assign to mesh
            /*vertPositions.Clear();
            vertColors.Clear();
            foreach (Vert vert in vertList)
            {
                vertPositions.Add(vert.position);
                vertColors.Add(vert.color);
            }*/

            // Assign data to the mesh
            //mesh.SetVertices(vertPositions);
            //mesh.SetIndices(indxList, MeshTopology.Triangles, 0);
            //mesh.SetColors(vertColors);
            meshJob.OutputToMesh(mesh);

            Graphics.DrawMesh(mesh, localToWorldMatrix, material, gameObject.layer, camera);
        }

        private static void BillboardLineSegment(float width, Vert p_1, Vert p0, Vert p1, Vert p2, NativeArray<Vert> vertSegment, NativeArray<uint> indxSegment, int capSegments, int maxElbowSegments, out int nVerts, out int nIndices)
        {
            nVerts = 0;
            nIndices = 0;
            // Line segment direction and 2D-normal
            Vector2 dir = ((Vector2)(p1.position - p0.position));
            if (dir.sqrMagnitude < 0.5f)
                return;

            // Flags for whether an end has a cap
            bool cap0 = p_1.position == p0.position;
            bool cap1 = p1.position == p2.position;

            dir = dir.normalized;
            Vector2 norm = new Vector2(-dir.y, dir.x);

            // Deal only in half-width so its a simple offset
            float halfWidth = width / 2;

            // Base index for this segment
            int vertCount = 0;

            // Draw the first end
            if (cap0)
            {
                // Draw a cap
                vertSegment[0] = p0;
                //verts.Add(p0);
                float capAngle = Mathf.PI / capSegments;
                //verts.AddRange(DrawCap(-norm * halfWidth, p0, capSegments, capAngle));
                nVerts = 1;
                var enumerator = DrawCap(-norm * halfWidth, p0, capSegments, capAngle).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    vertSegment[nVerts] = enumerator.Current;
                    nVerts += 1;
                }

                for (int i = 1; i <= capSegments; i++)
                {
                    indxSegment[(i - 1) * 3] = (uint)vertCount;
                    indxSegment[(i - 1) * 3 + 1] = (uint)(vertCount + i);
                    indxSegment[(i - 1) * 3 + 2] = (uint)(vertCount + i + 1);
                    //indices.Add(vertCount);
                    //indices.Add(vertCount + i);
                    //indices.Add(vertCount + i + 1);
                }
                nIndices = capSegments * 3;
            }
            else
            {
                // Draw an elbow at beginning of segment
                int numSegments;

                Vector2 prevDir = ((Vector2)(p0.position - p_1.position)).normalized;

                if (prevDir == dir)
                {
                    vertSegment[0] = p0;
                    vertSegment[1] = Vert.Offset(p0, -norm * halfWidth);
                    vertSegment[2] = Vert.Offset(p0, norm * halfWidth);
                    nVerts = 3;
                }
                else
                {

                    float elbowAngle = Vector2.Angle(prevDir, dir) * Mathf.Deg2Rad;
                    numSegments = Math.Max((int)(elbowAngle * invPi * maxElbowSegments), 1);

                    bool interiorIsUp = Vector2.Dot(norm, prevDir) > 0;

                    vertSegment[0] = p0;
                    //verts.Add(p0);
                    if (interiorIsUp)
                    {
                        // Draw interior corner first
                        vertSegment[1] = InteriorElbowPoint(p0, halfWidth, dir, prevDir);
                        //verts.Add(InteriorElbowPoint(p0, halfWidth, dir, prevDir));

                        // Draw exterior corner
                        var enumerator = DrawElbow(new Vector2(-prevDir.y, prevDir.x) * halfWidth, p0, numSegments, -elbowAngle).GetEnumerator();
                        nVerts = 2;
                        while (enumerator.MoveNext())
                        {
                            vertSegment[nVerts] = enumerator.Current;
                            nVerts += 1;
                        }
                        //verts.AddRange(DrawElbow(new Vector2(-prevDir.y, prevDir.x) * halfWidth, p0, numSegments, -elbowAngle));

                        for (int i = 0; i < numSegments; i++)
                        {
                            indxSegment[i * 3] = (uint)vertCount;
                            indxSegment[i * 3 + 1] = (uint)(vertCount + i + 2);
                            indxSegment[i * 3 + 2] = (uint)(vertCount + i + 3);
                            //indices.Add(vertCount);
                            //indices.Add(vertCount + i + 2);
                            //indices.Add(vertCount + i + 3);
                        }
                        nIndices = numSegments * 3;
                    }
                    else
                    {
                        // Draw exterior corner
                        var enumerator = DrawElbow(-norm * halfWidth, p0, numSegments, -elbowAngle).GetEnumerator();
                        nVerts = 1;
                        while (enumerator.MoveNext())
                        {
                            vertSegment[nVerts] = enumerator.Current;
                            nVerts += 1;
                        }
                        //verts.AddRange(DrawElbow(-norm * halfWidth, p0, numSegments, -elbowAngle));

                        for (int i = 0; i < numSegments; i++)
                        {
                            indxSegment[i * 3] = (uint)vertCount;
                            indxSegment[i * 3 + 1] = (uint)(vertCount + i + 1);
                            indxSegment[i * 3 + 2] = (uint)(vertCount + i + 2);
                            //indices.Add(vertCount);
                            //indices.Add(vertCount + i + 1);
                            //indices.Add(vertCount + i + 2);
                        }
                        nIndices = numSegments * 3;

                        // Draw interior corner last
                        vertSegment[nVerts] = InteriorElbowPoint(p0, halfWidth, dir, prevDir);
                        nVerts += 1;
                        //verts.Add(InteriorElbowPoint(p0, halfWidth, dir, prevDir));
                    }
                }
            }

            // Draw the second end
            int endVertCount = nVerts;// verts.Count;

            if (cap1)
            {
                // Draw an endcap
                vertSegment[nVerts] = p1;
                nVerts += 1;
                //verts.Add(p1);
                float capAngle = Mathf.PI / capSegments;
                var enumerator = DrawCap(norm * halfWidth, p1, capSegments, capAngle).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    vertSegment[nVerts] = enumerator.Current;
                    nVerts += 1;
                }
                //verts.AddRange(DrawCap(norm * halfWidth, p1, capSegments, capAngle));

                for (int i = 1; i <= capSegments; i++)
                {
                    indxSegment[nIndices + (i - 1) * 3] = (uint)endVertCount;
                    indxSegment[nIndices + (i - 1) * 3 + 1] = (uint)(endVertCount + i);
                    indxSegment[nIndices + (i - 1) * 3 + 2] = (uint)(endVertCount + i + 1);
                    //indices.Add(endVertCount);
                    //indices.Add(endVertCount + i);
                    //indices.Add(endVertCount + i + 1);
                }
                nIndices += capSegments * 3;
            }
            else
            {
                // Second elbow gets draw by the next patch.
                // Just creating the vertices for this patch.
                vertSegment[nVerts] = p1;

                Vector2 nextDir = ((Vector2)(p2.position - p1.position)).normalized;
                if (nextDir == dir)
                {
                    vertSegment[nVerts + 1] = Vert.Offset(p1, (Vector3)norm * halfWidth);
                    vertSegment[nVerts + 2] = Vert.Offset(p1, -(Vector3)norm * halfWidth);
                }
                else
                {
                    bool interiorIsUp = Vector2.Dot(norm, nextDir) > 0;
                    //verts.Add(p1);

                    if (interiorIsUp)
                    {
                        vertSegment[nVerts + 1] = InteriorElbowPoint(p1, halfWidth, nextDir, dir);
                        //verts.Add(InteriorElbowPoint(p1, halfWidth, nextDir, dir));
                        vertSegment[nVerts + 2] = Vert.Offset(p1, -(Vector3)norm * halfWidth);
                        //verts.Add(Vert.Offset(p1, -(Vector3)norm * halfWidth));
                    }
                    else
                    {
                        vertSegment[nVerts + 1] = Vert.Offset(p1, (Vector3)norm * halfWidth);
                        //verts.Add(Vert.Offset(p1, (Vector3)norm * halfWidth));
                        vertSegment[nVerts + 2] = InteriorElbowPoint(p1, halfWidth, nextDir, dir);
                        //verts.Add(InteriorElbowPoint(p1, halfWidth, nextDir, dir));
                    }
                }
                nVerts += 3;
            }

            int finalVertCount = nVerts;

            // Draw the primary triangles

            // First endpoints:
            // Center: vertCount
            // First: vertCount + 1
            // Second: endVertCount - 1

            // Second endpoints:
            // Center: endVertCount
            // First: endVertCount + 1
            // Second: finalVertCount - 1

            indxSegment[nIndices + 0] = (uint)vertCount;
            indxSegment[nIndices + 1] = (uint)endVertCount;
            indxSegment[nIndices + 2] = (uint)(vertCount + 1);
            //indices.Add(vertCount);
            //indices.Add(endVertCount);
            //indices.Add(vertCount + 1);

            indxSegment[nIndices + 3] = (uint)endVertCount;
            indxSegment[nIndices + 4] = (uint)(finalVertCount - 1);
            indxSegment[nIndices + 5] = (uint)(vertCount + 1);
            //indices.Add(endVertCount);
            //indices.Add(finalVertCount - 1);
            //indices.Add(vertCount + 1);

            indxSegment[nIndices + 6] = (uint)vertCount;
            indxSegment[nIndices + 7] = (uint)(endVertCount + 1);
            indxSegment[nIndices + 8] = (uint)endVertCount;
            //indices.Add(vertCount);
            //indices.Add(endVertCount + 1);
            //indices.Add(endVertCount);

            indxSegment[nIndices + 9] = (uint)vertCount;
            indxSegment[nIndices + 10] = (uint)(endVertCount - 1);
            indxSegment[nIndices + 11] = (uint)(endVertCount + 1);
            //indices.Add(vertCount);
            //indices.Add(endVertCount - 1);
            //indices.Add(endVertCount + 1);

            nIndices += 12;
        }

        private static IEnumerable<Vert> DrawElbow(Vector2 offset, Vert point, int numSegments, float elbowAngle)
        {
            float cosAngle = Mathf.Cos(elbowAngle / numSegments);
            float sinAngle = Mathf.Sin(elbowAngle / numSegments);

            // TODO: There's a duplicate vertex created in this method.
            yield return Vert.Offset(point, (Vector3)offset);

            for (int i = 1; i <= numSegments; i++)
            {
                offset = new Vector2(offset.x * cosAngle - offset.y * sinAngle, offset.x * sinAngle + offset.y * cosAngle);
                yield return Vert.Offset(point, (Vector3)offset);
            }
        }

        private static Vert InteriorElbowPoint(Vert point, float offset, Vector2 dir, Vector2 prevDir)
        {
            return Vert.Offset(point, (Vector3)(((dir - prevDir) / 2).normalized * (offset / Mathf.Cos(Vector2.Angle(dir, prevDir) * Mathf.Deg2Rad / 2))));
        }

        private static IEnumerable<Vert> DrawCap(Vector2 offset, Vert point, int numSegments, float rotationAngle = 0)
        {
            if (rotationAngle == 0)
                rotationAngle = Mathf.PI / numSegments;

            float cosAngle = Mathf.Cos(rotationAngle);
            float sinAngle = Mathf.Sin(rotationAngle);

            yield return Vert.Offset(point, (Vector3)offset);

            for (int i = 0; i < numSegments; i++)
            {
                offset = new Vector2(offset.x * cosAngle + offset.y * sinAngle, -offset.x * sinAngle + offset.y * cosAngle);
                yield return Vert.Offset(point, (Vector3)offset);
            }
        }

        private void ValidateColors()
        {
            if (colors == null)
                colors = new Color[] { Color.black };
            if (colors.Length < (points?.Length ?? 0))
            {
                Color[] newColors = new Color[points.Length];
                for (int i = 0; i < colors.Length; i++)
                    newColors[i] = colors[i];
                for (int i = colors.Length; i < points.Length; i++)
                    newColors[i] = colors[i % colors.Length];
                colors = newColors;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnEnable()
        {
            ValidateColors();
            MarkCacheDirty();
            SetupNativeArrays();
            Camera.onPreCull += SetupMesh;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDisable()
        {
            DisposeNativeArrays();
            Camera.onPreCull -= SetupMesh;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDestroy()
            => Dispose();

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnValidate()
        {
            maxSegments = Math.Max(capSegments, maxElbowSegments);
            ValidateColors();
            MarkCacheDirty();
            SetupNativeArrays();
        }

        private void SetupNativeArrays()
        {
            if (screenSpacePoints.IsCreated)
                DisposeNativeArrays();
            if (points == null || points.Length <= 1)
                return;
            // screenSpacePoints    [n]
            // vertices             [(n - 1) * 2 * (c + 2)]
            // indices              [(n - 1) * (2 * c + 4) * 3]
            // numVerts             [n - 1]
            // numIndices           [n - 1]

            int pointsLength = points.Length;
            screenSpacePoints = new NativeArray<Vert>(pointsLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            pointsLength -= 1;
            vertices = new NativeArray<Vert>(pointsLength * 2 * (maxSegments + 2), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            indices = new NativeArray<uint>(pointsLength * (2 * maxSegments + 4) * 3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            numVerts = new NativeArray<int>(pointsLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            numIndices = new NativeArray<int>(pointsLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        public void Dispose()
        {
            foreach (var cache in meshCache)
                cache.Value.Dispose();
            meshCache = null;
        }

        private void DisposeNativeArrays()
        {
            if (!screenSpacePoints.IsCreated)
                return;
            screenSpacePoints.Dispose();
            vertices.Dispose();
            indices.Dispose();
            numVerts.Dispose();
            numIndices.Dispose();
        }

        private MeshCache GetOrAddMeshCache(Camera camera)
        {
            if (meshCache == null)
                return null;
            if (meshCache.TryGetValue(camera, out MeshCache value))
                return value;
            MeshCache newMeshCache = new MeshCache();
            meshCache.Add(camera, newMeshCache);
            return newMeshCache;
        }

        private bool CacheIsValid(Matrix4x4 localToWorldMatrix, Camera camera, MeshCache cache)
            => ViewProjectionMatrix(camera) * localToWorldMatrix == cache.WVPMatrix;

        private static Matrix4x4 ViewProjectionMatrix(Camera camera)
            => camera.projectionMatrix * camera.worldToCameraMatrix;

        private struct MeshJob : IJobParallelFor
        {
            // n is the number of points
            // c is the larger of the number of cap segments or max elbow segments
            [ReadOnly]
            private readonly NativeArray<Vert> screenSpacePoints;   // [n]
            [WriteOnly]
            public NativeArray<Vert> vertices;                      // [(n - 1) * 2 * (c + 2)]
            [WriteOnly]
            public NativeArray<uint> indices;                       // [(n - 1) * (2 * c + 4) * 3]
            [WriteOnly]
            public NativeArray<int> numVerts;                       // [n - 1]
            [WriteOnly]
            public NativeArray<int> numIndices;                     // [n - 1]

            [ReadOnly]
            private readonly float width;
            [ReadOnly]
            private readonly int capSegments;
            [ReadOnly]
            private readonly int maxElbowSegments;
            [ReadOnly]
            private readonly int maxPointSegments;

            public MeshJob(NativeArray<Vert> screenSpacePoints, NativeArray<Vert> vertices, NativeArray<uint> indices, NativeArray<int> numVerts, NativeArray<int> numIndices, float width, int capSegments, int maxElbowSegments)
            {
                this.screenSpacePoints = screenSpacePoints;
                this.vertices = vertices;
                this.indices = indices;
                this.numVerts = numVerts;
                this.numIndices = numIndices;
                this.width = width;
                this.capSegments = capSegments;
                this.maxElbowSegments = maxElbowSegments;
                maxPointSegments = Math.Max(capSegments, maxElbowSegments);
            }

            public void Execute(int index)
            {
                // Subarrays:
                // vertices: starts at index * 2 * (c + 2)      has 2 * (c + 2) elements
                // indices: starts at index * (2 * c + 4) * 3   has (2 * c + 4) * 3 elements

                int startVerts = index * 2 * (maxPointSegments + 2);
                int startIndxs = index * (2 * maxPointSegments + 4) * 3;
                int vertElmnts = 2 * (maxPointSegments + 2);
                int indxElmnts = (2 * maxPointSegments + 4) * 3;
                BillboardLineSegment(
                    width,
                    screenSpacePoints[Math.Max(index - 1, 0)],
                    screenSpacePoints[index],
                    screenSpacePoints[index + 1],
                    screenSpacePoints[Math.Min(index + 2, screenSpacePoints.Length - 1)],
                    vertices.GetSubArray(startVerts, vertElmnts),
                    indices.GetSubArray(startIndxs, indxElmnts),
                    capSegments, maxElbowSegments,
                    out int nVerts, out int nIndices);
                numVerts[index] = nVerts;
                numIndices[index] = nIndices;
            }

            public void OutputToMesh(Mesh mesh)
            {
                int totalVerts = numVerts.Sum();
                int totalIndices = numIndices.Sum();

                mesh.SetVertexBufferParams(totalVerts, Vert.VertexAttribute);
                mesh.SetIndexBufferParams(totalIndices, IndexFormat.UInt32);

                MeshUpdateFlags meshUpdateFlags =
                    MeshUpdateFlags.DontValidateIndices |
                    MeshUpdateFlags.DontResetBoneBounds |
                    MeshUpdateFlags.DontNotifyMeshUsers |
                    MeshUpdateFlags.DontRecalculateBounds;

                int meshVertOffset = 0;
                int meshIndxOffest = 0;
                for (int i = 0; i < screenSpacePoints.Length - 1; i++)
                {
                    int startVerts = i * 2 * (maxPointSegments + 2);
                    int startIndxs = i * (2 * maxPointSegments + 4) * 3;
                    int nVerts = numVerts[i];
                    int nIndxs = numIndices[i];

                    mesh.SetVertexBufferData(vertices, startVerts, meshVertOffset, nVerts, 0, meshUpdateFlags);
                    mesh.SetIndexBufferData(indices, startIndxs, meshIndxOffest, nIndxs, meshUpdateFlags);

                    meshVertOffset += nVerts;
                    meshIndxOffest += nIndxs;
                }

                mesh.subMeshCount = 1;
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, totalIndices, MeshTopology.Triangles));
                mesh.RecalculateBounds();
            }
        }

        private class MeshCache : IDisposable
        {
            public Matrix4x4 WVPMatrix;
            public Mesh mesh;

            public MeshCache()
            {
                WVPMatrix = Matrix4x4.zero;
                mesh = new Mesh();
                mesh.MarkDynamic();
            }

            public void UpdateWVPMatrix(Camera camera, Matrix4x4 localToWorldMatrix)
            {
                if (camera == null)
                {
                    WVPMatrix = localToWorldMatrix;
                    return;
                }
                WVPMatrix = ViewProjectionMatrix(camera) * localToWorldMatrix;
            }

            public void Dispose()
            {
                Destroy(mesh);
                mesh = null;
            }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private readonly struct Vert
        {
            public readonly Vector3 position;
            public readonly Color32 color;

            public static VertexAttributeDescriptor[] VertexAttribute { get; } = new VertexAttributeDescriptor[]
            {
            new VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4)
            };

            public Vert(Vector3 position, Color color)
            {
                this.position = position;
                this.color = color;
            }

            public Vert(Vector3 position, Vert baseVert)
            {
                this.position = position;
                color = baseVert.color;
            }

            public static Vert Offset(Vert baseVert, Vector3 offset)
                => new Vert(baseVert.position + offset, baseVert.color);

            public Vert ToScreenSpace(Matrix4x4 localToWorldMatrix, Camera camera)
            {
                return new Vert(camera.WorldToScreenPoint(localToWorldMatrix * position), color);
            }
            public Vert ToLocalSpace(Matrix4x4 worldToLocalMatrix, Camera camera)
            {
                return new Vert(worldToLocalMatrix * camera.ScreenToWorldPoint(position), color);
            }
        }
    }
}