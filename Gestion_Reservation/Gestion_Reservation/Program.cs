using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GestionReservations
{
    // =================================================================
    //  PROGRAMME PRINCIPAL (menu console)
    // =================================================================
    public static class Program
    {
        public static void Main(string[] args)
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }

            CentreSportif centre = new CentreSportif("Centre sportif Raquette Plus", "123, rue des Sports, Montréal (QC)");
            InitialiserDonnees(centre);

            Console.WriteLine("Bienvenue au " + centre.Nom);
            Console.WriteLine(centre.Adresse);

            bool continuer = true;
            while (continuer)
            {
                AfficherMenu();
                string choix = Lire("Votre choix");
                Console.WriteLine();
                try
                {
                    switch (choix)
                    {
                        case "1": AjouterMembre(centre); break;
                        case "2": AfficherMembres(centre); break;
                        case "3": AjouterTerrain(centre); break;
                        case "4": AfficherTerrains(centre); break;
                        case "5": ReserverOccasionnelle(centre); break;
                        case "6": ReserverPermanente(centre); break;
                        case "7": ListerReservations(centre); break;
                        case "8": RechercherReservation(centre); break;
                        case "9": ModifierReservation(centre); break;
                        case "10": AnnulerReservation(centre); break;
                        case "11": VerifierDisponibilite(centre); break;
                        case "12": PayerReservation(centre); break;
                        case "13": TransfererReservation(centre); break;
                        case "14": SousLouerReservation(centre); break;
                        case "15": GenererRapport(centre); break;
                        case "0": continuer = false; break;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur : " + ex.Message);
                }
                Console.WriteLine();
            }
            Console.WriteLine("Au revoir !");
        }

       
        private static void InitialiserDonnees(CentreSportif centre)
        {
            for (int i = 1; i <= 15; i++)
                centre.ajouterTerrain(new Terrain("T" + i, Sport.Tennis, i <= 5 ? TypeTerrain.Exterieur : TypeTerrain.Interieur, 20.0));
            for (int i = 1; i <= 5; i++)
                centre.ajouterTerrain(new Terrain("S" + i, Sport.Squash, TypeTerrain.Interieur, 16.0));
            for (int i = 1; i <= 8; i++)
                centre.ajouterTerrain(new Terrain("R" + i, Sport.RacketBall, TypeTerrain.Interieur, 14.0));

         
            var blocsSemaine = new[] { Tuple.Create("07:00-09:00", 1.0), Tuple.Create("09:00-17:00", 0.7), Tuple.Create("17:00-23:00", 1.3) };
            var blocsFinSemaine = new[] { Tuple.Create("07:00-12:00", 1.1), Tuple.Create("12:00-18:00", 1.2), Tuple.Create("18:00-23:00", 1.4) };
            foreach (Sport sport in Enum.GetValues(typeof(Sport)))
            {
                double prixBase = sport == Sport.Tennis ? 20.0 : sport == Sport.Squash ? 16.0 : 14.0;
                foreach (DayOfWeek jour in Enum.GetValues(typeof(DayOfWeek)))
                {
                    bool finSemaine = jour == DayOfWeek.Saturday || jour == DayOfWeek.Sunday;
                    foreach (var bloc in finSemaine ? blocsFinSemaine : blocsSemaine)
                        centre.ajouterTarification(new Tarification(sport, bloc.Item1, jour, Math.Round(prixBase * bloc.Item2, 2)));
                }
            }

            MembreRegulier a = new MembreRegulier("Diop", "Aminata", "aminata.diop@exemple.ca", "514-555-0101");
            centre.ajouterMembre(a);
            a.ajouterCarte(new CarteMembre(TypeCarte.Annuel, DateTime.Today, new List<Sport> { Sport.Tennis, Sport.Squash, Sport.RacketBall }));

            MembreRegulier b = new MembreRegulier("Ndiaye", "Moussa", "moussa.ndiaye@exemple.ca", "514-555-0102");
            centre.ajouterMembre(b);
            b.ajouterCarte(new CarteMembre(TypeCarte.Annuel, DateTime.Today, Sport.Tennis));

            centre.ajouterMembre(new MembreVisiteur("Fall", "Awa", "awa.fall@exemple.ca", "514-555-0103", DateTime.Today));
            centre.ajouterMembre(new Instructeur("Sow", "Ibrahima", "ibrahima.sow@exemple.ca", "514-555-0104", "Tennis"));
            centre.ajouterMembre(new Clientele("Ba", "Fatou", "fatou.ba@exemple.ca", "514-555-0105", "Comptoir d'accueil"));
        }

        private static void AfficherMenu()
        {
            Console.WriteLine("==================== SGR - MENU ====================");
            Console.WriteLine(" 1. Ajouter un membre");
            Console.WriteLine(" 2. Afficher les membres");
            Console.WriteLine(" 3. Ajouter un terrain");
            Console.WriteLine(" 4. Afficher les terrains");
            Console.WriteLine(" 5. Réserver un terrain (occasionnelle)");
            Console.WriteLine(" 6. Réserver un terrain (permanente)");
            Console.WriteLine(" 7. Lister les réservations");
            Console.WriteLine(" 8. Rechercher une réservation");
            Console.WriteLine(" 9. Modifier une réservation");
            Console.WriteLine("10. Annuler une réservation");
            Console.WriteLine("11. Vérifier la disponibilité / grille d'occupation");
            Console.WriteLine("12. Payer un solde");
            Console.WriteLine("13. Transférer une réservation permanente");
            Console.WriteLine("14. Sous-louer une réservation permanente");
            Console.WriteLine("15. Générer un rapport d'occupation");
            Console.WriteLine(" 0. Quitter");
            Console.WriteLine("====================================================");
        }

        // ---------------------------------------------------------------
        //  Actions du menu
        // ---------------------------------------------------------------
        private static void AjouterMembre(CentreSportif centre)
        {
            Console.WriteLine("Type de membre : 1. Régulier  2. Visiteur  3. Instructeur  4. Préposé à la clientèle");
            int type = LireEntier("Type", 1, 4);
            string nom = LireTexte("Nom");
            string prenom = LireTexte("Prénom");
            string courriel = Lire("Courriel (facultatif)");
            string tel = Lire("Téléphone (facultatif)");

            Membre m;
            CarteMembre carte = null;
            switch (type)
            {
                case 1:
                    m = new MembreRegulier(nom, prenom, courriel, tel);
                    TypeCarte tc = LireEnum<TypeCarte>("Type de carte :");
                    carte = new CarteMembre(tc, DateTime.Today, LireSports());
                    break;
                case 2:
                    m = new MembreVisiteur(nom, prenom, courriel, tel,
                                           LireDate("Date de visite (AAAA-MM-JJ, Entrée = aujourd'hui)"));
                    break;
                case 3:
                    m = new Instructeur(nom, prenom, courriel, tel, LireTexte("Spécialité"));
                    break;
                default:
                    m = new Clientele(nom, prenom, courriel, tel, LireTexte("Poste"));
                    break;
            }

            if (!centre.ajouterMembre(m))
            {
                Console.WriteLine("Le système de gestion des membres a refusé l'ajout.");
                return;
            }
            if (carte != null) m.ajouterCarte(carte);

            Console.WriteLine("Membre ajouté :");
            m.afficher();
        }

        private static void AfficherMembres(CentreSportif centre)
        {
            List<Membre> liste = centre.afficherMembres();
            if (liste.Count == 0) { Console.WriteLine("Aucun membre."); return; }
            foreach (Membre m in liste) m.afficher();
            Console.WriteLine(liste.Count + " membre(s).");
        }

        private static void AjouterTerrain(CentreSportif centre)
        {
            Sport sport = LireEnum<Sport>("Sport du terrain :");
            string numero = LireTexte("Numéro du terrain (ex. T16)");
            TypeTerrain type = LireEnum<TypeTerrain>("Type de terrain :");
            double prix = LireDouble("Prix de base à l'heure ($)");
            Terrain t = new Terrain(numero, sport, type, prix);
            centre.ajouterTerrain(t);
            Console.WriteLine("Terrain ajouté :");
            t.afficher();
        }

        private static void AfficherTerrains(CentreSportif centre)
        {
            int choix = LireEntier("Sport (0 = tous, 1 = Tennis, 2 = Squash, 3 = RacketBall)", 0, 3);
            List<Terrain> liste = choix == 0 ? centre.afficherTerrains() : centre.afficherTerrains((Sport)(choix - 1));
            foreach (Terrain t in liste)
            {
                t.mettreAJourStatut();
                t.afficher();
            }
            Console.WriteLine(liste.Count + " terrain(s).");
        }

        private static void ReserverOccasionnelle(CentreSportif centre)
        {
            Membre membre = ChoisirMembre(centre, "N° du membre");
            Terrain terrain = ChoisirTerrain(centre);
            DateTime date = LireDate("Date (AAAA-MM-JJ, Entrée = aujourd'hui)");
            TimeSpan heure = LireHeure("Heure de début (HH:mm)");
            CanalReservation canal = LireEnum<CanalReservation>("Canal de réservation :");

            Reservation r = membre.reserverTerrain(terrain, date, heure, canal);
            Console.WriteLine("Réservation enregistrée :");
            r.afficher();

            ReservationOccasionnelle occ = r as ReservationOccasionnelle;
            if (occ != null && occ.PaiementImmediatRequis)
            {
                TypePaiement mode = LireEnum<TypePaiement>("Paiement immédiat - mode :");
                r.payer(r.Montant, mode);
                Console.WriteLine("Paiement reçu : " + Util.Argent(r.Montant));
            }
            else
            {
                Console.WriteLine("Paiement à effectuer à l'arrivée du joueur (réservation par téléphone).");
            }
        }

        private static void ReserverPermanente(CentreSportif centre)
        {
            Membre membre = ChoisirMembre(centre, "N° du membre (chef d'équipe)");
            Terrain terrain = ChoisirTerrain(centre);
            DayOfWeek jour = LireEnum<DayOfWeek>("Jour fixe :");
            TimeSpan heureDebut = LireHeure("Heure de début (HH:mm)");
            TimeSpan heureFin = LireHeure("Heure de fin (HH:mm)");
            int saisonCourante = PeriodeSaison.saisonCourante().DateDebut.Year;
            int annee = LireEntier("Année de début de saison (1er septembre) [Entrée = " + saisonCourante + "]", 2000, 2100, saisonCourante);
            int joueurs = LireEntier("Nombre de joueurs (2 à 4)", 2, 4);

            PeriodeSaison periode = new PeriodeSaison(annee);
            Reservation r = membre.reserverTerrain(terrain, jour, heureDebut, heureFin, periode, joueurs);

            Console.WriteLine("Réservation permanente enregistrée :");
            r.afficher();

            ReservationPermanente perm = (ReservationPermanente)r;
            TypePaiement mode = LireEnum<TypePaiement>("Acompte de 50 % à payer maintenant - mode :");
            perm.payerAcompte(mode);
            Console.WriteLine("Acompte reçu. Le solde devra être payé avant le début de la saison (menu 12).");
            r.afficher();
        }

        private static void ListerReservations(CentreSportif centre)
        {
            List<Reservation> liste = centre.listerReservations();
            if (liste.Count == 0) { Console.WriteLine("Aucune réservation."); return; }
            foreach (Reservation r in liste) r.afficher();
            Console.WriteLine(liste.Count + " réservation(s).");
        }

        private static void RechercherReservation(CentreSportif centre)
        {
            int mode = LireEntier("Rechercher par : 1. Numéro  2. Nom du membre", 1, 2);
            if (mode == 1)
            {
                int id = LireEntier("Numéro de réservation", 1, int.MaxValue);
                Reservation r = centre.rechercherReservation(id);
                if (r == null) Console.WriteLine("Réservation introuvable.");
                else r.afficher();
            }
            else
            {
                string nom = LireTexte("Nom (ou partie du nom) du membre");
                List<Reservation> resultats = centre.rechercherReservation(nom);
                if (resultats.Count == 0) Console.WriteLine("Aucune réservation trouvée.");
                foreach (Reservation r in resultats) r.afficher();
            }
        }

        private static void ModifierReservation(CentreSportif centre)
        {
            Reservation r = ChoisirReservation(centre, "Numéro de la réservation à modifier");
            r.afficher();
            if (r is ReservationPermanente)
                Console.WriteLine("Réservation permanente : le jour de la semaine et l'heure de la nouvelle date deviennent le nouveau créneau fixe.");
            DateTime date = LireDate("Nouvelle date (AAAA-MM-JJ)");
            TimeSpan heure = LireHeure("Nouvelle heure de début (HH:mm)");

            r.Membre.modifierReservation(r, date.Date + heure);
            Console.WriteLine("Réservation modifiée :");
            r.afficher();
        }

        private static void AnnulerReservation(CentreSportif centre)
        {
            Reservation r = ChoisirReservation(centre, "Numéro de la réservation à annuler");
            if (r.Membre.annulerReservation(r.IdReservation))
            {
                Console.WriteLine("Réservation annulée (les paiements reçus sont remboursés).");
                r.afficher();
            }
            else
            {
                Console.WriteLine(r.MotifRefus);
            }
        }

        private static void VerifierDisponibilite(CentreSportif centre)
        {
            int choix = LireEntier("1. Grille d'occupation d'un sport  2. Vérifier un terrain à une heure précise", 1, 2);
            if (choix == 1)
            {
                Sport sport = LireEnum<Sport>("Sport :");
                DateTime date = LireDate("Date (AAAA-MM-JJ, Entrée = aujourd'hui)");
                Console.WriteLine(centre.genererGrille(sport, date));
            }
            else
            {
                Terrain terrain = ChoisirTerrain(centre);
                DateTime date = LireDate("Date (AAAA-MM-JJ, Entrée = aujourd'hui)");
                TimeSpan heure = LireHeure("Heure (HH:mm)");
                bool dispo = centre.verifierDisponibilite(terrain, date.Date + heure);
                Console.WriteLine("Le terrain " + terrain.Sport + " " + terrain.Numero + " est "
                                  + (dispo ? "DISPONIBLE" : "OCCUPÉ") + " le " + Util.Date(date) + " à " + Util.Heure(heure) + ".");
            }
        }

        private static void PayerReservation(CentreSportif centre)
        {
            Reservation r = ChoisirReservation(centre, "Numéro de la réservation");
            double solde = r.soldeDu();
            if (solde <= 0.004)
            {
                Console.WriteLine("Aucun solde à payer pour cette réservation.");
                return;
            }
            Console.WriteLine("Solde dû : " + Util.Argent(solde));
            TypePaiement mode = LireEnum<TypePaiement>("Mode de paiement :");

            ReservationPermanente perm = r as ReservationPermanente;
            if (perm != null) perm.payerSolde(mode);
            else r.payer(solde, mode);

            Console.WriteLine("Paiement enregistré.");
            r.afficher();
        }

        private static void TransfererReservation(CentreSportif centre)
        {
            ReservationPermanente perm = ChoisirReservation(centre, "Numéro de la réservation permanente") as ReservationPermanente;
            if (perm == null) { Console.WriteLine("Cette réservation n'est pas permanente."); return; }

            Membre nouveau = ChoisirMembre(centre, "N° du nouveau titulaire");
            if (perm.transferer(nouveau))
            {
                Console.WriteLine("Réservation transférée à " + nouveau.NomComplet + " :");
                perm.afficher();
            }
            else
            {
                Console.WriteLine("Transfert impossible (le nouveau titulaire doit être un membre régulier avec une carte valide pour ce sport).");
            }
        }

        private static void SousLouerReservation(CentreSportif centre)
        {
            ReservationPermanente perm = ChoisirReservation(centre, "Numéro de la réservation permanente") as ReservationPermanente;
            if (perm == null) { Console.WriteLine("Cette réservation n'est pas permanente."); return; }

            DateTime date = LireDate("Date de l'occurrence à sous-louer (AAAA-MM-JJ)");
            TimeSpan heure = LireHeure("Heure de la sous-location (HH:mm)");
            Membre locataire = ChoisirMembre(centre, "N° du locataire");
            if (!locataire.peutReserver(perm.Terrain.Sport))
            {
                Console.WriteLine("Ce membre n'a pas accès à ce sport : sous-location impossible.");
                return;
            }

            SousLocation sl = perm.sousLouer(date, heure);
            sl.Locataire = locataire;
            sl.confirmer();
            Console.WriteLine("Sous-location confirmée.");

            if (LireOuiNon("Attribuer maintenant une heure de remplacement au titulaire ?"))
            {
                DateTime dateR = LireDate("Date de remplacement (AAAA-MM-JJ)");
                TimeSpan heureR = LireHeure("Heure de remplacement (HH:mm)");
                sl.attribuerHeureR(dateR, heureR);
                Console.WriteLine("Heure de remplacement attribuée.");
            }
            perm.afficher();
        }

        private static void GenererRapport(CentreSportif centre)
        {
            TypeRapport type = LireEnum<TypeRapport>("Type de rapport :");
            DateTime reference = LireDate("Date de référence (AAAA-MM-JJ, Entrée = aujourd'hui)");
            RapportOccupation rapport = centre.genererRapport(type, reference);
            rapport.afficher();
            if (LireOuiNon("Exporter ce rapport dans un fichier texte ?")) rapport.exporter();
        }

        // ---------------------------------------------------------------
        //  Sélection d'objets
        // ---------------------------------------------------------------
        private static Membre ChoisirMembre(CentreSportif centre, string invite)
        {
            int id = LireEntier(invite, 1, int.MaxValue);
            Membre m = centre.rechercherMembre(id);
            if (m == null) throw new InvalidOperationException("Membre introuvable.");
            return m;
        }

        private static Terrain ChoisirTerrain(CentreSportif centre)
        {
            int id = LireEntier("N° du terrain (voir menu 4)", 1, int.MaxValue);
            Terrain t = centre.rechercherTerrain(id);
            if (t == null) throw new InvalidOperationException("Terrain introuvable.");
            return t;
        }

        private static Reservation ChoisirReservation(CentreSportif centre, string invite)
        {
            int id = LireEntier(invite, 1, int.MaxValue);
            Reservation r = centre.rechercherReservation(id);
            if (r == null) throw new InvalidOperationException("Réservation introuvable.");
            return r;
        }

        // ---------------------------------------------------------------
        //  Saisies console
        // ---------------------------------------------------------------
        private static string Lire(string invite)
        {
            Console.Write(invite + " : ");
            string s = Console.ReadLine();
            if (s == null) Environment.Exit(0);
            return s.Trim();
        }

        private static string LireTexte(string invite)
        {
            while (true)
            {
                string s = Lire(invite);
                if (s.Length > 0) return s;
                Console.WriteLine("Ce champ est obligatoire.");
            }
        }

        private static int LireEntier(string invite, int min, int max, int? defaut = null)
        {
            while (true)
            {
                string s = Lire(invite);
                if (s.Length == 0 && defaut.HasValue) return defaut.Value;
                int n;
                if (int.TryParse(s, out n) && n >= min && n <= max) return n;
                Console.WriteLine("Veuillez entrer un nombre entier valide.");
            }
        }

        private static double LireDouble(string invite)
        {
            while (true)
            {
                string s = Lire(invite).Replace(',', '.');
                double v;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v >= 0) return v;
                Console.WriteLine("Veuillez entrer un montant valide.");
            }
        }

        private static DateTime LireDate(string invite)
        {
            while (true)
            {
                string s = Lire(invite);
                if (s.Length == 0) return DateTime.Today;
                DateTime d;
                if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                    return d;
                Console.WriteLine("Format attendu : AAAA-MM-JJ (ex. 2026-10-14).");
            }
        }

        private static TimeSpan LireHeure(string invite)
        {
            while (true)
            {
                string s = Lire(invite);
                TimeSpan t;
                if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out t) && t >= TimeSpan.Zero && t < TimeSpan.FromHours(24))
                    return t;
                Console.WriteLine("Format attendu : HH:mm (ex. 18:00).");
            }
        }

        private static bool LireOuiNon(string invite)
        {
            string s = Lire(invite + " (o/n)").ToLowerInvariant();
            return s == "o" || s == "oui";
        }

        private static T LireEnum<T>(string titre) where T : struct
        {
            T[] valeurs = (T[])Enum.GetValues(typeof(T));
            Console.WriteLine(titre);
            for (int i = 0; i < valeurs.Length; i++)
                Console.WriteLine("  " + (i + 1) + ". " + valeurs[i]);
            int choix = LireEntier("Choix", 1, valeurs.Length);
            return valeurs[choix - 1];
        }

        private static List<Sport> LireSports()
        {
            while (true)
            {
                Console.WriteLine("Sports couverts par la carte : 1 = Tennis, 2 = Squash, 3 = RacketBall (séparés par des virgules, ex. 1,3)");
                string[] parties = Lire("Sports").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<Sport> resultat = new List<Sport>();
                bool ok = parties.Length > 0;
                foreach (string p in parties)
                {
                    int n;
                    if (int.TryParse(p, out n) && n >= 1 && n <= 3)
                    {
                        Sport s = (Sport)(n - 1);
                        if (!resultat.Contains(s)) resultat.Add(s);
                    }
                    else ok = false;
                }
                if (ok) return resultat;
                Console.WriteLine("Saisie invalide.");
            }
        }
    }
}