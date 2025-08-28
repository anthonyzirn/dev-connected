using Mirror;
using UnityEngine;
using Cinemachine;

public class PlayerLocalOnly : NetworkBehaviour
{
    [Header("Local Player Components")]
    public Camera playerCamera;
    public AudioListener playerListener;

    [Header("Input and Control Scripts")]
    public Behaviour[] inputAndControlScripts;

    [Header("Local Only Objects")]
    public GameObject[] localOnlyObjects;

    void Awake()
    {
        // Auto-trouve les composants si pas assignés
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerListener == null)
            playerListener = GetComponentInChildren<AudioListener>();

        // IMPORTANT: Désactiver les composants par défaut
        // Ils seront activés uniquement pour le joueur local
        if (playerListener != null)
            playerListener.enabled = false;

        if (playerCamera != null)
            playerCamera.enabled = false;
    

        // Auto-trouve les scripts de contrôle si pas assignés
        if (inputAndControlScripts == null || inputAndControlScripts.Length == 0)
        {
            var controls = GetComponentsInChildren<MonoBehaviour>(true);
            var controlList = new System.Collections.Generic.List<Behaviour>();

            foreach (var control in controls)
            {
                if (control == this) continue;

                string typeName = control.GetType().Name;
                if (typeName.Contains("Controller") || typeName.Contains("Input") || typeName.Contains("Movement"))
                {
                    controlList.Add(control);
                }
            }
            inputAndControlScripts = controlList.ToArray();
        }

        // CRITIQUE: Désactiver AudioListener par défaut
        // Il sera réactivé seulement pour le joueur local
        if (playerListener != null)
        {
            playerListener.enabled = false;
            Debug.Log($"[Player] AudioListener désactivé sur {playerListener.gameObject.name}");
        }
    }

    public override void OnStartLocalPlayer()
    {
        SetLocal(true);
        Debug.Log($"[Player] LOCAL: controls={inputAndControlScripts?.Length ?? 0}, objects={localOnlyObjects?.Length ?? 0}");
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
        {
            SetLocal(false);
            Debug.Log($"[Player] REMOTE: controls={inputAndControlScripts?.Length ?? 0}, objects={localOnlyObjects?.Length ?? 0}");
        }
    }

    void SetLocal(bool on)
    {
        // Gestion des AudioListeners - MODIFIÉ POUR PREFAB
        if (playerListener != null)
        {
            // SEUL le joueur local a son AudioListener activé
            playerListener.enabled = (on && isLocalPlayer);
            Debug.Log($"[Player] AudioListener sur {playerListener.gameObject.name} enabled = {playerListener.enabled}");
        }

        // Gestion de la caméra
        if (playerCamera != null)
        {
            // SEUL le joueur local a sa caméra activée
            playerCamera.enabled = (on && isLocalPlayer);
            Debug.Log($"[Player] Camera sur {playerCamera.gameObject.name} enabled = {playerCamera.enabled}");
        }

        // Scripts de contrôle
        if (inputAndControlScripts != null)
        {
            foreach (var script in inputAndControlScripts)
            {
                if (script)
                {
                    script.enabled = on;
                    Debug.Log($"[Player] Script {script.GetType().Name} enabled = {on}");
                }
            }
        }

        // Objets locaux
        if (localOnlyObjects != null)
        {
            foreach (var obj in localOnlyObjects)
            {
                if (obj)
                {
                    obj.SetActive(on);
                    Debug.Log($"[Player] Object {obj.name} active = {on}");
                }
            }
        }
    }
}
