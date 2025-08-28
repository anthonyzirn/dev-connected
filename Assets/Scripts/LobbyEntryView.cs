using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LobbyEntryView : MonoBehaviour
{
    public TMP_Text txtTitle;
    public TMP_Text txtPin;
    public Button btn;

    public void Bind(string name, bool requiresPin, System.Action onClick)
    {
        txtTitle.text = name;
        txtPin.text = requiresPin ? "🔒 PIN" : "Ouvert";
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }
}
