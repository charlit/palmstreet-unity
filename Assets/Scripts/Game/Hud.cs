using System;
using System.Collections.Generic;
using PalmStreet.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Interface (FR-002, FR-009, FR-014, FR-016, FR-017, FR-021, FR-022, FR-026) : accueil avec JOUER, MODE INFINI,
    /// PROFIL et TUTORIEL (TOP 10 en fin de partie infinie) ; profil où l'on change la tenue ; score, BEST, combo, bandeaux, barre de vie du boss,
    /// textes de points ; écran WIPEOUT ; pause ; son.
    /// Le canevas est rendu par la caméra du jeu, en unités proches de l'écran logique de 390 × 620.
    /// </summary>
    /// <summary>Ce que les boutons de l'interface déclenchent dans le jeu.</summary>
    public sealed class HudActions
    {
        public Action Play, Endless, OpenProfile, CloseProfile, Top10, Mute, ToggleVibrate, OutfitChanged,
                      OpenSettings, AskQuit, Resume, Quit, Home, Map, Retry, Next;
        public Action<int> PickLevel, Cast;
        public Func<bool> SoundOn, VibrateOn;
        public Func<Progress> Progress;
    }

    public sealed partial class Hud : MonoBehaviour
    {
        static readonly Color Cream = Hex("#FFF4E0"), Orange = Hex("#FF7A1A"), Pink = Hex("#FF4D8D"), Night = Hex("#1B1028"), Yellow = Hex("#FFE14D");

        Font font;
        Sprite round;
        RectTransform safe;
        GameObject menu, game, over, pause, profile;
        Text score, best, combo, theme, banner, subBanner, overScore, overBest, record, hint, menuBest, menuNext, toast, endlessLabel, endlessLock;
        Image endlessImage;
        RectTransform bossBar, bossFill, bannerRt;
        /// <summary>Le mot STREET du titre de l'accueil (l'animation de lancement grinde dessus).</summary>
        public Text TitleStreet { get; private set; }
        CanvasGroup toastGroup;
        RectTransform profileStage;
        GameObject overStory, overEndless;
        Func<Progress> progress;
        Func<bool> soundOn, vibrateOn;
        Action toggleVibrate, mute;
        RectTransform startButton, settingsButton;   // settingsButton : RÉGLAGES et la croix
        GameObject quitConfirm, settingsOnly;   // settingsOnly : RÉGLAGES, caché quand la partie est finie (la croix reste)
        GameObject gameSettings;
        Action outfitChanged;
        readonly List<Action> refresh = new List<Action>();
        readonly List<(Image tab, GameObject page)> tabs = new List<(Image, GameObject)>(), sections = new List<(Image, GameObject)>();
        readonly List<(Text text, float life, Vector3 pos)> floats = new List<(Text, float, Vector3)>();
        readonly Queue<(string, string, Color)> banners = new Queue<(string, string, Color)>();
        float bannerTime = 99, toastTime = 99, comboPop;
        bool daySky;   // ville de jour : bandeaux, figures et points en sombre (spec 003)
        int lastCombo = 1;

        public void Init(Camera cam, HudActions a)
        {
            soundOn = a.SoundOn; vibrateOn = a.VibrateOn; toggleVibrate = a.ToggleVibrate; mute = a.Mute;
            progress = a.Progress;
            outfitChanged = a.OutfitChanged;
            Action openSettings = a.OpenSettings, askQuit = a.AskQuit, resume = a.Resume, quit = a.Quit;
            font = Resources.Load<Font>("Fonts/Rubik-Bold")   // Rubik Bold, licence OFL (spec 003)
                ?? Font.CreateDynamicFontFromOSFont(new[] { "Avenir Next Condensed", "Futura", "Arial Black", "Roboto Condensed", "Roboto", "Arial" }, 32);
            round = RoundSprite();

            var canvasGo = new GameObject("Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1;
            canvas.sortingOrder = 500;   // au-dessus du monde ; les skateurs des cartes sont dessinés juste au-dessus (ordre 600)
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 620);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform, false);

            safe = Rect("Zone sûre", canvasGo.transform);
            ApplySafeArea();

            BuildGame();
            BuildMenu(a.Play, a.Endless, a.OpenProfile);
            BuildMap(a);
            BuildProfile(a.CloseProfile);
            BuildOver(a);
            BuildCleared(a);
            BuildPreview(a.PickLevel);
            BuildShout();
            BuildSpells(a.Cast);
            BuildTutorial();

            pause = Panel("Pause", 0.55f);
            Label(pause.transform, "PAUSE", 54, Cream, new Vector2(0, 40));
            Label(pause.transform, "Touche l'écran pour reprendre", 18, Cream, new Vector2(0, -20));

            // en partie, en haut à droite : RÉGLAGES et une croix pour quitter (les deux mettent le jeu en pause)
            settingsButton = Rect("Boutons en partie", safe);
            settingsButton.anchorMin = settingsButton.anchorMax = settingsButton.pivot = new Vector2(1, 1);
            settingsButton.anchoredPosition = Vector2.zero;
            settingsButton.sizeDelta = new Vector2(170, 60);
            settingsOnly = Button(settingsButton, "Réglages", new Vector2(1, 1), new Vector2(-110, -30), new Vector2(92, 44), new Color(Night.r, Night.g, Night.b, 0.6f), openSettings);
            Label(settingsOnly.transform, "RÉGLAGES", 14, Cream, Vector2.zero);
            var xbtn = Button(settingsButton, "Quitter", new Vector2(1, 1), new Vector2(-32, -30), new Vector2(46, 44), new Color(Night.r, Night.g, Night.b, 0.6f), askQuit);
            Label(xbtn.transform, "X", 22, Cream, Vector2.zero);

            // confirmation avant de quitter : un toucher par erreur ne fait pas perdre la partie
            quitConfirm = Panel("Quitter la partie ?", 0.7f);
            quitConfirm.GetComponent<Image>().raycastTarget = true;
            var qbox = Rect("Fenêtre", quitConfirm.transform);
            qbox.anchorMin = qbox.anchorMax = new Vector2(0.5f, 0.5f);
            qbox.sizeDelta = new Vector2(300, 200);
            Img(qbox, new Color(Night.r, Night.g, Night.b, 0.95f)).raycastTarget = false;
            Label(qbox, "QUITTER LA PARTIE ?", 24, Cream, new Vector2(0, -40), new Vector2(0.5f, 1));
            Label(qbox, "Le score de cette partie sera perdu", 14, Cream, new Vector2(0, -74), new Vector2(0.5f, 1));
            var yes = Button(qbox, "Oui, quitter", new Vector2(0.5f, 0), new Vector2(-72, 46), new Vector2(128, 50), Pink, quit);
            Label(yes.transform, "QUITTER", 18, Cream, Vector2.zero);
            var no = Button(qbox, "Non, reprendre", new Vector2(0.5f, 0), new Vector2(72, 46), new Vector2(128, 50), Orange, resume);
            Label(no.transform, "REPRENDRE", 18, Night, Vector2.zero);

            // fenêtre des réglages en partie : son, vibrations, reprendre ou revenir à l'accueil
            gameSettings = Panel("Réglages en partie", 0.7f);
            gameSettings.GetComponent<Image>().raycastTarget = true;   // un toucher à côté ne relance pas la partie
            var box = Rect("Fenêtre", gameSettings.transform);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(300, 430);
            Img(box, new Color(Night.r, Night.g, Night.b, 0.95f)).raycastTarget = false;
            Label(box, "RÉGLAGES", 30, Cream, new Vector2(0, -34), new Vector2(0.5f, 1));
            var page = Page(box, "Interrupteurs", -64).transform;
            float gy = 0;
            Toggle(page, ref gy, "SON (MUSIQUE ET BRUITAGES)", soundOn, mute);
            Toggle(page, ref gy, "VIBRATIONS", vibrateOn, toggleVibrate);
            var gb = Button(box, "Gestes", new Vector2(0.5f, 0), new Vector2(0, 138), new Vector2(220, 38), new Color(1, 1, 1, 0.12f), () => ShowTutorial());
            Label(gb.transform, "TUTORIEL", 15, Cream, Vector2.zero);
            var rb = Button(box, "Reprendre", new Vector2(0.5f, 0), new Vector2(0, 82), new Vector2(220, 48), Orange, resume);
            Label(rb.transform, "REPRENDRE", 22, Night, Vector2.zero);
            var qb = Button(box, "Accueil", new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(220, 38), new Color(1, 1, 1, 0.12f), quit);
            Label(qb.transform, "QUITTER LA PARTIE", 15, Cream, Vector2.zero);

            var tg = new GameObject("Message", typeof(RectTransform), typeof(CanvasGroup));
            var trt = tg.GetComponent<RectTransform>();
            trt.SetParent(safe, false);
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0);
            trt.anchoredPosition = new Vector2(0, 150);
            trt.sizeDelta = new Vector2(330, 44);
            toastGroup = tg.GetComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.alpha = 0;
            Img(trt, new Color(Night.r, Night.g, Night.b, 0.85f));
            toast = Label(trt, "", 15, Cream, Vector2.zero);
        }

        // ---------- construction ----------

        void BuildGame()
        {
            game = Group("Partie");
            var t = game.transform;
            Label(t, "SCORE", 13, Cream, new Vector2(18, -18), new Vector2(0, 1), TextAnchor.UpperLeft);
            score = Label(t, "0", 34, Cream, new Vector2(18, -32), new Vector2(0, 1), TextAnchor.UpperLeft);
            best = Label(t, "BEST 0", 14, Yellow, new Vector2(18, -72), new Vector2(0, 1), TextAnchor.UpperLeft);
            combo = Label(t, "", 22, Pink, new Vector2(18, -92), new Vector2(0, 1), TextAnchor.UpperLeft);
            theme = Label(t, "", 13, Cream, new Vector2(-14, -60), new Vector2(1, 1), TextAnchor.UpperRight);

            bossBar = Rect("Vie du boss", t);
            bossBar.anchorMin = bossBar.anchorMax = new Vector2(0.5f, 1);
            bossBar.anchoredPosition = new Vector2(0, -134);
            bossBar.sizeDelta = new Vector2(230, 16);
            Img(bossBar, new Color(Night.r, Night.g, Night.b, 0.8f));
            bossFill = Rect("Vie", bossBar);
            bossFill.offsetMin = new Vector2(3, 3); bossFill.offsetMax = new Vector2(-3, -3);
            Img(bossFill, Pink);
            Label(bossBar, "BOSS", 13, Cream, new Vector2(0, 18));

            bannerRt = Rect("Bandeau", t);
            bannerRt.anchorMin = bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRt.anchoredPosition = new Vector2(0, 90);
            banner = Label(bannerRt, "", 48, Yellow, Vector2.zero);
            subBanner = Label(bannerRt, "", 22, Cream, new Vector2(0, -44));
            subBanner.rectTransform.sizeDelta = new Vector2(350, 52);   // deux lignes au plus
            Fit(subBanner, 12);   // les conseils des boss sont longs
            bannerRt.gameObject.SetActive(false);

            for (int i = 0; i < 14; i++)
            {
                var f = Label(t, "", 19, Cream, Vector2.zero);
                f.gameObject.SetActive(false);
                floats.Add((f, 0, Vector3.zero));
            }
        }

        void BuildMenu(Action play, Action endless, Action openProfile)
        {
            menu = Group("Accueil");
            var t = menu.transform;
            Label(t, "PALM", 62, Cream, new Vector2(0, -80), new Vector2(0.5f, 1));
            TitleStreet = Label(t, "STREET", 62, Orange, new Vector2(0, -132), new Vector2(0.5f, 1));
            Label(t, "TEAM", 40, Pink, new Vector2(0, -178), new Vector2(0.5f, 1));
            Label(t, "au cœur du Pays basque", 15, Cream, new Vector2(0, -212), new Vector2(0.5f, 1));
            menuBest = Label(t, "", 15, Yellow, new Vector2(0, -240), new Vector2(0.5f, 1));   // étoiles et record infini

            // tout ce qui se touche est en bas, sous le pouce (HIG : zone du pouce, cibles d'au moins 44 pt) :
            // JOUER en grand, puis MODE INFINI, puis une barre PROFIL · TUTORIEL
            startButton = StartButton(t, "JOUER", play);
            menuNext = Label(t, "", 13, Cream, new Vector2(0, 152), new Vector2(0.5f, 0));   // le niveau que JOUER va proposer
            var eb = Button(t, "INFINI", new Vector2(0.5f, 0), new Vector2(0, 108), new Vector2(250, 50), Hex("#4A2A6E"), endless);
            endlessImage = eb.GetComponent<Image>();
            endlessLabel = Label(eb.transform, "MODE INFINI", 19, Yellow, Vector2.zero);
            endlessLock = Label(eb.transform, "APRÈS LES 12 VILLES", 10, Cream, new Vector2(0, -14));
            string[] bar = { "PROFIL", "TUTORIEL" };
            Action[] barActions = { openProfile, () => ShowTutorial() };
            for (int i = 0; i < bar.Length; i++)
            {
                var b = Button(t, bar[i], new Vector2(0.5f, 0), new Vector2(-64 + i * 128, 38), new Vector2(120, 48), i == 0 ? Pink : Hex("#4A2A6E"), barActions[i]);
                Label(b.transform, bar[i], 16, Cream, Vector2.zero);
            }
        }

        // ---------- profil : tenue du skateur (FR-026) ----------

        void BuildProfile(Action close)
        {
            profile = Group("Profil");
            var t = profile.transform;
            Label(t, "PROFIL", 30, Cream, new Vector2(0, -36), new Vector2(0.5f, 1));

            // deux grands onglets : TENUE et RÉGLAGES
            string[] sectionNames = { "TENUE", "FIGURES", "RÉGLAGES" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var tb = Button(t, sectionNames[i], new Vector2(0.5f, 1), new Vector2(-118 + i * 118, -94), new Vector2(112, 40), Night, () => ShowSection(index));
                Label(tb.transform, sectionNames[i], 18, Cream, Vector2.zero);
                sections.Add((tb.GetComponent<Image>(), Rect("Section " + sectionNames[i], t).gameObject));
            }

            // TENUE : aperçu animé, puis les parties du skateur en petits onglets
            var tenue = sections[0].page.transform;
            profileStage = Rect("Aperçu", tenue);
            profileStage.anchorMin = profileStage.anchorMax = new Vector2(0.5f, 1);
            profileStage.anchoredPosition = new Vector2(0, -186);
            profileStage.sizeDelta = new Vector2(170, 130);
            Img(profileStage, new Color(Night.r, Night.g, Night.b, 0.45f)).raycastTarget = false;
            Panel(tenue, -262);

            // spec 002 (FR-023 à FR-027) : des objets gagnés en finissant les niveaux, plus de couleurs libres
            string[] names = { "TÊTE", "HAUT", "BAS", "PIEDS", "PLANCHE", "CORPS" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var tb = Button(tenue, names[i], new Vector2(0.5f, 1), new Vector2(-150 + i * 60, -276), new Vector2(56, 28), Night, () => ShowTab(index));
                Label(tb.transform, names[i], 12, Cream, Vector2.zero);
                tabs.Add((tb.GetComponent<Image>(), Page(tenue, "Page " + names[i], -298)));
            }
            foreach (Slot slot in System.Enum.GetValues(typeof(Slot))) ItemCards(tabs[(int)slot].page.transform, slot);
            float y = 0;
            var p = tabs[5].page.transform;
            Swatches(p, ref y, "PEAU", Wardrobe.Skins, null, () => progress().Skin, v => progress().Skin = v);
            Selector(p, ref y, "COIFFURE", Wardrobe.HairStyles, new int[Wardrobe.HairStyles.Length], () => progress().HairStyle, v => progress().HairStyle = v);
            Swatches(p, ref y, "CHEVEUX", Wardrobe.Hairs, null, () => progress().HairColor, v => progress().HairColor = v);

            // RÉGLAGES : son et vibrations
            BuildTricksPage(sections[1].page.transform);
            var settings = sections[2].page.transform;
            Panel(settings, -126);
            p = Page(settings, "Réglages", -146).transform; y = 0;
            Toggle(p, ref y, "SON (MUSIQUE ET BRUITAGES)", soundOn, mute);
            Toggle(p, ref y, "VIBRATIONS", vibrateOn, toggleVibrate);

            var ok = Button(t, "RETOUR", new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(210, 46), Orange, close);
            Label(ok.transform, "RETOUR", 22, Night, Vector2.zero);
            ShowTab(0);
            ShowSection(0);
            Refresh();
        }

        /// <summary>Fond sombre des réglages, du haut donné jusqu'en bas de l'écran.</summary>
        void Panel(Transform parent, float top)
        {
            var back = Rect("Fond", parent);
            back.offsetMin = new Vector2(8, 8); back.offsetMax = new Vector2(-8, top);
            Img(back, new Color(Night.r, Night.g, Night.b, 0.88f)).raycastTarget = false;
        }

        static GameObject Page(Transform parent, string name, float top)
        {
            var page = Rect(name, parent);
            page.anchorMin = page.anchorMax = new Vector2(0.5f, 1);
            page.pivot = new Vector2(0.5f, 1);
            page.anchoredPosition = new Vector2(0, top);
            page.sizeDelta = new Vector2(390, 10);
            return page.gameObject;
        }

        static void Select(List<(Image tab, GameObject page)> list, int index)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].tab.color = i == index ? Orange : new Color(Night.r, Night.g, Night.b, 0.9f);
                list[i].page.SetActive(i == index);
            }
        }

        public void ShowTab(int index) => Select(tabs, index);
        public void ShowSection(int index) => Select(sections, index);

        void Changed()
        {
            Refresh();
            outfitChanged();
        }

        void Refresh() { foreach (var r in refresh) r(); }

        /// <summary>Cartes des objets d'un emplacement : possédés à porter (cadre orange sur le porté), verrouillés avec leur niveau.</summary>
        void ItemCards(Transform parent, Slot slot)
        {
            int k = 0;
            foreach (var item in Items.InSlot(slot))
            {
                var it = item;
                int col = k % 2, row = k / 2;
                k++;
                var card = Button(parent, it.Name, new Vector2(0.5f, 1), new Vector2(col == 0 ? -80 : 80, -40 - row * 72), new Vector2(152, 62), Cream, () =>
                {
                    if (progress().Wear(it.Id)) Changed();
                    else if (it.Stars > 0) Toast($"Se gagne avec {it.Stars} étoiles (tu en as {progress().TotalStars})");
                    else if (it.Boss != BossKind.None) Toast($"Tenue de {Game.BossName(it.Boss)} : bats-le au niveau {it.Level}");
                    else { var d = Levels.Get(it.Level); Toast($"Se gagne en finissant le niveau {d.Number} · {d.City}"); }
                });
                var frame = card.GetComponent<Image>();
                var inner = Rect("Fond", card.transform);
                inner.offsetMin = new Vector2(3, 3); inner.offsetMax = new Vector2(-3, -3);
                Img(inner, Hex("#2A1B3D")).raycastTarget = false;
                var chip = Rect("Couleur", card.transform);
                chip.anchorMin = chip.anchorMax = new Vector2(0, 0.5f);
                chip.anchoredPosition = new Vector2(26, 0);
                chip.sizeDelta = new Vector2(36, 36);
                var chipImg = Img(chip, Wardrobe.ItemColor(it.Id));
                chipImg.pixelsPerUnitMultiplier = 0.6f;
                chipImg.raycastTarget = false;
                var dot = Rect("Motif", chip);   // couleur du motif de l'objet
                dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 0.5f);
                dot.sizeDelta = new Vector2(14, 14);
                var dotImg = Img(dot, Wardrobe.ItemDetail(it.Id));
                dotImg.pixelsPerUnitMultiplier = 0.3f;
                dotImg.raycastTarget = false;
                var name = Label(card.transform, it.Name, 12, Cream, new Vector2(22, 8));
                name.rectTransform.sizeDelta = new Vector2(100, 18);
                Fit(name, 8);
                var state = Label(card.transform, "", 10, Yellow, new Vector2(22, -11));
                state.rectTransform.sizeDelta = new Vector2(104, 16);
                Fit(state, 7);
                refresh.Add(() =>
                {
                    var p = progress();
                    bool owned = p.Owns(it.Id), worn = p.WornIn(it.Slot) == it.Id;
                    frame.color = worn ? Orange : owned ? new Color(Cream.r, Cream.g, Cream.b, 0.25f) : Hex("#3A3442");
                    chipImg.color = owned ? Wardrobe.ItemColor(it.Id) : Hex("#5C6670");
                    dotImg.color = owned ? Wardrobe.ItemDetail(it.Id) : Color.clear;
                    name.color = owned ? Cream : new Color(Cream.r, Cream.g, Cream.b, 0.5f);
                    state.text = worn ? "PORTÉ" : owned ? "" : it.Stars > 0 ? $"{it.Stars} ÉTOILES" : it.Boss != BossKind.None ? "BATS " + Game.BossName(it.Boss) : $"NIVEAU {it.Level} · {Levels.Get(it.Level).City}";
                    state.color = worn ? Yellow : new Color(Cream.r, Cream.g, Cream.b, 0.6f);
                });
            }
        }

        /// <summary>Choix dans une liste de mots : ‹ VALEUR ›.</summary>
        void Selector(Transform parent, ref float y, string label, string[] names, int[] unused, Func<int> get, Action<int> set)
        {
            Label(parent, label, 13, Yellow, new Vector2(0, y - 10), new Vector2(0.5f, 1));
            var value = Label(parent, "", 18, Cream, new Vector2(0, y - 38), new Vector2(0.5f, 1));
            int n = names.Length;
            var l = Button(parent, label + " ‹", new Vector2(0.5f, 1), new Vector2(-130, y - 38), new Vector2(46, 34), Orange, () => { set((get() + n - 1) % n); Changed(); });
            Label(l.transform, "<", 20, Night, Vector2.zero);
            var r = Button(parent, label + " ›", new Vector2(0.5f, 1), new Vector2(130, y - 38), new Vector2(46, 34), Orange, () => { set((get() + 1) % n); Changed(); });
            Label(r.transform, ">", 20, Night, Vector2.zero);
            refresh.Add(() => value.text = names[((get() % n) + n) % n]);
            y -= 66;
        }

        /// <summary>Pastilles de couleur, 7 par ligne ; la pastille choisie a un anneau clair.</summary>
        void Swatches(Transform parent, ref float y, string label, Color[] colors, int[] unused, Func<int> get, Action<int> set)
        {
            Label(parent, label, 13, Yellow, new Vector2(0, y - 10), new Vector2(0.5f, 1));
            const float step = 44;
            var rings = new Image[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                int index = i, row = i / 7, col = i % 7, inRow = Mathf.Min(7, colors.Length - row * 7);
                var pos = new Vector2((col - (inRow - 1) / 2f) * step, y - 40 - row * step);
                var ring = Rect("Anneau", parent);
                ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 1);
                ring.anchoredPosition = pos;
                ring.sizeDelta = new Vector2(40, 40);
                rings[i] = Img(ring, Cream);
                rings[i].raycastTarget = false;
                var b = Button(parent, label + " " + i, new Vector2(0.5f, 1), pos, new Vector2(32, 32), colors[i], () => { set(index); Changed(); });
                b.GetComponent<UnityEngine.UI.Button>().transition = Selectable.Transition.None;
            }
            refresh.Add(() => { for (int i = 0; i < rings.Length; i++) rings[i].enabled = i == get(); });
            y -= 26 + Mathf.CeilToInt(colors.Length / 7f) * step;
        }

        /// <summary>Interrupteur : vert ACTIVÉ / gris COUPÉ.</summary>
        void Toggle(Transform parent, ref float y, string label, Func<bool> get, Action flip)
        {
            Label(parent, label, 13, Yellow, new Vector2(0, y - 10), new Vector2(0.5f, 1));
            var b = Button(parent, label, new Vector2(0.5f, 1), new Vector2(0, y - 44), new Vector2(220, 44), Orange, () => { flip(); Refresh(); });
            var img = b.GetComponent<Image>();
            var text = Label(b.transform, "", 19, Night, Vector2.zero);
            refresh.Add(() =>
            {
                bool on = get();
                img.color = on ? Hex("#3BB273") : Hex("#5C6670");
                text.text = on ? "ACTIVÉ" : "COUPÉ";
                text.color = on ? Night : Cream;
            });
            y -= 80;
        }

        /// <summary>Bouton START : pilule orange en relief (ombre, reflet), icône lecture, qui respire doucement.</summary>
        RectTransform StartButton(Transform parent, string text, Action start)
        {
            var root = Rect("START", parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0);
            root.anchoredPosition = new Vector2(0, 208);
            root.sizeDelta = new Vector2(250, 68);
            var shadow = Rect("Ombre", root);
            shadow.offsetMin = new Vector2(0, -7); shadow.offsetMax = new Vector2(0, -7);
            Img(shadow, Hex("#A8400A")).raycastTarget = false;
            var face = Rect("Bouton", root);
            var img = Img(face, Orange);
            img.pixelsPerUnitMultiplier = 0.9f;   // coins très ronds : une pilule
            shadow.GetComponent<Image>().pixelsPerUnitMultiplier = 0.9f;
            var shine = Rect("Reflet", face);
            shine.anchorMin = new Vector2(0, 0.66f); shine.offsetMin = new Vector2(24, 0); shine.offsetMax = new Vector2(-24, -5);
            var s = Img(shine, new Color(1, 1, 1, 0.16f));
            s.raycastTarget = false;
            s.pixelsPerUnitMultiplier = 1.4f;
            var b = face.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => start());
            var icon = Rect("Lecture", face);
            icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = new Vector2(-62, 0);
            icon.sizeDelta = new Vector2(26, 28);
            var tri = icon.gameObject.AddComponent<Image>();
            tri.sprite = TriangleSprite();
            tri.color = Night;
            tri.raycastTarget = false;
            Label(face, text, 34, Night, new Vector2(16, 0));
            return root;
        }

        static Sprite TriangleSprite()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float half = (n - x) / 2f, d = Mathf.Min(half - Mathf.Abs(y + 0.5f - n / 2f), x + 0.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(d)));
                }
            tex.Apply();
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
        }

        void BuildOver(HudActions a)
        {
            over = Panel("Fin", 0.45f);
            var t = over.transform;
            // histoire : RECOMMENCER, CARTE ; infini : toucher pour rejouer, TOP 10, ACCUEIL (spec 002)
            overStory = Rect("Boutons histoire", t).gameObject;
            var rb = Button(overStory.transform, "Recommencer", new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(240, 50), Orange, a.Retry);
            Label(rb.transform, "RECOMMENCER", 22, Night, Vector2.zero);
            var mb = Button(overStory.transform, "Carte", new Vector2(0.5f, 0.5f), new Vector2(0, -92), new Vector2(240, 42), new Color(Night.r, Night.g, Night.b, 0.85f), a.Map);
            Label(mb.transform, "CARTE", 18, Cream, Vector2.zero);
            overEndless = Rect("Boutons infini", t).gameObject;
            var tb = Button(overEndless.transform, "Top 10", new Vector2(0.5f, 0.5f), new Vector2(-64, -30), new Vector2(118, 42), new Color(Night.r, Night.g, Night.b, 0.85f), a.Top10);
            Label(tb.transform, "TOP 10", 18, Yellow, Vector2.zero);
            var hb = Button(overEndless.transform, "Accueil", new Vector2(0.5f, 0.5f), new Vector2(64, -30), new Vector2(118, 42), new Color(Night.r, Night.g, Night.b, 0.85f), a.Home);
            Label(hb.transform, "ACCUEIL", 18, Cream, Vector2.zero);
            Label(t, "WIPEOUT !", 58, Hex("#FF5A4D"), new Vector2(0, 190));
            Label(t, "SCORE", 15, Cream, new Vector2(0, 136));
            overScore = Label(t, "0", 46, Cream, new Vector2(0, 104));
            overBest = Label(t, "", 17, Yellow, new Vector2(0, 66));
            record = Label(t, "NOUVEAU RECORD !", 22, Pink, new Vector2(0, 36));
            hint = Label(overEndless.transform, "Touche l'écran pour rejouer", 18, Cream, new Vector2(0, -88));
        }

        // ---------- écrans ----------

        public void ShowMenu(int bestScore)
        {
            Show(menu);
            var p = progress();
            menuBest.text = $"{p.TotalStars}/{Progress.MaxStars} ÉTOILES" + (bestScore > 0 ? "   ·   RECORD INFINI " + bestScore : "");
            var next = Levels.Get(p.NextLevel);
            menuNext.text = $"NIVEAU {next.Number} · {next.City}";
            bool open = p.EndlessOpen;   // le mode infini s'ouvre une fois la carte finie
            endlessImage.color = open ? Hex("#4A2A6E") : Hex("#2E2440");
            endlessLabel.color = open ? Yellow : new Color(Yellow.r, Yellow.g, Yellow.b, 0.45f);
            endlessLabel.rectTransform.anchoredPosition = new Vector2(0, open ? 0 : 6);
            endlessLock.gameObject.SetActive(!open);
        }

        public void ShowProfile()
        {
            Show(profile);
            ShowSection(0);
            Refresh();
        }

        public void ShowGame()
        {
            Show(game);
            banners.Clear(); bannerTime = 99; bannerRt.gameObject.SetActive(false);
            bubbleTime = 99;   // pas de bulle d'aide d'un niveau précédent
            for (int i = 0; i < floats.Count; i++) { floats[i].text.gameObject.SetActive(false); floats[i] = (floats[i].text, 0, Vector3.zero); }
        }

        public void ShowOver(int s, int b, bool newBest, bool story)
        {
            over.SetActive(true);
            overStory.SetActive(story);
            overEndless.SetActive(!story);
            over.GetComponent<Image>().raycastTarget = story;   // en histoire, seuls les boutons comptent
            overScore.text = s.ToString();
            overBest.text = "BEST " + b;
            record.gameObject.SetActive(newBest);
        }

        public void ShowPause(bool on) => pause.SetActive(on);

        void Show(GameObject g)
        {
            menu.SetActive(g == menu);
            map.SetActive(g == map);
            cleared.SetActive(false);
            game.SetActive(g == game);
            profile.SetActive(g == profile);
            settingsButton.gameObject.SetActive(g == game);   // en partie ; sinon les réglages sont dans PROFIL
            gameSettings.SetActive(false);
            quitConfirm.SetActive(false);
            over.SetActive(false);
            pause.SetActive(false);
            preview.SetActive(false);
        }

        public void ShowGameSettings(bool on, bool askQuit = false)
        {
            gameSettings.SetActive(on && !askQuit);
            quitConfirm.SetActive(on && askQuit);
            settingsButton.gameObject.SetActive(!on && game.activeSelf);
            if (on) Refresh();
        }

        public bool GameSettingsOpen => gameSettings.activeSelf;

        public void Toast(string message)
        {
            toast.text = message;
            toastTime = 0;
        }

        /// <summary>Position monde des pieds du skateur du profil, et l'échelle à lui donner.</summary>
        public Vector3 PreviewFeet(out float scale)
        {
            scale = profileStage.lossyScale.x / 0.01f * 1.45f;
            return profileStage.TransformPoint(new Vector3(0, -56, 0));
        }

        public bool PreviewVisible => profile.activeSelf && sections[0].page.activeSelf;

        public bool MenuVisible => menu.activeSelf;

        // ---------- en partie ----------

        public void Banner(string text, string sub, Color col) => banners.Enqueue((text, sub, col));

        public void Float(string text, Vector3 world, Color col)
        {
            for (int i = 0; i < floats.Count; i++)
            {
                if (floats[i].life > 0) continue;
                var f = floats[i].text;
                f.text = text;
                f.color = daySky ? Night : col;
                f.gameObject.SetActive(true);
                floats[i] = (f, 1, world);
                return;
            }
        }

        public void Tick(Run run, float dt, float time)
        {
            TickStory(run, dt);
            if (settingsOnly.activeSelf != (run.Phase == Phase.Playing)) settingsOnly.SetActive(run.Phase == Phase.Playing);
            TickTutorial(time);
            TickShout(dt);
            TickSpells(run);
            if (menu.activeSelf) startButton.localScale = Vector3.one * (1 + 0.035f * Mathf.Sin(time * 3.2f));
            if (game.activeSelf)
            {
                score.text = run.Score.ToString();
                best.text = "BEST " + Mathf.Max(run.Best, run.Score);
                if (run.Combo != lastCombo) { if (run.Combo > lastCombo) comboPop = 1; lastCombo = run.Combo; }
                combo.text = run.Combo > 1 ? "COMBO x" + run.Combo : "";
                comboPop = Mathf.MoveTowards(comboPop, 0, dt * 4);
                combo.rectTransform.localScale = Vector3.one * (1 + comboPop * 0.35f);
                theme.text = ThemeLook.Of(run.Theme).Name;
                daySky = Levels.IsDay(run.Theme);
                bossBar.gameObject.SetActive(run.Boss != null && run.Boss.State != BossState.Dying);
                if (run.Boss != null) bossFill.anchorMax = new Vector2(Mathf.Clamp01((float)run.Boss.Hp / run.Boss.MaxHp), 1);

                bannerTime += dt;
                if (bannerTime > 1.7f && banners.Count > 0)
                {
                    var (text, sub, col) = banners.Dequeue();
                    banner.text = text; banner.color = daySky ? Night : col; subBanner.text = sub ?? "";
                    subBanner.color = daySky ? Night : Cream;
                    bannerTime = 0;
                }
                bool showBanner = bannerTime < 1.7f;
                bannerRt.gameObject.SetActive(showBanner);
                if (showBanner)
                {
                    float pop = bannerTime < 0.15f ? Mathf.Lerp(1.6f, 1, bannerTime / 0.15f) : 1;
                    bannerRt.localScale = Vector3.one * pop;
                    banner.canvasRenderer.SetAlpha(Mathf.Clamp01((1.7f - bannerTime) * 3));
                    subBanner.canvasRenderer.SetAlpha(Mathf.Clamp01((1.7f - bannerTime) * 3));
                }

                float z = safe.position.z;
                for (int i = 0; i < floats.Count; i++)
                {
                    var (text, life, pos) = floats[i];
                    if (life <= 0) continue;
                    life -= dt * 0.9f;
                    pos.y += dt * 0.45f;
                    text.rectTransform.position = new Vector3(pos.x, pos.y, z);
                    text.canvasRenderer.SetAlpha(Mathf.Clamp01(life * 2.5f));
                    if (life <= 0) text.gameObject.SetActive(false);
                    floats[i] = (text, life, pos);
                }
            }
            if (over.activeSelf) hint.canvasRenderer.SetAlpha(0.55f + 0.45f * Mathf.Sin(time * 4));
            if (over.activeSelf && record.gameObject.activeSelf) record.rectTransform.localScale = Vector3.one * (1 + 0.06f * Mathf.Sin(time * 6));

            toastTime += dt;
            toastGroup.alpha = toastTime < 2.6f ? Mathf.Clamp01((2.6f - toastTime) * 3) : 0;
        }

        // ---------- outils uGUI ----------

        void ApplySafeArea()
        {
            var a = Screen.safeArea;
            safe.anchorMin = new Vector2(a.xMin / Screen.width, a.yMin / Screen.height);
            safe.anchorMax = new Vector2(a.xMax / Screen.width, a.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        Rect lastSafe;
        void Update()
        {
            if (Screen.safeArea != lastSafe) { lastSafe = Screen.safeArea; ApplySafeArea(); }
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        GameObject Group(string name) => Rect(name, safe).gameObject;

        GameObject Panel(string name, float dim)
        {
            var rt = Rect(name, safe.parent);   // le voile couvre tout l'écran, encoche comprise
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.04f, 0.01f, 0.08f, dim);
            img.raycastTarget = false;   // un toucher sur le voile va au jeu (rejouer, reprendre)
            rt.gameObject.SetActive(false);
            return rt.gameObject;
        }

        Image Img(RectTransform rt, Color col)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = round;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2;
            img.color = col;
            return img;
        }

        GameObject Button(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color col, Action onClick)
        {
            var rt = Rect(name, parent);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = Img(rt, col);
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick());
            return rt.gameObject;
        }

        Text Label(Transform parent, string text, int size, Color col, Vector2 pos, Vector2? anchor = null, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Rect("Texte " + text, parent);
            var a = anchor ?? new Vector2(0.5f, 0.5f);
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = new Vector2(align == TextAnchor.UpperLeft ? 0 : align == TextAnchor.UpperRight ? 1 : 0.5f, align == TextAnchor.MiddleCenter ? 0.5f : 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360, size * 1.4f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = col;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;   // pas d'ombre : elle rendait le texte flou (spec 003)
            return t;
        }

        /// <summary>Texte qui rapetisse (jusqu'à min) pour tenir dans sa boîte au lieu de déborder (spec 003, FR-005).</summary>
        static void Fit(Text t, int min)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = t.fontSize;
        }

        static Sprite RoundSprite()
        {
            const int s = 64, r = 22;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, s - r), cy = Mathf.Clamp(y + 0.5f, r, s - r);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
                }
            tex.Apply();
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }
    }
}
