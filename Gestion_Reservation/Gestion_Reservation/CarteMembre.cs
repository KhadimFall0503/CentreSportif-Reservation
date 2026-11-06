using System;
// =================================================================
//  CARTE MEMBRE
// =================================================================

public class CarteMembre
{
    // Grille progressive : plus on ajoute de sports, plus le prix par sport baisse.
    private static readonly double[] TarifAnnuelProgressif = { 0.0, 200.0, 350.0, 450.0 };

    private static int compteur = 0;

    private int idCarte;
    private TypeCarte typeCarte;
    private DateTime dateDebut;
    private DateTime dateFin;
    private double montant;
    private List<Sport> sports;          // sports couverts par la carte

    public CarteMembre(TypeCarte typeCarte, DateTime dateDebut, Sport sport)
        : this(typeCarte, dateDebut, new List<Sport> { sport }) { }

    public CarteMembre(TypeCarte typeCarte, DateTime dateDebut, List<Sport> sports)
    {
        if (sports == null || sports.Count == 0)
            throw new ArgumentException("La carte doit couvrir au moins un sport.");

        idCarte = ++compteur;
        this.typeCarte = typeCarte;
        this.dateDebut = dateDebut.Date;
        this.sports = sports.Distinct().ToList();

        switch (typeCarte)
        {
            case TypeCarte.Annuel: dateFin = this.dateDebut.AddYears(1).AddDays(-1); break;
            case TypeCarte.Mensuel: dateFin = this.dateDebut.AddMonths(1).AddDays(-1); break;
            default: dateFin = this.dateDebut.AddDays(6); break;
        }
        calculerMontant();
    }

    public int IdCarte { get { return idCarte; } }
    public TypeCarte TypeCarte { get { return typeCarte; } set { typeCarte = value; } }
    public DateTime DateDebut { get { return dateDebut; } }
    public DateTime DateFin { get { return dateFin; } }
    public double Montant { get { return montant; } }
    public List<Sport> Sports { get { return new List<Sport>(sports); } }

    public bool donnerAcces(Sport sport) { return sports.Contains(sport); }

    public bool estValide()
    {
        return DateTime.Today >= dateDebut && DateTime.Today <= dateFin;
    }

    public double calculerMontant()
    {
        double baseAnnuelle = TarifAnnuelProgressif[sports.Count];
        double coef = typeCarte == TypeCarte.Annuel ? 1.0
                    : typeCarte == TypeCarte.Mensuel ? 0.15
                    : 0.05;
        montant = Tarification.ajouterTaxes(baseAnnuelle * coef);
        return montant;
    }

    public override string ToString()
    {
        return "Carte #" + idCarte + " " + typeCarte + " [" + string.Join("+", sports) + "] du "
               + Util.Date(dateDebut) + " au " + Util.Date(dateFin) + " | " + Util.Argent(montant)
               + " taxes incl. | " + (estValide() ? "valide" : "non valide");
    }
}