using System;
using System.Text;

public class CentreSportif
{
    public const int HeureOuverture = 7;
    public const int HeureFermeture = 23;

    private static int compteur = 0;

    private int idCentre;
    private string nom;
    private string adresse;
    private List<Terrain> listeTerrains = new List<Terrain>();
    private List<Membre> listeMembres = new List<Membre>();
    private List<Reservation> listeReservations = new List<Reservation>();
    private List<Tarification> listeTarifications = new List<Tarification>();
    private ISystemeGestionMembres systemeMembres;

    public CentreSportif(string nom, string adresse, ISystemeGestionMembres systemeMembres)
    {
        idCentre = ++compteur;
        this.nom = nom;
        this.adresse = adresse;
        this.systemeMembres = systemeMembres ?? new SystemeGestionMembresSimule();
    }

    public CentreSportif(string nom, string adresse)
        : this(nom, adresse, new SystemeGestionMembresSimule()) { }

    public int IdCentre { get { return idCentre; } }
    public string Nom { get { return nom; } set { nom = value; } }
    public string Adresse { get { return adresse; } set { adresse = value; } }
    public List<Tarification> Tarifications { get { return new List<Tarification>(listeTarifications); } }

    // ---- membres ----
    public bool ajouterMembre(Membre membre)
    {
        if (membre == null) return false;
        if (!systemeMembres.ajouterMembre(membre)) return false;
        listeMembres.Add(membre);
        membre.Centre = this;
        return true;
    }

    public List<Membre> afficherMembres() { return new List<Membre>(listeMembres); }

    public Membre rechercherMembre(int id) { return listeMembres.FirstOrDefault(m => m.IdMembre == id); }

    // ---- terrains ----
    public void ajouterTerrain(Terrain t)
    {
        if (t == null) throw new ArgumentNullException("t");
        if (listeTerrains.Any(x => x.Sport == t.Sport && x.Numero == t.Numero))
            throw new InvalidOperationException("Un terrain de " + t.Sport + " porte déjà le numéro " + t.Numero + ".");
        listeTerrains.Add(t);
    }

    public List<Terrain> afficherTerrains() { return new List<Terrain>(listeTerrains); }

    public List<Terrain> afficherTerrains(Sport sport)
    {
        return listeTerrains.Where(t => t.Sport == sport).ToList();
    }

    public Terrain rechercherTerrain(int id) { return listeTerrains.FirstOrDefault(t => t.IdTerrain == id); }

    // ---- tarification ----
    public void ajouterTarification(Tarification t)
    {
        if (t != null) listeTarifications.Add(t);
    }

    public Tarification trouverTarification(Sport sport, DateTime date, TimeSpan heure)
    {
        return listeTarifications.FirstOrDefault(t => t.couvre(sport, date, heure));
    }

    // ---- disponibilité ----
    public bool verifierDisponibilite(Terrain terrain, DateTime dateHeure)
    {
        return terrain != null && terrain.estDisponible(dateHeure);
    }

    // Grille visuelle : une ligne par terrain, une colonne par heure.
    public string genererGrille(Sport sport, DateTime date)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Occupation " + sport + " le " + Util.Date(date) + "   (X = occupé, . = libre)");
        sb.Append("Terrain   ");
        for (int h = HeureOuverture; h < HeureFermeture; h++) sb.Append(h.ToString("00") + " ");
        sb.AppendLine();
        foreach (Terrain t in listeTerrains.Where(x => x.Sport == sport))
        {
            sb.Append(t.Numero.PadRight(10));
            for (int h = HeureOuverture; h < HeureFermeture; h++)
                sb.Append(t.estDisponible(date.Date.AddHours(h)) ? " . " : " X ");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // ---- réservations ----
    public bool effectuerReservation(Reservation r, out string erreur)
    {
        erreur = null;
        if (r == null) { erreur = "Réservation nulle."; return false; }
        if (listeReservations.Contains(r)) { erreur = "Réservation déjà enregistrée."; return false; }
        if (!listeMembres.Contains(r.Membre)) { erreur = "Le membre n'est pas inscrit à ce centre."; return false; }
        if (!listeTerrains.Contains(r.Terrain)) { erreur = "Le terrain n'appartient pas à ce centre."; return false; }

        erreur = r.validerRegles();
        if (erreur != null) return false;

        if (!r.Terrain.reserver()) { erreur = "Le terrain est fermé."; return false; }

        r.calculerMontant();
        r.Statut = StatutReservation.Confirmee;
        listeReservations.Add(r);
        r.Membre.enregistrerReservation(r);
        r.Terrain.enregistrerReservation(r);
        r.Terrain.mettreAJourStatut();
        return true;
    }

    public void effectuerReservation(Reservation r)
    {
        string erreur;
        if (!effectuerReservation(r, out erreur))
            throw new InvalidOperationException(erreur);
    }

    public List<Reservation> listerReservations()
    {
        foreach (Reservation r in listeReservations) r.actualiserStatut();
        return new List<Reservation>(listeReservations);
    }

    public Reservation rechercherReservation(int num)
    {
        return listeReservations.FirstOrDefault(r => r.IdReservation == num);
    }

    public List<Reservation> rechercherReservation(string nomMembre)
    {
        if (string.IsNullOrWhiteSpace(nomMembre)) return new List<Reservation>();
        return listeReservations
               .Where(r => r.Membre.NomComplet.IndexOf(nomMembre, StringComparison.OrdinalIgnoreCase) >= 0)
               .ToList();
    }

    public bool modifierReservation(Reservation r, DateTime nouvelleDateDebut, out string erreur)
    {
        erreur = null;
        if (r == null || !listeReservations.Contains(r))
        {
            erreur = "Réservation introuvable.";
            return false;
        }
        bool ok = r.modifier(nouvelleDateDebut, out erreur);
        r.Terrain.mettreAJourStatut();
        return ok;
    }

    public bool annulerReservation(int num, out string erreur)
    {
        erreur = null;
        Reservation r = rechercherReservation(num);
        if (r == null)
        {
            erreur = "Réservation introuvable.";
            return false;
        }
        bool ok = r.annuler();
        if (!ok) erreur = r.MotifRefus;
        r.Terrain.mettreAJourStatut();
        return ok;
    }

    // ---- rapports ----
    public RapportOccupation genererRapport(TypeRapport type, DateTime debut, DateTime fin)
    {
        RapportOccupation rapport = new RapportOccupation(type, debut, fin);
        rapport.generer(listeTerrains);
        return rapport;
    }

    public RapportOccupation genererRapport(TypeRapport type, DateTime reference)
    {
        RapportOccupation rapport = new RapportOccupation(type, reference);
        rapport.generer(listeTerrains);
        return rapport;
    }
}