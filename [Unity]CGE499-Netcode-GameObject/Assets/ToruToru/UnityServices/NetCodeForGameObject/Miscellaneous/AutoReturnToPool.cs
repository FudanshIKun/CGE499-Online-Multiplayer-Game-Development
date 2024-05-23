using Unity.Netcode;
using UnityEngine;

namespace ToruToru{
    [RequireComponent(typeof(NetworkObject))]
    internal class AutoReturnToPool : NetworkBehaviour{
        //-----------//
        // INSPECTOR //
        //-----------//
        [Header("Time alive in seconds (s)")]
        [Min(0f)] [SerializeField]
        private float m_autoDestroyTime;

        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        public override void OnNetworkSpawn(){
            if (!IsServer) 
                enabled = false;
        }

        private void Update(){
            if (!IsServer) return;
            if (gameObject.activeInHierarchy == false) return;
            m_autoDestroyTime -= Time.deltaTime;
            if(m_autoDestroyTime <= 0f)
                NetcodeObjectPool.Instance.ReturnNetworkObject(NetworkObject, gameObject);
        }
    }
}