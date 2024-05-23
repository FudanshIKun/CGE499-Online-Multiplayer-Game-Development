using System;
using UnityEngine;

namespace ToruToru{
	//---------------//
	// STATE MACHINE //
	//---------------//
	[Serializable]
	public abstract class SimpleStateMachine{
		protected SimpleStateMachine(MonoBehaviour owner)
			=> this.owner = owner;
	  
  		//---------//
  		// MEMBERS //
  		//---------//
		public MonoBehaviour owner{ get; }
		public ISimpleState DefaultState { get; protected set; }
		public ISimpleState previousState { get; protected set; }
		public ISimpleState currentState { get; protected set; }

  		//---------------------//
  		// BEHAVIOUR INTERFACE //
  		//---------------------//
		public void OnStart()
			=> ChangeState(DefaultState);
		
		public void OnFixedUpdate()
			=> currentState?.OnFixedUpdate();

		public void OnUpdate()
			=> currentState?.OnUpdate();

		public void OnTriggerEnter(Collider collider)
			=> currentState?.OnTriggerEnter(collider);

		public void OnTriggerExit(Collider collider)
			=> currentState?.OnTriggerExit(collider);
		
		//---------//
		// METHODS //
		//---------//
		public void ChangeState(ISimpleState state){
			if (state == currentState)
				return;
			currentState?.OnExit();
			previousState = currentState;
			currentState = state;
			currentState.OnEnter();
		}
	}
}