using System.Windows.Media;
namespace OpenCvWpfTracking
{
    // 2026-10-15 V31_13: UI-only palette; immutable shared brushes avoid per-event parsing/allocation.
    internal static class LegacyVideoIntegration
    {
        internal const string EoColor="#C9E987",IrColor="#C4AFF3",AiColor="#39FF14",FireColor="#FF3B30",SmokeColor="#D8D0AB";
        internal const string AlertAiColor="#55EE32",AlertFireColor="#FF8178";
        private static readonly Brush Eo=FrozenBrush(EoColor),Ir=FrozenBrush(IrColor),Ai=FrozenBrush(AlertAiColor),Fire=FrozenBrush(AlertFireColor),Smoke=FrozenBrush(SmokeColor);
        private static readonly Brush FireBackground=FrozenBrush("#652D2A"),SmokeBackground=FrozenBrush("#595645"),AiBackground=FrozenBrush("#29452F");
        internal static Brush FrozenBrush(string color){var brush=(Brush)new BrushConverter().ConvertFromString(color);brush.Freeze();return brush;}
        internal static Brush ChannelBrush(bool ir)=>ir?Ir:Eo;
        internal static Brush EventBrush(string type)=>type=="FIRE"?Fire:type=="SMOKE"?Smoke:Ai;
        internal static Brush EventBackground(string type)=>type=="FIRE"?FireBackground:type=="SMOKE"?SmokeBackground:AiBackground;
    }
}
