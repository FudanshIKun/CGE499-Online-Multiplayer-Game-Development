using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Cinemachine;
using JetBrains.Annotations;
using Rewired.ComponentControls;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace ToruToru{
	/// <summary>
	/// 
	/// </summary>
	internal class NetcodeGameManager : NetworkBehaviour {
		//----------------//
		// STATIC MEMBERS //
		//----------------//
		public static NetcodeGameManager Instance { get; private set; }

		//-----------//
		// INSPECTOR //
		//-----------//
		public List<NetCodeActor> Actors = new();
		
		//--------------------//
		[Header("Components")]
		public CinemachineTargetGroup Group;
		public Transform LeftSpawnPoint;
		public Transform RightSpawnPoint;
		[SerializeField] 
		protected GameObject HostPrefab;
		[SerializeField] 
		protected GameObject ClientPrefab;
		[SerializeField] 
		protected float RespawnDuration;
		public NetworkVariable<int> HostScore;
		public NetworkVariable<int> ClientScore;
		public TMP_Text HostScoreText;
		public TMP_Text ClientScoreText;
		public TMP_Text GameResultText;
		public NetworkVariable<bool> TimerIsActive;
		public TMP_Text TimerText;
		public float MaximumTimer;
		public NetworkVariable<float> Timer;
		
		//--------------------//
		[Header("Controller")]
		public TouchJoystick LeftStick;
		public TouchJoystick RightStick;
		
		//----------------//
		[Header("Events")]
		public UnityEvent OnGameStart;
		public UnityEvent OnGameStop;

		//---------------------//
		// BEHAVIOUR INTERFACE //
		//---------------------//
		private void Awake() {
			if (Instance != null && Instance != this)
			{ Destroy(gameObject); }
			else
			{ Instance = this; }
			Debug.Log($"[{gameObject.name}] Awake", gameObject);
		}
		
		public override void OnNetworkSpawn() {
			// HANDLE CONNECTION //
			AddOnClientDisconnectCallback();
			
			// HANDLE GAMEPLAY //
			TimerText.text = "00:00";
			HostScoreText.text = "0";
			ClientScoreText.text = "0";
			GameResultText.SetText("");
			if (IsServer){
				Timer.Value = MaximumTimer;
				HostScore.Value = 0;
				ClientScore.Value = 0;
			}
			else{
				if (Mathf.Approximately(Timer.Value, MaximumTimer))
				{ Debug.LogWarning($"NetworkVariable was {Timer.Value} upon being spawned" + $" when it should have been {MaximumTimer}"); }
				else
				{ Debug.Log($"NetworkVariable is {Timer.Value} when spawned."); }
			}
			
			Timer.OnValueChanged += OnTimerValueChanged;
			HostScore.OnValueChanged += OnHostScoreValueChanged;
			ClientScore.OnValueChanged += OnClientScoreValueChanged;
			Debug.Log($"[{gameObject.name}] OnNetworkSpawn", gameObject);
		}

		private void Start() {
			if (IsSpawned == false) return;
			var id = NetworkManager.Singleton.LocalClientId;
			SpawnPlayerServerRpc(id);
			// START GAME //
			if (IsServer == false)
			{ StartGameServerRpc(); }
		}

		private void Update() {
			if (IsSpawned == false) return;
			if (Group == null) 
				Group = FindObjectOfType<CinemachineTargetGroup>();
			var players = GameObject.FindGameObjectsWithTag("Player");
			for (var i = 0; i < players.Length; i++) {
				if (Group.m_Targets[i].target != null) continue;
				CinemachineTargetGroup.Target actor;
				actor.target = players[i].transform;
				actor.weight = .5f;
				actor.radius = .5f;
				if (Group.m_Targets.Any(target => target.target == actor.target)) continue;
				Group.m_Targets.SetValue(actor, i);
			} 

			if (IsServer == false) return;
			if (TimerIsActive.Value && Timer.Value > 0)
				Timer.Value -= Time.unscaledDeltaTime;
		}

		public override void OnNetworkDespawn(){
			base.OnNetworkDespawn();
			Timer.OnValueChanged -= OnTimerValueChanged;
			HostScore.OnValueChanged -= OnHostScoreValueChanged;
			ClientScore.OnValueChanged -= OnClientScoreValueChanged;
		}

		public override void OnDestroy()
			=> RemoveOnClientDisconnectCallback();

		//------------//
		// SERVER RPC //
		//------------//
		[ServerRpc(RequireOwnership = false)]
		private void SpawnPlayerServerRpc(ulong playerId){
			var actor = playerId == OwnerClientId 
				? Instantiate(HostPrefab, LeftSpawnPoint.position, Quaternion.LookRotation(Vector3.right))
					.GetComponent<NetCodeActor>() 
				: Instantiate(ClientPrefab, RightSpawnPoint.position, Quaternion.LookRotation(Vector3.left))
					.GetComponent<NetCodeActor>();
			actor.ClientId = playerId;
			actor.NetworkObject.SpawnWithOwnership(playerId);
			Actors.Add(actor);
			Debug.Log($"[{gameObject.name}] SpawnPlayerServerRpc", gameObject);
		}
		
		[ServerRpc(RequireOwnership = false)]
		public void StartGameServerRpc(){
			Transitioner.Instance.FinishTransition();
			OnGameStart?.Invoke();
			StartGameClientRpc();
			TimerIsActive.Value = true;
			Debug.Log($"[{gameObject.name}] StartGameServerRpc", gameObject);
		}
		
		[ServerRpc(RequireOwnership = false)]
		public void ChangeHostScoreServerRpc(int score)
			=> HostScore.Value = score;

		[ServerRpc(RequireOwnership = false)]
		public void RespawnHostServerRpc(){
			if (IsServer == false) return;
			RespawnHostClientRpc();
			Debug.Log($"[{gameObject.name}] RespawnHostServerRpc", gameObject);
		}

		[ServerRpc(RequireOwnership = false)]
		public void ChangeClientScoreServerRpc(int score)
			=> ClientScore.Value = score;
		
		[ServerRpc(RequireOwnership = false)]
		public void RespawnClientServerRpc(){
			RespawnClientClientRpc();
			Debug.Log($"[{gameObject.name}] RespawnClientServerRpc", gameObject);
		}
		
		//------------//
		// CLIENT RPC //
		//------------//
		[ClientRpc]
		public void StartGameClientRpc(){
			Transitioner.Instance.FinishTransition();
			OnGameStart.Invoke();
			Debug.Log($"[{gameObject.name}] StartClientGame", gameObject);
		}
		
		[ClientRpc]
		public void StopGameClientRpc(){
			string result;
			if (HostScore.Value == ClientScore.Value)
			{ result = "Draw!"; }
			else if (HostScore.Value > ClientScore.Value)
			{ result = "Host Win!"; }
			else
			{ result = "Client Win!"; }
			StopGame(result);
			OnGameStopped();
			Debug.Log($"[{gameObject.name}] StopGameClientRpc", gameObject);
		}

		[ClientRpc]
		public void RespawnHostClientRpc(){
			StartCoroutine(Respawn(NetCodeActor.Host));
			Debug.Log($"[{gameObject.name}] RespawnHostClientRpc", gameObject);
		}
		
		[ClientRpc]
		public void RespawnClientClientRpc(){
			StartCoroutine(Respawn(NetCodeActor.Client));
			Debug.Log($"[{gameObject.name}] RespawnClientClientRpc", gameObject);
		}
		
		//---------//
		// METHODS //
		//---------//
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
		
		private IEnumerator Respawn(NetCodeActor actor){
			actor.SetActive(false);
			actor.IsAlive = false;
			Debug.Log("Start Respawn", actor.gameObject);
			yield return null;
			actor.transform.position = NetworkManager.IsServer ? LeftSpawnPoint.position : RightSpawnPoint.position;
			actor.Rigidbody.velocity = Vector3.zero;
			yield return new WaitForSeconds(RespawnDuration);
			actor.SetActive(true);
			actor.IsAlive = true;
			Debug.Log("Stop Respawn", actor.gameObject);
		}
		
		public void OnHostDied(){
			ChangeClientScoreServerRpc(ClientScore.Value + 1);
			RespawnHostServerRpc();
			Debug.Log($"[{gameObject.name}] OnHostDied", gameObject);
		}

		public void OnClientDied(){
			ChangeHostScoreServerRpc(HostScore.Value + 1);
			RespawnClientServerRpc();
			Debug.Log($"[{gameObject.name}] OnClientDied", gameObject);
		}
		
		//--------------------------------//
		private void OnHostScoreValueChanged(int previous, int current)
			=> UpdateHostScore(current);
		
		private void UpdateHostScore(int value)
			=> HostScoreText.SetText(value.ToString());
		
		private void OnClientScoreValueChanged(int previous, int current)
			=> UpdateClientScore(current);

		private void UpdateClientScore(int value)
			=> ClientScoreText.SetText(value.ToString());

		//--------------------------------//
		private void OnTimerValueChanged(float previous, float current){
			UpdateTimer(current);
			if (IsServer && current <= 0) StopGameClientRpc();
		}
		
		private void UpdateTimer(float value){
			var clamped = Mathf.Clamp(value, 0f, MaximumTimer);
			var minutes = Mathf.FloorToInt(clamped / 60f);
			var seconds = Mathf.FloorToInt(clamped - minutes * 60);
			var formatted = $"{minutes:0}:{seconds:00}";
			TimerText.text = formatted;
		}
		
		//--------------------------------//
		public void StopGame(string result){
			OnGameStop.Invoke();
			if (IsServer) TimerIsActive.Value = false;
			if (GameResultText.text == "")
				GameResultText.SetText(result);
		}

		private async void OnGameStopped(){
			await Task.Delay(5000);
			await LeaveGame();
			if (Instance!= null) Instance.ReturnToLobby();
		}
		
		private async void OnClientDisconnectCallback(ulong playerId){
			StopGame(IsServer ? "Host Win!" : "Client Win!");
			await LeaveGame();
			ReturnToLobby();
			Debug.Log($"[{gameObject.name}] OnClientDisconnectCallback", gameObject);
		}
		
		private Task LeaveGame(){
			Debug.Log($"[{gameObject.name}] LeaveGame", gameObject);
			NetworkManager.Singleton.Shutdown();
			return MatchmakingService.LeaveLobby();
		}

		private void ReturnToLobby(){
			Debug.Log($"[{gameObject.name}] ReturnToLobby", gameObject);
			Transitioner.Instance.TransitionToScene("Lobby");
		}
	}
}