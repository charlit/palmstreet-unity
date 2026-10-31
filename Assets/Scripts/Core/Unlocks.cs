namespace PalmStreet.Core
{
    /// <summary>
    /// Vêtements à débloquer (FR-027) : chaque coiffure, coupe et couleur de la palette a un prix en points
    /// cumulés (la somme des scores de toutes les parties). Prix 0 : disponible dès le début.
    /// Les peaux et les couleurs de cheveux sont toujours libres.
    /// </summary>
    public static class Unlocks
    {
        // même ordre que Wardrobe.Palette : rouge, orange, jaune, vert, turquoise, bleu, bleu nuit,
        // violet, rose, crème, noir, gris, jean, beige
        public static readonly int[] Colors = { 0, 1000, 2500, 5000, 8000, 1500, 0, 12000, 20000, 0, 0, 500, 0, 0 };
        public static readonly int[] Heads = { 0, 3000, 0, 10000 };   // casquette, bonnet, boucles, bandeau
        public static readonly int[] Legs = { 0, 6000 };              // pantalon, short

        public static bool Open(int cost, int total) => total >= cost;

        /// <summary>Nombre d'éléments débloqués avec ce total (pour annoncer les nouveautés en fin de partie).</summary>
        public static int CountOpen(int total)
        {
            int n = 0;
            foreach (var c in Colors) if (c <= total) n++;
            foreach (var c in Heads) if (c <= total) n++;
            foreach (var c in Legs) if (c <= total) n++;
            return n;
        }
    }
}
