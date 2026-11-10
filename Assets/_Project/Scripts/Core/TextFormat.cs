using System.Globalization;

namespace QueteI1.StatusEffects
{
    /// <summary>Formatage commun des nombres affichés (journal, panneau de debug).</summary>
    public static class TextFormat
    {
        public static string Num(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public static string Seconds(float value) => value.ToString("0.0", CultureInfo.InvariantCulture) + " s";

        public static string Signed(float value) => (value >= 0f ? "+" : "−") + Num(UnityEngine.Mathf.Abs(value));
    }
}
