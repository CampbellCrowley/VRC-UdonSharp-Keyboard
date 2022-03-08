using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class KeyboardJoinMessage : UdonSharpBehaviour {
  public Keyboard keyboard;
  public bool playerJoinEnabled = true;
  public string joinMessage = ">>> <color=white>{}</color><color=yellow> joined the world.</color>";
  public bool playerLeaveEnabled = true;
  public string leaveMessage = "<<< <color=white>{}</color><color=yellow> left the world.</color>";
  void Start() {
    if (keyboard == null) keyboard = GetComponent<Keyboard>();
  }

  public override void OnPlayerJoined(VRCPlayerApi player) {
    if (!playerJoinEnabled) return;
    string playerName = keyboard.SanitizeInput(player.displayName);
    string message = joinMessage.Replace("{}", playerName);
    keyboard.AppendLine(message);
  }
  public override void OnPlayerLeft(VRCPlayerApi player) {
    if (!playerLeaveEnabled) return;
    string playerName = keyboard.SanitizeInput(player.displayName);
    string message = leaveMessage.Replace("{}", playerName);
    keyboard.AppendLine(message);
  }
}
