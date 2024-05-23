using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ToruToru{
	/// <summary>
	/// Manage game room creation.
	/// </summary>
	internal class CreateLobbyScreen : MonoBehaviour{
		//----------------//
		// STATIC MEMBERS //
		//----------------//
		public static event Action<LobbyData> LobbyCreated;
		
		//-----------//
		// INSPECTOR //
		//-----------//
		[SerializeField] 
		private TMP_InputField _nameInput, _maxPlayersInput;
		
		[SerializeField] 
		private TMP_Dropdown   _typeDropdown;

		//---------------------//
		// BEHAVIOUR INTERFACE //
		//---------------------//
		private void Start(){
			SetOptions(_typeDropdown, Constants.GameTypes);
			return;

			void SetOptions(TMP_Dropdown dropdown, IEnumerable<string> values){
				dropdown.options = values.Select(type => new TMP_Dropdown.OptionData{ text = type }).ToList();
			}
		}

		//---------//
		// METHODS //
		//---------//
		public void OnCreateClicked(){
			var lobbyData = new LobbyData{
				Name = _nameInput.text,
				MaxPlayers = int.Parse(_maxPlayersInput.text),
				Type = _typeDropdown.value
			};

			LobbyCreated?.Invoke(lobbyData);
		}
	}
}