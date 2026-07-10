using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

//ランキングデータ用ワッパー
[System.Serializable]
public class RankingDataWrapper
{
    public List<ScoreEntry> items;
}

//リザルト用GASからレスポンスを受け取るためのワッパー
[System.Serializable]
public class OnlineRankingResponse
{
    public List<ScoreEntry> rankings;
    public int myRank;
}

public class OnLineRanking : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static OnLineRanking Instance { get; private set; }
    #endregion

    #region 変数
    [SerializeField] private string gasUrl = "https://script.google.com/macros/s/AKfycbzSE0ZGkaOauklcFnCOVlG6mUJ5orv6whRMlLOzhFbDBxYleIevRrBPCtU-ab6oGJSw/exec";

    //内部管理用のID
    private string userId;

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
        DontDestroyOnLoad(gameObject);

        //既にログイン済みならIDが保存されている
        userId = PlayerPrefs.GetString("OnlineUserID", "");
    }

    #endregion

    //ログイン済みかどうかを確認するプロパティ
    public bool IsLoggedIn => !string.IsNullOrEmpty(userId);

    /// <summary>
    /// 新規ユーザー登録
    /// </summary>
    /// <param name="onResponse">完了時のコールバック</param>
    public void Register(string name, string pass, Action<bool, string> onResponse)
    {
        string json = $"{{\"type\":\"registerUser\", \"playerName\":\"{name}\", \"password\":\"{pass}\"}}";
        StartCoroutine(AuthCoroutine(json, onResponse));
    }

    /// <summary>
    /// 既存ユーザーでログイン
    /// </summary>
    public void Login(string name, string pass, Action<bool, string> onResponse)
    {
        string json = $"{{\"type\":\"loginUser\", \"playerName\":\"{name}\", \"password\":\"{pass}\"}}";
        StartCoroutine(AuthCoroutine(json, onResponse));
    }
    
    /// <summary>
    /// 認証通信の共通コルーチン
    /// </summary>
    private IEnumerator AuthCoroutine(string json, Action<bool, string> onResponse)
    {
        using (UnityWebRequest request = new UnityWebRequest(gasUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                if (response.StartsWith("Success:"))
                {
                    //サーバーから送られてきた内部IDを保存
                    userId = response.Split(':')[1];
                    PlayerPrefs.SetString("OnlineUserID", userId);
                    PlayerPrefs.Save();
                    onResponse?.Invoke(true, "成功しました");
                }
                else
                {
                    //エラーメッセージ（「名前が使われています」）をそのまま返す
                    onResponse?.Invoke(false, response);
                }
            }
            else
            {
                onResponse?.Invoke(false, "サーバーとの通信に失敗しました");
            }
        }
    }

    /// <summary>
    /// スコア送信（ログイン済みのuserIdを使用）
    /// </summary>
    public void SendScore(int stageIndex, float time)
    {
        if (!IsLoggedIn) return;

        string json = $"{{\"type\":\"postScore\", \"stageIndex\":{stageIndex}, \"userId\":\"{userId}\", \"clearTime\":{time}}}";
        StartCoroutine(PostScoreCoroutine(json));
    }

    private IEnumerator PostScoreCoroutine(string json)
    {
        using (UnityWebRequest request = new UnityWebRequest(gasUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
        }
    }

    /// <summary>
    /// 【ランキングの取得】
    /// GASからランキングデータを取得。完了時にTitleManager側の表示処理を実行
    /// </summary>
    /// <param name="onCompleted">通信終了後に実行したい処理（Action）</param>
    public void GetRanking(int stageIndex, Action<List<ScoreEntry>> onCompleted)
    {
        StartCoroutine(GetRankingCoroutine(stageIndex, onCompleted));
    }

    /// <summary>
    /// ランキング取得の通信実体
    /// </summary>
    private IEnumerator GetRankingCoroutine(int stageIndex, Action<List<ScoreEntry>> onCompleted)
    {
        //URLパラメータにステージ番号を与える
        string requestUrl = $"{gasUrl}?stageIndex={stageIndex}";

        using (UnityWebRequest request = UnityWebRequest.Get(requestUrl))
        {
            yield return request.SendWebRequest();

            List<ScoreEntry> resultList = new List<ScoreEntry>();

            if (request.result == UnityWebRequest.Result.Success)
            {
                //JsonUtilityで読み込めるように配列JSONをオブジェクト形式にラップ
                string jsonResponse = request.downloadHandler.text;
                string wrappedJson = "{\"items\":" + jsonResponse + "}";
                RankingDataWrapper wrapper = JsonUtility.FromJson<RankingDataWrapper>(wrappedJson);
                resultList = wrapper.items;
            }
            else
            {
                //通信失敗
                Debug.LogError($"ランキング取得失敗: {request.error}");
            }

            //通信が終わったことを、呼び出し元に通知
            //引数として取得したリスト（失敗時は空リスト）を渡す
            onCompleted?.Invoke(resultList);
        }
    }

    /// <summary>
    /// リザルト用ランキングと自分の順位の取得
    /// 完了時にランキングリストと自分の全体順位（圏外は-1）を返す
    /// </summary>
    public void GetResultRanking(int stageIndex, string userId, Action<List<ScoreEntry>, int> onCompleted)
    {
        StartCoroutine(GetRankingWithRankCoroutine(stageIndex, userId, onCompleted));
    }

    /// <summary>
    /// 自分の順位も含むランキング取得の通信実体
    /// </summary>
    private IEnumerator GetRankingWithRankCoroutine(int stageIndex, string userId, Action<List<ScoreEntry>, int> onCompleted)
    {
        //URLパラメータに stageIndex と userId の両方を付与する
        string requestUrl = $"{gasUrl}?stageIndex={stageIndex}&userId={UnityWebRequest.EscapeURL(userId)}";

        using (UnityWebRequest request = UnityWebRequest.Get(requestUrl))
        {
            yield return request.SendWebRequest();

            List<ScoreEntry> resultList = new List<ScoreEntry>();
            int myRank = -1;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                try
                {
                    //GAS側からそのままパース
                    OnlineRankingResponse response = JsonUtility.FromJson<OnlineRankingResponse>(jsonResponse);

                    if (response != null)
                    {
                        resultList = response.rankings;
                        myRank = response.myRank;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSONのパースに失敗しました: {e.Message}\nResponse: {jsonResponse}");
                }
            }
            else
            {
                Debug.LogError($"ランキング・順位取得失敗: {request.error}");
            }

            //通信終了後、リストと順位を呼び出し元に返す
            onCompleted?.Invoke(resultList, myRank);
        }
    }

    #region ログアウト処理
    /// <summary>
    /// ユーザーIDの削除（空文字）
    /// </summary>
    public void ResetId()
    {
        userId = "";
    }

    #endregion
}