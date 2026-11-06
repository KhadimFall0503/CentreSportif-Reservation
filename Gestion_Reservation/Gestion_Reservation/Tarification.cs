using System;
//  TARIFICATION
//  Un enregistrement = un sport + un jour de semaine + un bloc horaire.
// =================================================================
public class Tarification
{
    public const double TauxTPS = 0.05;
    public const double TauxTVQ = 0.09975;

    // À alimenter avec les jours fériés (tarif "jourFerie" = true).
    public static readonly HashSet<DateTime> JoursFeries = new HashSet<DateTime>();

    private static int compteur = 0;

    private int idTarification;
    private Sport sport;
    private string blocHoraire;          // format "HH:mm-HH:mm"
    private DayOfWeek jourSemaine;
    private bool jourFerie;
    private double prixHeure;
    private TimeSpan debutBloc;
    private TimeSpan finBloc;

    public Tarification(Sport sport, string blocHoraire, DayOfWeek jourSemaine, double prixHeure)
        : this(sport, blocHoraire, jourSemaine, false, prixHeure) { }

    public Tarification(Sport sport, string blocHoraire, DayOfWeek jourSemaine, bool jourFerie, double prixHeure)
    {
        string[] bornes = (blocHoraire ?? "").Split('-');
        TimeSpan debut = TimeSpan.Zero, fin = TimeSpan.Zero;
        bool valide = bornes.Length == 2
                      && TimeSpan.TryParse(bornes[0], out debut)
                      && TimeSpan.TryParse(bornes[1], out fin)
                      && fin > debut;
        if (!valide)
            throw new ArgumentException("Bloc horaire invalide (format attendu : HH:mm-HH:mm).");
        if (prixHeure < 0)
            throw new ArgumentException("Le prix à l'heure ne peut pas être négatif.");

        idTarification = ++compteur;
        this.sport = sport;
        this.blocHoraire = blocHoraire;
        this.jourSemaine = jourSemaine;
        this.jourFerie = jourFerie;
        this.prixHeure = prixHeure;
        debutBloc = debut;
        finBloc = fin;
    }

    public int IdTarification { get { return idTarification; } }
    public Sport Sport { get { return sport; } }
    public string BlocHoraire { get { return blocHoraire; } set { blocHoraire = value; } }
    public DayOfWeek JourSemaine { get { return jourSemaine; } }
    public bool JourFerie { get { return jourFerie; } }
    public double PrixHeure { get { return prixHeure; } set { prixHeure = value; } }

    // Ce tarif s'applique-t-il à ce sport, cette date et cette heure ?
    public bool couvre(Sport s, DateTime date, TimeSpan heure)
    {
        if (s != sport) return false;
        bool ferie = JoursFeries.Contains(date.Date);
        if (ferie != jourFerie) return false;
        if (!jourFerie && date.DayOfWeek != jourSemaine) return false;
        return heure >= debutBloc && heure < finBloc;
    }

    // Montant hors taxes pour une durée donnée.
    public double calculerMontant(TimeSpan duree)
    {
        return Math.Round(prixHeure * duree.TotalHours, 2);
    }

    public static double ajouterTaxes(double montantHT)
    {
        return Math.Round(montantHT * (1.0 + TauxTPS + TauxTVQ), 2);
    }

    public override string ToString()
    {
        return Sport + " | " + jourSemaine + " " + blocHoraire + " | " + Util.Argent(prixHeure) + "/h";
    }
}