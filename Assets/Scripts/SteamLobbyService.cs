using UnityEngine;
using Steamworks;
using Mirror;
using System.Collections.Generic;

public class SteamLobbyService : MonoBehaviour
{
    public static SteamLobbyService I;

    protected Callback<LobbyCreated_t> cbLobbyCreated;
    protected Callback<LobbyMatchList_t> cbLobbyList;
    protected Callback<GameLobbyJoinRequested_t> cbJoinRequested;
    protected Callback<LobbyEnter_t> cbLobbyEntered;

    private CSteamID currentLobby;
    private string currentName;
    private string currentPin;

    public System.Action<List<LobbySummary>> OnLobbyListUpdated;
    public System.Action<string> OnStatus;

    void Awake()
    {
        I = this;
        cbLobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        cbLobbyList = Callback<LobbyMatchList_t>.Create(OnLobbyList);
        cbJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        cbLobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    // PIN maintenant optionnel
    public void CreateLobby(string lobbyName, string pin = "")
    {
        currentName = lobbyName;
        currentPin = string.IsNullOrWhiteSpace(pin) ? "" : pin;

        OnStatus?.Invoke("Création du lobby Steam…");
        Debug.Log($"[Lobby] CreateLobby name='{currentName}' pin='{currentPin}'");

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 8); // 8 slots
    }

    public void RefreshLobbies()
    {
        OnStatus?.Invoke("Recherche des parties en ligne…");
        // Filtre par notre jeu
        SteamMatchmaking.AddRequestLobbyListStringFilter("game", "Connected", ELobbyComparison.k_ELobbyComparisonEqual);
        // (optionnel) portée mondiale
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        SteamMatchmaking.RequestLobbyList();
    }

    public void JoinLobby(CSteamID lobbyID, string pinIfAsked = "")
    {
        string hasPin = SteamMatchmaking.GetLobbyData(lobbyID, "hasPin");
        if (hasPin == "1")
        {
            string required = SteamMatchmaking.GetLobbyData(lobbyID, "pin");
            if (required != pinIfAsked)
            {
                OnStatus?.Invoke("PIN incorrect.");
                Debug.LogWarning("[Lobby] PIN erroné.");
                return;
            }
        }
        OnStatus?.Invoke("Connexion au lobby…");
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    void OnLobbyCreated(LobbyCreated_t data)
    {
        if (data.m_eResult != EResult.k_EResultOK)
        {
            OnStatus?.Invoke("Échec création lobby.");
            Debug.LogError("[Lobby] Create failed: " + data.m_eResult);
            return;
        }

        currentLobby = new CSteamID(data.m_ulSteamIDLobby);
        Debug.Log("[Lobby] Créé: " + currentLobby);

        SteamMatchmaking.SetLobbyData(currentLobby, "name", string.IsNullOrEmpty(currentName) ? "SansTitre" : currentName);
        SteamMatchmaking.SetLobbyData(currentLobby, "game", "Connected");
        SteamMatchmaking.SetLobbyData(currentLobby, "hasPin", string.IsNullOrEmpty(currentPin) ? "0" : "1");
        SteamMatchmaking.SetLobbyData(currentLobby, "pin", string.IsNullOrEmpty(currentPin) ? "" : currentPin);
        SteamMatchmaking.SetLobbyJoinable(currentLobby, true);

        NetworkManager.singleton.StartHost();
        OnStatus?.Invoke("Host démarré, chargement de la scène…");
    }

    void OnLobbyEntered(LobbyEnter_t data)
    {
        CSteamID lobbyID = new CSteamID(data.m_ulSteamIDLobby);
        Debug.Log("[Lobby] Entré dans: " + lobbyID);

        // Si on n'est pas le propriétaire, on est client → pointer le SteamID de l'hôte
        if (!SteamMatchmaking.GetLobbyOwner(lobbyID).Equals(SteamUser.GetSteamID()))
        {
            var owner = SteamMatchmaking.GetLobbyOwner(lobbyID);
            // FizzySteamworks lit networkAddress comme SteamID64 cible
            NetworkManager.singleton.networkAddress = owner.m_SteamID.ToString();
            Debug.Log("[Lobby] Cible client (steamID64) = " + NetworkManager.singleton.networkAddress);

            OnStatus?.Invoke("Connexion au host…");
            NetworkManager.singleton.StartClient();
        }
    }

    void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t data)
    {
        Debug.Log("[Lobby] Join request reçu.");
        SteamMatchmaking.JoinLobby(data.m_steamIDLobby);
    }

    void OnLobbyList(LobbyMatchList_t data)
    {
        int count = (int)data.m_nLobbiesMatching;
        var list = new List<LobbySummary>(count);
        for (int i = 0; i < count; i++)
        {
            CSteamID id = SteamMatchmaking.GetLobbyByIndex(i);
            string name = SteamMatchmaking.GetLobbyData(id, "name");
            string hasPin = SteamMatchmaking.GetLobbyData(id, "hasPin");
            list.Add(new LobbySummary
            {
                id = id,
                name = string.IsNullOrEmpty(name) ? "(Sans nom)" : name,
                requiresPin = hasPin == "1"
            });
            Debug.Log($"[Lobby] Vu: {id} '{name}' pin={hasPin}");
        }
        Debug.Log($"[Lobby] {list.Count} lobby(s) trouvé(s).");
        OnLobbyListUpdated?.Invoke(list);
        OnStatus?.Invoke($"{list.Count} partie(s) en ligne.");
    }

    public struct LobbySummary
    {
        public CSteamID id;
        public string name;
        public bool requiresPin;
    }
}
