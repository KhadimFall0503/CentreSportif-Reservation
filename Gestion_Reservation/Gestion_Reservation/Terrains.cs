using System;
using GestionReservations;


// =================================================================
//  TERRAIN
// =================================================================
public class Terrain
{
    private static int compteur = 0;

    private int idTerrain;
    private string numero;
    private Sport sport;
    private TypeTerrain type;
    private StatutTerrain statut;
    private double prixHeure;
    private List<Reservation> reservations = new List<Reservation>();

    public Terrain(string numero, Sport sport, TypeTerrain type, double prixHeure)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new ArgumentException("Le numéro du terrain est obligatoire.");
        if (prixHeure < 0)
            throw new ArgumentException("Le prix à l'heure ne peut pas être négatif.");
        idTerrain = ++compteur;
        this.numero = numero.Trim();
        this.sport = sport;
        this.type = type;
        this.prixHeure = prixHeure;
        statut = StatutTerrain.Disponible;
    }

    public Terrain(string numero, Sport sport) : this(numero, sport, TypeTerrain.Interieur, 0.0) { }

    public int IdTerrain { get { return idTerrain; } }
    public string Numero { get { return numero; } set { numero = value; } }
    public Sport Sport { get { return sport; } }
    public TypeTerrain Type { get { return type; } set { type = value; } }
    public StatutTerrain Statut { get { return statut; } set { statut = value; } }
    public double PrixHeure { get { return prixHeure; } set { prixHeure = value; } }
    public List<Reservation> Reservations { get { return new List<Reservation>(reservations); } }

    public void enregistrerReservation(Reservation r)
    {
        if (r != null && !reservations.Contains(r)) reservations.Add(r);
    }

    // Disponibilité pour une location d'une heure.
    public bool estDisponible(DateTime dateHeure)
    {
        return estDisponible(dateHeure, TimeSpan.FromHours(1), null);
    }

    // Disponibilité pour une durée donnée (en ignorant éventuellement une réservation).
    public bool estDisponible(DateTime debut, TimeSpan duree, Reservation aIgnorer)
    {
        if (statut == StatutTerrain.Ferme) return false;
        DateTime fin = debut + duree;
        foreach (Reservation r in reservations)
        {
            if (r == aIgnorer || !r.estActive()) continue;
            if (r.chevauche(debut, fin)) return false;
        }
        return true;
    }

    // Tente de réserver le terrain (impossible s'il est fermé).
    public bool reserver()
    {
        if (statut == StatutTerrain.Ferme) return false;
        statut = StatutTerrain.Reserve;
        return true;
    }

    public void liberer()
    {
        if (statut == StatutTerrain.Reserve) statut = StatutTerrain.Disponible;
    }

    // Recalcule le statut selon les réservations de l'heure courante.
    public void mettreAJourStatut()
    {
        if (statut == StatutTerrain.Ferme) return;
        DateTime now = DateTime.Now;
        DateTime heurePleine = now.Date.AddHours(now.Hour);
        statut = estDisponible(heurePleine) ? StatutTerrain.Disponible : StatutTerrain.Reserve;
    }

    public double heuresOccupees(DateTime debut, DateTime fin)
    {
        return reservations.Where(r => r.Statut != StatutReservation.Annulee)
                           .Sum(r => r.heuresOccupees(debut, fin));
    }

    public void afficher()
    {
        Console.WriteLine("[" + idTerrain + "] " + sport + " " + numero + " | " + type
                          + " | " + statut + " | base : " + Util.Argent(prixHeure) + "/h");
    }
}