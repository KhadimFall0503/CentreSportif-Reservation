using System;
using GestionReservations;


// =================================================================
//  MEMBRE (classe de base) ET SPÉCIALISATIONS
// =================================================================
public class Membre
{
    private static int compteur = 0;

    private int idMembre;
    private string nom;
    private string prenom;
    private string courriel;
    private string telephone;
    private DateTime dateInscription;
    private List<CarteMembre> cartes = new List<CarteMembre>();
    private List<Reservation> reservations = new List<Reservation>();
    private CentreSportif centre;

    public Membre(string nom, string prenom, string courriel, string telephone)
    {
        if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom))
            throw new ArgumentException("Le nom et le prénom sont obligatoires.");
        idMembre = ++compteur;
        this.nom = nom.Trim();
        this.prenom = prenom.Trim();
        this.courriel = courriel ?? "";
        this.telephone = telephone ?? "";
        dateInscription = DateTime.Now;
    }

    public Membre(string nom, string prenom) : this(nom, prenom, "", "") { }

    public int IdMembre { get { return idMembre; } }
    public string Nom { get { return nom; } set { nom = value; } }
    public string Prenom { get { return prenom; } set { prenom = value; } }
    public string Courriel { get { return courriel; } set { courriel = value; } }
    public string Telephone { get { return telephone; } set { telephone = value; } }
    public DateTime DateInscription { get { return dateInscription; } }
    public string NomComplet { get { return prenom + " " + nom; } }
    public List<CarteMembre> Cartes { get { return new List<CarteMembre>(cartes); } }
    public CentreSportif Centre { get { return centre; } set { centre = value; } }

    // ---- cartes ----
    public virtual void ajouterCarte(CarteMembre carte)
    {
        if (carte != null) cartes.Add(carte);
    }

    public CarteMembre carteValidePour(Sport sport)
    {
        return cartes.FirstOrDefault(c => c.estValide() && c.donnerAcces(sport));
    }

    // ---- règles d'accès (redéfinies dans les classes filles) ----
    public virtual bool peutReserver(Sport sport) { return false; }
    public virtual string typeMembre() { return "Membre"; }

    // ---- gestion des réservations du membre ----
    public void enregistrerReservation(Reservation r)
    {
        if (r != null && !reservations.Contains(r)) reservations.Add(r);
    }

    public void retirerReservation(Reservation r) { reservations.Remove(r); }

    public List<Reservation> consulterReservation()
    {
        foreach (Reservation r in reservations) r.actualiserStatut();
        return new List<Reservation>(reservations);
    }

    // Réservation OCCASIONNELLE
    public Reservation reserverTerrain(Terrain terrain, DateTime date, TimeSpan heure, CanalReservation canal)
    {
        exigerCentre();
        DateTime debut = date.Date + heure;
        Tarification tarif = centre.trouverTarification(terrain.Sport, debut.Date, heure);
        ReservationOccasionnelle r = new ReservationOccasionnelle(this, terrain, tarif, debut, canal);
        string erreur;
        if (!centre.effectuerReservation(r, out erreur))
            throw new InvalidOperationException(erreur);
        return r;
    }

    // Réservation PERMANENTE (surcharge)
    public Reservation reserverTerrain(Terrain terrain, DayOfWeek jourFixe, TimeSpan heureDebut,
                                       TimeSpan heureFin, PeriodeSaison periode, int nombreJoueurs)
    {
        exigerCentre();
        DateTime premier = periode.jours(jourFixe).FirstOrDefault();
        if (premier == default(DateTime))
            throw new InvalidOperationException("La période ne contient aucune occurrence de ce jour.");
        Tarification tarif = centre.trouverTarification(terrain.Sport, premier, heureDebut);
        ReservationPermanente r = new ReservationPermanente(this, terrain, tarif, jourFixe,
                                                            heureDebut, heureFin, periode, nombreJoueurs);
        string erreur;
        if (!centre.effectuerReservation(r, out erreur))
            throw new InvalidOperationException(erreur);
        return r;
    }

    public void modifierReservation(Reservation reservation, DateTime nouvelleDateDebut)
    {
        exigerCentre();
        if (reservation == null || reservation.Membre != this)
            throw new InvalidOperationException("Cette réservation n'appartient pas à ce membre.");
        string erreur;
        if (!centre.modifierReservation(reservation, nouvelleDateDebut, out erreur))
            throw new InvalidOperationException(erreur);
    }

    public bool annulerReservation(int id)
    {
        exigerCentre();
        Reservation r = reservations.FirstOrDefault(x => x.IdReservation == id);
        if (r == null)
            throw new InvalidOperationException("Aucune réservation n° " + id + " pour ce membre.");
        string erreur;
        return centre.annulerReservation(id, out erreur);
    }

    public virtual void afficher()
    {
        Console.WriteLine("[" + idMembre + "] " + NomComplet + " (" + typeMembre() + ") | " + courriel
                          + " | " + telephone + " | inscrit le " + Util.Date(dateInscription));
        foreach (CarteMembre c in cartes) Console.WriteLine("      " + c);
    }

    private void exigerCentre()
    {
        if (centre == null)
            throw new InvalidOperationException("Le membre n'est rattaché à aucun centre sportif.");
    }
}
public class MembreRegulier : Membre
{
    private TypeCarte typeCarte;

    public MembreRegulier(string nom, string prenom, string courriel, string telephone)
        : base(nom, prenom, courriel, telephone) { typeCarte = TypeCarte.Annuel; }

    public MembreRegulier(string nom, string prenom) : base(nom, prenom) { typeCarte = TypeCarte.Annuel; }

    public TypeCarte TypeCarte { get { return typeCarte; } set { typeCarte = value; } }

    public override void ajouterCarte(CarteMembre carte)
    {
        base.ajouterCarte(carte);
        if (carte != null) typeCarte = carte.TypeCarte;
    }

    public override bool peutReserver(Sport sport) { return carteValidePour(sport) != null; }
    public override string typeMembre() { return "Membre régulier"; }
}
public class MembreVisiteur : Membre
{
    private DateTime dateVisite;

    public MembreVisiteur(string nom, string prenom, string courriel, string telephone, DateTime dateVisite)
        : base(nom, prenom, courriel, telephone) { this.dateVisite = dateVisite.Date; }

    public MembreVisiteur(string nom, string prenom) : base(nom, prenom) { dateVisite = DateTime.Today; }

    public DateTime DateVisite { get { return dateVisite; } set { dateVisite = value.Date; } }

    // Règle simplifiée : les règles réelles sont appliquées par le système de gestion des membres.
    public override bool peutReserver(Sport sport) { return dateVisite >= DateTime.Today; }
    public override string typeMembre() { return "Membre visiteur"; }

    public override void afficher()
    {
        base.afficher();
        Console.WriteLine("      Date de visite : " + Util.Date(dateVisite));
    }
}

public class Instructeur : Membre
{
    private string specialite;

    public Instructeur(string nom, string prenom, string courriel, string telephone, string specialite)
        : base(nom, prenom, courriel, telephone) { this.specialite = specialite ?? ""; }

    public Instructeur(string nom, string prenom, string specialite)
        : base(nom, prenom) { this.specialite = specialite ?? ""; }

    public string Specialite { get { return specialite; } set { specialite = value; } }

    public override bool peutReserver(Sport sport) { return true; }
    public override string typeMembre() { return "Instructeur"; }

    public override void afficher()
    {
        base.afficher();
        Console.WriteLine("      Spécialité : " + specialite);
    }
}
// Préposé à la clientèle / au comptoir d'accueil

public class Clientele : Membre
{
    private string poste;

    public Clientele(string nom, string prenom, string courriel, string telephone, string poste)
        : base(nom, prenom, courriel, telephone) { this.poste = poste ?? ""; }

    public Clientele(string nom, string prenom, string poste)
        : base(nom, prenom) { this.poste = poste ?? ""; }

    public string Poste { get { return poste; } set { poste = value; } }

    public override bool peutReserver(Sport sport) { return true; }
    public override string typeMembre() { return "Préposé clientèle"; }

    public override void afficher()
    {
        base.afficher();
        Console.WriteLine("      Poste : " + poste);
    }
}




