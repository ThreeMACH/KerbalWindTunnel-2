using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Graphing.UI
{
    [ExecuteInEditMode]
    public class JumpFloodOutlineRenderer : MonoBehaviour
    {
        //[ColorUsage(false, false)] public Color outlineColor = Color.white;
        [Range(0.0f, 50.0f)] public float outlinePixelWidth = 4f;

        // list of all renderer components you want to have outlined as a single silhouette
        public List<Renderer> renderers = new List<Renderer>();

        // hidden reference to ensure shader gets included with builds
        // gets auto-assigned with an OnValidate() function later
        [HideInInspector, SerializeField] internal Shader outlineShader;

        // some hidden settings
        const string shaderName = "Hidden/JumpFloodOutline";
        const CameraEvent cameraEvent = CameraEvent.AfterForwardAlpha;
        const bool useSeparableAxisMethod = true;

        // shader pass indices
        const int SHADER_PASS_INTERIOR_STENCIL = 0;
        const int SHADER_PASS_SILHOUETTE_BUFFER_FILL = 1;
        const int SHADER_PASS_JFA_INIT = 2;
        const int SHADER_PASS_JFA_FLOOD = 3;
        const int SHADER_PASS_JFA_FLOOD_SINGLE_AXIS = 4;
        const int SHADER_PASS_JFA_OUTLINE = 5;
        const int SHADER_PASS_JFA_OUTLINE_DEPTH = 6;
        const int SHADER_PASS_DEPTHINIT = 7;

        // render texture IDs
        static private readonly int silhouetteBufferID = Shader.PropertyToID("_SilhouetteBuffer");
        static private readonly int nearestPointID = Shader.PropertyToID("_NearestPoint");
        static private readonly int nearestPointPingPongID = Shader.PropertyToID("_NearestPointPingPong");
        static private readonly int depthBufferID = Shader.PropertyToID("_JumpDepthBuffer");

        // shader properties
        static private readonly int outlineWidthID = Shader.PropertyToID("_OutlineWidth");
        static private readonly int stepWidthID = Shader.PropertyToID("_StepWidth");
        static private readonly int axisWidthID = Shader.PropertyToID("_AxisWidth");
        static private readonly int colorTexID = Shader.PropertyToID("_ColorTex");
        static private readonly int depthTexID = Shader.PropertyToID("_DepthTex");

        // private variables
        private CommandBuffer cb;
        private Material outlineMat;
        private Camera bufferCam;

        private Mesh MeshFromRenderer(Renderer r)
        {
            if (r is SkinnedMeshRenderer)
                return (r as SkinnedMeshRenderer).sharedMesh;
            else if (r is MeshRenderer)
                return r.GetComponent<MeshFilter>().sharedMesh;

            return null;
        }

        private void CreateCommandBuffer(Camera cam)
        {
            if (renderers == null || renderers.Count == 0)
                return;

            if (cb == null)
            {
                cb = new CommandBuffer();
                cb.name = "JumpFloodOutlineRenderer: " + gameObject.name;
            }
            else
            {
                cb.Clear();
            }

            if (outlineMat == null)
            {
                outlineMat = new Material(outlineShader != null ? outlineShader : Shader.Find(shaderName));
            }

            // do nothing if no outline will be visible
            if (outlinePixelWidth <= 1f)  //outlineColor.a <= (1f / 255f) || 
            {
                cb.Clear();
                return;
            }

            // support meshes with sub meshes
            // can be from having multiple materials, complex skinning rigs, or a lot of vertices
            int renderersCount = renderers.Count;
            int[] subMeshCount = new int[renderersCount];

            for (int i = 0; i < renderersCount; i++)
            {
                var mesh = MeshFromRenderer(renderers[i]);
                if (mesh == null)
                    continue;
                Debug.Assert(mesh != null, "JumpFloodOutlineRenderer's renderer [" + i + "] is missing a valid mesh.", gameObject);
                if (mesh != null)
                {
                    // assume staticly batched meshes only have one sub mesh
                    if (renderers[i].isPartOfStaticBatch)
                        subMeshCount[i] = 1; // hack hack hack
                    else
                        subMeshCount[i] = mesh.subMeshCount;
                }
            }

            // render meshes to main buffer for the interior stencil mask
            cb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            for (int i = 0; i < renderersCount; i++)
            {
                //for (int m = 0; m < subMeshCount[i]; m++)
                //cb.DrawRenderer(renderers[i], outlineMat, m, SHADER_PASS_INTERIOR_STENCIL);
            }

            // match current quality settings' MSAA settings
            // doesn't check if current camera has MSAA enabled
            // also could just always do MSAA if you so pleased
            //int msaa = Mathf.Max(1, QualitySettings.antiAliasing);

            int width = cam.scaledPixelWidth;
            int height = cam.scaledPixelHeight;

            // setup descriptor for silhouette render texture
            RenderTextureDescriptor silhouetteRTD = new RenderTextureDescriptor()
            {
                dimension = TextureDimension.Tex2D,
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,

                width = width,
                height = height,

                msaaSamples = 1,// msaa,
                depthBufferBits = 0,

                sRGB = false,

                useMipMap = false,
                autoGenerateMips = false
            };

            // create silhouette buffer and assign it as the current render target
            cb.GetTemporaryRT(silhouetteBufferID, silhouetteRTD, FilterMode.Point);
            cb.SetRenderTarget(silhouetteBufferID);
            cb.ClearRenderTarget(false, true, Color.clear);

            // render meshes to silhouette buffer (silhouetteBufferID)
            for (int i = 0; i < renderersCount; i++)
            {
                for (int m = 0; m < subMeshCount[i]; m++)
                    //cb.DrawRenderer(renderers[i], outlineMat, m, SHADER_PASS_SILHOUETTE_BUFFER_FILL);
                    cb.DrawRenderer(renderers[i], renderers[i].sharedMaterial, m);
            }

            // render to the depth texture that we'll use later
            RenderTextureDescriptor depthRTD = silhouetteRTD;
            depthRTD.graphicsFormat = GraphicsFormat.R32_SFloat;
            cb.GetTemporaryRT(depthBufferID, depthRTD, FilterMode.Point);
            cb.SetRenderTarget(depthBufferID);
            //cb.ClearRenderTarget(false, true, Color.clear);
            for (int i = 0; i < renderersCount; i++)
            {
                for (int m = 0; m < subMeshCount[i]; m++)
                    cb.DrawRenderer(renderers[i], outlineMat, m, SHADER_PASS_DEPTHINIT);
            }

            // Humus3D wire trick, keep line 1 pixel wide and fade alpha instead of making line smaller
            // slightly nicer looking and no more expensive
            //Color adjustedOutlineColor = outlineColor;
            //adjustedOutlineColor.a *= Mathf.Clamp01(outlinePixelWidth);
            //cb.SetGlobalColor(outlineColorID, adjustedOutlineColor.linear);
            cb.SetGlobalFloat(outlineWidthID, Mathf.Max(0f, (outlinePixelWidth - 1) / 2));

            // setup descriptor for jump flood render textures
            RenderTextureDescriptor jfaRTD = new RenderTextureDescriptor()
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                dimension = TextureDimension.Tex2D,

                width = width,
                height = height,

                msaaSamples = 1,
                depthBufferBits = 0,

                sRGB = false,

                useMipMap = false,
                autoGenerateMips = false
            };

            jfaRTD.graphicsFormat = GraphicsFormat.R16G16_SNorm; // This line is somehow key to Unity *not* crashing!
                                                                 // Why?! Even with the same format above!
                                                                 //Could store depth position during Init instead of looking it up during the outline pass...
                                                                 //if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16_SNorm, FormatUsage.Render))
                                                                 //jfaRTD.graphicsFormat = GraphicsFormat.R16G16B16_SNorm;
                                                                 //else
                                                                 //jfaRTD.graphicsFormat = GraphicsFormat.R16G16B16A16_SNorm;

            // create jump flood buffers to ping pong between
            cb.GetTemporaryRT(nearestPointID, jfaRTD, FilterMode.Point);
            cb.GetTemporaryRT(nearestPointPingPongID, jfaRTD, FilterMode.Point);

            // calculate the number of jump flood passes needed for the current outline width
            // + 1.0f to handle half pixel inset of the init pass and antialiasing
            int numMips = Mathf.CeilToInt(Mathf.Log(outlinePixelWidth + 1.0f, 2f));
            int jfaIter = numMips - 1;

            cb.SetGlobalTexture(depthTexID, depthBufferID);

            // Alan Wolfe's separable axis JFA - https://www.shadertoy.com/view/Mdy3D3
            if (useSeparableAxisMethod)
            {

                // jfa init
                cb.Blit(silhouetteBufferID, nearestPointID, outlineMat, SHADER_PASS_JFA_INIT);

                // jfa flood passes
                for (int i = jfaIter; i >= 0; i--)
                {
                    // calculate appropriate jump width for each iteration
                    // + 0.5 is just me being cautious to avoid any floating point math rounding errors
                    float stepWidth = Mathf.Pow(2, i) + 0.5f;

                    // the two separable passes, one axis at a time
                    cb.SetGlobalVector(axisWidthID, new Vector2(stepWidth, 0f));
                    cb.Blit(nearestPointID, nearestPointPingPongID, outlineMat, SHADER_PASS_JFA_FLOOD_SINGLE_AXIS);
                    cb.SetGlobalVector(axisWidthID, new Vector2(0f, stepWidth));
                    cb.Blit(nearestPointPingPongID, nearestPointID, outlineMat, SHADER_PASS_JFA_FLOOD_SINGLE_AXIS);
                }
            }

            // traditional JFA
            else
            {
                // choose a starting buffer so we always finish on the same buffer
#pragma warning disable CS0162 // Unreachable code detected
                int startBufferID = (jfaIter % 2 == 0) ? nearestPointPingPongID : nearestPointID;
#pragma warning restore CS0162 // Unreachable code detected

                // jfa init
                cb.Blit(silhouetteBufferID, startBufferID, outlineMat, SHADER_PASS_JFA_INIT);

                // jfa flood passes
                for (int i = jfaIter; i >= 0; i--)
                {
                    // calculate appropriate jump width for each iteration
                    // + 0.5 is just me being cautious to avoid any floating point math rounding errors
                    cb.SetGlobalFloat(stepWidthID, Mathf.Pow(2, i) + 0.5f);

                    // ping pong between buffers
                    if (i % 2 == 1)
                        cb.Blit(nearestPointID, nearestPointPingPongID, outlineMat, SHADER_PASS_JFA_FLOOD);
                    else
                        cb.Blit(nearestPointPingPongID, nearestPointID, outlineMat, SHADER_PASS_JFA_FLOOD);
                }
            }

            // jfa decode & outline render
            cb.SetGlobalTexture(colorTexID, silhouetteBufferID);
            cb.Blit(nearestPointID, BuiltinRenderTextureType.CameraTarget, outlineMat, SHADER_PASS_JFA_OUTLINE_DEPTH);

            cb.ReleaseTemporaryRT(silhouetteBufferID);
            cb.ReleaseTemporaryRT(nearestPointID);
            cb.ReleaseTemporaryRT(nearestPointPingPongID);
            cb.ReleaseTemporaryRT(depthBufferID);
        }

        void ApplyCommandBuffer(Camera cam)
        {
#if UNITY_EDITOR
            // hack to avoid rendering in the inspector preview window
            if (cam.gameObject.name == "Preview Scene Camera")
                return;
#endif

            if (bufferCam != null)
            {
                if (bufferCam == cam)
                    return;
                else
                    RemoveCommandBuffer(cam);
            }

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

            // skip rendering if none of the renderers are in view
            bool visible = false;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (GeometryUtility.TestPlanesAABB(planes, renderers[i].bounds))
                {
                    visible = true;
                    break;
                }
            }

            if (!visible)
                return;

            CreateCommandBuffer(cam);
            if (cb == null)
                return;

            bufferCam = cam;
            bufferCam.AddCommandBuffer(cameraEvent, cb);
        }

        void RemoveCommandBuffer(Camera cam)
        {
            if (bufferCam != null && cb != null)
            {
                bufferCam.RemoveCommandBuffer(cameraEvent, cb);
                bufferCam = null;
            }
        }

        void OnEnable()
        {
            Camera.onPreRender += ApplyCommandBuffer;
            Camera.onPostRender += RemoveCommandBuffer;
        }

        void OnDisable()
        {
            Camera.onPreRender -= ApplyCommandBuffer;
            Camera.onPostRender -= RemoveCommandBuffer;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (renderers != null)
            {
                for (int i = renderers.Count - 1; i > -1; i--)
                {
                    if (renderers[i] == null || (!(renderers[i] is SkinnedMeshRenderer) && !(renderers[i] is MeshRenderer)))
                        renderers.RemoveAt(i);
                    else
                    {
                        bool foundDuplicate = false;
                        for (int k = 0; k < i; k++)
                        {
                            if (renderers[i] == renderers[k])
                            {
                                foundDuplicate = true;
                                break;
                            }
                        }

                        if (foundDuplicate)
                            renderers.RemoveAt(i);
                    }
                }
            }

            if (outlineShader == null)
                outlineShader = Shader.Find(shaderName);
        }

        public void FindActiveMeshes()
        {
            Undo.RecordObject(this, "Filling with all active Renderer components");
            GameObject parent = this.gameObject;
            if (renderers != null)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer)
                    {
                        parent = renderer.transform.parent.gameObject;
                        break;
                    }
                }
            }

            if (parent != null)
            {
                var skinnedMeshes = parent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var meshes = parent.GetComponentsInChildren<MeshRenderer>(true);
                if (skinnedMeshes.Length > 0 || meshes.Length > 0)
                {
                    foreach (var sk in skinnedMeshes)
                    {
                        if (sk.gameObject.activeSelf)
                            renderers.Add(sk);
                    }
                    foreach (var mesh in meshes)
                    {
                        if (mesh.gameObject.activeSelf)
                            renderers.Add(mesh);
                    }
                    OnValidate();
                }
                else
                    Debug.LogError("No Active Meshes Found");
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(JumpFloodOutlineRenderer))]
    public class JumpFloodOutlineRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Get Active Children Renderers"))
            {
                UnityEngine.Object[] objs = serializedObject.targetObjects;

                foreach (var obj in objs)
                {
                    var mh = (obj as JumpFloodOutlineRenderer);
                    mh.FindActiveMeshes();
                }
            }
        }
    }
#endif
}