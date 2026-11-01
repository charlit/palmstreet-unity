using System;
using PalmStreet.Core;
using UnityEngine;
using UnityEngine.UI;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Aperçu d'une ville avant de la jouer, tutoriel des gestes (animé, au premier JOUER et depuis les réglages en
    /// partie) et annonce en grand des figures enchaînées.
    /// </summary>
    public sealed partial class Hud
    {
        GameObject preview, tutorial;
        Text previewTitle, previewCity, previewInfo, shout;
        int previewLevel;
        float shoutTime = 9;
        RectTransform[] fingers;
        Action tutorialDone;

        // ---------- aperçu d'une ville ----------

        void BuildPreview(Action<int> pick)
        {
            preview = Panel("Aperçu de la ville", 0.65f);
            preview.GetComponent<Image>().raycastTarget = true;
            var box = Rect("Fiche", preview.transform);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(310, 330);
            Img(box, new Color(Night.r, Night.g, Night.b, 0.96f)).raycastTarget = false;
            previewTitle = Label(box, "", 16, Pink, new Vector2(0, -28), new Vector2(0.5f, 1));
            previewCity = Label(box, "", 30, Cream, new Vector2(0, -62), new Vector2(0.5f, 1));
            previewInfo = Label(box, "", 15, Cream, new Vector2(0, -150), new Vector2(0.5f, 1));
            previewInfo.rectTransform.sizeDelta = new Vector2(280, 130);
            previewInfo.horizontalOverflow = HorizontalWrapMode.Wrap;
            var go = Button(box, "Jouer la ville", new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(220, 50), Orange, () => { preview.SetActive(false); pick(previewLevel); });
            Label(go.transform, "JOUER", 24, Night, Vector2.zero);
            var close = Button(box, "Fermer", new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(220, 36), new Color(1, 1, 1, 0.12f), () => preview.SetActive(false));
            Label(close.transform, "FERMER", 15, Cream, Vector2.zero);
        }

        public void ShowPreview(int n)
        {
            var d = Levels.Get(n);
            var p = progress();
            previewLevel = n;
            previewTitle.text = $"NIVEAU {n}" + (d.Boss != BossKind.None ? " · BOSS : " + Game.BossName(d.Boss) : "");
            previewCity.text = d.City;
            string star = p.Stars[n - 1] ? "RÉUSSI" : "à faire";
            string info = $"Meilleur score : {(p.IsCleared(n) ? p.Best[n - 1].ToString() : "—")}\nDéfi : {d.ChallengeText} ({star})";
            if (d.Unlocks >= 0) info += $"\nDébloque : {Tuning.TrickNames[d.Unlocks]}";
            if (d.NewObstacle != null) info += $"\n{d.NewObstacle}";
            previewInfo.text = info;
            preview.SetActive(true);
        }


        // ---------- tutoriel des gestes ----------

        static readonly string[] Gestures =
        {
            "TOUCHE : ollie",
            "EN L'AIR, GLISSE : une figure par direction",
            "2 DOIGTS : 180, passe en fakie (figures ×1,5)",
            "SUR UN RAIL, GLISSE : 8 grinds (50-50, boardslide, smith…)\nTOUCHE pour sauter du rail",
        };

        void BuildTutorial()
        {
            tutorial = Panel("Comment jouer", 0.9f);
            tutorial.GetComponent<Image>().raycastTarget = true;
            Label(tutorial.transform, "COMMENT JOUER", 32, Cream, new Vector2(0, -90), new Vector2(0.5f, 1));
            fingers = new RectTransform[Gestures.Length * 2];
            for (int i = 0; i < Gestures.Length; i++)
            {
                var row = Rect("Geste " + (i + 1), tutorial.transform);
                row.anchorMin = row.anchorMax = new Vector2(0.5f, 1);
                row.anchoredPosition = new Vector2(0, -180 - i * 100);
                row.sizeDelta = new Vector2(340, 84);
                var pad = Rect("Écran", row);
                pad.anchorMin = pad.anchorMax = new Vector2(0, 0.5f);
                pad.anchoredPosition = new Vector2(42, 0);
                pad.sizeDelta = new Vector2(76, 76);
                Img(pad, new Color(1, 1, 1, 0.1f)).raycastTarget = false;
                for (int f = 0; f < 2; f++)
                {
                    var dot = Rect("Doigt", pad);
                    dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 0.5f);
                    dot.sizeDelta = new Vector2(20, 20);
                    var img = Img(dot, Yellow);
                    img.pixelsPerUnitMultiplier = 0.3f;
                    img.raycastTarget = false;
                    fingers[i * 2 + f] = dot;
                    dot.gameObject.SetActive(f == 0 || i == 2);
                }
                var text = Label(row, Gestures[i], 15, Cream, new Vector2(54, 0), null, TextAnchor.MiddleLeft);
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0, 0.5f);
                text.rectTransform.pivot = new Vector2(0, 0.5f);
                text.rectTransform.anchoredPosition = new Vector2(92, 0);
                text.rectTransform.sizeDelta = new Vector2(244, 80);
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            var ok = Button(tutorial.transform, "C'est parti", new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(240, 54), Orange, () =>
            {
                tutorial.SetActive(false);
                var done = tutorialDone; tutorialDone = null; done?.Invoke();
            });
            Label(ok.transform, "C'EST PARTI", 24, Night, Vector2.zero);
        }

        /// <summary>Montre les gestes ; done est appelé à la fermeture.</summary>
        public void ShowTutorial(Action done = null)
        {
            tutorialDone = done;
            tutorial.SetActive(true);
            tutorial.transform.SetAsLastSibling();
        }

        public bool TutorialVisible => tutorial.activeSelf;
        public void HideTutorial() => tutorial.SetActive(false);
        /// <summary>Une fenêtre couvre la carte : le petit skateur (dessiné au-dessus de l'interface) se cache.</summary>
        public bool OverlayOpen => tutorial.activeSelf || preview.activeSelf;

        void TickTutorial(float time)
        {
            if (!tutorial.activeSelf) return;
            float c = Mathf.Repeat(time, 1.4f) / 1.4f, ease = c < 0.6f ? Mathf.SmoothStep(0, 1, c / 0.6f) : 1;
            float press = c < 0.2f ? 1 - c / 0.2f : 0;   // le doigt qui se pose
            fingers[0].localScale = Vector3.one * (0.7f + 0.6f * (c < 0.3f ? Mathf.Sin(c / 0.3f * Mathf.PI) : 0));   // toucher
            fingers[2].anchoredPosition = new Vector2(-18, -18) + new Vector2(36, 36) * ease;                            // glissé ↗
            fingers[2].localScale = Vector3.one * (0.8f + 0.3f * press);
            fingers[4].anchoredPosition = new Vector2(-12, 0); fingers[5].anchoredPosition = new Vector2(12, 0);       // deux doigts
            float two = 0.7f + 0.6f * (c < 0.3f ? Mathf.Sin(c / 0.3f * Mathf.PI) : 0);
            fingers[4].localScale = fingers[5].localScale = Vector3.one * two;
            fingers[6].anchoredPosition = new Vector2(0, -20) + new Vector2(0, 40) * ease;                              // glissé sur rail
            for (int i = 0; i < fingers.Length; i += 2)
                fingers[i].GetComponent<Image>().color = new Color(Yellow.r, Yellow.g, Yellow.b, c < 0.85f ? 1 : (1 - c) / 0.15f);
        }

        // ---------- figures enchaînées ----------

        void BuildShout()
        {
            shout = Label(game.transform, "", 34, Yellow, new Vector2(0, 150));
            shout.gameObject.SetActive(false);
        }

        /// <summary>Figure annoncée en grand à partir d'un combo de 3.</summary>
        public void Shout(string trick, int combo)
        {
            shout.text = trick.TrimEnd(' ', '!') + "  x" + combo;
            shout.color = combo >= 8 ? Pink : combo >= 5 ? Orange : Yellow;
            shout.gameObject.SetActive(true);
            shoutTime = 0;
        }

        void TickShout(float dt)
        {
            if (!shout.gameObject.activeSelf) return;
            shoutTime += dt;
            float pop = shoutTime < 0.12f ? Mathf.Lerp(1.7f, 1, shoutTime / 0.12f) : 1;
            shout.rectTransform.localScale = Vector3.one * pop;
            shout.canvasRenderer.SetAlpha(Mathf.Clamp01((1.1f - shoutTime) * 3));
            if (shoutTime > 1.1f) shout.gameObject.SetActive(false);
        }
    }
}
