using TMPro;
using UnityEngine;

namespace RTLTMPro
{
    [ExecuteInEditMode]
    public class RTLTextMeshPro : TextMeshProUGUI
    {
        // ReSharper disable once InconsistentNaming
#if TMP_VERSION_2_1_0_OR_NEWER
        public override string text
#else
        public new string text
#endif
        {
            get { return base.text; }
            set
            {
                if (originalText == value)
                    return;

                originalText = value;

                UpdateText();
            }
        }

        public string OriginalText
        {
            get { return originalText; }
        }

        public bool PreserveNumbers
        {
            get { return preserveNumbers; }
            set
            {
                if (preserveNumbers == value)
                    return;

                preserveNumbers = value;
                havePropertiesChanged = true;
            }
        }

        public bool Farsi
        {
            get { return farsi; }
            set
            {
                if (farsi == value)
                    return;

                farsi = value;
                havePropertiesChanged = true;
            }
        }

        public bool FixTags
        {
            get { return fixTags; }
            set
            {
                if (fixTags == value)
                    return;

                fixTags = value;
                havePropertiesChanged = true;
            }
        }

        public bool ForceFix
        {
            get { return forceFix; }
            set
            {
                if (forceFix == value)
                    return;

                forceFix = value;
                havePropertiesChanged = true;
            }
        }

        [SerializeField] protected bool preserveNumbers;

        [SerializeField] protected bool farsi = true;

        [SerializeField] [TextArea(3, 10)] protected string originalText;

        [SerializeField] protected bool fixTags = true;

        [SerializeField] protected bool forceFix;

        protected readonly FastStringBuilder finalText = new FastStringBuilder(RTLSupport.DefaultBufferSize);

        protected void Update()
        {
            if (havePropertiesChanged)
            {
                UpdateText();
            }
        }

        public void UpdateText()
        {
            if (originalText == null)
                originalText = "";

            if (ForceFix == false && TextUtils.IsRTLInput(originalText) == false)
            {
                isRightToLeftText = false;
                SwapFont(false);
                SuspendFontFeatures(false);
                base.text = originalText;
                MirrorAlignment(false);
            } else
            {
                isRightToLeftText = true;
                SwapFont(true);
                SuspendFontFeatures(true);
                base.text = GetFixedText(originalText);
                MirrorAlignment(true);
            }

            havePropertiesChanged = true;
        }

        // ElectroGrid: TMP applies kerning pairs as if the reversed RTL string were in reading order, which pulls some
        // letters on top of their neighbours (ר over ד in הגדרות). RTL text renders without font features.
        private System.Collections.Generic.List<UnityEngine.TextCore.OTL_FeatureTag> suspendedFeatures;

        private void SuspendFontFeatures(bool rtl)
        {
            if (rtl)
            {
                if (suspendedFeatures != null || fontFeatures.Count == 0) return;
                suspendedFeatures = new System.Collections.Generic.List<UnityEngine.TextCore.OTL_FeatureTag>(fontFeatures);
                fontFeatures.Clear();
            }
            else if (suspendedFeatures != null)
            {
                fontFeatures.AddRange(suspendedFeatures);
                suspendedFeatures = null;
            }
        }

        /// <summary>
        /// ElectroGrid: picks the font for a right-to-left string, or null to keep the label's own font. Display fonts
        /// rarely have Hebrew or Arabic, and borrowing the letters through a fallback mixes in mismatched metrics.
        /// </summary>
        public static System.Func<string, TMP_FontAsset> RtlFontSelector;

        private TMP_FontAsset ltrFont;
        private Material ltrMaterial;

        private void SwapFont(bool rtl)
        {
            if (rtl)
            {
                var rtlFont = RtlFontSelector?.Invoke(originalText);
                if (!rtlFont || rtlFont == font) return;

                if (!ltrFont)
                {
                    ltrFont = font;
                    ltrMaterial = fontSharedMaterial;
                }
                font = rtlFont;
                // The font setter keeps the old material, which samples the old font's atlas
                fontSharedMaterial = rtlFont.material;
            }
            else if (ltrFont)
            {
                font = ltrFont;
                fontSharedMaterial = ltrMaterial;
                ltrFont = null;
                ltrMaterial = null;
            }
        }

        // ElectroGrid: right-to-left text reads from the right edge, so a left-aligned label flips to the right while
        // it shows RTL text and flips back when it shows left-to-right text again. Centered text is left alone.
        private bool alignmentMirrored;

        private void MirrorAlignment(bool rtl)
        {
            if (rtl == alignmentMirrored) return;

            var mirrored = MirroredAlignment(horizontalAlignment);
            if (mirrored == horizontalAlignment) return;

            horizontalAlignment = mirrored;
            alignmentMirrored = rtl;
        }

        private static HorizontalAlignmentOptions MirroredAlignment(HorizontalAlignmentOptions alignment)
        {
            switch (alignment)
            {
                case HorizontalAlignmentOptions.Left: return HorizontalAlignmentOptions.Right;
                case HorizontalAlignmentOptions.Right: return HorizontalAlignmentOptions.Left;
                default: return alignment;
            }
        }

        private string GetFixedText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            finalText.Clear();
            RTLSupport.FixRTL(input, finalText, farsi, fixTags, preserveNumbers);
            finalText.Reverse();
            return finalText.ToString();
        }
    }
}