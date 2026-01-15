using System.Collections.Generic;
using UnityEngine;

public class DebugMarkersManager : MonoBehaviour
{
    private string _scriptName = "DebugMarkersManager";

    [SerializeField] private GameObject debugMarkerPrefab;
    [SerializeField] private RectTransform debugCanvas;

    private readonly List<DebugMarker> _markers = new();

    void Update()
    {
        CurrentPointsProvider provider = CurrentPointsProvider.Instance;
        if (provider == null)
        {
            Debug.LogError($"[{_scriptName}] CurrentPointsProvider.Instance is null.");
            return;
        }

        List<TouchPoint> points = provider.CurrentPoints;
        if (points == null)
        {
            Debug.LogError($"[{_scriptName}] CurrentPointsProvider.Instance.CurrentPoints is null.");
            return;
        }

        FulfilMarkerNeeds(points.Count);

        // Arrange markers to points
        {
            int i = 0;

            for (; i < points.Count; i++)
            {
                DebugMarker marker = _markers[i];
                marker.gameObject.SetActive(true);
                marker.Set(points[i].Id, points[i].Position);
            }

            for (; i < _markers.Count; i++)
            {
                _markers[i].gameObject.SetActive(false);
            }
        }
    }

    private void FulfilMarkerNeeds(int needed)
    {
        if (debugMarkerPrefab == null)
        {
            Debug.LogError($"[{_scriptName}] debugMarkerPrefab is null.");
            return;
        }

        if (debugCanvas == null)
        {
            Debug.LogError($"[{_scriptName}] debugCanvas is null.");
            return;
        }

        while (_markers.Count < needed)
        {
            GameObject obj = Instantiate(debugMarkerPrefab, debugCanvas);
            DebugMarker marker = obj.GetComponent<DebugMarker>();
            _markers.Add(marker);
        }
    }
}
