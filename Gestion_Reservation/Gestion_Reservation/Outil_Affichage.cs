using System;
//  OUTILS D'AFFICHAGE

internal static class Util
{
    public static string Heure(TimeSpan t) { return t.ToString(@"hh\:mm"); }
    public static string Argent(double v) { return v.ToString("0.00") + " $"; }
    public static string DateHeure(DateTime d) { return d.ToString("yyyy-MM-dd HH:mm"); }
    public static string Date(DateTime d) { return d.ToString("yyyy-MM-dd"); }
}