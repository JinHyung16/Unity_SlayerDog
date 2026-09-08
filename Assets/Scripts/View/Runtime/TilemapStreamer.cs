using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jinhyeong_Common;
using Jinhyeong_Character;

namespace Jinhyeong_View
{
    /// <summary>
    /// 플레이어 주변만 타일을 채우는 무한 바닥.
    ///
    /// 왜 이렇게 하나: 뱀서류는 12분 동안 한 방향으로만 수천 유닛을 갈 수 있다.
    /// 그만한 타일맵을 미리 깔면 메모리·컬링 비용만 나가고, 반대로 반복 텍스처 한 장으로 때우면
    /// 격자 패턴이 그대로 눈에 띈다. **보이는 창만 채우고 따라 움직이는** 게 답이다.
    ///
    /// 핵심은 **결정적 해시**다. 칸마다 (x,z) 해시로 타일 종류를 정하면, 창이 지나갔다가
    /// 돌아와도 같은 그림이 나온다. 난수로 채우면 되돌아올 때마다 바닥이 바뀌어 어지럽다.
    ///
    /// 장식(바위·나무)은 타일로 깔지 않는다 — 원본 아트가 측면 뷰라 바닥에 눕히면 뭉개진다.
    /// 캐릭터와 같이 세워서(빌보드) 배치한다.
    /// </summary>
    public class TilemapStreamer : BaseBehaviour
    {
        [Header("Tilemap")]
        [SerializeField] private Tilemap _ground;
        [SerializeField] private TileBase[] _grassTiles;

        [Tooltip("칸 하나의 월드 크기. 타일 스프라이트 크기(64px)/PPU(32) 와 반드시 같아야 이음매가 안 생긴다.")]
        [SerializeField] private float _cellSize = 2f;

        [Tooltip("플레이어 기준 반경(칸). 화면(세로 ~18유닛)보다 넉넉해야 가장자리가 안 보인다.")]
        [SerializeField] private int _radius = 16;

        [Header("Decoration")]
        [SerializeField] private Sprite[] _decoSprites;

        [Tooltip("칸당 장식이 놓일 확률. 너무 높으면 이동을 방해하는 것처럼 보인다.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _decoChance = 0.035f;

        [SerializeField] private int _decoSortingOrder = 8;

        private Transform _target;
        private bool _hasCell;
        private int _cellX;
        private int _cellZ;

        private readonly Dictionary<long, GameObject> _decos = new Dictionary<long, GameObject>(256);
        private readonly Stack<GameObject> _decoPool = new Stack<GameObject>(64);
        private Transform _decoRoot;

        private static long Key(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }

        /// <summary>칸 좌표로만 결정되는 해시. 같은 칸은 언제 와도 같은 값이라 그림이 안 흔들린다.</summary>
        private static int Hash(int x, int z, int salt)
        {
            // [사고] 곱하고 XOR 한 뒤 한 번만 섞었더니 하위 비트가 x 변화에 둔감해,
            // 타일 선택이 가로 띠로 뭉쳤다. 눈에 아주 잘 띈다. 제대로 뒤섞어야 한다.
            unchecked
            {
                uint h = (uint)(x * 0x27d4eb2d) ^ (uint)(z * 0x165667b1) ^ (uint)(salt * 0x9e3779b1);
                h ^= h >> 15;
                h *= 0x2c1b3c6d;
                h ^= h >> 12;
                h *= 0x297a2d39;
                h ^= h >> 15;
                return (int)(h & 0x7FFFFFFF);
            }
        }

        private void Awake()
        {
            if (_ground == null)
                _ground = GetComponentInChildren<Tilemap>(true);

            if (_decoRoot == null)
            {
                GameObject root = new GameObject("Decor");
                root.transform.SetParent(transform, false);
                _decoRoot = root.transform;
            }
        }

        protected override void OnDisabled()
        {
            _hasCell = false;
        }

        private void LateUpdate()
        {
            if (_ground == null || _grassTiles == null || _grassTiles.Length == 0)
                return;

            if (_target == null)
            {
                Player p = Jinhyeong_Managers.GameEvents.CurrentPlayer;
                if (p == null)
                    return;
                _target = p.transform;
            }

            Vector3 pos = _target.position;
            int cx = Mathf.FloorToInt(pos.x / _cellSize);
            int cz = Mathf.FloorToInt(pos.z / _cellSize);

            if (_hasCell && cx == _cellX && cz == _cellZ)
                return;

            if (_hasCell == false)
            {
                FillRect(cx - _radius, cz - _radius, cx + _radius, cz + _radius);
            }
            else
            {
                // 창이 한 칸 움직이면 들어온 줄만 채우고 나간 줄만 지운다.
                // 매번 전부 다시 깔면 초당 수천 번 SetTile 이 돌아 프레임이 끊긴다.
                ShiftWindow(_cellX, _cellZ, cx, cz);
            }

            _cellX = cx;
            _cellZ = cz;
            _hasCell = true;
        }

        private void ShiftWindow(int oldX, int oldZ, int newX, int newZ)
        {
            int oldMinX = oldX - _radius, oldMaxX = oldX + _radius;
            int oldMinZ = oldZ - _radius, oldMaxZ = oldZ + _radius;
            int newMinX = newX - _radius, newMaxX = newX + _radius;
            int newMinZ = newZ - _radius, newMaxZ = newZ + _radius;

            // 멀리 순간이동했으면 겹치는 부분이 없다 — 통째로 다시 깐다.
            if (newMinX > oldMaxX || newMaxX < oldMinX || newMinZ > oldMaxZ || newMaxZ < oldMinZ)
            {
                ClearRect(oldMinX, oldMinZ, oldMaxX, oldMaxZ);
                FillRect(newMinX, newMinZ, newMaxX, newMaxZ);
                return;
            }

            for (int x = newMinX; x <= newMaxX; x++)
            {
                for (int z = newMinZ; z <= newMaxZ; z++)
                {
                    if (x >= oldMinX && x <= oldMaxX && z >= oldMinZ && z <= oldMaxZ)
                        continue;
                    SetCell(x, z);
                }
            }

            for (int x = oldMinX; x <= oldMaxX; x++)
            {
                for (int z = oldMinZ; z <= oldMaxZ; z++)
                {
                    if (x >= newMinX && x <= newMaxX && z >= newMinZ && z <= newMaxZ)
                        continue;
                    ClearCell(x, z);
                }
            }
        }

        private void FillRect(int minX, int minZ, int maxX, int maxZ)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                    SetCell(x, z);
            }
        }

        private void ClearRect(int minX, int minZ, int maxX, int maxZ)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                    ClearCell(x, z);
            }
        }

        private void SetCell(int x, int z)
        {
            // Grid 를 -90° 눕혔으므로 셀의 +y 는 월드 -z 로 간다. 부호를 맞춰야
            // 타일 위치와 장식 위치가 어긋나지 않는다.
            _ground.SetTile(new Vector3Int(x, -z, 0), _grassTiles[Hash(x, z, 1) % _grassTiles.Length]);

            if (_decoSprites == null || _decoSprites.Length == 0)
                return;
            if (Hash(x, z, 2) % 1000 >= _decoChance * 1000f)
                return;

            long key = Key(x, z);
            if (_decos.ContainsKey(key))
                return;

            GameObject go = _decoPool.Count > 0 ? _decoPool.Pop() : CreateDeco();
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = _decoSprites[Hash(x, z, 3) % _decoSprites.Length];

            // 칸 안에서 살짝 흩어 놓는다. 격자에 정확히 맞으면 인공적으로 보인다.
            float ox = (Hash(x, z, 4) % 100) / 100f - 0.5f;
            float oz = (Hash(x, z, 5) % 100) / 100f - 0.5f;
            go.transform.position = new Vector3(
                (x + 0.5f + ox * 0.6f) * _cellSize, 0f, (z + 0.5f + oz * 0.6f) * _cellSize);
            go.SetActive(true);
            _decos[key] = go;
        }

        private void ClearCell(int x, int z)
        {
            _ground.SetTile(new Vector3Int(x, -z, 0), null);

            long key = Key(x, z);
            if (_decos.TryGetValue(key, out GameObject go) == false)
                return;
            _decos.Remove(key);
            if (go == null)
                return;
            go.SetActive(false);
            _decoPool.Push(go);
        }

        private GameObject CreateDeco()
        {
            GameObject go = new GameObject("Deco");
            go.transform.SetParent(_decoRoot, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = _decoSortingOrder;
            go.AddComponent<Jinhyeong_UI.Billboard>();
            return go;
        }
    }
}
