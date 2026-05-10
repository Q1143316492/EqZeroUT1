using UnityEngine;

namespace EqZero.Core
{
    /// <summary>
    /// 全局单例管理器，随 Bootstrap 场景常驻，跨场景不销毁。
    /// 对标 UE 的 GameInstance。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // DontDestroyOnLoad 只对根节点有效；若挂在子节点下则先脱离父级
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
