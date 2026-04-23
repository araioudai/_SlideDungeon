using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using static UnityEngine.GraphicsBuffer;
using static InputPlayerManager;

public class TutorialManager : MonoBehaviour
{
    #region シングルトン
    public static TutorialManager Instance { get; private set; } //他のスクリプトからInstanceでアクセスできるようにする
    #endregion

    #region 列挙対
    public enum TutorialState
    {
        NONE,       //何もしない状態
        SHOW_SWIPE, //お手本（指の動き）を見せる状態
        WAIT_SWIPE, //ユーザーの入力を待つ状態
        SHOW_GOAL,  //成功時の演出状態
        COMPLETE    //全て完了した状態
    }

    #endregion

    #region private変数
    [Header("チュートリアル用手プレファブ")]
    [SerializeField] private GameObject hand;
    [Header("チュートリアル用テキスト背景オブジェクト")]
    [SerializeField] private GameObject tutorialBack;
    [Header("指の移動にかかる時間とフェードにかかる時間")]
    [SerializeField] private float moveTime = 1.0f;                          //移動にかかる時間（秒）
    [SerializeField] private float fadeTime = 0.5f;                          //フェードにかかる時間（秒）
    [Header("スワイプチュートリアルの微調整")]
    [SerializeField] private Vector3 handOffset = new Vector3(1f, 0.5f, 0f); //プレイヤーからのずれ（右上）
    [SerializeField] private float swipeMoveDistance = 5.0f;                 //1回のスワイプ移動距離
    [SerializeField] private int swipeLoops = 3;                             //スワイプを繰り返す回数
    [Header("ゴールチュートリアル用パネル")]
    [SerializeField] private GameObject goalMaskPanel;                       //全体を暗くする、薄い黒のパネル

    private Text tutorialText;            //チュートリアル用テキスト
    private TutorialState state;          //現在のチュートリアル状況
    private Sequence swipeSequence;       //アニメーション制御用
    private GameObject handInstance;      //生成した実体を保持する変数
    private Vector3 startPos;             //手のアニメーション開始場所
    private Vector3 endPos;               //手のアニメーション終了場所
    private bool isReady = false;         //準備完了フラグ
    private bool 
        isGoalEffectStarted = false;      //演出が二重に走らないためのフラグ
    private int originalGoalSortingOrder; //元のSortingOrderを保存する変数
    private Tween delayedCallTween;       //DelayedCallを止めるために保持

    #endregion

    #region Get関数

    /// <summary>
    /// チュートリアル状況ゲット用
    /// </summary>
    /// <returns>現在のチュートリアル状態</returns>
    public TutorialState GetState() {  return state; }

    #endregion

    #region Unityイベント関数
    void Awake()
    {
        //シングルトン管理
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); //既にInstanceがあれば自分を破棄
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Init();
    }

    void Update()
    {
        if (!isReady || !StageIndex.Instance.GetFirst()) return; //準備ができるまでは何もしない

        TutorialSituation();
    }

    void OnDestroy()
    {
        //オブジェクト破棄時にすべてのTweenを確実に止める
        swipeSequence?.Kill();
        delayedCallTween?.Kill();
        
        //全てのDOTweenをこのオブジェクトに関連付けて止める場合
        DOTween.Kill(this);
    }

    #endregion

    #region Start呼び出し関数
    /// <summary>
    /// 初期化
    /// </summary>
    void Init()
    {
        if (!StageIndex.Instance.GetFirst()) { return; }

        tutorialText = tutorialBack.GetComponentInChildren<Text>();
        
        //プレイヤーの位置を基準に、手のアニメーション座標を設定するコルーチンを開始
        StartCoroutine(SetupHandPositions());
    }

    /// <summary>
    /// ステージ番号からチュートリアルの指の向きを取得
    /// </summary>
    /// <param name="index">ステージ番号</param>
    /// <returns>指の向き</returns>
    Vector2 Direction(int index)
    {
        switch (index)
        {
            case 2: 
            case 7:
                swipeMoveDistance = 2;
                return Vector2.right;
            default:
                return Vector2.up;
        }
    }

    /// <summary>
    /// プレイヤーの位置を基準に、手のアニメーション座標を設定する
    /// </summary>
    IEnumerator SetupHandPositions()
    {
        GameObject playerObj = null;
        //プレイヤーが生成されるまで待機
        while (playerObj == null)
        {
            playerObj = GameObject.FindWithTag("Player");
            yield return null;
        }

        //そのステージで必要な方向を取得
        Vector2 targetDir = Direction(StageIndex.Instance.GetIndex());

        //プレイヤーの現在位置から少しずらした位置を開始点にする
        startPos = playerObj.transform.position + handOffset;

        //正解の方向に向かってデモの終点を決める
        endPos = startPos + (Vector3)targetDir * swipeMoveDistance;

        //座標が決まったら準備完了
        isReady = true;
        state = TutorialState.SHOW_SWIPE;
    }
    #endregion

    #region Update呼出し関数
    /// <summary>
    /// 現在のチュートリアル状況
    /// </summary>
    void TutorialSituation()
    {
        switch (state)
        {
            case TutorialState.SHOW_SWIPE:
                ShowSwipeDemo();
                break;

            case TutorialState.WAIT_SWIPE:
                CheckSwipe();
                break;
            case TutorialState.SHOW_GOAL:
                ShowGoal();
                break;
        }
    }

    /// <summary>
    /// 操作を見せる処理
    /// </summary>
    void ShowSwipeDemo()
    {
        tutorialBack.SetActive(true);

        //指のオブジェクトがなければ生成する
        if (handInstance == null)
        {
            handInstance = Instantiate(hand, transform);
            handInstance.SetActive(false);
        }

        //アニメーション再生中なら二重に実行しない
        if (swipeSequence != null && swipeSequence.IsActive()) { return; }

        handInstance.SetActive(true);
        CanvasGroup group = handInstance.GetComponent<CanvasGroup>() ?? handInstance.AddComponent<CanvasGroup>();

        //デモの開始前にプレイヤーと指を初期位置にリセットする
        //プレイヤーは元の位置（手の位置からオフセット分戻した場所）
        Player.Instance.transform.position = startPos - handOffset;
        handInstance.transform.position = startPos;
        group.alpha = 0;

        //現在のステージに応じた正解の方向を取得
        Vector2 targetDir = Direction(StageIndex.Instance.GetIndex());
        tutorialText.fontSize = 75;
        tutorialText.text = "スワイプで動かそう";

        //Sequenceを初期化
        swipeSequence = DOTween.Sequence();
        swipeSequence?.Kill();                                  //既存のがあれば消す
        swipeSequence = DOTween.Sequence().SetLink(gameObject); //SetLinkを追加

        //まず指をフェードイン
        swipeSequence.Append(group.DOFade(1f, fadeTime));

        //(設定回数 - 1) 回分、スワイプして「戻る」アニメーションを繰り返す
        if (swipeLoops > 1)
        {
            Sequence loopPart = DOTween.Sequence()
                .Append(handInstance.transform.DOMove(endPos, moveTime).SetEase(Ease.OutQuad))
                .Append(handInstance.transform.DOMove(startPos, 0f)); //戻る動作

            swipeSequence.Append(loopPart.SetLoops(swipeLoops - 1, LoopType.Restart));
        }

        //最後の1回：移動しながらフェードアウト
        swipeSequence.Append(handInstance.transform.DOMove(endPos, moveTime).SetEase(Ease.OutQuad))
                     .Join(group.DOFade(0f, moveTime)) //移動と同時に消える
                     .AppendCallback(() => {
                         //指が消えた瞬間にプレイヤーのお手本移動を開始
                         Player.Instance.TutorialMove(targetDir);
                     });

        //待機と終了処理
        //プレイヤーが動いている間、少し待機（間を持たせる）
        swipeSequence.AppendInterval(1.5f)
            .OnComplete(() => {
                //一通り見せ終わったら「ユーザー入力待ち」へ切り替える
                if (Player.Instance != null) { Player.Instance.TutorialInit(); }
                StopSwipeDemo();
                state = TutorialState.WAIT_SWIPE;
            });
    }

    /// <summary>
    /// ステートが切り替わる時に呼ぶ処理
    /// </summary>
    private void StopSwipeDemo()
    {
        if (swipeSequence != null)
        {
            swipeSequence.Kill();
            swipeSequence = null;
        }

        //実体の方を非表示にする
        if (handInstance != null)
        {
            handInstance.SetActive(false);
        }
    }

    /// <summary>
    /// 操作が正しく行われたかチェック処理
    /// </summary>
    /// <summary>
    /// 操作が正しく行われたかチェック処理
    /// </summary>
    void CheckSwipe()
    {
        tutorialText.fontSize = 100;
        tutorialText.text = "やってみよう！";

        // 現在の入力方向を取得
        InputPlayerManager.OperationType input = InputPlayerManager.Instance.GetDirection();

        // 入力がない(NONE)ときは何もしない
        if (input == InputPlayerManager.OperationType.NONE) return;

        // そのステージで期待されている正解の方向
        Vector2 correctDir = Direction(StageIndex.Instance.GetIndex());
        bool isCorrect = false;

        // 入力された方向がチュートリアルの指示と合っているか判定
        if (correctDir == Vector2.up && input == InputPlayerManager.OperationType.UP) isCorrect = true;
        if (correctDir == Vector2.right && input == InputPlayerManager.OperationType.RIGHT) isCorrect = true;

        if (isCorrect)
        {
            //入力判定を即座に止めるため、一時的に NONE にする
            state = TutorialState.NONE;

            //成功時
            Player.Instance.TutorialMove(correctDir);
            tutorialText.fontSize = 100;
            tutorialText.text = "Good!";

            //成功してプレイヤーが少し動いた後、1秒待ってからゴール紹介へ
            //変数に代入して、破棄時にKillできるようにする
            delayedCallTween = DOVirtual.DelayedCall(1.0f, () => {
                state = TutorialState.SHOW_GOAL;
            }).SetLink(gameObject);
        }
        else
        {
            //失敗時
            //判定が何度も走らないように、一時的にステートを NONE にする
            state = TutorialState.NONE;

            //正解の方向に応じてテキスト表示
            string dirName = (correctDir == Vector2.up) ? "上" : "右";
            tutorialText.fontSize = 75;
            tutorialText.text = $"惜しい！{dirName}方向に\nスワイプしてみよう！";

            //テキストを揺らす演出
            tutorialText.transform.DOShakePosition(0.5f, 10f);

            //プレイヤーを初期位置に戻す
            Player.Instance.TutorialInit();

            //1.5秒待ってから、もう一度お手本（SHOW_SWIPE）に戻す
            delayedCallTween = DOVirtual.DelayedCall(1.5f, () => {
                state = TutorialState.SHOW_SWIPE;
            }).SetLink(gameObject);
        }
    }

    /// <summary>
    /// ゴールを見せる処理
    /// </summary>
    void ShowGoal()
    {
        if (isGoalEffectStarted) return;
        isGoalEffectStarted = true;

        //ゴールのオブジェクトを探す
        GameObject goalObj = GameObject.FindGameObjectWithTag("Goal");

        if (goalObj != null)
        {
            //カメラのターゲットをゴールに変更
            CameraManager.Instance.SetTarget(goalObj.transform);

            //全体を暗くするパネルを表示
            if (goalMaskPanel != null) goalMaskPanel.SetActive(true);

            //ゴールを前面に浮かび上がらせる（SortingOrderを高くする）
            SpriteRenderer sr = goalObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                originalGoalSortingOrder = sr.sortingOrder;     //元の値を保存
                string originalLayerName = sr.sortingLayerName; //元のレイヤー名を保存

                sr.sortingLayerName = "GameUI";                 //レイヤー変更
                sr.sortingOrder = 10000;                        //十分高い値を設定して、マスクパネルより前に出す
            }

            //ゴールを目立たせる演出（DOTweenで大きくしたり赤くしたり）
            //周りを赤くする代わりに、ゴール自体を赤く点滅させ、少し大きくする
            Sequence goalSeq = DOTween.Sequence();

            tutorialText.fontSize = 70;
            tutorialText.text = "赤の場所がゴールだよ！\nそこを目指そう！";

            //ゴール本体を「ピクッ」と動かして色を変える（3回繰り返す）
            goalSeq.Append(goalObj.transform.DOScale(1.5f, 0.3f).SetEase(Ease.OutQuad)) // 少し大きく
                   .Join(sr.DOColor(Color.yellow, 0.3f))                               // 白い部分を黄色く
                   .Append(goalObj.transform.DOScale(1.0f, 0.3f).SetEase(Ease.InQuad))  // 元のサイズへ
                   .Join(sr.DOColor(Color.white, 0.3f))                                // 元の色へ
                   .SetLoops(3, LoopType.Restart)
                   .OnStepComplete(() => {
                       // 各ループの終わりに波紋エフェクトを生成（視認性向上）
                       if (goalObj != null) CreateRipple(goalObj, sr.sortingOrder);
                   });

            // 演出開始時にも1回波紋を出す
            CreateRipple(goalObj, sr.sortingOrder);

            //説明が終わったら完了へ（3秒後に次のテキスト、さらに1.5秒後に終了）
            DOVirtual.DelayedCall(3.0f, () =>
            {
                if (tutorialText == null) { return; }

                tutorialText.text = "さあ冒険の始まりだ！";

                DOVirtual.DelayedCall(1.5f, () => {
                    //自分自身が破棄されてたら中断
                    if (this == null) { return; }

                    //チュートリアル終了時にカメラをプレイヤーに戻す
                    if (CameraManager.Instance != null) { CameraManager.Instance.ResetTarget(); }

                    //パネルを非表示にし、ゴールのSortingOrderを元に戻す
                    if (goalMaskPanel != null) goalMaskPanel.SetActive(false);
                    
                    //元に戻す処理
                    if (sr != null)
                    {
                        sr.sortingLayerName = "Game";               //元のレイヤー名に戻す
                        sr.sortingOrder = originalGoalSortingOrder; //元の値に戻す
                    }

                    tutorialText.text = "";              //テキストを消す
                    tutorialBack.SetActive(false);       //テキスト背景を消す
                    Player.Instance.TutorialInit();      //プレイヤーの場所を初期位置に
                    StageIndex.Instance.SetFirst(false); //最初のプレイをオフに
                    state = TutorialState.COMPLETE;      //チュートリアル終了
                }).SetLink(gameObject);
            }).SetLink(gameObject);
        }
        else
        {
            //ゴールが見つからない場合の安全策
            state = TutorialState.COMPLETE;
        }
    }

    /// <summary>
    /// ゴールの背後に広がる波紋エフェクトを生成する（使い捨てオブジェクト）
    /// </summary>
    /// <param name="target">コピー元のオブジェクト</param>
    /// <param name="sortingOrder">基準となる描画順</param>
    void CreateRipple(GameObject target, int sortingOrder)
    {
        if (target == null) return;

        //ゴールを複製して波紋のベースにする
        GameObject ring = Instantiate(target, target.transform.position, Quaternion.identity);

        //複製されたオブジェクトから不要な機能を削除（当たり判定やスクリプト）
        //これを忘れると、チュートリアル中に予期せぬ衝突判定が発生する可能性がある
        Destroy(ring.GetComponent<Collider2D>());
        var tutorialScript = ring.GetComponent<TutorialManager>();
        if (tutorialScript != null) Destroy(tutorialScript);

        //開始時の大きさは現在のターゲットに合わせる
        ring.transform.localScale = target.transform.localScale;

        SpriteRenderer ringSr = ring.GetComponent<SpriteRenderer>();
        if (ringSr != null)
        {
            //波紋もマスクより手前に表示
            ringSr.sortingLayerName = "GameUI";
            ringSr.sortingOrder = sortingOrder - 1;  //本体のすぐ後ろ
            ringSr.color = new Color(1, 1, 1, 0.5f); //半透明から開始

            //波紋のアニメーション
            //大きく広がりながら、透明になって消える
            ring.transform.DOScale(ring.transform.localScale.x * 2.5f, 0.6f).SetEase(Ease.OutCubic);
            ringSr.DOFade(0, 0.6f).OnComplete(() => {
                //アニメーション終了後にメモリ確保のため削除
                if (ring != null) Destroy(ring);
            });
        }
        else
        {
            //Rendererがない場合は即座に削除
            Destroy(ring);
        }
    }

    #endregion

    #region 外部呼出し関数

    /// <summary>
    /// チュートリアルのスタート
    /// </summary>
    public IEnumerator TutorialStart()
    {
        state = TutorialState.SHOW_SWIPE;

        //state が COMPLETE（完了）になるまで、コルーチンをループさせて待機する
        while (state != TutorialState.COMPLETE)
        {
            yield return null; //1フレーム待機してループ
        }
    }

    #endregion
}
