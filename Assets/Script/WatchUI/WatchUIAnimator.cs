using UnityEngine;
using DG.Tweening;

public class WatchUIAnimator : MonoBehaviour
{
    [SerializeField] private float _openDuration = 0.35f;
    [SerializeField] private float _closeDuration = 0.25f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;

    public void PlayOpen(RectTransform target, System.Action onComplete = null)
    {
        if (target == null) return;

        target.gameObject.SetActive(true);
        target.localScale = Vector3.one * 0.5f;

        var cg = GetOrAddCanvasGroup(target);
        cg.alpha = 0f;

        var seq = DOTween.Sequence();
        seq.Join(target.DOScale(Vector3.one, _openDuration).SetEase(_openEase));
        seq.Join(cg.DOFade(1f, _openDuration * 0.6f));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void PlayClose(RectTransform target, System.Action onComplete = null)
    {
        if (target == null) return;

        var cg = GetOrAddCanvasGroup(target);

        var seq = DOTween.Sequence();
        seq.Join(target.DOScale(Vector3.one * 0.5f, _closeDuration).SetEase(_closeEase));
        seq.Join(cg.DOFade(0f, _closeDuration));
        seq.OnComplete(() =>
        {
            target.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void PlayFlash(UnityEngine.UI.Image flashImage, Color color, System.Action onComplete = null)
    {
        if (flashImage == null) return;

        flashImage.color = new Color(color.r, color.g, color.b, 0f);
        flashImage.gameObject.SetActive(true);

        var seq = DOTween.Sequence();
        seq.Append(flashImage.DOFade(0.7f, 0.08f));
        seq.Append(flashImage.DOFade(0f, 0.2f));
        seq.OnComplete(() =>
        {
            flashImage.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform target)
    {
        var cg = target.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = target.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}