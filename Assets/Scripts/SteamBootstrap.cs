using UnityEngine;
using Steamworks;

public class SteamBootstrap : MonoBehaviour
{
    private void Awake()
    {
        try
        {
            if (!SteamAPI.Init())
                Debug.LogError("[Steam] SteamAPI.Init() a échoué. Ouvre Steam et relance.");
            else
                Debug.Log($"[Steam] Connecté en tant que {SteamFriends.GetPersonaName()} (AppId 480 test).");
        }
        catch (System.Exception e) { Debug.LogError("[Steam] Exception init: " + e); }
    }

    private void OnEnable() => DontDestroyOnLoad(gameObject);
    private void OnApplicationQuit() { try { SteamAPI.Shutdown(); } catch { } }
    private void Update() { try { SteamAPI.RunCallbacks(); } catch { } }
}
