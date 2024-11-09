using ChatdollKit.IO;
using ChatdollKit.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatdollKit.Demo
{
    public class HandWaveDetector : MonoBehaviour
    {
        // 履歴として保持するフレーム数
        private const int FRAME_HISTORY = 10;

        // 動きを検出する閾値
        private const float MOVEMENT_THRESHOLD = 0.1f;

        // 手を振ったと判定するのに必要なフレーム数
        private const int WAVE_FRAMES = 3;

        // モデルのアニメーション制御を行うコントローラー
        private ModelController modelController;

        // カメラからフレームを取得するためのコンポーネント
        private SimpleCamera simpleCamera;

        // 過去のフレームの平均色を保持するキュー
        private Queue<Color> averageColors = new Queue<Color>();

        // 手を振る動作を検出するためのフレームカウント
        private int waveDetectedFrames = 0;

        // 初期化処理
        async void Start()
        {
            // ModelControllerの取得
            modelController = GetComponent<ModelController>();

            // SimpleCameraの取得
            simpleCamera = FindObjectOfType<SimpleCamera>();
            if (simpleCamera == null)
            {
                // SimpleCameraが見つからない場合はエラーメッセージを出力
                Debug.LogError("SimpleCamera component not found in the scene.");
                return;
            }

            // カメラが起動した時に実行されるイベントを購読
            simpleCamera.OnCameraStarted += OnCameraStarted;
        }

        // カメラ起動後に呼び出される処理
        private void OnCameraStarted()
        {
            Debug.Log("Start AnalyzeFrames."); // デバッグメッセージ
            StartCoroutine(AnalyzeFrames()); // フレーム解析のコルーチンを開始
        }

        // フレームを継続的に解析するコルーチン
        IEnumerator AnalyzeFrames()
        {
            // カメラのテクスチャを取得
            var webCamTexture = simpleCamera.GetGetWebCamTexture();
            while (true)
            {
                // テクスチャが再生中かつ新しいフレームが更新されている場合
                if (webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
                {
                    // フレームを解析して手を振る動作を検出
                    if (DetectHandWave(webCamTexture.GetPixels32()))
                    {
                        // 手を振るアニメーションを再生
                        PlayWaveAnimation();
                    }
                }
                // 次のフレームまで待機
                yield return new WaitForEndOfFrame();
            }
        }

        // 手を振る動作を検出する処理
        bool DetectHandWave(Color32[] pixels)
        {
            // 現在のフレームの平均色を計算
            Color averageColor = CalculateAverageColor(pixels);

            // 平均色を履歴キューに追加
            averageColors.Enqueue(averageColor);
            if (averageColors.Count > FRAME_HISTORY)
            {
                // キューが履歴の上限を超えた場合、古い色を削除
                averageColors.Dequeue();
            }

            // 十分なフレーム数が集まったら解析を開始
            if (averageColors.Count == FRAME_HISTORY)
            {
                // 履歴の平均色の変化量を計算
                float totalMovement = CalculateTotalMovement();

                // 変化量が閾値を超えた場合、手を振る動作のカウントを増加
                if (totalMovement > MOVEMENT_THRESHOLD)
                {
                    waveDetectedFrames++;
                    // 指定されたフレーム数に達したら手を振る動作を検出と判定
                    if (waveDetectedFrames >= WAVE_FRAMES)
                    {
                        waveDetectedFrames = 0; // カウントをリセット
                        return true; // 手を振る動作を検出
                    }
                }
                else
                {
                    // 動きが小さい場合はカウントをリセット
                    waveDetectedFrames = 0;
                }
            }

            return false; // 手を振る動作を検出していない
        }

        // 手を振るアニメーションを再生する処理
        void PlayWaveAnimation()
        {
            // ModelControllerを使用して手を振るアニメーションを取得し、再生
            var waveAnimation = modelController.GetRegisteredAnimation("waving_arm", 3.0f);
            modelController.Animate(new List<Model.Animation> { waveAnimation });
        }

        // ピクセルから平均色を計算するメソッド
        private Color CalculateAverageColor(Color32[] pixels)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < pixels.Length; i++)
            {
                sum.x += pixels[i].r; // 赤の値を加算
                sum.y += pixels[i].g; // 緑の値を加算
                sum.z += pixels[i].b; // 青の値を加算
            }
            // 平均色を計算してColorとして返す
            sum /= pixels.Length;
            return new Color(sum.x / 255f, sum.y / 255f, sum.z / 255f);
        }

        // 色の変化量を計算するメソッド
        private float CalculateTotalMovement()
        {
            float totalMovement = 0f;
            // 履歴の色データを配列に変換
            Color[] colorArray = averageColors.ToArray();

            // 履歴のフレーム間で色の変化量を計算
            for (int i = 1; i < colorArray.Length; i++)
            {
                totalMovement += ColorDifference(colorArray[i], colorArray[i - 1]);
            }

            return totalMovement; // 合計の変化量を返す
        }

        // 2つの色間の差を計算するメソッド
        private float ColorDifference(Color c1, Color c2)
        {
            // RGB成分の差の合計を返す
            return Mathf.Abs(c1.r - c2.r) + Mathf.Abs(c1.g - c2.g) + Mathf.Abs(c1.b - c2.b);
        }
    }
}
