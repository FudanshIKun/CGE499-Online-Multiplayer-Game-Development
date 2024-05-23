using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

#if UNITY_EDITOR
using ParrelSync;
#endif

namespace ToruToru{
	internal static class AuthenticationService {
		//----------------//
		// STATIC MEMBERS //
		//----------------//
		public static string PlayerId { get; private set; }

		//----------------//
		// STATIC METHODS //
		//----------------//
		public static async Task Start(){
			if (UnityServices.State == ServicesInitializationState.Uninitialized) {
				var options = new InitializationOptions();
				#if UNITY_EDITOR
				options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
				#endif
				await UnityServices.InitializeAsync(options);
			}

			if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn) {
				await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
				PlayerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
			}
		}
	}
}