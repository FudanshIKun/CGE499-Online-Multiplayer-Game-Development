using System.Collections;
using System.Threading;
using UnityEngine;

namespace ToruToru{
    internal class SimpleFramerateManager : MonoBehaviour {
        //---------------//
        // CONST MEMBERS //
        //---------------//
        private const int MaxRate = 9999;

        //-----------//
        // INSPECTOR //
        //-----------//
        [Header("Frame Settings")] 
        public float TargetFrameRate = 60.0f;

        private float currentFrameTime;
        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        private void Awake() {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = MaxRate;
            currentFrameTime = Time.realtimeSinceStartup;
            StartCoroutine(nameof(WaitForNextFrame));
            DontDestroyOnLoad(gameObject);
        }
        
        //---------//
        // METHODS //
        //---------//
        private IEnumerator WaitForNextFrame() {
            while (true) {
                yield return new WaitForEndOfFrame();
                currentFrameTime += 1.0f / TargetFrameRate;
                var t = Time.realtimeSinceStartup;
                var sleepTime = currentFrameTime - t - 0.01f;
                if (sleepTime > 0) Thread.Sleep((int)(sleepTime * 1000));
                while (t < currentFrameTime) t = Time.realtimeSinceStartup;
            }
        }
    }
}
