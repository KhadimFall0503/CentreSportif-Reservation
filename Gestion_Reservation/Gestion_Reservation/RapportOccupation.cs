using System;
using GestionReservations;
using System.Text;


// =================================================================
//  RAPPORT D'OCCUPATION
// =================================================================
public class RapportOccupation
{
    private static int compteur = 0;

    private int idRapport;
    private TypeRapport typeRapport;
    private DateTime dateDebut;
    private DateTime dateFin;
    private double tauxOccupation;
    private string statistiques = "";

    // Période calculée à partir d'une date de référence et du type de rapport.
    public RapportOccupation(TypeRapport typeRapport, DateTime reference)
    {
        idRapport = ++compteur;
        this.typeRapport = typeRapport;
        switch (typeRapport)
        {
            case TypeRapport.Quotidien:
                dateDebut = reference.Date;
                dateFin = reference.Date;
                break;
            case TypeRapport.Hebdo:
                {
                    int decalage = ((int)reference.DayOfWeek + 6) % 7;   // lundi = début de semaine
                    dateDebut = reference.Date.AddDays(-decalage);
                    dateFin = dateDebut.AddDays(6);
                    break;
                }
            case TypeRapport.Mensuel:
                dateDebut = new DateTime(reference.Year, reference.Month, 1);
                dateFin = dateDebut.AddMonths(1).AddDays(-1);
                break;
            default:
                dateDebut = new DateTime(reference.Year, 1, 1);
                dateFin = new DateTime(reference.Year, 12, 31);
                break;
        }
    }

    public RapportOccupation(TypeRapport typeRapport, DateTime dateDebut, DateTime dateFin)
    {
        if (dateFin < dateDebut) throw new ArgumentException("La date de fin doit suivre la date de début.");
        idRapport = ++compteur;
        this.typeRapport = typeRapport;
        this.dateDebut = dateDebut.Date;
        this.dateFin = dateFin.Date;
    }

    public int IdRapport { get { return idRapport; } }
    public TypeRapport TypeRapport { get { return typeRapport; } }
    public DateTime DateDebut { get { return dateDebut; } }
    public DateTime DateFin { get { return dateFin; } }
    public double TauxOccupation { get { return tauxOccupation; } }
    public string Statistiques { get { return statistiques; } }

    public void generer(IEnumerable<Terrain> terrains)
    {
        List<Terrain> liste = terrains.ToList();
        int nbJours = (dateFin.Date - dateDebut.Date).Days + 1;
        double heuresParTerrain = nbJours * (CentreSportif.HeureFermeture - CentreSportif.HeureOuverture);
        double totalOccupe = 0.0, totalDispo = 0.0;
        StringBuilder sb = new StringBuilder();

        foreach (Sport sport in Enum.GetValues(typeof(Sport)))
        {
            List<Terrain> duSport = liste.Where(t => t.Sport == sport).ToList();
            if (duSport.Count == 0) continue;
            double occupe = duSport.Sum(t => t.heuresOccupees(dateDebut, dateFin));
            double dispo = duSport.Count * heuresParTerrain;
            totalOccupe += occupe;
            totalDispo += dispo;
            double taux = dispo > 0 ? occupe / dispo * 100.0 : 0.0;
            sb.AppendLine("  " + sport.ToString().PadRight(11) + ": " + duSport.Count + " terrain(s) | "
                          + occupe.ToString("0.#") + " h occupées sur " + dispo.ToString("0.#")
                          + " h | " + taux.ToString("0.0") + " %");
        }

        List<Reservation> touchees = liste.SelectMany(t => t.Reservations)
                                          .Distinct()
                                          .Where(r => r.Statut != StatutReservation.Annulee
                                                      && r.heuresOccupees(dateDebut, dateFin) > 0)
                                          .ToList();
        sb.AppendLine("  Réservations touchant la période : " + touchees.Count
                      + " (permanentes : " + touchees.Count(r => r is ReservationPermanente)
                      + ", occasionnelles : " + touchees.Count(r => r is ReservationOccasionnelle) + ")");

        tauxOccupation = totalDispo > 0 ? totalOccupe / totalDispo * 100.0 : 0.0;
        statistiques = sb.ToString();
    }

    private string construireTexte()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== RAPPORT D'OCCUPATION #" + idRapport + " (" + typeRapport + ") ===");
        sb.AppendLine("Période : " + Util.Date(dateDebut) + " au " + Util.Date(dateFin));
        sb.AppendLine("Taux d'occupation global : " + tauxOccupation.ToString("0.0") + " %");
        sb.AppendLine("Détail :");
        sb.Append(statistiques);
        return sb.ToString();
    }

    public void afficher() { Console.WriteLine(construireTexte()); }

    public void exporter()
    {
        string chemin = "rapport_" + typeRapport + "_" + dateDebut.ToString("yyyyMMdd") + ".txt";
        File.WriteAllText(chemin, construireTexte(), Encoding.UTF8);
        Console.WriteLine("Rapport exporté : " + Path.GetFullPath(chemin));
    }
}