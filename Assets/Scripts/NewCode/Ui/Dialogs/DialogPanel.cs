// DialogPanel.cs
using UnityEngine;
using UnityEngine.UI;
using System;
#if TMP_PRESENT
using TMPro;
using TextType = TMPro.TMP_Text;   // если используешь TMP, поставь символ TMP_PRESENT в Player Settings → Scripting Define Symbols
#else
using TextType = UnityEngine.UI.Text;
#endif

public class DialogPanel : MonoBehaviour
{
    [Header("Core")]
    public CanvasGroup group;                 // CanvasGroup на корне "DialogPanel"
    public TextType titleText;                // твой заголовок (слева сверху)
    public TextType bodyText;                 // ВАЖНО: текст внутри ScrollView/Viewport/Content/Text
    public ScrollRect scroll;                 // сам ScrollView (чтобы сбрасывать позицию к началу)

    [Header("Buttons")]
    public Button primaryButton;              // нижняя правая (например, "Принять"/"Ещё")
    public Button secondaryButton;            // нижняя левая ("Закрыть"/"Отмена")

    [Header("Choices (optional)")]
    public RectTransform choicesContainer;    // контейнер для вариантов (вертикальный LayoutGroup)
    public Button choiceButtonPrefab;         // префаб кнопки варианта (Text/TMP_Text внутри)

    void Awake() { Show(false); }

    public void Show(bool v)
    {
        if (!group) return;
        group.alpha = v ? 1 : 0;
        group.interactable = v;
        group.blocksRaycasts = v;
        if (v && scroll) { scroll.verticalNormalizedPosition = 1f; }
    }

    public TextType subtitleText; // Профессия (опционально)

    public void SetBody(
        string title, string body,
        string primaryLabel, Action onPrimary,
        string secondaryLabel, Action onSecondary,
        string subtitle = null) // <-- новый опциональный параметр
    {
        if (titleText) titleText.text = title ?? "";
        if (subtitleText)
        {
            subtitleText.text = subtitle ?? "";
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        }
        if (bodyText) bodyText.text = body ?? "";
        if (choicesContainer) choicesContainer.gameObject.SetActive(false);
        SetupButton(primaryButton, primaryLabel, onPrimary);
        SetupButton(secondaryButton, secondaryLabel, onSecondary);
    }

    public void SetChoices(string title, string body, (string label, Action onClick)[] choices,
                           string secondaryLabel = "Закрыть", Action onSecondary = null)
    {
        if (titleText) titleText.text = title ?? "";
        if (bodyText) bodyText.text = body ?? "";

        // показываем список вариантов
        if (choicesContainer)
        {
            choicesContainer.gameObject.SetActive(true);
            // очистить старые дети
            for (int i = choicesContainer.childCount - 1; i >= 0; i--)
                Destroy(choicesContainer.GetChild(i).gameObject);

            if (choiceButtonPrefab != null && choices != null)
            {
                foreach (var ch in choices)
                {
                    var btn = Instantiate(choiceButtonPrefab, choicesContainer);
#if TMP_PRESENT
                    var t = btn.GetComponentInChildren<TMP_Text>();
#else
                    var t = btn.GetComponentInChildren<Text>();
#endif
                    if (t) t.text = ch.label ?? "…";
                    btn.onClick.AddListener(() => { ch.onClick?.Invoke(); });
                }
            }
        }

        // основная кнопка не нужна — варианты её заменяют
        SetupButton(primaryButton, null, null);
        SetupButton(secondaryButton, secondaryLabel, onSecondary);
    }

    void SetupButton(Button b, string label, Action onClick)
    {
        if (!b) return;
        bool show = !string.IsNullOrEmpty(label);
        b.gameObject.SetActive(show);
        b.onClick.RemoveAllListeners();
        if (!show) return;

#if TMP_PRESENT
        var t = b.GetComponentInChildren<TMP_Text>();
#else
        var t = b.GetComponentInChildren<Text>();
#endif
        if (t) t.text = label;
        b.onClick.AddListener(() => onClick?.Invoke());
    }
}