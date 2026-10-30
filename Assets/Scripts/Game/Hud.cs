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
    /// Interface (FR-002, FR-009, FR-014, FR-016, FR-017, FR-021, FR-022) : accueil avec les 4 skateurs, START et
    /// TOP 10 ; score, BEST, combo, bandeaux, barre de vie du boss, textes de points ; écran WIPEOUT ; pause ; son.
    /// Le canevas est rendu par la caméra du jeu, en unités proches de l'écran logique de 390 × 620.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        static readonly Color Cream = Hex("#FFF4E0"), Orange = Hex("#FF7A1A"), Pink = Hex("#FF4D8D"), Night = Hex("#1B1028"), Yellow = Hex("#FFE14D");

        Font font;
        Sprite round;
        RectTransform safe;
        GameObject menu, game, over, pause;
        Text score, best, combo, theme, banner, subBanner, overScore, overBest, record, hint, menuBest, toast, muteLabel;
        RectTransform bossBar, bossFill, bannerRt;
        CanvasGroup toastGroup;
        readonly Image[] cardFrames = new Image[4];
        readonly RectTransform[] cards = new RectTransform[4];
        readonly List<(Text text, float life, Vector3 pos)> floats = new List<(Text, float, Vector3)>();
        readonly Queue<(string, string, Color)> banners = new Queue<(string, string, Color)>();
        float bannerTime = 99, toastTime = 99, comboPop;
        int lastCombo = 1;

        public void Init(Camera cam, Action start, Action<Rider> select, Action top10, Action mute, Action changeRider)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Avenir Next Condensed", "Futura", "Arial Black", "Roboto Condensed", "Roboto", "Arial" }, 32);
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
            BuildMenu(start, select, top10);
            BuildOver(changeRider);

            pause = Panel("Pause", 0.55f);
            Label(pause.transform, "PAUSE", 54, Cream, new Vector2(0, 40));
            Label(pause.transform, "Touche l'écran pour reprendre", 18, Cream, new Vector2(0, -20));

            // bouton son, toujours visible
            var mb = Button(safe, "Son", new Vector2(1, 1), new Vector2(-34, -30), new Vector2(52, 40), new Color(Night.r, Night.g, Night.b, 0.6f), mute);
            muteLabel = Label(mb.transform, "SON", 15, Cream, Vector2.zero);

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
            bannerRt.gameObject.SetActive(false);

            for (int i = 0; i < 14; i++)
            {
                var f = Label(t, "", 19, Cream, Vector2.zero);
                f.gameObject.SetActive(false);
                floats.Add((f, 0, Vector3.zero));
            }
        }

        void BuildMenu(Action start, Action<Rider> select, Action top10)
        {
            menu = Group("Accueil");
            var t = menu.transform;
            Label(t, "PALM", 62, Cream, new Vector2(0, -58), new Vector2(0.5f, 1));
            Label(t, "STREET", 62, Orange, new Vector2(0, -110), new Vector2(0.5f, 1));
            Label(t, "S K A T E   R U N", 15, Pink, new Vector2(0, -150), new Vector2(0.5f, 1));
            menuBest = Label(t, "", 15, Yellow, new Vector2(0, -174), new Vector2(0.5f, 1));

            for (int i = 0; i < 4; i++)
            {
                var r = (Rider)i;
                var pos = new Vector2(i % 2 == 0 ? -76 : 76, i < 2 ? -262 : -382);
                var frame = Button(t, RiderLook.All[i].Name, new Vector2(0.5f, 1), pos, new Vector2(144, 112), Orange, () => select(r));
                cardFrames[i] = frame.GetComponent<Image>();
                var inner = Rect("Fond", frame.transform);
                inner.offsetMin = new Vector2(3, 3); inner.offsetMax = new Vector2(-3, -3);
                Img(inner, new Color(Night.r, Night.g, Night.b, 0.72f)).raycastTarget = false;
                Label(frame.transform, RiderLook.All[i].Name, 15, Cream, new Vector2(0, -44));
                cards[i] = frame.GetComponent<RectTransform>();
            }

            var sb = Button(t, "START", new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(230, 58), Orange, start);
            Label(sb.transform, "START", 30, Night, Vector2.zero);
            var tb = Button(t, "TOP 10", new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(150, 38), new Color(Night.r, Night.g, Night.b, 0.75f), top10);
            Label(tb.transform, "TOP 10", 17, Yellow, Vector2.zero);
            Label(t, "Touche : ollie  ·  double touche en l'air : figure", 12, Cream, new Vector2(0, 22), new Vector2(0.5f, 0));
        }

        void BuildOver(Action changeRider)
        {
            over = Panel("Fin", 0.45f);
            var t = over.transform;
            Label(t, "WIPEOUT !", 58, Hex("#FF5A4D"), new Vector2(0, 190));
            Label(t, "SCORE", 15, Cream, new Vector2(0, 136));
            overScore = Label(t, "0", 46, Cream, new Vector2(0, 104));
            overBest = Label(t, "", 17, Yellow, new Vector2(0, 66));
            record = Label(t, "NOUVEAU RECORD !", 22, Pink, new Vector2(0, 36));
            var cb = Button(t, "Changer", new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(240, 46), new Color(Night.r, Night.g, Night.b, 0.85f), changeRider);
            Label(cb.transform, "CHANGER DE SKATEUR", 17, Cream, Vector2.zero);
            hint = Label(t, "Touche l'écran pour rejouer", 18, Cream, new Vector2(0, -88));
        }

        // ---------- écrans ----------

        public void ShowMenu(Rider selected, int bestScore)
        {
            Show(menu);
            menuBest.text = bestScore > 0 ? "BEST " + bestScore : "";
            Select(selected);
        }

        public void Select(Rider r)
        {
            for (int i = 0; i < 4; i++) cardFrames[i].color = i == (int)r ? Orange : new Color(Cream.r, Cream.g, Cream.b, 0.25f);
        }

        public void ShowGame()
        {
            Show(game);
            banners.Clear(); bannerTime = 99; bannerRt.gameObject.SetActive(false);
            for (int i = 0; i < floats.Count; i++) { floats[i].text.gameObject.SetActive(false); floats[i] = (floats[i].text, 0, Vector3.zero); }
        }

        public void ShowOver(int s, int b, bool newBest)
        {
            over.SetActive(true);
            overScore.text = s.ToString();
            overBest.text = "BEST " + b;
            record.gameObject.SetActive(newBest);
        }

        public void ShowPause(bool on) => pause.SetActive(on);

        void Show(GameObject g)
        {
            menu.SetActive(g == menu);
            game.SetActive(g == game);
            over.SetActive(false);
            pause.SetActive(false);
        }

        public void SetMuted(bool m) => muteLabel.text = m ? "MUET" : "SON";

        public void Toast(string message)
        {
            toast.text = message;
            toastTime = 0;
        }

        /// <summary>Position monde des pieds du skateur d'une carte, et l'échelle à lui donner.</summary>
        public Vector3 CardFeet(int i, out float scale)
        {
            scale = cards[i].lossyScale.x / 0.01f * 1.15f;
            return cards[i].TransformPoint(new Vector3(0, -30, 0));
        }

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
                f.color = col;
                f.gameObject.SetActive(true);
                floats[i] = (f, 1, world);
                return;
            }
        }

        public void Tick(Run run, float dt, float time)
        {
            if (game.activeSelf)
            {
                score.text = run.Score.ToString();
                best.text = "BEST " + Mathf.Max(run.Best, run.Score);
                if (run.Combo != lastCombo) { if (run.Combo > lastCombo) comboPop = 1; lastCombo = run.Combo; }
                combo.text = run.Combo > 1 ? "COMBO x" + run.Combo : "";
                comboPop = Mathf.MoveTowards(comboPop, 0, dt * 4);
                combo.rectTransform.localScale = Vector3.one * (1 + comboPop * 0.35f);
                theme.text = ThemeLook.Of(run.Theme).Name;
                bossBar.gameObject.SetActive(run.Boss != null && run.Boss.State != BossState.Dying);
                if (run.Boss != null) bossFill.anchorMax = new Vector2(Mathf.Clamp01((float)run.Boss.Hp / Tuning.BossHp), 1);

                bannerTime += dt;
                if (bannerTime > 1.7f && banners.Count > 0)
                {
                    var (text, sub, col) = banners.Dequeue();
                    banner.text = text; banner.color = col; subBanner.text = sub ?? "";
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
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.05f, 0.01f, 0.1f, 0.75f);
            sh.effectDistance = new Vector2(size / 16f + 1, -(size / 16f + 1));
            return t;
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
