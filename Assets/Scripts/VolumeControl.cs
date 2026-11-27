using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour
{
    [SerializeField] Button iconButton;
    [SerializeField] Image icon;
    [SerializeField] Sprite highSprite;
    [SerializeField] Sprite lowSprite;
    [SerializeField] Sprite muteSprite;
    [SerializeField] Slider slider;
    [SerializeField, Range(0f, 1f)] float defaultVolume = 1f;
    [SerializeField, Range(0f, 1f)] float minRestoreVolume = 0.25f;
    [SerializeField] float fadeTime = 0.15f;

    const string VolumeKey = "RivetRush.Volume";
    const string LastVolumeKey = "RivetRush.LastVolume";

    float lastVolume;
    bool open;
    CanvasGroup sliderGroup;
    Camera cam;

    void Awake()
    {
        cam = GetComponentInParent<Canvas>().worldCamera;

        sliderGroup = slider.GetComponent<CanvasGroup>();
        if (sliderGroup == null) sliderGroup = slider.gameObject.AddComponent<CanvasGroup>();
        sliderGroup.alpha = 0f;
        sliderGroup.blocksRaycasts = false;
        slider.gameObject.SetActive(false);

        float volume = PlayerPrefs.GetFloat(VolumeKey, defaultVolume);
        lastVolume = PlayerPrefs.GetFloat(LastVolumeKey, volume > 0f ? volume : defaultVolume);

        slider.SetValueWithoutNotify(volume);
        Apply(volume);

        slider.onValueChanged.AddListener(OnSlider);
        iconButton.onClick.AddListener(OnIcon);
    }

    void Update()
    {
        Fade();
        if (!open) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Hide();
            return;
        }

        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        if (!click && !touch) return;

        Vector2 pos = click ? Mouse.current.position.ReadValue() : Touchscreen.current.primaryTouch.position.ReadValue();
        if (!Inside(slider.transform, pos) && !Inside(iconButton.transform, pos)) Hide();
    }

    void Fade()
    {
        float target = open ? 1f : 0f;
        if (Mathf.Approximately(sliderGroup.alpha, target)) return;

        sliderGroup.alpha = Mathf.MoveTowards(sliderGroup.alpha, target, Time.unscaledDeltaTime / fadeTime);
        if (!open && sliderGroup.alpha <= 0f) slider.gameObject.SetActive(false);
    }

    bool Inside(Transform t, Vector2 screenPos) =>
        RectTransformUtility.RectangleContainsScreenPoint((RectTransform)t, screenPos, cam);

    void OnIcon()
    {
        Deselect();

        if (!open)
        {
            Show();
            return;
        }

        if (AudioListener.volume > 0f)
        {
            lastVolume = AudioListener.volume;
            SetVolume(0f);
        }
        else
        {
            SetVolume(Mathf.Max(lastVolume, minRestoreVolume));
        }
    }

    void OnSlider(float volume)
    {
        if (volume > 0f) lastVolume = volume;
        Apply(volume);
    }

    void SetVolume(float volume)
    {
        slider.SetValueWithoutNotify(volume);
        Apply(volume);
    }

    void Apply(float volume)
    {
        AudioListener.volume = volume;
        icon.sprite = volume <= 0f ? muteSprite : volume <= 0.5f ? lowSprite : highSprite;
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.SetFloat(LastVolumeKey, lastVolume);
    }

    void Show()
    {
        open = true;
        slider.gameObject.SetActive(true);
        sliderGroup.blocksRaycasts = true;
    }

    void Hide()
    {
        open = false;
        sliderGroup.blocksRaycasts = false;
        Deselect();
        PlayerPrefs.Save();
    }

    static void Deselect()
    {
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }
}