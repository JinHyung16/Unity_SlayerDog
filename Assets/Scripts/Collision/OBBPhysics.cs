using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Collision
{
    public static class OBBPhysics
    {
        public static bool OverlapsXZ(Vector3 pos, float radius, OBBCollider ignore = null)
        {
            IReadOnlyList<OBBCollider> boxes = OBBCollider.All;
            float rSq = radius * radius;
            for (int i = 0; i < boxes.Count; i++)
            {
                OBBCollider b = boxes[i];
                if (b == null || b == ignore)
                    continue;

                b.GetOBBXZ(out Vector2 c, out Vector2 axisX, out float halfX, out Vector2 axisZ, out float halfZ);
                float dx = pos.x - c.x;
                float dz = pos.z - c.y;
                float localX = dx * axisX.x + dz * axisX.y;
                float localZ = dx * axisZ.x + dz * axisZ.y;

                float clampedX = Mathf.Clamp(localX, -halfX, halfX);
                float clampedZ = Mathf.Clamp(localZ, -halfZ, halfZ);

                float ex = localX - clampedX;
                float ez = localZ - clampedZ;
                if (ex * ex + ez * ez <= rSq)
                    return true;
            }
            return false;
        }

        public static Vector3 ResolvePlanarMove(Vector3 current, Vector3 planarDelta, float radius, OBBCollider ignore = null)
        {
            Vector3 result = Vector3.zero;

            Vector3 tryX = current + new Vector3(planarDelta.x, 0f, 0f);
            if (OverlapsXZ(tryX, radius, ignore) == false)
                result.x = planarDelta.x;

            Vector3 afterX = current + new Vector3(result.x, 0f, 0f);
            Vector3 tryZ = afterX + new Vector3(0f, 0f, planarDelta.z);
            if (OverlapsXZ(tryZ, radius, ignore) == false)
                result.z = planarDelta.z;

            return result;
        }

        public static bool SegmentBlockedXZ(Vector3 from, Vector3 to, float radius, OBBCollider ignore = null)
        {
            const int steps = 6;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 p = Vector3.Lerp(from, to, t);
                if (OverlapsXZ(p, radius, ignore))
                    return true;
            }
            return false;
        }
    }
}
