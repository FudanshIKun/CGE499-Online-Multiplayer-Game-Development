using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace ToruToru{
	/// <summary>
    /// Manage components on screen when user join a room.
    /// </summary>
    internal class GameRoomScreen : MonoBehaviour {
        //----------------//
        // STATIC MEMBERS //
        //----------------//
        public static event Action StartPressed; 
        public static event Action LobbyLeft;
        
        //-----------//
        // INSPECTOR //
        //-----------//
        [SerializeField] 
        protected LobbyPlayerInstance PlayerInstancePrefab;
        
        [SerializeField] 
        protected Transform PlayerInstanceParent;
         
        [SerializeField] 
        protected TMP_Text WaitingText;
        
        [SerializeField] 
        private GameObject startButton;

        [SerializeField] 
        private GameObject readyButton;

        protected readonly List<LobbyPlayerInstance> Instances = new();
        private bool allReady;
        private bool ready;
        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        private void OnEnable() {
            foreach (Transform child in PlayerInstanceParent) 
                Destroy(child.gameObject);
            Instances.Clear();
            LobbyManager.LobbyPlayersUpdated += NetworkLobbyPlayersUpdated;
            MatchmakingService.CurrentLobbyRefreshed += OnCurrentLobbyRefreshed;
            startButton.SetActive(false);
            readyButton.SetActive(false);
            ready = false;
        }

        private void OnDisable() {
            LobbyManager.LobbyPlayersUpdated -= NetworkLobbyPlayersUpdated;
            MatchmakingService.CurrentLobbyRefreshed -= OnCurrentLobbyRefreshed;
        }

        //---------//
        // METHODS //
        //---------//
        public void OnReadyClicked() {
            readyButton.SetActive(false);
            ready = true;
        }

        public void OnStartClicked()
            => StartPressed?.Invoke();
        
        public void OnLeaveLobby() 
            => LobbyLeft?.Invoke();

        private void NetworkLobbyPlayersUpdated(Dictionary<ulong, bool> players) {
            var allActivePlayerIds = players.Keys;

            // REMOVE INACTIVE INSTANCE //
            var toDestroy = Instances.Where(instance => !allActivePlayerIds.Contains(instance.PlayerId)).ToList();
            foreach (var panel in toDestroy) {
                Instances.Remove(panel);
                Destroy(panel.gameObject);
            }

            foreach (var player in players) {
                var currentPanel = Instances.FirstOrDefault(instance => instance.PlayerId == player.Key);
                if (currentPanel != null) 
                { if (player.Value) currentPanel.SetReady(); }
                else {
                    var panel = Instantiate(PlayerInstancePrefab, PlayerInstanceParent);
                    panel.Init(player.Key);
                    Instances.Add(panel);
                }
            }

            startButton.SetActive(NetworkManager.Singleton.IsHost && players.All(player => player.Value));
            readyButton.SetActive(!ready);
        }

        private void OnCurrentLobbyRefreshed(Lobby lobby) 
           => WaitingText.text = $"Waiting for {lobby.Players.Count}/{lobby.MaxPlayers}";
    }
}