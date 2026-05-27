using System.Collections;
using TMPro;
using UnityEngine;

public class RespawnUI : MonoBehaviour
{
    [SerializeField] private GameObject _respawnPanel;
    [SerializeField] private TMP_Text _timerText;

    private PlayerNetwork _localPlayer;
    private Coroutine _timerCoroutine;

    private void Start()
    {
        StartCoroutine(FindLocalPlayer());
    }

    private IEnumerator FindLocalPlayer()
    {
        while (_localPlayer == null)
        {
            foreach (PlayerNetwork p in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
            {
                if (p.Owner != null && p.Owner.IsLocalClient)
                {
                    _localPlayer = p;
                    _localPlayer.IsAlive.OnChange += OnIsAliveChanged;
                    break;
                }
            }
            yield return null;
        }
    }

    private void OnIsAliveChanged(bool prev, bool next, bool asServer)
    {
        if (next) HidePanel(); else ShowPanel();
    }

    private void ShowPanel()
    {
        _respawnPanel?.SetActive(true);
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(TimerRoutine());
    }

    private void HidePanel()
    {
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        _respawnPanel?.SetActive(false);
    }

    private IEnumerator TimerRoutine()
    {
        float t = 5f;
        while (t > 0f)
        {
            if (_timerText) _timerText.text = $"Respawn: {t:F1}s";
            t -= Time.deltaTime;
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (_localPlayer) _localPlayer.IsAlive.OnChange -= OnIsAliveChanged;
    }
}