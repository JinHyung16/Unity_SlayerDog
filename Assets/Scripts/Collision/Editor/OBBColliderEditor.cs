using UnityEditor;
using UnityEngine;
using Jinhyeong_Collision;

namespace Jinhyeong_Collision.Editor
{
    [CustomEditor(typeof(OBBCollider))]
    [CanEditMultipleObjects]
    public class OBBColliderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "컴포넌트가 부착된 오브젝트의 트랜스폼의 회전·스케일이 자동 반영된다. " +
                "Sync를 누르면 메시의 로컬 바운즈를 콜라이더 로컬 공간으로 모아 Center/Size에 넣는다(Size엔 스케일 이전의 순수 로컬 크기). " +
                "충돌은 XZ 평면에서 원 vs 회전 사각형으로 판정된다.",
                MessageType.Info);

            if (GUILayout.Button("Sync to Mesh Bounds (local)"))
            {
                foreach (Object obj in targets)
                {
                    OBBCollider col = obj as OBBCollider;
                    if (col != null)
                        SyncToMeshBounds(col);
                }
            }
        }

        private static void SyncToMeshBounds(OBBCollider col)
        {
            Transform root = col.transform;
            Bounds local = new Bounds();
            bool hasBounds = false;

            MeshFilter[] filters = col.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter mf = filters[i];
                if (mf == null || mf.sharedMesh == null)
                    continue;
                Accumulate(root, mf.transform, mf.sharedMesh.bounds, ref local, ref hasBounds);
            }

            SkinnedMeshRenderer[] skinned = col.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < skinned.Length; i++)
            {
                SkinnedMeshRenderer smr = skinned[i];
                if (smr == null)
                    continue;
                Accumulate(root, smr.transform, smr.localBounds, ref local, ref hasBounds);
            }

            if (hasBounds == false)
            {
                Debug.LogWarning($"[OBBCollider] '{col.name}': 하위에 Mesh/SkinnedMesh가 없어 Sync 불가", col);
                return;
            }

            Undo.RecordObject(col, "Sync OBB to Mesh Bounds");
            col.Center = local.center;
            col.Size = local.size;
            EditorUtility.SetDirty(col);
        }

        private static void Accumulate(Transform root, Transform child, Bounds childLocal, ref Bounds acc, ref bool has)
        {
            Vector3 c = childLocal.center;
            Vector3 e = childLocal.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 world = child.TransformPoint(corner);
                Vector3 rootLocal = root.InverseTransformPoint(world);
                if (has == false)
                {
                    acc = new Bounds(rootLocal, Vector3.zero);
                    has = true;
                }
                else
                {
                    acc.Encapsulate(rootLocal);
                }
            }
        }
    }
}
