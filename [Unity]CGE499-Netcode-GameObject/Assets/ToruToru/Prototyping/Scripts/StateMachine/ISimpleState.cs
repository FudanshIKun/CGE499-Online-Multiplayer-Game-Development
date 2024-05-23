using UnityEngine;

namespace ToruToru{
    public interface ISimpleState{
        public void OnEnter();
        public void OnFixedUpdate();
        public void OnUpdate();
        public void OnExit();
        public void OnTriggerEnter(Collider collider);
        public void OnTriggerExit(Collider collider);
    }
}