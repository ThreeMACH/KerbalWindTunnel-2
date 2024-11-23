using UnityEngine;
using UnityEngine.UI;

namespace UI_Tools
{
    public class FlexibleGridLayoutGroup : GridLayoutGroup
    {
        public bool flexible = true;
        protected override void Start()
        {
            if (flexible)
            {
                AdjustCellSize();
            }
            base.Start();
        }
        protected override void OnRectTransformDimensionsChange()
        {
            if (flexible)
            {
                AdjustCellSize();
            }
            base.OnRectTransformDimensionsChange();
        }
        private void AdjustCellSize()
        {
            if (constraint == Constraint.FixedColumnCount)
            {
                Vector2 cellSize = this.cellSize;
                cellSize.x = (rectTransform.rect.width - spacing.x * (constraintCount - 1)) / constraintCount;
                this.cellSize = cellSize;
            }
            else if (constraint == Constraint.FixedRowCount)
            {
                Vector2 cellSize = this.cellSize;
                cellSize.y = (rectTransform.rect.height - spacing.y * (constraintCount - 1)) / constraintCount;
                this.cellSize = cellSize;
            }
        }
    }
}