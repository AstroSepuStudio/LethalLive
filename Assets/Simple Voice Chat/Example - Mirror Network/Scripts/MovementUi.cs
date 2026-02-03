using UnityEngine;

namespace SimpleVoiceChat.Mirror_Example_3d {

    /// <summary>
    /// This is used to control player's movement on mobile device.
    /// </summary>
    public class MovementUi : MonoBehaviour {

        int forward = 0;
        int back = 0;
        int left = 0;
        int right = 0;

        void Update() {
            if (SimpleOnlinePlayer.localPlayer != null) {
                if (forward == 1)
                    SimpleOnlinePlayer.localPlayer.Move(true);
                if (back == 1)
                    SimpleOnlinePlayer.localPlayer.Move(false);
                if (left == 1)
                    SimpleOnlinePlayer.localPlayer.Rotate(true);
                if (right == 1)
                    SimpleOnlinePlayer.localPlayer.Rotate(false);
            }
        }

        // This method is called by some UI element.
        public void Up() {
            forward = 1;
        }

        // This method is called by some UI element.
        public void UpRelease() {
            forward = 0;
        }

        // This method is called by some UI element.
        public void Down() {
            back = 1;
        }

        // This method is called by some UI element.
        public void DownRelease() {
            back = 0;
        }

        // This method is called by some UI element.
        public void Left() {
            left = 1;
        }

        // This method is called by some UI element.
        public void LeftRelease() {
            left = 0;
        }

        // This method is called by some UI element.
        public void Right() {
            right = 1;
        }

        // This method is called by some UI element.
        public void RightRelease() {
            right = 0;
        }
    }
}
