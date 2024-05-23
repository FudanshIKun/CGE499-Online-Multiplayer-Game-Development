using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace ToruToru{
	/// <summary>
	/// 
	/// </summary>
	internal class LobbyPlayerInstance : MonoBehaviour {
		//-----------//
		// INSPECTOR //
		//-----------//
		[SerializeField] 
		private TMP_Text nameText;

		[SerializeField] 
		private TMP_Text statusText;

		public ulong PlayerId { get; private set; }

		//---------//
		// METHODS //
		//---------//
		public void Init(ulong playerId){
			PlayerId = playerId;
			nameText.text = playerId switch{
				0 => "Host",
				_ => "Client"
			};
		}

		public void SetReady(){
			statusText.text = "Ready";
			statusText.color = Color.green;
		}
	}
}