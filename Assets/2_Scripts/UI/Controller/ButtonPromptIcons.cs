using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The icons <see cref="ButtonPrompt"/> shows for a bound control, per device: Xbox-style pads, PlayStation pads and
/// the keyboard. Each entry is keyed by the Input System control name (buttonSouth, start, r, escape...). Lives at
/// Resources/ButtonPromptIcons. A control with no icon falls back to its name on a badge.
/// </summary>
[CreateAssetMenu(fileName = "ButtonPromptIcons", menuName = "Scriptable Objects/Button Prompt Icons")]
public class ButtonPromptIcons : ScriptableObject
{
    [System.Serializable]
    public class Icon
    {
        public string control;
        public Sprite sprite;
    }

    [SerializeField] private List<Icon> xbox = new List<Icon>();
    [SerializeField] private List<Icon> playStation = new List<Icon>();
    [SerializeField] private List<Icon> keyboard = new List<Icon>();

    private static ButtonPromptIcons _instance;
    private static bool _loaded;

    public static Sprite Get(string control, ButtonPrompt.Device device)
    {
        if (!_loaded)
        {
            _instance = Resources.Load<ButtonPromptIcons>("ButtonPromptIcons");
            _loaded = true;
        }
        if (!_instance || string.IsNullOrEmpty(control)) return null;

        var icons = device switch
        {
            ButtonPrompt.Device.PlayStation => _instance.playStation,
            ButtonPrompt.Device.Keyboard => _instance.keyboard,
            _ => _instance.xbox
        };
        foreach (var icon in icons)
        {
            if (icon != null && icon.control == control) return icon.sprite;
        }

        return null;
    }
}
