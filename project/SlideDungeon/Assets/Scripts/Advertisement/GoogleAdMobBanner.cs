using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

public class GoogleAdMobBanner : MonoBehaviour
{
    #region シングルトン
    public static GoogleAdMobBanner Instance { get; private set; }
    #endregion

    #region 変数
    private BannerView bannerView;
    private bool isInitialized = false;   //初期化が完了したかどうかのフラグ
    private bool shouldShowBanner = true; //現在バナーを表示すべき状態かどうかの状態管理フラグ
    #endregion

    #region 外部呼出し関数

    /// <summary>
    /// バナーを非表示
    /// </summary>
    public void BannerHide()
    {
        shouldShowBanner = false; //「今は表示したくない」状態にする

        //Destroyして確実にバグを防ぐ
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null; //nullに戻しておく
        }
    }

    /// <summary>
    /// バナーを表示
    /// </summary>
    public void BannerShow()
    {
        shouldShowBanner = true; //「表示したい」状態にする

        //毎回ロード（RequestBanner）を走らせることで、確実に再表示させる
        if (isInitialized)
        {
            RequestBanner();
        }
    }

    #endregion

    #region Unityイベント関数
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        //SDKの初期化
        MobileAds.Initialize(initStatus =>
        {
            //初期化完了のコールバックからメインスレッドを呼び出すための処理
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isInitialized = true; //初期化完了フラグを立てる

                //初期化を待っている間にBannerHideが呼ばれていなければ、広告を読み込む
                if (shouldShowBanner)
                {
                    RequestBanner();
                }
            });
        });
    }

    //オブジェクトが破棄されるときはメモリリークを防ぐためにバナーも消す
    private void OnDestroy()
    {
        //自身が正規のインスタンスの場合のみ破棄
        if (Instance == this && bannerView != null)
        {
            bannerView.Destroy();
        }
    }

    #endregion

    /// <summary>
    /// バナーを表示する為の処理
    /// </summary>
    private void RequestBanner()
    {
#if UNITY_ANDROID
        //Android用広告ユニットID
        string adUnitId = "ca-app-pub-4583036480674919/7802084538";
#elif UNITY_IPHONE
        //iOS用広告ユニットID
        string adUnitId = "ca-app-pub-4583036480674919/3841142851";
#else
        string adUnitId = "unexpected_platform";
#endif

        //古いバナーが残っている場合は破棄
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        //画面の下に配置
        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

        AdRequest request = new AdRequest();

        //広告の読み込み
        bannerView.LoadAd(request);
    }
}