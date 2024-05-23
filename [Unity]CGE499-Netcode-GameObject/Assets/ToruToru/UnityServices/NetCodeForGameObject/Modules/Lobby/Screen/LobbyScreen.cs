using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace ToruToru{
    /// <summary>
    /// Manage lobbies on screen.
    /// </summary>
    internal class LobbyScreen : MonoBehaviour {
        //-----------//
        // INSPECTOR //
        //-----------//
        [SerializeField] 
        private LobbyRoomInstance prefab;
        
        [SerializeField] 
        private Transform parent;
        
        [SerializeField] 
        private GameObject noLobbiesText;
        
        [SerializeField] 
        private float refreshRate = 2;

        private readonly List<LobbyRoomInstance> lobbies = new();
        private float nextRefreshTime;

        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        private void OnEnable(){
            foreach (Transform child in parent) Destroy(child.gameObject);
            lobbies.Clear();
        }
        
        private void Update(){
            if (Time.time >= nextRefreshTime) 
                FetchLobbies();
        }

        //---------//
        // METHODS //
        //---------//
        private async void FetchLobbies() {
            try {
                nextRefreshTime = Time.time + refreshRate;
                var allLobbies = await MatchmakingService.GatherLobbies();
                var ids = allLobbies.Where(lobby => lobby.HostId != AuthenticationService.PlayerId).Select(lobby => lobby.Id);
                var unActive = lobbies.Where(lobby => !ids.Contains(lobby.Lobby.Id)).ToList();

                foreach (var panel in unActive) {
                    Destroy(panel.gameObject);
                    lobbies.Remove(panel);
                }
                
                foreach (var lobby in allLobbies) {
                    var current = lobbies.FirstOrDefault(p => p.Lobby.Id == lobby.Id);
                    if (current != null) 
                    { current.UpdateDetails(lobby); }
                    else {
                        var panel = Instantiate(prefab, parent);
                        panel.Init(lobby);
                        lobbies.Add(panel);
                    }
                }

                if (noLobbiesText != null)
                    noLobbiesText.SetActive(lobbies.Any() == false);
            }
            catch (Exception e) 
            { Debug.LogError(e); }
        }
    }
}