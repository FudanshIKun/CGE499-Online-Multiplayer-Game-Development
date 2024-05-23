using System;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ToruToru{
	internal sealed class SceneLoading : IDisposable {
		public SceneLoading() 
			=> SimpleLoading.Instance.StartLoading();
		
		public void Dispose()
			=> SimpleLoading.Instance.StopLoading();
	}

	internal class SimpleLoading : MonoBehaviour {
		//----------------//
		// STATIC MEMBERS //
		//----------------//
		private static SimpleLoading _instance;
		public static SimpleLoading Instance { 
			get{
				if (_instance != null) 
					return _instance;
				var prefab = Addressables.InstantiateAsync("Assets/ToruToru/Prefabs/LoadingUtilities.prefab").Result;
				_instance = prefab.GetComponent<SimpleLoading>();
				prefab.hideFlags = HideFlags.NotEditable;
				_instance.hideFlags = HideFlags.NotEditable;
				return _instance;
			}
		}

		//-----------//
		// INSPECTOR //
		//-----------//
		public float LoadTime;
		
		private TweenerCore<float, float, FloatOptions> _tween;
		private Vector2 _hotSpot = Vector2.zero;
		//---------------------//
		// BEHAVIOUR INTERFACE //
		//---------------------//
		private void Awake(){
			_instance = this;
			DontDestroyOnLoad(gameObject);
		}
		
		//---------//
		// METHODS //
		//---------//
		public void StartLoading(){
			
		}

		public void StopLoading(){
			
		}

		public void ShowError(string text){
			
		}
	}
}