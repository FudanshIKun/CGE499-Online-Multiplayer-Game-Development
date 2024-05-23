using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ToruToru{
	/// <summary>
	/// Infinitely scroll RawImage in specific direction
	/// </summary>
	internal sealed class RawImageScroller : MonoBehaviour {
		//-----------//
		// INSPECTOR //
		//-----------//
		[SerializeField] 
		private Vector2 Direction = new(0, 0.01f);
		private RawImage Image;

		//---------------------//
		// BEHAVIOUR INTERFACE //
		//---------------------//
		private void Awake()
			=> Image = GetComponent<RawImage>();

		private void Update()
			=> Image.uvRect = new Rect(Image.uvRect.position + Direction * Time.fixedDeltaTime, Image.uvRect.size);
	}
}