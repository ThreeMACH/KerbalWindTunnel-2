using System;
using UnityEngine;
using Accord.Math.Optimization;
using static KerbalWindTunnel.AeroPredictor;

namespace KerbalWindTunnel.VesselCache
{
    public static class AeroOptimizer
    {
        const float defaultAoATolerance = 0.05f * Mathf.Deg2Rad;
        const float defaultAoAOptTolerance = 0.1f * Mathf.Deg2Rad;
        const float defaultInputTolerance = 0.01f;

        static readonly Unity.Profiling.ProfilerMarker s_findMax = new Unity.Profiling.ProfilerMarker("AeroOptimizer.FindMaxAoA");
        static readonly Unity.Profiling.ProfilerMarker s_findMin = new Unity.Profiling.ProfilerMarker("AeroOptimizer.FindMinAoA");
        static readonly Unity.Profiling.ProfilerMarker s_findLevel = new Unity.Profiling.ProfilerMarker("AeroOptimizer.FindLevelAoA");
        static readonly Unity.Profiling.ProfilerMarker s_findInput = new Unity.Profiling.ProfilerMarker("AeroOptimizer.FindStablePitchInput");

        public static float FindMaxAoA(this AeroPredictor predictor, Conditions conditions, out float lift, float guess = float.NaN, float tolerance = defaultAoAOptTolerance)
        {
            const float pitchInput = 0;
            float result;
            s_findMax.Begin();
            if (predictor is IDirectAoAMaxProvider aoaProvider && aoaProvider.DirectAoAInitialized)
            {
                result = aoaProvider.GetAoAMax(conditions);
                lift = (float)predictor.AerodynamicObjectiveFunc(conditions, pitchInput)(result);
            }
            else
            {
                IOptimizationMethod<double, double> optimizationMethod;
                if (predictor is ILiftAoADerivativePredictor derivativePredictor)
                    optimizationMethod = FindMaxAoA_NoDerivative(predictor.AerodynamicObjectiveFunc(conditions, pitchInput, -1), guess, tolerance);
                else
                    optimizationMethod = FindMaxAoA_NoDerivative(predictor.AerodynamicObjectiveFunc(conditions, pitchInput, -1), guess, tolerance);
                lift = -(float)optimizationMethod.Value;
                result = (float)optimizationMethod.Solution;
            }
            s_findMax.End();
            return result;
        }
        private static IOptimizationMethod<double, double> FindMaxAoA_NoDerivative(Func<double, double> objectiveFunc, float guess, float tolerance)
        {
            const float lowerBound = 10 * Mathf.Deg2Rad;
            const float midUpperBound = 60 * Mathf.Deg2Rad;
            const float upperBound = 80 * Mathf.Deg2Rad;
            const float stage1 = 5 * Mathf.Deg2Rad;
            const float stage2 = 10 * Mathf.Deg2Rad;
            const float stage3 = 20 * Mathf.Deg2Rad;

            BrentSearch optimizer = new BrentSearch(objectiveFunc, lowerBound, upperBound, tolerance);
            if (float.IsNaN(guess) || float.IsInfinity(guess))
                optimizer.Minimize();
            else
            {
                guess = Mathf.Clamp(guess, -90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);
                (float, float, float) lowers = (guess - stage1, guess - stage2, Mathf.Min(lowerBound, guess - stage3));
                (float, float, float) uppers = (guess + stage1, guess + stage2, Mathf.Clamp(guess + stage3, midUpperBound, upperBound));
                SequentialBrentMinimize(optimizer, lowers, uppers);
            }
            return optimizer;
        }
        private static void SequentialBrentMinimize(BrentSearch optimizer, (float, float, float) lowers, (float, float, float) uppers)
        {
            optimizer.LowerBound = lowers.Item1;
            optimizer.UpperBound = uppers.Item1;
            if (optimizer.Minimize())
                return;

            optimizer.LowerBound = lowers.Item2;
            optimizer.UpperBound = uppers.Item2;
            if (optimizer.Minimize())
                return;

            optimizer.LowerBound = lowers.Item3;
            optimizer.UpperBound = lowers.Item3;
            optimizer.Minimize();

            return;
        }

        public static float FindMinAoA(this AeroPredictor predictor, Conditions conditions, float guess = float.NaN, float tolerance = defaultAoAOptTolerance)
        {
            const float pitchInput = 0;
            s_findMin.Begin();
            IOptimizationMethod<double, double> optimizationMethod;
            if (predictor is ILiftAoADerivativePredictor derivativePredictor)
                optimizationMethod = FindMinAoA_NoDerivative(predictor.AerodynamicObjectiveFunc(conditions, pitchInput), guess, tolerance);
            else
                optimizationMethod = FindMinAoA_NoDerivative(predictor.AerodynamicObjectiveFunc(conditions, pitchInput), guess, tolerance);
            s_findMin.End();
            return (float)optimizationMethod.Solution;
        }
        private static IOptimizationMethod<double, double> FindMinAoA_NoDerivative(Func<double, double> objectiveFunc, float guess, float tolerance)
        {
            const float lowerBound = -80 * Mathf.Deg2Rad;
            const float midLowerBound = -60 * Mathf.Deg2Rad;
            const float upperBound = -10 * Mathf.Deg2Rad;
            const float stage1 = 5 * Mathf.Deg2Rad;
            const float stage2 = 10 * Mathf.Deg2Rad;
            const float stage3 = 20 * Mathf.Deg2Rad;

            BrentSearch optimizer = new BrentSearch(objectiveFunc, lowerBound, upperBound, tolerance);
            if (float.IsNaN(guess) || float.IsInfinity(guess))
                optimizer.Minimize();
            else
            {
                guess = Mathf.Clamp(guess, -90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);
                (float, float, float) lowers = (guess - stage1, guess - stage2, Mathf.Clamp(guess - stage3, midLowerBound, lowerBound));
                (float, float, float) uppers = (guess + stage1, guess + stage2, Mathf.Max(guess + stage3, upperBound));
                SequentialBrentMinimize(optimizer, lowers, uppers);
            }
            return optimizer;
        }

        public static float FindLevelAoA(this AeroPredictor predictor, Conditions conditions, float offsettingForce, float guess = 0, float pitchInput = 0, float tolerance = defaultAoATolerance)
        {
            s_findLevel.Begin();
            if (float.IsNaN(guess) || float.IsInfinity(guess))
                guess = 0;
            else
                guess = Mathf.Clamp(guess, -90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);
            IOptimizationMethod<double, double> optimizationMethod;
            if (predictor is ILiftAoADerivativePredictor derivativePredictor)
                optimizationMethod = FindLevelAoA_NoDerivative(predictor.LevelFlightObjectiveFunc(conditions, offsettingForce, pitchInput), guess, tolerance);
            else
                optimizationMethod = FindLevelAoA_NoDerivative(predictor.LevelFlightObjectiveFunc(conditions, offsettingForce, pitchInput), guess, tolerance);
            s_findLevel.End();
            return (float)optimizationMethod.Solution;
        }
        private static IOptimizationMethod<double, double> FindLevelAoA_NoDerivative(Func<double, double> objectiveFunc, float guess, float tolerance)
        {
            const float lowerBound = -10 * Mathf.Deg2Rad;
            const float upperBound = 35 * Mathf.Deg2Rad;
            const float step = 5 * Mathf.Deg2Rad;
            guess = Mathf.Clamp(guess, -90 * Mathf.Deg2Rad + step, 90 * Mathf.Deg2Rad - step);

            BrentSearch solver = new BrentSearch(objectiveFunc, lowerBound, upperBound, tolerance);
            SequentialBrentSearch(solver, (guess - step, lowerBound), (guess + step, upperBound));
            return solver;
        }
        private static void SequentialBrentSearch(BrentSearch solver, (float, float) lowers, (float, float) uppers)
        {
            solver.LowerBound = lowers.Item1;
            solver.UpperBound = uppers.Item1;
            if (solver.FindRoot())
                return;

            solver.LowerBound = lowers.Item2;
            solver.UpperBound = uppers.Item2;
            solver.FindRoot();

            return;
        }

        public static float FindLevelAoAWithControl(this AeroPredictor predictor, Conditions conditions, float offsettingForce, float aoaGuess = 0, float inputGuess = 0, bool dryTorque = false, float aoaTolerance = defaultAoATolerance, float inputTolerance = defaultInputTolerance)
        {
            if (float.IsNaN(aoaGuess) || float.IsInfinity(aoaGuess))
                aoaGuess = 0;
            else
                aoaGuess = Mathf.Clamp(aoaGuess, -90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);
            if (float.IsNaN(inputGuess) || float.IsInfinity(inputGuess))
                inputGuess = 0;
            else
                inputGuess = Mathf.Clamp01(inputGuess);

            float approxAoA = FindLevelAoA(predictor, conditions, offsettingForce, aoaGuess, aoaTolerance);
            float approxInput = FindStablePitchInput(predictor, conditions, approxAoA, inputGuess, dryTorque, inputTolerance);

            float pitchFun(float aoa) => FindStablePitchInput(predictor, conditions, aoa, approxInput, dryTorque, inputTolerance);
            Func<double, double> objectiveFunc = predictor.LevelFlightObjectiveFunc(conditions, offsettingForce, pitchFun);

            IOptimizationMethod<double, double> optimizationMethod;
            if (predictor is ILiftAoADerivativePredictor derivativePredictor)
                optimizationMethod = FindLevelAoA_NoDerivative(objectiveFunc, approxAoA, aoaTolerance);
            else
                optimizationMethod = FindLevelAoA_NoDerivative(objectiveFunc, approxAoA, aoaTolerance);
            return (float)optimizationMethod.Solution;
        }

        public static float FindStablePitchInput(this AeroPredictor predictor, Conditions conditions, float aoa, float guess = 0, bool dryTorque = false, float tolerance = defaultAoATolerance)
        {
            s_findInput.Begin();
            IOptimizationMethod<double, double> optimizationMethod;
            if (float.IsNaN(guess) || float.IsInfinity(guess))
                guess = 0;
            else
                guess = Mathf.Clamp01(guess);
            optimizationMethod = FindStablePitchInput_NoDerivative(predictor.PitchInputObjectiveFunc(conditions, aoa, dryTorque), guess, tolerance, out bool succeeded);
            s_findInput.End();
            if (succeeded)
                return (float)optimizationMethod.Solution;
            else if (predictor.GetAeroTorque(conditions, aoa, 0, dryTorque).x > 0)
                return -1;
            else
                return 1;
        }
        private static IOptimizationMethod<double, double> FindStablePitchInput_NoDerivative(Func<double, double> objectiveFunc, float guess, float tolerance, out bool succeeded)
        {
            BrentSearch solver = new BrentSearch(objectiveFunc, -1, 1, tolerance);
            SequentialBrentSearch(solver, (Mathf.Clamp01(guess - 0.3f), -1), (Mathf.Clamp01(guess + 0.3f), 1));
            succeeded = solver.Status == BrentSearchStatus.Success;
            return solver;
        }

        public interface ILiftAoADerivativePredictor
        {
            float GetLiftForceMagnitudeAoADerivative(Conditions conditions, float AoA, float pitchInput = 0);
        }

        public interface IDirectAoAMaxProvider
        {
            bool DirectAoAInitialized { get; }
            float GetAoAMax(Conditions conditions);
        }
    }
}
