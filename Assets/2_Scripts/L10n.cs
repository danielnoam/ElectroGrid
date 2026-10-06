using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;

/// <summary>
/// Looks up player-facing text in the UI string table. Placeholders are named, so a translation can put
/// {amount} wherever its grammar needs it: L10n.Get("objective.pieces.description", ("amount", 5)).
/// </summary>
public static class L10n
{
    public const string Table = "UI";

    // Outside Play mode nothing selects a locale, so editor tools like the level inspector read the project locale.
    // Null in Play mode and builds means the selected locale.
    private static Locale Locale => Application.isPlaying ? null : LocalizationSettings.ProjectLocale;

    public static string Get(string key)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(Table, key, Locale);
    }

    /// <summary>Resolves a LocalizedString field on an asset, with the same editor fallback as the key lookups.</summary>
    public static string Get(LocalizedString text)
    {
        if (text == null || text.IsEmpty) return string.Empty;
        return LocalizationSettings.StringDatabase.GetLocalizedString(text.TableReference, text.TableEntryReference, Locale);
    }

    public static string Get(string key, params (string name, object value)[] arguments)
    {
        var values = new Dictionary<string, object>(arguments.Length);
        foreach (var (name, value) in arguments) values[name] = value;
        return LocalizationSettings.StringDatabase.GetLocalizedString(Table, key, new object[] { values }, Locale);
    }

    /// <summary>Whether the selected language reads right to left (Hebrew, Arabic). False outside Play mode.</summary>
    public static bool IsRightToLeft
    {
        get
        {
            if (!Application.isPlaying) return false;
            var culture = LocalizationSettings.SelectedLocale ? LocalizationSettings.SelectedLocale.Identifier.CultureInfo : null;
            return culture != null && culture.TextInfo.IsRightToLeft;
        }
    }

    /// <summary>Raised after the player picks another language. Screens that build text in code re-read it here.</summary>
    public static event System.Action LanguageChanged;

    /// <summary>Switches to the next available language and remembers it in the save.</summary>
    public static void CycleLanguage()
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count < 2) return;

        int next = (locales.IndexOf(LocalizationSettings.SelectedLocale) + 1) % locales.Count;
        LocalizationSettings.SelectedLocale = locales[next];

        // The pseudo-locale shares English's code, so it is never saved; a relaunch comes back in English
        if (SaveManager.Instance && locales[next] is not PseudoLocale) SaveManager.Instance.Settings.language = locales[next].Identifier.Code;
    }

    /// <summary>The language's own name for itself, "Deutsch" rather than "German", so a player can find it in any language.</summary>
    public static string LanguageName(Locale locale)
    {
        if (!locale) return string.Empty;
        if (locale is PseudoLocale) return "Pseudo";
        var culture = locale.Identifier.CultureInfo;
        // "Português" rather than "Português (Brasil)", which does not fit the button; revisit if two variants of one language ship
        if (culture != null && !culture.IsNeutralCulture && culture.Parent != null && !string.IsNullOrEmpty(culture.Parent.Name)) culture = culture.Parent;
        return culture != null ? culture.TextInfo.ToTitleCase(culture.NativeName) : locale.LocaleName;
    }

    // Domain reload is off in this project, so statics survive between Play sessions and are reset by hand
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        LanguageChanged = null;
        _hebrewFont = null;
        _arabicFont = null;
        RTLTMPro.RTLTextMeshPro.RtlFontSelector = SelectRtlFont;
    }

    // The game's display fonts have no Hebrew or Arabic, so right-to-left labels switch to a full font for their script
    private static TMPro.TMP_FontAsset _hebrewFont;
    private static TMPro.TMP_FontAsset _arabicFont;

    private static TMPro.TMP_FontAsset SelectRtlFont(string text)
    {
        foreach (char c in text)
        {
            if (RTLTMPro.TextUtils.IsArabicCharacter(c)) return _arabicFont ? _arabicFont : _arabicFont = Resources.Load<TMPro.TMP_FontAsset>("NotoSansArabic-Regular SDF");
            if (RTLTMPro.TextUtils.IsHebrewCharacter(c)) return _hebrewFont ? _hebrewFont : _hebrewFont = Resources.Load<TMPro.TMP_FontAsset>("NotoSansHebrew-Regular SDF");
        }
        return null;
    }

    private static void OnSelectedLocaleChanged(Locale locale) => LanguageChanged?.Invoke();

    // The saved language is applied once Localization has picked its startup locale, which otherwise follows the system
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedLanguage()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        LocalizationSettings.InitializationOperation.Completed += _ =>
        {
            // Pseudo-localization is a development tool, players never see it in the language list
            if (!Debug.isDebugBuild)
            {
                var locales = LocalizationSettings.AvailableLocales.Locales;
                for (int i = locales.Count - 1; i >= 0; i--)
                    if (locales[i] is PseudoLocale) LocalizationSettings.AvailableLocales.RemoveLocale(locales[i]);
            }

            string code = SaveManager.Instance ? SaveManager.Instance.Settings.language : null;
            if (string.IsNullOrEmpty(code)) return;

            var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale && locale != LocalizationSettings.SelectedLocale) LocalizationSettings.SelectedLocale = locale;
        };
    }
}
