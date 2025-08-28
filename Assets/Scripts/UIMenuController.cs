using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Mirror; // ← nécessaire pour StartHost/StartClient/Stop*

public class UIMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMain;   // ← enfant du Canvas (ex: PanelMain)
    public GameObject panelHost;   // ← PanelHostBox
    public GameObject panelJoin;   // ← PanelJoinBox

    [Header("Host UI")]
    public TMP_InputField inputName;   // ← InputPartyName
    public Button btnStartHost;        // ← BtnStart
    public Button btnBackHost;         // ← BtnBackHost

    [Header("Join UI")]
    public Transform lobbyListContent; // ← PanelJoinBox/ScrollViewLobbies/Viewport/Content
    public GameObject lobbyEntryPrefab;// ← (prefab bleu) Assets/Prefabs/LobbyEntryPrefab
    public Button btnBackJoin;         // ← BtnBackJoin

    [Header("Status")]
    public TMP_Text statusText;        // ← TextStatus

    void Start()
    {
        // Log auto-diagnostic des références
        Debug.Log($"[UI] Check refs: " +
                  $"panelMain={(panelMain ? panelMain.name : "NULL")}, " +
                  $"panelHost={(panelHost ? panelHost.name : "NULL")}, " +
                  $"panelJoin={(panelJoin ? panelJoin.name : "NULL")}, " +
                  $"inputName={(inputName ? inputName.name : "NULL")}, " +
                  $"btnStartHost={(btnStartHost ? btnStartHost.name : "NULL")}, " +
                  $"btnBackHost={(btnBackHost ? btnBackHost.name : "NULL")}, " +
                  $"lobbyListContent={(lobbyListContent ? lobbyListContent.name : "NULL")}, " +
                  $"lobbyEntryPrefab={(lobbyEntryPrefab ? lobbyEntryPrefab.name : "NULL")}, " +
                  $"btnBackJoin={(btnBackJoin ? btnBackJoin.name : "NULL")}, " +
                  $"statusText={(statusText ? statusText.name : "NULL")}");

        ShowPanel(panelMain); // affiche le menu principal au départ

        // Boutons HOST (Steam/Fizzy)
        if (btnStartHost) btnStartHost.onClick.AddListener(() =>
        {
            if (SteamLobbyService.I == null)
            {
                Debug.LogError("[UI] SteamLobbyService manquant dans la scène.");
                if (statusText) statusText.text = "Service lobby manquant.";
                return;
            }
            SteamLobbyService.I.CreateLobby(inputName ? inputName.text : "SansTitre");
        });
        if (btnBackHost) btnBackHost.onClick.AddListener(() => ShowPanel(panelMain));

        // Bouton JOIN (retour)
        if (btnBackJoin) btnBackJoin.onClick.AddListener(() => ShowPanel(panelMain));

        // Souscriptions aux événements du service lobby (Steam)
        if (SteamLobbyService.I != null)
        {
            SteamLobbyService.I.OnLobbyListUpdated += RefreshLobbyList;
            SteamLobbyService.I.OnStatus += s => { if (statusText) statusText.text = s; };
        }
        else
        {
            Debug.LogError("[UI] SteamLobbyService.I est NULL au Start(). Place le composant dans la scène Menu.");
            if (statusText) statusText.text = "Service lobby non trouvé.";
        }
    }

    // Appelé par le bouton HOST (OnClick sur BtnHost) - panneau Steam Host
    public void OnClickHost()
    {
        ShowPanel(panelHost);
    }

    // Appelé par le bouton JOIN (OnClick sur BtnJoin) - panneau Steam Join
    public void OnClickJoin()
    {
        ShowPanel(panelJoin);
        if (SteamLobbyService.I != null)
        {
            SteamLobbyService.I.RefreshLobbies();
        }
        else
        {
            Debug.LogError("[UI] Impossible de rafraîchir: SteamLobbyService absent.");
            if (statusText) statusText.text = "Service lobby non trouvé.";
        }
    }

    // ----- BOUTONS DE TEST LOCAL (KCP/Telepathy) -----

    // Associer ce handler au bouton "Host Local"
    public void OnClickHostLocal()
    {
        if (statusText) statusText.text = "Host local (KCP) en cours...";
        Debug.Log("[UI] Host local (KCP)...");
        NetworkManager.singleton.StartHost(); // Transport doit être KcpTransport (ou Telepathy) dans l’Inspector
    }

    // Associer ce handler au bouton "Join Local"
    public void OnClickJoinLocal()
    {
        NetworkManager.singleton.networkAddress = "localhost"; // ou 127.0.0.1
        if (statusText) statusText.text = "Connexion au host local…";
        Debug.Log("[UI] Join local → localhost");
        NetworkManager.singleton.StartClient();
    }

    // (Option) Bouton Stop pour couper proprement entre 2 essais locaux
    public void OnClickStopLocal()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            Debug.Log("[UI] Stop Host (serveur + client)");
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkServer.active)
        {
            Debug.Log("[UI] Stop Server");
            NetworkManager.singleton.StopServer();
        }
        else if (NetworkClient.isConnected)
        {
            Debug.Log("[UI] Stop Client");
            NetworkManager.singleton.StopClient();
        }
        if (statusText) statusText.text = "Arrêt réseau.";
    }

    // ----- Commun -----

    // Affiche un seul panneau à la fois (sans éteindre tout le Canvas)
    void ShowPanel(GameObject target)
    {
        if (panelMain) panelMain.SetActive(target == panelMain);
        if (panelHost) panelHost.SetActive(target == panelHost);
        if (panelJoin) panelJoin.SetActive(target == panelJoin);
    }

    // Reconstruit la liste des lobbys dans la ScrollView (Steam)
    public void RefreshLobbyList(List<SteamLobbyService.LobbySummary> items)
    {
        // Vérif des refs
        if (lobbyListContent == null)
        {
            Debug.LogError("[UI] lobbyListContent est NULL (glisse PanelJoinBox/ScrollViewLobbies/Viewport/Content dans l’Inspector).");
            if (statusText) statusText.text = "Erreur UI: Content manquant.";
            return;
        }
        if (lobbyEntryPrefab == null)
        {
            Debug.LogError("[UI] lobbyEntryPrefab est NULL (glisse le PREFAB BLEU depuis Assets/Prefabs).");
            if (statusText) statusText.text = "Erreur UI: Prefab manquant.";
            return;
        }

        // Nettoyage
        foreach (Transform child in lobbyListContent)
            Destroy(child.gameObject);

        // Cas vide
        if (items == null || items.Count == 0)
        {
            if (statusText) statusText.text = "0 partie en ligne.";
            Debug.Log("[UI] Liste vide.");
            return;
        }

        // Création des entrées
        foreach (var it in items)
        {
            var go = Instantiate(lobbyEntryPrefab, lobbyListContent);
            if (go == null)
            {
                Debug.LogError("[UI] Instantiate a renvoyé null (prefab cassé ?).");
                continue;
            }

            // Remplir les textes (on prend le 1er TMP_Text trouvé pour le titre)
            var texts = go.GetComponentsInChildren<TMP_Text>(true);
            if (texts == null || texts.Length == 0)
            {
                Debug.LogError("[UI] AUCUN TMP_Text trouvé dans LobbyEntryPrefab. Ajoute au moins un Text (TMP) enfant (ex: txtTitle).");
            }
            else
            {
                texts[0].text = string.IsNullOrWhiteSpace(it.name) ? "(Sans nom)" : it.name;
            }

            // Brancher le clic (Steam Join)
            var btn = go.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError("[UI] Pas de Button sur la racine du prefab (il en faut un).");
            }
            else
            {
                var id = it.id; // capture
                btn.onClick.AddListener(() => SteamLobbyService.I.JoinLobby(id));
            }
        }

        if (statusText) statusText.text = $"{items.Count} partie(s) en ligne.";
        Debug.Log($"[UI] Liste reconstruite: {items.Count} entrées.");
    }
}
