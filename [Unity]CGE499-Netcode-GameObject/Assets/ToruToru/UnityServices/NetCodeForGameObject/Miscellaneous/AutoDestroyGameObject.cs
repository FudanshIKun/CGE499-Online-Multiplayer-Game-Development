using UnityEngine;

namespace ToruToru {
    internal class AutoDestroyGameObject  : MonoBehaviour {
        //-----------//
        // INSPECTOR //
        //-----------//
        [SerializeField] 
        protected float LifeTime;

        private float timer;
        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        private void Start()
            => timer = LifeTime;

        private void Update() {
            if (timer > 0) 
            { timer -= Time.unscaledDeltaTime; }
            else
            { Destroy(gameObject); }
        }
    }
}