using System;
using System.Linq;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace UnityEngine
{
    [Serializable]
    internal sealed class DynamicReference : ScriptableObject
    {
        // Internal
        [SerializeField] internal UnityEngine.Object assetReference;
    }

    [Serializable]
    public sealed class DynamicReference<T> where T : UnityEngine.Object
    {
        // Type
        internal enum ReferenceType
        {
            None = 0,
            Resources,
            ResourcesProxy,
            AssetBundle,
#if UNITY_ADDRESSABLES
            Addressable,
#endif
        }

        // Internal
        [SerializeField] internal string guid = "";
        [SerializeField] internal string path = "";
        [SerializeField] internal string bundleName = "";
        [SerializeField] internal ReferenceType type = 0;

        // Private
        private const string notAssignedError = "Cannot load asset dynamically because no asset was assigned!";

        private T loadedAsset = null;

        // Properties
        public T Asset
        {
            get
            {
                // Check for loaded
                if (loadedAsset == null)
                    loadedAsset = Load();

                return loadedAsset;
            }
        }

        public bool IsLoaded
        {
            get { return loadedAsset != null; }
        }

        public bool IsAssigned
        {
            get { return type != 0 && string.IsNullOrEmpty(path) == false; }
        }

        // Methods
        public T Load(bool throwOnError = true)
        {
            // Check for assigned
            if (CheckAssigned(throwOnError) == false)
                return null;

            // Perform load
            switch(type)
            {
                case ReferenceType.Resources:
                    {
                        return Resources.Load<T>(path);
                    }
                case ReferenceType.ResourcesProxy:
                    {
                        // Load proxy
                        DynamicReference proxy = Resources.Load<DynamicReference>(path);

                        // Get as T
                        return proxy.assetReference as T;
                    }
                case ReferenceType.AssetBundle:
                    {
                        // Find all loaded bundles
                        AssetBundle[] loadedBundles = Resources.FindObjectsOfTypeAll<AssetBundle>();

                        // Try to find bundle
                        AssetBundle targetBundle = loadedBundles.FirstOrDefault(b => b.name == bundleName);

                        // Check for found
                        if (targetBundle == null)
                            throw new InvalidOperationException("Asset bundle must be loaded before the asset can be accessed: " + bundleName);

                        // Load the asset
                        return targetBundle.LoadAsset<T>(path);
                    }
#if UNITY_ADDRESSABLES
                case ReferenceType.Addressable:
                    {
                        // Request load
                        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
                        T result = null;

                        // Wait for completed
                        handle.Completed += (AsyncOperationHandle<T> async) => 
                            result = async.Status == AsyncOperationStatus.Succeeded ? async.Result : null;

                        // Block thread except for WebGL
#if !UNITY_WEBGL        
                        handle.WaitForCompletion();
#endif
                        return result;
                    }
#endif

            }
            return null;
        }

        public CustomYieldInstruction LoadAsync()
        {
            return default;
        }

        private bool CheckAssigned(bool throwOnError)
        {
            // Check for assigned
            if (IsAssigned == false)
            {
                if (throwOnError == true)
                    throw new InvalidOperationException(notAssignedError);

                Debug.LogError(notAssignedError);
                return false;
            }
            return true;
        }

        public static implicit operator T(DynamicReference<T> reference)
        {
            return reference.Asset;
        }
    }
}