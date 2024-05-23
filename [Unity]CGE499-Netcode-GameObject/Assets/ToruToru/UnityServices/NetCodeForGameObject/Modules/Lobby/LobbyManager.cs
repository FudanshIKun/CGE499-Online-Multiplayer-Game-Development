using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AYellowpaper.SerializedCollections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToruToru{
	/// <summary>
	/// Lobby management logics.
	/// </summary>
	internal sealed class LobbyManager : NetworkBehaviour{
		//----------------//
		// STATIC MEMBERS //
		//----------------//
		public static event Action<Dictionary<ulong, bool>> LobbyPlayersUpdated;
		
		//-----------//
		// INSPECTOR //
		//-----------//
		[SerializeField]
		private LobbyScreen LobbyScreen;
		[SerializeField] 
		private CreateLobbyScreen CreateLobbyScreen;
		[SerializeField] 
		private GameRoomScreen GameRoomScreen;

		public SerializedDictionary<ulong, bool> Players = new();
		private float timer;
		//---------------------//
		// BEHAVIOUR INTERFACE //
		//---------------------//
		private void Awake(){
			Players = new SerializedDictionary<ulong, bool>();
			Debug.Log($"[{gameObject.name}] Awake", gameObject);
		}

		public override void OnNetworkSpawn(){
			if (IsServer){
				AddOnClientConnectedCallback();
				Players.Add(NetworkManager.Singleton.LocalClientId, false);
				UpdateLobbyInterface();
				return;
			}

			// CLIENT USE THIS WHEN HOST DESTROY THE LOBBY //
			AddOnClientDisconnectCallback();
			Debug.Log($"[{gameObject.name}] OnNetworkSpawn", gameObject);
		}
		
		private void Start(){
			LobbyScreen.gameObject.SetActive(true);
			CreateLobbyScreen.gameObject.SetActive(false);
			GameRoomScreen.gameObject.SetActive(false);
			CreateLobbyScreen.LobbyCreated += CreateLobby;
			LobbyRoomInstance.LobbySelected += OnLobbySelected;
			GameRoomScreen.LobbyLeft += OnLeaveLobby;
			GameRoomScreen.StartPressed += OnGameStart;
			NetworkObject.DestroyWithScene = true;
			Debug.Log($"[{gameObject.name}] Start", gameObject);
		}
		
		public override void OnDestroy(){
			base.OnDestroy();
			CreateLobbyScreen.LobbyCreated -= CreateLobby;
			LobbyRoomInstance.LobbySelected -= OnLobbySelected;
			GameRoomScreen.LobbyLeft -= OnLeaveLobby;
			GameRoomScreen.StartPressed -= OnGameStart;
			if (IsServer) RemoveOnClientConnectedCallback();
			RemoveOnClientDisconnectCallback();
			Debug.Log($"[{gameObject.name}] OnDestroy", gameObject);
		}
		
		//------------//
		// SERVER RPC //
		//------------//
		[ServerRpc(RequireOwnership = false)]
		private void SetReadyServerRpc(ulong playerId){
			Players[playerId] = true;
			PropagateToClients();
			UpdateLobbyInterface();
			Debug.Log($"[{gameObject.name}] SetReadyServerRpc", gameObject);
		}

		//------------//
		// CLIENT RPC //
		//------------//
		[ClientRpc]
		private void UpdatePlayerClientRpc(ulong clientId, bool isReady){
			if (IsServer) return;
			Players[clientId] = isReady;
			UpdateLobbyInterface();
			Debug.Log($"[{gameObject.name}] UpdatePlayerClientRpc", gameObject);
		}

		[ClientRpc]
		private void RemovePlayerClientRpc(ulong clientId){
			if (IsServer) return;
			if (Players.ContainsKey(clientId)) Players.Remove(clientId);
			UpdateLobbyInterface();
			Debug.Log($"[{gameObject.name}] RemovePlayerClientRpc", gameObject);
		}
		
		[ClientRpc]
		private void TransitionOutClientRpc()
			=> Transitioner.Instance.TransitionOutWithoutChangingScene();
		
		//---------//
		// METHODS //
		//---------//
		private void AddOnClientConnectedCallback(){
			if (NetworkManager.Singleton == null) return;
			NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
			Debug.Log($"[{gameObject.name}] AddOnClientConnectedCallback", gameObject);
		}
		
		private void RemoveOnClientConnectedCallback(){
			if (NetworkManager.Singleton == null) return;
			NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
			Debug.Log($"[{gameObject.name}] RemoveOnClientConnectedCallback", gameObject);
		}
		
		private void AddOnClientDisconnectCallback(){
			if (NetworkManager.Singleton == null) return;
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
			Debug.Log($"[{gameObject.name}] AddOnClientDisconnectCallback", gameObject);
		}

		private void RemoveOnClientDisconnectCallback(){
			if (NetworkManager.Singleton == null) return;
			NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
			Debug.Log($"[{gameObject.name}] RemoveOnClientDisconnectCallback", gameObject);
		}
		
		private async void OnGameStart(){
			using (new SceneLoading()){
				await MatchmakingService.LockLobby();
				TransitionOutClientRpc();
				await Task.Delay(TimeSpan.FromSeconds(Transitioner.Instance._transitionTime));
				NetworkManager.Singleton.SceneManager.LoadScene("GameMatch", LoadSceneMode.Single);
			}
		}
		
		private void UpdateLobbyInterface()
			=> LobbyPlayersUpdated?.Invoke(Players);
		
		public void OnReadyClicked()
			=> SetReadyServerRpc(NetworkManager.Singleton.LocalClientId);
		
		private async void CreateLobby(LobbyData data){
			using (new SceneLoading()){
				try{
					await MatchmakingService.CreateLobbyWithAllocation(data);
					CreateLobbyScreen.gameObject.SetActive(false);
					GameRoomScreen.gameObject.SetActive(true);
					// START THE HOST IMMEDIATELY TO KEEP THE RELAY SERVER ALIVE //
					NetworkManager.Singleton.StartHost();
				}
				catch (Exception e){
					Debug.LogError(e);
					SimpleLoading.Instance.ShowError("Failed creating lobby");
				}
			}
		}
		
		private async void OnLobbySelected(Lobby lobby){
			using (new SceneLoading()){
				try{
					await MatchmakingService.JoinLobbyWithAllocation(lobby.Id);
					LobbyScreen.gameObject.SetActive(false);
					GameRoomScreen.gameObject.SetActive(true);
					NetworkManager.Singleton.StartClient();
				}
				catch (Exception exception){
					Debug.LogError(exception);
					SimpleLoading.Instance.ShowError("Failed joining lobby");
				}
			}
		}

		private void OnClientConnectedCallback(ulong playerId){
			if (IsServer){
				// ADD LOCAL //
				Players.TryAdd(playerId, false);
				PropagateToClients();
				UpdateLobbyInterface();
			}
			
			Debug.Log($"[{gameObject.name}] OnClientConnectedCallback", gameObject);
		}
		
		private void OnClientDisconnectCallback(ulong playerId){
			if (IsServer){
				// HANDLE LOCALLY //
				if (Players.ContainsKey(playerId)) Players.Remove(playerId);
				// PROPAGATE CLIENTS //
				RemovePlayerClientRpc(playerId);
				UpdateLobbyInterface();
			}
			else{
				// IF THE HOST DISCONNECTS THE LOBBY //
				GameRoomScreen.gameObject.SetActive(false);
				LobbyScreen.gameObject.SetActive(true);
				OnLeaveLobby();
			}
			
			Debug.Log($"[{gameObject.name}] OnClientDisconnectCallback", gameObject);
		}
		
		private async void OnLeaveLobby(){
			using (new SceneLoading()){
				Players.Clear();
				NetworkManager.Singleton.Shutdown();
				await MatchmakingService.LeaveLobby();
			}
		}

		private void PropagateToClients(){
			foreach (var player in Players) 
				UpdatePlayerClientRpc(player.Key, player.Value);
		}
	}
}