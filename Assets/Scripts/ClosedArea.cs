using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ClosedArea : MonoBehaviour
{
    [Header("Line Settings")]
    [Tooltip("Color of boundary lines")]
    public Color lineColor = new Color(0, 1, 1, 1f);
    [Tooltip("Line thickness")]
    public float lineThickness = 2f;

    [Header("Point Settings")]
    [Tooltip("Color of control points")]
    public Color pointColor = Color.red;
    [Tooltip("Size of control points")]
    public float pointSize = 0.3f;

    [Header("Configuration")]
    public bool showInEditMode = true;
    public List<Transform> points = new List<Transform>();

    [SerializeField, HideInInspector] private string _areaGuid;

    public string AreaGuid => string.IsNullOrEmpty(_areaGuid) ? (_areaGuid = Guid.NewGuid().ToString()) : _areaGuid;

    void OnValidate() => _areaGuid = string.IsNullOrEmpty(_areaGuid) ? Guid.NewGuid().ToString() : _areaGuid;
    public Transform[] GetAreaPoints() => points.ToArray();

    void OnDrawGizmos()
    {
        if (!showInEditMode || points.Count < 2) return;
        SyncPointsForGizmo();

#if UNITY_EDITOR
        // Use Handles for thicker, more visible lines
        Handles.color = lineColor;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 start = points[i].position;
            Vector3 end = points[(i + 1) % points.Count].position;
            Handles.DrawAAPolyLine(lineThickness, start, end);
        }
#endif
    }

    void SyncPointsForGizmo()
    {
        points.Clear();
        foreach (Transform child in transform)
        {
            if (child != null) points.Add(child);
        }
        OrderPointsSequentially(); // Simple hierarchy-based order
    }

    // Order points based on their hierarchy order (child index)
    void OrderPointsSequentially()
    {
        points = points.OrderBy(p => p.GetSiblingIndex()).ToList();
    }
}