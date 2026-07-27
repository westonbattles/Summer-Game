using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class WorldItemOutline
{
    const int MaxRoundedOutlineTriangles = 50000;
    const int MaxSubdivisionSteps = 2;

    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly Dictionary<Mesh, Mesh> StaticOutlineMeshes = new();
    static readonly Dictionary<Mesh, Mesh> SkinnedOutlineMeshes = new();

    public static void AddTo(GameObject root, Material material, Color color)
    {
        if (root == null || material == null) return;

        Renderer[] sourceRenderers = root.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetColor(OutlineColorId, color);

        foreach (Renderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer is MeshRenderer meshRenderer)
            {
                CreateMeshOutline(meshRenderer, material, properties);
            }
            else if (sourceRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                CreateSkinnedMeshOutline(skinnedMeshRenderer, material, properties);
            }
        }
    }

    static void CreateMeshOutline(MeshRenderer source, Material material, MaterialPropertyBlock properties)
    {
        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null) return;

        GameObject outlineObject = CreateOutlineObject(source);
        MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
        outlineFilter.sharedMesh = GetSmoothOutlineMesh(sourceFilter.sharedMesh, roundCorners: true);

        MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
        ConfigureRenderer(source, outlineRenderer, sourceFilter.sharedMesh.subMeshCount, material, properties);
    }

    static void CreateSkinnedMeshOutline(SkinnedMeshRenderer source, Material material, MaterialPropertyBlock properties)
    {
        if (source.sharedMesh == null) return;

        GameObject outlineObject = CreateOutlineObject(source);
        SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
        outlineRenderer.sharedMesh = GetSmoothOutlineMesh(source.sharedMesh, roundCorners: false);
        outlineRenderer.bones = source.bones;
        outlineRenderer.rootBone = source.rootBone;
        outlineRenderer.localBounds = source.localBounds;
        outlineRenderer.quality = source.quality;
        outlineRenderer.updateWhenOffscreen = source.updateWhenOffscreen;

        ConfigureRenderer(source, outlineRenderer, source.sharedMesh.subMeshCount, material, properties);
    }

    static GameObject CreateOutlineObject(Renderer source)
    {
        GameObject outlineObject = new GameObject($"{source.gameObject.name} (Outline)");
        outlineObject.layer = source.gameObject.layer;
        outlineObject.transform.SetParent(source.transform, false);
        return outlineObject;
    }

    static void ConfigureRenderer(Renderer source, Renderer outline, int subMeshCount,
        Material material, MaterialPropertyBlock properties)
    {
        int materialCount = Mathf.Max(1, subMeshCount);
        Material[] materials = new Material[materialCount];
        for (int i = 0; i < materialCount; i++) materials[i] = material;

        outline.sharedMaterials = materials;
        outline.enabled = source.enabled;
        outline.shadowCastingMode = ShadowCastingMode.Off;
        outline.receiveShadows = false;
        outline.lightProbeUsage = LightProbeUsage.Off;
        outline.reflectionProbeUsage = ReflectionProbeUsage.Off;
        outline.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        outline.renderingLayerMask = source.renderingLayerMask;
        outline.sortingLayerID = source.sortingLayerID;
        outline.sortingOrder = source.sortingOrder;
        outline.SetPropertyBlock(properties);
    }

    static Mesh GetSmoothOutlineMesh(Mesh source, bool roundCorners)
    {
        Dictionary<Mesh, Mesh> cache = roundCorners ? StaticOutlineMeshes : SkinnedOutlineMeshes;
        if (cache.TryGetValue(source, out Mesh cachedMesh)) return cachedMesh;

        if (!source.isReadable)
        {
            Debug.LogWarning(
                $"Outline mesh '{source.name}' is not readable, so its hard-edge normals cannot be smoothed. " +
                "Item models under Assets/#Game/Items are made readable automatically when imported.", source);
            cache.Add(source, source);
            return source;
        }

        Vector3[] vertices = source.vertices;
        Vector3[] smoothNormals = CalculateSmoothNormals(vertices, source.normals);
        Mesh outlineMesh = roundCorners
            ? CreateRoundedOutlineMesh(source, vertices, smoothNormals)
            : CreateSkinnedOutlineMesh(source, smoothNormals);

        cache.Add(source, outlineMesh);
        return outlineMesh;
    }

    static Vector3[] CalculateSmoothNormals(Vector3[] vertices, Vector3[] normals)
    {
        if (normals.Length != vertices.Length) return normals;

        Dictionary<PositionKey, Vector3> normalSums = new(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            PositionKey key = new PositionKey(vertices[i]);
            normalSums.TryGetValue(key, out Vector3 sum);
            normalSums[key] = sum + normals[i];
        }

        Vector3[] smoothNormals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            smoothNormals[i] = normalSums[new PositionKey(vertices[i])].normalized;
        }

        return smoothNormals;
    }

    static Mesh CreateSkinnedOutlineMesh(Mesh source, Vector3[] smoothNormals)
    {
        Mesh outlineMesh = UnityEngine.Object.Instantiate(source);
        outlineMesh.name = $"{source.name} (Smooth Outline)";
        outlineMesh.hideFlags = HideFlags.HideAndDontSave;
        outlineMesh.tangents = ToTangents(smoothNormals);
        return outlineMesh;
    }

    static Mesh CreateRoundedOutlineMesh(Mesh source, Vector3[] vertices, Vector3[] smoothNormals)
    {
        List<Vector3> positions = new List<Vector3>(vertices);
        List<Vector3> expansionNormals = new List<Vector3>(smoothNormals);
        List<int>[] subMeshTriangles = new List<int>[source.subMeshCount];
        int triangleCount = 0;

        for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
        {
            if (source.GetTopology(subMesh) != MeshTopology.Triangles)
            {
                return CreateSkinnedOutlineMesh(source, smoothNormals);
            }

            subMeshTriangles[subMesh] = new List<int>(source.GetTriangles(subMesh));
            triangleCount += subMeshTriangles[subMesh].Count / 3;
        }

        int subdivisionSteps = 0;
        while (subdivisionSteps < MaxSubdivisionSteps && triangleCount * 4 <= MaxRoundedOutlineTriangles)
        {
            Dictionary<EdgeKey, int> edgeMidpoints = new();

            for (int subMesh = 0; subMesh < subMeshTriangles.Length; subMesh++)
            {
                List<int> oldTriangles = subMeshTriangles[subMesh];
                List<int> newTriangles = new List<int>(oldTriangles.Count * 4);

                for (int i = 0; i < oldTriangles.Count; i += 3)
                {
                    int a = oldTriangles[i];
                    int b = oldTriangles[i + 1];
                    int c = oldTriangles[i + 2];
                    int ab = GetMidpoint(a, b, positions, expansionNormals, edgeMidpoints);
                    int bc = GetMidpoint(b, c, positions, expansionNormals, edgeMidpoints);
                    int ca = GetMidpoint(c, a, positions, expansionNormals, edgeMidpoints);

                    AddTriangle(newTriangles, a, ab, ca);
                    AddTriangle(newTriangles, ab, b, bc);
                    AddTriangle(newTriangles, ca, bc, c);
                    AddTriangle(newTriangles, ab, bc, ca);
                }

                subMeshTriangles[subMesh] = newTriangles;
            }

            triangleCount *= 4;
            subdivisionSteps++;
        }

        Mesh outlineMesh = new Mesh
        {
            name = $"{source.name} (Rounded Outline)",
            hideFlags = HideFlags.HideAndDontSave,
            indexFormat = positions.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
        };

        outlineMesh.SetVertices(positions);
        outlineMesh.SetNormals(expansionNormals);
        outlineMesh.tangents = ToTangents(expansionNormals);
        outlineMesh.subMeshCount = subMeshTriangles.Length;

        for (int subMesh = 0; subMesh < subMeshTriangles.Length; subMesh++)
        {
            outlineMesh.SetTriangles(subMeshTriangles[subMesh], subMesh, false);
        }

        Bounds bounds = source.bounds;
        bounds.Expand(0.5f);
        outlineMesh.bounds = bounds;
        return outlineMesh;
    }

    static int GetMidpoint(int a, int b, List<Vector3> positions, List<Vector3> normals,
        Dictionary<EdgeKey, int> edgeMidpoints)
    {
        EdgeKey key = new EdgeKey(a, b);
        if (edgeMidpoints.TryGetValue(key, out int midpoint)) return midpoint;

        midpoint = positions.Count;
        positions.Add((positions[a] + positions[b]) * 0.5f);
        normals.Add((normals[a] + normals[b]).normalized);
        edgeMidpoints.Add(key, midpoint);
        return midpoint;
    }

    static void AddTriangle(List<int> triangles, int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    static Vector4[] ToTangents(IReadOnlyList<Vector3> normals)
    {
        Vector4[] tangents = new Vector4[normals.Count];
        for (int i = 0; i < normals.Count; i++)
        {
            Vector3 normal = normals[i];
            tangents[i] = new Vector4(normal.x, normal.y, normal.z, 0f);
        }

        return tangents;
    }

    readonly struct PositionKey : IEquatable<PositionKey>
    {
        const float Precision = 100000f;

        readonly int x;
        readonly int y;
        readonly int z;

        public PositionKey(Vector3 position)
        {
            x = Mathf.RoundToInt(position.x * Precision);
            y = Mathf.RoundToInt(position.y * Precision);
            z = Mathf.RoundToInt(position.z * Precision);
        }

        public bool Equals(PositionKey other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is PositionKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = x;
                hash = hash * 397 ^ y;
                return hash * 397 ^ z;
            }
        }
    }

    readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        readonly int a;
        readonly int b;

        public EdgeKey(int first, int second)
        {
            a = Mathf.Min(first, second);
            b = Mathf.Max(first, second);
        }

        public bool Equals(EdgeKey other) => a == other.a && b == other.b;
        public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);
        public override int GetHashCode() => a * 397 ^ b;
    }
}
