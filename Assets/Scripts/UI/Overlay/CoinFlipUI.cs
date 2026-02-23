using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoinFlipUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Images")]
    [SerializeField] private Image coinFrameImage;   // 14장 프레임 표시할 Image
    [SerializeField] private Sprite decidingImage;   // "선공을 정합니다" (코인 프레임 자리에 잠깐 보여줄 용도)
    [SerializeField] private Image resultSideImage;  // HEAD/TAIL
    [SerializeField] private Image roleImage;        // 선공/후공

    [Header("Sprites")]
    [SerializeField] private Sprite[] coinFrames;    // 14장 순서대로
    [SerializeField] private float frameFps = 12f;   // 12fps면 12장=1초

    [SerializeField] private Sprite headSprite;
    [SerializeField] private Sprite tailSprite;
    [SerializeField] private Sprite isFirstSprite;
    [SerializeField] private Sprite isSecondSprite;

    [Header("Timings")]
    [SerializeField] private float decidingHold = 0.35f;   // "선공을 정합니다" 보여주는 시간
    [SerializeField] private float resultHold = 0.8f;      // 결과(HEAD/TAIL + 선공/후공) 유지 시간

    private Coroutine _co;

    /// <summary>
    /// 코인 플립 연출 시작
    /// isHeads : 동전 결과(헤드/테일)
    /// amIStarter : 내 로컬 플레이어가 선공이면 true, 후공이면 false
    /// </summary>
    public void Play(bool isHeads, bool amIStarter)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(PlaySequence(isHeads, amIStarter));
    }

    public void Hide()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
        if (panelRoot != null) panelRoot.SetActive(false);
        else gameObject.SetActive(false);
    }

    /// <summary>
    /// CoinFlip 전체 길이(초). GameStateManager CoinFlip 유지시간 맞출 때 쓰면 좋음.
    /// </summary>
    public float GetTotalDuration()
    {
        float frameDt = (frameFps <= 0f) ? (1f / 12f) : (1f / frameFps);
        float flipLen = (coinFrames != null ? coinFrames.Length : 0) * frameDt;
        return decidingHold + flipLen + resultHold;
    }

    private IEnumerator PlaySequence(bool isHeads, bool amIStarter)
    {
        // 켜기
        if (panelRoot != null) panelRoot.SetActive(true);
        else gameObject.SetActive(true);

        // 초기 숨김
        if (resultSideImage != null) resultSideImage.gameObject.SetActive(false);
        if (roleImage != null) roleImage.gameObject.SetActive(false);

        // 1) "선공을 정합니다" 표시 (coinFrameImage 자리에)
        if (coinFrameImage != null && decidingImage != null)
            coinFrameImage.sprite = decidingImage;

        yield return new WaitForSeconds(decidingHold);

        // 2) 코인 프레임 애니(14장)
        if (coinFrames == null || coinFrames.Length == 0 || coinFrameImage == null)
        {
            // 프레임이 없으면 그냥 스킵하고 결과로
            yield return null;
        }
        else
        {
            float frameDt = (frameFps <= 0f) ? (1f / 12f) : (1f / frameFps);

            for (int i = 0; i < coinFrames.Length; i++)
            {
                coinFrameImage.sprite = coinFrames[i];
                yield return new WaitForSeconds(frameDt);
            }
        }

        // 3) 결과 표시 (HEAD/TAIL + 선공/후공)
        if (resultSideImage != null)
        {
            resultSideImage.sprite = isHeads ? headSprite : tailSprite;
            resultSideImage.gameObject.SetActive(true);
        }

        if (roleImage != null)
        {
            roleImage.sprite = amIStarter ? isFirstSprite : isSecondSprite;
            roleImage.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(resultHold);

        // 4) 끝나면 숨김 (상태 전환은 GameStateManager가 함)
        Hide();
    }
}