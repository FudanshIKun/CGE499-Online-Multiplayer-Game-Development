using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace ToruToru{
    internal class NetcodeObjectPool : NetworkBehaviour{
        //----------------//
        // STATIC MEMBERS //
        //----------------//
        public static NetcodeObjectPool Instance { get; private set; }
        
        //-----------//
        // INSPECTOR //
        //-----------//
        [SerializeField]
        public List<PoolConfigObject> PooledPrefabsList;
        public readonly HashSet<GameObject> m_Prefabs = new();
        public readonly Dictionary<GameObject, ObjectPool<NetworkObject>> m_PooledObjects = new();

        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        public void Awake(){
            if (Instance != null && Instance != this)
            { Destroy(gameObject); }
            else
            { Instance = this; }
            Debug.Log($"[{gameObject.name}] Awake", gameObject);
        }
        
        public void OnValidate(){
            for (var i = 0; i < PooledPrefabsList.Count; i++){
                var prefab = PooledPrefabsList[i].Prefab;
                if (prefab != null)
                    Assert.IsNotNull(
                        prefab.GetComponent<NetworkObject>(), 
                        $"{nameof(NetcodeObjectPool)}: " 
                        + $"Pooled prefab \"{prefab.name}\" at index {i.ToString()}" 
                        + $" has no {nameof(NetworkObject)} component."
                    );
            }
        }

        public override void OnNetworkSpawn(){
            // REGISTER TO CACHE //
            foreach (var configObject in PooledPrefabsList)
                RegisterPrefabInternal(configObject.Prefab, configObject.PrewarmCount);
            Debug.Log($"[{gameObject.name}] OnNetworkSpawn", gameObject);
        }

        public override void OnNetworkDespawn(){
            // UNREGISTER FROM CACHE //
            foreach (var prefab in m_Prefabs){
                // UNREGISTER FROM PREFABS //
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
                m_PooledObjects[prefab].Clear();
            }
            
            m_PooledObjects.Clear();
            m_Prefabs.Clear();
        }

        //---------//
        // METHODS //
        //---------//
        /// <summary>
        /// Gets an instance of the given prefab from the pool. The prefab must be registered to the pool.
        /// </summary>
        /// <remarks>
        /// To spawn a NetworkObject from one of the pools, this must be called on the server, then the instance
        /// returned from it must be spawned on the server. This method will then also be called on the client by the
        /// PooledPrefabInstanceHandler when the client receives a spawn message for a prefab that has been registered
        /// here.
        /// </remarks>
        /// <param name="prefab"></param>
        /// <param name="position">The position to spawn the object at.</param>
        /// <param name="rotation">The rotation to spawn the object with.</param>
        /// <returns></returns>
        public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation){
            var networkObject = m_PooledObjects[prefab].Get();
            var noTransform = networkObject.transform;
            noTransform.position = position;
            noTransform.rotation = rotation;
            Debug.Log($"GetNetworkObject {prefab.name}" );
            return networkObject;
        }

        /// <summary>
        /// Return an object to the pool (reset objects before returning).
        /// </summary>
        public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab){
            m_PooledObjects[prefab].Release(networkObject);
            Debug.Log($"ReturnNetworkObject {prefab.name}" );
        }

        /// <summary>
        /// Builds up the cache for a prefab.
        /// </summary>
        private void RegisterPrefabInternal(GameObject prefab, int prewarmCount){
            #region LOCAL FUNCTIONS
            NetworkObject OnCreateAction()
                => Instantiate(prefab).GetComponent<NetworkObject>();

            void OnGetAction(NetworkObject networkObject)
                => networkObject.gameObject.SetActive(true);

            void OnReleaseAction(NetworkObject networkObject)
                => networkObject.gameObject.SetActive(false);

            void OnDestroyAction(NetworkObject networkObject)
                => Destroy(networkObject.gameObject);
            #endregion
            m_Prefabs.Add(prefab);

            // CREATE POOL //
            m_PooledObjects[prefab] = new ObjectPool<NetworkObject>(OnCreateAction, OnGetAction, OnReleaseAction, OnDestroyAction, defaultCapacity: prewarmCount);

            // POPULATE POOL //
            var prewarmNetworkObjects = new List<NetworkObject>();
            for (var i = 0; i < prewarmCount; i++)
                prewarmNetworkObjects.Add(m_PooledObjects[prefab].Get());
            foreach (var networkObject in prewarmNetworkObjects)
                m_PooledObjects[prefab].Release(networkObject);
            
            // REGISTER TO HANDLER //
            NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));
            Debug.Log("RegisterPrefabInternal");
        }
    }
    
    [Serializable]
    internal struct PoolConfigObject{
        public GameObject Prefab;
        public int PrewarmCount;
    }

    internal class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler{
        private readonly GameObject m_Prefab;
        private readonly NetcodeObjectPool m_Pool;

        public PooledPrefabInstanceHandler(GameObject prefab, NetcodeObjectPool pool){
            m_Prefab = prefab;
            m_Pool = pool;
        }

        NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            => m_Pool.GetNetworkObject(m_Prefab, position, rotation);

        void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
            =>  m_Pool.ReturnNetworkObject(networkObject, m_Prefab);
    }
}