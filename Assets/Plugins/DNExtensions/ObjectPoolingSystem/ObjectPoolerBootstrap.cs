using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.ObjectPooling
{
    public static class ObjectPoolerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (ObjectPooler.Instance) return;
            
            GameObject poolerPrefab = Resources.Load<GameObject>("ObjectPooler");
            
            if (poolerPrefab)
            {
                GameObject poolerObject = Object.Instantiate(poolerPrefab);
                poolerObject.name = "ObjectPooler";
            }
            else
            {
                Debug.LogError("ObjectPooler prefab not found in Resources folder! Please create a Resources folder and place the ObjectPooler prefab there.");
            }
        }
    }
}