using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI goldLossText;
    [SerializeField] Button respawnVillageButton;
    [SerializeField] Button respawnHereButton;
    [SerializeField] CanvasGroup canvasGroup;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        EventBus.On<PlayerDeathEvent>(OnDeath);

        if (respawnVillageButton != null)
            respawnVillageButton.onClick.AddListener(() => Respawn(true));
        if (respawnHereButton != null)
            respawnHereButton.onClick.AddListener(() => Respawn(false));
    }

    void OnDeath(PlayerDeathEvent e)
    {
        if (panel == null) return;
        panel.SetActive(true);

        var gm = GameManager.Instance;
        if (gm != null)
        {
            int loss = Mathf.FloorToInt(gm.PlayerState.Gold * GameConfig.DeathGoldPenalty);
            if (goldLossText != null) goldLossText.text = $"Gold lost: {loss}";
            gm.GetComponentInChildren<PlayerController>()?.SetSpeed(0);
        }

        if (canvasGroup != null) StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 2f)
        {
            canvasGroup.alpha = t;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    void Respawn(bool atVillage)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (atVillage)
        {
            var player = gm.GetComponentInChildren<PlayerController>();
            if (player != null)
                player.transform.position = Vector3.zero; // Village center
        }

        gm.PlayerState.FullHeal();
        var pc = gm.GetComponentInChildren<PlayerController>();
        if (pc != null) pc.SetSpeed(gm.PlayerState.CurrentStats.spd);

        if (panel != null) panel.SetActive(false);
    }
}
