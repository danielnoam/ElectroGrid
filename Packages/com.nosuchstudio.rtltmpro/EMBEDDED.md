Embedded copy of RTLTMPro 4.0.0 (https://github.com/pnarimani/RTLTMPro, MIT, UPMPackage folder at tag v4.0.0).

Changed from upstream:
- Scripts/Editor removed: its custom inspector uses TMP_UiEditorPanel, which no longer exists in com.unity.ugui 2.x, so it does not compile.
- Tests and the demo resources package removed.
- RTLTMPro.asmdef also defines TMP_VERSION_2_1_0_OR_NEWER for com.unity.ugui 2.0+, where TextMeshPro now lives. Without it RTLTextMeshPro hides `text` instead of overriding it, and code that sets text through a TMP_Text reference skips the RTL fix.
- RTLTextMeshPro mirrors left/right horizontal alignment while it shows RTL text and restores it for LTR text (`MirrorAlignment`). Centered text is unchanged. Avoid saving a scene while previewing an RTL locale in edit mode, or the mirrored alignment is saved.
- `RTLTextMeshPro.RtlFontSelector` lets the game swap a label to a script-specific font while it shows RTL text, restoring the label's own font and material afterwards. ElectroGrid registers it in `L10n`.
- RTL text renders with font features (kerning) suspended: TMP applies kerning pairs to the reversed string, which stacks some Hebrew letters on top of each other. Restored when the label shows LTR text again.
