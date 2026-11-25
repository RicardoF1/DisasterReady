using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DisasterReady
{
    /// <summary>
    /// Pantalla final: nota A/B/C animada, puntaje, resumen de decisiones y consejos
    /// educativos según los errores del jugador.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister() => SceneBoot.Register(GameConfig.HouseScene, Spawn);

        static void Spawn()
        {
            if (FindAnyObjectByType<ResultsScreen>() != null) return;
            new GameObject("Pantalla de resultados").AddComponent<ResultsScreen>();
        }

        void Awake() => GameEvents.StateChanged += OnStateChanged;
        void OnDestroy() => GameEvents.StateChanged -= OnStateChanged;

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Resultado) StartCoroutine(Show());
        }

        static Color GradeColor(string grade) =>
            grade == "A" ? UIKit.Good : grade == "B" ? UIKit.Warn : UIKit.Bad;

        IEnumerator Show()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            var gm = GameManager.Instance;
            if (gm == null) yield break;

            var canvas = UIKit.CreateCanvas("Resultados", 50);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var overlay = UIKit.Panel(canvas.transform, "Fondo", new Color(0.02f, 0.03f, 0.06f, 0.72f), false);
            overlay.rectTransform.Stretch();
            overlay.raycastTarget = true;

            var card = UIKit.Panel(overlay.transform, "Tarjeta", UIKit.Ink);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 900f));
            var accentBar = UIKit.Panel(card.transform, "Acento", GradeColor(gm.Grade), true, 0.4f);
            accentBar.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(1440f, 8f));

            // ----- Columna izquierda: nota -----
            Color gc = GradeColor(gm.Grade);
            var circle = UIKit.Picture(card.transform, "Círculo", UIKit.Circle, gc);
            circle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(110f, -80f), new Vector2(300f, 300f));
            var inner = UIKit.Picture(circle.transform, "Interior", UIKit.Circle, new Color(0.07f, 0.09f, 0.13f, 1f));
            inner.rectTransform.Stretch(14f, 14f, 14f, 14f);
            var gradeText = UIKit.Label(circle.transform, "Nota", gm.Grade, 190, gc, TextAnchor.MiddleCenter, FontStyle.Bold);
            gradeText.rectTransform.Stretch();

            var title = UIKit.Label(card.transform, "Título", gm.ResultTitle, 38, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0f, 1f), new Vector2(40f, -410f), new Vector2(440f, 110f));

            UIKit.Label(card.transform, "Etq", "PUNTAJE FINAL", 18, UIKit.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold, false)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(40f, -530f), new Vector2(440f, 30f));
            var scoreText = UIKit.Label(card.transform, "Puntaje", "0", 72, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(40f, -560f), new Vector2(440f, 90f));

            UIKit.Label(card.transform, "Escala", $"A ≥ {GameConfig.GradeA}   ·   B ≥ {GameConfig.GradeB}   ·   evacuar es obligatorio", 16,
                UIKit.TextDim, TextAnchor.MiddleCenter, FontStyle.Normal, false)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(40f, -660f), new Vector2(440f, 30f));

            // ----- Columna derecha: resumen -----
            UIKit.Label(card.transform, "Resumen", "TUS DECISIONES", 22, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold, false)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(540f, -50f), new Vector2(900f, 34f));
            for (int i = 0; i < gm.ResultRows.Count; i++)
            {
                var row = gm.ResultRows[i];
                float y = -96f - i * 40f;
                var dot = UIKit.Picture(card.transform, "Punto", UIKit.Circle, row.Good ? UIKit.Good : UIKit.Bad);
                dot.rectTransform.Place(new Vector2(0f, 1f), new Vector2(544f, y - 10f), new Vector2(16f, 16f));
                UIKit.Label(card.transform, "Etiqueta", row.Label, 22, UIKit.TextMain, TextAnchor.MiddleLeft, FontStyle.Normal, false)
                    .rectTransform.Place(new Vector2(0f, 1f), new Vector2(576f, y), new Vector2(400f, 36f));
                UIKit.Label(card.transform, "Valor", row.Value, 22, row.Good ? UIKit.Good : UIKit.Bad, TextAnchor.MiddleRight, FontStyle.Bold, false)
                    .rectTransform.Place(new Vector2(0f, 1f), new Vector2(980f, y), new Vector2(460f, 36f));
            }

            float tipsTop = -96f - gm.ResultRows.Count * 40f - 26f;
            UIKit.Label(card.transform, "Consejos", "PARA LA PRÓXIMA", 22, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold, false)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(540f, tipsTop), new Vector2(900f, 34f));
            var tips = new System.Text.StringBuilder();
            int count = 0;
            foreach (var tip in gm.Tips)
            {
                if (count++ >= 4) break;
                tips.Append("•  ").Append(tip).Append('\n');
            }
            UIKit.Label(card.transform, "Lista consejos", tips.ToString(), 20, UIKit.TextMain, TextAnchor.UpperLeft, FontStyle.Normal, false)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(544f, tipsTop - 40f), new Vector2(900f, 200f));

            // ----- Botones -----
            var size = new Vector2(380f, 74f);
            UIKit.Button(card.transform, "Reintentar", "REINTENTAR", () => GameManager.Instance?.Restart(), UIKit.Accent, size)
                .GetComponent<RectTransform>().Place(new Vector2(1f, 0f), new Vector2(-440f, 36f), size);
            UIKit.Button(card.transform, "Menú", "MENÚ PRINCIPAL", () => GameManager.Instance?.GoToMenu(), new Color(0.3f, 0.32f, 0.38f), size)
                .GetComponent<RectTransform>().Place(new Vector2(1f, 0f), new Vector2(-40f, 36f), size);

            // ----- Animación -----
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.5f;
                group.alpha = t;
                yield return null;
            }

            t = 0f;
            int target = gm.Score;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 1.4f;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                scoreText.text = Mathf.RoundToInt(target * e).ToString();
                float pop = t < 0.85f ? 0.6f + 0.4f * e : 1f + Mathf.Sin((t - 0.85f) / 0.15f * Mathf.PI) * 0.15f;
                circle.rectTransform.localScale = Vector3.one * pop;
                yield return null;
            }
            scoreText.text = target.ToString();
            circle.rectTransform.localScale = Vector3.one;
        }
    }
}
