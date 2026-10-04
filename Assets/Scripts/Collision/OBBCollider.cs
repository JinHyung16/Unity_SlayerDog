using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Collision
{
    public class OBBCollider : BaseBehaviour
    {
        [SerializeField] private Vector3 _center = Vector3.zero;
        [SerializeField] private Vector3 _size = new Vector3(1f, 2f, 1f);

        private static readonly List<OBBCollider> _all = new List<OBBCollider>(64);
        public static IReadOnlyList<OBBCollider> All { get { return _all; } }

        public Vector3 Center { get { return _center; } set { _center = value; } }
        public Vector3 Size { get { return _size; } set { _size = value; } }

        public Vector3 WorldCenter { get { return transform.TransformPoint(_center); } }

        protected override void OnEnabled()
        {
            if (_all.Contains(this) == false)
                _all.Add(this);
        }

        protected override void OnDisabled()
        {
            _all.Remove(this);
        }

        public void GetOBBXZ(out Vector2 center, out Vector2 axisX, out float halfX, out Vector2 axisZ, out float halfZ)
        {
            Vector3 wc = transform.TransformPoint(_center);
            center = new Vector2(wc.x, wc.z);

            Vector3 wx = transform.TransformVector(new Vector3(Mathf.Abs(_size.x) * 0.5f, 0f, 0f));
            Vector3 wz = transform.TransformVector(new Vector3(0f, 0f, Mathf.Abs(_size.z) * 0.5f));

            Vector2 wxXZ = new Vector2(wx.x, wx.z);
            Vector2 wzXZ = new Vector2(wz.x, wz.z);
            halfX = wxXZ.magnitude;
            halfZ = wzXZ.magnitude;
            axisX = halfX > 1e-6f ? wxXZ / halfX : new Vector2(1f, 0f);
            axisZ = halfZ > 1e-6f ? wzXZ / halfZ : new Vector2(0f, 1f);
        }

        public void GetXZBounds(out float minX, out float maxX, out float minZ, out float maxZ)
        {
            GetOBBXZ(out Vector2 c, out Vector2 ax, out float hx, out Vector2 az, out float hz);
            float extX = Mathf.Abs(ax.x * hx) + Mathf.Abs(az.x * hz);
            float extZ = Mathf.Abs(ax.y * hx) + Mathf.Abs(az.y * hz);
            minX = c.x - extX;
            maxX = c.x + extX;
            minZ = c.y - extZ;
            maxZ = c.y + extZ;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.12f);
            Gizmos.DrawCube(_center, _size);
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(_center, _size);
            Gizmos.matrix = prev;

            UnityEditor.Handles.color = new Color(1f, 0.5f, 0.3f, 1f);
            Vector3 top = transform.TransformPoint(_center + Vector3.up * (Mathf.Abs(_size.y) * 0.5f + 0.15f));
            UnityEditor.Handles.Label(top, name + " (OBB)");
        }
#endif
    }
}
