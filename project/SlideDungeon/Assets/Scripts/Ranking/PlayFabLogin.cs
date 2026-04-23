using PlayFab.ClientModels;
using PlayFab;
using System.Collections;
using UnityEngine;
using System.Text;

/// <summary>
/// PlayFabで一意なcustomIDのログイン処理を行うクラス
/// </summary>
public class PlayFabLogin : MonoBehaviour
{
    // アカウントを新規作成するかどうか
    bool shouldCreateAccount;

    // customIDを代入しておく変数
    string customID;

    public static bool IsLoggedIn { get; private set; }

    public static event System.Action LoginSucceeded;

    // デバイスにcustomIDを保存する場合に使うキー
    static readonly string CUSTOM_ID_SAVE_KEY = "TEST_RANKING_SAVE_KEY";

    // customIDに使用する文字一覧
    static readonly string ID_CHARACTERS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    void Start()
    {
        if (DebugMode.Instance.GetDebugMode())
        {
            //ログイン処理の実行
            Login();
        }
    }

    void Login()
    {
        // customIDを読み込む
        customID = LoadCustomID();

        //Debug.Log($"CustomID: {customID}, CreateAccount: {shouldCreateAccount}");

        // ログイン情報の代入
        var request = new LoginWithCustomIDRequest
        {
            CustomId = customID,
            CreateAccount = shouldCreateAccount // 新規ならtrue, 既存ならfalse
        };

        // ログイン処理
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    void OnLoginSuccess(LoginResult result)
    {
        if (result.NewlyCreated)
        {
            Debug.Log("新規アカウントが作成されました");
            // 念のため保存（初回のみ）
            PlayerPrefs.SetString(CUSTOM_ID_SAVE_KEY, customID);
            PlayerPrefs.Save();
        }

        IsLoggedIn = true;
        Debug.Log("ログインに成功しました");
        Debug.Log($"CustomID : {customID}");

        // イベント通知
        LoginSucceeded?.Invoke();
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
        Debug.Log("ログインに失敗しました");

        // AccountNotFound が返ってきた場合は「必ず CreateAccount = true」で再ログイン
        if (error.Error == PlayFabErrorCode.AccountNotFound)
        {
            Debug.Log("ユーザーが存在しないので新規作成します");
            shouldCreateAccount = true;
            StartCoroutine(RetryLoginAfterDelay(0.5f));
        }
    }

    IEnumerator RetryLoginAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Login();
    }

    /// <summary>
    /// customIDの取得
    /// </summary>
    string LoadCustomID()
    {
        // PlayerPrefsを使ってcustomIDを取得（保存されていなければ空文字）
        string id = PlayerPrefs.GetString(CUSTOM_ID_SAVE_KEY);

        if (string.IsNullOrEmpty(id))
        {
            shouldCreateAccount = true;
            id = GenerateAndSaveCustomID(); // 新規作成して保存
        }
        else
        {
            shouldCreateAccount = false;
        }

        return id;
    }

    /// <summary>
    /// customIDを生成して保存する
    /// </summary>
    string GenerateAndSaveCustomID()
    {
        int idLength = 32;
        StringBuilder stringBuilder = new StringBuilder(idLength);
        var random = new System.Random();

        for (int i = 0; i < idLength; i++)
        {
            stringBuilder.Append(ID_CHARACTERS[random.Next(ID_CHARACTERS.Length)]);
        }

        string newId = stringBuilder.ToString();

        // 生成したIDを保存
        PlayerPrefs.SetString(CUSTOM_ID_SAVE_KEY, newId);
        PlayerPrefs.Save();

        Debug.Log($"新しいCustomIDを生成して保存しました: {newId}");

        return newId;
    }
}





/*using PlayFab.ClientModels;
using PlayFab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

// <summary>
// PlayFabで一意なcustomIDのログイン処理を行うクラス
// </summary>

public class PlayFabLogin : MonoBehaviour
{
    //アカウントを新規作成するかどうか
    bool shouIdCreateAccount;

    //customIDを代入しておく変数
    string customID;

    public static bool IsLoggedIn { get; private set; }

    public static event System.Action LoginSucceeded;

    /// <summary>
    /// ログイン処理
    /// </summary>

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //ログイン処理の実行
        Login();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void Login()
    {
        Debug.Log($"CustomID: {customID}, CreateAccount: {shouIdCreateAccount}");

        // TitleID を明示的に設定
        //PlayFabSettings.staticSettings.TitleId = "90775";

        //customIDを読み込む
        customID = LoadCustomID();

        //ログイン情報の代入
        var request = new LoginWithCustomIDRequest { CustomId = customID, CreateAccount = shouIdCreateAccount };

        //ログイン処理
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    //ログイン成功の処理
    void OnLoginSuccess(LoginResult result)
    {
        if (shouIdCreateAccount && !result.NewlyCreated)
        {
            Login();
            return;
        }

        if (result.NewlyCreated)
        {
            SaveCustomID();
        }

        IsLoggedIn = true;
        Debug.Log("ログインに成功しました");
        Debug.Log($"CustomID :{customID}");

        // イベント通知
        LoginSucceeded?.Invoke();
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
        Debug.Log("ログインに失敗しました");
    }

    /// <summary>
    /// customIDの取得
    /// </summary>

    // デバイスにcustomIDを保存する場合に使うキーの設定
    // PlayerPrefsを使って保存をcustomIDの保存を行うので、そのキーの設定です
    // 詳しくは「オフラインランキングを実装する」の「PlayerPrefsって何？」をご覧ください
    static readonly string CUSTOM_ID_SAVE_KEY = "TEST_RANKING_SAVE_KEY";

    //自分のIDを取得するメソッド
    string LoadCustomID()
    {
        //PlayerPrefsを使って、customIDを取得する
        //もし保存されていない場合は空文字を返す
        string id = PlayerPrefs.GetString(CUSTOM_ID_SAVE_KEY);

        //もしidが空文字だったらshouldCreateAccountにtrueを代入、そうでないならfalseを代入する
        shouIdCreateAccount = string.IsNullOrEmpty(id);

        //shouldCreateAccountがtrueならcustomIDを新規作成、falseなら保存されていたcustomIDを返す
        return shouIdCreateAccount ? GenerateCustomID() : id;
    }

    //customIDをデバイスに保存するメゾット
    void SaveCustomID()
    {
        //PlayerPrefsを使って、customIDを保存する
        PlayerPrefs.SetString(CUSTOM_ID_SAVE_KEY, customID);
    }

    /// <summary>
    /// customIDの生成
    /// </summary>
    //customIDに使用する文字一覧
    static readonly string ID_CHARACTERS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    //customIDを生成するメソッド
    string GenerateCustomID()
    {
        //customIDの長さ
        int idLength = 32;

        //生成したcustomIDを代入する変数の初期化
        StringBuilder stringBuilder = new StringBuilder(idLength);

        //customIDをランダムで出力するために乱数を使う
        var random = new System.Random();

        //customIDの生成
        for (int i = 0; i < idLength; i++)
        {
            //乱数を使ってランダムに文字列を代入する
            //代入された文字列をcustomIDにする
            stringBuilder.Append(ID_CHARACTERS[random.Next(ID_CHARACTERS.Length)]);
        }

        //生成したcustomIDを返す
        return stringBuilder.ToString();
    }
}*/
