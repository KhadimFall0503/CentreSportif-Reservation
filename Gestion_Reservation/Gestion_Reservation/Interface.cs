using System;
using GestionReservations;

// =================================================================
//  INTERFACE DU SYSTÈME DE GESTION DES MEMBRES (système existant)
// =================================================================
public interface ISystemeGestionMembres
{
    bool ajouterMembre(Membre membre);
    bool verifierMembre(int idMembre);
    void modifierMembre(Membre membre);
    void supprimerMembre(int idMembre);
}

// Simulation en mémoire du système existant (pour le prototype).
public class SystemeGestionMembresSimule : ISystemeGestionMembres
{
    private readonly Dictionary<int, Membre> registre = new Dictionary<int, Membre>();

    public bool ajouterMembre(Membre membre)
    {
        if (membre == null || registre.ContainsKey(membre.IdMembre)) return false;
        registre[membre.IdMembre] = membre;
        return true;
    }

    public bool verifierMembre(int idMembre) { return registre.ContainsKey(idMembre); }

    public void modifierMembre(Membre membre)
    {
        if (membre != null && registre.ContainsKey(membre.IdMembre))
            registre[membre.IdMembre] = membre;
    }

    public void supprimerMembre(int idMembre) { registre.Remove(idMembre); }
}