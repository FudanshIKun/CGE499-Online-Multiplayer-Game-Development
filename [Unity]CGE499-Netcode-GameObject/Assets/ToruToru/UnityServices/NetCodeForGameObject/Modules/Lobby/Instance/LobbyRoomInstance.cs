using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;


namespace ToruToru{
	/// <summary>
	/// 
	/// </summary>
	internal class LobbyRoomInstance : MonoBehaviour {
		//----------------//
		// STATIC MEMBERS //
		//----------------//
		public static event Action<Lobby> LobbySelected;
		
		//-----------//
		// INSPECTOR //
		//-----------//
		[SerializeField] 
        protected TMP_Text  NameText, TypeText, PlayerCountText;

		public Lobby Lobby { get; private set; }

		//---------//
		// METHODS //
		//---------//
		public void Init(Lobby lobby)
			=> UpdateDetails(lobby);

		public void UpdateDetails(Lobby lobby){
			Lobby = lobby;
			NameText.text = lobby.Name;
			TypeText.text = Constants.GameTypes[GetValue(Constants.GameTypeKey)];
			PlayerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
			return;

			int GetValue(string key)
				=> int.Parse(lobby.Data[key].Value);
		}

		public void Clicked(){
			LobbySelected?.Invoke(Lobby);
		}
	}
}