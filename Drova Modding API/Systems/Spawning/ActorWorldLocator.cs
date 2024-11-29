using Il2Cpp;
using Il2CppDrova;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    ///  A class that helps to locate a free position in the world for an actor.
    ///  Class is copy pasted from <see cref="Il2CppDrova.Spawners.ActorWorldRasterizerWorldLocator"/> 
    /// </summary>
    public class ActorWorldLocator
    {
        private Vector2 _minMaxRange = new(50.0f, 150.0f);
        private float _stepAngle = 10.0f;
        private readonly RaycastHit2D[] _circleHits = new RaycastHit2D[1];
        private readonly RaycastHit2D[] _directionHits = new RaycastHit2D[1];

        /**
         * <summary>
         * Set the min and max range for the actor to spawn.
         * </summary>
         * <param name="minMaxRange">The min and max range for the actor to spawn</param>
         */
        public void SetMinMaxRange(Vector2 minMaxRange)
        {
            _minMaxRange = minMaxRange;
        }

        /**
         * <summary>
         * Set the step angle for the actor to spawn.
         * </summary>
         * <param name="stepAngle">The step angle for the actor to spawn</param>
         */
        public void SetStepAngle(float stepAngle)
        {
            _stepAngle = stepAngle;
        }

        /// <summary>
        /// Gets a random free position in the world.
        /// </summary>
        /// <param name="origin">To look from, mostly this should be the player position</param>
        /// <returns>Null when nothing found</returns>
        public Vector2? GetRandomFreePosition(Vector2 origin)
        {
            var dir = UnityEngine.Random.insideUnitCircle.normalized;
            Vector2? result = null;

            for (int i = 0; i < 360 / _stepAngle; i++)
            {
                var direction = dir.RotatedDegree(i * _stepAngle);
                var helpOrigin = origin + direction * _minMaxRange.x;
                var checkPosition = helpOrigin + direction * (_minMaxRange.RandomValueBetween() - _minMaxRange.x);
                if (IsPositionFree(helpOrigin, checkPosition, out var curTarget))
                {
                    result = curTarget;
                    break;
                }
            }

            return result;
        }

        private bool IsPositionFree(Vector2 origin, Vector2 position, out Vector2 target)
        {
            return IsPositionFreeObstacle(origin, position, out target);
        }

        private bool IsPositionFreeObstacle(Vector2 origin, Vector2 position, out Vector2 target)
        {
            var raycastMask = RaycastUtil.GetSmallObstacleMask() | RaycastUtil.ObstacleLayer.Mask;
            //check if target position is free
            var hit = Physics2D.CircleCastNonAlloc(position, 12.0f, Vector2.zero, _circleHits, 0.0f, raycastMask);

            if (hit == 0)
            {
                //check if line from origin to target position is free:
                var dir = position - origin;
                dir = Vec2Util.Normalized(dir, out var magnitude);
                var rHit = Physics2D.RaycastNonAlloc(origin, dir, _directionHits, magnitude, raycastMask);

                if (rHit == 0)
                {
                    target = position;
                    return true;
                }
                else
                {
                    //spawn at collision point from origin to position
                    target = _directionHits[0].point;
                    var safetyHits = Physics2D.CircleCastNonAlloc(target, 12.0f, Vector2.zero, _circleHits, 0.0f, raycastMask);

                    if (safetyHits == 0)
                    {
                        return true;
                    }

                    return false;
                }
            }
            target = position;
            return false;
        }

        //private bool IsPathfindingReachable(Vector2 origin, Vector2 position)
        //{
        //    if (AstarPath.active != null && AstarPath.active.graphs.Length > 0 && AstarPath.active.graphs[0] is GridGraph gridGraph)
        //    {
        //        var originGraphPoint = gridGraph.transform.InverseTransform(origin);
        //        var targetGraphPoint = gridGraph.transform.InverseTransform(position);
        //        var originNode = gridGraph.GetNode((int)originGraphPoint.x, (int)originGraphPoint.z);
        //        var targetNode = gridGraph.GetNode((int)targetGraphPoint.x, (int)targetGraphPoint.z);
        //        if (originNode != null && targetNode != null)
        //        {
        //            return PathUtilities.IsPathPossible(originNode, targetNode) && targetNode.Walkable;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        return true;
        //    }
        //}
    }
}
