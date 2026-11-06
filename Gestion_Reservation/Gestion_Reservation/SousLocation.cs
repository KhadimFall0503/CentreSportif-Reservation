using System;
using GestionReservations;

public class SousLocation
{
    private static int compteur = 0;

    private int idSousL;
    private DateTime dateSousLocation;
    private TimeSpan heure;
    private Membre locataire;
    private ReservationPermanente reservation;
    private bool confirmee;
    private ReservationOccasionnelle remplacement;

    public SousLocation(ReservationPermanente reservation, DateTime dateSousLocation, TimeSpan heure)
        : this(reservation, dateSousLocation, heure, null) { }

    public SousLocation(ReservationPermanente reservation, DateTime dateSousLocation, TimeSpan heure, Membre locataire)
    {
        if (reservation == null) throw new ArgumentNullException("reservation");
        idSousL = ++compteur;
        this.reservation = reservation;
        this.dateSousLocation = dateSousLocation.Date;
        this.heure = heure;
        this.locataire = locataire;
    }

    public int IdSousL { get { return idSousL; } }
    public DateTime DateSousLocation { get { return dateSousLocation; } }
    public TimeSpan Heure { get { return heure; } }
    public Membre Locataire { get { return locataire; } set { locataire = value; } }
    public ReservationPermanente Reservation { get { return reservation; } }
    public bool Confirmee { get { return confirmee; } }
    public ReservationOccasionnelle Remplacement { get { return remplacement; } }

    public void confirmer()
    {
        if (locataire == null)
            throw new InvalidOperationException("Aucun locataire n'est défini pour cette sous-location.");
        if (!locataire.peutReserver(reservation.Terrain.Sport))
            throw new InvalidOperationException("Le locataire n'a pas accès à ce sport.");
        confirmee = true;
    }

    // Heure de remplacement sur le même terrain.
    public void attribuerHeureR(DateTime dateRemplacement, TimeSpan heureRemplacement)
    {
        attribuerHeureR(reservation.Terrain, dateRemplacement, heureRemplacement);
    }

    // Heure de remplacement sur le terrain de son choix (selon les disponibilités).
    public void attribuerHeureR(Terrain terrain, DateTime dateRemplacement, TimeSpan heureRemplacement)
    {
        if (!confirmee)
            throw new InvalidOperationException("La sous-location doit d'abord être confirmée.");
        if (remplacement != null)
            throw new InvalidOperationException("Une heure de remplacement a déjà été attribuée.");

        Membre proprietaire = reservation.Membre;
        CentreSportif centre = proprietaire.Centre;
        DateTime debut = dateRemplacement.Date + heureRemplacement;
        Tarification tarif = centre.trouverTarification(terrain.Sport, debut.Date, heureRemplacement);

        ReservationOccasionnelle r = new ReservationOccasionnelle(proprietaire, terrain, tarif, debut, CanalReservation.Comptoir);
        r.marquerCommeRemplacement();

        string erreur;
        if (!centre.effectuerReservation(r, out erreur))
            throw new InvalidOperationException(erreur);
        remplacement = r;
    }

    public void afficher()
    {
        Console.WriteLine("      Sous-location #" + idSousL + " : " + Util.Date(dateSousLocation) + " " + Util.Heure(heure)
                          + " | locataire : " + (locataire == null ? "(aucun)" : locataire.NomComplet)
                          + " | " + (confirmee ? "confirmée" : "en attente")
                          + (remplacement != null ? " | remplacement : réservation #" + remplacement.IdReservation : ""));
    }
}