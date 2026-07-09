using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.Presentation
{
    /// <summary>
    /// 코루틴 연출 헬퍼 — 외부 트윈 라이브러리 금지 규약에 따라 lerp 만 사용한다 (task-108 제약).
    /// 게임 규칙은 이 코루틴들의 완료를 기다리지 않는다 (표현 전용).
    /// </summary>
    public static class PresentationTween
    {
        /// <summary>RectTransform anchoredPosition 보간.</summary>
        public static IEnumerator MoveAnchored(RectTransform target, Vector2 from, Vector2 to, float duration)
        {
            if (target == null) yield break;
            float t = 0f;
            target.anchoredPosition = from;
            while (t < duration)
            {
                t += Time.deltaTime;
                target.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            target.anchoredPosition = to;
        }

        /// <summary>Graphic(Image/TMP) alpha 보간.</summary>
        public static IEnumerator FadeAlpha(Graphic target, float from, float to, float duration)
        {
            if (target == null) yield break;
            float t = 0f;
            SetAlpha(target, from);
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(target, Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(target, to);
        }

        /// <summary>localScale 보간 (팝 연출).</summary>
        public static IEnumerator ScaleTo(Transform target, Vector3 from, Vector3 to, float duration)
        {
            if (target == null) yield break;
            float t = 0f;
            target.localScale = from;
            while (t < duration)
            {
                t += Time.deltaTime;
                target.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            target.localScale = to;
        }

        /// <summary>정수 카운트업 — setter 콜백으로 표시를 위임하고 마지막 프레임에 정확히 to 를 보장한다.</summary>
        public static IEnumerator CountUpInt(int from, int to, float duration, Action<int> setter)
        {
            if (setter == null) yield break;
            float t = 0f;
            setter(from);
            while (t < duration)
            {
                t += Time.deltaTime;
                setter(Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration))));
                yield return null;
            }
            setter(to); // 최종값 정확성 보장 (task-108 설계 26단계)
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }
    }
}
