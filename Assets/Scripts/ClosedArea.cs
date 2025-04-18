using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[ExecuteAlways]
public class ClosedArea : MonoBehaviour
{
    [Tooltip("Color of the area wireframe and fill in the Scene view")]
    public Color areaColor = new Color(0, 1, 1, 0.3f);
    [Tooltip("Toggle visualization in the Scene view")]
    public bool showInEditMode = true;
    [Tooltip("The list of child Transforms defining the polygon vertices")]
    public List<Transform> points = new List<Transform>();

    // Persistent, serialized GUID
    [SerializeField, HideInInspector]
    private string _areaGuid;

    public string AreaGuid
    {
        get
        {
            if (string.IsNullOrEmpty(_areaGuid))
                _areaGuid = Guid.NewGuid().ToString();
            return _areaGuid;
        }
    }

    void OnValidate()
    {
        if (string.IsNullOrEmpty(_areaGuid))
            _areaGuid = Guid.NewGuid().ToString();
    }

    /// <summary>Returns the points defining the closed area.</summary>
    public Transform[] GetAreaPoints() => points.ToArray();

    void OnDrawGizmos()
    {
        if (!showInEditMode || points.Count < 2) return;

        // Always re-fetch points from child transforms
        SyncPointsForGizmo();

        Gizmos.color = areaColor;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 a = points[i].position;
            Vector3 b = points[(i + 1) % points.Count].position;
            Gizmos.DrawLine(a, b);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(areaColor.r, areaColor.g, areaColor.b, areaColor.a * 0.5f);
        Vector3[] verts = points.Select(p => p.position).ToArray();
        UnityEditor.Handles.DrawAAConvexPolygon(verts);
#endif
    }

    // Sync points list with child transforms
    void SyncPointsForGizmo()
    {
        points.Clear();
        foreach (Transform child in transform)
        {
            points.Add(child);
        }
    }
}


