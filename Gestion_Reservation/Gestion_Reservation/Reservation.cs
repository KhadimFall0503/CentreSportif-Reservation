using GestionReservations;

public abstract class Reservation
{
    private static int compteur = 0;

    private int idReservation;
    private DateTime dateReservation;
    private DateTime dateDebut;
    private TimeSpan duree;
    private string typeReservation;
    private StatutReservation statut;
    private double montant;
    private int nombreJoueurs;
    private Membre membre;
    private Terrain terrain;
    private Tarification tarif;
    private List<Paiement> paiements = new List<Paiement>();
    private string motifRefus = "";

    protected Reservation(Membre membre, Terrain terrain, Tarification tarif, DateTime dateDebut,
                          TimeSpan duree, string typeReservation, int nombreJoueurs)
    {
        if (membre == null) throw new ArgumentNullException("membre");
        if (terrain == null) throw new ArgumentNullException("terrain");
        if (duree <= TimeSpan.Zero) throw new ArgumentException("La durée doit être positive.");
        if (nombreJoueurs < 1 || nombreJoueurs > 4)
            throw new ArgumentException("Le nombre de joueurs doit être compris entre 1 et 4.");

        idReservation = ++compteur;
        dateReservation = DateTime.Now;
        this.membre = membre;
        this.terrain = terrain;
        this.tarif = tarif;
        this.dateDebut = dateDebut;
        this.duree = duree;
        this.typeReservation = typeReservation;
        this.nombreJoueurs = nombreJoueurs;
        statut = StatutReservation.EnAttente;
    }

    public int IdReservation { get { return idReservation; } }
    public DateTime DateReservation { get { return dateReservation; } }
    public DateTime DateDebut { get { return dateDebut; } protected set { dateDebut = value; } }
    public TimeSpan Duree { get { return duree; } }
    public string TypeReservation { get { return typeReservation; } }
    public StatutReservation Statut { get { return statut; } set { statut = value; } }
    public double Montant { get { return montant; } protected set { montant = value; } }
    public int NombreJoueurs { get { return nombreJoueurs; } set { nombreJoueurs = value; } }
    public Membre Membre { get { return membre; } }
    public Terrain Terrain { get { return terrain; } }
    public Tarification Tarif { get { return tarif; } protected set { tarif = value; } }
    public List<Paiement> Paiements { get { return new List<Paiement>(paiements); } }
    public string MotifRefus { get { return motifRefus; } protected set { motifRefus = value; } }

    // ---- à définir dans les classes filles ----
    public abstract double calculerMontant();
    public abstract IEnumerable<DateTime> obtenirCreneaux();   // début de chaque occurrence

    public bool estActive()
    {
        return statut == StatutReservation.EnAttente || statut == StatutReservation.Confirmee;
    }

    public virtual bool estDisponible()
    {
        foreach (DateTime c in obtenirCreneaux())
            if (!terrain.estDisponible(c, duree, this)) return false;
        return true;
    }

    public bool chevauche(DateTime debut, DateTime fin)
    {
        foreach (DateTime c in obtenirCreneaux())
            if (c < fin && c + duree > debut) return true;
        return false;
    }

    public double heuresOccupees(DateTime debut, DateTime fin)
    {
        DateTime limite = fin.Date.AddDays(1);
        int nb = obtenirCreneaux().Count(c => c >= debut.Date && c < limite);
        return nb * duree.TotalHours;
    }

    // Retourne null si la réservation respecte les règles, sinon un message d'erreur.
    public virtual string validerRegles()
    {
        if (!membre.peutReserver(terrain.Sport))
            return "Ce membre n'a pas accès à ce sport (carte absente, expirée ou non adaptée).";
        if (tarif == null)
            return "Aucune tarification n'est définie pour ce créneau.";

        TimeSpan ouverture = new TimeSpan(CentreSportif.HeureOuverture, 0, 0);
        TimeSpan fermeture = new TimeSpan(CentreSportif.HeureFermeture, 0, 0);
        foreach (DateTime c in obtenirCreneaux())
        {
            if (c.Minute != 0)
                return "Les réservations se font à l'heure pile (ex. 18:00).";
            if (c.TimeOfDay < ouverture || c.TimeOfDay + duree > fermeture)
                return "Les terrains sont accessibles de " + CentreSportif.HeureOuverture
                       + " h à " + CentreSportif.HeureFermeture + " h.";
        }
        if (!estDisponible())
            return "Le terrain n'est pas disponible à ce moment.";
        return null;
    }

    // Modification : revalide les règles et recalcule le montant. Annule le changement en cas d'échec.
    public bool modifier(DateTime nouvelleDateDebut, out string erreur)
    {
        erreur = null;
        if (!estActive())
        {
            erreur = "Seule une réservation active peut être modifiée.";
            return false;
        }

        DateTime ancienneDate = dateDebut;
        Tarification ancienTarif = tarif;
        double ancienMontant = montant;

        appliquerNouvelleDate(nouvelleDateDebut);
        if (membre.Centre != null)
            tarif = membre.Centre.trouverTarification(terrain.Sport, nouvelleDateDebut.Date, nouvelleDateDebut.TimeOfDay);

        erreur = validerRegles();
        if (erreur == null)
        {
            calculerMontant();
            return true;
        }

        appliquerNouvelleDate(ancienneDate);
        tarif = ancienTarif;
        montant = ancienMontant;
        return false;
    }

    protected virtual void appliquerNouvelleDate(DateTime nouvelleDateDebut)
    {
        dateDebut = nouvelleDateDebut;
    }

    public virtual bool annuler()
    {
        if (!estActive())
        {
            motifRefus = "La réservation n'est pas active (déjà annulée ou terminée).";
            return false;
        }
        foreach (Paiement p in paiements)
            if (p.Statut == StatutPaiement.Paye) p.rembourser();
        statut = StatutReservation.Annulee;
        motifRefus = "";
        return true;
    }

    public void actualiserStatut()
    {
        if (statut != StatutReservation.Confirmee) return;
        DateTime fin = obtenirCreneaux().DefaultIfEmpty(dateDebut).Max() + duree;
        if (fin < DateTime.Now) statut = StatutReservation.Terminee;
    }

    // ---- paiements ----
    public Paiement payer(double somme, TypePaiement mode)
    {
        if (somme <= 0) throw new InvalidOperationException("Le montant à payer doit être positif.");
        if (somme > soldeDu() + 0.005) throw new InvalidOperationException("Le montant dépasse le solde dû.");
        Paiement p = new Paiement(Math.Round(somme, 2), mode);
        p.effectuer();
        paiements.Add(p);
        return p;
    }

    public double montantPaye()
    {
        return Math.Round(paiements.Where(p => p.Statut == StatutPaiement.Paye).Sum(p => p.Montant), 2);
    }

    public double soldeDu()
    {
        if (!estActive()) return 0.0;
        return Math.Max(0.0, Math.Round(montant - montantPaye(), 2));
    }

    protected void definirMembre(Membre nouveau) { membre = nouveau; }

    public virtual void afficher()
    {
        Console.WriteLine("[" + idReservation + "] " + typeReservation + " | " + terrain.Sport + " "
                          + terrain.Numero + " | " + membre.NomComplet + " | " + Util.DateHeure(dateDebut)
                          + " (" + duree.TotalHours + " h) | joueurs : " + nombreJoueurs + " | " + statut
                          + " | " + Util.Argent(montant) + " taxes incl. | payé : " + Util.Argent(montantPaye()));
    }
}

// RESERVATION OCCASIONNELLE
public class ReservationOccasionnelle : Reservation
{
    private CanalReservation canal;
    private bool estRemplacement;

    public ReservationOccasionnelle(Membre membre, Terrain terrain, Tarification tarif,
                                    DateTime dateDebut, CanalReservation canal)
        : this(membre, terrain, tarif, dateDebut, canal, 2) { }

    public ReservationOccasionnelle(Membre membre, Terrain terrain, Tarification tarif,
                                    DateTime dateDebut, CanalReservation canal, int nombreJoueurs)
        : base(membre, terrain, tarif, dateDebut, TimeSpan.FromHours(1), "Occasionnelle", nombreJoueurs)
    {
        this.canal = canal;
    }

    public CanalReservation Canal { get { return canal; } set { canal = value; } }
    public bool EstRemplacement { get { return estRemplacement; } }

    // Paiement immédiat sauf pour une réservation par téléphone (payée à l'arrivée).
    public bool PaiementImmediatRequis { get { return canal != CanalReservation.Telephone && !estRemplacement; } }

    public void marquerCommeRemplacement()
    {
        estRemplacement = true;
        calculerMontant();
    }

    public override IEnumerable<DateTime> obtenirCreneaux()
    {
        yield return DateDebut;
    }

    public override double calculerMontant()
    {
        if (estRemplacement || Tarif == null) Montant = 0.0;
        else Montant = Tarification.ajouterTaxes(Tarif.calculerMontant(Duree));
        return Montant;
    }

    public override string validerRegles()
    {
        if (DateDebut < DateTime.Now)
            return "Cette date est déjà passée.";
        if (DateDebut > DateTime.Now.AddDays(3))
            return "Une réservation occasionnelle se fait au plus 3 jours à l'avance.";
        return base.validerRegles();
    }

    // Annulation selon le délai réel avant la réservation.
    public override bool annuler()
    {
        double heures = (DateDebut - DateTime.Now).TotalHours;
        return annuler((int)Math.Floor(heures));
    }

    // Annulation avec le nombre d'heures restant avant la réservation.
    public bool annuler(int heureAvant)
    {
        if (!estActive())
        {
            MotifRefus = "La réservation n'est pas active (déjà annulée ou terminée).";
            return false;
        }
        if (heureAvant < 24)
        {
            MotifRefus = "Annulation refusée : il faut annuler au moins 24 h à l'avance. "
                       + "La réservation non occupée demeure payable.";
            return false;
        }
        return base.annuler();
    }

    public override void afficher()
    {
        base.afficher();
        Console.WriteLine("      Canal : " + canal + (estRemplacement ? " | heure de remplacement (sans frais)" : ""));
    }
}
// =================================================================
//  RÉSERVATION PERMANENTE
//  (saison du 1er septembre au 31 mai, jour et heures fixes,
//   50 % à la réservation et 50 % avant le début de la saison)
// =================================================================
public class ReservationPermanente : Reservation
{
    private DayOfWeek jourFixe;
    private TimeSpan heureDebut;
    private TimeSpan heureFin;
    private PeriodeSaison periode;
    private int nbrOcc;
    private List<SousLocation> sousLocations = new List<SousLocation>();

    public ReservationPermanente(Membre membre, Terrain terrain, Tarification tarif, DayOfWeek jourFixe,
                                 TimeSpan heureDebut, TimeSpan heureFin, int nombreJoueurs)
        : this(membre, terrain, tarif, jourFixe, heureDebut, heureFin, PeriodeSaison.saisonCourante(), nombreJoueurs) { }

    public ReservationPermanente(Membre membre, Terrain terrain, Tarification tarif, DayOfWeek jourFixe,
                                 TimeSpan heureDebut, TimeSpan heureFin, PeriodeSaison periode, int nombreJoueurs)
        : base(membre, terrain, tarif, PremiereOccurrence(periode, jourFixe) + heureDebut,
               heureFin - heureDebut, "Permanente", nombreJoueurs)
    {
        this.jourFixe = jourFixe;
        this.heureDebut = heureDebut;
        this.heureFin = heureFin;
        this.periode = periode;
        nbrOcc = periode.compterOccurrences(jourFixe);
    }

    private static DateTime PremiereOccurrence(PeriodeSaison p, DayOfWeek jour)
    {
        if (p == null) throw new ArgumentNullException("periode");
        DateTime d = p.DateDebut;
        while (d.DayOfWeek != jour) d = d.AddDays(1);
        return d;
    }

    public DayOfWeek JourFixe { get { return jourFixe; } }
    public TimeSpan HeureDebut { get { return heureDebut; } }
    public TimeSpan HeureFin { get { return heureFin; } }
    public PeriodeSaison Periode { get { return periode; } }
    public int NbrOcc { get { return nbrOcc; } }
    public List<SousLocation> SousLocations { get { return new List<SousLocation>(sousLocations); } }

    public override IEnumerable<DateTime> obtenirCreneaux()
    {
        foreach (DateTime jour in periode.jours(jourFixe))
            yield return jour + heureDebut;
    }

    public override double calculerMontant()
    {
        if (Tarif == null) Montant = 0.0;
        else Montant = Tarification.ajouterTaxes(Tarif.calculerMontant(Duree) * nbrOcc);
        return Montant;
    }

    public override string validerRegles()
    {
        if (!(Membre is MembreRegulier))
            return "Seul un membre régulier peut faire une réservation permanente.";
        if (NombreJoueurs < 2 || NombreJoueurs > 4)
            return "Une réservation permanente implique de 2 à 4 joueurs.";
        if (heureFin <= heureDebut || Duree.TotalMinutes % 60 != 0)
            return "Les heures fixes doivent former un nombre entier d'heures (heure de fin > heure de début).";
        if (nbrOcc == 0)
            return "La période ne contient aucune occurrence de ce jour.";
        return base.validerRegles();
    }

    // Pour une réservation permanente, la "nouvelle date" fixe le nouveau jour de semaine et la nouvelle heure.
    protected override void appliquerNouvelleDate(DateTime nouvelleDateDebut)
    {
        jourFixe = nouvelleDateDebut.DayOfWeek;
        heureDebut = nouvelleDateDebut.TimeOfDay;
        heureFin = heureDebut + Duree;
        nbrOcc = periode.compterOccurrences(jourFixe);
        DateDebut = PremiereOccurrence(periode, jourFixe) + heureDebut;
    }

    // ---- paiements : 50 % à la réservation, 50 % avant la saison ----
    public Paiement payerAcompte(TypePaiement mode)
    {
        if (montantPaye() > 0.004)
            throw new InvalidOperationException("L'acompte a déjà été versé.");
        return payer(Math.Round(Montant / 2.0, 2), mode);
    }

    public void payerSolde() { payerSolde(TypePaiement.Comptant); }

    public void payerSolde(TypePaiement mode)
    {
        double solde = soldeDu();
        if (solde <= 0.004)
            throw new InvalidOperationException("Aucun solde à payer.");
        payer(solde, mode);
    }

    // ---- changements en cours d'année ----
    public bool transferer(Membre nouveauMembre)
    {
        if (!estActive() || nouveauMembre == null) return false;
        if (!(nouveauMembre is MembreRegulier)) return false;
        if (!nouveauMembre.peutReserver(Terrain.Sport)) return false;

        Membre ancien = Membre;
        ancien.retirerReservation(this);
        definirMembre(nouveauMembre);
        nouveauMembre.enregistrerReservation(this);
        return true;
    }

    public SousLocation sousLouer(DateTime date, TimeSpan heure)
    {
        if (!estActive())
            throw new InvalidOperationException("La réservation permanente n'est plus active.");
        if (!periode.contient(date) || date.DayOfWeek != jourFixe)
            throw new InvalidOperationException("Cette date ne correspond à aucune occurrence de la réservation permanente.");
        if (heure < heureDebut || heure + TimeSpan.FromHours(1) > heureFin)
            throw new InvalidOperationException("Cette heure est en dehors de la plage horaire réservée.");
        if (sousLocations.Any(s => s.DateSousLocation.Date == date.Date && s.Heure == heure))
            throw new InvalidOperationException("Ce créneau est déjà sous-loué.");

        SousLocation sl = new SousLocation(this, date.Date, heure);
        sousLocations.Add(sl);
        return sl;
    }

    public override void afficher()
    {
        base.afficher();
        Console.WriteLine("      Jour fixe : " + jourFixe + " " + Util.Heure(heureDebut) + "-" + Util.Heure(heureFin)
                          + " | saison : " + periode + " | occurrences : " + nbrOcc
                          + " | acompte 50 % : " + Util.Argent(Math.Round(Montant / 2.0, 2))
                          + " | solde dû : " + Util.Argent(soldeDu()));
        foreach (SousLocation s in sousLocations) s.afficher();
    }
}

