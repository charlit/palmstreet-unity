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
            mapArt.Stars = new int[12];
            for (int i = 0; i < 12; i++) mapArt.Stars[i] = p.StarCount(i + 1);
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

        /// <summary>Figure montrée par le skateur du profil (index de Tuning.TrickNames, 8 = 180, ou DemoOllie, DemoGrind, DemoManual).</summary>
        public int DemoTrick { get; private set; }
        public const int DemoOllie = 100, DemoGrind = 101, DemoManual = 102, DemoNoseManual = 103;
        RectTransform trickStage, demoRail;
        Text demoName, demoHow;

        void BuildTricksPage(Transform section)
        {
            var back = Rect("Fond", section);
            back.offsetMin = new Vector2(8, 8); back.offsetMax = new Vector2(-8, -126);
            Img(back, new Color(Night.r, Night.g, Night.b, 0.88f)).raycastTarget = false;

            // à gauche : le skateur fait la figure choisie, en boucle
            trickStage = Rect("Démonstration", section);
            trickStage.anchorMin = trickStage.anchorMax = new Vector2(0.5f, 1);
            trickStage.anchoredPosition = new Vector2(-104, -262);
            trickStage.sizeDelta = new Vector2(150, 230);
            Img(trickStage, new Color(1, 1, 1, 0.07f)).raycastTarget = false;
            demoRail = Rect("Rail", trickStage);
            demoRail.anchorMin = demoRail.anchorMax = new Vector2(0.5f, 0.5f);
            demoRail.anchoredPosition = new Vector2(0, -73);
            demoRail.sizeDelta = new Vector2(150, 5);
            Img(demoRail, Hex("#C9CED6")).raycastTarget = false;
            demoName = Label(section, "", 18, Yellow, new Vector2(-104, -396), new Vector2(0.5f, 1));
            demoName.rectTransform.sizeDelta = new Vector2(150, 24);
            demoHow = Label(section, "", 12, Cream, new Vector2(-104, -430), new Vector2(0.5f, 1));
            demoHow.rectTransform.sizeDelta = new Vector2(150, 44);
            demoHow.horizontalOverflow = HorizontalWrapMode.Wrap;
            Sharp(demoHow);

            // à droite : la liste ; toucher une figure la montre
            var entries = new System.Collections.Generic.List<(string name, int id)>
                { ("OLLIE", DemoOllie), ("GRIND", DemoGrind), ("MANUAL", DemoManual), ("NOSE MANUAL", DemoNoseManual), ("180 · FAKIE", Tuning.HalfTurn) };
            for (int k = 0; k < Tuning.TrickNames.Length; k++) entries.Add((Tuning.TrickNames[k], k));
            var equippedCount = Label(section, "", 13, Yellow, new Vector2(76, -142), new Vector2(0.5f, 1));
            equippedCount.rectTransform.sizeDelta = new Vector2(200, 18);
            Sharp(equippedCount);
            refresh.Add(() => equippedCount.text = $"ÉQUIPÉES : {progress().ActiveTricks.Count}/{Progress.MaxEquipped}  (+ / -)");
            for (int i = 0; i < entries.Count; i++)
            {
                var (name, id) = entries[i];
                bool air = id < Tuning.TrickNames.Length;   // seules les figures en l'air s'équipent
                var row = Button(section, "Figure " + name, new Vector2(0.5f, 1), new Vector2(air ? 56 : 76, -168 - i * 36), new Vector2(air ? 156 : 196, 33), Hex("#3BB273"), () => SelectDemo(id));
                var bg = row.GetComponent<Image>();
                var title = Label(row.transform, name, 15, Night, new Vector2(0, 7));
                title.rectTransform.sizeDelta = new Vector2(190, 18);
                var state = Label(row.transform, "", 11, Night, new Vector2(0, -8));
                state.rectTransform.sizeDelta = new Vector2(190, 14);
                Sharp(title); Sharp(state);
                Image eqBg = null; Text eqText = null;
                if (air)
                {
                    var eq = Button(section, "Équiper " + name, new Vector2(0.5f, 1), new Vector2(156, -168 - i * 36), new Vector2(36, 33), Yellow, () =>
                    {
                        var p = progress();
                        if (!p.ToggleEquip(id)) Toast(p.Tricks.Contains(id) ? "4 figures au plus : retire-en une avec -" : "Pas encore débloquée");
                        outfitChanged?.Invoke();   // sauvegarde
                        Refresh();
                    });
                    eqBg = eq.GetComponent<Image>();
                    eqText = Label(eq.transform, "+", 24, Night, Vector2.zero);
                    Sharp(eqText);
                }
                refresh.Add(() =>
                {
                    var p = progress();
                    bool known = !air || p.Tricks.Contains(id), on = air && p.ActiveTricks.Contains(id);
                    bg.color = DemoTrick == id ? Orange : !known ? Hex("#5C6670") : air && !on ? Hex("#2E7D57") : Hex("#3BB273");
                    state.text = DemoHow(id, true);
                    state.color = title.color = known || DemoTrick == id ? Night : Cream;
                    if (eqBg != null)
                    {
                        eqBg.color = on ? Yellow : known ? new Color(1, 1, 1, 0.25f) : new Color(1, 1, 1, 0.08f);
                        eqText.text = on ? "-" : "+";
                    }
                });
            }
            SelectDemo(0, false);
        }

        /// <summary>Texte net sur petit corps : sans l'ombre portée, qui le faisait paraître flou.</summary>
        static void Sharp(Text t)
        {
            var sh = t.GetComponent<Shadow>();
            if (sh != null) Destroy(sh);
        }

        /// <summary>Le geste d'une figure, ou le niveau qui la débloque si elle n'est pas connue (short : pour la liste).</summary>
        string DemoHow(int id, bool shortText)
        {
            switch (id)
            {
                case DemoOllie: return "TOUCHE";
                case DemoGrind: return shortText ? "SUR UN RAIL · GLISSE : 8 GRINDS" : "Retombe sur un rail ; glisse pour changer de grind (50-50, boardslide, smith…)";
                case DemoManual: return shortText ? "GLISSE VERS LE BAS AVANT LE SOL" : "Juste avant de retomber, glisse vers le bas : il roule sur les roues arrière";
                case DemoNoseManual: return shortText ? "GLISSE VERS LE HAUT AVANT LE SOL" : "Juste avant de retomber, glisse vers le haut : il roule sur les roues avant";
                case Tuning.HalfTurn: return shortText ? "2 DOIGTS · FIGURES ×1,5" : "Touche avec 2 doigts : 180, il passe en fakie (figures ×1,5)";
            }
            if (progress != null && !progress().Tricks.Contains(id))
            {
                var def = System.Array.Find(Levels.All, l => l.Unlocks == id);
                return $"NIVEAU {def.Number} · {def.City}";
            }
            string dir = Tuning.SwipeWords[Tuning.TrickSwipe[id]];
            return shortText ? "GLISSE " + dir.ToUpper() : "En l'air, glisse le doigt " + dir;
        }

        void SelectDemo(int id, bool refreshAll = true)
        {
            DemoTrick = id;
            demoName.text = id == DemoOllie ? "OLLIE" : id == DemoGrind ? "GRIND" : id == DemoManual ? "MANUAL" : id == DemoNoseManual ? "NOSE MANUAL" : id == Tuning.HalfTurn ? "180 · FAKIE" : Tuning.TrickNames[id];
            demoHow.text = DemoHow(id, false);
            demoRail.gameObject.SetActive(id == DemoGrind);
            if (refreshAll) foreach (var r in refresh) r();
        }

        /// <summary>Pieds du skateur de démonstration (profil, onglet FIGURES) et son échelle.</summary>
        public Vector3 DemoFeet(out float scale)
        {
            scale = trickStage.lossyScale.x / 0.01f * 1.7f;
            return trickStage.TransformPoint(new Vector3(0, -70, 0));
        }

        public bool DemoVisible => profile.activeSelf && sections[1].page.activeSelf;

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
            challengeHud.gameObject.SetActive(story && run.Boss == null);   // pendant le boss, sa barre de vie prend la place
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
