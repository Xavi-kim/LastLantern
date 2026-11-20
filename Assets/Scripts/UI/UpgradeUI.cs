using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastLantern
{
    /// <summary>Экран выбора улучшения: 3 карточки. Выбор отправляется событием UpgradePicked.</summary>
    public class UpgradeUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button[] cardButtons = new Button[0];
        [SerializeField] TMP_Text[] cardTitles = new TMP_Text[0];
        [SerializeField] TMP_Text[] cardDescriptions = new TMP_Text[0];

        readonly List<UpgradeData> shown = new List<UpgradeData>();

        void Awake()
        {
            if (panel) panel.SetActive(false);
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i; // копия для замыкания
                if (cardButtons[i]) cardButtons[i].onClick.AddListener(() => Pick(index));
            }
        }

        void OnEnable() => GameEvents.UpgradeChoicesOffered += Show;
        void OnDisable() => GameEvents.UpgradeChoicesOffered -= Show;

        void Show(IReadOnlyList<UpgradeData> choices)
        {
            shown.Clear();
            for (int i = 0; i < choices.Count; i++) shown.Add(choices[i]);

            for (int i = 0; i < cardButtons.Length; i++)
            {
                bool visible = i < shown.Count;
                if (cardButtons[i]) cardButtons[i].gameObject.SetActive(visible);
                if (!visible) continue;
                if (i < cardTitles.Length && cardTitles[i]) cardTitles[i].text = shown[i].title;
                if (i < cardDescriptions.Length && cardDescriptions[i]) cardDescriptions[i].text = shown[i].description;
            }

            if (panel) panel.SetActive(true);

            // Для геймпада сразу выделяем первую карточку
            if (EventSystem.current && cardButtons.Length > 0 && cardButtons[0])
                EventSystem.current.SetSelectedGameObject(cardButtons[0].gameObject);
        }

        void Pick(int index)
        {
            if (index < 0 || index >= shown.Count) return;
            if (panel) panel.SetActive(false);
            GameEvents.RaiseUpgradePicked(shown[index]);
        }
    }
}
