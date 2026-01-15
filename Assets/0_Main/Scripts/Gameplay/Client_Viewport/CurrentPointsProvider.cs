using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CurrentPointsProvider : MonoBehaviour
{
    #region Singleton
    public static CurrentPointsProvider Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // optional, but avoids multiple singletons
            return;
        }

        Instance = this;
    }
    #endregion

    private List<IPointCollector> pointCollectors;

    void Start()
    {
        pointCollectors = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPointCollector>().ToList();
    }

    public List<TouchPoint> CurrentPoints { get; private set; } = new();

    void Update()
    {
        CurrentPoints.Clear();

        foreach (IPointCollector pointCollector in pointCollectors)
        {
            CurrentPoints.AddRange(pointCollector.Collect());
        }
    }
}
