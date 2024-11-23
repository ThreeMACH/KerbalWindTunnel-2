using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Graphing.UI
{
    public class GraphLayoutGroup : LayoutGroup
    {
        // [0] is the main graph
        // [1] is left axis group
        // [2] is bottom axis group
        // [3] is right axis group
        // [4] is top axis group

        private Vector2[] minimum, preferred;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            InitializeLayout();

            float totalMinWidth = 0;
            float totalPreferredWidth = 0;

            for (int i = 1; i < minimum.Length; i += 2)
            {
                totalMinWidth += minimum[i].x;
                totalPreferredWidth += preferred[i].x;
            }
            float interimMin = 0;
            float interimPreferred = 0;
            for (int i = 2; i < minimum.Length; i += 2)
            {
                interimMin = Mathf.Max(interimMin, minimum[i].x);
                interimPreferred = Mathf.Max(interimPreferred, preferred[i].x);
            }
            totalMinWidth += Mathf.Max(interimMin, minimum[0].x) + padding.horizontal;
            totalPreferredWidth += Mathf.Max(interimPreferred, preferred[0].x) + padding.horizontal;

            SetLayoutInputForAxis(totalMinWidth, totalPreferredWidth, -1, 0);
        }
        public override void CalculateLayoutInputVertical()
        {
            base.CalculateLayoutInputHorizontal();
            InitializeLayout();

            float totalMinHeight = 0;
            float totalPreferredHeight = 0;

            for (int i = 2; i < minimum.Length; i += 2)
            {
                totalMinHeight += minimum[i].y;
                totalPreferredHeight += preferred[i].y;
            }
            float interimMin = 0;
            float interimPreferred = 0;
            for (int i = 1; i < minimum.Length; i += 2)
            {
                interimMin = Mathf.Max(interimMin, minimum[i].y);
                interimPreferred = Mathf.Max(interimPreferred, preferred[i].y);
            }
            totalMinHeight += Mathf.Max(interimMin, minimum[0].y) + padding.vertical;
            totalPreferredHeight += Mathf.Max(interimPreferred, preferred[0].y) + padding.vertical;

            SetLayoutInputForAxis(totalMinHeight, totalPreferredHeight, -1, 1);
        }
        public override void SetLayoutHorizontal() => SetChildrenAlongAxis(0);
        public override void SetLayoutVertical() => SetChildrenAlongAxis(1);

        private void SetChildrenAlongAxis(int axis)
        {
            float space, gridOrigin, centerOffset = 0, centerSize;//extraSpace

            if (axis == 0)
            {
                space = rectTransform.rect.width;
                gridOrigin = padding.left;
            }
            else
            {
                space = rectTransform.rect.height;
                gridOrigin = padding.top;
            }
            centerOffset += gridOrigin;
            //extraSpace = space - LayoutUtility.GetPreferredSize(rectTransform, axis);
            centerSize = space - (axis == 0 ? padding.horizontal : padding.vertical);


            float[] offsets = new float[4];
            for (int i = 1; i < rectChildren.Count; i++)
            {
                if ((i - 1) % 2 == axis)
                    centerSize -= axis == 0 ? preferred[i].x : preferred[i].y;
                if (i % 4 <= 1)
                    offsets[i % 4] += axis == 0 ? preferred[i].x : preferred[i].y;
            }

            centerOffset += offsets[1 - axis];

            if (rectChildren.Count > 0)
                SetChildAlongAxis(rectChildren[0], axis, centerOffset, centerSize);

            for (int i = 1; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                float size = axis == 0 ? preferred[i].x : preferred[i].y;

                if (axis == 0 && i % 2 == 0)
                    size = centerSize;
                else if (axis != 0 && i % 2 == 1)
                    size = centerSize;

                switch (i % 4)
                {
                    case 1:
                        offsets[1] -= size;
                        SetChildAlongAxis(child, axis, axis == 0 ? gridOrigin + offsets[1] : centerOffset, size);
                        break;
                    case 2:
                        SetChildAlongAxis(child, axis, centerOffset + (axis == 0 ? 0 : centerSize + offsets[2]), size);
                        offsets[2] += size;
                        break;
                    case 3:
                        SetChildAlongAxis(child, axis, centerOffset + (axis == 0 ? centerSize + offsets[3] : 0), size);
                        offsets[3] += size;
                        break;
                    case 0: // 4
                        offsets[0] -= size;
                        SetChildAlongAxis(child, axis, axis == 0 ? centerOffset : gridOrigin + offsets[0], size);
                        break;
                }
            }
        }

        private void InitializeLayout()
        {
            List<RectTransform> children = rectChildren;
            int numChildren = rectChildren.Count;
            preferred = new Vector2[numChildren];
            minimum = new Vector2[numChildren];
            for (int i = 0; i < numChildren; i++)
            {
                preferred[i] = new Vector2(LayoutUtility.GetPreferredWidth(children[i]), LayoutUtility.GetPreferredHeight(children[i]));
                minimum[i] = new Vector2(LayoutUtility.GetMinWidth(children[i]), LayoutUtility.GetMinHeight(children[i]));
            }
        }
    }
}