using UnityEngine;

namespace ToruToru{
	/// <summary>
	/// Simple SignIn Logics.
	/// </summary>
	internal class AuthenticationManager : MonoBehaviour{
		//-----------//
		// INSPECTOR //
		//-----------//
		[SerializeField] 
		protected string NextSceneAfterAuthentication = "Lobby";
		
		//---------//
		// METHODS //
		//---------//
		public async void LoginAnonymously() {
			using (new SceneLoading()){
				await AuthenticationService.Start();
				Transitioner.Instance.TransitionToScene(NextSceneAfterAuthentication);
			}
		}

		public void Exit() {
			Application.Quit();
			#if (UNITY_EDITOR)
			UnityEditor.EditorApplication.isPlaying = false;
			#endif
		}
	}
}