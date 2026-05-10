# Unity 2D 上手计划（基于 UE / Lyra 经验迁移）

> 项目代号：EqZeroUT1
> 目标：用一个「瓦片地图 + 战斗为核心 + UGUI 重度使用」的 2D 小项目，系统性掌握 Unity 的工作流与核心 API。
> 风格约定：能用 Unity 共识做法的，就用 Unity 的方式（Component / ScriptableObject / Prefab Variant / Addressables / UnityEvent / Animator），不强行套 UE（UObject / GameplayAbility / DataAsset / GAS）的概念；但在「设计意图」层面会做一份对照表，方便迁移思维。

---

## 0. 项目愿景与范围（MVP）

一个俯视角或横版 2D 小型动作 / Roguelike-lite 原型：

- 一张由 Tilemap 构建的关卡（含可碰撞地形、装饰层、触发区）。
- 一个可控玩家角色：移动、跳跃或冲刺、近战 + 远程攻击、受击 / 死亡。
- 至少 2 种敌人：近战追击型、远程射击型，具备简单 AI 状态机。
- 战斗系统：HP / MP（或耐力）/ 伤害类型 / 暴击 / 击退 / 无敌帧 / DOT。
- 拾取物：金币、药水、武器；背包与装备。
- UGUI：主菜单、HUD（血条/技能图标/小地图）、暂停菜单、背包面板、设置面板、对话框、伤害飘字、Boss 血条。
- 存档：JSON 存档槽，记录玩家进度、装备、关卡解锁。
- 音频：BGM 切换、SFX 池、UI 音效。
- 一个简单的 Boss 关用于综合验证。

**不做（先砍掉）**：联网、复杂剧情系统、Shader Graph 高级特效、ECS/DOTS、URP 自定义 RenderFeature。后期作为可选扩展。

---

## 1. UE → Unity 心智迁移对照表

| UE / Lyra 概念 | Unity 等价 / 推荐做法 | 备注 |
|---|---|---|
| `UObject` / `Actor` | `GameObject` + `Component` | Unity 没有 UObject 反射根，序列化靠 `[SerializeField]` |
| `ActorComponent` | `MonoBehaviour` 组件 | 生命周期函数：`Awake / OnEnable / Start / Update / FixedUpdate / OnDisable / OnDestroy` |
| `UDataAsset` / `UPrimaryDataAsset` | `ScriptableObject`（SO） | 配置数据资产，可被 Inspector 拖引用；冷数据/共享配置首选 |
| `UDeveloperSettings` / `DefaultGame.ini` | `ScriptableObject` 单例 + `Resources` 或 Addressables | 没有原生 ini 系统 |
| `GameplayTag` | 字符串/`enum`/或第三方 `UniTag`，简单项目可用 `string` + 静态常量 | 后期可引入 `Unity.Tags` 包或自写 |
| `GAS` (GameplayAbilitySystem) | 自写 `AbilitySystemComponent` + SO 化的 `AbilityDefinition` + `EffectDefinition` | Unity 没官方 GAS，按需简化即可 |
| `Pawn` / `Character` / `CharacterMovement` | `GameObject` + `Rigidbody2D` + 自写 `PlayerMovement2D` | 2D 用 `Rigidbody2D` + `Collider2D` |
| `PlayerController` | 自写 `PlayerInputHandler` + 新版 `Input System` 包 | 推荐 `InputActionAsset` |
| `EnhancedInput` | `Input System (com.unity.inputsystem)` | 概念非常接近：Action / Binding / Map |
| `GameMode` / `GameState` | 自写 `GameManager`（DontDestroyOnLoad 单例） | 不要滥用单例 |
| `UMG` Widget | `UGUI`（Canvas + RectTransform + CanvasGroup） | UI Toolkit 是新方案，先用 UGUI 打基础 |
| `UWidgetBlueprint` 子类 | `MonoBehaviour` 挂在 UI Prefab 根 | 一个 Prefab = 一个 Widget |
| `UDataTable` | SO 列表 / CSV 导入 / `Addressables` | 推荐 SO 列表 |
| `GameplayCue` | 事件 + Prefab 池 + Animator/ParticleSystem | 自写 VFX/SFX 派发器 |
| `UGameplayStatics` | 静态工具类 / `Object.FindObjectOfType` / `Physics2D` API | 注意 `Find*` 性能 |
| `LevelStreaming` | `SceneManager.LoadSceneAsync(Additive)` | Additive 加载 + 卸载 |
| `World Partition` | 无原生对应；可用 Tilemap 分块 / Addressables 分组 | 2D 项目通常不需要 |
| `Subsystem`（Game/World/LocalPlayer） | SO 单例 / 自写 `IService` + 简易 ServiceLocator / Zenject(VContainer) | 入门期用手写单例足够 |
| `AssetManager` / Soft Reference | `Addressables`（`AssetReference`） | 推荐学习 |
| Blueprint | 没有原生可视化脚本；用 C# 即可。可视化可选 `Visual Scripting`（前 Bolt） | 不推荐入门用 |
| `UAnimInstance` / AnimBP | `Animator` + `AnimatorController`（状态机） + `Animation Events` | 2D 用 Sprite 动画或 Spine |
| `Niagara` | `ParticleSystem`（Shuriken）/ `VFX Graph` | 2D 入门用 Shuriken |
| `Sound Cue` / MetaSound | `AudioSource` + `AudioMixer` + 自写 SFX 池 | |
| Console Variables | 自写调试面板 / `Debug.Log` + `ImGui`(第三方) | |

**关键心智转变**：

1. Unity 的「资产即代码引用」更扁平。能 SO 化的配置就 SO 化，不要做成 MonoBehaviour 挂场景。
2. 没有 `BeginPlay` 的严格全局顺序；多用 `Awake` 取引用、`Start` 做依赖逻辑、`OnEnable` 订阅事件、`OnDisable` 反订阅。
3. 物理走 `FixedUpdate`，输入与表现走 `Update`，相机走 `LateUpdate`。
4. 不要在每帧 `Find` / `GetComponent`；缓存引用。
5. UGUI 重排版（Layout）开销不低，HUD 频繁更新数值用 `TextMeshPro` + 仅在变化时 `SetText`。

---

## 2. 学习路径与阶段计划

每个阶段都包含：**功能目标** / **要掌握的 Unity 知识点** / **产出物** / **UE 对照提示** / **自检题**。

文档放在 `Docs/` 下，按阶段拆分子文档逐步记录（建议命名 `02-Phase1-xxx.md` ...）。

---

### 阶段 1：环境与项目骨架

**功能目标**
- 用 2D (URP) 模板初始化项目（已存在则补齐设置）。
- 建立目录规范、命名规范、Git 忽略、`asmdef` 程序集划分。
- 跑通一个空场景 + 一个 `GameManager` 单例 + 一个 `Bootstrap` 启动场景。

**Unity 知识**
- Project Settings：Quality / Graphics / Player / Time(`fixedDeltaTime`)。
- Package Manager：Input System、2D Tilemap Extras、Cinemachine、TextMeshPro、Addressables、Universal RP（2D Renderer）。
- `asmdef` 程序集定义：拆分 `Game.Runtime` / `Game.UI` / `Game.Editor`，加快编译。
- Editor 布局、Console、Project、Hierarchy、Inspector、Scene/Game 视图。
- `EditorPrefs` vs `PlayerPrefs`。
- `.meta` 文件、GUID、Force Text 序列化。

**产出物**
- 目录：`Assets/_Project/{Art, Audio, Data, Prefabs, Scenes, Scripts, Settings, UI}`。
- `Bootstrap.unity` → 加载 `MainMenu.unity`。
- `GameManager`（DontDestroyOnLoad）。

**UE 对照**
- 类似建立 `GameInstance` + `Bootstrap Map`。

**自检**
- 解释 `.meta` 为什么必须进版本库。
- 解释 `Awake / OnEnable / Start` 的差别与触发时机。

---

### 阶段 2：输入系统（Input System）

**功能目标**
- 用新版 Input System 配置 `Move / Jump / Attack / Skill1 / Interact / OpenInventory / Pause`。
- 支持键鼠 + 手柄；运行时切换设备时 UI 提示图标改变。

**Unity 知识**
- `InputActionAsset`、`Action Map`、`Composite Binding`、`Processors`、`Interactions`。
- `PlayerInput` 组件三种回调模式（SendMessages / UnityEvents / C# Events）。
- `InputAction.performed/started/canceled` 的语义。
- 设备热插拔：`InputUser.onChange`、`InputSystem.onDeviceChange`。

**UE 对照**
- 等同 EnhancedInput 的 `IA_*` 与 `IMC_*`。

**自检**
- 为什么不要在 `Update` 里用 `Keyboard.current.aKey.isPressed` 写正式逻辑。

---

### 阶段 3：2D 物理与角色控制

**功能目标**
- 玩家可移动、跳跃（含土狼时间 Coyote Time、跳跃缓冲 Jump Buffer）、冲刺。
- 受击击退、无敌帧。

**Unity 知识**
- `Rigidbody2D`（Dynamic / Kinematic / Static）、`Collider2D`（Box/Circle/Capsule/Composite）。
- `Physics2D` 设置：Layer Collision Matrix、Gravity、`Queries Hit Triggers`。
- Layer 与 Tag、`LayerMask`。
- `OnCollisionEnter2D` vs `OnTriggerEnter2D`，触发条件。
- `Physics2D.OverlapBox/Circle/Raycast`、`ContactFilter2D`、非分配版 API（`*NonAlloc`）。
- `FixedUpdate` 内修改速度，`Update` 收集输入、`LateUpdate` 跟随相机。

**UE 对照**
- `CharacterMovementComponent` 在 Unity 没有，自己写，但更可控。

**自检**
- 解释 `Rigidbody2D.MovePosition` 与直接改 `transform.position` 的区别。

---

### 阶段 4：Tilemap 关卡

**功能目标**
- 用 Tile Palette 绘制一关：Ground / Platform（单向）/ Spike / Decoration / Foreground。
- 单向平台、可破坏砖块、机关触发（按钮开门）。

**Unity 知识**
- `Grid` + `Tilemap` + `TilemapRenderer` + `TilemapCollider2D` + `CompositeCollider2D`。
- `RuleTile`、`AnimatedTile`、`ScriptableTile`（自定义 Tile）。
- 多 Tilemap 分层（按 Sorting Layer / Order in Layer）。
- `Tilemap` 与触发器结合：自定义 Tile 在 `GetTileData` 中设置 ColliderType。
- `PlatformEffector2D`（单向平台）。
- 性能：`CompositeCollider2D` + `UsedByComposite`。

**UE 对照**
- 类比 PaperZD/PaperTilemap，但 Unity 的 Tilemap 更成熟。

**产出物**
- `Assets/_Project/Art/Tiles/` + 一张完整可玩关卡。

**自检**
- 解释 `Sorting Layer` 与 `Order in Layer` 与 Z 排序的关系。

---

### 阶段 5：相机（Cinemachine 2D）

**功能目标**
- 跟随玩家、有死区、有边界框（不超出关卡）。
- 战斗时屏幕震动；进入 Boss 区域切换镜头。

**Unity 知识**
- `CinemachineBrain` + `CinemachineVirtualCamera`（2D Framing Transposer）。
- `Confiner2D` 边界限制（用 `PolygonCollider2D` 圈关卡）。
- `Cinemachine Impulse Source/Listener`（震屏）。
- 像素完美：`Pixel Perfect Camera` 组件、PPU（Pixels Per Unit）一致性。

**UE 对照**
- 类比 Camera Modifier / Camera Shake。

---

### 阶段 6：动画

**功能目标**
- 玩家：Idle / Run / Jump / Fall / Attack1/2/3 连段 / Hit / Death。
- 通过 `Animation Events` 在攻击关键帧触发判定。

**Unity 知识**
- `Animator`、`Animation Clip`、`Animator Controller`（State Machine、Blend Tree、Sub-State Machine、Layer）。
- 参数：Float / Int / Bool / Trigger，`AnyState` 与 Transition 条件。
- `Animation Event` 回调到 MonoBehaviour 方法。
- `Sprite Atlas`（合批，减少 DrawCall）。
- 可选：`2D Animation` 包 + 骨骼动画。

**UE 对照**
- AnimBP 状态机 + AnimNotify ≈ AnimatorController + AnimationEvent。

**自检**
- 何时用 Trigger 何时用 Bool？

---

### 阶段 7：战斗系统（核心）

**功能目标**
- 抽象出：`Attribute`（HP/MP/ATK/DEF/CritRate...）、`Damage`（伤害实例：来源、类型、数值、击退方向）、`Hitbox / Hurtbox`、`Effect`（buff/debuff/DOT）、`Ability`（技能定义）。
- 玩家三连击、可蓄力技能、可冷却技能。
- 敌人有「攻击意图预警 → 出招 → 后摇」节奏。
- 伤害飘字、命中停帧（Hit Stop）、击退、屏震、SFX/VFX。

**架构建议（Unity 化的精简 GAS）**
- `AttributeSet : MonoBehaviour`（持有运行时属性，触发 `OnChanged` 事件）。
- `AbilityDefinition : ScriptableObject`（冷数据：冷却、消耗、动画状态名、判定时间窗）。
- `AbilitySystem : MonoBehaviour`（运行时实例化 ability，处理冷却、消耗、激活）。
- `EffectDefinition : ScriptableObject`（即时/持续修饰，作用于 AttributeSet）。
- `Hitbox`（攻击帧期间启用的 `Collider2D`，带 `team`/`ownerAbility`）。
- `Hurtbox`（接收伤害的 `Collider2D`）。
- `DamageContext`（`struct`：source, value, knockback, type, isCrit）。
- `IDamageable` 接口。

**Unity 知识**
- ScriptableObject 作为配置；运行时是否需要克隆（共享 vs 实例属性）。
- C# `event` / `Action` vs `UnityEvent`：性能与可视化的取舍。
- 协程 `Coroutine` 与 `WaitForSeconds(Realtime)`；或 `UniTask`（推荐学，但可放后期）。
- `Time.timeScale` 实现 Hit Stop（注意 UI/音频的时间尺度）。
- 对象池（`UnityEngine.Pool.ObjectPool<T>`）。

**UE 对照**
- 把 Lyra 的 GAS 做"白盒"重写一遍是绝佳学习路径，但要克制：先把"伤害能进出、技能能放、buff 能叠"跑通。

**自检**
- 为什么不要把 ScriptableObject 当成可变运行时容器（多个角色共享会串数据）？

---

### 阶段 8：AI（敌人行为）

**功能目标**
- 近战敌人：巡逻 → 发现 → 追击 → 攻击 → 后摇 → 复位。
- 远程敌人：保持距离 → 预瞄 → 射击 → 走位。
- 一个 Boss：多阶段、招式表。

**Unity 知识**
- 简易状态机（`enum` + `switch`，或自写 `IState`）。
- 可选：`Behavior Designer`/`NodeCanvas` 等行为树插件，先用手写状态机。
- 视野检测：`Physics2D.OverlapCircle` + `Raycast` 验证遮挡。
- 寻路（2D）：自写网格寻路 / `A* Pathfinding Project`（免费版）。Tilemap 上做 A* 入门很合适。

**UE 对照**
- BehaviorTree + Blackboard 的思维可保留，但 Unity 入门期手写更清晰。

---

### 阶段 9：UGUI（重头戏）

**功能目标**
- 主菜单 / 设置（分辨率、音量、按键重绑）/ 暂停 / HUD / 背包 / 装备 / 商店 / 对话 / 死亡结算 / Boss 血条 / 伤害飘字 / Toast。

**Unity 知识**
- `Canvas` 三种 Render Mode：Screen Space - Overlay / Camera / World Space。
- `CanvasScaler`（`Scale With Screen Size` + Reference Resolution）。
- `RectTransform`：Anchor / Pivot / Anchored Position；与 Layout 的组合。
- 布局组件：`HorizontalLayoutGroup` / `VerticalLayoutGroup` / `GridLayoutGroup` / `ContentSizeFitter` / `LayoutElement`。
- `EventSystem`、`GraphicRaycaster`、`IPointerXxxHandler`、`ISelectHandler`。
- `TextMeshPro`：字体资产、Fallback、富文本、SDF。
- `Button` / `Toggle` / `Slider` / `ScrollRect` + `ScrollView` 的虚拟化（大列表用 `LoopScrollRect` 或自写）。
- `CanvasGroup`（控制 alpha、interactable、blocksRaycasts，做淡入淡出）。
- UI 与游戏状态解耦：MV(VM/P) 模式；推荐"UI 监听数据事件"，不让数据反向依赖 UI。
- 资源：UI Atlas / `SpriteAtlas`、九宫格 `Image (Sliced)`。
- 动画：`DOTween`（推荐学）/ `Animator` 控制 UI。
- 多分辨率适配：Safe Area（移动）、宽高比变化。
- 本地化：`com.unity.localization` 包。

**UI 架构建议**
- `UIManager`：管理打开/关闭、栈式导航（按 ESC 回上一层）、模态遮罩。
- `UIPanel` 基类：`Show/Hide/OnFocus/OnBlur`。
- 数据驱动：`PlayerStats` 触发 `OnHpChanged` → HUD 监听更新血条。

**UE 对照**
- UMG 的 Widget Switcher / Overlay 概念都能在 UGUI 用 GameObject 树 + 激活/隐藏复刻。

**自检**
- 为什么频繁修改 `Text` 文本会触发 Canvas 重建？如何减少重建（拆 Canvas、用 TMP）？

---

### 阶段 10：数据、存档、配置

- SO 配置库：物品表、技能表、敌人表、关卡表。
- 存档：JSON（`JsonUtility` 或 Newtonsoft）写入 `Application.persistentDataPath`。
- 多存档槽、版本号迁移。
- 简单加密 / 校验（防手改，非反外挂）。

---

### 阶段 11：音频

- `AudioMixer`：Master / BGM / SFX / UI 分组，暴露参数做音量滑条。
- BGM 交叉淡化、场景切换续播。
- SFX 池，避免 `AudioSource.PlayClipAtPoint` 滥用。
- UI 全局音效（按钮 hover/click）。

---

### 阶段 12：场景管理与流程

- `SceneManager.LoadSceneAsync`（Single / Additive）。
- 加载界面（进度条 + 假进度）。
- Bootstrap → MainMenu → Gameplay 的流程。
- Additive：常驻 `Persistent` 场景放 Manager，Gameplay 场景做内容。

---

### 阶段 13：资源管理（Addressables）

- 远程/本地分组、标签、`AssetReference`、异步加载与释放。
- 用于：UI 图集按需加载、关卡 Prefab、配置表。
- 与 SO 配置库结合。

---

### 阶段 14：性能与调试

- `Profiler`、`Frame Debugger`、`Memory Profiler`（包）。
- DrawCall 优化：`SpriteAtlas`、合并材质、合理 Sorting Layer。
- GC：避免 `foreach` 装箱、避免每帧 `new`、缓存字符串、对象池。
- 物理优化：合理 `Fixed Timestep`、关闭睡眠物体。
- 编辑器调试：`Gizmos.DrawWireSphere`、`Debug.DrawLine`、自定义 `Editor` / `PropertyDrawer`。

---

### 阶段 15：构建与发布

- Build Settings、平台切换（PC/Android）。
- Player Settings：图标、启动画面、脚本后端 (Mono/IL2CPP)、Managed Stripping Level。
- 资产管线：纹理压缩平台覆盖。
- 命令行构建（CI 雏形）。

---

### 阶段 16（可选扩展）

- Shader Graph 2D（描边、闪白、溶解）。
- URP 2D Lights / Normal Map。
- VContainer / Zenject 做 DI。
- UniTask 替代部分 Coroutine。
- ECS / DOTS 学习（独立项目，不混进本项目）。

---

## 3. 推荐目录结构

```
Assets/
  _Project/
    Art/              # Sprites, Tiles, Atlas, VFX
    Audio/            # Clips, Mixer
    Data/             # ScriptableObject 实例（配置数据）
    Prefabs/
      Characters/
      Enemies/
      Items/
      VFX/
      UI/
    Scenes/
      Bootstrap.unity
      MainMenu.unity
      Gameplay_01.unity
    Scripts/
      Runtime/
        Core/         # GameManager, Services, Events
        Input/
        Gameplay/
          Combat/     # Attribute/Ability/Effect/Hitbox
          Player/
          Enemies/
          AI/
          Items/
          Tilemap/
        UI/
        Audio/
        Save/
      Editor/
    Settings/         # URP, Input Actions, AudioMixer
    UI/               # UI Sprites, Fonts, TMP Assets
```

每个 Runtime/Editor 文件夹下放一个 `*.asmdef`。

---

## 4. 命名与代码规范（建议）

- C# 命名遵循微软规范：`PascalCase` 类型/方法/属性，`camelCase` 局部/参数，`_camelCase` 私有字段。
- `[SerializeField] private Foo _foo;` 优于 `public Foo foo;`。
- Unity 事件函数显式 `private`。
- 一个文件一个公共类型，文件名 = 类型名。
- ScriptableObject 加 `[CreateAssetMenu(menuName = "EqZero/...")]`。
- 资源命名：`SPR_Player_Idle_01`、`SO_Item_Potion_Small`、`UI_Panel_Inventory`。

---

## 5. 后续文档落点

按阶段在 `Docs/` 增量记录，建议结构：

- `02-Phase1-Bootstrap.md`
- `03-Phase2-Input.md`
- `04-Phase3-Movement2D.md`
- `05-Phase4-Tilemap.md`
- `06-Phase5-Cinemachine.md`
- `07-Phase6-Animation.md`
- `08-Phase7-Combat.md`
- `09-Phase8-AI.md`
- `10-Phase9-UGUI.md`
- `11-Phase10-Data-Save.md`
- `12-Phase11-Audio.md`
- `13-Phase12-Scene-Flow.md`
- `14-Phase13-Addressables.md`
- `15-Phase14-Performance.md`
- `16-Phase15-Build.md`
- `99-Glossary-UE-vs-Unity.md`（持续完善的对照表）
- `99-Pitfalls.md`（踩坑记录）

每篇按统一模板：**目标 / 实现要点 / 关键 API / 与 UE 差异 / 遇到的问题 / 验证方式 / 待办**。

---

## 6. 第一周建议节奏（可执行起步）

1. 完成阶段 1：项目骨架 + 目录 + Bootstrap 流程。
2. 完成阶段 2：Input System 跑通 Move/Jump，能输出日志。
3. 完成阶段 3：玩家在空场景里能移动 + 跳跃。
4. 完成阶段 4：用 Tilemap 画一小段地形，玩家能站立、踩单向平台。
5. 给玩家加 Idle/Run/Jump 动画（阶段 6 的最小子集）。
6. 写一个 HUD 雏形：左上角显示 HP（阶段 9 的最小子集）。

完成上述 6 步后，再回头进入阶段 7 战斗系统的深水区。

---

## 7. 给"UE 老兵"的几条提醒

- **不要复刻 GAS 的全部抽象**。先做"能打人、能掉血、能放技能"的最小闭环，再按需抽象。
- **不要把 ScriptableObject 当 UObject 用**。SO 是资产/配置，运行时如需可变状态请克隆或在 MonoBehaviour 上保存。
- **不要无脑单例化 Manager**。能注入就注入，能用事件就用事件。
- **不要在 Tick 里 `GetComponent` / `Find`**。在 `Awake` 缓存。
- **UI 别和数据双向耦合**。数据先变 → 事件通知 → UI 刷新。
- **协程不是万能**。状态机 + 事件 比一长串嵌套协程更易维护。
- **Prefab Variant 是好朋友**。基础敌人 Prefab + 多个 Variant 派生不同数值/外观。
- **善用 `[Header] [Tooltip] [Range] [SerializeReference]`** 让 Inspector 可读。

---

> 本文档是骨架，后续每个阶段单独成文记录"做了什么 / 学到了什么 / 踩了什么坑"。逐步累积，最终汇成系统性的 Unity 2D 知识地图。
