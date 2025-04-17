using System;
using UnityEngine;

namespace Graphing
{
    [Serializable]
    public class Axis
    {
        [SerializeField]
        private float min, max;
        [SerializeField]
        private bool autoRoundMin = true;
        [SerializeField]
        private bool autoRoundMax = true;
        [SerializeField]
        public bool horizontal = true;

        public bool AutoRoundMin
        {
            get => autoRoundMin;
            set
            {
                if (autoRoundMin == value)
                    return;
                autoRoundMin = value;
                CalculateBounds(min, max, horizontal, autoRoundMin, autoRoundMax);
            }
        }
        public bool AutoRoundMax
        {
            get => autoRoundMax;
            set
            {
                if (autoRoundMax == value)
                    return;
                autoRoundMax = value;
                CalculateBounds(min, max, horizontal, autoRoundMin, autoRoundMax);
            }
        }

        public event EventHandler<AxisBoundsEventArgs> AxisBoundsChangedEvent;

        public class AxisBoundsEventArgs : EventArgs
        {
            public readonly float min, max;
            public readonly float oldMin, oldMax;
            public AxisBoundsEventArgs(float min, float max, float oldMin, float oldMax)
            {
                this.min = min;
                this.max = max;
                this.oldMin = oldMin;
                this.oldMax = oldMax;
            }
        }

        /// <summary>
        /// The major unit (tick mark step size).
        /// </summary>
        public float MajorUnit { get; private set; }
        /// <summary>
        /// Get or set the axis lower bound.
        /// </summary>
        public float Min
        {
            get { return min; }
            set
            {
                if (value > max)
                {
                    Debug.LogError("Cannot set minimum higher than maximum.");
                    value = max;
                }
                CalculateBounds(value, max, horizontal, autoRoundMin, autoRoundMax);
            }
        }
        /// <summary>
        /// Get or set the axis upper bound.
        /// </summary>
        public float Max
        {
            get { return max; }
            set
            {
                if (value < min)
                {
                    Debug.LogError("Cannot set maximum lower than minimum.");
                    value = min;
                }
                CalculateBounds(min, value, horizontal, autoRoundMin, autoRoundMax);
            }
        }

        protected virtual void BoundsChanged(float oldMin, float oldMax)
            => AxisBoundsChangedEvent?.Invoke(this, new AxisBoundsEventArgs(min, max, oldMin, oldMax));

        public void SetBounds(float min, float max)
        {
            if (max < min)
            {
                Debug.LogError("Cannot set maximum lower than minimum.");
                max = min;
            }
            CalculateBounds(min, max, horizontal, autoRoundMin, autoRoundMax);
        }
        
        private void CalculateBounds(float min, float max, bool forX = true, bool autoMin = true, bool autoMax = true)
        {
            float oldMin = this.min;
            float oldMax = this.max;

            if (min > max)
            {
                (max, min) = (min, max);
            }
            if (float.IsNaN(min) || float.IsNaN(max) || float.IsInfinity(min) || float.IsInfinity(max))
            {
                this.min = min;
                this.max = max;
                this.MajorUnit = 0;
                return;
            }
            this.MajorUnit = GetMajorUnit(min, max, forX);
            float minVar = min / MajorUnit;
            float maxVar = max / MajorUnit;
            bool exactMin = Mathf.Abs(Mathf.RoundToInt(minVar) - minVar) / MajorUnit < 5E-6;
            bool exactMax = Mathf.Abs(Mathf.RoundToInt(maxVar) - maxVar) / MajorUnit < 5E-6;
            if (autoMin)
            {
                if (exactMin)
                    this.min = min;
                else
                    this.min = Mathf.Floor(Mathf.Min(min, 0) / MajorUnit * 1.05f) * MajorUnit;
            }
            else
                this.min = min;
            if (autoMax)
            {
                if (exactMax)
                    this.max = max;
                else
                    this.max = Mathf.Ceil(Mathf.Max(max, 0) / MajorUnit * 1.05f) * MajorUnit;
            }
            else
                this.max = max;

            if (oldMin != this.min || oldMax != this.max)
                BoundsChanged(oldMin, oldMax);
        }

        /// <summary>
        /// Gets the major unit for a given minimum and maximum value.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="forX">Bool representing if this axis is for the X-axis.</param>
        /// <returns>The major unit.</returns>
        public static float GetMajorUnit(float min, float max, bool forX = true)
        {
            if (Mathf.Sign(max) != Mathf.Sign(min))
                return GetMajorUnit(max - min);
            float c;
            if (forX)
                c = 12f / 7;
            else
                c = 40f / 21;
            float range = Mathf.Max(max, -min);
            if (range == 0)
                return 1;
            if (range < 0)
                range = -range;
            float oom = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(range)));
            float normVal = range / oom;
            if (normVal > 5 * c)
                return 2 * oom;
            else if (normVal > 2.5f * c)
                return oom;
            else if (normVal > c)
                return 0.5f * oom;
            else
                return 0.2f * oom;
        }
        /// <summary>
        /// Gets the major unit for a given range.
        /// </summary>
        /// <param name="range">The distance from the upper bound to the lower bound.</param>
        /// <returns>The major unit.</returns>
        public static float GetMajorUnit(float range)
        {
            const float c = 18f / 11;
            if (range < 0)
                range = -range;
            float oom = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(range)));
            float normVal = range / oom;
            if (normVal > 5 * c)
                return 2 * oom;
            else if (normVal > 2.5f * c)
                return oom;
            else if (normVal > c)
                return 0.5f * oom;
            else
                return 0.2f * oom;
        }
        /// <summary>
        /// Gets the upper bound for a given minimum and maximum value.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="forX">Bool representing if this axis is for the X-axis.</param>
        /// <returns></returns>
        public static float GetMax(float min, float max, bool forX = true)
        {
            if (min > max)
            {
                (max, min) = (min, max);
            }
            float majorUnit = GetMajorUnit(min, max, forX);
            if (max % majorUnit == 0)
                return max;
            else
                return Mathf.Ceil(Mathf.Max(max, 0) / majorUnit * 1.05f) * majorUnit;
        }
        /// <summary>
        /// Gets the lower bound for a given minimum and maximum value.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="forX">Bool representing if this axis is for the X-axis.</param>
        /// <returns>The lower bound.</returns>
        public static float GetMin(float min, float max, bool forX = true)
        {
            if (min > max)
            {
                (max, min) = (min, max);
            }
            float majorUnit = GetMajorUnit(min, max, forX);
            if (min % majorUnit == 0)
                return min;
            else
                return Mathf.Floor(Mathf.Min(min, 0) / majorUnit * 1.05f) * majorUnit;
        }
    }
}