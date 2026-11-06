using System;
// =================================================================
//  PERIODE DE SAISON (type utilisé par ReservationPermanente)
//  Saison : du 1er septembre au 31 mai.
// =================================================================
public class PeriodeSaison
{
    private DateTime dateDebut;
    private DateTime dateFin;

    public PeriodeSaison(DateTime dateDebut, DateTime dateFin)
    {
        if (dateFin < dateDebut)
            throw new ArgumentException("La date de fin doit suivre la date de début.");
        this.dateDebut = dateDebut.Date;
        this.dateFin = dateFin.Date;
    }

    public PeriodeSaison(int anneeDebut)
        : this(new DateTime(anneeDebut, 9, 1), new DateTime(anneeDebut + 1, 5, 31)) { }

    public static PeriodeSaison saisonCourante()
    {
        DateTime auj = DateTime.Today;
        return new PeriodeSaison(auj.Month <= 5 ? auj.Year - 1 : auj.Year);
    }

    public DateTime DateDebut { get { return dateDebut; } }
    public DateTime DateFin { get { return dateFin; } }

    public bool contient(DateTime date)
    {
        return date.Date >= dateDebut && date.Date <= dateFin;
    }

    // Toutes les dates de la saison tombant un jour de semaine donné.
    public IEnumerable<DateTime> jours(DayOfWeek jour)
    {
        DateTime d = dateDebut;
        while (d.DayOfWeek != jour) d = d.AddDays(1);
        for (; d <= dateFin; d = d.AddDays(7)) yield return d;
    }

    public int compterOccurrences(DayOfWeek jour) { return jours(jour).Count(); }

    public override string ToString() { return Util.Date(dateDebut) + " au " + Util.Date(dateFin); }
}