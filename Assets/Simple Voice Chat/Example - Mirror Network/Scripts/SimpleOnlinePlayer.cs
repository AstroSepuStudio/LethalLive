using Mirror;
using UnityEngine;

namespace SimpleVoiceChat.Mirror_Example_3d {

    /// <summary>
    /// This script is placed on Online Player prefab in demo scene.
    /// </summary>
    public class SimpleOnlinePlayer : NetworkBehaviour {

        public static SimpleOnlinePlayer localPlayer;

        [Header("Refs")]
        [SerializeField] private Speaker speaker;
        [SerializeField] private GameObject speakingIcon;
        [SerializeField] private Camera camera;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed;
        [SerializeField] private float rotationSpeed;

        private float speakTime;

        public override void OnStartLocalPlayer() {
            base.OnStartLocalPlayer();
            localPlayer = this;
            Recorder.OnSendDataToNetwork += Cmd_SendVoiceToServer;
            camera.gameObject.SetActive(true);
        }

        void Update() {
            if (localPlayer == this)
                ProcessKeyboardMovement();

            if (Time.time > speakTime)
                speakingIcon.gameObject.SetActive(false);
        }

        void OnDestroy() {
            if (localPlayer != null && localPlayer == this)
                Recorder.OnSendDataToNetwork -= Cmd_SendVoiceToServer;
        }

        /// <summary>
        /// This method transfers voice data from client to server via Mirror Networking.
        /// </summary>
        [Command]
        public void Cmd_SendVoiceToServer(byte[] voiceData) {
            Rpc_SendVoice(voiceData);
        }

        /// <summary>
        /// This method transfer voice data from server to clients.
        /// </summary>
        [ClientRpc(includeOwner = false)]
        private void Rpc_SendVoice(byte[] voiceData) {
            speaker.ProcessVoiceData(voiceData);
            ShowSpeakingIconAboveHead();
        }

        void ShowSpeakingIconAboveHead() {
            speakTime = Time.time + 0.3f;
            speakingIcon.gameObject.SetActive(true);
        }

        #region Movement

        /// <summary>
        /// It is called for local player only.
        /// </summary>
        void ProcessKeyboardMovement() {

            // Movement.
            if (Input.GetKey(KeyCode.W))
                Move(true);
            else if (Input.GetKey(KeyCode.S))
                Move(false);

            // Rotation left/right.
            if (Input.GetKey(KeyCode.A))
                Rotate(true);
            else if (Input.GetKey(KeyCode.D))
                Rotate(false);
        }

        public void Move(bool forward) {
            if (forward)
                transform.Translate(transform.forward * Time.deltaTime * moveSpeed, Space.World);
            else
                transform.Translate(-transform.forward * Time.deltaTime * moveSpeed, Space.World);
        }

        public void Rotate(bool left) {
            if (left)
                transform.Rotate(0f, -Time.deltaTime * rotationSpeed, 0f);
            else
                transform.Rotate(0f, Time.deltaTime * rotationSpeed, 0f);
        }

        #endregion
    }
}