using System;
// =================================================================
//  PAIEMENT
// =================================================================
public class Paiement
{
    private static int compteur = 0;

    private int idPaiement;
    private DateTime datePaiement;
    private double montant;
    private TypePaiement modePaiement;
    private StatutPaiement statut;

    public Paiement(double montant, TypePaiement modePaiement)
    {
        if (montant <= 0) throw new ArgumentException("Le montant du paiement doit être positif.");
        idPaiement = ++compteur;
        this.montant = montant;
        this.modePaiement = modePaiement;
        datePaiement = DateTime.Now;
        statut = StatutPaiement.EnAttente;
    }

    public Paiement(double montant) : this(montant, TypePaiement.Comptant) { }

    public int IdPaiement { get { return idPaiement; } }
    public DateTime DatePaiement { get { return datePaiement; } }
    public double Montant { get { return montant; } }
    public TypePaiement ModePaiement { get { return modePaiement; } set { modePaiement = value; } }
    public StatutPaiement Statut { get { return statut; } }

    public void effectuer()
    {
        if (statut == StatutPaiement.Paye) return;
        statut = StatutPaiement.Paye;
        datePaiement = DateTime.Now;
    }

    public void rembourser()
    {
        if (statut == StatutPaiement.Paye) statut = StatutPaiement.Rembourse;
    }

    public override string ToString()
    {
        return "Paiement #" + idPaiement + " " + Util.Argent(montant) + " " + modePaiement
               + " " + statut + " (" + Util.DateHeure(datePaiement) + ")";
    }
}