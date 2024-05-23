using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ToruToru{
	/// <summary>
	/// Bootstrapper Is the class that'll run before any script in the scene
	/// </summary>
	internal static class Bootstrapper{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize(){
			MatchmakingService.ResetStatics();
			Addressables.InstantiateAsync("Assets/ToruToru/Prefabs/LoadingUtilities.prefab");
		}
	}
}