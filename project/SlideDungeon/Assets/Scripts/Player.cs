using DG.Tweening;
//using PlayFab.DataModels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static InputPlayerManager;

public class Player : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static Player Instance { get; private set; }
    #endregion

    #region private変数
    [Header("壁との当たり判定用")]
    [SerializeField] private LayerMask wallCheckLayer;    //壁当たり判定用のレイヤー
    [Header("ポータル1との当たり判定用")]
    [SerializeField] private LayerMask portalFirstLayer;  //ポータル1当たり判定用のレイヤー
    [Header("ポータル2との当たり判定用")]
    [SerializeField] private LayerMask portalSecondLayer; //ポータル2当たり判定用のレイヤー
    [Header("ゴールとの当たり判定用")]
    [SerializeField] private LayerMask goalLayer;         //ゴール当たり判定用のレイヤー
    [Header("落とし穴との当たり判定用")]
    [SerializeField] private LayerMask holeLayer;         //落とし穴当たり判定用のレイヤー
    [Header("プレイヤーの移動スピード（初速）")]
    [SerializeField] private float initialSpeed;          //プレイヤーの初速
    [Header("プレイヤーの最高速度")]
    [SerializeField] private float maxSpeed;              //プレイヤーの最高速度
    [Header("プレイヤーの加速度")]
    [SerializeField] private float acceleration;          //プレイヤーの加速度
    [Header("先行入力移動タイマーセット:float")]
    [SerializeField] private float bufferTime;            //先行入力移動タイマーセット用
    [Header("スライムの通過跡用")]
    [SerializeField] private GameObject trace;            //足跡

    private Tilemap floorTilemap;     //タイルマップの参照（跡の生成位置計算用）
    private Vector3Int lastTraceCell; //最後に跡を置いたマスの座標

    private Animator m_player;        //プレイヤーのアニメーション用
    private Vector3 moveDirection;    //敵の進む向きに変える用
    private Vector3 firstPos;         //最初にいた場所
    private float moveBufferTimer;    //先行入力移動タイマー(確保時間)
    private bool moveBuffered;        //先行入力移動用フラグ
    private bool isMove;              //移動スタートフラグ
    private bool movable;             //移動可能かのフラグ
    private bool isSound;             //サウンド一回だけ再生用
    private bool isJump;              //ジャンプ中かのフラグ
    //private bool canJump;
    private bool expansion;           //拡大するかどうか
    private bool isOverUIForbidden;   //特定のUIに乗っている間はtrue
    private float inputLockTimer;     //入力禁止時間
    private float currentSpeed;       //現在の移動速度
    private const float INPUT_LOCK_DURATION = 0.2f; //シーン開始から0.2秒は入力無効
    private int shakeCount;           //シェイクしていい数カウント


    #endregion

    #region ボタン押下処理
    /// <summary>
    /// ボタン押下時UI判別用
    /// </summary>
    /// <param name="value">特定のUIに乗っているか</param>
    public void SetOverUIForbidden(bool value)
    {
        isOverUIForbidden = value;
    }
    #endregion

    #region 外部呼出し関数
    /// <summary>
    /// 初期位置に戻す
    /// </summary>
    public void TutorialInit() {
        transform.position = firstPos;

        //座標を戻した時に、跡の判定基準もリセットする
        if (floorTilemap != null)
        {
            lastTraceCell = floorTilemap.WorldToCell(transform.position);
        }
    }

    /// <summary>
    /// TutorialManagerから強制的に移動を開始させるためのメソッド
    /// </summary>
    /// <param name="direction"></param>
    public void TutorialMove(Vector3 direction)
    {
        moveDirection = direction;
        currentSpeed = initialSpeed;
        isMove = true;
        m_player.SetBool("isMove", true);

        //方向に応じてアニメーションのトリガーも引く
        if (direction == Vector3.up) m_player.SetTrigger("moveUp");
        else if (direction == Vector3.down) m_player.SetTrigger("moveDown");
        else if (direction == Vector3.right)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            m_player.SetTrigger("moveSide");
        }
        else if (direction == Vector3.left)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
            m_player.SetTrigger("moveSide");
        }
    }
    #endregion

    #region Get関数
    /// <summary>
    /// 現在の移動可能かどうか取得
    /// </summary>
    /// <returns>現在の移動可能状況</returns>
    public bool GetMovable() {  return movable; }
    #endregion

    #region Set関数
    /// <summary>
    /// 現在の移動可能かどうかセット用
    /// </summary>
    /// <param name="move">現在の移動可能状況</param>
    public void SetMovable(bool move) { movable = move; }
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

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        //共通の更新処理
        //移動開始していて移動可能状態だったら移動
        Move();
        //移動中でジャンプ可能状態だったらジャンプ
        //Jump();
        UpdateBuffer();
        PlayerAnima();

        //チュートリアル固有の処理
        if (StageIndex.Instance.GetFirst())
        {
            // チュートリアル中
        }
        else
        {
            //通常ゲーム中の入力制限
            if (GameManager.Instance.GetGameMode() != GameManager.Modes.GAME || !GameManager.Instance.GetIsStart())
            {
                ResetInputState();
                return;
            }

            //特定のUI（ボタン等）の上に指がある時は入力を受け付けない
            if (isOverUIForbidden) { return; }

            if (inputLockTimer > 0f)
            {
                inputLockTimer -= Time.deltaTime;
                return; //入力を受け付けない
            }

            //入力受付
            OperationType direction = InputPlayerManager.Instance.GetDirection();
            if (direction != OperationType.NONE)
            {
                TapCheck(direction);
            }
        }

        //クリア・ミス・ギミック
        GameManager.Instance.SetHitCheck(IsHitPortalFirst(), IsHitPortalSecond());
        GameClear();
        GameOver();
    }

    private void OnDestroy()
    {
        //Playerに関連付けられたTween（CameraShakeへの命令など）を止める
        DOTween.Kill(this);
    }

    #endregion

    #region Start呼び出し関数
    /// <summary>
    /// 初期化処理
    /// </summary>
    void Init()
    {
        floorTilemap = GameObject.Find("TilemapFloor").GetComponent<Tilemap>();
        inputLockTimer = INPUT_LOCK_DURATION; //開始時にロック
        m_player = GetComponent<Animator>();
        currentSpeed = 0f;
        moveBufferTimer = 0;
        shakeCount = 0;
        moveBuffered = false;
        isMove = false;
        movable = true;
        isSound = false;
        isJump = false;
        //canJump = false;
        expansion = false;
        moveDirection = Vector3.zero;
        firstPos = transform.position;

        if (floorTilemap != null)
        {
            lastTraceCell = floorTilemap.WorldToCell(transform.position);
        }
    }
    #endregion

    #region Update呼び出し関数

    #region 状態リセット処理
    /// <summary>
    /// 状態リセット処理
    /// </summary>
    void ResetInputState()
    {
        isMove = false;
        moveBuffered = false;
        moveBufferTimer = 0f;
        movable = true;
    }
    #endregion

    #region 指定した場所の中心に跡を生成
    /// <summary>
    /// 指定したセル座標の中心に跡を生成処理
    /// </summary>
    /// <param name="cellPos">生成する場所</param>
    /// <param name="rotation">生成する向き</param>
    void SpawnTrace(Vector3Int cellPos, Quaternion rotation)
    {
        if (trace == null || floorTilemap == null) return;

        //セルの中心座標を取得
        Vector3 spawnPos = floorTilemap.GetCellCenterWorld(cellPos);

        //生成
        Instantiate(trace, spawnPos, rotation);
    }

    #endregion

    #region Player移動
    /// <summary>
    /// Playerの移動処理
    /// </summary>
    void Move()
    {
        if(GameManager.Instance.GetGameMode() == GameManager.Modes.CAMERA || GameManager.Instance.GetGameOver()) { return; }

        //壁に当たったら即停止して return
        if (IsHitWall())
        {
            if(shakeCount == 1)
            {
                if (GameManager.Instance.GetGameClear()) { return; }

                //CameraManagerが存在するか確認してから呼ぶ
                if (CameraManager.Instance != null)
                {
                    //カメラを0.3秒間、0.3の強度で揺らす
                    CameraManager.Instance.CameraShake(0.1f, 0.2f);
                }

                // 「ゲームクリアかオーバーしていない」かつ「設定で振動がON」のときだけ実行
                if ((!GameManager.Instance.GetGameClear() || !IsHitHole()) && PlayerSetting.Instance.GetVibration())
                {
#if UNITY_ANDROID || UNITY_IOS
                    Vibration.VibratePop();
#endif
                }

                shakeCount = 0;
            }
            //壁に当たっていたらフラグなどをリセット
            currentSpeed = 0f;
            isMove = false;
            movable = true;
            isSound = false;
            //canJump = false;
            GameManager.Instance.SetPlayerSwiped(false); //スワイプしてない状態にする

            if (moveBuffered) //バッファされてたら次へ移行
            {
                isMove = true;        //移動開始
                moveBuffered = false; //移動可能かのフラグを不可能に
                //canJump = true;
            }

            return;
        }

        if (isMove)
        {
            if (!isSound)
            {
                isSound = true;
            }
            //加速度の計算
            //初速からスタートし、毎フレーム acceleration 分だけ加算
            currentSpeed += acceleration * Time.deltaTime;

            //最高速度でストップ（これがないと無限に速くなります）
            if (currentSpeed > maxSpeed)
            {
                currentSpeed = maxSpeed;
            }

            Vector3 velocity = moveDirection * currentSpeed;
            transform.position += velocity * Time.deltaTime;

            //現在のワールド座標をタイルのセル座標（整数）に変換
            Vector3Int currentCell = floorTilemap.WorldToCell(transform.position);

            //前回のマスと違うマスに移動したら
            if (currentCell != lastTraceCell)
            {
                if (IsHitWall()) { return; }

                //移動方向に基づいて跡の回転を決定する
                Quaternion traceRotation = Quaternion.identity; //デフォルトは回転なし

                //横移動（右または左）の場合
                if (moveDirection.x != 0)
                {
                    //Z軸周りに90度回転させて横にする
                    traceRotation = Quaternion.Euler(0, 0, 90f);
                }
                //縦移動（上または下）の場合は moveDirection.x が 0 なので、
                //traceRotation は Quaternion.identity のまま（縦のまま生成）

                //currentCell ではなく lastTraceCell（一歩前のマス）を渡す
                //第2引数に決定した回転を渡す
                SpawnTrace(lastTraceCell, traceRotation);

                //跡を置いた後に、現在のマスを「前回のマス」として保存
                lastTraceCell = currentCell;
            }

            shakeCount = 1;
        }
        else
        {
            //移動していないときは速度を0に保つ
            currentSpeed = 0f;
            shakeCount = 0;
        }
    }
    #endregion

    #region バッファタイムの更新
    /// <summary>
    /// バッファ時間の更新処理
    /// </summary>
    void UpdateBuffer()
    {
        //バッファ時間の更新
        if (moveBuffered)
        {
            //バッファ確保時間減らす
            moveBufferTimer -= Time.deltaTime;
            if (moveBufferTimer <= 0f)
            {
                moveBuffered = false; //時間切れで無効化
            }
        }
    }
    #endregion

    #region Playerジャンプ(今は使ってない、ギミックで使えるかも)
    void Jump()
    {
        if (isJump)
        {
            if (!expansion)
            {
                //拡大中
                transform.localScale += Vector3.one * Time.deltaTime;
                if (transform.localScale.x >= 1.7f)
                {
                    expansion = true;
                }
            }
            else
            {
                //縮小中
                transform.localScale -= Vector3.one * Time.deltaTime;
                if (transform.localScale.x <= 1f)
                {
                    transform.localScale = Vector3.one;
                    isJump = false;
                    expansion = false; //次回ジャンプのためにリセット
                }
            }
        }
    }
    #endregion

    #region 移動方向と移動開始
    /// <summary>
    /// 移動方向と移動開始処理
    /// </summary>
    /// <param name="type">現在の操作状態</param>
    void TapCheck(OperationType type)
    {
        if (GameManager.Instance.GetPause() || GameManager.Instance.GetGameMode() == GameManager.Modes.CAMERA) { return; }
        moveBuffered = true;
        moveBufferTimer = bufferTime;
        var direction = type;
        //m_player.SetFloat("direction", (float)direction);
        //m_player.SetInteger("directions", (int)direction);
        switch (direction)
        {
            case OperationType.UP:
                //上にレイを飛ばす
                moveDirection = Vector3.up;
                currentSpeed = initialSpeed;                //初速をつける
                isMove = true;                              //移動フラグをセット
                //canJump = true;
                GameManager.Instance.SetPlayerSwiped(true); //スワイプしたことを通知
                m_player.SetTrigger("moveUp");
                break;
            case OperationType.DOWN:
                //下にレイを飛ばす
                moveDirection = Vector3.down;
                currentSpeed = initialSpeed;                //初速をつける
                isMove = true;                              //移動フラグをセット
                //canJump = true;
                GameManager.Instance.SetPlayerSwiped(true); //スワイプしたことを通知
                m_player.SetTrigger("moveDown");
                break;
            case OperationType.RIGHT:
                //右にレイを飛ばす
                moveDirection = Vector3.right;
                //右にスワイプされたら右を向く
                transform.rotation = new Quaternion(transform.rotation.x, 0, transform.rotation.z, transform.rotation.w);
                currentSpeed = initialSpeed;                //初速をつける
                isMove = true;                              //移動フラグをセット
                //canJump = true;
                GameManager.Instance.SetPlayerSwiped(true); //スワイプしたことを通知
                m_player.SetTrigger("moveSide");
                break;
            case OperationType.LEFT:
                //左にレイを飛ばす
                moveDirection = Vector3.left;
                //左にスワイプされたら左を向く
                transform.rotation = new Quaternion(transform.rotation.x, 180, transform.rotation.z, transform.rotation.w);
                currentSpeed = initialSpeed;                //初速をつける
                isMove = true;                              //移動フラグをセット
                //canJump = true;
                GameManager.Instance.SetPlayerSwiped(true); //スワイプしたことを通知
                m_player.SetTrigger("moveSide");
                break;
            case OperationType.TAP:
                moveBuffered = false; //TAPなら無効
                //ここを「移動中」のみジャンプできるように
                if (!isJump && isMove)
                {
                    isJump = true;
                }
                break;
        }

        //スライド回数を加算する（1回だけ）
        if (direction != OperationType.TAP && direction != OperationType.NONE)
        {
            GameManager.Instance.SlideCount(1);
/*            if (!GameManager.Instance.GetGameClear() || !IsHitHole()) {
                if (GameManager.Instance.GetGameClear() || IsHitHole() || !PlayerSetting.Instance.GetVibration()) { return; } 
                Handheld.Vibrate(); 
            }*/
        }

        //すぐ動けるなら開始TAPだった場合はisMoveを書き換えないようにする
        if (direction != OperationType.TAP && movable)
        {
            isMove = true;
            moveBuffered = false; //消化済み
        }
    }

    #endregion

    #region ループアニメーション用
    /// <summary>
    /// プレイヤーのアニメーション処理
    /// </summary>
    void PlayerAnima()
    {
        m_player.SetBool("isMove", isMove);
    }

    #endregion

    #region ゲームクリア、ゲームオーバー監視用
    /// <summary>
    /// ゲームクリアフラグ処理
    /// </summary>
    void GameClear()
    {
        if (IsHitGoal())
        {
            GameManager.Instance.SetGameClear(true);
        }
    }

    /// <summary>
    /// ゲームオーバーフラグ処理
    /// </summary>
    void GameOver()
    {
        if (IsHitHole())
        {
            GameManager.Instance.SetGameOver(true);
        }
    }

    #endregion

    #region Ray当たり判定
    /// <summary>
    /// 進む方向にRayを飛ばして壁との当たり判定を行う
    /// </summary>
    /// <returns>壁と当っているか</returns>
    private bool IsHitWall()
    {
        float rayLength = 0.6f;              //Rayの距離
        Vector2 origin = transform.position; //Rayの始点

        //進む方向にRaycast（wallChecklayerに当たったらhit）
        RaycastHit2D hit = Physics2D.Raycast(origin, moveDirection, rayLength, wallCheckLayer);

        //デバッグ用にRayを表示
        Debug.DrawRay(origin, moveDirection * rayLength, Color.green);

        return hit.collider != null;         //wallに当たったらtrue
    }
    #endregion

    #region ポータルとの当たり判定

    /// <summary>
    /// 進む方向にRayを飛ばしてポータルとの当たり判定を行う
    /// </summary>
    /// <returns>ポータルと当っているか</returns>
    private bool IsHitPortalFirst()
    {
        float rayLength = 0.01f;              //Rayの距離
        Vector2 origin = transform.position; //Rayの始点

        //進む方向にRaycast（portalFirstLayerに当たったらhit）
        RaycastHit2D hit = Physics2D.Raycast(origin, moveDirection, rayLength, portalFirstLayer);

        //デバッグ用にRayを表示
        Debug.DrawRay(origin, moveDirection * rayLength, Color.green);

        return hit.collider != null;         //portalに当たったらtrue
    }

    //進む方向にRayを飛ばしてポータルとの当たり判定を行う
    private bool IsHitPortalSecond()
    {
        float rayLength = 0.01f;              //Rayの距離
        Vector2 origin = transform.position; //Rayの始点

        //進む方向にRaycast（portalSecondLayerに当たったらhit）
        RaycastHit2D hit = Physics2D.Raycast(origin, moveDirection, rayLength, portalSecondLayer);

        //デバッグ用にRayを表示
        Debug.DrawRay(origin, moveDirection * rayLength, Color.green);

        return hit.collider != null;         //portalに当たったらtrue
    }

    #endregion

    #region ゴールとの当たり判定

    /// <summary>
    /// 進む方向にRayを飛ばしてゴールとの当たり判定を行う
    /// </summary>
    /// <returns>ゴールと当っているか</returns>
    private bool IsHitGoal()
    {
        float rayLength = 0.01f;              //Rayの距離
        Vector2 origin = transform.position; //Rayの始点

        //進む方向にRaycast（goalLayerに当たったらhit）
        RaycastHit2D hit = Physics2D.Raycast(origin, moveDirection, rayLength, goalLayer);

        //デバッグ用にRayを表示
        Debug.DrawRay(origin, moveDirection * rayLength, Color.green);

        return hit.collider != null;         //goalに当たったらtrue
    }

    #endregion

    #region 落とし穴との当たり判定

    /// <summary>
    /// 進む方向にRayを飛ばして落とし穴との当たり判定を行う
    /// </summary>
    /// <returns>落とし穴と当っているか</returns>
    private bool IsHitHole()
    {
        float rayLength = 0.1f;              //Rayの距離
        Vector2 origin = transform.position; //Rayの始点

        //進む方向にRaycast（holeLayerに当たったらhit）
        RaycastHit2D hit = Physics2D.Raycast(origin, moveDirection, rayLength, holeLayer);

        //デバッグ用にRayを表示
        Debug.DrawRay(origin, moveDirection * rayLength, Color.green);

        return hit.collider != null;         //holeに当たったらtrue
    }

    #endregion

#endregion
}
