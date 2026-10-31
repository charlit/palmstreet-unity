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
        const float NodeStep = 112, MapTop = 90;

        GameObject map, cleared;
        RectTransform mapViewport, mapContent;
        readonly RectTransform[] nodes = new RectTransform[12];
        readonly Image[] nodeFills = new Image[12];
        readonly Text[] nodeBests = new Text[12];
        Image clearedChip;
        Text clearedTitle, clearedCity, clearedScore, clearedBest, clearedGift, clearedTrick, clearedHow, nextLabel;
        RectTransform storyBar, storyFill, bubble;
        Text bubbleText;
        float bubbleTime = 99;

        // ---------- carte (FR-002, FR-003, FR-022) ----------

        void BuildMap(HudActions a)
        {
            map = Group("Carte");
            var t = map.transform;
            Label(t, "LA CÔTE BASQUE", 26, Cream, new Vector2(0, -34), new Vector2(0.5f, 1));

            mapViewport = Rect("Vue", t);
            mapViewport.offsetMin = new Vector2(0, 76); mapViewport.offsetMax = new Vector2(0, -64);
            mapViewport.gameObject.AddComponent<RectMask2D>();
            var scroll = mapViewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            mapContent = Rect("Côte", mapViewport);
            mapContent.anchorMin = new Vector2(0, 1); mapContent.anchorMax = new Vector2(1, 1);
            mapContent.pivot = new Vector2(0.5f, 1);
            mapContent.sizeDelta = new Vector2(0, MapTop + 11 * NodeStep + 120);
            scroll.content = mapContent;
            scroll.viewport = mapViewport;
            var land = Img(mapContent, Hex("#3E6B47"));   // la terre
            land.raycastTarget = true;                     // pour faire défiler au doigt partout
            var sea = Rect("Océan", mapContent);
            sea.anchorMax = new Vector2(0, 1); sea.offsetMax = new Vector2(96, 0);
            Img(sea, Hex("#1D5F8C")).raycastTarget = false;
            var foam = Rect("Écume", mapContent);
            foam.anchorMax = new Vector2(0, 1); foam.offsetMin = new Vector2(94, 0); foam.offsetMax = new Vector2(104, 0);
            Img(foam, Hex("#E9D8A6")).raycastTarget = false;

            var pos = new Vector2[12];
            for (int i = 0; i < 12; i++) pos[i] = NodePos(i);
            for (int i = 0; i < 11; i++)   // la route
            {
                var seg = Rect("Route", mapContent);
                seg.anchorMin = seg.anchorMax = new Vector2(0.5f, 1);
                var d = pos[i + 1] - pos[i];
                seg.anchoredPosition = (pos[i] + pos[i + 1]) / 2;
                seg.sizeDelta = new Vector2(d.magnitude, 10);
                seg.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                Img(seg, Hex("#E9D8A6")).raycastTarget = false;
            }
            for (int i = 0; i < 12; i++)
            {
                var def = Levels.All[i];
                int n = def.Number;
                bool boss = def.Boss != BossKind.None;
                float size = boss ? 74 : 62;
                var node = Button(mapContent, "Niveau " + n, new Vector2(0.5f, 1), pos[i], new Vector2(size, size), Cream, () => a.PickLevel(n));
                node.GetComponent<Image>().pixelsPerUnitMultiplier = 0.6f;   // pastille ronde
                var fill = Rect("Pastille", node.transform);
                fill.offsetMin = new Vector2(4, 4); fill.offsetMax = new Vector2(-4, -4);
                nodeFills[i] = Img(fill, Orange);
                nodeFills[i].pixelsPerUnitMultiplier = 0.6f;
                nodeFills[i].raycastTarget = false;
                Label(node.transform, n.ToString(), boss ? 30 : 26, Night, Vector2.zero);
                var name = Label(mapContent, def.City, 13, Cream, pos[i] + new Vector2(0, -size / 2 - 12), new Vector2(0.5f, 1));
                name.rectTransform.sizeDelta = new Vector2(200, 18);
                nodeBests[i] = Label(mapContent, "", 11, Yellow, pos[i] + new Vector2(0, -size / 2 - 27), new Vector2(0.5f, 1));
                if (boss) Label(mapContent, "BOSS", 12, Pink, pos[i] + new Vector2(0, size / 2 + 9), new Vector2(0.5f, 1));
                nodes[i] = node.GetComponent<RectTransform>();
            }

            var back = Button(t, "Retour carte", new Vector2(0.5f, 0), new Vector2(0, 38), new Vector2(200, 44), Orange, a.Home);
            Label(back.transform, "RETOUR", 20, Night, Vector2.zero);
            BuildStoryHud();
        }

        /// <summary>Pastilles en zigzag le long de la côte, du nord (Anglet) vers le sud puis l'intérieur.</summary>
        static Vector2 NodePos(int i) => new Vector2(i % 2 == 0 ? 10 : 90, -MapTop - i * NodeStep);

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
            ScrollTo(next);
        }

        void ScrollTo(int n)
        {
            Canvas.ForceUpdateCanvases();
            float view = mapViewport.rect.height, content = mapContent.rect.height;
            float y = -NodePos(n - 1).y - view / 2;
            mapContent.anchoredPosition = new Vector2(0, Mathf.Clamp(y, 0, Mathf.Max(0, content - view)));
        }

        public bool MapVisible => map.activeSelf;

        /// <summary>Position monde d'une pastille (le petit skateur s'y pose) et si elle est dans la partie visible.</summary>
        public Vector3 NodeWorld(int n, out bool visible, out float scale)
        {
            var node = nodes[n - 1];
            var w = node.TransformPoint(new Vector3(-58, -18, 0));   // à côté de la pastille, pas dessus
            var local = mapViewport.InverseTransformPoint(w);
            visible = mapViewport.rect.Contains(new Vector2(local.x, local.y));
            scale = node.lossyScale.x / 0.01f * 0.75f;
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
            var nb = Button(t, "Suivant", new Vector2(0.5f, 0.5f), new Vector2(0, -96), new Vector2(250, 54), Orange, a.Next);
            nextLabel = Label(nb.transform, "NIVEAU SUIVANT", 22, Night, Vector2.zero);
            var mb = Button(t, "Carte", new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(250, 42), new Color(Night.r, Night.g, Night.b, 0.85f), a.Map);
            Label(mb.transform, "CARTE", 18, Cream, Vector2.zero);
        }

        public void ShowCleared(LevelDef def, int score, int best, string trick, Item gift)
        {
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
            clearedTrick.text = trick != null ? "NOUVELLE FIGURE : " + trick : "";
            clearedHow.text = trick != null ? "Saute, puis touche l'écran en l'air" : "";
            nextLabel.text = last ? "MODE INFINI" : "NIVEAU SUIVANT";
        }

        // ---------- profil : figures connues et à débloquer (FR-017) ----------

        void BuildTricksPage(Transform section)
        {
            var back = Rect("Fond", section);
            back.offsetMin = new Vector2(8, 8); back.offsetMax = new Vector2(-8, -126);
            Img(back, new Color(Night.r, Night.g, Night.b, 0.88f)).raycastTarget = false;
            string[] names = { "OLLIE", "GRIND", "FLIP", "SHOVE-IT", "360°", "360° INVERSÉ" };
            for (int i = 0; i < names.Length; i++)
            {
                int trick = i - 2;   // −2, −1 : ollie et grind, toujours connus
                var row = Rect("Figure " + names[i], section);
                row.anchorMin = row.anchorMax = new Vector2(0.5f, 1);
                row.anchoredPosition = new Vector2(0, -160 - i * 62);
                row.sizeDelta = new Vector2(320, 52);
                var bg = Img(row, Hex("#3BB273"));
                bg.raycastTarget = false;
                Label(row, names[i], 20, Night, new Vector2(-60, 0));
                var state = Label(row, "", 13, Night, new Vector2(80, 0));
                refresh.Add(() =>
                {
                    var p = progress();
                    var def = trick < 0 ? null : System.Array.Find(Levels.All, l => l.Unlocks == trick);
                    bool known = trick < 0 || p.Tricks.Contains(trick);
                    bg.color = known ? Hex("#3BB273") : Hex("#5C6670");
                    state.text = known ? "CONNUE" : $"NIVEAU {def.Number} · {def.City}";
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
            bubbleTime += dt;
            bubble.gameObject.SetActive(bubbleTime < 3.5f);
        }
    }
}
