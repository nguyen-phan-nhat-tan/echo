using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    // Singleton: Để gọi ObjectPooler.Instance từ bất kỳ đâu
    public static ObjectPooler Instance;

    [System.Serializable]
    public struct Pool
    {
        public string tag;           // Tên nhãn (VD: "Bullet")
        public GameObject prefab;    // Prefab cần pool
        public int size;             // Số lượng tạo sẵn
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false); // Tắt nó đi chờ lệnh
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        // Lấy ra khỏi hàng chờ
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Đưa lại vào cuối hàng chờ (để xoay vòng)
        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }
}