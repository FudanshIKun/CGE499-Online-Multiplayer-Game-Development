using System;
using UnityEngine;

namespace ToruToru{
    [Serializable]
    public abstract class SimpleState : ISimpleState{
        protected SimpleState(SimpleStateMachine stateMachine){
            machine = stateMachine;
            Transform = stateMachine.owner.transform;
            Rigidbody = stateMachine.owner.GetComponent<Rigidbody>();
            Animator = stateMachine.owner.GetComponent<Animator>();
        }
		
        //---------//
        // MEMBERS //
        //---------//
        protected SimpleStateMachine machine;
        protected Transform Transform { get; }
        protected Rigidbody Rigidbody { get; }
        protected Animator Animator { get; }

        //---------//
        // METHODS //
        //---------//
        public virtual void OnEnter()
            => Debug.Log($"[Simple StateMachine] OnEnter ({GetType().Name}) state.");

        public virtual void OnFixedUpdate()
            => Debug.Log($"[Simple StateMachine] OnFixedUpdate ({GetType().Name}) state.");

        public virtual void OnUpdate()
            => Debug.Log($"[Simple StateMachine] OnUpdate ({GetType().Name}) state.");

        public virtual void OnExit()
            => Debug.Log($"[Simple StateMachine] OnExit ({GetType().Name}) state.");

        public virtual void OnTriggerEnter(Collider collider)
            => Debug.Log($"[Simple StateMachine] OnTriggerEnter ({GetType().Name}) state with {collider.gameObject.name}.");

        public virtual void OnTriggerExit(Collider collider)
            => Debug.Log($"[Simple StateMachine] OnTriggerExit ({GetType().Name}) state with {collider.gameObject.name}.");
    }
}