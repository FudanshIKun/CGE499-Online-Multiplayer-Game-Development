using UnityEngine;

namespace ToruToru{
    internal class SimpleBullet : MonoBehaviour, IDestructible{
        //-----------//
        // INSPECTOR //
        //-----------//
        public NetCodeActor Owner { get; set; } = null;
        public Rigidbody Rigidbody { get; set; }
        public Destructible Destruction { get; set; }
        
        [SerializeField] 
        private AudioClip OnShootClip;
        [SerializeField] 
        private AudioClip OnImpactClip;
        [SerializeField] 
        private GameObject OnImpactParticle;
        [SerializeField] 
        private float ImpactScale;
        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        private void Awake(){
            Destruction = GetComponent<Destructible>();
            Rigidbody = GetComponent<Rigidbody>();
            Debug.Log($"[{gameObject.name}] Awake", gameObject);
        }

        private void OnTriggerEnter(Collider other){
            var manager = NetcodeGameManager.Instance;
            if (other.gameObject == Owner.gameObject) return;
            if (other.TryGetComponent<NetCodeActor>(out var actor)){
                switch (manager.IsOwner){
                    case true when actor.IsOwner == false:
                        manager.OnClientDied();
                        break;
                    case false when actor.IsOwner == false:
                        manager.OnHostDied();
                        break;
                }
            }
            
            if (other.TryGetComponent(out IDestructible destructible)){
                var target = destructible.Destruction;
                if (target != null) target.DestroyMesh();
                Destruction.DestroyMesh();
            }
            else
            { Destruction.DestroyMesh(); }
           
            if (OnImpactParticle != null) {
                var OnImpactParticleGO = Instantiate(OnImpactParticle, transform.position, Quaternion.identity);
                OnImpactParticleGO.transform.localScale = new Vector3(ImpactScale, ImpactScale, ImpactScale);
            }
   
            if (OnImpactClip != null) AudioSource.PlayClipAtPoint(OnImpactClip, transform.position);
            Debug.Log($"[{gameObject.name}] OnTriggerEnter", gameObject);
        }
        
        //---------//
        // METHODS //
        //---------//
        public void Init(NetCodeActor creator, Vector3 force){
            creator.SetBulletOwner(this);
            Rigidbody.AddForce(force, ForceMode.Impulse);
            if (OnShootClip != null) AudioSource.PlayClipAtPoint(OnShootClip, transform.position);
        }
    }
}