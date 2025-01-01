using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Graphing
{
    [RequireComponent(typeof(RectTransform))]
    public partial class GraphDrawer : MonoBehaviour
    {
        [SerializeField]
        public Material surfGraphMaterial;
        [SerializeField]
        public Material outlineGraphMaterial;
        [SerializeField]
        protected GameObject _lineRendererPrefab;
        //protected Shader JumpFloodShader;
        //[SerializeField]
        //public Material lineVertexMaterial;
        //[SerializeField]
        //protected GameObject _ownPrefab;

        protected Grapher grapher;
        protected IGraphable graph;
        protected Mesh mesh;
        protected bool markedForRedraw = false;
        protected readonly List<EventArgs> redrawReasons = new List<EventArgs>();
        protected bool ignoreZScalePos = false;

        public Material ShaderMaterial
        {
            get => _material;
            set
            {
                if (_material == value)
                    return;
                
                _material = value;
                materialIsUnique = false;

                if (TryGetComponent(out MeshRenderer meshRenderer))
                    meshRenderer.material = _material;

                foreach (GraphDrawer graphDrawer in GetComponentsInChildren<GraphDrawer>(true))
                    if (graphDrawer != this && !(graphDrawer.graph is OutlineMask))
                        graphDrawer.ShaderMaterial = value;
            }
        }
        private Material _material = null;
        private bool materialIsUnique = false;

        public Bounds Bounds
        {
            get
            {
                if (graph == null)
                    return new Bounds(Vector3.zero, Vector3.zero);
                else if (graph is IGraphable3 iGraphable3)
                {
                    if (iGraphable3 is Graphable3 graphable3 && graphable3.Transpose)
                        return new Bounds(
                            new Vector3((graph.YMax + graph.YMin) / 2, (graph.XMax + graph.XMin) / 2, -(iGraphable3.ZMax + iGraphable3.ZMin) / 2),
                            new Vector3(graph.YMax - graph.YMin, graph.XMax - graph.XMin, iGraphable3.ZMax - iGraphable3.ZMin));
                    return new Bounds(
                        new Vector3((graph.XMax + graph.XMin) / 2, (graph.YMax + graph.YMin) / 2, -(iGraphable3.ZMax + iGraphable3.ZMin) / 2),
                        new Vector3(graph.XMax - graph.XMin, graph.YMax - graph.YMin, iGraphable3.ZMax - iGraphable3.ZMin));
                }
                if (graph is Graphable _graphable && _graphable.Transpose)
                    return new Bounds(
                        new Vector3((graph.YMax + graph.YMin) / 2, (graph.XMax + graph.XMin) / 2),
                        new Vector3(graph.YMax - graph.YMin, graph.XMax - graph.XMin));
                return new Bounds(
                    new Vector3((graph.XMax + graph.XMin) / 2, (graph.YMax + graph.YMin) / 2),
                    new Vector3(graph.XMax - graph.XMin, graph.YMax - graph.YMin));
            }
        }

        public IGraphable Graph
        {
            get => graph;
            set => SetGraph(value, grapher);
        }

        public IGraphable FirstVisibleInHierarchy => GraphableCollection.FirstVisibleGraph(graph);
        public IColorGraph FirstColorGraphInHierarchy => GraphableCollection.FirstColorGraph(graph);

        public virtual void SetGraph(IGraphable graph, Grapher grapher)
        {
            this.grapher = grapher;

            if (this.graph == graph)
                return;

            if (this.graph != null)
            {
                this.graph.ValuesChanged -= OnValuesChangedHandler;
                this.graph.DisplayChanged -= OnDisplayChangedHandler;
            }
            // Reset this if the new graph does not share the current graph's drawing methods.
            // ILineGraphs are not necessarily AssignableFrom, but share drawing systems.
            // Always wipe for a new GraphableCollection.
            if (this.graph == null ||
                graph is GraphableCollection ||
                (!this.graph.GetType().IsAssignableFrom(graph.GetType()) && !(this.graph is ILineGraph && graph is ILineGraph)))
                ResetGrapher(graph.GetType());
            else
                SetupForType(graph.GetType());

            this.graph = graph;

            lock (redrawReasons)
                redrawReasons.Clear();
            this.graph.ValuesChanged += OnValuesChangedHandler;
            this.graph.DisplayChanged += OnDisplayChangedHandler;
            if (this.graph is Graphable _graphable)
                Transpose = _graphable.Transpose;
            else
                Transpose = false;

            Draw(true);

            if (!graph.Visible)
                gameObject.SetActive(false);
        }

        private void ResetGrapher(Type graphType)
        {
            foreach (GraphDrawer childDrawer in GetComponentsInChildren<GraphDrawer>().Where(drawer => drawer != this))
                Destroy(childDrawer.gameObject);
            foreach (Component component in GetComponents<Component>())
            {
                if (component == this)
                    continue;
                if (component is Transform)
                    continue;
                Destroy(component);
            }
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localEulerAngles = Vector3.zero;
            ignoreZScalePos = false;
            mesh?.Clear();
            SetupForType(graphType);
        }

        protected MeshRenderer MeshRendererSetup()
        {
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return meshRenderer;
        }

        protected virtual void SetupForType(Type graphType)
        {
            if (graphType == null)
                return;
            if (typeof(ILineGraph).IsAssignableFrom(graphType))
            {
                Instantiate(_lineRendererPrefab, transform);
                //MeshRendererSetup();
                //LineGraphSetup();
                return;
            }
            if (typeof(SurfGraph).IsAssignableFrom(graphType))
            {
                MeshRendererSetup();
                SurfGraphSetup();
                return;
            }
            if (typeof(OutlineMask).IsAssignableFrom(graphType))
            {
                MeshRendererSetup();
                OutlineGraphSetup();
                return;
            }
            if (typeof(GraphableCollection).IsAssignableFrom(graphType))
            {
                // No setup required. Just following the pattern.
                return;
            }
            Debug.LogError("GraphDrawer is not equipped to handle that type of graph.");
        }

        protected virtual void OnDisplayChangedHandler(object sender, IDisplayEventArgs args)
        {
            if (sender != graph)
                return;

            if (args is DisplayNameChangedEventArgs displayNameChangedEventArgs)
                grapher.DisplayNameChangedHandler((IGraphable)sender, displayNameChangedEventArgs);
            else if (args is VisibilityChangedEventArgs visibilityArgs)
            {
                grapher.QueueActivation(this, visibilityArgs.Visible);
                MarkForRedraw(visibilityArgs);
            }
            else
                MarkForRedraw((EventArgs)args);
        }

        protected virtual void OnValuesChangedHandler(object sender, IValueEventArgs args)
        {
            if (sender != graph)
                return;
            MarkForRedraw((EventArgs)args);
        }

        private bool _transpose = false;
        protected bool Transpose
        {
            get => _transpose;
            set
            {
                bool changed = _transpose != value;
                _transpose = value;
                if (!changed)
                    return;

                if (_transpose)
                    transform.localRotation = Quaternion.Euler(0, 0, -90);
                else
                    transform.localRotation = Quaternion.identity;

                Vector3 localScale = transform.localScale;
                Vector3 boundsSize = Bounds.size;
                float tempX, tempY;
                tempX = localScale.x;
                tempY = localScale.y;
                //                              Bounds has already swapped X and Y.
                localScale.x = tempY / tempX * (boundsSize.x / boundsSize.y);
                localScale.y = Mathf.Abs(tempX) / tempY * (boundsSize.y / boundsSize.x);

                if (float.IsNaN(localScale.x) || float.IsInfinity(localScale.x)) localScale.x = 1;
                if (float.IsNaN(localScale.y) || float.IsInfinity(localScale.y)) localScale.y = 1;

                localScale.x = -localScale.x;
                transform.localScale = localScale;

                Vector2 localPosition = ((RectTransform)transform).anchoredPosition;
                tempX = localPosition.x;
                localPosition.x = localPosition.y;
                localPosition.y = tempX;
                ((RectTransform)transform).anchoredPosition = localPosition;
            }
        }

        protected void MarkForRedraw(EventArgs reason)
        {
            lock (redrawReasons)
            {
                redrawReasons.Add(reason);
                markedForRedraw = true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void LateUpdate()
        {
            if (markedForRedraw)
            {
                LateRedraw();
            }
        }
        protected virtual void LateRedraw() => Draw();

        protected virtual void GenerateOtherGraph(IGraphable graphable, IGrouping<Type, EventArgs> redrawReasons, bool forceRegenerate = false)
            => Debug.LogError("GraphDrawer is not equipped to draw that type of graph.");

        protected virtual void Draw(bool forceRegenerate = false)
        {
            int localFlag = 0;
            lock (redrawReasons)
            {
                if (redrawReasons.Count == 0)
                    redrawReasons.Add(new EventArgs());
                foreach (IGrouping<Type, EventArgs> reasonGroup in redrawReasons.Reverse<EventArgs>().GroupBy(reason => reason.GetType()).OrderBy(group => EventArgsSort(group.Key)))
                {
                    if (reasonGroup.Key == typeof(VisibilityChangedEventArgs))
                    {
                        gameObject.SetActive(graph.Visible);
                        continue;
                    }
                    if (reasonGroup.Key == typeof(TransposeChangedEventArgs))
                    {
                        Transpose = (reasonGroup.Last() as TransposeChangedEventArgs).Transpose;
                        continue;
                    }
                    if (graph is ILineGraph lineGraphable)
                    {
                        localFlag = DrawLineGraph(lineGraphable, reasonGroup, localFlag, forceRegenerate);
                        continue;
                    }
                    if (graph is SurfGraph surfGraph)
                    {
                        localFlag = DrawSurfGraph(surfGraph, reasonGroup, localFlag, forceRegenerate);
                        continue;
                    }
                    if (graph is OutlineMask outlineMask)
                    {
                        localFlag = DrawOutlineGraph(outlineMask, reasonGroup, localFlag, forceRegenerate);
                        continue;
                    }
                    if (graph is GraphableCollection collection)
                    {
                        localFlag = DrawCollection(collection, reasonGroup, localFlag, forceRegenerate);
                        continue;
                    }

                    GenerateOtherGraph(graph, reasonGroup, forceRegenerate);
                }

                redrawReasons.Clear();
                markedForRedraw = false;
            }
        }

        /// <summary>
        /// Sets the transform position and scale. This should only ever be called on the parent GraphDrawer, and never on its children.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="origin"></param>
        /// <param name="scale"></param>
        public void SetOriginAndScale(AxisUI.AxisDirection axis, float origin, float scale)
        {
            if (transform.parent.GetComponent<GraphDrawer>() != null)
                return;
            if (axis == AxisUI.AxisDirection.Depth && ignoreZScalePos)
                return;
            if (axis < AxisUI.AxisDirection.Horizontal || axis > AxisUI.AxisDirection.Depth)
                throw new ArgumentOutOfRangeException("axis", "Axis argument must be 0, 1, or 2.");
            if (float.IsNaN(scale) || float.IsInfinity(scale))
                scale = 0;

            Vector3 transformPosition = ((RectTransform)transform).anchoredPosition3D;
            Vector3 transformScale = transform.localScale;
            transformPosition[(int)axis] = -origin * scale;
            transformScale[(int)axis] = scale;
            if (axis == AxisUI.AxisDirection.Depth && graph is IGraphable3 && !(graph is GraphableCollection))
                transformPosition.z += grapher.ZOffset2D;

            ((RectTransform)transform).anchoredPosition3D = transformPosition;
            transform.localScale = transformScale;
        }
        public void SetOriginAndScale(Vector3 origin, Vector3 scale)
        {
            if (float.IsNaN(scale.x) || float.IsInfinity(scale.x))
                scale.x = 0;
            if (float.IsNaN(scale.y) || float.IsInfinity(scale.y))
                scale.y = 0;
            if (float.IsNaN(scale.z) || float.IsInfinity(scale.z))
                scale.z = 0;

            if (Transpose)
            {
                float temp = scale.x;
                scale.x = -scale.y;
                scale.y = temp;
            }

            if (graph is IGraphable3 && !(graph is GraphableCollection))
                origin -= new Vector3(0, 0, grapher.ZOffset2D);
            ((RectTransform)transform).anchoredPosition3D = -origin;
            transform.localScale = scale;
        }

        public void SetColorMapFlat(Mesh mesh, Color color)
        {
            return;
            Color[] colors = new Color[mesh.vertexCount];
            for (int i = mesh.vertexCount - 1; i >= 0; i--)
                colors[i] = color;
            mesh.SetColors(colors);
        }
        public void SetColorMapProperties(Mesh mesh, Graphable graph)
        {
            return;
            // TODO throw new NotImplementedException();
            if (graph.UseSingleColor)
            {
                SetColorMapFlat(mesh, graph.color);
                return;
            }

            Color[] colors = new Color[mesh.vertexCount];
            for (int i = mesh.vertexCount - 1; i >= 0; i--)
                colors[i] = graph.EvaluateColor(mesh.vertices[i]);
            mesh.SetColors(colors);
        }

        protected virtual void Awake()
        {
            grapher = GetComponentInParent<Grapher>();
        }

        protected virtual void OnDestroy()
        {
            if (graph != null)
            {
                graph.ValuesChanged -= OnValuesChangedHandler;
                graph.DisplayChanged -= OnDisplayChangedHandler;
            }
            grapher?.UnregisterGraphDrawer(this);
            if (mesh != null)
                Destroy(mesh);
            if (materialIsUnique)
                Destroy(_material);
        }

        public void MakeMaterialUnique()
        {
            if (materialIsUnique)
                return;
            if (_material == null)
                return;
            ShaderMaterial = Instantiate(_material);
            materialIsUnique = true;
        }

        protected static int EventArgsSort(Type eventType)
        {
            if (!typeof(EventArgs).IsAssignableFrom(eventType))
                return -1;
            if (eventType == typeof(ChildDisplayChangedEventArgs))
                return -1;
            if (eventType == typeof(ChildValueChangedEventArgs))
                return -1;
            if (eventType == typeof(ValuesChangedEventArgs))
                return 0;
            if (eventType == typeof(VisibilityChangedEventArgs))
                return 1;
            if (eventType == typeof(BoundsChangedEventArgs))
                return 2;
            if (eventType == typeof(GraphElementRemovedEventArgs))
                return 3;
            if (eventType == typeof(GraphElementsRemovedEventArgs))
                return 4;
            if (eventType == typeof(GraphElementAddedEventArgs))
                return 5;
            if (eventType == typeof(GraphElementsAddedEventArgs))
                return 6;
            if (eventType == typeof(TransposeChangedEventArgs))
                return 7;
            if (eventType == typeof(ColorChangedEventArgs))
                return 8;
            if (eventType == typeof(LineWidthChangedEventArgs))
                return 9;
            if (eventType == typeof(MaskLineOnlyChangedEventArgs))
                return 10;
            if (eventType == typeof(MaskCriteriaChangedEventArgs))
                return 11;
            if (eventType == typeof(DisplayNameChangedEventArgs))
                return 12;
            if (eventType == typeof(AxisNameChangedEventArgs))
                return 13;
            if (eventType == typeof(AxisUnitChangedEventArgs))
                return 14;
            return -1;
        }
    }
}