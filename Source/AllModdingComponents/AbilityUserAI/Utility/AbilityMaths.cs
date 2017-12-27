using System.Collections.Generic;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-11-04
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Lightweights Maths bookcase tailored for doing Mathematics for Abilities..
    /// </summary>
    public static class AbilityMaths
    {
        /// <summary>
        ///     Calculates the Target with most intersections radially.
        /// </summary>
        /// <param name="targets">Supplied collection of targets to go through.</param>
        /// <returns>Target with most intersections.</returns>
        public static LocalTargetInfo PickMostRadialIntersectingTarget(IEnumerable<LocalTargetInfo> targets,
            float radius)
        {
            var targetList = new List<LocalTargetInfo>(targets);

            if (targetList.Count == 0)
                return LocalTargetInfo.Invalid;

            var highestIntersecting = LocalTargetInfo.Invalid;
            var highestIntersections = 0;

            //First Target
            foreach (var targetA in targetList)
            {
                var intersections = 0;

                //Second Target
                foreach (var targetB in targetList)
                    //If the circles overlap then we intersect.
                    if (CircleIntersectionTest(targetB.Cell.x, targetB.Cell.y, 1f, targetA.Cell.x, targetA.Cell.y,
                        radius))
                        intersections++;

                if (intersections > highestIntersections)
                {
                    highestIntersecting = targetA;
                    highestIntersections = intersections;
                }
            }

            return highestIntersecting;
        }

        /// <summary>
        ///     Simple Circle to Circle intersection test.
        /// </summary>
        /// <param name="x0">Circle 0 X-position</param>
        /// <param name="y0">Circle 0 Y-position</param>
        /// <param name="radius0">Circle 0 Radius</param>
        /// <param name="x1">Circle 1 X-position</param>
        /// <param name="y1">Circle 1 Y-position</param>
        /// <param name="radius1">Circle 1 Radius</param>
        /// <returns>True if a intersection occured. False if not.</returns>
        public static bool CircleIntersectionTest(float x0, float y0, float radius0, float x1, float y1, float radius1)
        {
            var radiusSum = radius0 * radius0 + radius1 * radius1;
            var distance = (x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0);

            //Intersection occured.
            if (distance <= radiusSum)
                return true;

            //No intersection.
            return false;
        }
    }
}