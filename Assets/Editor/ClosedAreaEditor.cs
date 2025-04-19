using UnityEngine;
using UnityEditor;
using System.Linq;

public class ClosedAreaEditorWindow : EditorWindow
{
    const string kEditorPrefKey = "ClosedAreaEditorWindow_SelectedGuid";

    [MenuItem("Tools/Closed Area Editor")]
    public static void Open() => GetWindow<ClosedAreaEditorWindow>("Closed Area Editor");

    [SerializeField] GameObject areaObject;
    [SerializeField] bool runtimeEditing = false;

    ClosedArea area;
    Vector2 scroll;

    enum ShapeType { Custom, Rectangle, Circle, RegularPolygon }
    [SerializeField] ShapeType shapeType = ShapeType.Custom;
    [SerializeField] float shapeSize = 5f;
    [SerializeField] int polygonSides = 6;

    void OnEnable()
    {
        // Try to re‑select last area by GUID
        string savedGuid = EditorPrefs.GetString(kEditorPrefKey, "");
        if (!string.IsNullOrEmpty(savedGuid))
        {
            var all = FindObjectsOfType<ClosedArea>();
            var match = all.FirstOrDefault(a => a.AreaGuid == savedGuid);
            if (match != null)
                areaObject = match.gameObject;
        }

        AssignAreaComponent();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
        => SceneView.duringSceneGui -= OnSceneGUI;

    void OnGUI()
    {
        // — Area assignment / creation —
        areaObject = (GameObject)EditorGUILayout.ObjectField("Area GameObject", areaObject, typeof(GameObject), true);
        if (GUILayout.Button("Create New ClosedArea"))
            CreateAreaObject();

        AssignAreaComponent();

        if (area == null)
        {
            EditorGUILayout.HelpBox("Please assign or create a GameObject to define your ClosedArea.", MessageType.Warning);
            return;
        }

        // Persist GUID for domain‑reload safety
        EditorPrefs.SetString(kEditorPrefKey, area.AreaGuid);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        // ─── Shape Generation ───────────────────────────
        EditorGUILayout.LabelField("Shape Generation", EditorStyles.boldLabel);
        shapeType = (ShapeType)EditorGUILayout.EnumPopup("Shape Type", shapeType);
        shapeSize = EditorGUILayout.FloatField("Size", shapeSize);
        if (shapeType == ShapeType.RegularPolygon)
            polygonSides = EditorGUILayout.IntSlider("Sides", polygonSides, 3, 64);
        if (GUILayout.Button("Generate Shape"))
            GenerateShape();

        EditorGUILayout.Space();

        // ─── Point Operations ───────────────────────────
        EditorGUILayout.LabelField("Point Operations", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        if (GUILayout.Button("Add Point (Center)"))
            CreatePoint(area.transform.position);

        if (Selection.activeGameObject?.transform.parent == area.transform)
        {
            if (GUILayout.Button("Insert Before"))
                InsertPoint(false);
            if (GUILayout.Button("Insert After"))
                InsertPoint(true);
            if (GUILayout.Button("Remove Selected Point"))
                RemovePoint();
        }

        if (GUILayout.Button("Clear All Points"))
            ClearAllPoints();
        if (GUILayout.Button("Snap Points to Ground"))
            SnapAllToGround();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        // ─── Visualization & Runtime Edit ──────────────
        area.showInEditMode = EditorGUILayout.Toggle("Show Area Gizmo", area.showInEditMode);
        runtimeEditing = EditorGUILayout.Toggle("Runtime Editing", runtimeEditing);
        if (runtimeEditing && Application.isPlaying)
            EditorGUILayout.HelpBox("You may now drag points in Play Mode.", MessageType.Info);

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
            EditorUtility.SetDirty(area);
    }

    void AssignAreaComponent()
    {
        if (areaObject == null) { area = null; return; }
        area = areaObject.GetComponent<ClosedArea>();
        if (area == null)
        {
            Undo.RegisterCompleteObjectUndo(areaObject, "Add ClosedArea");
            area = Undo.AddComponent<ClosedArea>(areaObject);
        }
    }

    void CreateAreaObject()
    {
        var go = new GameObject("ClosedArea");
        Undo.RegisterCreatedObjectUndo(go, "Create ClosedArea");
        areaObject = go;
        AssignAreaComponent();
        Selection.activeGameObject = go;
    }

    void GenerateShape()
    {
        ClearAllPoints();
        Vector3 c = area.transform.position;
        switch (shapeType)
        {
            case ShapeType.Rectangle:
                GenRectangle(c); break;
            case ShapeType.Circle:
            case ShapeType.RegularPolygon:
                GenPolygon(c); break;
            case ShapeType.Custom:
                CreatePoint(c); break;
        }
    }

    void GenRectangle(Vector3 c)
    {
        float h = shapeSize * 0.5f;
        Vector3[] pts = {
            c + new Vector3(-h, 0, -h),
            c + new Vector3( h, 0, -h),
            c + new Vector3( h, 0,  h),
            c + new Vector3(-h, 0,  h)
        };
        foreach (var p in pts) CreatePoint(p);
    }

    void GenPolygon(Vector3 c)
    {
        for (int i = 0; i < polygonSides; i++)
        {
            float a = i * Mathf.PI * 2f / polygonSides;
            CreatePoint(c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * shapeSize);
        }
    }

    void CreatePoint(Vector3 worldPos)
    {
        Undo.RecordObject(area, "Add Point");
        var go = new GameObject("Point");
        Undo.RegisterCreatedObjectUndo(go, "Create Point");
        go.transform.SetParent(area.transform);
        go.transform.position = worldPos;
        area.points.Add(go.transform);
        SyncPoints();
        Selection.activeGameObject = go;
    }

    void InsertPoint(bool after)
    {
        var sel = Selection.activeTransform;
        if (sel == null || sel.parent != area.transform) return;

        int idx = sel.GetSiblingIndex();
        int totalPoints = area.points.Count;

        // Edge Case: Inserting after the last point
        if (after && idx == totalPoints - 1)
        {
            // Calculate direction from second-to-last to last point
            Vector3 dir = (sel.position - area.points[totalPoints - 2].position).normalized;
            // Place new point slightly beyond the last point in that direction
            Vector3 newPos = sel.position + dir * 2f; // Adjust 2f as needed
            CreatePoint(newPos);
        }
        else
        {
            // Standard midpoint calculation between adjacent points
            int neighborIdx = after ? (idx + 1) % totalPoints : (idx - 1 + totalPoints) % totalPoints;
            Vector3 neighborPos = area.points[neighborIdx].position;
            Vector3 midpoint = Vector3.Lerp(sel.position, neighborPos, 0.5f);
            CreatePoint(midpoint);
        }

        // Adjust hierarchy order
        int insertIndex = after ? idx + 1 : idx;
        var added = area.transform.GetChild(area.transform.childCount - 1);
        added.SetSiblingIndex(insertIndex);
        SyncPoints();
        Selection.activeGameObject = added.gameObject;

        SyncPoints();
        SceneView.RepaintAll();
    }

    void RemovePoint()
    {
        var sel = Selection.activeTransform;
        if (sel == null || sel.parent != area.transform) return;
        Undo.DestroyObjectImmediate(sel.gameObject);
        SyncPoints();
    }

    void ClearAllPoints()
    {
        Undo.RecordObject(area, "Clear Points");
        for (int i = area.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(area.transform.GetChild(i).gameObject);
        area.points.Clear();
    }

    void SnapAllToGround()
    {
        Undo.RecordObject(area, "Snap Points");
        foreach (var t in area.points)
            if (Physics.Raycast(t.position + Vector3.up * 10, Vector3.down, out var hit, 50f))
                t.position = hit.point;
    }

    void SyncPoints()
    {
        area.points.Clear();
        for (int i = 0; i < area.transform.childCount; i++)
            area.points.Add(area.transform.GetChild(i));
    }

    void OnSceneGUI(SceneView sv)
    {
        if (area == null || !area.showInEditMode || area.points.Count < 2) return;

        var pts = area.points;
        int count = pts.Count;

        // Draw boundary lines
        Handles.color = new Color(area.lineColor.r, area.lineColor.g, area.lineColor.b, 0.8f);
        for (int i = 0; i < count; i++)
            Handles.DrawAAPolyLine(area.lineThickness, pts[i].position, pts[(i + 1) % count].position);

        // Draw points with enhanced visibility
        for (int i = 0; i < count; i++)
        {
            Transform t = pts[i];
            Vector3 pos = t.position;
            float handleSize = HandleUtility.GetHandleSize(pos) * area.pointSize;

            // Unselected points
            Handles.color = area.pointColor;
            if (Handles.Button(pos, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                Selection.activeGameObject = t.gameObject;
        }

        // Highlight selected point
        var sel = Selection.activeTransform;
        if (sel != null && sel.parent == area.transform)
        {
            int idx = sel.GetSiblingIndex();
            float handleSize = HandleUtility.GetHandleSize(pts[idx].position) * area.pointSize * 1.2f;

            // Selected point
            Handles.color = Color.white;
            Handles.SphereHandleCap(0, pts[idx].position, Quaternion.identity, handleSize, EventType.Repaint);

            // Adjacent line highlights
            Handles.color = Color.yellow;
            Handles.DrawAAPolyLine(area.lineThickness * 2,
                pts[(idx - 1 + count) % count].position,
                pts[idx].position,
                pts[(idx + 1) % count].position);
        }
    }
}



