using System;
using System.Collections;
using Cinemachine;
using Rewired;
using Rewired.ComponentControls;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Serialization;

namespace ToruToru{
    /// <summary>
    /// 
    /// </summary>
    [RequireComponent(typeof(NetCodeActor))]
    [RequireComponent(typeof(ClientNetworkTransform))]
    [RequireComponent(typeof(NetworkRigidbody))]
    internal class NetCodeActor : NetworkBehaviour, IDestructible {
        //----------------//
        // STATIC MEMBERS //
        //----------------//
        public static NetCodeActor Host { get; set; }
        public static NetCodeActor Client { get; set; }
        public static NetCodeActor Local { get; private set; }
        
        //-----------//
        // INSPECTOR //
        //-----------//
        public ulong ClientId;
        public bool IsAlive = true;
        public bool IsActive { get; private set; }
        public Destructible Destruction{ get; set; }

        [Header("Required Components")] 
        public MeshRenderer Renderer;
        public BoxCollider Collider;
        public Rigidbody Rigidbody;
        public CinemachineTargetGroup Group;
        [SerializeField] 
        protected SimpleBullet BulletPrefab;
        [SerializeField] 
        protected Transform BulletReleasePoint;
        [SerializeField] 
        protected float BulletForce;
        
        [Header("Optional Components")]
        public Animator LineObjectAnimator;
        
        [Header("Controller")]
        public TouchJoystick LeftStick;
        public TouchJoystick RightStick;
        
        [Header("Movement")]
        public Vector3 DashDirection;
        public Vector3 AimDirection;
        [SerializeField] 
        protected float DashForce = 10;

        [Header("Combat")] 
        [SerializeField] 
        protected float CooldownDuration;
        
        protected bool IsCharging;
        protected bool IsAiming;
        protected bool IndicatorStatus;
        protected bool IsOnCooldown;

        private float Timer;
        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        private void Awake() {
            Debug.Log($"[{gameObject.name}] Awake", gameObject);
            // REQUIRED COMPONENTS //
            if (Renderer == null) Renderer = GetComponent<MeshRenderer>();
            if (Collider == null) Collider = GetComponent<BoxCollider>();
            if (Rigidbody == null) Rigidbody = GetComponent<Rigidbody>();
            if (Destruction == null) Destruction = GetComponent<Destructible>();
            // OPTIONAL COMPONENTS //
            if (LineObjectAnimator == null) LineObjectAnimator = GetComponentInChildren<Animator>();
        }
        
        public override void OnNetworkSpawn() {
            if (IsOwner) {
                LeftStick = NetcodeGameManager.Instance.LeftStick;
                RightStick = NetcodeGameManager.Instance.RightStick;
                Local = this;
            }

            if (NetworkManager.IsServer){
                if (IsOwner)
                { Host = this; }
                else
                { Client = this; }
            }
            else{
                if (IsOwner)
                { Client = this; }
                else
                { Host = this; }
            }
            
            Debug.Log($"[{gameObject.name}] OnNetworkSpawn", gameObject);
        }

        private void Start() {
            if (LeftStick != null){
                LeftStick.TouchDownEvent += OnStartCharge;
                LeftStick.ValueChangedEvent += OnCharging;
                LeftStick.TouchUpEvent += OnStopCharge;
            }

            if (RightStick != null){
                RightStick.TouchDownEvent += OnStartAim;
                RightStick.ValueChangedEvent += OnAiming;
                RightStick.TouchUpEvent += OnStopAim;
            }
            
            Debug.Log($"[{gameObject.name}] Start", gameObject);
        }

        private void Update() {
            if (Timer > 0) { Timer -= Time.unscaledDeltaTime; }
            else { IsOnCooldown = false; }
        }

        //------------//
        // SERVER RPC //
        //------------//
        [ServerRpc]
        public void FireBulletServerRpc(){
            FireBulletClientRpc();
            Debug.Log($"[{gameObject.name}] FireBulletServerRpc", gameObject);
        }

        [ServerRpc]
        public void ShowIndicatorServerRpc(){
            ShowIndicatorClientRpc();
            Debug.Log($"[{gameObject.name}] ShowIndicatorServerRpc", gameObject);
        }

        [ServerRpc]
        public void HideIndicatorServerRpc(){
            HideIndicatorClientRpc();
            Debug.Log($"[{gameObject.name}] HideIndicatorServerRpc", gameObject);
        }
        
        //------------//
        // CLIENT RPC //
        //------------//
        [ClientRpc]
        public void FireBulletClientRpc(){
            if (IsOwner) return;
            Fire();
            Debug.Log($"[{gameObject.name}] SpawnBulletServerRpc", gameObject);
        }

        [ClientRpc]
        public void ShowIndicatorClientRpc(){
            if (IsOwner) return;
            ShowIndicator();
            Debug.Log($"[{gameObject.name}] ShowIndicatorClientRpc", gameObject);
        }

        [ClientRpc]
        public void HideIndicatorClientRpc(){
            if (IsOwner) return;
            HideIndicator();
            Debug.Log($"[{gameObject.name}] HideIndicatorClientRpc", gameObject);
        }
        
        //---------//
        // METHODS //
        //---------//
        public void OnStartCharge(){
            if (IsAlive == false) return;
            if (IsAiming) return;
            IsCharging = true;
            RightStick.interactable = false;
        }

        public void OnCharging(Vector2 direction){
            if (IsAiming) return;
            IsCharging = true;
            if (direction.x != 0f && direction.y != 0f) DashDirection = direction;
            if (RightStick.interactable) RightStick.interactable = false;
        }

        public void OnStopCharge(){
            if (IsAiming) return;
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.AddForce(DashDirection * DashForce, ForceMode.Impulse);
            RightStick.interactable = true;
            IsCharging = false;
        }
        
        public void OnStartAim(){
            if (IsAlive == false) return;
            if (IsCharging || IsOnCooldown) return;
            IsAiming = true;
            LeftStick.interactable = false;
            Rigidbody.isKinematic = true;
            ShowIndicatorServerRpc();
            ShowIndicator();
        }

        public void OnAiming(Vector2 direction){
            if (IsAlive == false) return;
            if (IsCharging  || IsOnCooldown) return;
            IsAiming = true;
            if (direction.x != 0f && direction.y != 0f) AimDirection = direction;
            if (LeftStick.interactable) LeftStick.interactable = false;
            if (Rigidbody.isKinematic == false) Rigidbody.isKinematic = true;
            if (IndicatorStatus == false) {
                ShowIndicatorServerRpc();
                ShowIndicator();
            }
            
            Rigidbody.rotation = Quaternion.LookRotation(AimDirection);
        }

        public void OnStopAim(){
            if (IsAlive == false) return;
            if (IsCharging  || IsOnCooldown) return;
            LeftStick.interactable = true;
            transform.rotation = Rigidbody.rotation;
            Rigidbody.isKinematic = false;
            Rigidbody.AddForce(-transform.forward * DashForce, ForceMode.Impulse);
            IsAiming = false;
            IsOnCooldown = true;
            HideIndicatorServerRpc();
            HideIndicator();
            FireBulletServerRpc();
            Fire();
        }
        
        public void ShowIndicator(){
            if (IsAiming == false) return;
            LineObjectAnimator.Play("Show");
            IndicatorStatus = true;
        }
        
        public void HideIndicator(){
            if (IsAiming) return;
            LineObjectAnimator.Play("Hidden");
            IndicatorStatus = false;
        }

        public void Fire(){
            var bullet = Instantiate(BulletPrefab, BulletReleasePoint.position, transform.rotation);
            bullet.Init(this, BulletForce * transform.forward);
            Timer = CooldownDuration;
        }

        public void SetActive(bool value){
            switch (value){
                case true:
                    IsActive = true;
                    gameObject.SetActive(true);
                    break;
                case false:
                    IsActive = false;
                    gameObject.SetActive(false);
                    break;
            }
        }

        public void SetBulletOwner(SimpleBullet bullet) {
            bullet.Owner = this;
            Invoke(nameof(DestroyParticle), 5f);
            return;
            void DestroyParticle() => Destroy(bullet);
        }
    }
}