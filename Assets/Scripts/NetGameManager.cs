using Mirror;
using UnityEngine;

public class NetGameManager : NetworkManager
{
    [Header("Connected - spawn")]
    public Transform spawnPoint;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[Net] Serveur démarré (host).");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("[Net] Serveur arrêté.");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[Net] Client CONNECTÉ au host.");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[Net] Client DÉCONNECTÉ.");
    }

    public override void OnClientError(TransportError error, string reason)
    {
        Debug.LogError($"[Net] Client ERROR: {error} - {reason}");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform start = spawnPoint != null ? spawnPoint : transform;
        GameObject player = Instantiate(playerPrefab, start.position, start.rotation);
        NetworkServer.AddPlayerForConnection(conn, player);
        Debug.Log($"[Net] Joueur ajouté pour connexion {conn.connectionId}.");
    }
}
