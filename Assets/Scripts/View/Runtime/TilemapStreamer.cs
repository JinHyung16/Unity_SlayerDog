using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jinhyeong_Common;
using Jinhyeong_Character;

namespace Jinhyeong_View
{
    public class TilemapStreamer : BaseBehaviour
    {
        [Header("Tilemap")]
        [SerializeField] private Tilemap _ground;
        [Tooltip("앞 절반은 서로 다른 색, 뒤 절반은 같은 순서의 좌우 반전. 색은 패치 단위, 반전은 칸 단위로 고른다.")]
        [SerializeField] private TileBase[] _grassTiles;

        [Tooltip("색이 바뀌는 패치 한 변(칸). 칸마다 색을 바꾸면 체커 무늬가 되어 화면에서 가장 시끄러운 요소가 된다.")]
        [SerializeField] private int _patchCells = 4;

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

        [Tooltip("소품 톤 머티리얼. 캐릭터 · 이펙트보다 채도 · 밝기를 낮춰 시선을 뺏지 않게 한다.")]
        [SerializeField] private Material _decoMaterial;

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

        private static int Hash(int x, int z, int salt)
        {
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
            _ground.SetTile(new Vector3Int(x, -z, 0), PickGrass(x, z));

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

            float ox = (Hash(x, z, 4) % 100) / 100f - 0.5f;
            float oz = (Hash(x, z, 5) % 100) / 100f - 0.5f;
            go.transform.position = new Vector3(
                (x + 0.5f + ox * 0.6f) * _cellSize, 0f, (z + 0.5f + oz * 0.6f) * _cellSize);
            go.SetActive(true);
            _decos[key] = go;
        }

        private TileBase PickGrass(int x, int z)
        {
            int colors = Mathf.Max(1, _grassTiles.Length / 2);
            int patch = Mathf.Max(1, _patchCells);
            int px = Mathf.FloorToInt((float)x / patch);
            int pz = Mathf.FloorToInt((float)z / patch);
            int color = Hash(px, pz, 1) % colors;
            int flip = _grassTiles.Length >= colors * 2 ? Hash(x, z, 6) % 2 : 0;
            return _grassTiles[color + colors * flip];
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
            if (_decoMaterial != null)
                sr.sharedMaterial = _decoMaterial;
            go.AddComponent<Jinhyeong_UI.Billboard>();
            return go;
        }
    }
}
