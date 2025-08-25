using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class QuickOutline : MonoBehaviour
{
    public enum Mode { AlwaysOn, Hover }
    public Mode activationMode = Mode.AlwaysOn;

    [ColorUsage(true, true)] public Color outlineColor = Color.cyan;
    [Range(0f, 0.2f)] public float outlineWidth = 0.03f;

    [Tooltip("Optional: assign a ready-made material using Hidden/Outline/Unlit. If left empty, one is created at runtime.")]
    public Material outlineMaterialTemplate;

    MeshRenderer sourceRenderer;
    GameObject outlineChild;
    Material runtimeMat;

    // Cache of baked smoothed normals per mesh so we only compute once.
    static readonly Dictionary<Mesh, Vector3[]> smoothCache = new();

    void Awake()
    {
        sourceRenderer = GetComponent<MeshRenderer>();

        // Create child that renders the outline shell
        outlineChild = new GameObject(name + "_Outline");
        outlineChild.transform.SetParent(transform, false);
        outlineChild.layer = gameObject.layer;

        var srcMF = GetComponent<MeshFilter>();
        var mf = outlineChild.AddComponent<MeshFilter>();
        mf.sharedMesh = Instantiate(srcMF.sharedMesh); // clone so we can inject smoothed normals

        var mr = outlineChild.AddComponent<MeshRenderer>();
        runtimeMat = outlineMaterialTemplate ? new Material(outlineMaterialTemplate)
                                            : new Material(Shader.Find("Hidden/Outline/Unlit"));
        mr.sharedMaterial = runtimeMat;

        // Bake smoothed normals into the clone so cubes look good too
        ApplySmoothedNormals(mf.sharedMesh);

        // Start enabled or disabled depending on mode
        bool on = (activationMode == Mode.AlwaysOn);
        outlineChild.SetActive(on);

        ApplyMaterialProps();
        EnsureColliderIfHover();
    }

    void OnValidate()
    {
        if (runtimeMat) ApplyMaterialProps();
        if (activationMode == Mode.Hover && GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();
    }

    void ApplyMaterialProps()
    {
        runtimeMat.SetColor("_OutlineColor", outlineColor);
        runtimeMat.SetFloat("_OutlineWidth", outlineWidth);
    }

    void EnsureColliderIfHover()
    {
        if (activationMode == Mode.Hover && GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();
    }

    // --- Hover control using mouse events (simple demo) ---
    void OnMouseEnter() { if (activationMode == Mode.Hover) outlineChild.SetActive(true); }
    void OnMouseExit() { if (activationMode == Mode.Hover) outlineChild.SetActive(false); }

    // --- Smooth normal baker: averages shared vertex positions to handle hard edges ---
    void ApplySmoothedNormals(Mesh m)
    {
        if (m == null) return;

        if (smoothCache.TryGetValue(m, out var cached))
        {
            m.normals = cached;
            return;
        }

        var verts = m.vertices;
        var norms = new Vector3[verts.Length];

        // Group by position (world-unit tolerance)
        var map = new Dictionary<Vector3, List<int>>(new Vec3Comparer(1e-4f));
        for (int i = 0; i < verts.Length; i++)
        {
            if (!map.TryGetValue(verts[i], out var list))
            {
                list = new List<int>(4);
                map.Add(verts[i], list);
            }
            list.Add(i);
        }

        foreach (var kv in map)
        {
            // average the original normals of this position group
            Vector3 avg = Vector3.zero;
            foreach (int idx in kv.Value)
                avg += m.normals[idx];
            avg.Normalize();
            foreach (int idx in kv.Value)
                norms[idx] = avg;
        }

        m.normals = norms;
        smoothCache[m] = norms;
    }

    // Position comparer with tolerance to merge duplicated vertices
    class Vec3Comparer : IEqualityComparer<Vector3>
    {
        float eps;
        public Vec3Comparer(float epsilon) { eps = epsilon; }
        public bool Equals(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < eps * eps;
        public int GetHashCode(Vector3 v) => v.GetHashCode();
    }
}
