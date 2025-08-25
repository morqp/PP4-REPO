using UnityEngine;

public class LineSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject uniquePrefab;
    public GameObject fillerPrefab;

    [Header("Layout")]
    [Min(2)] public int count = 5;          // total number of objects
    public float spacing = 2f;              // distance between each object
    public Vector3 axis = Vector3.right;    // direction of the line (default = X axis)

    void Start()
    {
        Spawn();
    }

    void Spawn()
    {
        if (uniquePrefab == null || fillerPrefab == null)
        {
            Debug.LogWarning("Assign both prefabs before spawning.");
            return;
        }

        int uniqueIndex = Random.Range(0, count);
        Vector3 dir = axis.normalized;

        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) / 2f) * spacing;
            Vector3 pos = transform.position + dir * offset;

            GameObject prefabToSpawn = (i == uniqueIndex) ? uniquePrefab : fillerPrefab;
            Instantiate(prefabToSpawn, pos, Quaternion.identity);
        }
    }
}
