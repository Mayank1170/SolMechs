using System.Text.RegularExpressions;

namespace MechBattle
{
    /// <summary>
    /// Helpers to parse and compose part/matrix codes (e.g., RA01, LA02, IN03, M04).
    /// </summary>
    public static class PartCodeUtil
    {
        static readonly Regex RxRA = new Regex(@"^RA(\d{2})$", RegexOptions.Compiled);
        static readonly Regex RxLA = new Regex(@"^LA(\d{2})$", RegexOptions.Compiled);
        static readonly Regex RxIN = new Regex(@"^IN(\d{2})$", RegexOptions.Compiled);
        static readonly Regex RxM = new Regex(@"^M(\d{2})$", RegexOptions.Compiled);

        public static bool IsRightArm(string code) => RxRA.IsMatch(code ?? "");
        public static bool IsLeftArm(string code) => RxLA.IsMatch(code ?? "");
        public static bool IsLower(string code) => RxIN.IsMatch(code ?? "");
        public static bool IsMatrix(string code) => RxM.IsMatch(code ?? "");

        /// <summary>Return family number (e.g., "01") or null if not parsable.</summary>
        public static string FamilyOf(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            var m = RxRA.Match(code); if (m.Success) return m.Groups[1].Value;
            m = RxLA.Match(code); if (m.Success) return m.Groups[1].Value;
            m = RxIN.Match(code); if (m.Success) return m.Groups[1].Value;
            m = RxM.Match(code); if (m.Success) return m.Groups[1].Value;
            return null;
        }

        public static string ComposeMatrix(string family) => $"M{family}";
        public static string ComposeRightArm(string family) => $"RA{family}";
        public static string ComposeLeftArm(string family) => $"LA{family}";
        public static string ComposeLower(string family) => $"IN{family}";

        public static MechBuild BuildForFamily(string family)
        {
            return new MechBuild
            {
                matrixId = ComposeMatrix(family),
                rightArmId = ComposeRightArm(family),
                leftArmId = ComposeLeftArm(family),
                lowerBodyId = ComposeLower(family)
            };
        }
    }
}
