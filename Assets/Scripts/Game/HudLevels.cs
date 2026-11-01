using PalmStreet.Core;
using UnityEngine;
using UnityEngine.UI;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Interface du mode histoire (spec 002) : carte de la côte avec les 12 niveaux et le petit skateur, écran
    /// NIVEAU RÉUSSI, barre de progression vers l'arrivée et bulle d'aide des nouveaux obstacles.
    /// </summary>
    public sealed partial class Hud
    {
        GameObject map, cleared;
        RectTransform mapViewport;
        MapArt mapArt;
        readonly RectTransform[] nodes = new RectTransform[12];
        readonly Image[] nodeFills = new Image[12];
        readonly Text[] nodeBests = new Text[12];
        Image clearedChip;
        Text clearedTitle, clearedCity, clearedScore, clearedBest, clearedGift, clearedTrick, clearedHow, clearedChallenge, nextLabel, challengeHud;
        RectTransform storyBar, storyFill, bubble;
        Text bubbleText;
        float bubbleTime = 99;

        // ---------- carte (FR-002, FR-003, FR-022) ----------

        void BuildMap(HudActions a)
        {
            map = Group("Carte");
            var t = map.transform;
            Label(t, "LE PAYS BASQUE", 26, Cream, new Vector2(0, -34), new Vector2(0.5f, 1));

            mapViewport = Rect("Carte du Pays basque", t);
            mapViewport.offsetMin = new Vector2(8, 70); mapViewport.offsetMax = new Vector2(-8, -64);
            mapViewport.gameObject.AddComponent<CanvasRenderer>();   // créé par code : RequireComponent ne l'ajoute pas ici
            mapArt = mapViewport.gameObject.AddComponent<MapArt>();
            mapArt.raycastTarget = false;
            Label(mapViewport, "OCÉAN\nATLANTIQUE", 12, Hex("#FFF4E0", 0.5f), Vector2.zero, new Vector2(0.13f, 0.62f)).rectTransform.sizeDelta = new Vector2(120, 40);
            Label(mapViewport, "ESPAGNE", 12, Hex("#FFF4E0", 0.5f), Vector2.zero, new Vector2(0.2f, 0.06f));
            Label(mapViewport, "LA RHUNE", 10, Hex("#FFF4E0", 0.6f), new Vector2(0, -16), new Vector2(0.40f, 0.17f));

            for (int i = 0; i < 12; i++)
            {
                var def = Levels.All[i];
                int n = def.Number;
                bool boss = def.Boss != BossKind.None;
                float size = boss ? 50 : 42;
                var at = MapArt.Towns[i];
                var node = Button(mapViewport, "Niveau " + n, at, Vector2.zero, new Vector2(size, size), Cream, () => ShowPreview(n));
                node.GetComponent<Image>().pixelsPerUnitMultiplier = 0.6f;   // pastille ronde
                var fill = Rect("Pastille", node.transform);
                fill.offsetMin = new Vector2(3, 3); fill.offsetMax = new Vector2(-3, -3);
                nodeFills[i] = Img(fill, Orange);
                nodeFills[i].pixelsPerUnitMultiplier = 0.6f;
                nodeFills[i].raycastTarget = false;
                Label(node.transform, n.ToString(), boss ? 22 : 19, Night, Vector2.zero);
                var name = Label(mapViewport, def.City, 11, Cream, new Vector2(0, -size / 2 - 8), at);
                name.rectTransform.sizeDelta = new Vector2(160, 16);
                nodeBests[i] = Label(mapViewport, "", 10, Yellow, new Vector2(0, -size / 2 - 20), at);
                if (boss) Label(mapViewport, "BOSS", 11, Pink, new Vector2(0, size / 2 + 7), at);
                nodes[i] = node.GetComponent<RectTransform>();
            }

            var back = Button(t, "Retour carte", new Vector2(0.5f, 0), new Vector2(0, 38), new Vector2(200, 44), Orange, a.Home);
            Label(back.transform, "RETOUR", 20, Night, Vector2.zero);
            BuildStoryHud();
        }

        public void ShowMap(Progress p)
        {
            Show(map);
            int next = p.NextLevel;
            for (int i = 0; i < 12; i++)
            {
                int n = i + 1;
                bool open = p.IsOpen(n), done = p.IsCleared(n);
                nodeFills[i].color = done ? Hex("#3BB273") : n == next ? Orange : open ? Yellow : Hex("#5C6670");
                nodes[i].GetComponent<UnityEngine.UI.Button>().interactable = open;
                nodeBests[i].text = done ? "BEST " + p.Best[i] : open ? "" : "VERROUILLÉ";
            }
            int reached = 0;
            while (reached < 12 && p.IsOpen(reached + 1)) reached++;
            mapArt.Open = reached;
            mapArt.Stars = (bool[])p.Stars.Clone();
            mapArt.SetVerticesDirty();
        }

        public bool MapVisible => map.activeSelf;

        /// <summary>Position monde d'une pastille (le petit skateur s'y pose) et si elle est dans la partie visible.</summary>
        public Vector3 NodeWorld(int n, out bool visible, out float scale)
        {
            var node = nodes[n - 1];
            var w = node.TransformPoint(new Vector3(-34, -12, 0));   // à côté de la pastille, pas dessus
            var local = mapViewport.InverseTransformPoint(w);
            visible = mapViewport.rect.Contains(new Vector2(local.x, local.y));
            scale = node.lossyScale.x / 0.01f * 0.5f;
            return w;
        }

        // ---------- niveau réussi (FR-008, FR-015, FR-026) ----------

        void BuildCleared(HudActions a)
        {
            cleared = Panel("Niveau réussi", 0.6f);
            cleared.GetComponent<Image>().raycastTarget = true;
            var t = cleared.transform;
            clearedTitle = Label(t, "NIVEAU RÉUSSI !", 44, Yellow, new Vector2(0, 210));
            clearedCity = Label(t, "", 20, Cream, new Vector2(0, 170));
            Label(t, "SCORE", 14, Cream, new Vector2(0, 132));
            clearedScore = Label(t, "", 44, Cream, new Vector2(0, 102));
            clearedBest = Label(t, "", 16, Yellow, new Vector2(0, 66));
            clearedGift = Label(t, "", 20, Pink, new Vector2(14, 30));
            var chip = Rect("Objet gagné", t);
            chip.anchorMin = chip.anchorMax = new Vector2(0.5f, 0.5f);
            chip.sizeDelta = new Vector2(30, 30);
            clearedChip = Img(chip, Cream);
            clearedChip.pixelsPerUnitMultiplier = 0.6f;
            clearedChip.raycastTarget = false;
            clearedTrick = Label(t, "", 20, Hex("#35F2FF"), new Vector2(0, -4));
            clearedHow = Label(t, "", 14, Cream, new Vector2(0, -28));
            clearedChallenge = Label(t, "", 15, Yellow, new Vector2(0, -52));
            var nb = Button(t, "Suivant", new Vector2(0.5f, 0.5f), new Vector2(0, -96), new Vector2(250, 54), Orange, a.Next);
            nextLabel = Label(nb.transform, "NIVEAU SUIVANT", 22, Night, Vector2.zero);
            var mb = Button(t, "Carte", new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(250, 42), new Color(Night.r, Night.g, Night.b, 0.85f), a.Map);
            Label(mb.transform, "CARTE", 18, Cream, Vector2.zero);
        }

        public void ShowCleared(LevelDef def, int score, int best, string trick, Item gift, string challenge)
        {
            clearedChallenge.text = challenge;
            cleared.SetActive(true);
            settingsButton.gameObject.SetActive(false);
            bool last = def.Number == Levels.Count;
            clearedTitle.text = last ? "TU ES UN VRAI PST !" : "NIVEAU RÉUSSI !";
            clearedCity.text = $"NIVEAU {def.Number} · {def.City}" + (last ? " · FIN DE L'AVENTURE" : "");
            clearedScore.text = score.ToString();
            clearedBest.text = "MEILLEUR : " + best;
            clearedGift.text = gift != null ? "NOUVEAU : " + gift.Name : "";
            clearedChip.gameObject.SetActive(gift != null);
            if (gift != null)
            {
                clearedChip.color = Wardrobe.ItemColor(gift.Id);
                clearedChip.rectTransform.anchoredPosition = new Vector2(-clearedGift.preferredWidth / 2 - 10, 30);
            }
            int k = System.Array.IndexOf(Tuning.TrickNames, trick);
            clearedTrick.text = trick != null ? "NOUVELLE FIGURE : " + trick : "";
            clearedHow.text = trick != null ? "En l'air, glisse le doigt " + Tuning.SwipeWords[Tuning.TrickSwipe[k]] : "";
            nextLabel.text = last ? "MODE INFINI" : "NIVEAU SUIVANT";
        }

        // ---------- profil : figures connues et à débloquer (FR-017) ----------

        void BuildTricksPage(Transform section)
        {
            var back = Rect("Fond", section);
            back.offsetMin = new Vector2(8, 8); back.offsetMax = new Vector2(-8, -126);
            Img(back, new Color(Night.r, Night.g, Night.b, 0.88f)).raycastTarget = false;
            var names = new System.Collections.Generic.List<string> { "OLLIE", "GRIND", "180 · FAKIE" };
            names.AddRange(Tuning.TrickNames);
            for (int i = 0; i < names.Count; i++)
            {
                int trick = i - 3;   // −3, −2, −1 : ollie, grind et 180, toujours connus
                var row = Rect("Figure " + names[i], section);
                row.anchorMin = row.anchorMax = new Vector2(0.5f, 1);
                row.anchoredPosition = new Vector2(0, -150 - i * 40);
                row.sizeDelta = new Vector2(340, 35);
                var bg = Img(row, Hex("#3BB273"));
                bg.raycastTarget = false;
                Label(row, names[i], 17, Night, new Vector2(-82, 0));
                var state = Label(row, "", 12, Night, new Vector2(88, 0));
                refresh.Add(() =>
                {
                    var p = progress();
                    var def = trick < 0 ? null : System.Array.Find(Levels.All, l => l.Unlocks == trick);
                    bool known = trick < 0 || p.Tricks.Contains(trick);
                    bg.color = known ? Hex("#3BB273") : Hex("#5C6670");
                    state.text = !known ? $"NIVEAU {def.Number} · {def.City}"
                        : trick == -3 ? "TOUCHE" : trick == -2 ? "RAIL · GLISSE : 8 VARIANTES" : trick == -1 ? "TOUCHE À 2 DOIGTS · FIGURES ×1,5"
                        : "GLISSE " + Tuning.SwipeWords[Tuning.TrickSwipe[trick]].ToUpper();
                    state.color = known ? Night : Cream;
                });
            }
        }

        // ---------- en partie : progression et bulle d'aide (FR-005, FR-007) ----------

        void BuildStoryHud()
        {
            var t = game.transform;
            storyBar = Rect("Vers l'arrivée", t);
            storyBar.anchorMin = storyBar.anchorMax = new Vector2(0.5f, 1);
            storyBar.anchoredPosition = new Vector2(0, -112);
            storyBar.sizeDelta = new Vector2(200, 10);
            Img(storyBar, new Color(Night.r, Night.g, Night.b, 0.6f)).raycastTarget = false;
            storyFill = Rect("Parcouru", storyBar);
            storyFill.offsetMin = new Vector2(2, 2); storyFill.offsetMax = new Vector2(-2, -2);
            Img(storyFill, Yellow).raycastTarget = false;

            bubble = Rect("Bulle d'aide", t);
            bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
            bubble.anchoredPosition = new Vector2(0, 10);
            bubble.sizeDelta = new Vector2(320, 64);
            Img(bubble, new Color(Night.r, Night.g, Night.b, 0.9f)).raycastTarget = false;
            challengeHud = Label(t, "", 13, Cream, new Vector2(0, -128), new Vector2(0.5f, 1));
            bubbleText = Label(bubble, "", 16, Cream, Vector2.zero);
            bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bubbleText.rectTransform.sizeDelta = new Vector2(300, 60);
            bubble.gameObject.SetActive(false);
        }

        public void Tip(string text)
        {
            bubbleText.text = text;
            bubbleTime = 0;
        }

        void TickStory(Run run, float dt)
        {
            bool story = run.Mode == GameMode.Story && run.Def != null;
            storyBar.gameObject.SetActive(story && run.Boss == null);
            if (story) storyFill.anchorMax = new Vector2(Mathf.Clamp01(run.Skater.X / run.Def.Finish), 1);
            challengeHud.gameObject.SetActive(story);
            if (story)
            {
                bool done = run.ChallengeDone(run.Def);
                challengeHud.text = done ? "DÉFI RÉUSSI : " + run.Def.ChallengeText : $"DÉFI : {run.ChallengeValue(run.Def)} / {run.Def.ChallengeText}";
                challengeHud.color = done ? Yellow : Cream;
            }
            bubbleTime += dt;
            bubble.gameObject.SetActive(bubbleTime < 3.5f);
        }
    }
}
