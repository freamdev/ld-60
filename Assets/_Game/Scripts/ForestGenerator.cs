
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ForestGenerator : MonoBehaviour
{
    [Header("Tree")]
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private float minTreeScale = 0.8f;
    [SerializeField] private float maxTreeScale = 1.5f;

    [Header("Cone Variation")]
    [SerializeField] private Vector2 coneYOffset = new(-0.35f, 0.35f);

    [Header("Curved Paths")]
    [SerializeField] private bool generatePaths = true;
    [SerializeField, Min(0)] private int pathCount = 3;
    [SerializeField, Min(0.1f)] private float pathWidth = 5f;
    [SerializeField, Min(2)] private int pathControlPoints = 5;
    [SerializeField, Min(2)] private int samplesPerSegment = 12;
    [SerializeField, Range(0f, 1f)] private float pathWander = 0.35f;
    [SerializeField, Min(0f)] private float pathEdgeMargin = 10f;

    [Header("Forest")]
    [SerializeField] private Vector2 forestSize = new(100f, 100f);

    [Min(0f)]
    [SerializeField] private float treeClearance = 2f;

    [Min(0.01f)]
    [SerializeField] private float trunkRadius = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float density = 0.8f;

    [Header("Natural Clustering")]
    [SerializeField] private bool useNoise = true;

    [SerializeField] private float noiseScale = 0.035f;

    [Range(0f, 1f)]
    [SerializeField] private float noiseInfluence = 0.6f;

    [Header("Dense Forest Border")]
    [SerializeField] private bool generateBorder = true;

    [Min(0.1f)]
    [SerializeField] private float borderWidth = 8f;

    [Min(0.1f)]
    [SerializeField] private float borderSpacing = 1.5f;

    [Range(0f, 0.45f)]
    [SerializeField] private float borderJitter = 0.3f;

    [SerializeField] private Vector2 borderTreeScale = new(1.1f, 1.8f);

    [SerializeField] private bool createBoundaryColliders = true;

    [Min(1f)]
    [SerializeField] private float boundaryHeight = 20f;

    [Header("Generation")]
    [SerializeField] private int seed = 12345;

    private const int AttemptsPerPoint = 30;
    private const string ContainerName = "Generated Trees";

    private struct TreePoint
    {
        public Vector2 position;
        public float scale;

        public TreePoint(Vector2 position, float scale)
        {
            this.position = position;
            this.scale = scale;
        }
    }

    [ContextMenu("Generate Forest")]
    public void Generate()
    {
        if (treePrefab == null)
        {
            return;
        }

        if (forestSize.x <= 0f || forestSize.y <= 0f)
            return;

        Clear();

        var random = new System.Random(seed);

        var minScale = Mathf.Max(0.01f, minTreeScale);
        var maxScale = Mathf.Max(minScale, maxTreeScale);

        var minRadius = trunkRadius * minScale;
        var maxRadius = trunkRadius * maxScale;

        var minimumDistance = 2f * minRadius + treeClearance;
        var maximumDistance = minRadius + maxRadius + treeClearance;

        var cellSize = minimumDistance / Mathf.Sqrt(2f);
        var gridWidth = Mathf.CeilToInt(forestSize.x / cellSize);
        var gridHeight = Mathf.CeilToInt(forestSize.y / cellSize);

        var grid = new Dictionary<Vector2Int, List<int>>();
        var points = new List<TreePoint>();
        var active = new List<int>();

        var firstPosition = new Vector2(RandomRange(random, 0f, forestSize.x), RandomRange(random, 0f, forestSize.y));

        AddPoint(new TreePoint(firstPosition, RandomRange(random, minScale, maxScale)));

        while (active.Count > 0)
        {
            var activeIndex = random.Next(active.Count);
            var origin = points[active[activeIndex]];
            var found = false;

            for (var attempt = 0; attempt < AttemptsPerPoint; attempt++)
            {
                var angle = RandomRange(random, 0f, Mathf.PI * 2f);
                var scale = RandomRange(random, minScale, maxScale);
                var requiredDistance = trunkRadius * (origin.scale + scale) + treeClearance;
                var distance = RandomRange(random, requiredDistance, requiredDistance + maximumDistance);

                var candidate = origin.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                if (candidate.x < 0f || candidate.y < 0f || candidate.x >= forestSize.x || candidate.y >= forestSize.y)
                    continue;

                if (!IsValid(candidate, scale))
                    continue;

                var probability = density;

                if (useNoise)
                {
                    var noise = Mathf.PerlinNoise(candidate.x * noiseScale + seed * 0.0137f, candidate.y * noiseScale + seed * 0.0173f);
                    probability *= Mathf.Lerp(1f, noise, noiseInfluence);
                }

                if (random.NextDouble() > probability)
                    continue;

                AddPoint(new TreePoint(candidate, scale));
                found = true;
                break;
            }

            if (!found)
                active.RemoveAt(activeIndex);
        }

        var paths = generatePaths ? BuildPaths(random) : new List<List<Vector2>>();

        var container = new GameObject(ContainerName);
        container.transform.SetParent(transform, false);

        var placedCount = 0;
        foreach (var point in points)
        {
            if (IsOnPath(point, paths))
                continue;

            var position = transform.TransformPoint(new Vector3(point.position.x - forestSize.x / 2f, 0f, point.position.y - forestSize.y / 2f));

#if UNITY_EDITOR
            var tree = !Application.isPlaying ? (GameObject)PrefabUtility.InstantiatePrefab(treePrefab) : Instantiate(treePrefab);
#else
            var tree = Instantiate(treePrefab);
#endif

            tree.transform.SetParent(container.transform, true);
            tree.transform.position = position;

            tree.transform.rotation = transform.rotation * Quaternion.Euler(0f, RandomRange(random, 0f, 360f), 0f);

            tree.transform.localScale = treePrefab.transform.localScale * point.scale;
            RandomizeCone(tree, random);
            placedCount++;
        }

        if (generateBorder)
        {
            GenerateDenseBorder(container, random);

            if (createBoundaryColliders)
                CreateBoundaryColliders(container);
        }

        Debug.Log($"Generated {placedCount} interior trees, removed {points.Count - placedCount} for {paths.Count} paths.");

        void AddPoint(TreePoint point)
        {
            var index = points.Count;
            points.Add(point);
            active.Add(index);

            var cell = GetCell(point.position);

            if (!grid.TryGetValue(cell, out var indices))
            {
                indices = new List<int>();
                grid.Add(cell, indices);
            }

            indices.Add(index);
        }

        Vector2Int GetCell(Vector2 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.y / cellSize));
        }

        bool IsValid(Vector2 candidate, float scale)
        {
            var cell = GetCell(candidate);

            var searchRadius = Mathf.CeilToInt((2f * maxRadius + treeClearance) / cellSize) + 1;

            for (var x = -searchRadius; x <= searchRadius; x++)
            {
                for (var y = -searchRadius; y <= searchRadius; y++)
                {
                    var neighbor = cell + new Vector2Int(x, y);

                    if (!grid.TryGetValue(neighbor, out var indices))
                        continue;

                    foreach (var index in indices)
                    {
                        var other = points[index];
                        var required = trunkRadius * (scale + other.scale) + treeClearance;

                        if ((candidate - other.position).sqrMagnitude < required * required)
                            return false;
                    }
                }
            }

            return true;
        }
    }

    private void RandomizeCone(GameObject tree, System.Random random)
    {
        foreach (var child in tree.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Cone")
                continue;

            var local = child.localPosition;
            local.y += RandomRange(random, coneYOffset.x, coneYOffset.y);
            child.localPosition = local;
        }
    }

    private List<List<Vector2>> BuildPaths(System.Random random)
    {
        var result = new List<List<Vector2>>();
        var margin = Mathf.Min(pathEdgeMargin, forestSize.x * 0.4f, forestSize.y * 0.4f);
        var min = new Vector2(margin, margin);
        var max = forestSize - min;

        for (var pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            var horizontal = random.Next(2) == 0;
            var fixedStart = horizontal ? min.x : min.y;
            var fixedEnd = horizontal ? max.x : max.y;
            var crossMin = horizontal ? min.y : min.x;
            var crossMax = horizontal ? max.y : max.x;
            var crossStart = RandomRange(random, crossMin, crossMax);
            var crossEnd = RandomRange(random, crossMin, crossMax);

            var count = Mathf.Max(2, pathControlPoints);
            var controls = new List<Vector2>(count);
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)(count - 1);
                var along = Mathf.Lerp(fixedStart, fixedEnd, t);
                var cross = Mathf.Lerp(crossStart, crossEnd, t);
                if (i > 0 && i < count - 1)
                    cross += RandomRange(random, -1f, 1f) * pathWander * (crossMax - crossMin);
                cross = Mathf.Clamp(cross, crossMin, crossMax);
                controls.Add(horizontal ? new Vector2(along, cross) : new Vector2(cross, along));
            }

            var sampled = new List<Vector2>();
            for (var i = 0; i < controls.Count - 1; i++)
            {
                var p0 = controls[Mathf.Max(0, i - 1)];
                var p1 = controls[i];
                var p2 = controls[i + 1];
                var p3 = controls[Mathf.Min(controls.Count - 1, i + 2)];
                var samples = Mathf.Max(2, samplesPerSegment);
                for (var j = 0; j < samples; j++)
                {
                    var t = j / (float)samples;
                    var p = CatmullRom(p0, p1, p2, p3, t);
                    sampled.Add(new Vector2(Mathf.Clamp(p.x, min.x, max.x),
                                            Mathf.Clamp(p.y, min.y, max.y)));
                }
            }
            sampled.Add(controls[controls.Count - 1]);
            result.Add(sampled);
        }
        return result;
    }

    private bool IsOnPath(TreePoint tree, List<List<Vector2>> paths)
    {
        var radius = pathWidth * 0.5f + tree.scale * trunkRadius;
        var radiusSquared = radius * radius;
        foreach (var path in paths)
            for (var i = 1; i < path.Count; i++)
                if (SqrDistanceToSegment(tree.position, path[i - 1], path[i]) <= radiusSquared)
                    return true;
        return false;
    }

    private static float SqrDistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.sqrMagnitude;
        var t = lengthSquared > 0.00001f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared) : 0f;
        return (point - (a + ab * t)).sqrMagnitude;
    }

    private static Vector2 CatmullRom(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * (2f * b + (-a + c) * t +
                       (2f * a - 5f * b + 4f * c - d) * t2 +
                       (-a + 3f * b - 3f * c + d) * t3);
    }

    private void GenerateDenseBorder(GameObject container, System.Random random)
    {
        var halfX = forestSize.x * 0.5f;
        var halfZ = forestSize.y * 0.5f;

        var spacing = Mathf.Max(0.1f, borderSpacing);
        var width = Mathf.Max(0.1f, borderWidth);

        var jitter = spacing * borderJitter;

        var columns = Mathf.CeilToInt(forestSize.x / spacing);
        var rows = Mathf.CeilToInt(forestSize.y / spacing);

        var borderContainer = new GameObject("Dense Border");
        borderContainer.transform.SetParent(container.transform, false);

        var generated = 0;

        for (var x = 0; x <= columns; x++)
        {
            for (var z = 0; z <= rows; z++)
            {
                var localX = Mathf.Min(x * spacing, forestSize.x) - halfX;
                var localZ = Mathf.Min(z * spacing, forestSize.y) - halfZ;

                var distanceToEdge = Mathf.Min(halfX - Mathf.Abs(localX), halfZ - Mathf.Abs(localZ));

                var noise = Mathf.PerlinNoise((localX + seed * 0.13f) * 0.06f, (localZ + seed * 0.17f) * 0.06f);

                var localBorderWidth = width * Mathf.Lerp(0.65f, 1.35f, noise);

                if (distanceToEdge > localBorderWidth)
                    continue;

                var offsetX = (z % 2 == 0) ? 0f : spacing * 0.5f;

                var px = localX + offsetX + RandomRange(random, -jitter, jitter);
                var pz = localZ + RandomRange(random, -jitter, jitter);

                px = Mathf.Clamp(px, -halfX, halfX);
                pz = Mathf.Clamp(pz, -halfZ, halfZ);

                var scale = RandomRange(random, borderTreeScale.x, borderTreeScale.y);

                var worldPosition = transform.TransformPoint(new Vector3(px, 0f, pz));

#if UNITY_EDITOR
                var tree = !Application.isPlaying ? (GameObject)PrefabUtility.InstantiatePrefab(treePrefab) : Instantiate(treePrefab);
#else
            var tree = Instantiate(treePrefab);
#endif

                tree.transform.SetParent(borderContainer.transform, true);
                tree.transform.position = worldPosition;

                tree.transform.rotation = transform.rotation * Quaternion.Euler(
                    0f,
                    RandomRange(random, 0f, 360f),
                    0f
                );

                tree.transform.localScale = treePrefab.transform.localScale * scale;
                RandomizeCone(tree, random);

                generated++;
            }
        }

        Debug.Log($"Generated {generated} dense border trees.");
    }

    private void CreateBoundaryColliders(GameObject container)
    {
        var halfX = forestSize.x * 0.5f;
        var halfZ = forestSize.y * 0.5f;

        var thickness = 2f;
        var height = boundaryHeight;

        CreateWall("North", new Vector3(0f, 0, halfZ), new Vector3(forestSize.x + thickness * 2f, height, thickness));

        CreateWall("South", new Vector3(0f, 0, -halfZ), new Vector3(forestSize.x + thickness * 2f, height, thickness));

        CreateWall("East", new Vector3(halfX, 0, 0f), new Vector3(thickness, height, forestSize.y + thickness * 2f));

        CreateWall("West", new Vector3(-halfX, 0, 0f), new Vector3(thickness, height, forestSize.y + thickness * 2f));

        void CreateWall(string name, Vector3 position, Vector3 size)
        {
            var wall = new GameObject($"Boundary {name}");
            wall.transform.SetParent(container.transform, false);

            wall.transform.localPosition = position;

            var collider = wall.AddComponent<BoxCollider>();
            collider.size = size;
        }
    }

    [ContextMenu("Clear Forest")]
    public void Clear()
    {
        var existing = transform.Find(ContainerName);

        if (existing == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(existing.gameObject);
        else
            Destroy(existing.gameObject);
#else
        Destroy(existing.gameObject);
#endif
    }

    private static float RandomRange(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(Vector3.zero, new Vector3(forestSize.x, 0.1f, forestSize.y));
    }
}
