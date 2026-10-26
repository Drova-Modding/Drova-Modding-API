using Il2Cpp;
using Il2CppDrova;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Locates a free position in the world for an actor.
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
         */
        public void SetMinMaxRange(Vector2 minMaxRange)
        {
            _minMaxRange = minMaxRange;
        }

        /**
         * <summary>
         * Set the step angle for the actor to spawn.
         * </summary>
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
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            Vector2? result = null;

            for (int i = 0; i < 360 / _stepAngle; i++)
            {
                Vector2 direction = dir.RotatedDegree(i * _stepAngle);
                Vector2 helpOrigin = origin + (direction * _minMaxRange.x);
                Vector2 checkPosition = helpOrigin + (direction * (_minMaxRange.RandomValueBetween() - _minMaxRange.x));
                if (IsPositionFree(helpOrigin, checkPosition, out Vector2 curTarget))
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
            int raycastMask = RaycastUtil.GetSmallObstacleMask() | RaycastUtil.ObstacleLayer.Mask;
            int hit = Physics2D.CircleCastNonAlloc(position, 12.0f, Vector2.zero, _circleHits, 0.0f, raycastMask);

            if (hit == 0)
            {
                Vector2 dir = position - origin;
                dir = Vec2Util.Normalized(dir, out float magnitude);
                int rHit = Physics2D.RaycastNonAlloc(origin, dir, _directionHits, magnitude, raycastMask);

                if (rHit == 0)
                {
                    target = position;
                    return true;
                }
                else
                {
                    target = _directionHits[0].point;
                    int safetyHits = Physics2D.CircleCastNonAlloc(target, 12.0f, Vector2.zero, _circleHits, 0.0f, raycastMask);

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
    }
}
