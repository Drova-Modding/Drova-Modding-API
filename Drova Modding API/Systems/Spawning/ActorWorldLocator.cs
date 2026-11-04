using Il2Cpp;
using Il2CppDrova;
using Il2CppPathfinding;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Locates a free position in the world for an actor.
    /// </summary>
    public class ActorWorldLocator
    {
        /// <summary>Graph nodes lie up to about 40 from a player in a forest, so a start further from the first is off the graph.</summary>
        private const float MaxStartGap = 100f;

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
                    result = AlongPath(origin, curTarget);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// The spot on the walking path from <paramref name="origin"/> to <paramref name="position"/> that lies as far along
        /// it as <paramref name="position"/> is in a straight line. The free checks only look past the minimum range, so a
        /// spot behind a forest or a cliff passed, and creatures spawned there stood at their chase limit.
        /// The path graph covers only the area around this machine's player, so a spot off it is kept as it is.
        /// </summary>
        private Vector2 AlongPath(Vector2 origin, Vector2 position)
        {
            ABPath path = ABPath.Construct(new Vector3(origin.x, origin.y, 0f), new Vector3(position.x, position.y, 0f));
            AstarPath.StartPath(path);
            AstarPath.BlockUntilCalculated(path);
            if (path.error || path.vectorPath == null || path.vectorPath.Count == 0) return position;

            // A start off the graph snaps to its nearest node, which may lie anywhere toward this machine's player.
            Vector3 first = path.vectorPath[0];
            if ((new Vector2(first.x, first.y) - origin).sqrMagnitude > MaxStartGap * MaxStartGap) return position;

            float budget = Mathf.Min(Vector2.Distance(origin, position), _minMaxRange.y);
            Vector2 previous = origin;
            foreach (Vector3 point in path.vectorPath)
            {
                Vector2 flat = new(point.x, point.y);
                float step = (flat - previous).magnitude;
                if (step >= budget) return previous + ((flat - previous) * (budget / step));

                budget -= step;
                previous = flat;
            }

            return previous;
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
