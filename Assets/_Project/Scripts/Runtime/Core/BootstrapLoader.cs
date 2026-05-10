using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EqZero.Core
{
    /// <summary>
    /// 挂在 Bootstrap 场景的根 GameObject 上。
    /// Bootstrap 场景是 Build Settings 里 index=0 的场景，游戏启动后第一帧运行。
    /// 它负责初始化常驻系统（GameManager 等），然后跳转到 MainMenu。
    /// 对标 UE 的 Bootstrap Map + GameInstance 初始化流程。
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        [Header("跳转目标")]
        [SerializeField] private string _firstSceneName = "MainMenu";

        [Header("可选：最短停留时间（用于显示启动画面）")]
        [SerializeField] [Range(0f, 5f)] private float _minimumDisplayTime = 0f;

        private IEnumerator Start()
        {
            // 等待最短停留时间（可设为 0 跳过）
            if (_minimumDisplayTime > 0f)
                yield return new WaitForSecondsRealtime(_minimumDisplayTime);

            SceneManager.LoadScene(_firstSceneName);
        }
    }
}
