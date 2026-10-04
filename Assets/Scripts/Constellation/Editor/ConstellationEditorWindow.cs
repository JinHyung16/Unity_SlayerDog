using System.Collections.Generic;
using System.IO;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_Constellation.Editor
{

    public class ConstellationEditorWindow : EditorWindow
    {
        private enum EDragMode
        {
            None,
            Panning,
            MovingNodes,
            Linking
        }

        private const float MinZoom = 0.12f;
        private const float MaxZoom = 2.0f;
        private const float InspectorWidth = 300f;

        private readonly ConstellationGraph _graph = new ConstellationGraph();

        private Vector2 _pan;
        private float _zoom = 0.3f;
        private EDragMode _drag;
        private Vector2 _dragOriginGraph;

        private int _selected;
        private int _linkFrom;
        private readonly List<int> _movingWith = new List<int>(64);
        private bool _dragSubtree;

        private int _gridSize = 20;
        private bool _snapToGrid = true;
        private bool _loaded;
        private Vector2 _inspectorScroll;
        private ConstellationValidator.Result _lastResult;

        [MenuItem("Jinhyeong/성좌 편집기", false, 21)]
        public static void Open()
        {
            ConstellationEditorWindow w = GetWindow<ConstellationEditorWindow>("성좌 편집기");
            w.minSize = new Vector2(900f, 600f);
            w.Show();
        }

        private void OnEnable()
        {
            _loaded = false;
        }

        private void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }
            _loaded = true;

            if (PartRegistry.Count == 0)
            {
                PartRegistry.Load();
            }
            if (ConstellationRegionRegistry.Count == 0)
            {
                ConstellationRegionRegistry.Load();
            }

            if (LoadFromSource() == false)
            {
                ConstellationGenerator.Build(_graph);
            }
            FitView();
        }

        private void OnGUI()
        {
            EnsureLoaded();

            DrawToolbar();

            Rect canvas = new Rect(0f, EditorStyles.toolbar.fixedHeight,
                position.width - InspectorWidth, position.height - EditorStyles.toolbar.fixedHeight);
            Rect inspector = new Rect(canvas.xMax, canvas.y, InspectorWidth, canvas.height);

            HandleCanvasInput(canvas);
            DrawCanvas(canvas);
            DrawInspector(inspector);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("초안 굽기", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                if (EditorUtility.DisplayDialog("초안 굽기",
                        "지금 편집 중인 성좌를 버리고 시드에서 새로 만듭니다.\n손댄 것은 되돌릴 수 없습니다.",
                        "새로 만든다", "그만둔다"))
                {
                    ConstellationGenerator.Build(_graph);
                    _selected = 0;
                    _lastResult = null;
                    FitView();
                }
            }

            if (GUILayout.Button("불러오기", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                if (LoadFromSource() == false)
                {
                    ShowNotification(new GUIContent("저장된 성좌가 없다"));
                }
                else
                {
                    FitView();
                }
            }

            if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(50f)))
            {
                Save();
            }

            if (GUILayout.Button("검증", EditorStyles.toolbarButton, GUILayout.Width(50f)))
            {
                _lastResult = ConstellationValidator.Validate(_graph);
            }

            GUILayout.Space(12f);

            _snapToGrid = GUILayout.Toggle(_snapToGrid, "격자", EditorStyles.toolbarButton, GUILayout.Width(40f));
            _gridSize = Mathf.Max(1, EditorGUILayout.IntField(_gridSize, EditorStyles.toolbarTextField, GUILayout.Width(36f)));

            if (GUILayout.Button("전부 정렬", EditorStyles.toolbarButton, GUILayout.Width(64f)))
            {
                for (int i = 0; i < _graph.Nodes.Count; i++)
                {
                    Snap(_graph.Nodes[i]);
                }
            }

            GUILayout.Space(12f);
            _dragSubtree = GUILayout.Toggle(_dragSubtree, "가지째 끌기", EditorStyles.toolbarButton, GUILayout.Width(80f));

            GUILayout.FlexibleSpace();
            GUILayout.Label($"노드 {_graph.Nodes.Count} · 간선 {_graph.EdgeCount} · 값 {ConstellationValidator.TotalCost(_graph)}");

            if (GUILayout.Button("화면 맞춤", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                FitView();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCanvas(Rect canvas)
        {
            GUI.BeginClip(canvas);
            Rect local = new Rect(0f, 0f, canvas.width, canvas.height);
            EditorGUI.DrawRect(local, new Color(0.10f, 0.11f, 0.15f, 1f));

            Handles.BeginGUI();

            for (int i = 0; i < _graph.EdgeCount; i++)
            {
                ConstellationNode a = _graph.Get(_graph.EdgeA[i]);
                ConstellationNode b = _graph.Get(_graph.EdgeB[i]);
                if (a == null || b == null)
                {
                    continue;
                }
                bool touched = a.Id == _selected || b.Id == _selected;
                Handles.color = touched ? new Color(1f, 0.82f, 0.38f, 1f) : new Color(0.34f, 0.37f, 0.48f, 1f);
                Handles.DrawAAPolyLine(touched ? 3f : 1.5f, GraphToScreen(a), GraphToScreen(b));
            }

            if (_drag == EDragMode.Linking)
            {
                ConstellationNode from = _graph.Get(_linkFrom);
                if (from != null)
                {
                    Handles.color = new Color(0.45f, 0.95f, 0.6f, 1f);
                    Handles.DrawAAPolyLine(2.5f, GraphToScreen(from), Event.current.mousePosition);
                }
            }

            Handles.EndGUI();

            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                ConstellationNode n = _graph.Nodes[i];
                Rect box = NodeBox(n);
                if (box.xMax < 0f || box.yMax < 0f || box.xMin > local.width || box.yMin > local.height)
                {
                    continue;
                }

                EditorGUI.DrawRect(box, ConstellationPaletteEditor.ColorOf(n));
                if (n.Id == _selected)
                {
                    Handles.BeginGUI();
                    Handles.color = Color.white;
                    Handles.DrawAAPolyLine(2f,
                        new Vector3(box.xMin, box.yMin), new Vector3(box.xMax, box.yMin),
                        new Vector3(box.xMax, box.yMax), new Vector3(box.xMin, box.yMax),
                        new Vector3(box.xMin, box.yMin));
                    Handles.EndGUI();
                }

                if (_zoom >= 0.45f && ConstellationPaletteEditor.HasLabel(n.Type))
                {
                    Rect label = new Rect(box.xMin - 60f, box.yMax + 1f, box.width + 120f, 14f);
                    GUI.Label(label, n.DisplayName, ConstellationPaletteEditor.LabelStyle);
                }
            }

            GUI.EndClip();

            GUI.Label(new Rect(canvas.xMin + 8f, canvas.yMax - 38f, 620f, 34f),
                "좌클릭 선택·끌기   Shift+클릭 잇기/끊기   Ctrl+클릭 새 노드   Del 삭제\n" +
                "가운데 버튼 화면 이동   휠 확대   (배율 " + _zoom.ToString("0.00") + ")",
                EditorStyles.miniLabel);
        }

        private void HandleCanvasInput(Rect canvas)
        {
            Event e = Event.current;
            if (canvas.Contains(e.mousePosition) == false && _drag == EDragMode.None)
            {
                return;
            }

            Vector2 localMouse = e.mousePosition - new Vector2(canvas.x, canvas.y);

            if (e.type == EventType.ScrollWheel)
            {
                ZoomAt(localMouse, e.delta.y < 0f ? 1.12f : 1f / 1.12f);
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDown)
            {
                if (e.button == 2)
                {
                    _drag = EDragMode.Panning;
                    e.Use();
                    return;
                }
                if (e.button == 0)
                {
                    ConstellationNode hit = Pick(localMouse);

                    if (e.shift)
                    {
                        HandleShiftClick(hit);
                        e.Use();
                        Repaint();
                        return;
                    }
                    if (e.control || e.command)
                    {
                        CreateNodeAt(ScreenToGraph(localMouse));
                        e.Use();
                        Repaint();
                        return;
                    }

                    _selected = hit != null ? hit.Id : 0;
                    if (hit != null)
                    {
                        _drag = EDragMode.MovingNodes;
                        _dragOriginGraph = ScreenToGraph(localMouse);
                        _movingWith.Clear();
                        if (_dragSubtree)
                        {
                            CollectSubtree(hit.Id, _movingWith);
                        }
                    }
                    e.Use();
                    Repaint();
                    return;
                }
            }

            if (e.type == EventType.MouseDrag)
            {
                if (_drag == EDragMode.Panning)
                {
                    _pan += e.delta / _zoom;
                    e.Use();
                    Repaint();
                    return;
                }
                if (_drag == EDragMode.MovingNodes && _selected != 0)
                {
                    Vector2 now = ScreenToGraph(localMouse);
                    Vector2 delta = now - _dragOriginGraph;
                    _dragOriginGraph = now;

                    MoveNode(_graph.Get(_selected), delta);
                    for (int i = 0; i < _movingWith.Count; i++)
                    {
                        MoveNode(_graph.Get(_movingWith[i]), delta);
                    }
                    e.Use();
                    Repaint();
                    return;
                }
            }

            if (e.type == EventType.MouseUp)
            {
                if (_drag == EDragMode.MovingNodes && _snapToGrid)
                {
                    Snap(_graph.Get(_selected));
                    for (int i = 0; i < _movingWith.Count; i++)
                    {
                        Snap(_graph.Get(_movingWith[i]));
                    }
                }
                if (_drag != EDragMode.Linking)
                {
                    _drag = EDragMode.None;
                }
                _movingWith.Clear();
                Repaint();
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && _selected != 0)
            {
                DeleteNode(_selected);
                e.Use();
                Repaint();
            }
        }

        private void HandleShiftClick(ConstellationNode hit)
        {
            if (hit == null)
            {
                _drag = EDragMode.None;
                _linkFrom = 0;
                return;
            }

            if (_drag != EDragMode.Linking)
            {
                _drag = EDragMode.Linking;
                _linkFrom = hit.Id;
                _selected = hit.Id;
                return;
            }

            if (_linkFrom != hit.Id)
            {
                if (AreLinked(_linkFrom, hit.Id))
                {
                    Unlink(_linkFrom, hit.Id);
                }
                else
                {
                    _graph.Link(_linkFrom, hit.Id);
                }
            }
            _drag = EDragMode.None;
            _linkFrom = 0;
        }

        private void MoveNode(ConstellationNode n, Vector2 delta)
        {
            if (n == null)
            {
                return;
            }
            n.X += delta.x;
            n.Y += delta.y;
        }

        private void Snap(ConstellationNode n)
        {
            if (n == null || _gridSize <= 0)
            {
                return;
            }
            n.X = Mathf.Round(n.X / _gridSize) * _gridSize;
            n.Y = Mathf.Round(n.Y / _gridSize) * _gridSize;
        }

        private void CollectSubtree(int rootId, List<int> target)
        {
            HashSet<int> seen = new HashSet<int> { rootId };
            List<int> queue = new List<int> { rootId };
            for (int head = 0; head < queue.Count; head++)
            {
                ConstellationNode n = _graph.Get(queue[head]);
                if (n == null)
                {
                    continue;
                }
                for (int i = 0; i < n.Neighbors.Count; i++)
                {
                    if (seen.Add(n.Neighbors[i]) == false)
                    {
                        continue;
                    }
                    queue.Add(n.Neighbors[i]);
                    target.Add(n.Neighbors[i]);
                }
            }
        }

        private void CreateNodeAt(Vector2 graphPos)
        {
            ConstellationNode n = new ConstellationNode();
            n.Id = NextId();
            n.Type = EConstellationNodeType.Small;
            n.Family = _graph.Get(_selected) != null ? _graph.Get(_selected).Family : ESkillFamily.None;
            n.X = graphPos.x;
            n.Y = graphPos.y;
            n.Cost = CommonConfig.Constellation.CostSmall;
            n.MaxLevel = 1;
            n.Trait = EPartTrait.Damage;
            n.Value = 0.02f;
            ConstellationNaming.Apply(n);
            _graph.Add(n);

            if (_selected != 0)
            {
                _graph.Link(_selected, n.Id);
            }
            if (_snapToGrid)
            {
                Snap(n);
            }
            _selected = n.Id;
        }

        private int NextId()
        {
            int max = 0;
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                if (_graph.Nodes[i].Id > max)
                {
                    max = _graph.Nodes[i].Id;
                }
            }
            return max + 1;
        }

        private void DeleteNode(int id)
        {
            ConstellationNode n = _graph.Get(id);
            if (n == null || n.Type == EConstellationNodeType.Core)
            {
                return;
            }
            Rebuild(id, -1, -1);
            _selected = 0;
        }

        private bool AreLinked(int a, int b)
        {
            ConstellationNode na = _graph.Get(a);
            return na != null && na.Neighbors.Contains(b);
        }

        private void Unlink(int a, int b)
        {
            Rebuild(0, a, b);
        }

        private void Rebuild(int dropNodeId, int cutA, int cutB)
        {
            List<ConstellationNode> nodes = new List<ConstellationNode>(_graph.Nodes);
            List<int> ea = new List<int>(_graph.EdgeA);
            List<int> eb = new List<int>(_graph.EdgeB);

            _graph.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == dropNodeId)
                {
                    continue;
                }
                nodes[i].Neighbors.Clear();
                _graph.Add(nodes[i]);
            }
            for (int i = 0; i < ea.Count; i++)
            {
                if (ea[i] == dropNodeId || eb[i] == dropNodeId)
                {
                    continue;
                }
                if ((ea[i] == cutA && eb[i] == cutB) || (ea[i] == cutB && eb[i] == cutA))
                {
                    continue;
                }
                _graph.Link(ea[i], eb[i]);
            }
        }

        private Vector2 GraphToScreen(ConstellationNode n)
        {
            return GraphToScreen(new Vector2(n.X, n.Y));
        }

        private Vector2 GraphToScreen(Vector2 g)
        {
            return new Vector2((g.x + _pan.x) * _zoom, (-g.y + _pan.y) * _zoom);
        }

        private Vector2 ScreenToGraph(Vector2 s)
        {
            return new Vector2(s.x / _zoom - _pan.x, -(s.y / _zoom - _pan.y));
        }

        private Rect NodeBox(ConstellationNode n)
        {
            float size = Mathf.Max(8f, ConstellationPaletteEditor.SizeOf(n.Type) * _zoom);
            Vector2 c = GraphToScreen(n);
            return new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size);
        }

        private ConstellationNode Pick(Vector2 localMouse)
        {
            for (int i = _graph.Nodes.Count - 1; i >= 0; i--)
            {
                if (NodeBox(_graph.Nodes[i]).Contains(localMouse))
                {
                    return _graph.Nodes[i];
                }
            }
            return null;
        }

        private void ZoomAt(Vector2 localMouse, float factor)
        {
            float before = _zoom;
            _zoom = Mathf.Clamp(_zoom * factor, MinZoom, MaxZoom);
            if (Mathf.Approximately(before, _zoom))
            {
                return;
            }
            Vector2 underCursor = new Vector2(localMouse.x / before - _pan.x, localMouse.y / before - _pan.y);
            _pan = new Vector2(localMouse.x / _zoom - underCursor.x, localMouse.y / _zoom - underCursor.y);
        }

        private void FitView()
        {
            if (_graph.Nodes.Count == 0)
            {
                return;
            }
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                ConstellationNode n = _graph.Nodes[i];
                minX = Mathf.Min(minX, n.X);
                maxX = Mathf.Max(maxX, n.X);
                minY = Mathf.Min(minY, n.Y);
                maxY = Mathf.Max(maxY, n.Y);
            }

            float w = Mathf.Max(1f, maxX - minX);
            float h = Mathf.Max(1f, maxY - minY);
            float cw = Mathf.Max(100f, position.width - InspectorWidth);
            float ch = Mathf.Max(100f, position.height - EditorStyles.toolbar.fixedHeight);

            _zoom = Mathf.Clamp(Mathf.Min(cw * 0.88f / w, ch * 0.88f / h), MinZoom, MaxZoom);
            _pan = new Vector2(cw * 0.5f / _zoom - (minX + maxX) * 0.5f,
                               ch * 0.5f / _zoom + (minY + maxY) * 0.5f);
            Repaint();
        }

        private void DrawInspector(Rect area)
        {
            GUILayout.BeginArea(area, EditorStyles.helpBox);
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            ConstellationNode n = _graph.Get(_selected);
            if (n == null)
            {
                EditorGUILayout.LabelField("고른 노드 없음", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Ctrl+클릭 으로 노드를 놓고, Shift+클릭 두 번으로 잇는다.\n" +
                    "이미 이어진 둘을 Shift+클릭 하면 끊긴다.", MessageType.Info);
            }
            else
            {
                DrawNodeFields(n);
            }

            EditorGUILayout.Space(10f);
            DrawValidation();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNodeFields(ConstellationNode n)
        {
            EditorGUILayout.LabelField($"#{n.Id}  {n.DisplayName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(n.Description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();

            n.Type = (EConstellationNodeType)EditorGUILayout.EnumPopup(
                new GUIContent("종류", TypeHelp(n.Type)), n.Type);
            n.Family = (ESkillFamily)EditorGUILayout.EnumPopup("계열", n.Family);
            n.Ring = EditorGUILayout.IntField(new GUIContent("링", "안쪽이 0. 값과 색의 기준"), n.Ring);

            EditorGUILayout.Space(4f);
            n.Cost = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("값", "한 번 찍는 데 드는 슈퍼 DNA"), n.Cost));
            n.MaxLevel = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("최대 레벨", "몇 번까지 찍나. 한 번뿐이면 1"), n.MaxLevel));

            EditorGUILayout.Space(4f);
            n.Trait = (EPartTrait)EditorGUILayout.EnumPopup("특성", n.Trait);
            n.Value = EditorGUILayout.FloatField(new GUIContent("값(비율)", "0.02 = 2%"), n.Value);
            n.FamilyScoped = EditorGUILayout.Toggle(
                new GUIContent("계열 한정", "켜면 위 계열 스킬에만 걸린다. 끄면 모든 스킬"), n.FamilyScoped);

            if (n.Type == EConstellationNodeType.Skill)
            {
                n.SkillId = EditorGUILayout.IntField(new GUIContent("스킬 id", "이 노드를 사면 열리는 파츠"), n.SkillId);
            }
            if (n.Type == EConstellationNodeType.Gate)
            {
                n.NeedClear = EditorGUILayout.IntField(new GUIContent("필요 클리어", "이만큼 클리어해야 지나간다"), n.NeedClear);
            }
            if (ConstellationNaming.HoldsOwnName(n.Type))
            {
                n.DisplayName = EditorGUILayout.TextField("이름", n.DisplayName);
                n.Description = EditorGUILayout.TextField("설명", n.Description);
            }

            if (EditorGUI.EndChangeCheck())
            {
                ConstellationNaming.Apply(n);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"이웃 {n.Neighbors.Count}개", EditorStyles.miniBoldLabel);
            for (int i = 0; i < n.Neighbors.Count; i++)
            {
                ConstellationNode nb = _graph.Get(n.Neighbors[i]);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(nb != null ? $"#{nb.Id} {nb.DisplayName}" : $"#{n.Neighbors[i]} (없음)",
                    EditorStyles.miniLabel);
                if (GUILayout.Button("끊기", EditorStyles.miniButton, GUILayout.Width(40f)))
                {
                    Unlink(n.Id, n.Neighbors[i]);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static string TypeHelp(EConstellationNodeType type)
        {
            switch (type)
            {
                case EConstellationNodeType.Core: return "시작점. 처음부터 켜져 있다";
                case EConstellationNodeType.Small: return "잔가지. 작은 값 하나";
                case EConstellationNodeType.Level: return "여러 번 찍는 잔가지";
                case EConstellationNodeType.Notable: return "성단의 중심. 계열 한정 큰 값";
                case EConstellationNodeType.Skill: return "스킬 해금";
                case EConstellationNodeType.Gate: return "잠긴 문. 조건을 채워야 열린다";
                default: return "가지 끝의 제일 센 것";
            }
        }

        private void DrawValidation()
        {
            if (_lastResult == null)
            {
                EditorGUILayout.HelpBox("「검증」을 누르면 여기 결과가 나온다", MessageType.None);
                return;
            }

            if (_lastResult.Ok && _lastResult.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("이상 없음", MessageType.Info);
                return;
            }

            for (int i = 0; i < _lastResult.Errors.Count; i++)
            {
                EditorGUILayout.HelpBox(_lastResult.Errors[i], MessageType.Error);
            }
            for (int i = 0; i < _lastResult.Warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(_lastResult.Warnings[i], MessageType.Warning);
            }
        }

        private static string SourceFullPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, ConstellationGraphIO.SourcePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private bool LoadFromSource()
        {
            string path = SourceFullPath();
            if (File.Exists(path) == false)
            {
                return false;
            }
            return ConstellationGraphIO.TryParse(File.ReadAllText(path), _graph);
        }

        private void Save()
        {
            ConstellationValidator.Result result = ConstellationValidator.Validate(_graph);
            _lastResult = result;

            if (result.Ok == false)
            {
                EditorUtility.DisplayDialog("저장하지 않았다",
                    $"오류 {result.Errors.Count}건을 먼저 고쳐야 한다.\n\n{result.Errors[0]}", "확인");
                return;
            }

            string path = SourceFullPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, ConstellationGraphIO.Serialize(_graph), new System.Text.UTF8Encoding(false));

            Debug.Log($"[성좌 편집기] 저장 — 노드 {_graph.Nodes.Count} · 간선 {_graph.EdgeCount}\n{path}\n" +
                      "굽기: cd _DataExporter && node export.js");
            ShowNotification(new GUIContent("저장했다 — export.js 로 구우면 게임에 반영된다"));
        }
    }
}
