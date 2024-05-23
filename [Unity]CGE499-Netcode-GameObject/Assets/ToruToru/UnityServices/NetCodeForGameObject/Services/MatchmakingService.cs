using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ToruToru{
	/// <summary>
	/// 
	/// </summary>
	internal static class MatchmakingService{
		//---------------//
		// CONST MEMBERS //
		//---------------//
		private const int HeartbeatInterval = 15;
		private const int LobbyRefreshRate  = 2; // UNITY LIMITED THE RATE AT 2

		//----------------//
		// STATIC MEMBERS //
		//----------------//
		public static event Action<Lobby> CurrentLobbyRefreshed;
		private static UnityTransport transport;
		private static UnityTransport Transport{ 
			get => transport != null ? transport : transport = Object.FindObjectOfType<UnityTransport>();
			set => transport = value; 
		}
		
		private static Lobby currentLobby;
		private static CancellationTokenSource heartbeatSource;
		private static CancellationTokenSource updateLobbySource;
		//----------------//
		// STATIC METHODS //
		//----------------//
		public static async Task CreateLobbyWithAllocation(LobbyData data){
			// CREATE RELAY ALLOCATION AND GENERATE JOIN CODE FOR THE LOBBY //
			var allocation = await RelayService.Instance.CreateAllocationAsync(data.MaxPlayers);
			var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			var options = new CreateLobbyOptions{
				Data = new Dictionary<string, DataObject>{
					{ Constants.JoinKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) },{
						Constants.GameTypeKey,
						new DataObject(DataObject.VisibilityOptions.Public, data.Type.ToString(),
							DataObject.IndexOptions.N1)
					}
				}
			};

			currentLobby = await Lobbies.Instance.CreateLobbyAsync(data.Name, data.MaxPlayers, options);
			Transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);
			Heartbeat();
			PeriodicallyRefreshLobby();
		}
		
		public static async Task LockLobby(){
			try
			{ await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions{ IsLocked = true }); }
			catch (Exception e)
			{ Debug.Log($"Failed closing lobby: {e}"); }
		}
		
		public static async Task JoinLobbyWithAllocation(string lobbyId){
			currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
			var a = await RelayService.Instance.JoinAllocationAsync(currentLobby.Data[Constants.JoinKey].Value);
			Transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key,
				a.ConnectionData, a.HostConnectionData);
			PeriodicallyRefreshLobby();
		}
		
		/// <summary>
		/// Simple Gathering without any customization in the query
		/// </summary>
		/// <returns></returns>
		public static async Task<List<Lobby>> GatherLobbies(){
			var options = new QueryLobbiesOptions{
				Count = 15,
				Filters = new List<QueryFilter>{
					new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
					new(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
				}
			};

			var allLobbies = await Lobbies.Instance.QueryLobbiesAsync(options);
			return allLobbies.Results;
		}
		
		public static async Task LeaveLobby(){
			heartbeatSource?.Cancel();
			updateLobbySource?.Cancel();
			if (currentLobby != null)
				try{
					if (currentLobby.HostId == AuthenticationService.PlayerId)
					{ await Lobbies.Instance.DeleteLobbyAsync(currentLobby.Id); }
					else
					{ await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.PlayerId); }
					currentLobby = null;
				}
				catch (Exception e)
				{ Debug.Log(e); }
		}
		
		public static void ResetStatics(){
			if (Transport != null){
				Transport.Shutdown();
				Transport = null;
			}

			currentLobby = null;
		}

		private static async void Heartbeat(){
			heartbeatSource = new CancellationTokenSource();
			while (!heartbeatSource.IsCancellationRequested && currentLobby != null){
				await Lobbies.Instance.SendHeartbeatPingAsync(currentLobby.Id);
				await Task.Delay(HeartbeatInterval * 1000);
			}
		}

		private static async void PeriodicallyRefreshLobby(){
			updateLobbySource = new CancellationTokenSource();
			await Task.Delay(LobbyRefreshRate * 1000);
			while (updateLobbySource.IsCancellationRequested == false && currentLobby != null){
				currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);
				CurrentLobbyRefreshed?.Invoke(currentLobby);
				await Task.Delay(LobbyRefreshRate * 1000);
			}
		}
	}
}