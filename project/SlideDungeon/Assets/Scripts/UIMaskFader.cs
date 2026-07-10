using UnityEngine;
using DG.Tweening;
using System.Collections;
using System;

public class UIMaskFader : MonoBehaviour
{
    #region private変数
    [SerializeField] private GameObject unMask; //インスペクターで子要素のUnmaskをセット
    private bool isExiting;                     //遷移中フラグ（二重遷移防止用）
    #endregion

    #region Unityイベント関数
    //シーン開始時にリセット
    private void OnEnable()
    {
        isExiting = false;
    }

    #endregion

    #region フェード処理
    /// <summary>
    /// 視界が開く演出（フェードイン）
    /// </summary>
    /// <param name="duration">演出時間</param>
    /// <param name="onComplete">完了時に実行したい処理</param>
    public IEnumerator PlayFadeIn(float duration, Action onComplete = null)
    {
        //実行中のTweenがあれば停止（バグ防止）
        unMask.transform.DOKill();

        //演出開始前にスケールを完全に0（真っ暗）にする
        unMask.transform.localScale = Vector3.zero;

        //画面が切り替わった直後の違和感を消すための短い待機
        yield return new WaitForSecondsRealtime(0.3f);

        //拡大アニメーション（23倍まで大きくして画面全体を見せる）
        unMask.transform.DOScale(23f, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)                         //ポーズ中（Time.timeScale = 0）でも動くようにする
            .OnComplete(() => onComplete?.Invoke()); //終わったら登録された処理を実行

        //アニメーション時間分だけコルーチンを待機
        yield return new WaitForSecondsRealtime(duration);
    }

    /// <summary>
    /// 視界を閉じる演出（フェードアウト）
    /// </summary>
    /// <param name="duration">演出時間</param>
    /// <param name="onComplete">完了時に実行したい処理（シーン遷移など）</param>
    public IEnumerator PlayFadeOut(float duration, Action onComplete = null)
    {
        //既に遷移中（ボタン連打など）なら何もしない
        if (isExiting) yield break;
        isExiting = true;

        unMask.transform.DOKill();

        //縮小アニメーション（スケールを0にして画面を隠す）
        unMask.transform.DOScale(0f, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => {
                isExiting = false;    //フラグをリセット
                onComplete?.Invoke(); //アニメーション終了後にシーン遷移などを実行
            });

        //アニメーション時間分だけコルーチンを待機
        yield return new WaitForSecondsRealtime(duration);
    }
    #endregion

    #region マスクの初期サイズ設定
    public void SetScale(float scale)
    {
        unMask.transform.localScale = Vector3.one * scale;
    }
    #endregion
}