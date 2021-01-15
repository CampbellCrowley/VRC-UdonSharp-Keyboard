using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using System.Text.RegularExpressions;

public class Keyboard : UdonSharpBehaviour {
  public InputField inputField;
  public Text chatLog;
  public Toggle lShift;
  public Toggle rShift;
  public Toggle capsLock;
  [Range(1, 1000)] public int maxLength = 300;
  [Range(1, 1000)] public int maxLines = 14;

  public Color pressedColor = Color.blue;
  public Color normalColor = Color.white;

  private string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()-_=+[]{}\\|;:'\",<.>/? \t";

  [UdonSynced] public string delivery = "0@";
  private string lastDelivery = "0@";
  private int lastSeq = 0;

  private string queuedMessage = "";
  private int queuedSeq = 0;

  private VRCPlayerApi localPlayer;
  private string prefix = "@Unknown: ";

  // Use Upper-case.
  private bool shift = false;

  void Start() {
    lastDelivery = delivery;
    lastSeq = ParseSequenceNumber(delivery);

    inputField.characterLimit = maxLength;
    inputField.DeactivateInputField();

    localPlayer = Networking.LocalPlayer;
    if (localPlayer != null) prefix = "@" + localPlayer.displayName + ": ";
  }

  // Check for enter key.
  void Update() {
    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) SendMessage();
  }

  // User pressed key.
  public void AppendChar(char c) {
    inputField.text += c;
    lShift.isOn = false;
    rShift.isOn = false;
  }
  // Add line to chat log.
  public void AppendLine(string msg) {
    chatLog.text += "\n" + msg;

    string[] split = chatLog.text.Split('\n');
    int splitLen = split.Length;
    if (splitLen > maxLines) {
      chatLog.text = System.String.Join("\n", split, splitLen - maxLines, maxLines);
    }
  }
  // Delete last character. Caret doesn't seem to work, so we fall back to removing last character.
  public void Backspace() {
    if (inputField.caretPosition > 0) {
      int width = inputField.caretWidth;
      if (width == 0) width = 1;
      // Debug.Log("Backspacing: " + width + ", ending: " + inputField.caretPosition); 
      inputField.text = inputField.text.Remove(inputField.caretPosition - width, width);
    } else if (inputField.text.Length > 0) {
      // Debug.Log("Backspacing last");
      inputField.text = inputField.text.Remove(inputField.text.Length - 1);
    }
  }
  // Live sanitize input text. (CustomEvent)
  public void InputUpdated() {
    if (inputField.text.Length > maxLength) {
      inputField.text = inputField.text.Substring(0, maxLength);
    }
  }
  // Shift or capslock was changed.
  public void ShiftUpdated() {
    bool shifting = lShift.isOn || rShift.isOn;
    shift = shifting != capsLock.isOn;

    SetPressed(lShift, shift);
    SetPressed(rShift, shift);
    SetPressed(capsLock, capsLock.isOn);
  }
  private void SetPressed(Toggle t, bool set) {
    ColorBlock cb = t.colors;
    cb.normalColor = set ? pressedColor : normalColor;
    t.colors = cb;
  }
  // Check for commands typed. False means no command was detected.
  private bool ProcessCommands(string msg) {
    switch (msg) {
      case "/clear":
        chatLog.text = "<i><color=white>Cleared Log</color></i>";
        return true;
      default:
        return false;
    }
  }
  // Get sequence number of delivered message.
  private int ParseSequenceNumber(string input) {
    return System.Convert.ToInt32(input.Substring(0, input.IndexOf('@')));
  }
  private string ParseMessage(string input) {
    return input.Substring(input.IndexOf('@')); // Keeps @
  }
  // Sanitize user-input of malicious intent.
  private string SanitizeInput(string input) {
    input = input.Trim();
    for (int i = 0; i < input.Length; i++) {
      if (allowedChars.IndexOf(input[i]) < 0) {
        input = input.Remove(i, 1);
        i--;
      }
    }
    if (input.Length > maxLength) {
      input = input.Substring(0, maxLength);
    }
    return input.Replace("<", "<\u200B").Replace(">", "\u200B>");
  }
  // Send the message currently in the text input box.
  public void SendMessage() {
    // Debug.Log("Sending Message: " + inputField.text);
    if (inputField.text.Length == 0) return;
    if (ProcessCommands(inputField.text)) {
      inputField.text = "";
      return;
    }

    int seq = ParseSequenceNumber(delivery) + 1;
    // int seq = lastSeq + 1;

    if (localPlayer != null) Networking.SetOwner(localPlayer, gameObject);

    string sanitized = SanitizeInput(inputField.text);
    if (sanitized.Length == 0) return;
    queuedMessage = prefix + sanitized;
    delivery = seq + queuedMessage;
    queuedSeq = seq;

    inputField.text = "";

    OnDeserialization();
  }
  // Receive a new message.
  public override void OnDeserialization() {
    // Debug.Log("OnDeserialization: " + delivery);
    if (delivery == lastDelivery) return;
    lastDelivery = delivery;

    int seq = ParseSequenceNumber(delivery);
    if (seq < lastSeq) return;
    // if (seq != lastSeq + 1) {
    //   AppendLine("<i><color=white>Desynced...</color></i>");
    // }
    lastSeq = seq;

    string newChatline = ParseMessage(delivery);
    AppendLine(newChatline);

    // Resend our message because we were too late.
    /* if (seq == queuedSeq && queuedMessage != newChatline) {
      string tmp = inputField.text;
      inputField.text = queuedMessage;
      SendMessage();
      inputField.text = tmp;
    } */
  }

  // Key Press Handlers.
  public void Tilde() { AppendChar(shift ? '~' : '`'); }
  public void One() { AppendChar(shift ? '!' : '1'); }
  public void Two() { AppendChar(shift ? '@' : '2'); }
  public void Three() { AppendChar(shift ? '#' : '3'); }
  public void Four() { AppendChar(shift ? '$' : '4'); }
  public void Five() { AppendChar(shift ? '%' : '5'); }
  public void Six() { AppendChar(shift ? '^' : '6'); }
  public void Seven() { AppendChar(shift ? '&' : '7'); }
  public void Eight() { AppendChar(shift ? '*' : '8'); }
  public void Nine() { AppendChar(shift ? '(' : '9'); }
  public void Zero() { AppendChar(shift ? ')' : '0'); }
  public void Minus() { AppendChar(shift ? '_' : '-'); }
  public void Equals() { AppendChar(shift ? '+' : '='); }
  public void Tab() { AppendChar('\t'); }
  public void Q() { AppendChar(shift ? 'Q' : 'q'); }
  public void W() { AppendChar(shift ? 'W' : 'w'); }
  public void E() { AppendChar(shift ? 'E' : 'e'); }
  public void R() { AppendChar(shift ? 'R' : 'r'); }
  public void T() { AppendChar(shift ? 'T' : 't'); }
  public void Y() { AppendChar(shift ? 'Y' : 'y'); }
  public void U() { AppendChar(shift ? 'U' : 'u'); }
  public void I() { AppendChar(shift ? 'I' : 'i'); }
  public void O() { AppendChar(shift ? 'O' : 'o'); }
  public void P() { AppendChar(shift ? 'P' : 'p'); }
  public void LBracket() { AppendChar(shift ? '{' : '['); }
  public void RBracket() { AppendChar(shift ? '}' : ']'); }
  public void BackSlash() { AppendChar(shift ? '|' : '\\'); }
  public void A() { AppendChar(shift ? 'A' : 'a'); }
  public void S() { AppendChar(shift ? 'S' : 's'); }
  public void D() { AppendChar(shift ? 'D' : 'd'); }
  public void F() { AppendChar(shift ? 'F' : 'f'); }
  public void G() { AppendChar(shift ? 'G' : 'g'); }
  public void H() { AppendChar(shift ? 'H' : 'h'); }
  public void J() { AppendChar(shift ? 'J' : 'j'); }
  public void K() { AppendChar(shift ? 'K' : 'k'); }
  public void L() { AppendChar(shift ? 'L' : 'l'); }
  public void Semicolon() { AppendChar(shift ? ':' : ';'); }
  public void Quote() { AppendChar(shift ? '"' : '\''); }
  public void Z() { AppendChar(shift ? 'Z' : 'z'); }
  public void X() { AppendChar(shift ? 'X' : 'x'); }
  public void C() { AppendChar(shift ? 'C' : 'c'); }
  public void V() { AppendChar(shift ? 'V' : 'v'); }
  public void B() { AppendChar(shift ? 'B' : 'b'); }
  public void N() { AppendChar(shift ? 'N' : 'n'); }
  public void M() { AppendChar(shift ? 'M' : 'm'); }
  public void Comma() { AppendChar(shift ? '<' : ','); }
  public void Period() { AppendChar(shift ? '>' : '.'); }
  public void Slash() { AppendChar(shift ? '?' : '/'); }
  public void Space() { AppendChar(' '); }
}
