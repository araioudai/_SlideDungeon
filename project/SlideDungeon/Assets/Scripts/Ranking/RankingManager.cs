using PlayFab.ClientModels;
using PlayFab;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RankingManager : MonoBehaviour
{
    #region シングルトン
    public static RankingManager Instance { get; private set; }
    #endregion

    #region private変数
    [SerializeField] private Text[] textRanking;
    private string m_name;
    private float m_time;   //クリアタイム（秒）
    private bool m_request; //ランキング再取得リクエスト
    private string currentRankingName;
/*    private float lastUserDataUpdateTime = -1000f;   //最後に送信した時刻
    private const float userDataUpdateCooldown = 10f; //クールタイム（秒）*/
    #endregion

    #region セット関数
    public void SetTime(float time) { m_time = time; }
    public void SetRequest(bool req) { m_request = req; }
    public void SetStage(int stageIndex)
    {
        currentRankingName = $"Stage{stageIndex}Ranking";
        //Debug.Log($"RankingManager: {currentRankingName} に切り替えました");
    }
    #endregion

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //シーンロードイベント登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        currentRankingName = "Stage1Ranking";
        for (int i = 0; i < textRanking.Length; i++)
        {
            if (textRanking[i] != null)
            {
                textRanking[i].text = "";
            }
        }

        if (PlayFabLogin.IsLoggedIn)
        {
            GetRanking();
        }
        else
        {
            PlayFabLogin.LoginSucceeded += OnLoginSucceeded;
        }
    }

    void Update()
    {
        //Debug.Log($"Update m_request={m_request}, IsLoggedIn={PlayFabLogin.IsLoggedIn}");

        if (!PlayFabLogin.IsLoggedIn) return;

        m_name = InputManager.playerName;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetUserName(m_name);
            SubmitClearTime(m_time);
        }
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            if (GameManager.Instance.GetGameClear())
            {
                SetUserName(m_name);
                SubmitClearTime(m_time);
            }
        }
        if (m_request)
        {
            GetRanking();
            m_request = false;
        }
        //Debug.Log(m_request);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "TitleScene")
        {
            var parent = GameObject.Find("Canvas").transform.Find("RankingPanel/RankingTexts"); ; //オブジェクト名に合わせる
            if (parent != null)
            {
                textRanking = parent.GetComponentsInChildren<Text>();
                UpdateRankingUI();
            }
            else
            {
                Debug.LogWarning("RankingTexts が見つかりません");
            }
        }
    }

    void OnLoginSucceeded()
    {
        PlayFabLogin.LoginSucceeded -= OnLoginSucceeded;
        GetRanking();
    }

    void SetUserName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = "Guest";
        }

        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = name
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result => Debug.Log("プレイヤー名変更 成功"),
            error => Debug.Log("プレイヤー名変更 失敗: " + error.GenerateErrorReport()));
    }

    public void TestSend()
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
        {
            new StatisticUpdate { StatisticName = "Stage1Ranking", Value = 123 }
        }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result => Debug.Log("テスト送信成功"),
            error => Debug.LogError("テスト送信失敗: " + error.GenerateErrorReport())
        );
    }

    // スコア（タイム）送信
    void SubmitClearTime(float time)
    {
        if (!PlayFabLogin.IsLoggedIn)
        {
            Debug.LogWarning("ログインしていないためタイム送信できません");
            return;
        }

        int modifiedScore = int.MaxValue - Mathf.RoundToInt(time * 100f);
        if (modifiedScore < 0) modifiedScore = 0;

        var statisticUpdate = new StatisticUpdate
        {
            StatisticName = currentRankingName,
            Value = modifiedScore
        };

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> { statisticUpdate }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
        {
            Debug.Log($"タイム送信 成功: {modifiedScore}");

            // UserDataも同時更新
            var dataRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
            {
                { "ClearTime", time.ToString("F2") },
                { "ScoreTimestamp", System.DateTime.UtcNow.Ticks.ToString() }
            },
                Permission = UserDataPermission.Public
            };

            PlayFabClientAPI.UpdateUserData(dataRequest, _ => GetRanking(), err =>
            {
                Debug.LogWarning("UserData保存失敗: " + err.GenerateErrorReport());
                GetRanking();
            });

        }, error =>
        {
            Debug.LogError("タイム送信 失敗:\n" + error.GenerateErrorReport());
        });
    }


    /*    void SubmitClearTime(float time)
        {
            int modifiedScore = int.MaxValue - Mathf.RoundToInt(time * 100f);

            var statisticUpdate = new StatisticUpdate
            {
                StatisticName = currentRankingName,
                Value = modifiedScore,
            };

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate> { statisticUpdate }
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
            {
                Debug.Log("タイム送信 成功");

                // UserDataも同時に更新（参考値として）
                var dataRequest = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                        { "ScoreTimestamp", System.DateTime.UtcNow.Ticks.ToString() },
                        { "ClearTime", time.ToString("F2") }
                    },
                    Permission = UserDataPermission.Public
                };
                PlayFabClientAPI.UpdateUserData(dataRequest,
                    _ => GetRanking(),
                    err => { Debug.LogWarning("UserData保存失敗: " + err.GenerateErrorReport()); GetRanking(); });

            }, error =>
            {
                Debug.Log("タイム送信 失敗: " + error.GenerateErrorReport());
            });
        }*/


    /*void SubmitClearTime(float time)
    {
        // レートリミット対策: クールタイム未経過ならスキップ
        if (Time.time - lastUserDataUpdateTime < userDataUpdateCooldown)
        {
            Debug.LogWarning("UserData更新: クールタイム中のためスキップ");
            return;
        }
        lastUserDataUpdateTime = Time.time;

        int modifiedScore = int.MaxValue - Mathf.RoundToInt(time * 100f);
        var statisticUpdate = new StatisticUpdate
        {
            StatisticName = currentRankingName,
            Value = modifiedScore,
        };

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> { statisticUpdate }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
        {
            Debug.Log("タイム送信 成功");

            // UserDataも同時に更新（参考値として）
            var dataRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
            {
                { "ScoreTimestamp", System.DateTime.UtcNow.Ticks.ToString() },
                { "ClearTime", time.ToString("F2") }
            },
                Permission = UserDataPermission.Public
            };
            PlayFabClientAPI.UpdateUserData(dataRequest,
                _ => GetRanking(),
                err => { Debug.LogWarning("UserData保存失敗: " + err.GenerateErrorReport()); GetRanking(); });

        }, error =>
        {
            Debug.Log("タイム送信 失敗: " + error.GenerateErrorReport());
        });
    }*/

    // ランキング取得
    void GetRanking()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = currentRankingName,
            StartPosition = 0,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, leaderboardResult =>
        {
            cachedRanking.Clear();
            foreach (var item in leaderboardResult.Leaderboard)
            {
                float clearTime = (int.MaxValue - item.StatValue) / 100f;
                cachedRanking.Add((item.DisplayName, clearTime));
                Debug.Log($"ランキング取得: {item.DisplayName} {clearTime:F2}秒");
            }
            UpdateRankingUI();
        }, error =>
        {
            Debug.Log("ランキング取得失敗: " + error.GenerateErrorReport());
        });
    }

    // ランキングデータキャッシュ（名前・タイムのみ）
    private List<(string name, float clearTime)> cachedRanking = new List<(string, float)>();

    // UI 更新
    void UpdateRankingUI()
    {
        for (int i = 0; i < textRanking.Length; i++)
        {
            if (textRanking[i] == null) continue; //破棄されていたらスキップ

            if (i < cachedRanking.Count)
            {
                var r = cachedRanking[i];
                textRanking[i].text = $"{r.name}　{r.clearTime:F2}秒";
            }
            else
            {
                textRanking[i].text = "";
            }
        }
    }
}



/*using PlayFab.ClientModels;
using PlayFab;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RankingManager : MonoBehaviour
{
    #region シングルトン
    public static RankingManager Instance { get; private set; }
    #endregion

    [SerializeField] private Text[] textRanking;
    private string m_name;
    private float m_time;   //スコアではなくクリアタイムを保持
    private bool m_request; //タイトルマネージャーからリクエストされたらranking更新

    public void SetTime(float time) { m_time = time; }   //タイムをセット
    public void SetRequest(bool req) { m_request = req; }

    *//*float GetTime() { return m_time; }                  //タイムを取得*//*

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);  //シーン切り替えで破棄しない
    }

    void Start()
    {
        //ランキングのUIを初期化
        for (int i = 0; i < textRanking.Length; i++)
        {
            if (textRanking[i] != null)
            {
                textRanking[i].text = $"{i + 1}位";
            }
        }

        //ログイン済みならランキングを取得
        if (PlayFabLogin.IsLoggedIn)
        {
            GetRanking();
        }
        else
        {
            //ログイン完了時にイベントをフック
            PlayFabLogin.LoginSucceeded += OnLoginSucceeded;
        }
    }

    void Update()
    {
        if (!PlayFabLogin.IsLoggedIn) return;

        //入力されたプレイヤー名を取得
        m_name = InputManager.playerName;

        //スコア送信を試せる
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetUserName(m_name);
            SubmitClearTime(m_time);
        }
        if(SceneManager.GetActiveScene().name == "GameScene")
        {
            if (GameManager.Instance.GetGameClear())
            {
                SetUserName(m_name);
                SubmitClearTime(m_time);
            }
        }
        //Debug.Log(m_request);
        //ランキング再取得
        if (m_request)
        {
            GetRanking();
            m_request = false;
        }
    }

    void OnLoginSucceeded()
    {
        PlayFabLogin.LoginSucceeded -= OnLoginSucceeded;
        GetRanking();
    }

    //プレイヤー名をPlayFabのDisplayNameに設定
    void SetUserName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = "Guest";
        }

        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = name
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result => Debug.Log("プレイヤー名変更 成功"),
            error => Debug.Log("プレイヤー名変更 失敗: " + error.GenerateErrorReport()));
    }

    //スコア（タイム）送信
    void SubmitClearTime(float time)
    {
        //Debug.Log($"送信するタイム: {time:F2}秒");

        //PlayFabにはintしか送れないので "小さいタイム = 高順位" になるように変換
        int modifiedScore = int.MaxValue - Mathf.RoundToInt(time * 100f);

        var statisticUpdate = new StatisticUpdate
        {
            StatisticName = "Stage1Ranking",
            Value = modifiedScore,
        };

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> { statisticUpdate }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
        {
            Debug.Log("タイム送信 成功");

            //ランキング並び替え用にタイムとタイムスタンプをUserDataに保存
            var dataRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { "ScoreTimestamp", System.DateTime.UtcNow.Ticks.ToString() },
                    { "ClearTime", time.ToString("F2") } //← 小数第2位まで保存
                },
                Permission = UserDataPermission.Public //
            };
            PlayFabClientAPI.UpdateUserData(dataRequest,
                _ => GetRanking(), //保存成功後にランキング取得
                err => { Debug.LogWarning("UserData保存失敗: " + err.GenerateErrorReport()); GetRanking(); });

        }, error =>
        {
            Debug.Log("タイム送信 失敗: " + error.GenerateErrorReport());
        });
    }

    //ランキング取得
    void GetRanking()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "Stage1Ranking",
            StartPosition = 0,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, leaderboardResult =>
        {
            cachedRanking.Clear();
            var playerIds = new List<string>();

            foreach (var item in leaderboardResult.Leaderboard)
            {
                //ClearTime は後で UserData から取得するので "??" を入れておく
                cachedRanking.Add((item.DisplayName, item.StatValue, long.MaxValue, item.PlayFabId, "??"));
                playerIds.Add(item.PlayFabId);
            }

            UpdateRankingUI(); //先に暫定表示

            int receivedCount = 0;

            //各プレイヤーの UserData から ClearTime と Timestamp を取得
            foreach (var pid in playerIds)
            {
                var dataRequest = new GetUserDataRequest { PlayFabId = pid };
                PlayFabClientAPI.GetUserData(dataRequest, dataResult =>
                {
                    long ts = long.MaxValue;
                    string clearTimeStr = "??";

                    if (dataResult.Data != null)
                    {
                        if (dataResult.Data.ContainsKey("ScoreTimestamp"))
                            long.TryParse(dataResult.Data["ScoreTimestamp"].Value, out ts);

                        if (dataResult.Data.ContainsKey("ClearTime"))
                            clearTimeStr = dataResult.Data["ClearTime"].Value;
                    }

                    for (int i = 0; i < cachedRanking.Count; i++)
                    {
                        if (cachedRanking[i].playFabId == pid)
                        {
                            cachedRanking[i] = (cachedRanking[i].name, cachedRanking[i].score, ts, pid, clearTimeStr);
                            break;
                        }
                    }

                    receivedCount++;
                    if (receivedCount == cachedRanking.Count) SortAndDisplay();

                }, err =>
                {
                    Debug.LogWarning("UserData取得失敗: " + err.GenerateErrorReport());
                    receivedCount++;
                    if (receivedCount == cachedRanking.Count) SortAndDisplay();
                });
            }

        }, error =>
        {
            Debug.Log("ランキング取得失敗: " + error.GenerateErrorReport());
        });
    }

    //並べ替え（スコア優先 → タイムスタンプ順）
    void SortAndDisplay()
    {
        cachedRanking.Sort((a, b) =>
        {
            int scoreCompare = b.score.CompareTo(a.score); //スコア（変換済み値）で比較
            if (scoreCompare != 0) return scoreCompare;
            return a.timestamp.CompareTo(b.timestamp);    //同スコアなら古い送信を優先
        });
        UpdateRankingUI();
    }

    //ランキングデータキャッシュ
    private List<(string name, int score, long timestamp, string playFabId, string clearTime)> cachedRanking
        = new List<(string, int, long, string, string)>();

    //UI 更新
    void UpdateRankingUI()
    {
        for (int i = 0; i < textRanking.Length; i++)
        {
            if (i < cachedRanking.Count)
            {
                var r = cachedRanking[i];
                textRanking[i].text = $"{r.name}　{r.clearTime:F2}秒"; // ClearTimeはStatValueから計算
            }
            else
            {
                textRanking[i].text = "";
            }
        }

        *//*        for (int i = 0; i < textRanking.Length; i++)
                {
                    if (textRanking[i] == null) continue; //破棄されていたらスキップ
                    if (i < cachedRanking.Count)
                    {
                        var r = cachedRanking[i];
                        //"xx.xx秒" の形式で表示
                        textRanking[i].text = $"{r.name}　{r.clearTime}秒";
                    }
                    else
                    {
                        textRanking[i].text = "";
                    }
                }*//*
    }
}*/

/*
using PlayFab.ClientModels;
using PlayFab;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingManager : MonoBehaviour
{
    #region シングルトン
    public static RankingManager Instance { get; private set; } //他のスクリプトからInstanceでアクセスできるようにする
    #endregion

    [SerializeField] private Text[] textRanking;
    string m_name;
    float m_time;

    public void SetTime(float time) { m_time = time; }

    float GetTime() { return m_time; }

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
        //DontDestroyOnLoad(gameObject);  //シーン切り替えで破棄しない
    }

    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            if (textRanking[i] != null)
            {
                textRanking[i].text = $"{i + 1}位";
            }
        }

        //ログイン済みなら即ランキング取得
        if (PlayFabLogin.IsLoggedIn)
        {
            GetRanking();
        }
        else
        {
            //ラムダ式だと解除しづらいからメソッド登録推奨
            PlayFabLogin.LoginSucceeded += OnLoginSucceeded;
        }
    }

    //テスト用の仮プログラム
    //ランキングの送受信を行う
    void Update()
    {
        if (!PlayFabLogin.IsLoggedIn) return; //ログイン完了してなければ何もしない

        m_name = InputManager.playerName;

        //キーボードで「B」が入力されたらスコアとしてm_scoreを送信する
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetUserName(m_name);
            SubmitClearTime(GetTime());
        }

        //キーボードで「C」が入力されたらランキングを取得する
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            GetRanking();
        }
    }

    #endregion

    void OnLoginSucceeded()
    {
        PlayFabLogin.LoginSucceeded -= OnLoginSucceeded; //登録解除
        GetRanking();
    }

    //変更後のプレイヤー名を引数にする
    void SetUserName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = "Guest";
        }

        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = name
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnSetUserNameSuccess, OnSetUserNameFailure);

        void OnSetUserNameSuccess(UpdateUserTitleDisplayNameResult result)
        {
            Debug.Log("成功");
            //SubmitScore(GetScore());
        }

        void OnSetUserNameFailure(PlayFabError error)
        {
            Debug.Log("失敗");
            //SubmitScore(GetScore());
        }
    }


    //送信するスコアを引数にする
    *//*    void SubmitScore(int score)
        {
            //昇順ランキング用にスコアを修正する
            //降順ランキングの場合はこの作業は省略する
            //処理の内容と理由は後述
            //int modifiedScore = int.MaxValue - score;

            Debug.Log($"送信するスコア: {score}");
            int modifiedScore = score;

            var statisticUpdate = new StatisticUpdate
            {
                StatisticName = "Stage1Ranking",
                Value = modifiedScore,
            };
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate> { statisticUpdate }
            };

            //スコア送信
            PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
            {
                Debug.Log("スコアの送信に成功しました");

                //送信時刻をUserDataに保存（同スコア時の送信順判定用）
                var dataRequest = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                        { "ScoreTimestamp", System.DateTime.UtcNow.Ticks.ToString() }
                    }
                };
                PlayFabClientAPI.UpdateUserData(dataRequest,
                    _ => GetRanking(),
                    err => { Debug.LogWarning("時刻保存失敗: " + err.GenerateErrorReport()); GetRanking(); });

            }, error =>
            {
                Debug.Log("スコアの送信に失敗しました");
            });
        }*//*

    void SubmitClearTime(float time)
    {
        Debug.Log($"送信するスコア: {time}");

        //小数第2位まで扱いたいので100倍して整数化
        int modifiedScore = int.MaxValue - Mathf.RoundToInt(time * 100f);

        var statisticUpdate = new StatisticUpdate
        {
            StatisticName = "Stage1Ranking",
            Value = modifiedScore,
        };
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> { statisticUpdate }
        };

        //スコア送信
        PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
        {
            Debug.Log("スコアの送信に成功しました");

            //本来のfloat値もUserDataに保存しておく
            var dataRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
            {
                { "ScoreTimestamp", System.DateTime.UtcNow.Ticks.ToString() },
                { "ClearTime", time.ToString("F2") } //小数第2位まで保存
            }
            };
            PlayFabClientAPI.UpdateUserData(dataRequest,
                _ => GetRanking(),
                err => { Debug.LogWarning("時刻保存失敗: " + err.GenerateErrorReport()); GetRanking(); });

        }, error =>
        {
            Debug.Log("スコアの送信に失敗しました");
        });
    }


    ///<summary>
    ///ランキング取得メソッド
    ///</summary>
    void GetRanking()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "Stage1Ranking",
            StartPosition = 0,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, leaderboardResult =>
        {
            cachedRanking.Clear();
            var playerIds = new List<string>();

            foreach (var item in leaderboardResult.Leaderboard)
            {
                //時刻は一旦最大値（後で置き換える）
                cachedRanking.Add((item.DisplayName, item.StatValue, long.MaxValue, item.PlayFabId));
                playerIds.Add(item.PlayFabId);
            }

            //まずスコアのみで暫定表示
            UpdateRankingUI();

            int receivedCount = 0;

            //各プレイヤーの送信時刻を取得
            foreach (var pid in playerIds)
            {
                var dataRequest = new GetUserDataRequest { PlayFabId = pid };
                PlayFabClientAPI.GetUserData(dataRequest, dataResult =>
                {
                    long ts = long.MaxValue;
                    if (dataResult.Data != null && dataResult.Data.ContainsKey("ScoreTimestamp"))
                    {
                        long.TryParse(dataResult.Data["ScoreTimestamp"].Value, out ts);
                    }

                    for (int i = 0; i < cachedRanking.Count; i++)
                    {
                        if (cachedRanking[i].playFabId == pid)
                        {
                            cachedRanking[i] = (cachedRanking[i].name, cachedRanking[i].score, ts, pid);
                            break;
                        }
                    }

                    receivedCount++;

                    //全員分揃ったらソート＆再表示
                    if (receivedCount == cachedRanking.Count)
                    {
                        SortAndDisplay();
                    }

                }, err =>
                {
                    Debug.LogWarning("ユーザーデータ取得失敗: " + err.GenerateErrorReport());
                    receivedCount++;

                    if (receivedCount == cachedRanking.Count)
                    {
                        SortAndDisplay();
                    }
                });
            }

        }, error =>
        {
            Debug.Log("ランキングの取得に失敗しました");
        });
    }

    void SortAndDisplay()
    {
        cachedRanking.Sort((a, b) =>
        {
            int scoreCompare = b.score.CompareTo(a.score); //スコア降順
            if (scoreCompare != 0) return scoreCompare;
            return a.timestamp.CompareTo(b.timestamp);    //送信時刻昇順（早い方が上）
        });
        UpdateRankingUI();
    }

    //ユーザー周辺の昇順ランキングのスコア取得メソッド
    void GetRankingAroundPlayer()
    {
        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = "Stage1Ranking",
            MaxResultsCount = 11
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, leaderboardResult =>
        {
            foreach (var item in leaderboardResult.Leaderboard)
            {
                Debug.Log($"{item.Position + 1}位　プレイヤー名：{item.DisplayName}　スコア：{item.StatValue}");
            }
        }, error =>
        {
            Debug.Log("ランキングの取得に失敗しました");
        });
    }

    //時刻対応版ランキングキャッシュ
    private List<(string name, int score, long timestamp, string playFabId)> cachedRanking
        = new List<(string, int, long, string)>();

    void UpdateRankingUI()
    {
        for (int i = 0; i < textRanking.Length; i++)
        {
            if (i < cachedRanking.Count) { var r = cachedRanking[i]; textRanking[i].text = $"{i + 1}位　{r.name}　{r.score}"; }
            else
            {
                textRanking[i].text = $"{i + 1}位";
            }
        }
    }
}*/