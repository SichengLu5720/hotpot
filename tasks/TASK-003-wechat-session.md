# 微信小游戏平台与每日会话

> 使用 `workspace task` 创建，不要直接复制未绑定模板派发。下方唯一 `harness-state` 是状态、合同、批准和运行记录的机器权威源；正文是 PM 的可读工作表与索引，不能覆盖机器块。
> 本文由 PM 维护，子代理返回结果后由 PM 通过统一入口接收。删除不适用的正文区块时保留原因；未知信息写“待确认”，不能把空白或默认值当作用户决定。
> Task 阶段只维护 QA Intent；版本交付时由 **Designer 制定 QA 计划、用例、断言与接口需求，Builder 编写或复用脚本，普通 Runner 执行**。

```harness-state
{
  "schema_version": 1,
  "kind": "task",
  "id": "TASK-003",
  "workspace": "C:\\Users\\charlielu\\Documents\\ChatGPT\\下锅喽-worktrees\\TASK-003-wechat-session",
  "branch": "task/TASK-003-wechat-session",
  "base_commit": "4639e070f78d520609d3ae8256d87e202d75cde3",
  "runs": {
    "run-d7945ff46a394a21": {
      "run_id": "run-d7945ff46a394a21",
      "step": "designer",
      "role": "feature_designer",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:14:59.879074+00:00",
      "inputs": {
        "task_revision": 2,
        "contract_digest": "855fe28bd9949847e0e8a6f179eedb859b628e07153b7739bbb4e374a3d1bc74"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "low",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [],
      "stop_confirmed": true,
      "approved_inputs": {},
      "handoff_digest": "445e99b8993885c3d222ddf0bd60bcbc46e098e201222b22e350e6fd345b0207",
      "accepted_at": "2026-09-20T09:17:26.548460+00:00",
      "payload": {
        "interface_notes": {
          "identity": {
            "task_revision": 2,
            "contract_digest": "855fe28bd9949847e0e8a6f179eedb859b628e07153b7739bbb4e374a3d1bc74",
            "base_commit": "4639e070f78d520609d3ae8256d87e202d75cde3",
            "scope": "接口事实与实现要求，不是版本 QA Plan 或通过报告。"
          },
          "interfaces": [
            {
              "name": "ChallengeContext",
              "source": "Unity/Assets/HotpotSort/Contracts",
              "parameters": "challengeId/contentVersion/configurationDigest/timeSource 非空，retryIndex 非负",
              "requirements": "冻结活动局上下文，重试保持日期与版本；C 自有观察提供 fallbackReason/resolvedUtc。",
              "called": false
            },
            {
              "name": "IGameSessionFactory/IGameSession",
              "source": "Unity/Assets/HotpotSort/Contracts",
              "parameters": "CreateSession(ChallengeContext), Pause, Resume, Dispose",
              "requirements": "每次重试新建实例，取消旧事件并拒绝过期回调；提供状态、暂停原因、活动用时、实例身份和错误的只读观察。",
              "called": false
            },
            {
              "name": "IGameViewFactory/IGameView/ISessionActions",
              "source": "Unity/Assets/HotpotSort/Contracts",
              "requirements": "重试先清理旧动画/物理再绑定新会话；保留 Aborted/Failed 语义；适配层转换微信安全区到 Unity 左下原点像素坐标。",
              "called": false
            },
            {
              "name": "ITimeProvider/IMonotonicClock/IPlatformLifecycleAdapter",
              "source": "机器合同与官方 WXSDK Runtime/WX.cs",
              "confirmed_signatures": "WX.OnHide/OffHide, WX.OnShow/OffShow",
              "requirements": "用户暂停与后台原因独立幂等；暂停不计时；跨午夜不替换活动局；可信失败与设备回退可注入；订阅/取消订阅成对。",
              "called": false
            },
            {
              "name": "IDiagnosticSink/ReplayPackage/RuntimePaths",
              "source": "Unity/Assets/HotpotSort/Contracts",
              "requirements": "本地写入确认成功后才返回 Success；实际消费隔离的 DataRoot/CacheRoot/BuildRoot；不声称远程上传。",
              "called": false
            }
          ],
          "toolchain": {
            "unity_candidate": "2022.3.62f2 (6896052288fd)",
            "wx_sdk_commit": "a09d4b29daa1dd8358b09b5b5639554ab08cfdc2",
            "documented_support": "官方 CHANGELOG v0.1.29 声明 Unity 2021/2022 兼容；依赖身份以精确 Git SHA 为准，不使用 package.json 的滞后 0.1.1 字符串代表最新版。",
            "appid": "官方字段 ProjectConf.Appid；构建入口仅从环境配置注入，缺失明确失败，不提交真实值。",
            "verification_status": "只读源代码/文档核对；未安装 SDK、未导出、未上传、未实机验证。"
          },
          "remaining_gaps": [
            "真实 A/B 工厂未交付；可隔离开发 C，但生产 Boot 缺工厂必须明确失败。",
            "未找到可运行 Unity 编辑器；官方兼容声明不等于 2022.3.62f2 已编译通过。",
            "QI-007 黄金回放与 QI-008 真机证据留待版本阶段。"
          ]
        }
      },
      "summary": "已核对 revision 2 机器合同与 K0 真实接口，完成 Task3 只读技术交接；未修改文件或 QA Intent，未运行游戏或测试。",
      "artifacts": []
    },
    "run-93c8465cef034625": {
      "run_id": "run-93c8465cef034625",
      "step": "code",
      "role": "code_builder",
      "status": "REVOKED",
      "created_at": "2026-09-20T09:17:38.542149+00:00",
      "inputs": {
        "task_revision": 2,
        "contract_digest": "855fe28bd9949847e0e8a6f179eedb859b628e07153b7739bbb4e374a3d1bc74"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "low",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [
        "Unity/Assets/HotpotSort/Runtime/Session",
        "Unity/Assets/HotpotSort/Runtime/Platform/WeChat",
        "Unity/Assets/HotpotSort/Runtime/Bootstrap",
        "Unity/Assets/HotpotSort/Scenes/Boot.unity",
        "Unity/Assets/HotpotSort/Scenes/Boot.unity.meta",
        "Unity/Assets/HotpotSort/Plugins/WeChat",
        "Unity/Assets/HotpotSort/Editor/WeChatBuild",
        "Unity/Packages",
        "Unity/ProjectSettings",
        "build/wechat"
      ],
      "stop_confirmed": true,
      "approved_inputs": {
        "build": {
          "task_revision": 2,
          "contract_digest": "855fe28bd9949847e0e8a6f179eedb859b628e07153b7739bbb4e374a3d1bc74",
          "decision": "2026-09-20 用户指令‘实行task3’，并回复‘同意’采用已列明默认方案",
          "artifacts": []
        }
      },
      "stop_evidence": "Code Builder 已返回 NEEDS_CLARIFICATION 并确认停止写入；宿主任务已完成，无在途进程"
    },
    "run-3fbb0d66055346ba": {
      "run_id": "run-3fbb0d66055346ba",
      "step": "designer",
      "role": "feature_designer",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:24:50.897780+00:00",
      "inputs": {
        "task_revision": 3,
        "contract_digest": "a41b523b690644f19f20c1c8bde89c4aa741c1076f63f455f4b3b43c03fa9836"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "low",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [],
      "stop_confirmed": true,
      "approved_inputs": {},
      "handoff_digest": "e34340a53d58d2f5268d144156e441312de26034b5be3665e0aa484481bbd5e8",
      "accepted_at": "2026-09-20T09:29:36.563199+00:00",
      "payload": {
        "interface_notes": {
          "identity": {
            "task_revision": 3,
            "contract_digest": "a41b523b690644f19f20c1c8bde89c4aa741c1076f63f455f4b3b43c03fa9836"
          },
          "local_engine": {
            "path": "D:/GameDev/Tools/Unity/6000.0.26f1/Editor/Unity.exe",
            "version": "6000.0.26f1_ccb7c73d2c02",
            "modules": [
              "WebGLSupport",
              "windowsstandalonesupport"
            ],
            "instantgame": "安装树与内置 PackageManager 均未发现 Unity.InstantGame/com.unity.instantgame 或未知 GUID。"
          },
          "official_sdk": {
            "commit": "a09d4b29daa1dd8358b09b5b5639554ab08cfdc2",
            "unity6_evidence": "CHANGELOG 及源码 UNITY_6000 分支显示 Unity 6 适配意图，不代表已在本项目通过。",
            "editor_dependency": "WxEditor 无条件引用 Unity.InstantGame.Editor；直接 InstantGame 调用由 UNITY_INSTANTGAME 保护，当前普通 Unity WebGL 路线不启用 AutoStreaming。",
            "runtime_dependency": "Wx asmdef 唯一引用未知 GUID 39e0a8d734341a748a11d45f50641371；固定官方归档内无定义，不得猜测身份。Runtime 源码实际需要 UnityEngine.UI/EventSystems。",
            "alternative_revision": "TuanJie tag 仍含相同缺失引用，无证据表明降级能解决。"
          },
          "builder_requirements": [
            "在 Unity/Packages 中使用可追踪的嵌入式 SDK 或等价可复现补丁，记录上游 commit、原文件摘要与补丁差异，不修改共享 PackageCache。",
            "普通 Unity WebGL 不使用 AutoStreaming；可移除 WxEditor 的悬空 Unity.InstantGame.Editor 引用，保持 UNITY_INSTANTGAME 未定义，不为整个 WxEditor 增加宏约束。",
            "不猜测未知 GUID；移除悬空引用后以已固定的 Unity 6 UGUI 程序集名称引用满足真实源码依赖，必须用最小脚本编译诊断确认。",
            "编译依赖明确后继续会话、时间、诊断、Bootstrap 实现；修复旧初稿 busy 清理等已知问题。"
          ],
          "stop_conditions": [
            "补丁后发现对 InstantGame/团结专用程序集的硬依赖时停止，不创建假程序集掩盖错误。",
            "需启用 AutoStreaming、换引擎发行版、改共享 Contracts 或移除产品能力时回 PM。",
            "编译成功仅表示依赖阻断解除，不代表微信导出或真机通过。"
          ]
        }
      },
      "summary": "完成 revision 3 Unity 6 迁移的只读调查；未修改文件、未启动 Unity、未编译或执行游戏。",
      "artifacts": []
    },
    "run-a7aef77a25184b78": {
      "run_id": "run-a7aef77a25184b78",
      "step": "code",
      "role": "code_builder",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:29:37.028680+00:00",
      "inputs": {
        "task_revision": 3,
        "contract_digest": "a41b523b690644f19f20c1c8bde89c4aa741c1076f63f455f4b3b43c03fa9836"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "low",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [
        "Unity/Assets/HotpotSort/Runtime/Session",
        "Unity/Assets/HotpotSort/Runtime/Platform/WeChat",
        "Unity/Assets/HotpotSort/Runtime/Bootstrap",
        "Unity/Assets/HotpotSort/Scenes/Boot.unity",
        "Unity/Assets/HotpotSort/Scenes/Boot.unity.meta",
        "Unity/Assets/HotpotSort/Plugins/WeChat",
        "Unity/Assets/HotpotSort/Editor/WeChatBuild",
        "Unity/Packages",
        "Unity/ProjectSettings",
        "build/wechat"
      ],
      "stop_confirmed": true,
      "approved_inputs": {
        "build": {
          "task_revision": 3,
          "contract_digest": "a41b523b690644f19f20c1c8bde89c4aa741c1076f63f455f4b3b43c03fa9836",
          "decision": "2026-09-20 用户回复‘实行’，授权 Unity 6000.0.26f1 迁移及针对官方微信 SDK 的可追踪兼容补丁与最小编译诊断",
          "artifacts": []
        }
      },
      "handoff_digest": "a61c78ee13fade6f73f9f06924ae17d529b185c7e88155af1553080b47c576b8",
      "accepted_at": "2026-09-20T09:37:32.464667+00:00",
      "payload": {
        "implementation_facts": {
          "entrypoints": [
            "SessionController.StartTodayAsync/Request/Retry/Exit/Dispose",
            "TimeResolver.ResolveAsync",
            "WeChatPlatform.InitializeAsync/Start/Dispose/FromTopLeft",
            "LocalDiagnostics.SaveAsync",
            "Bootstrap.Start + ProductionComposition",
            "HotpotSort.Build.WeChatBuild.Export/CompilePlayerDiagnostic"
          ],
          "observations": [
            "SessionController.Generation/Pauses/Snapshot/Error/IsBusy/ActiveSeconds/CanAcceptInput",
            "ResolvedChallenge.Context/ResolvedUtc/FallbackReason",
            "LocalDiagnostics.Paths/LastResult",
            "Bootstrap.Status/IsConfigured/Controller/Diagnostics"
          ],
          "behavior": [
            "会话代次与订阅清理阻止旧回调影响新会话；重试创建新 core/view，冻结日期版本，退出重进重新解析日期。",
            "用户与后台暂停原因分离幂等；非 Running 不累计活动时间，终局不会由 Resume 复活。",
            "微信 OnHide/OffHide、OnShow/OffShow 成对；SDK 初始化成功后才组合，安全区转换为 Unity 左下像素坐标。",
            "本地文件成功写入后才报告成功，微信使用 WX 文件系统；实际采用 DataRoot/CacheRoot，构建入口采用 RuntimePaths.BuildRoot。",
            "Bootstrap 缺少真实 ProductionComposition 时明确失败，拒绝开发替身；未添加任何替身。"
          ],
          "toolchain": {
            "unity": "6000.0.26f1",
            "sdk_commit": "a09d4b29daa1dd8358b09b5b5639554ab08cfdc2",
            "patch_record": "Unity/Packages/WXSDK_COMPATIBILITY.md",
            "diagnostic": "build/wechat/compile-05-final.log",
            "result": "HOTPOT_WEBGL_PLAYER_SCRIPTS_OK assemblies=7; Unity exit 0",
            "earlier_diagnostics": "compile-01 缺 AssetBundle 内置模块、compile-03 诊断脚本 Length/Count 错误均已修复；保留日志。"
          },
          "known_errors": [],
          "untested": [
            "无正式游戏 QA、无完整 WebGL 导出、无微信开发工具运行或真实设备验证。",
            "真实 A/B 工厂、输入/历史回放协议尚未交付；当前生产 Boot 按合同失败。ProductionComposition 必须绑定真实诊断输出和活动时间观察，不能把当前组合称为可玩游戏。",
            "Unity 自动生成父目录 metadata 在 allowed_paths 之外，按 PM 指令未列 artifacts，由 PM 单写者协调。",
            "SDK 管理的缓存策略保持官方实现；平台设备上的存储可用性与隔离效果尚待实机验证。",
            "Unity ProjectSettings 迁移序列化包含尾随空白；不是 C# 编译错误。"
          ],
          "scope": "仅授权工作区及 allowed_paths 内实现，无 Git 提交，无上传/提审/发布/体验版激活。"
        }
      },
      "summary": "已实现 Task3 会话、时间、微信生命周期、安全区、本地诊断、环境 AppID 构建入口与生产组合保护；完成 Unity 6 官方 SDK 可追踪兼容迁移及最终 WebGL 玩家脚本编译诊断。",
      "artifacts": [
        {
          "path": "Unity/Assets/HotpotSort/Editor/WeChatBuild/HotpotSort.WeChatBuild.asmdef",
          "sha256": "98c3995ad2673a099b3e494c1e51789edb96f388d934baa41cd3aa337d1de830",
          "bytes": 184,
          "artifact_id": "artifact-db7aac2939254c90",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Editor/WeChatBuild/HotpotSort.WeChatBuild.asmdef.meta",
          "sha256": "dd3bde40ac14b407e7973f8b4a17c3abb78f8c5fddd6ff6e43d4209509005334",
          "bytes": 166,
          "artifact_id": "artifact-210f7b2058974777",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Editor/WeChatBuild/WeChatBuild.cs",
          "sha256": "e1ad718b456d821e751e1f4e8d1c07131c4a5002b61f5d374920caeec1cd2495",
          "bytes": 3261,
          "artifact_id": "artifact-be539e1aa9c74c6c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Editor/WeChatBuild/WeChatBuild.cs.meta",
          "sha256": "fa4d3cdd6323bd45e211c102e45f7e0e1bce1581590e5fb18e4ffca5311f598e",
          "bytes": 59,
          "artifact_id": "artifact-364826d870a7493e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Bootstrap/ProductionComposition.cs",
          "sha256": "bb25d609fe1a700fdc60aea5f00a7ef5e5ad4c901f3a777544c73b91ac92b69d",
          "bytes": 978,
          "artifact_id": "artifact-fd44f7fbcf7c4ff2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Bootstrap/ProductionComposition.cs.meta",
          "sha256": "d5de9efba61a530dce5793f76bc7f04c498e69ae8bef4d2575a0c76e44a7be37",
          "bytes": 59,
          "artifact_id": "artifact-55f2bdb057564535",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/HotpotSort.WeChat.asmdef",
          "sha256": "022cde2567211415b45748933813cf7790338c00b4b0fe1072502ae8b11b1229",
          "bytes": 147,
          "artifact_id": "artifact-389209a0a2e84b55",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/HotpotSort.WeChat.asmdef.meta",
          "sha256": "5d9469934aa5ab8854f1ad003ec03c90894f475f9cb541f1a3451270a838111f",
          "bytes": 166,
          "artifact_id": "artifact-7541d69f3534403e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/LocalDiagnostics.cs",
          "sha256": "517be655c694cec61c2ad3da767113eea35565bedbb03ebe0a75285e32556d6a",
          "bytes": 2113,
          "artifact_id": "artifact-91949417b0664bef",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/LocalDiagnostics.cs.meta",
          "sha256": "ca6dc06e3672d912659b339d03d3fd421bf85ca66f2389d25f3f92463014303f",
          "bytes": 59,
          "artifact_id": "artifact-4540e30254c4490e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatPlatform.cs",
          "sha256": "688b4946536bc939cc9c5d51ae01a558cdb6520092c5b1faae360dcd2604b76f",
          "bytes": 3319,
          "artifact_id": "artifact-dd8830796e084735",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatPlatform.cs.meta",
          "sha256": "588f30a4f97a63a1765d563ab431628ea94e4ae016017ebace8cd8cd1f7ac81e",
          "bytes": 59,
          "artifact_id": "artifact-15552b11aa2e47ab",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Session/HotpotSort.Session.asmdef",
          "sha256": "94ba3bfa6998551b44f645f65060864b60afadf152d3a3aafd304d229fc769d8",
          "bytes": 142,
          "artifact_id": "artifact-ed3398e2d5344cf5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Session/HotpotSort.Session.asmdef.meta",
          "sha256": "efa78ca47092d44e0092fa2762f0663c703c2edf6e7e3903a7af341311d7db04",
          "bytes": 166,
          "artifact_id": "artifact-35be16ba42a14cca",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Session/SessionController.cs",
          "sha256": "c3e0567b1c36222cb73eec5fa5e71db6dde4dfe9a1cdb0d45dd66da53b057243",
          "bytes": 8579,
          "artifact_id": "artifact-28b8ad71bc1e446e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Session/SessionController.cs.meta",
          "sha256": "0263e8fdd492e0bc6567eb8e935d89b874f4f50f05382e2f1d81d94ec04ae0f1",
          "bytes": 59,
          "artifact_id": "artifact-fcb958f336aa4aff",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Session/TimeResolver.cs",
          "sha256": "cc5f2bb1a2b56dd8b61f1ad990537b85208c38f9015e1fb682b625da3462a334",
          "bytes": 2054,
          "artifact_id": "artifact-ed7853cfd8cc4b95",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Session/TimeResolver.cs.meta",
          "sha256": "75daeae913729f957d27d1cbc292973a7cf437cef255e49a2b043d025112a019",
          "bytes": 59,
          "artifact_id": "artifact-93dc1b3471ea4856",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/WXSDK_COMPATIBILITY.md",
          "sha256": "511cdbfb70deab489157564a15f6d03fb91e87a74ff5613a5f4e0e47bc462fc0",
          "bytes": 1533,
          "artifact_id": "artifact-fa8a8ee0586147c9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/.gitignore",
          "sha256": "e2eb93a61ffd7877ea5c751abcb3a618e8e2e9a2073a27f66d4114fe10819f86",
          "bytes": 9,
          "artifact_id": "artifact-27057166be8d47b0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/CHANGELOG.md",
          "sha256": "a5c6c66f419a06a747fd5e2e9cfbf421677e5951c891d5d985a052a4d0110b2b",
          "bytes": 34226,
          "artifact_id": "artifact-e2930154ecba4a8d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/CHANGELOG.md.meta",
          "sha256": "8d62ab1e0ae4800e7d66e68cc6cc00a99a82f0f532024b020385c3b3013a8517",
          "bytes": 158,
          "artifact_id": "artifact-703756d5141443f3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor.meta",
          "sha256": "3cd97ae33cf3da367955dea72e53a50f2064542e0a079702acbee3c3d3be5cbe",
          "bytes": 172,
          "artifact_id": "artifact-0154eaa109f2410d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli.meta",
          "sha256": "2994f978b60efa0fa3305d3490d4b46a561358283c5293a9a70e815bad3460c4",
          "bytes": 172,
          "artifact_id": "artifact-b141a4760efa4617",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/LICENSE.txt",
          "sha256": "3d180008e36922a4e8daec11c34c7af264fed5962d07924aea928c38e8663c94",
          "bytes": 1084,
          "artifact_id": "artifact-ed5a326fe99240cd",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/LICENSE.txt.meta",
          "sha256": "81726dfeaadcfef5425947a3db3bd3e4ddb9c1d25ed2705b4cf021f7b9e8774c",
          "bytes": 158,
          "artifact_id": "artifact-aba67c8d88784625",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/commits.txt",
          "sha256": "4e4808fe2ffdddd0218034374ee03cc4e940bb64801251baf2402d4787a0520a",
          "bytes": 86,
          "artifact_id": "artifact-7a8730ae0c1446e3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/commits.txt.meta",
          "sha256": "4c90ad352a3495966d43ca820eb7710fe648e4806778af8051b9e40ce3260ca3",
          "bytes": 158,
          "artifact_id": "artifact-1271580a0dbe4346",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64.meta",
          "sha256": "5433569f97fa32945f6c1a7a901932778de4da53e9ee8cc0778941fe80bdde26",
          "bytes": 172,
          "artifact_id": "artifact-c0b98eab7dc742b0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/brotli",
          "sha256": "cef2a41c929821b7abdc2276ffb735f9f7b4b7d913331a215fb45f07ba95ae5d",
          "bytes": 852560,
          "artifact_id": "artifact-25dcba7f874043f0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/brotli.meta",
          "sha256": "f128730561ac30af0b68550dd2e00df1af73e0fcee3fe400520c47d1b00101ad",
          "bytes": 155,
          "artifact_id": "artifact-986e5528c1824909",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/libbrotlicommon.so",
          "sha256": "c3e33564197cfad710d7fc11d5c1c85a2b73aac9198ae7208dcd523f3a0c437b",
          "bytes": 143728,
          "artifact_id": "artifact-6a6aa85a33eb47fd",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/libbrotlicommon.so.meta",
          "sha256": "0a260e4f73b41b185dc3ec3f7992985576c4b04c81eebf557ac1cb78b85e5e08",
          "bytes": 526,
          "artifact_id": "artifact-936dd045d502498d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/libbrotlienc.so",
          "sha256": "479035010432447c3e98cff954af4ba86c026fe5b0d6776d5917ac248324357f",
          "bytes": 719728,
          "artifact_id": "artifact-c39481bc3f9b47a8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/libbrotlienc.so.meta",
          "sha256": "6c69fb062db76c0ed4affa7ba21fac97683b94496687a0c2385d22771ee3f830",
          "bytes": 526,
          "artifact_id": "artifact-1e3bbc26b8354507",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos.meta",
          "sha256": "4e6a975540a9bb0f5a7a69a16d3ec7753383bd94bea94de16858907fe05b4a6d",
          "bytes": 172,
          "artifact_id": "artifact-39b896528b3e43d2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/brotli",
          "sha256": "6acd96976472377af3f71786be2bec7c26e733d186ff5a1bbe45c69d9358655d",
          "bytes": 1661624,
          "artifact_id": "artifact-02bc15f4acce49e2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/brotli.meta",
          "sha256": "93bb5a08fcf02a6988567f879853d185d240630e40538dd9a1a15f121280c87f",
          "bytes": 155,
          "artifact_id": "artifact-8acf52bf144a4ebe",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/libbrotlicommon.dylib",
          "sha256": "3e2a49ddc705bfaa8ad0879b60bf4ea3c06b63cd84f60dc655746c6172bcd1d0",
          "bytes": 297472,
          "artifact_id": "artifact-d9846e08af9c46e2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/libbrotlicommon.dylib.meta",
          "sha256": "4429d9d9db64d566d6cf60168d70724d837d225827a9c7ecd5b81df08f1733c5",
          "bytes": 526,
          "artifact_id": "artifact-30880e44a72242ea",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/libbrotlienc.dylib",
          "sha256": "e610167aca7cffe78baad1f166c6bc4764403fbcf6f1d9be2d29b0eea530f311",
          "bytes": 1507280,
          "artifact_id": "artifact-1383202e314f4fff",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/libbrotlienc.dylib.meta",
          "sha256": "1f6bb870a160006c2638f258e575543e8b42e8589019cc6ccce5cb062cdee034",
          "bytes": 526,
          "artifact_id": "artifact-c9c03615e98f4ab7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64.meta",
          "sha256": "c283bbd19e4e90b24548ea56940bd658c99c44e32cd5988170d602ddaf48af4b",
          "bytes": 172,
          "artifact_id": "artifact-0b2c410068cf432d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/brotli.exe",
          "sha256": "26501f636eb41473feb38d05251c71a6e7c4dec6006c459e7c5acf8442cd4394",
          "bytes": 749056,
          "artifact_id": "artifact-55df4e1319314757",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/brotli.exe.meta",
          "sha256": "a77299dc41dd1a27daf89361720114912b82ab9663165214d9e0db4a5ed29cb8",
          "bytes": 155,
          "artifact_id": "artifact-e94febd141ce472d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/libbrotlicommon.dll",
          "sha256": "16a4e31f18cf9a8e0bd2ff5093d1ab9dd73f1d28577305ef936813a3f6fddab1",
          "bytes": 378616,
          "artifact_id": "artifact-ee4a56e21fec4268",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/libbrotlicommon.dll.meta",
          "sha256": "0126fa8eb85e21cc955df03ec8157d885622e4014ade9d7b052fc24f93136421",
          "bytes": 526,
          "artifact_id": "artifact-81ffab9cad4c41f8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/libbrotlienc.dll",
          "sha256": "af79a66fc39ed13ca469a0a51868237d267f40fb42512783c7a1e3b909b6ce35",
          "bytes": 958662,
          "artifact_id": "artifact-40a92da519194b74",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/libbrotlienc.dll.meta",
          "sha256": "fbc144014d796a11f8a1816ac3f0e4deedfaf65cb45c6ed5e23347f81c281bae",
          "bytes": 526,
          "artifact_id": "artifact-3e6f01e802c84a26",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140.dll",
          "sha256": "2cc02c5e6654aa9175d5963f811cac222f4a2604dc28553139c675b1a78995a7",
          "bytes": 590112,
          "artifact_id": "artifact-d7fd59be8d8442af",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140.dll.meta",
          "sha256": "572a1935751ecd0aee101c6f6f3e4c681b3c73720e8934212ac608bc1ef9db79",
          "bytes": 526,
          "artifact_id": "artifact-ca15633d96174d70",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140_1.dll",
          "sha256": "d8a6c4f6a8da4dfc33cd956d59554cda144d0111d0e750ace8a77555e93365bd",
          "bytes": 31728,
          "artifact_id": "artifact-75d89258825b4a45",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140_1.dll.meta",
          "sha256": "075d985bd1beb8373039cbe941510da500740db375875382ae31f4d0f16682dc",
          "bytes": 526,
          "artifact_id": "artifact-9ed0439ecbd44649",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140_2.dll",
          "sha256": "9cdbab1a0a1b70228d781c5e19a0d055f42c9914cf8d9e93e7fe1709196e3e80",
          "bytes": 192800,
          "artifact_id": "artifact-f5b79221179c4067",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140_2.dll.meta",
          "sha256": "3cf256d30ad273151f856f16b601c1b2fe1ff83e366bead83c5a6db4775be039",
          "bytes": 526,
          "artifact_id": "artifact-8ed69a69d61a40ee",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/vcruntime140.dll",
          "sha256": "263988a0868053b6b01835cd2959c8f71e3f943610421b269da646f2d9e3b333",
          "bytes": 100880,
          "artifact_id": "artifact-008e10786ec949f8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/vcruntime140.dll.meta",
          "sha256": "0179803739e7353cdc20319f2ab124ae6fce298c698e90c705e89055a2240827",
          "bytes": 526,
          "artifact_id": "artifact-d49d0bdcae554e5f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/vcruntime140_1.dll",
          "sha256": "da5d3440dd53261bffec0f9163a46eb12e46b2a4e1bd72dd1b62c6bca9cca280",
          "bytes": 44320,
          "artifact_id": "artifact-900a36dba0f0440a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/vcruntime140_1.dll.meta",
          "sha256": "dd6589fc7fec873417b95dea7ad86f0a66f8c3b29d88d7c19b3b4fd3f3b7ad44",
          "bytes": 526,
          "artifact_id": "artifact-e2eded3955de4781",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile.meta",
          "sha256": "1065773ccb7fc9951e971570eb013e2d6e539f922dfedcf864f64eea88f47063",
          "bytes": 171,
          "artifact_id": "artifact-b5bfdb0c9aed4877",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinBuildProfileUpdater.cs",
          "sha256": "5be88115f14529006bf6b8caad934a88b521f4ecb04fdd051120a11d3054b247",
          "bytes": 3594,
          "artifact_id": "artifact-cd153878791843df",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinBuildProfileUpdater.cs.meta",
          "sha256": "4b07f71822876693fe443d461a3e0f0af3fe9fe2786b38d4bf424ec56e3e12d5",
          "bytes": 243,
          "artifact_id": "artifact-0709b0a3fea14bde",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinMiniGameSettings.cs",
          "sha256": "d7d9aac1c339b08b987c80baa14fbd2ca1124750d8304901c56806b3c140882a",
          "bytes": 5693,
          "artifact_id": "artifact-ef4b8fcb4c694d16",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinMiniGameSettings.cs.meta",
          "sha256": "7d141c00ed34b3999d63d4ad6cef05a1fe2c0036e3f16e6f1ef5d94019e7d4e0",
          "bytes": 146,
          "artifact_id": "artifact-b406f3c8c9094944",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinMiniGameSettingsEditor.cs",
          "sha256": "60c60fb97c86bbe722558539db788000b66355ff41784349a66f627470d4f44a",
          "bytes": 1455,
          "artifact_id": "artifact-a242c4b7824244d9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinMiniGameSettingsEditor.cs.meta",
          "sha256": "72f1e3e6da0ea0cc3d3dbe24c2990cdfe51e0513475cb9e12f70c1b3c9064f26",
          "bytes": 146,
          "artifact_id": "artifact-6e7f53d3ca5448ad",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinSubplatformInterface.cs",
          "sha256": "0cfb426bf72e5288d52f663347153453c7521ea5196aac4b57e5edb32b4682a1",
          "bytes": 13924,
          "artifact_id": "artifact-cfb2f294022f40f4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinSubplatformInterface.cs.meta",
          "sha256": "b856e96ca11d7cdfa8642a6f2efce90b5859e57392df57f2e4bab5d2b5ca38ef",
          "bytes": 146,
          "artifact_id": "artifact-afe0611ffecb4039",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib.meta",
          "sha256": "8d0a58603c9bd26394a1d3d293cb0659d63fbb24ef875f99b6dda044e5932131",
          "bytes": 171,
          "artifact_id": "artifact-4ae1e6c0d6144911",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib/libwx-metal-cpp.bc",
          "sha256": "1d84425ec57197f0f74464e4a9ebedbd1bfa3e40f1949c675e25b98fe2803b5c",
          "bytes": 2779888,
          "artifact_id": "artifact-1cea2452f6614cc3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib/libwx-metal-cpp.bc.meta",
          "sha256": "7b359ae4f3a91b41d1dd4fef07bf44aebdd1abb1047f74d662a52f8836f56bc2",
          "bytes": 1806,
          "artifact_id": "artifact-de86721d666e4158",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib/mtl_library.jslib",
          "sha256": "983bc417da173678ac912eeea15087f8ffb106dc18b67699de3aab202681ccf5",
          "bytes": 1522,
          "artifact_id": "artifact-8b8f85b9d5aa455e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib/mtl_library.jslib.meta",
          "sha256": "a6a5e05931c731730805bb4ae3e8a3df50b84ac4cd64962bf841643e85d3a830",
          "bytes": 1806,
          "artifact_id": "artifact-710553b8f4e94dd5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker.meta",
          "sha256": "06f792e1abc5d58f9f1861fbb7ffcbfdf56da8c1e5c134de5906c302b3efeed1",
          "bytes": 172,
          "artifact_id": "artifact-368003111d9544ea",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/mac.meta",
          "sha256": "fc6737db0757e6a0960a05dcf3f4a416780426afe289e5d31b1517a37a647155",
          "bytes": 172,
          "artifact_id": "artifact-b0bc0e79f94a4417",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/mac/newstatehooker.dylib",
          "sha256": "6852d71738c72e626bfd838d498aad883680fb571135a5c94ad375924cc5f7eb",
          "bytes": 35309464,
          "artifact_id": "artifact-5a2bff8c8c944c99",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/mac/newstatehooker.dylib.meta",
          "sha256": "b0431c8158822533e540f49cbccf39886f265211e07f3d0d9318304d9602b299",
          "bytes": 1195,
          "artifact_id": "artifact-c97f7ed4b86c49c9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win.meta",
          "sha256": "ce5a804b53581f01a872ae78d3029c212793ee82443a57bb618fa88cfb21e14b",
          "bytes": 172,
          "artifact_id": "artifact-d4fdcff935344c85",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win/newstatehooker.dll",
          "sha256": "958017e52455d00df130910a671a7661ddde63b66a36e40ea1c7e0babd317c50",
          "bytes": 7734272,
          "artifact_id": "artifact-c9f63f1d64104ada",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win/newstatehooker.dll.meta",
          "sha256": "353bd8688c5aee0d54ebb9e56841847d73cdc60d742e90fb503024d766cfaca6",
          "bytes": 1593,
          "artifact_id": "artifact-8f8c0450ccdf41c8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win/ucrtbased.dll",
          "sha256": "bed98a14f107cabd8e5e4ad43aedd0b357656ca1b577167c22d2829134d4e52e",
          "bytes": 2238056,
          "artifact_id": "artifact-62eb387fdc1349c9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win/ucrtbased.dll.meta",
          "sha256": "3a426ffe785ce20a1fac5a68133e457d268741fe364ddf39881eb3ad57143ae9",
          "bytes": 1593,
          "artifact_id": "artifact-54046d1edc9c4377",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/MiniGameConfig.asset",
          "sha256": "be9def00391429090a65b5fe821af958025ba789fdabc6ab1c6f43c5d01a117c",
          "bytes": 2338,
          "artifact_id": "artifact-57a457439d7a4e92",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/MiniGameConfig.asset.meta",
          "sha256": "6a8bc560cb7d2a84c39f00b23ee5c1d773207962a1c1988984698ec4c1b603a4",
          "bytes": 189,
          "artifact_id": "artifact-d83754c0019649cd",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node.meta",
          "sha256": "c2f922e2f4a38898a4ed8340438dbd5d7ee2cea2ded5cdfda0e89d87efa29c17",
          "bytes": 172,
          "artifact_id": "artifact-f0e6ee5ecc504b7f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/dump_wasm_symbol.mjs",
          "sha256": "0d850cfd0ec24c23269d64df10bae33082cf34aa17ab9d31d529050c70dad00d",
          "bytes": 751,
          "artifact_id": "artifact-16b5d3dd7f484116",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/dump_wasm_symbol.mjs.meta",
          "sha256": "607eaa0c6eb05a734730c44b2aa428e6ae3c11739521cc22417acb943e9be429",
          "bytes": 155,
          "artifact_id": "artifact-c8d47000aac34cb3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules.meta",
          "sha256": "9f2d8609f0d6c0617e269956e9329aad145c89cba15210d7d535b481ad809356",
          "bytes": 172,
          "artifact_id": "artifact-dc09568825994148",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen.meta",
          "sha256": "800003b63c20d6b7ffe9bcf0cc8431993f0c0865b4d38f10936cbc579d5bb558",
          "bytes": 172,
          "artifact_id": "artifact-a1f4b86a39db4d71",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/LICENSE",
          "sha256": "c5accbbd8546e94c34aed24afe689a617627d18eed5a6c48277e48db57c23851",
          "bytes": 11356,
          "artifact_id": "artifact-c4c46393e5194eae",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/LICENSE.meta",
          "sha256": "aba1be3cc175dfafdb6a8bb05678b621dd05d9c677fc766b5a905a04d59a608b",
          "bytes": 155,
          "artifact_id": "artifact-86d403ab14004814",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/README.md",
          "sha256": "e79c00d33c7422da9dbdb992dc403f3c72d70a2384a77cf6eff5d15bc633bf03",
          "bytes": 71745,
          "artifact_id": "artifact-7e92c674e74f48ef",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/README.md.meta",
          "sha256": "452579f86ebbefda7f59c815ac09b33d859f8ec0bb1bd76c448417fa54cc8a42",
          "bytes": 158,
          "artifact_id": "artifact-efe9f45e82864a2b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin.meta",
          "sha256": "25520dfdcb7f6c70df364b5d29f5f49b72f5f9e7e826cea64f8e1e990f9dec26",
          "bytes": 172,
          "artifact_id": "artifact-c2844e2b873e4bba",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/package.json",
          "sha256": "8005a3491db7d92f36ac66369861589f9c47123d3a7c71e643fc2c06168cd45a",
          "bytes": 25,
          "artifact_id": "artifact-204fc1a2ce174920",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/package.json.meta",
          "sha256": "178cdb5d56a392d7edc278065f8018ae95a53f72c31f9d55257199502c78975d",
          "bytes": 158,
          "artifact_id": "artifact-2b6d00baaaa1496a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/wasm-opt",
          "sha256": "74d74ff270b483c93b6c768328e1d37593ee604237c05467fb53f2ea5c5c0f8b",
          "bytes": 6080234,
          "artifact_id": "artifact-942b974f61b7417d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/wasm-opt.meta",
          "sha256": "9f3917b215e18bc951227bc2ee26836646a7c33a21daae48e23a2ba23665f5ed",
          "bytes": 155,
          "artifact_id": "artifact-821ce88335634c38",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/wasm2js",
          "sha256": "ee363a2bc1dd668446874c6fd38b723755be9037b2afb8623bdc92d1c1ac06ee",
          "bytes": 5626795,
          "artifact_id": "artifact-2664128040d34b3c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/wasm2js.meta",
          "sha256": "babe30789d4623cd7b88e60c3b17286ddb16d8fd49aade770d7dc85cc61b8936",
          "bytes": 155,
          "artifact_id": "artifact-735b6b8eb6c748c9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/index.d.ts",
          "sha256": "f9e5f46910a04c95f4f511322aa05f676bfe773d40fd48f0479924dd83dbb96b",
          "bytes": 78510,
          "artifact_id": "artifact-67129c63000f4799",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/index.d.ts.meta",
          "sha256": "83d18e9ae6672174bde02b85253637cfcc266a77a8f6de6a84eae63681b8a9b5",
          "bytes": 155,
          "artifact_id": "artifact-1a93801a644b44a4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/index.js",
          "sha256": "48749b8967512675a107971dcd7d0c76a20e9fdb6b00f3bc3067b4947190f0d6",
          "bytes": 6143047,
          "artifact_id": "artifact-d5a141c4ab764e95",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/index.js.meta",
          "sha256": "9d5ac86121388343379c09fd77890402bff1639959efe32248b8d3d09f9c840c",
          "bytes": 158,
          "artifact_id": "artifact-605c53e143b24a9a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/package.json",
          "sha256": "90a5e5740260046247105210d99f6493d9d7c20dee934dd99fd7115daa176ede",
          "bytes": 1173,
          "artifact_id": "artifact-12858800716840b3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/package.json.meta",
          "sha256": "9b9735a3607f75352bca7655f63b356a473da18ea19ac4495088dfaaa4474f6b",
          "bytes": 158,
          "artifact_id": "artifact-ca31f5ca08c1404f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/package.json",
          "sha256": "89291b451fa8ed256f3bc0969b507acdc606b3e41c728abdfcf49c75447cd609",
          "bytes": 158,
          "artifact_id": "artifact-199cf7ed497d41e0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/package.json.meta",
          "sha256": "5cd3c0b724156137c5d90e32a458847f934ec6b2e08ed6fac6adca04eda05f7a",
          "bytes": 158,
          "artifact_id": "artifact-b380fa6da0de4a86",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/PicCompressor.cs",
          "sha256": "851c1eaea2dd7966fb2590c7c41a84a116ed8b6c0a5228833874afc3ff2b0c6e",
          "bytes": 3962,
          "artifact_id": "artifact-7107b16c7ccc49ce",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/PicCompressor.cs.meta",
          "sha256": "5cdd4c7a4ac4eac71a23d3b1a64ff32dcc0a0e645925ab162668383d1a6ced98",
          "bytes": 243,
          "artifact_id": "artifact-b26950fc4b9e49ce",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable.meta",
          "sha256": "dd01f4724e0efeac382050d234d357af58dda3af14a5b56d75c942e5d35122a0",
          "bytes": 172,
          "artifact_id": "artifact-440efbc51bfd44e7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableConvertCore.cs",
          "sha256": "b900cf7c614a97644e4c5b8df61545e8a3b4903a1980ddd63c1ba916769844ca",
          "bytes": 5711,
          "artifact_id": "artifact-676689f865754fce",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableConvertCore.cs.meta",
          "sha256": "dea962e2c73653b610684620fdf05c32073d513d86c5879693b26923ea54423b",
          "bytes": 243,
          "artifact_id": "artifact-028ecae2aa634400",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableEditorSettingHelper.cs",
          "sha256": "16e9a7e6114ca997a9b9707770c2f9c47651ccbe606e2dc940d797abc35d6252",
          "bytes": 14409,
          "artifact_id": "artifact-329bfea6811647ba",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableEditorSettingHelper.cs.meta",
          "sha256": "8b3f1915b33a7d9794ba3f9d35ecaab5e6c030028487b006daa47a073e74e7ef",
          "bytes": 243,
          "artifact_id": "artifact-d46a83d4b7f2456a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableWindow.cs",
          "sha256": "d79724917000adff887b7f047fed7f9ce0a6f84c864cd2a314095ad1040fa69d",
          "bytes": 1286,
          "artifact_id": "artifact-8263c653a1114259",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableWindow.cs.meta",
          "sha256": "249b353d061b3759e9cddab6464ecf5e65058efc5b44c9eec0c984148267189e",
          "bytes": 243,
          "artifact_id": "artifact-24feca781c9d4aa5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor.meta",
          "sha256": "c4ee1f634dcf138d721dd6a9f3f4d78050b0e0b03aeb32f3d7d9232614bfd2f4",
          "bytes": 172,
          "artifact_id": "artifact-0041e504f77340d9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node.meta",
          "sha256": "7559a48383d0a3d165f741666416eff8376ee908e83b5af1bb8d174f3f7e627c",
          "bytes": 172,
          "artifact_id": "artifact-502ea71a7879401c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/PVRTexToolCLI",
          "sha256": "438f57a7e1c5384015b38b4b9dd2eb1f824a8747681b6bb83f4b286e5382eee3",
          "bytes": 5546016,
          "artifact_id": "artifact-2fca5d6327834ee4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/PVRTexToolCLI.exe",
          "sha256": "edfc7866faaa57419223555d25965bf7c9482133a07b8401c138ed2696df94d0",
          "bytes": 4920232,
          "artifact_id": "artifact-585f44f8ffc54996",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/PVRTexToolCLI.exe.meta",
          "sha256": "5404dbdc551fc564d58ee8d61bbc0a1f21c7989547922dac55b19e4e5ffb20ae",
          "bytes": 155,
          "artifact_id": "artifact-3fe48f69e5d04236",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/PVRTexToolCLI.meta",
          "sha256": "ed97b00adacda383b1f78129e2a57dfcc8bb803f617fd5481e1df0efb2360f54",
          "bytes": 155,
          "artifact_id": "artifact-448a81c36ab34d7f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-avx2",
          "sha256": "f98d53f9753c17ea4ba2140832a1a0942cfdd74caf41babe9d950de9c4497b50",
          "bytes": 640544,
          "artifact_id": "artifact-c69756a515754e2a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-avx2.exe",
          "sha256": "18b199c44173b1b00ef44a8aa34f97d8218bd384687522dadfe88850513770ee",
          "bytes": 999784,
          "artifact_id": "artifact-fee2bdb3991f4fde",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-avx2.exe.meta",
          "sha256": "ce5f878521c42fcc0e6cd4b8ed0495333e5da675f2332116eaf50db36ecb376e",
          "bytes": 155,
          "artifact_id": "artifact-c037b311d6f4468b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-avx2.meta",
          "sha256": "78726c1f94efb6f7b90e63da9c0c356a9b3a30a73b02ea63250e0bcbb9b4a3f7",
          "bytes": 155,
          "artifact_id": "artifact-c112b7995f2f48c8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-neon",
          "sha256": "4f29565b8722eac89b698db11773a5c55ead685bea080f6cb30171021cf99b15",
          "bytes": 558784,
          "artifact_id": "artifact-78c9dc8e90af4400",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-neon.meta",
          "sha256": "fe42a98fcb43fc93bcc67e9d49621cdad46a0ad8432a7168bb732526388006cd",
          "bytes": 155,
          "artifact_id": "artifact-1c3810c10fbe42a3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-sse4.1.exe",
          "sha256": "aeeb7e850ecc91a85bbfd9a06fb5e031bc4a2cbe8621a688ce2e02a5e1cf5161",
          "bytes": 813896,
          "artifact_id": "artifact-6da54ecb26f247e6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-sse4.1.exe.meta",
          "sha256": "9742a847ada9f74dfcc718aff8218e544855cb1abdfcd95dfa11bda522fc1225",
          "bytes": 155,
          "artifact_id": "artifact-66fbbb3b01f94a88",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/pngquant",
          "sha256": "eca7aa3b689b50d7577d9bc9853c644e566b5ec2e13fe32a9bdeb2e60de53463",
          "bytes": 837984,
          "artifact_id": "artifact-07f8d928d78c43b2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/pngquant.exe",
          "sha256": "76e383f1b3962cb19d3ea322f632b8778a0770a31e5d09dcb1466ddf19e26f7a",
          "bytes": 768512,
          "artifact_id": "artifact-3ffe24028dfa42e4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/pngquant.exe.meta",
          "sha256": "ebbc4aace90a28ab74bfc274611c1f635b2b77d846ca0e93c55c4ffaa843a1d6",
          "bytes": 155,
          "artifact_id": "artifact-cd300c0ab1224a6f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/pngquant.meta",
          "sha256": "722b51fd1261e27aa326e8c8df2b1a6d25139bdaabda663505dc36bdf91dd8cf",
          "bytes": 155,
          "artifact_id": "artifact-b1b9238153d043f7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release.meta",
          "sha256": "59a35c705d0b4cc3218e473ac3e1dacf82eb938846b179e9b2ca0d444579f1cb",
          "bytes": 172,
          "artifact_id": "artifact-24ee65367ca9483b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetRipper.TextureDecoder.dll",
          "sha256": "a5aa7cbe87ec00b579ba49832e0d75698ed92973754c8f91e619099ca5d1bbd3",
          "bytes": 116224,
          "artifact_id": "artifact-4200781c1ef64e36",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetRipper.TextureDecoder.dll.meta",
          "sha256": "236d9e9ca03bef0a3a30b2490af2c756a709e773c688484de798348ec9c34f0b",
          "bytes": 1668,
          "artifact_id": "artifact-905f7be27bc649b6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetsTools.NET.Texture.dll",
          "sha256": "d2ccd4e16374e72a0f5135d7845252eb39022c839adfc89a00c8adf88c575d15",
          "bytes": 66560,
          "artifact_id": "artifact-5ffd2581743d4780",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetsTools.NET.Texture.dll.meta",
          "sha256": "3a18a98107b310729ef1e3dba10eee4b01dcbfb5f1336eabb5c4d10582bebd11",
          "bytes": 1668,
          "artifact_id": "artifact-3f599c8c9bd84690",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetsTools.NET.dll",
          "sha256": "94ed77a6277d55921618a79cfa54795c235243b69d76be2d81fef1aaa2ec3ef1",
          "bytes": 193024,
          "artifact_id": "artifact-271f81487de44162",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetsTools.NET.dll.meta",
          "sha256": "a5ab2a0d8d9e517da8127d444f404bc8c70e0cb801fc6efa555050ad93c29b62",
          "bytes": 932,
          "artifact_id": "artifact-a6bdea9eded8406a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Mdb.dll",
          "sha256": "f5104aa2f42fd15d36a3c82c0d60b98c4f4e14e2dd0c027bcaf952ba1ecc1c2a",
          "bytes": 43008,
          "artifact_id": "artifact-2d4d1eb37b46487b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Mdb.dll.meta",
          "sha256": "4b83e5dac87f26daeea9d203b6896fb71642593f3057221c70b38afc753f8597",
          "bytes": 932,
          "artifact_id": "artifact-719280dd614c472e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Pdb.dll",
          "sha256": "a4788b2153629c49ab75297fa273ac0ca7ab0b799d36442ada968ed037023c71",
          "bytes": 86528,
          "artifact_id": "artifact-a12656d5169748cb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Pdb.dll.meta",
          "sha256": "712d4f9afc9bb3b1effed1c5e6b72062605c91bcfcb738e6f775c1f874ae0850",
          "bytes": 932,
          "artifact_id": "artifact-dde26dd4636347e9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Rocks.dll",
          "sha256": "31183b063f8043437c07fd3e7168517e0a7ad588b9270190f3cf74556db955ee",
          "bytes": 27648,
          "artifact_id": "artifact-f1a3acd87e5a4e20",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Rocks.dll.meta",
          "sha256": "f45c9bed156c12755c0ed23b5c73128ccf76ce607582175ed26c78c8c8f9cd36",
          "bytes": 932,
          "artifact_id": "artifact-f5108e248cce45f2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.dll",
          "sha256": "2c91802e6e95afc1d113f486df8c9c03792cb4898da219f0eb212bdd04fb3478",
          "bytes": 339456,
          "artifact_id": "artifact-efa8d50e43bc461b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.dll.meta",
          "sha256": "d765ccfa758345c0829f7ea571c4e5fd1afa9f21b05f6489df39c06886a26eae",
          "bytes": 932,
          "artifact_id": "artifact-d61593670eb4472b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/SixLabors.ImageSharp.dll",
          "sha256": "69588c5a315ecb02e27fc841465d62c8b41c91ffb0b40c19582bade8fac61451",
          "bytes": 1722880,
          "artifact_id": "artifact-ed56704df6ad4347",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/SixLabors.ImageSharp.dll.meta",
          "sha256": "8ce2aa3cc7fa07cdea4a78e4357884931f77e5d3f50bc413b15b828d7829ff8f",
          "bytes": 932,
          "artifact_id": "artifact-4b8182832950484a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/SixLabors.ImageSharp.xml",
          "sha256": "f255e023999c3f89c95ed52805f51f5d94a9e01cb4e886abf8819536f67bc305",
          "bytes": 3822252,
          "artifact_id": "artifact-8629c381ac5c4ffb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/SixLabors.ImageSharp.xml.meta",
          "sha256": "c39847eb64dc1dddb58b021b4ff23c1f6caf0c3ca8a6166d38a068dc7a61e5a4",
          "bytes": 158,
          "artifact_id": "artifact-29cae5f99eed4823",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Buffers.dll",
          "sha256": "accccfbe45d9f08ffeed9916e37b33e98c65be012cfff6e7fa7b67210ce1fefb",
          "bytes": 20856,
          "artifact_id": "artifact-4bbb8708dd7a4393",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Buffers.dll.meta",
          "sha256": "d52866eae8be25e99b82481ce34a6028da4a756f0a403c3c1e8e60a6e4a9a204",
          "bytes": 932,
          "artifact_id": "artifact-f99f7f1e6ea745c7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Buffers.xml",
          "sha256": "e6191e340a6b07a65d0e59c672c37bfa26095d7ef5c5a95b6e15a6c89c45f733",
          "bytes": 3444,
          "artifact_id": "artifact-c752e5c022b94619",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Buffers.xml.meta",
          "sha256": "2fb5ca699f6cad72b2c7adaa03a648e1e5c57f672499280d2f9b9f78517f26a1",
          "bytes": 158,
          "artifact_id": "artifact-261e1918f53e4aa2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Half.dll",
          "sha256": "b1fb190a77169e4a561acfc7883adca479ba43d36f95d85ec36db1b0fa11d0c0",
          "bytes": 13312,
          "artifact_id": "artifact-2efdf4d3b8404d59",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Half.dll.meta",
          "sha256": "682100ff915b21863dd66a795f3d57c2a85f16b3b32b4b8ec06cbd86368403b5",
          "bytes": 1668,
          "artifact_id": "artifact-5290832cfae645c8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Memory.dll",
          "sha256": "bf3fb84664f4097f1a8a9bc71a51dcf8cf1a905d4080a4d290da1730866e856f",
          "bytes": 142240,
          "artifact_id": "artifact-b014a387e58e4848",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Memory.dll.meta",
          "sha256": "d0368188134cdb824a9183872f6c114b483c4941eb0c1850a0577ddb2fcaa38b",
          "bytes": 932,
          "artifact_id": "artifact-ab15c9aa94074308",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Memory.xml",
          "sha256": "5d68c92d2372f23da7cb9ecd61ea6da6ebcb3eb5145f97463e64b4845cd8fd0b",
          "bytes": 13596,
          "artifact_id": "artifact-40a7b58891f84c4d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Memory.xml.meta",
          "sha256": "a4d409f314e3ff63ff91b78b663647153c049476f5f361be3a2d13e4e3802415",
          "bytes": 158,
          "artifact_id": "artifact-516a593cb0a04dec",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Numerics.Vectors.dll",
          "sha256": "1d3ef8698281e7cf7371d1554afef5872b39f96c26da772210a33da041ba1183",
          "bytes": 115856,
          "artifact_id": "artifact-9ae806743d0d4228",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Numerics.Vectors.dll.meta",
          "sha256": "53798ee71684af56ed27327f8f9ab0882b9e2027231a97f580765e821f2ee16b",
          "bytes": 932,
          "artifact_id": "artifact-855551a3d43b42ff",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Numerics.Vectors.xml",
          "sha256": "ee6c0cd10f585f83711e58a377284c883b12f2d77fb35c1b0fe6aef2f1259673",
          "bytes": 180864,
          "artifact_id": "artifact-487d2cb468724235",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Numerics.Vectors.xml.meta",
          "sha256": "1b10b27eb7554fc3a19add4c90fee9fd9c6192aabb4ab1425572ace60042b521",
          "bytes": 158,
          "artifact_id": "artifact-15bc2ea7824b418c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Runtime.CompilerServices.Unsafe.dll",
          "sha256": "66409f670315afe8610f17a4d3a1ee52d72b6a46c544cec97544e8385f90ad74",
          "bytes": 16768,
          "artifact_id": "artifact-ae5d8c979ded48d6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Runtime.CompilerServices.Unsafe.dll.meta",
          "sha256": "510818ccfff1f01511fa57d6d4010b151f5e15e9e902b24679b7322cd7427228",
          "bytes": 932,
          "artifact_id": "artifact-61bea9045fd94bba",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Runtime.CompilerServices.Unsafe.xml",
          "sha256": "ac87b3d560d5c1b65d2f3a1c13127d3bf194af3ea6f49cd27e3e75e6ec772d85",
          "bytes": 17741,
          "artifact_id": "artifact-52f673e6db7846d4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Runtime.CompilerServices.Unsafe.xml.meta",
          "sha256": "d88c236fdc1ef62344e3dbae4d7c4c66c34ad6ae2e790b233564c3327aff55ee",
          "bytes": 158,
          "artifact_id": "artifact-9675eb12833140f1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Text.Encoding.CodePages.dll",
          "sha256": "93da6a111239a3804da2efd6a6faa92bf5cbfe3b2b079ad3c04be643179f4088",
          "bytes": 758664,
          "artifact_id": "artifact-889f5c524ccd4914",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Text.Encoding.CodePages.dll.meta",
          "sha256": "258a946162f3648e1e22050ecde176e296d77da89516be06d8e17713beb78559",
          "bytes": 932,
          "artifact_id": "artifact-df569c0c462e4a6d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Text.Encoding.CodePages.xml",
          "sha256": "45c2e87787ea8aca93fe1d670bec6e7c7f7fa1ff2ba8ddbbd61b9d3180081d89",
          "bytes": 2047,
          "artifact_id": "artifact-01e68465d886483c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Text.Encoding.CodePages.xml.meta",
          "sha256": "017745d55bff2b1925d8399996910a7b8738e45519f41843ad32739bdc3f260e",
          "bytes": 158,
          "artifact_id": "artifact-c13b190740014543",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/WXTextureTools.exe",
          "sha256": "b3087b50f2616dada9ecfa3748bcc483a52acc329e363da921fd297e3cf8d2b6",
          "bytes": 137728,
          "artifact_id": "artifact-4e722eaa7e544321",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/WXTextureTools.exe.config",
          "sha256": "e438dc9a13e51181843a433f54e47e9b49e14a80efa5f969397cf54347ff29de",
          "bytes": 540,
          "artifact_id": "artifact-272eb7e157a64cd4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/WXTextureTools.exe.config.meta",
          "sha256": "a67372ea9d814b5131f34cdd3a47609590ea2f065213f853792a0f8133f282be",
          "bytes": 813,
          "artifact_id": "artifact-16ae8ba2a7be40ce",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/WXTextureTools.exe.meta",
          "sha256": "beadcd4edb3a32b5d4e3d4950e507c9aaabeb2e12f191cc8927455edd4d4b40a",
          "bytes": 155,
          "artifact_id": "artifact-cb206842475d4328",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/wxlog.dll",
          "sha256": "7557aeaab63d8e3315c6466afd8dae98d7473cb079a69b3e2acda8c65937e469",
          "bytes": 12288,
          "artifact_id": "artifact-bc325887f6d74261",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/wxlog.dll.meta",
          "sha256": "ea83e3ebd2b7519d9f3904e0031defafdc4eadd0f88f1ec360948e55dcfdcf73",
          "bytes": 932,
          "artifact_id": "artifact-81e8746c3c8b4753",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/classdata.tpk",
          "sha256": "4d7611e9bf424764824861e678616c234fd06b4ec49a9215f4decfedb0e49398",
          "bytes": 1134474,
          "artifact_id": "artifact-a0fe3f0523704623",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/classdata.tpk.meta",
          "sha256": "0a5b0ba313ebdf3860ef921c8876837e6030bb3bc758a791ba30736f66cba635",
          "bytes": 155,
          "artifact_id": "artifact-8623b484e4ac4886",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/slim.conf",
          "sha256": "0a282ee44ffe3c60595502ca4be5b77b6fdeab2ac6c5b5a97cc00382d9fe7f89",
          "bytes": 4976,
          "artifact_id": "artifact-2063f33513c34b27",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/slim.conf.meta",
          "sha256": "73798739480aabf63a9dbb9029ca302d3cf6fa6ebdf95a46cb4d0ed945ea4e52",
          "bytes": 155,
          "artifact_id": "artifact-88d779e0a13b4f70",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXAssetPostprocessor.cs",
          "sha256": "727b5dd3c9f51840d9aeac7ca2cccf1223eef00c8577cb272795a96de0470244",
          "bytes": 5521,
          "artifact_id": "artifact-564d355fa5174d2f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXAssetPostprocessor.cs.meta",
          "sha256": "9fcc1bb398b282908e62926ef0f48da437eb70b33866b2817fa50bef343d921f",
          "bytes": 146,
          "artifact_id": "artifact-81abdb3d8ac648ea",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXConvertCore.cs",
          "sha256": "b096efa0ed37dbf2ef94d06242991a3e74d350308043f0902eb98851bbd39ef7",
          "bytes": 107026,
          "artifact_id": "artifact-6054b0cf5fe24b17",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXConvertCore.cs.meta",
          "sha256": "7739d6a7f11c297b93900502a5217036bda1f046a8d625c0f24b90f0466cd301",
          "bytes": 243,
          "artifact_id": "artifact-38505b46ef1141f3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXEditorSettingHelper.cs",
          "sha256": "e2dd228c46ecf62299cf49490cd52fdc940b96fd1f2d87a39617bcc28e0314d9",
          "bytes": 70652,
          "artifact_id": "artifact-4c1f7dcbe1fe49f3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXEditorSettingHelper.cs.meta",
          "sha256": "67232ee762fa708edd6940ad3506f3917f75fc3c56cfa6bea3b1f6ed4561f63e",
          "bytes": 243,
          "artifact_id": "artifact-41edc41b52b54e80",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXEditorWindow.cs",
          "sha256": "900590d5c421d0e658d18a1a1b8a345006d7196cba265cd94395a6a1a8ef12fe",
          "bytes": 1222,
          "artifact_id": "artifact-ea11d97166794339",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXEditorWindow.cs.meta",
          "sha256": "971bbe65af3732b524ca2712dc82508f65e841a9f9991273ed19c1233ac4e631",
          "bytes": 243,
          "artifact_id": "artifact-0877223c46f64187",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXExtDef.cs",
          "sha256": "68e27312cf0a519ddaa86dac8702f97feb7c4d0eb00db3c9209e81d41daca6ed",
          "bytes": 5364,
          "artifact_id": "artifact-fbb0ae947b2545a1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXExtDef.cs.meta",
          "sha256": "2b9372bad36e65f18b860667aac6774e2e09dacf6602fc719b1616f177401240",
          "bytes": 243,
          "artifact_id": "artifact-2ffd425c064142a6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXMultiPackageMergeWindow.cs",
          "sha256": "58c07d1a8ab8b7a893147be9731b6e40a0730cc78ee67815d714cf0aa980632d",
          "bytes": 13413,
          "artifact_id": "artifact-893c5ab3820b4bfa",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXMultiPackageMergeWindow.cs.meta",
          "sha256": "d2362f6ae909156d73dfd5532a94031536c586ecbd86895544cd5b8ca51c9318",
          "bytes": 243,
          "artifact_id": "artifact-a5393964eff648c5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXPluginVersion.cs",
          "sha256": "8e60192be5e56e49efff9543b76208652e4f3d4ac1cdce718b4f4a7d75546302",
          "bytes": 295,
          "artifact_id": "artifact-d440705401654678",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXPluginVersion.cs.meta",
          "sha256": "617bf7ed4337087ba9c002ef1e00beb7d2fd37545b74d51bb7e17a802a0c7de8",
          "bytes": 243,
          "artifact_id": "artifact-006eb2642bc2469a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WxWasmSDKEditor.asmdef",
          "sha256": "1d3f4e9b0ab4646df0c0448935ed4302152f26b3a32c55bdb2a683e2108eff9d",
          "bytes": 529,
          "artifact_id": "artifact-33a489617d504652",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WxWasmSDKEditor.asmdef.meta",
          "sha256": "3feedcb2bce06dee95eea1e7c15e97203041b8d5685627c7948b7ac27ac1dfa4",
          "bytes": 166,
          "artifact_id": "artifact-dd7b87237d1c4181",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/convert.exe",
          "sha256": "ae0eb6b1c5a20c3482e60b49a3e4468ad6f8e9dc71f7efb3f5ed5a11f6aa9769",
          "bytes": 12839936,
          "artifact_id": "artifact-74b86741eacf431b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/convert.exe.meta",
          "sha256": "d5779b05a68f7c01ef2a8b80d355e60c8e6d8e21d93a8d8aea1c6c82ceab723f",
          "bytes": 155,
          "artifact_id": "artifact-f1894773578a4909",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/wx-editor.dll",
          "sha256": "a7569ee4dd0526b5a9841092d4bdb73fad8117f1cef3ae300b20c328bd24e56a",
          "bytes": 282112,
          "artifact_id": "artifact-531d7fab0218478f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/wx-editor.dll.meta",
          "sha256": "3fcd6fca2c8c6ef55732e1d5f7f40f6a6b41981427f982b78624fab9eadf789f",
          "bytes": 645,
          "artifact_id": "artifact-383c40ae3f20408a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/wx-editor.xml",
          "sha256": "bcd92b6606c7dc1e1eed112f738b7c099c6b07ef68baaf0619c389909cff8259",
          "bytes": 45787,
          "artifact_id": "artifact-1e6c6a0423da4c70",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Editor/wx-editor.xml.meta",
          "sha256": "19db4632b0d978738e9794846c5ae415f5a55c3f4232fb19ac8de1db9d644589",
          "bytes": 146,
          "artifact_id": "artifact-1ac63b709a814c47",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/LICENSE",
          "sha256": "9223170188b2dfafcafc2b89d96da21ffa8396fd97485671fc579756999aeb21",
          "bytes": 1074,
          "artifact_id": "artifact-ee1cc1a01d6647cc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/LICENSE.meta",
          "sha256": "c05ca1d54e899ac55651ca17c9cc86e5962d02fb9252726eecfadad6554fb009",
          "bytes": 155,
          "artifact_id": "artifact-8cef04e6c0304268",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/README.md",
          "sha256": "74dbf8cfb3c1c50559d00637960c25ec53cbb2018b179b1b67c72ac5e56c3b3e",
          "bytes": 873,
          "artifact_id": "artifact-98d9b6f3ae2d42ee",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/README.md.meta",
          "sha256": "b777b92e82cc921c563aab89cf1c1ab76d3ca72347e44abcf246513c0dd279d8",
          "bytes": 158,
          "artifact_id": "artifact-94caddb9cb2742e6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime.meta",
          "sha256": "cce01f53953522b7800f9beac790ebf8309413985671ef07f6ff8dfc4ab20eb7",
          "bytes": 172,
          "artifact_id": "artifact-10fd0ac03d1f4a84",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/DisableKeyboardInput.cs",
          "sha256": "62c5f624ea24c1d7a9d4297bd2fda820240c0961d52d7fdfc9eb2bf764413ca2",
          "bytes": 601,
          "artifact_id": "artifact-e97a17abb0ac464a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/DisableKeyboardInput.cs.meta",
          "sha256": "e7d944dc27dee231654396f5313f793c23155028a28907047e32609595a78b56",
          "bytes": 146,
          "artifact_id": "artifact-4f3b5d5627ec4e7c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/HideLoadingPage.cs",
          "sha256": "78ffc1485b6e992299c331bfa5b4f5b4538e6a0f2e053c17dff06e7479b9d2d5",
          "bytes": 764,
          "artifact_id": "artifact-f7ef98b3a0ab48ee",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/HideLoadingPage.cs.meta",
          "sha256": "41f8b0c804f0cb31c484775a678bb796704bb53c1563163b7117019d160e8d0d",
          "bytes": 243,
          "artifact_id": "artifact-9400f72a833d4a6c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins.meta",
          "sha256": "5a332633226339fe8e8f9a162df1a9141cd5efe0c3c3668d38350175fc2e970b",
          "bytes": 172,
          "artifact_id": "artifact-6950155e778f4578",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/AES.jslib",
          "sha256": "c2bfa77b6efedc5a788ec803f76afce4206a2c78c3004b01b1125eb2c6a277ca",
          "bytes": 3055,
          "artifact_id": "artifact-6b2f9f6d4ae94753",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/AES.jslib.meta",
          "sha256": "c52aa8372ed3b535c9b6ee757b12d7c68abd893f26483a37bd71f1389d34a85c",
          "bytes": 1029,
          "artifact_id": "artifact-a1d85e314e8348f6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Gyroscope.jslib",
          "sha256": "bd4296b8c4c4c8a2ade3a8439ce6c2766c33bd7ecb0dec854d356b58fa727347",
          "bytes": 1015,
          "artifact_id": "artifact-8118bcda7de543c5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Gyroscope.jslib.meta",
          "sha256": "41399f074ac5a6ffdda9b3da0e59925680f86d85ba201eed1ee5a452e9f0764d",
          "bytes": 1456,
          "artifact_id": "artifact-d0dc2d1d9556493b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LitJson.dll",
          "sha256": "c1d86a5fc075734207fc08c6ef8a7db2a8f8f3c3c82c1d15f1972a0cab2a0c04",
          "bytes": 60416,
          "artifact_id": "artifact-df5ccae06f2a4b57",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LitJson.dll.meta",
          "sha256": "35fdaa9834e1f562d5e7ee80473ae11c3b9e14aa95fc05abd3fa28a40173fc7f",
          "bytes": 645,
          "artifact_id": "artifact-d6aea6e0ccd34faf",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor.meta",
          "sha256": "2d2be814b26658798bbcb5d2ef801f1dbea88864e017c5f1a800f59378e66b62",
          "bytes": 172,
          "artifact_id": "artifact-d2c16ba82f744041",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_501.c",
          "sha256": "2755cabd191bde734a9e1a13b48542f820564077fe08331e8456a6850eb631e2",
          "bytes": 3010,
          "artifact_id": "artifact-ce15b8253c9046ec",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_501.c.meta",
          "sha256": "d3f1d42b5bae5b0a0320389ebec142a468f84fa2e97c0cb2166d2d4bf40f3e89",
          "bytes": 1363,
          "artifact_id": "artifact-252542c9fedd4570",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_503.c",
          "sha256": "bb8d12e4bf0d6cd81f057f013300118721bcc8e83d17793155800997d90d56ce",
          "bytes": 3596,
          "artifact_id": "artifact-048d4bb51e834cda",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_503.c.meta",
          "sha256": "1e1deddcfb7089b77bf48243655b2be9860729aae3025f17c17fa06745676b7c",
          "bytes": 1363,
          "artifact_id": "artifact-0ea4d6dfd6294cab",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_comm.c",
          "sha256": "7413519d7494c5ef23608aff43711051373efc7b1acc3c5159ddbaedd47108ba",
          "bytes": 1094,
          "artifact_id": "artifact-11755a36ee3746b0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_comm.c.meta",
          "sha256": "a08136064708e0c3e47659689fa949202c4fd04447b7f7cf3bb63fb4ed965d4b",
          "bytes": 1363,
          "artifact_id": "artifact-e99f099df7214812",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_import.h",
          "sha256": "ef1f90889f85eb4d759ddabed7fd31b6c8a47df4eb61936019d2d06cfa0b85dd",
          "bytes": 822,
          "artifact_id": "artifact-5d85c0ba14a2494e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_import.h.meta",
          "sha256": "1530168e5454f377c92c2bb3a7c1ab7b635c1311a35e8df74180e0dac4d9a6c8",
          "bytes": 1363,
          "artifact_id": "artifact-52240bc526f14853",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/MD5.jslib",
          "sha256": "0c3432a05ef50a2137927deab8eec3b5857d72991b09a54bef16562a5dc8411f",
          "bytes": 320,
          "artifact_id": "artifact-d8314b876a0d481b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/MD5.jslib.meta",
          "sha256": "87a25c61455599175c91c059425524b851a4c51cee4c82fea48214f4dc5399c4",
          "bytes": 1029,
          "artifact_id": "artifact-52598ea8eb7e48a5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-Call-JS-Old.jslib",
          "sha256": "0bf5cb0e3204a613bf9cdcb741f8b7cb4f588a05a883fb786d4abd1728e60d6a",
          "bytes": 52626,
          "artifact_id": "artifact-c2822167b1fb4687",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-Call-JS-Old.jslib.meta",
          "sha256": "4d8ddb33ff8cf4ebd30743bdb695b1bacb91f61bf2ebfaf3bb2daa833f674d18",
          "bytes": 1375,
          "artifact_id": "artifact-da65d10cdb734959",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-Call-JS.jslib",
          "sha256": "14da4f1bf1d0a98ce3f6ad379e919c25fde0ee7197d9fec4e10ccf2fc4afb7df",
          "bytes": 10233,
          "artifact_id": "artifact-24768ce3eeb54070",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-Call-JS.jslib.meta",
          "sha256": "42b148bf592c75ab1eb594cec5a1b4fd5db630d9357ce460b42c3ec43f9b32ef",
          "bytes": 1375,
          "artifact_id": "artifact-c6c2b3929a2141f7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL1.jslib",
          "sha256": "50872d7e076fdeb72227180c2c6cb172cf35f0693f3eb7f8ae664549dd7f598d",
          "bytes": 14567,
          "artifact_id": "artifact-2dbef6e351e949ae",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL1.jslib.meta",
          "sha256": "fc8191193178d71dda4f346ff7c15ba3faf8dd0b115d12d58f2f794ac04c602d",
          "bytes": 700,
          "artifact_id": "artifact-61d245524f1c46a1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL2-Linear.jslib",
          "sha256": "8528581a78713521112885ab7224c4ba7aab98ee72d696c2b81ef7b2ff74225c",
          "bytes": 14756,
          "artifact_id": "artifact-b3a37afed8694c72",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL2-Linear.jslib.meta",
          "sha256": "03aa502c4d2361f1a7f849164693dd73868cc67bfdb62b15cf378f028acc9fb5",
          "bytes": 604,
          "artifact_id": "artifact-4eb561dc0feb4094",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL2.jslib",
          "sha256": "bc6a0c6c6451ac5fb5bb20484d42d418f88d27a6525833ea8ab55a68c89f4d4b",
          "bytes": 14567,
          "artifact_id": "artifact-f64edc1aa6184f6c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL2.jslib.meta",
          "sha256": "b33f2b1051167dd32e1acb446747d5e034475489017f1b2c19a0464d5090f0a9",
          "bytes": 604,
          "artifact_id": "artifact-d8f23f7ccd014c6b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/TCPSocket.jslib",
          "sha256": "3c33d91b8fa4c1b96f2dd7c9656028074ec30eb8f0b66ea6721b96ac301efa04",
          "bytes": 2637,
          "artifact_id": "artifact-8eac92a55bdf4ac5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/TCPSocket.jslib.meta",
          "sha256": "747e4b3fde19be5f6a701fe8a1d23f736af5b7c3cb1a1f0564599bf40dba46d3",
          "bytes": 1456,
          "artifact_id": "artifact-5fd6782222dc4126",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Touch.jslib",
          "sha256": "56b6dca0ac799535e486d51a23b15425ccc2d44b601a1727829e9e749fc30e6a",
          "bytes": 1206,
          "artifact_id": "artifact-b79528de147c4a77",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Touch.jslib.meta",
          "sha256": "dc7d270609af4b63e17cd5765c6f15c3cdfc0e2344b7f9d0f84eb32ee1316ddb",
          "bytes": 1456,
          "artifact_id": "artifact-0abb68634b534a5e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/UDPSocket.jslib",
          "sha256": "c21cb749084ad1adb91feb3d5740ef14c160abeb775544aa2c73904e651696d0",
          "bytes": 3024,
          "artifact_id": "artifact-9105cc4908cd4e94",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/UDPSocket.jslib.meta",
          "sha256": "ce7d96a71e917bdb11eeeac6b3677296ec197c594e191153ee181e2a539faf81",
          "bytes": 1375,
          "artifact_id": "artifact-de764966dc554e99",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Unity.FontABTool.dll",
          "sha256": "88c7409cefe05324bd126c1c164884c9b510e926c156b938e1fcb5c8956d0d96",
          "bytes": 20992,
          "artifact_id": "artifact-13580b24d17c4233",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Unity.FontABTool.dll.meta",
          "sha256": "5ab0e3a12b7821314e8a30033fc0bd088637988d3128c10e7473ea2f8e9d0517",
          "bytes": 645,
          "artifact_id": "artifact-7a9d4580a94941b9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WXAssetBundle.jslib",
          "sha256": "2f60c79e9271d8ac3abe9c6bea016e38e2f09d7b54b5bbbfe2bdef461ae30b89",
          "bytes": 15746,
          "artifact_id": "artifact-3cdaf9549e0b4097",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WXAssetBundle.jslib.meta",
          "sha256": "06c300062c1fad0390ed3fe9d3e4de3fe60ab5ae11ed4e979d08817a375f40d4",
          "bytes": 1456,
          "artifact_id": "artifact-467ddb85854746cc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WXTouch.jslib",
          "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
          "bytes": 0,
          "artifact_id": "artifact-0a04bc13d1664dae",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WXTouch.jslib.meta",
          "sha256": "7172fc83eb341634269ba258a937687b02449607522cd4c377ee0a94a83d0868",
          "bytes": 1400,
          "artifact_id": "artifact-ab97e617e70142f7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WxGameDataMonitor.jslib",
          "sha256": "5e46ea8a1b4af028abbbddff9abd15292d1ffb02ed981ceac60103eb3e5722c3",
          "bytes": 2941,
          "artifact_id": "artifact-750a3fef1a244ea0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WxGameDataMonitor.jslib.meta",
          "sha256": "a5750ea75db022dc49fec67ea229ef29d35925d34624d87436a35f0f7ccc385b",
          "bytes": 1456,
          "artifact_id": "artifact-c39a7cb3e4b14bf5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WxPerfJsBridge.jslib",
          "sha256": "7b2fb61fde0a8e3ae58600090f83cad2cc8daa07fcc2b599604b7478cb0494d7",
          "bytes": 6223,
          "artifact_id": "artifact-5aa7af32aa064a8b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WxPerfJsBridge.jslib.meta",
          "sha256": "596add03ac1430c0b66205617dce38e674e6e26836e16fb7f28435602821d77f",
          "bytes": 1456,
          "artifact_id": "artifact-26e5fa6c78e249df",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/crypto.jspre",
          "sha256": "737ae84200b8f41f0d8c4015f7275ec1649c73c7fec550423f7c7039476f6e12",
          "bytes": 88752,
          "artifact_id": "artifact-e8f141b7309546f3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/crypto.jspre.meta",
          "sha256": "eb7862e432fc8b42bdfc697362845e570d5bef4cdc4b27b71a0c2d5ac10ce32f",
          "bytes": 1029,
          "artifact_id": "artifact-ed44b430a16a4c7d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/dumper.jslib",
          "sha256": "1946d42325f777e06e4909ed983a510b1ae8a360fa5af837645e541f41ccdb80",
          "bytes": 1300,
          "artifact_id": "artifact-9103b9436d4b4181",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/dumper.jslib.meta",
          "sha256": "c8bdc6be790181e390901a6106512b0ab8e529911d83822ad14b9be25acc98b7",
          "bytes": 1375,
          "artifact_id": "artifact-ac124818b45844d4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx-mt-wasmex.a",
          "sha256": "4a93905a997312d991eb9a271583218a64c5312b14999c8387d14a545977ec4a",
          "bytes": 4334640,
          "artifact_id": "artifact-a6454ed446a64f88",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx-mt-wasmex.a.meta",
          "sha256": "33896b6038079e610344a68adcf54ce56371721f3be81f6d132a25eaeb0d5246",
          "bytes": 1801,
          "artifact_id": "artifact-82440ac8a5a6471d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx-mt.a",
          "sha256": "3570aca33bd7d253fb00cd44109328699e48c79b05c8f42db7989160b1e06753",
          "bytes": 4305308,
          "artifact_id": "artifact-0c77b32f9d7948e1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx-mt.a.meta",
          "sha256": "b59ff0b3d95f3dfe77ca9937f777315116a97a655f7573401023a059f955c98a",
          "bytes": 1801,
          "artifact_id": "artifact-c77ed1f051e64bfd",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx.a",
          "sha256": "c9bdf98461da257f1aa8d13dd6c7f2233890519aba6702acdd458b946143ff27",
          "bytes": 4313240,
          "artifact_id": "artifact-fefc49ebf7ec4922",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx.a.meta",
          "sha256": "4efd49e21024b884b2c264f78f4535deaf1627b7b524ea7b3af5bb1a6e9dba3f",
          "bytes": 1380,
          "artifact_id": "artifact-76150d0e85714200",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx_2021.a",
          "sha256": "65ed8fd8e643bf441197e62126770ac6f57af5284fdd443f709cdb8091678f9c",
          "bytes": 4369042,
          "artifact_id": "artifact-6f470bfd9c2f45b1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx_2021.a.meta",
          "sha256": "e12219c82fe3d338bdd6144d8e8d599fb42300bc237239bb11ee73f029eff526",
          "bytes": 1801,
          "artifact_id": "artifact-8e1282be590241c1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/link.xml",
          "sha256": "acee5d8011af33a39bc32a1a810c1ece66ab47bdffcc94b21b0a4ab5ca81483c",
          "bytes": 160,
          "artifact_id": "artifact-edc3a59e895e4391",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/link.xml.meta",
          "sha256": "ddbb198fbde41fbfe20bc7938a844e620760bcd8f7fa6a177b9be6aff9644d22",
          "bytes": 158,
          "artifact_id": "artifact-a2e22fbac4fb4c1e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-perf.dll",
          "sha256": "e555891264c35b19637a218582460e2f44e8788746666e9ef389509e6f695f49",
          "bytes": 55808,
          "artifact_id": "artifact-7c88eeb09e9a41a3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-perf.dll.meta",
          "sha256": "143e1d2ddee2cdafdef6c01a59264100c1190b3fa16a17259ecc8118466bb781",
          "bytes": 1482,
          "artifact_id": "artifact-4c4a7af16e5841a3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-perf.xml",
          "sha256": "ed1353b6f108983c8ff72c45a5d155eae2a3870e3d9116cd2cf62588d35b0063",
          "bytes": 1941,
          "artifact_id": "artifact-e1a21395ae594e1d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-perf.xml.meta",
          "sha256": "951c5bcc3328103f7061b8473af05abb32884dde0b9b527ed292a082b569b692",
          "bytes": 158,
          "artifact_id": "artifact-d7bec6fae8164cbc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime-editor.dll",
          "sha256": "f65a17ff5138a028a2de8a6033e6224e904c4ce13307c65f3d8682b997c7d5cd",
          "bytes": 275968,
          "artifact_id": "artifact-a664366bfe7c49ef",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime-editor.dll.meta",
          "sha256": "119a9e4ebd0771d11f5e81413a7cd2d4ce3966f64a9783e087b17be136c4934e",
          "bytes": 1775,
          "artifact_id": "artifact-7fa01bb69ad34635",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime-editor.xml",
          "sha256": "af19e7fbb91ac07af3cee6481063803af6d4895875e0b7e28e6c1046a3920d08",
          "bytes": 427720,
          "artifact_id": "artifact-41249f6fba67429e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime-editor.xml.meta",
          "sha256": "c4769557f17901a733b4cde647844a10f8590cbb592ced1e68562200968c5483",
          "bytes": 146,
          "artifact_id": "artifact-6d2486c4a9ba4233",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime.dll",
          "sha256": "3fcea021200e27a3fe392833e544d96a20434292a5ecf7263c59e79a6d00d4c5",
          "bytes": 250880,
          "artifact_id": "artifact-28928fbc0b8a401c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime.dll.meta",
          "sha256": "4c9e10dad4b91d1a189eb07c5edec135d122d6779c50c1e6f605c6c61fed42c0",
          "bytes": 1983,
          "artifact_id": "artifact-655b8500d4f44a85",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime.xml",
          "sha256": "17765fe4dad06169883cb034bb9448877101378f53ac0c29bacc9ce6b1342849",
          "bytes": 427965,
          "artifact_id": "artifact-db72d0fe27174656",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime.xml.meta",
          "sha256": "68d93fc8ab5b31dd6631a3d1d17e1e493ffa224011a364ee4238315d1ef149be",
          "bytes": 146,
          "artifact_id": "artifact-c309fc203db9424b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx_perf_2021.a",
          "sha256": "e2ee123bd1e1514fb03e1eaa73e5ea5301b8f312662f631bdeccc430bf86a80b",
          "bytes": 2756530,
          "artifact_id": "artifact-cf8cde1b105348ab",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx_perf_2021.a.meta",
          "sha256": "67ae92e387e933dbea29c6f940578e7b768504c06934f9cfa1f162926ab1f4bf",
          "bytes": 1807,
          "artifact_id": "artifact-2d93581ffd104012",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx_perf_2022.a",
          "sha256": "a57690e4f4c897de51fdf7152293800320617ec0a4272e9380806f29786ce753",
          "bytes": 2649934,
          "artifact_id": "artifact-f83f6c02c5a8476a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx_perf_2022.a.meta",
          "sha256": "d2fdb1716d4c3c774124c1fc259717b84f1dfec3c0301ad9fe0b8e3c6a30ee32",
          "bytes": 1363,
          "artifact_id": "artifact-2770004aec8b4eee",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WX.cs",
          "sha256": "7780554641e592282908dc5d15ac8e75b5cabf2f262d4e8062c71d66435c4526",
          "bytes": 223011,
          "artifact_id": "artifact-814491389a1d4491",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WX.cs.meta",
          "sha256": "e415f7b7a9c6a0dcf4d0a4648149f309fc73fda563841584a018bea7a3cbea07",
          "bytes": 243,
          "artifact_id": "artifact-3675299b26324f7f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXBase.cs",
          "sha256": "3649f43a8ba0d99487994b9edaa5dffc49e3c41a7eb0821c3234a008fbe8f20b",
          "bytes": 48909,
          "artifact_id": "artifact-a9395bd6673d404c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXBase.cs.meta",
          "sha256": "bc019e8d0fd5da510a1a26f69274eca30ce4557c04fc8ddb61370453eee57344",
          "bytes": 243,
          "artifact_id": "artifact-e8a59dd469824260",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXProfileStatsScript.cs",
          "sha256": "4272d299ab608c397b9432a0700fa18fc9d8042133693b247b68c3c64cf51251",
          "bytes": 18497,
          "artifact_id": "artifact-e6d2203cb5784a9e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXProfileStatsScript.cs.meta",
          "sha256": "93e34a09c9ef172cda75e3da18e9dd02b814f5aabf2ed10257618dd524066956",
          "bytes": 243,
          "artifact_id": "artifact-81074bc989f148cc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXRuntimeExtDef.cs",
          "sha256": "aedec5c2055416f57edb9fe04599b63eefee00616ebd2471fa6b609fbe94c7f9",
          "bytes": 4246,
          "artifact_id": "artifact-8992c054ef6540ba",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXRuntimeExtDef.cs.meta",
          "sha256": "7674f27cd1aa4641eda55532323384df7f65b6b4df68b7fe43d0f7235b0d877c",
          "bytes": 146,
          "artifact_id": "artifact-9b83fa65f37e47ba",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXSDKPerf.meta",
          "sha256": "45177aed7c785bf1bdec6f066cd2cea78ad3f688f1ce038366217f62181d0594",
          "bytes": 172,
          "artifact_id": "artifact-fe7bdbe008154f09",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXSDKPerf/WXPerfEngine.cs",
          "sha256": "231dccd4ffd5da40208e0c3b730bd8d16f4e9d8651058d671c9e277c8e1751a1",
          "bytes": 8154,
          "artifact_id": "artifact-16a1ecabb506486b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXSDKPerf/WXPerfEngine.cs.meta",
          "sha256": "3bee556d73d9a6cd334469c29ce2b6b07900bb1c29f1e3bb5f91d4f365c784bb",
          "bytes": 243,
          "artifact_id": "artifact-6e829b183ccb4852",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXTouchInputOverride.cs",
          "sha256": "09980d6c4ee8c0aea32c1740401a9fd557fefda89647ecfab895ac3bcb9c6902",
          "bytes": 8833,
          "artifact_id": "artifact-d55dc39dd5784747",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXTouchInputOverride.cs.meta",
          "sha256": "8d503d02bca41229475dd9ebd5c3945df75e72f363c75433edad75250daa09b3",
          "bytes": 243,
          "artifact_id": "artifact-ffa43e9d0ebb46dc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WebAES.cs",
          "sha256": "ffbc138771bf2c6be130db707ea7d1f1087ca5aa4f06071f3926d026a88008ed",
          "bytes": 3950,
          "artifact_id": "artifact-26f8eccfb3284d6a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WebAES.cs.meta",
          "sha256": "1cdc3b73e7d9e89c26067bcb5e717239bacbec0270b3918bc1df036022002d38",
          "bytes": 86,
          "artifact_id": "artifact-cf4f95c2f9db489a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WebMD5.cs",
          "sha256": "f3d430825013c9de8b902deef8ba7c4a0647c0737884c398c4a409dfa567d6ec",
          "bytes": 912,
          "artifact_id": "artifact-87b4113a046d48ea",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WebMD5.cs.meta",
          "sha256": "8b6cba93f17ff78de498a39280ceb584cae3dbc4d9c6ab051d0ce35be307ffb6",
          "bytes": 86,
          "artifact_id": "artifact-6e54bcf5a6814752",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WxWasmSDKRuntime.asmdef",
          "sha256": "dd9c976a29b94c20924ed2f5f4ca938d3963f60c08fed783d5c83c9e942167e4",
          "bytes": 366,
          "artifact_id": "artifact-a85353b2e42b4c4a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WxWasmSDKRuntime.asmdef.meta",
          "sha256": "43d2c99bbde5f618fd787686e9e2c1580f9e36fcb9c12b12e8a66b1203d1b6a6",
          "bytes": 166,
          "artifact_id": "artifact-5f212e6f8a9f4f24",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default.meta",
          "sha256": "a57c0bcc7f17754df09d5b3d4f673e599490c17681553d5ae87df5b38cc84479",
          "bytes": 172,
          "artifact_id": "artifact-7494601614964faa",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/check-version.js",
          "sha256": "599b2656f6b77962c358e0f4be6e1f3e6536a7eaa804ce76a58d028a0d007dc8",
          "bytes": 7689,
          "artifact_id": "artifact-8fd98e4997484578",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/check-version.js.meta",
          "sha256": "5961a8a9d3508ed476604153b20d4c405b8cb431570a65e048e38ecc9a1e148f",
          "bytes": 166,
          "artifact_id": "artifact-ba5af8f136504b5b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/data-package.meta",
          "sha256": "c87d5d3672ddce321df2ab77ca18669ab0472d50594e1b368dc6e96ff328ef6f",
          "bytes": 171,
          "artifact_id": "artifact-573cbef2ef8249d3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/data-package/game.js",
          "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
          "bytes": 0,
          "artifact_id": "artifact-7cfe09a07ef84d99",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/data-package/game.js.meta",
          "sha256": "518b4d5bd8665c2ee916342e097ba92cc4c6744df490440ffc3fa54259a3f132",
          "bytes": 166,
          "artifact_id": "artifact-beb6aa7cce974f71",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/events.js",
          "sha256": "0e8d53bd4cd51e8833018558abf50a27e28c6294faf16da100142e7b27a41681",
          "bytes": 1545,
          "artifact_id": "artifact-e7f82a8c42924ddb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/events.js.meta",
          "sha256": "ced94a33e69c061cd46b9e8370a07394fc691e396429cc18c46a9b4afac39a7b",
          "bytes": 166,
          "artifact_id": "artifact-20792ed4ab67481d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/game.js",
          "sha256": "ff1ed459ad39ed127a1dbbd972b6879d0a3d7bdab51c7f5290a5d81efb5f9b53",
          "bytes": 7987,
          "artifact_id": "artifact-cb43ae0737de4abe",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/game.js.meta",
          "sha256": "03692a463ccb88d8a6df850b4c62994ada5de755549a9a8dd0da44c0b856c0f3",
          "bytes": 166,
          "artifact_id": "artifact-82ef6733dff848f1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/game.json",
          "sha256": "a8b4ca92b89dc6849c1c03d3d1e9636099669efb4a015260096c4783ee6a06c0",
          "bytes": 134,
          "artifact_id": "artifact-db98eb2e16af410a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/game.json.meta",
          "sha256": "6c351739ff12ccc3c3ec46227b35a8ac7651c39fcc52fc987d5ed4d5e8779a9c",
          "bytes": 166,
          "artifact_id": "artifact-6e2fbe12fb6447f0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/playable-fix.js",
          "sha256": "4a90a197cb694e97c09274f3bc818a2eacea25b1939e16a9d9f37c0ce1059ac8",
          "bytes": 1077,
          "artifact_id": "artifact-4b3c2d2b605e4028",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/playable-fix.js.meta",
          "sha256": "cf0b58ebd94cafa85c0420618d12ce1390e56e091533ed310369a57a3f27c750",
          "bytes": 166,
          "artifact_id": "artifact-ec79afe3e87b475e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugin-config.js",
          "sha256": "00fc1c1d6765f328b5cea31de6aa093e493a78bb6c70fc1ff4f20f2f5b47d658",
          "bytes": 487,
          "artifact_id": "artifact-988a2c5d3c1f4a53",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugin-config.js.meta",
          "sha256": "0f3bee3b92bf95aea2459b73e26e8f5d8b8e7bee7791d6f19595c25c17dd9692",
          "bytes": 166,
          "artifact_id": "artifact-5617e077284d430e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins.meta",
          "sha256": "424cc3c994efb98de546facdb2599133140c716b5245e1deffdf62d3328de147",
          "bytes": 171,
          "artifact_id": "artifact-0c9686b5e83d45cf",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins/check-update.js",
          "sha256": "8723f7b849d6ff531301139470105d22abbc4e1022b3971942c2d5e945a75d71",
          "bytes": 739,
          "artifact_id": "artifact-fcfed57a98944189",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins/check-update.js.meta",
          "sha256": "c0b39ec84a8b5b415602ea3a8502e50d21ca44eaca07f2ee234211f525d5d5fa",
          "bytes": 166,
          "artifact_id": "artifact-b1c06441db5c45df",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins/screen-adapter.js",
          "sha256": "14162739f3761ed1883ea76df0103259687b646bc2b5bdf5933ed22912e49527",
          "bytes": 325,
          "artifact_id": "artifact-acbe0d23d8e04274",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins/screen-adapter.js.meta",
          "sha256": "29af925419e60aa0db3b204a7830ab8f31492fd3abb5964b0a215f65e737b41f",
          "bytes": 166,
          "artifact_id": "artifact-db5c4808d9e847fe",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/project.config.json",
          "sha256": "80ee6721578830cdef6f812ebc3b34480a5a4060e548f38b3ef627082979847f",
          "bytes": 1646,
          "artifact_id": "artifact-e5d559a8da7c41a9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/project.config.json.meta",
          "sha256": "ceecd2bbdbfebdbcf68b471403a3145407680d343c8a672b44a5202761f040a9",
          "bytes": 166,
          "artifact_id": "artifact-8d166f0fe63f43e2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/texture-config.js",
          "sha256": "7f7726142203ab208fd7a062af4bb04951ab8296d634b28dff7a63a65688e515",
          "bytes": 122,
          "artifact_id": "artifact-741df1d3e2334852",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/texture-config.js.meta",
          "sha256": "6077c1956148e4fc0258914dc870573b249f7771dc426f42ca23c18e4fb9b0ec",
          "bytes": 166,
          "artifact_id": "artifact-c7a7e13038914f9a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-namespace.js",
          "sha256": "57a8941c93d7a3a9857e0ce98c14c9f98411297b656f052045dbbbeb2be3fd7f",
          "bytes": 8603,
          "artifact_id": "artifact-4a0be36bc04d4b71",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-namespace.js.meta",
          "sha256": "d3e1634bb1c987a5961b5df9682f590bbe5e0b3ea7e9c7a5117872a8e3f845fe",
          "bytes": 166,
          "artifact_id": "artifact-58f1de6b687343d2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-playable-plugin.meta",
          "sha256": "1f4a7c4668274d448c9d2869b8996031b6e08a8af692258a951ffd100d9a9929",
          "bytes": 171,
          "artifact_id": "artifact-a77d2642d65c49cb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-playable-plugin/index.js",
          "sha256": "53df65173e4e24d99731cb3ce9ddd0e82d2e86aa4912a6009c6122eb4c79aa26",
          "bytes": 69089,
          "artifact_id": "artifact-e73181491c4f4e99",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-playable-plugin/index.js.meta",
          "sha256": "b0be81d40ec9bba0d551d05d634892fae9b89e58ba7230857ed557601f334e22",
          "bytes": 166,
          "artifact_id": "artifact-9abaaee82d8047c7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk.meta",
          "sha256": "be5133ba8e50a22bac8bb601126f306b65db3c0551807bfbacc73ce990b42507",
          "bytes": 171,
          "artifact_id": "artifact-7f299eb83ddb44f0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio.meta",
          "sha256": "c6c20df58db056781222871d0840bc660440b7777ab22fe4b0c2c5a5f355e698",
          "bytes": 171,
          "artifact_id": "artifact-bdb95450f9484af0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/common.js",
          "sha256": "daebe0c943184912dbbdfb9abd32fe58cfdfc2fc502a9df869e44b5cf617dc10",
          "bytes": 2230,
          "artifact_id": "artifact-3be02664aa5e45d5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/common.js.meta",
          "sha256": "0ffa3761455877c9107d401375d8f9b35797d6c860bb18f97c1e20ac2fa16bc7",
          "bytes": 166,
          "artifact_id": "artifact-8f19c2af050548bb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/const.js",
          "sha256": "31a941dae51b6e28bd78520284ce896dae4307f91e1c70ea2311826e4fcf79b9",
          "bytes": 231,
          "artifact_id": "artifact-9df8db3e7d93487a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/const.js.meta",
          "sha256": "b46e5d7ae62c160f8f652fed2d4287cbe47c5412cf99bf305cb6a1074f9d357d",
          "bytes": 166,
          "artifact_id": "artifact-6ca3663017394838",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/index.js",
          "sha256": "596885e8def00ca4cbc47421b94d4a9e1d9d8f666c0444733ada04a33f0b27b1",
          "bytes": 184,
          "artifact_id": "artifact-d33f32f8155547af",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/index.js.meta",
          "sha256": "63955ba03d6813741ddf44875e2bd40c5b62d8da15be8bc835f7564a374b281c",
          "bytes": 166,
          "artifact_id": "artifact-f599ed96e9a24c17",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/inner-audio.js",
          "sha256": "41657bb73852f50f7e996d663ce1184fea4b4c5819fec459680db52152588425",
          "bytes": 12247,
          "artifact_id": "artifact-863546944874419e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/inner-audio.js.meta",
          "sha256": "6b17f560398dbf0160f186bc6b9fd179ddc9a0cf25d8b7fc517f43b6486bdeb1",
          "bytes": 166,
          "artifact_id": "artifact-2d4c554d4d714852",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/store.js",
          "sha256": "d52bb7e2ed9de12b8182d48341e15fddd9c36e2f42484c878401206e90624dc7",
          "bytes": 659,
          "artifact_id": "artifact-1b0c26f070694c98",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/store.js.meta",
          "sha256": "d40bb2c38a2574a0fd634c318245adad0ead16dee20c39d14ce74a33b8f4be75",
          "bytes": 166,
          "artifact_id": "artifact-68d8209021be4cbc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/unity-audio.js",
          "sha256": "de7aed485a4b0628ee12e2d28a8b6aabeef2088ef63fd8a67b6e4c6c41955afa",
          "bytes": 45362,
          "artifact_id": "artifact-b7cf1c04c34c4d34",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/unity-audio.js.meta",
          "sha256": "51c9246f86fb0caae962e64dba7c8df5fe0cc871be0f703300a170471353478f",
          "bytes": 166,
          "artifact_id": "artifact-c5c5539e148c4629",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/utils.js",
          "sha256": "401b07b84ab2745020ba15b0deb9fd68e8b96f5e4e4ecdc6aabe20fc2364e780",
          "bytes": 1974,
          "artifact_id": "artifact-18705f8ea559441f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/utils.js.meta",
          "sha256": "c18939faa7d02a18d25e2f718877b71fa34eb3c9df0f3b19bd53b7c210fb1eb4",
          "bytes": 166,
          "artifact_id": "artifact-af0d0ae83bb943af",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/canvas-context.js",
          "sha256": "dfdc1d383529c529afd74d61eb3555ebce0303eae5dddf585c611b27fb783334",
          "bytes": 330,
          "artifact_id": "artifact-b9bc36e2d11f4978",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/canvas-context.js.meta",
          "sha256": "3f869f0270693b0e76c15ab3d1f2e70c50962d3f18fbf2ac1d5fbb1b862e6eb5",
          "bytes": 166,
          "artifact_id": "artifact-12a78e5a60134c27",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/canvas.js",
          "sha256": "8e4a0fd95e0ab4c4935f58a6201e23468a8ec74c9e3efaeef36329397aef8af5",
          "bytes": 829,
          "artifact_id": "artifact-33f46dab62e947b6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/canvas.js.meta",
          "sha256": "a0c129dbc2b9965681c84631a1f764ec6eeb5753b120844932fe9ae9cd65ccc6",
          "bytes": 166,
          "artifact_id": "artifact-a431783b8cef4957",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/conf.js",
          "sha256": "b1dd63338c3f2689ae3077902a39f3720d45e40db163e4f1979cde4f10f03d36",
          "bytes": 50,
          "artifact_id": "artifact-ffb2852d4e294621",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/conf.js.meta",
          "sha256": "c0661016df709c156fa6b8573db28e1a4f8e699df900946db9dd843299db8b51",
          "bytes": 166,
          "artifact_id": "artifact-e91bfd3944a6406b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/file-info.js",
          "sha256": "f0db1cc7c28a00bf1647325d2ade29f9175c3889d97e58a9f4cf5762b2162c4d",
          "bytes": 1574,
          "artifact_id": "artifact-1f9446ab466e4e9c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/file-info.js.meta",
          "sha256": "d96cad7b68cd3d179a0aba8e49dc787d0ce75f1b2522b5c1dfd75d4c97dd7002",
          "bytes": 166,
          "artifact_id": "artifact-173def5509ca446a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/fix.js",
          "sha256": "221fbd407c603fed4592ca6df54a4b696ce05eaff97c515caf78bb38fa60aadb",
          "bytes": 2776,
          "artifact_id": "artifact-2c70eff88471478d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/fix.js.meta",
          "sha256": "bf79658902e06ef68b8da4c54007397e18c9b428ce6907c2794a8bbcd601028c",
          "bytes": 166,
          "artifact_id": "artifact-729018fd8a104026",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/fs.js",
          "sha256": "d28ff7791372fcdf6ab40c1390131d57b256625e7b7288ca5aa7e3bbc6e95d08",
          "bytes": 16218,
          "artifact_id": "artifact-c6b924bbd1c24a17",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/fs.js.meta",
          "sha256": "c7fd5d5a2d8830052f65efb3b2386ff74e52eef256ae6a8b9127f89073635c24",
          "bytes": 166,
          "artifact_id": "artifact-4d3ccd1755fe42e5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/index.js",
          "sha256": "fe53546589f071aebb4df4dd14f018cded72e9421be3ec49c2fa7bbf479f865b",
          "bytes": 2159,
          "artifact_id": "artifact-af064381f0cb4531",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/index.js.meta",
          "sha256": "e7afe4590049a3abafa55cfc55ef64c2a09f863a42df70fb880f0af647c686d0",
          "bytes": 166,
          "artifact_id": "artifact-38b6266f10044a02",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/logger.js",
          "sha256": "7d765a2c857a05424d08fc4cca83ba32393558f79fbc46c653a98bf440b03013",
          "bytes": 620,
          "artifact_id": "artifact-2eba68b22e9b47d9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/logger.js.meta",
          "sha256": "0b0b536d48dacc871d41a8e88ce3386a3357f14822bb3d60795d77fdd9126f3f",
          "bytes": 166,
          "artifact_id": "artifact-5d542a60844548a6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/mobileKeyboard.meta",
          "sha256": "8ba6b8516d7a2c0499878ce887fca28e751d83294e2e628274ecd3cad8cc35c3",
          "bytes": 171,
          "artifact_id": "artifact-2e606830fdc8477e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/mobileKeyboard/index.js",
          "sha256": "fff32a092621032170bd2ef8f92bf95d0a2607a239634e659e6b9bee6f01be0b",
          "bytes": 4273,
          "artifact_id": "artifact-055db8d47bde4733",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/mobileKeyboard/index.js.meta",
          "sha256": "7b22d69248c2bb7e3396a774dd9692c324a6a4850ab461bf6cb50868bbef3d71",
          "bytes": 166,
          "artifact_id": "artifact-965e6cfa81ee4cf9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/module-helper.js",
          "sha256": "5be6623e72561c69479b279dbb423b09e70041c914a465d9ff2837d03cb8e913",
          "bytes": 355,
          "artifact_id": "artifact-0a6c776c95f54498",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/module-helper.js.meta",
          "sha256": "c1c280179e7f9a5e87bbc71db4f643e469155d80818f06149edeaf7ff9ceb614",
          "bytes": 166,
          "artifact_id": "artifact-6d4ccf49a2584272",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/recorder.js",
          "sha256": "eaba87090f60fb22bb59bf1e82dd16a5aae134b2e9dc08c403f79c61652b51cf",
          "bytes": 4469,
          "artifact_id": "artifact-9d59a139e9214b03",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/recorder.js.meta",
          "sha256": "13a829d1c0acd26c69cf3c7166fa92cfa731b193d084ffb44731616d79722d09",
          "bytes": 166,
          "artifact_id": "artifact-0cb1ea0bb5964aa3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/resType.js",
          "sha256": "e2135586c03404792b961ea032e53d7000f552a4b61cb066896d149b13b6d2cf",
          "bytes": 30137,
          "artifact_id": "artifact-f4a6cf6db1714c27",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/resType.js.meta",
          "sha256": "4f7a2d6b7310798709f5c3be9a3c116721d86a6e4c6a706717565175dc6a2646",
          "bytes": 166,
          "artifact_id": "artifact-2a215998c6a04ec2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/resTypeOther.js",
          "sha256": "af5e7538fad8f6640239322cdfc7a6c5181995551cd7a6772b8311b2df55b247",
          "bytes": 2293,
          "artifact_id": "artifact-88d012e1128849a9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/resTypeOther.js.meta",
          "sha256": "659bfc0974df7c23aa972298bf8a1a493eef79d40fd2caf68b484ee78b6d162d",
          "bytes": 166,
          "artifact_id": "artifact-4dcd0b763d2b456a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/response.js",
          "sha256": "9bf4b44ef758ec8917aac3a1827de70f27bacc881849bf985cafaab313216304",
          "bytes": 1891,
          "artifact_id": "artifact-964cc740f6af4d9b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/response.js.meta",
          "sha256": "447d6df2a8f0a822911666ed6558e03655bfaffa58d0b9fb09e5f7f560873c62",
          "bytes": 166,
          "artifact_id": "artifact-60e4e6803e56482d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/sdk.js",
          "sha256": "8377aca08a51213a937cd6fe0b49837dccaa607e1d73dd1c955d7181324811f1",
          "bytes": 17209,
          "artifact_id": "artifact-83a162bedaff4bcc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/sdk.js.meta",
          "sha256": "b77a286dcb3bf4ea4839d2718fc78af8f473eab3edca5a05a55494f0a80e8845",
          "bytes": 166,
          "artifact_id": "artifact-f67d2782202f4745",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/special-callbacks.js",
          "sha256": "30bcf71dab5024efd455339a9e4e2fd0aa56bd918fe6169f974ae8a246012919",
          "bytes": 1345,
          "artifact_id": "artifact-488c4686a90b474c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/special-callbacks.js.meta",
          "sha256": "afa6b7de060146a698df759af34538605745d6118f6b30daa60ecfa0812bf4ae",
          "bytes": 166,
          "artifact_id": "artifact-e673b9b7c0174985",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/texture.js",
          "sha256": "ff27b378812ef12f39cacc4a2605355d74141d67adfaf83528df597adad77aac",
          "bytes": 10740,
          "artifact_id": "artifact-adb8172a9d08443b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/texture.js.meta",
          "sha256": "166bcd33143f4fc4055fbdcb8b4afe80ff48936459c3180788b86b9a1a52446d",
          "bytes": 166,
          "artifact_id": "artifact-abb8d9957ea1489b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/touch.meta",
          "sha256": "3047e978a9f8f217bebe722afabf16918308fb0cc8b0c739b105033392a84fb8",
          "bytes": 171,
          "artifact_id": "artifact-deeb3fa17c2c438b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/touch/index.js",
          "sha256": "a5917a61534de7bc2f5224a9c9402863a264cf6bd2106d5536eb6018299de53f",
          "bytes": 2301,
          "artifact_id": "artifact-72a19baefbe546f0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/touch/index.js.meta",
          "sha256": "4817217627f375988f6cb94c0293e86196e57edede1a9e7a8d6bdfdeece2455a",
          "bytes": 166,
          "artifact_id": "artifact-e161f5c743d0462f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/util.js",
          "sha256": "f7694b65a403bfa09e90458b33414f0a48afeab225657e2ee6d256f580388312",
          "bytes": 6193,
          "artifact_id": "artifact-34eeb9cffd504e12",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/util.js.meta",
          "sha256": "491cb8c6359f52a306b9176fc82e12a9a8f14e8eda2cd4f7e163c0a2a7b0193f",
          "bytes": 166,
          "artifact_id": "artifact-2d210b9bcd1344c7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/utils.js",
          "sha256": "fa04582ad3583d37db517798364eae68075b884906d93beeb3b067f697cabd5c",
          "bytes": 12987,
          "artifact_id": "artifact-ad698da503ef458d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/utils.js.meta",
          "sha256": "9ad8faab707b2e80efaf972935bf70a80693e904e7e236d769444755d97a66b8",
          "bytes": 166,
          "artifact_id": "artifact-e1cad2a018c34241",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video.js",
          "sha256": "b2d156cb1b437508d55bf8a32a9096f6d370ddf196f63bf5113d9c4597e2e128",
          "bytes": 2552,
          "artifact_id": "artifact-dd488b3949ce428b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video.js.meta",
          "sha256": "1bc652235dbdd6b56c78715aae87dfc2e5625f40c0ecbb043b038ce05421255c",
          "bytes": 166,
          "artifact_id": "artifact-39dccc3f34e84708",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video.meta",
          "sha256": "2667056c792e591d58a5926a2617c12e05b1be95297c145550809cdb4e2c0b0b",
          "bytes": 171,
          "artifact_id": "artifact-1397ff9b51f74461",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video/index.js",
          "sha256": "b70a96625b7f87d0556f16ca2cb8bef1ca0e7638330a433a66003e627c9b24ac",
          "bytes": 16149,
          "artifact_id": "artifact-15f8d379fe7d494d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video/index.js.meta",
          "sha256": "61a3194948e1d7539f082c49d3f97fb71787705d6be4018c4be7dec3b02d153a",
          "bytes": 166,
          "artifact_id": "artifact-054f5df9e96d42ad",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/wasmcode.meta",
          "sha256": "ac46a4dad77e83ffe13b6b8d08d9571665907009029dde9265c601af75be238f",
          "bytes": 171,
          "artifact_id": "artifact-7f260c0901774137",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/wasmcode/game.js",
          "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
          "bytes": 0,
          "artifact_id": "artifact-349d74e68ce74e01",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/wasmcode/game.js.meta",
          "sha256": "5d49180cc8a3313700be9bc041c347576ee10c63028049b4e5812a12a87fde07",
          "bytes": 166,
          "artifact_id": "artifact-bbc259f482cd4075",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/weapp-adapter.js",
          "sha256": "2787e9f449849de9b4fb632841bf94b9f405d03446e4d54a37e50ea9d3d65a75",
          "bytes": 72552,
          "artifact_id": "artifact-10fdff2a4c144d5a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/weapp-adapter.js.meta",
          "sha256": "d22fe5c7f8be7d5677521792c46773e16fe92ea1a169c99ff253e69af5204994",
          "bytes": 166,
          "artifact_id": "artifact-780fccbbe73e41e3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default.meta",
          "sha256": "991d2fa8857422a650828bee21d04f4a0442cdc93b365bf702aba47e1d36595f",
          "bytes": 172,
          "artifact_id": "artifact-e70f40edac5347b5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/check-version.js",
          "sha256": "0cd6a4e91506b41a7d1cf1b87474e79685fe577ffe1e8c3f0b3b66f75c8e8242",
          "bytes": 8264,
          "artifact_id": "artifact-5026402d42dd45f8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/check-version.js.meta",
          "sha256": "e41269d888890ef6c66ef44a172a1dec74d161f9b6c4d359845218c6df4aa8ef",
          "bytes": 166,
          "artifact_id": "artifact-bfe2c2d29f524a0c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/data-package.meta",
          "sha256": "36cd46b6904899aabd744d59ac7f8cad6462ee7625bf214aee273636505f6eb6",
          "bytes": 171,
          "artifact_id": "artifact-fcf9efa962aa4636",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/data-package/game.js",
          "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
          "bytes": 0,
          "artifact_id": "artifact-cd5fc020e6a24501",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/data-package/game.js.meta",
          "sha256": "df3311bd85471c23b5445eacbbf1d699c40674b958fcd2da1120e7ec7651ec9d",
          "bytes": 166,
          "artifact_id": "artifact-f7d41e3479064190",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/events.js",
          "sha256": "0e8d53bd4cd51e8833018558abf50a27e28c6294faf16da100142e7b27a41681",
          "bytes": 1545,
          "artifact_id": "artifact-90d5bc9c2ff84f3a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/events.js.meta",
          "sha256": "f6daf9de83b0a5da30e284c3bca883dddaab3fd7e9b72b20894cd994874a1d09",
          "bytes": 166,
          "artifact_id": "artifact-6825fc5171994192",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/game.js",
          "sha256": "da25420acad9ae5e62fbc93d9408634bc04b2ef8957f28355ab1a757b725ea7a",
          "bytes": 8141,
          "artifact_id": "artifact-945050864fa84f7d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/game.js.meta",
          "sha256": "b9a69f5a60753737f2e6aaac9a3caddc9599fbea89422cd2fe1c03ac1dc1a240",
          "bytes": 166,
          "artifact_id": "artifact-b9ef7e3485a5474f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/game.json",
          "sha256": "33ce331329781c2704935c796fc3ef4ad763824b058d17c2ffbe6be2cf534bd3",
          "bytes": 1014,
          "artifact_id": "artifact-182f22d1d9ec4d57",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/game.json.meta",
          "sha256": "9d91efe5a9b7d4a081ae146b01c150b667ebe84390bfb78959ff7d05b7920b1b",
          "bytes": 166,
          "artifact_id": "artifact-4c1faa7a4e7544e5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images.meta",
          "sha256": "27cec5c2cb92e37fdf1e69daf1ea3c8f23c30d5ad9b067d2494516873f3b9d96",
          "bytes": 171,
          "artifact_id": "artifact-94b1c9cd928c4e3c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images/background.jpg",
          "sha256": "ee88b4672db843d57b164c5deeaf62e096e3bf91881bdba71a85dd980687db80",
          "bytes": 8917,
          "artifact_id": "artifact-36946001a0614ea4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images/background.jpg.meta",
          "sha256": "9f63d1c0b7ec63b3578bb79afd19540e055f1c09c753287a7292267ac5ef4c8e",
          "bytes": 166,
          "artifact_id": "artifact-5863f3c41be1476c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images/unity_logo.png",
          "sha256": "34799ce1b7a341e39c6c6ea7fa439f170989f3ecc5d0cd3681573569b4b6d239",
          "bytes": 1216,
          "artifact_id": "artifact-4fde62f7ee874895",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images/unity_logo.png.meta",
          "sha256": "a9f0edc0331aeeac5e7973e9e9f4cfea78c98079a45883676cdc610b162653db",
          "bytes": 166,
          "artifact_id": "artifact-ca174fe43a494b32",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data.meta",
          "sha256": "1808ef2224daae0c7540139c16b03ff68b70d2f304fddb8c15c7efe2626339f6",
          "bytes": 171,
          "artifact_id": "artifact-ec649f0596814812",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data.meta",
          "sha256": "f1105aa903f989e01c1af4bfcc91893d5bf0a42f38495aa6dccef9194b89ba7d",
          "bytes": 171,
          "artifact_id": "artifact-895ab2c8498f4e64",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data/index.js",
          "sha256": "cf9f355257a4b6cdad59436962371b816847fa7df294fe0ba2fce7eb22b87552",
          "bytes": 5017,
          "artifact_id": "artifact-909eae2c75234c09",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data/index.js.meta",
          "sha256": "73d48412cffe027f3bdce4b05903adb6108c225403368e9e6102e89958c61403",
          "bytes": 166,
          "artifact_id": "artifact-1aeb548d80ad4881",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data/utils.js",
          "sha256": "5b154de8c3812effc187a5d32969aa41c3d0ce471e0cee803d1de0820dd2cdc9",
          "bytes": 285,
          "artifact_id": "artifact-a6d1f7c7ac644af4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data/utils.js.meta",
          "sha256": "cc3c1f56e6ab10d6d95bbf08906d6433964cb2366ee46af1a18ab94a77efcb68",
          "bytes": 166,
          "artifact_id": "artifact-4a1240a9638b4d92",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/index.js",
          "sha256": "df850050fcf461dc5ad74f5bfb4da3ba2841b30a7b410a3ff17a7945becc0648",
          "bytes": 5249,
          "artifact_id": "artifact-eb7dc49408e340b8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/index.js.meta",
          "sha256": "e708a2305390e2c4d7275f7820f7d3b4426f1e30bb84e270b4bc70951e3d7f96",
          "bytes": 166,
          "artifact_id": "artifact-650cbbdd93c24975",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/loading.js",
          "sha256": "c6eb83fb34a7532a7490a80fd3040636776f7a32c7719ebc5a9d194483155d38",
          "bytes": 868,
          "artifact_id": "artifact-3d643d51a20f41b5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/loading.js.meta",
          "sha256": "b1a8ad7aca4371db776e710528e56a67ba40418de85104f75601f732e614c7de",
          "bytes": 166,
          "artifact_id": "artifact-4b0dd1bf80134514",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render.meta",
          "sha256": "dc502c7ec9d41193cc0cde2d789293a917575c29584cba5ea27c5f371a5001d1",
          "bytes": 171,
          "artifact_id": "artifact-45ed3d4687484d4b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image.meta",
          "sha256": "561e5e55760533e8914206cca18b91f478a53b6d0b9917784cf63f727cc2a44d",
          "bytes": 171,
          "artifact_id": "artifact-9490633fb10947cb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/avatar.png",
          "sha256": "63196c60196e8526399f4c4b4421c9844718738a32bbdaa7767ebac2864e85b8",
          "bytes": 5026,
          "artifact_id": "artifact-098f4b136a714eae",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/avatar.png.meta",
          "sha256": "b1d17fc500694d14a291337ebfb26c845ee855298327174a501f595f692d08d8",
          "bytes": 166,
          "artifact_id": "artifact-34d08816d75340fc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button1.png",
          "sha256": "694daefae30a8ce1900ac09b894b100cc8f5bd14b5bc9bc5da21d77515b4717c",
          "bytes": 870,
          "artifact_id": "artifact-14de44e43990439b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button1.png.meta",
          "sha256": "66032004fdee0c6079fe6df4e0a0f9da2b5137e593a475ae13e817bdb9fcfecd",
          "bytes": 166,
          "artifact_id": "artifact-0098856dc62f43f9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button2.png",
          "sha256": "6375e5e4136df1f6a36eafbc9c5199fcf5e0572e607b4e6afc234bb4fe433323",
          "bytes": 816,
          "artifact_id": "artifact-93ae4e8364a34878",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button2.png.meta",
          "sha256": "a3b94d41930f40d735128507d81580d1b5018002b163ecd0d12c9c7aabd6309a",
          "bytes": 166,
          "artifact_id": "artifact-ece802153cf04752",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button3.png",
          "sha256": "7b79d41816520bade57466ccdcc7fbbf1b423166c5cad02c705b3e9ab513b3f6",
          "bytes": 8880,
          "artifact_id": "artifact-b19fc19710a04a4d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button3.png.meta",
          "sha256": "244e726d9cbdb72ef3c744b942bbb099d11d2d5ac429345bb9ff55b3e29cb8aa",
          "bytes": 166,
          "artifact_id": "artifact-cf15aa834c15459d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/loading.png",
          "sha256": "ced01531e93259285e4ce9f440699921f7d7ad2616084ba6caff7af24eee43eb",
          "bytes": 6244,
          "artifact_id": "artifact-b3635ed780c8499d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/loading.png.meta",
          "sha256": "a65ea8e04e84d0617cdbab3d66a539e109d46e40b5dc93999f135c4d1ab2c780",
          "bytes": 166,
          "artifact_id": "artifact-0d9be0fbc81246ee",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/nameBg.png",
          "sha256": "9aad869319d994c9cd1c417ab8e965c51765af7691c77cb5bd638cafe6c8770f",
          "bytes": 339,
          "artifact_id": "artifact-377024368ce94555",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/nameBg.png.meta",
          "sha256": "d820643240d4200265b5c1d3e1859a198e2c97934e425689f3f8542afa9f5254",
          "bytes": 166,
          "artifact_id": "artifact-629106610e614415",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/rankAvatar.png",
          "sha256": "0fcbc7c8ad88eee4e3fdc241fc13e7c625220b20b3a894663de329d5500e4af0",
          "bytes": 7121,
          "artifact_id": "artifact-dd17f86131c94cfb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/rankAvatar.png.meta",
          "sha256": "e6488adbb97bfc924e5ac6a1000d61528c1a943df5e78f891bc3ae9c80066d1c",
          "bytes": 166,
          "artifact_id": "artifact-2608b46b1ed342cb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/rankBg.png",
          "sha256": "a8e4cc0b6fd9878e7243526b435b661ec91c5bc7c95b666d453d85d81cf1ae56",
          "bytes": 61065,
          "artifact_id": "artifact-a272be7bd4b54ab5",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/rankBg.png.meta",
          "sha256": "1560d64cbf7f77087d589e90da3982df6b89c6371a051c359dd5fcead1f2e8a9",
          "bytes": 166,
          "artifact_id": "artifact-3796270072e64b36",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/shareBg.png",
          "sha256": "d666d8a12d78635b0c97dd448101d78324cec1f981a74f37978e5520d936d8c3",
          "bytes": 9008,
          "artifact_id": "artifact-02c8c0ff77494d0e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/shareBg.png.meta",
          "sha256": "7de11d365f94e3a8c290813c1c44e2e9f799c6bddf1312f3880e918354e38d2f",
          "bytes": 166,
          "artifact_id": "artifact-bab23c9f1f664f8a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/shareBg2.png",
          "sha256": "db92ef8013c61365ada8917d015530345262e7611cc3f83c5aac75a80bfa92be",
          "bytes": 13432,
          "artifact_id": "artifact-3fce5430aeee4558",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/shareBg2.png.meta",
          "sha256": "13216ba15b311962aaca5d7877c05daa57b82aa4168f63720c33ae4229a1ba7f",
          "bytes": 166,
          "artifact_id": "artifact-70888ba0b3f448d7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles.meta",
          "sha256": "e8e97600b8c4557be406e2626a133005580a15080a5834a70e54ab0a1edb061a",
          "bytes": 171,
          "artifact_id": "artifact-23fff7b2f859412c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles/friendRank.js",
          "sha256": "ed202f428d6e32ec47a46b94c910102cea0bbad04c49d87f476041b374e275ea",
          "bytes": 4552,
          "artifact_id": "artifact-e9f2c3200dbf4a2c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles/friendRank.js.meta",
          "sha256": "e8753c6d3085b4715adde0a97d3ef3a7ed3aa7844f826bb987e2e82f89abdbfb",
          "bytes": 166,
          "artifact_id": "artifact-e6b9fcb2f9a447c3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles/tips.js",
          "sha256": "cbb306f11d4bcd6f2ed7e1dd9e6d1058d15233b91e592c33a32f3c6c2ee305bf",
          "bytes": 436,
          "artifact_id": "artifact-6b9d908332314811",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles/tips.js.meta",
          "sha256": "d98a5fad9a14cd47af201dcc4680c79521aa79b0ddd0fe494410548110c8939e",
          "bytes": 166,
          "artifact_id": "artifact-96e055075a6c40c9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls.meta",
          "sha256": "fc237ef0250834d27f5271ff5f2b0b695189d0bd406abcede090ea3dfdc825b4",
          "bytes": 171,
          "artifact_id": "artifact-ea5eeca41da64e62",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls/friendRank.js",
          "sha256": "7f0d1eb610da78c0723f40a5a3d0ffbe698b0f50ed4ad9d7729f140ed424176a",
          "bytes": 2803,
          "artifact_id": "artifact-80c4b499cc024908",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls/friendRank.js.meta",
          "sha256": "4ef2f2ceb2da79170523b5455a7325023200a309e12c29b6e0b0b152ab7e0179",
          "bytes": 166,
          "artifact_id": "artifact-5595a888d74d4c94",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls/tips.js",
          "sha256": "3d8ad929216eec1234e36cb7e13653f58889b23d33034b1eff867a1641ac3a4e",
          "bytes": 1032,
          "artifact_id": "artifact-65011e01396447da",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls/tips.js.meta",
          "sha256": "863d57e359d90306242aa14f507261b661bab4c46e1f312b5831771e9d397892",
          "bytes": 166,
          "artifact_id": "artifact-841b5ef8cb4a459e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugin-config.js",
          "sha256": "00fc1c1d6765f328b5cea31de6aa093e493a78bb6c70fc1ff4f20f2f5b47d658",
          "bytes": 487,
          "artifact_id": "artifact-bdfc334487334f92",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugin-config.js.meta",
          "sha256": "2a7fa239f5f17247f8ea923c78fe016f2b91c55a1a9efb7f5220cd25b377b89a",
          "bytes": 166,
          "artifact_id": "artifact-8e5e4c8f58444324",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins.meta",
          "sha256": "f22671a4cf1d7d0c2f391836f0ec4b72a7c630ce0348852dbad2cd3ca542ba62",
          "bytes": 171,
          "artifact_id": "artifact-987420e6163c4b1c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins/check-update.js",
          "sha256": "8723f7b849d6ff531301139470105d22abbc4e1022b3971942c2d5e945a75d71",
          "bytes": 739,
          "artifact_id": "artifact-d0baf6aa10e34632",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins/check-update.js.meta",
          "sha256": "52f32dbadd8ad8ea673faa330e4858a10e28e55bb2d445849d034ff69c4f38f7",
          "bytes": 166,
          "artifact_id": "artifact-84f38528cae844b6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins/screen-adapter.js",
          "sha256": "14162739f3761ed1883ea76df0103259687b646bc2b5bdf5933ed22912e49527",
          "bytes": 325,
          "artifact_id": "artifact-b3e8f1b139184787",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins/screen-adapter.js.meta",
          "sha256": "392115a55763b54935ddde75253808bc922062d3a0be6b4467cbc95cf2357cf4",
          "bytes": 166,
          "artifact_id": "artifact-02a7910e14a14765",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/project.config.json",
          "sha256": "a9b5e250e47455f3b4db07d41b6ca338c9d62215fdc1c750f0e2bc6e9fa3127d",
          "bytes": 1647,
          "artifact_id": "artifact-5e081488f02c4554",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/project.config.json.meta",
          "sha256": "9421a371074e7da969b7505f4be035bb0f8f3bf41efa25f952c8f8fe2bf4d3d5",
          "bytes": 166,
          "artifact_id": "artifact-ef4a089d812241e9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/texture-config.js",
          "sha256": "7f7726142203ab208fd7a062af4bb04951ab8296d634b28dff7a63a65688e515",
          "bytes": 122,
          "artifact_id": "artifact-8c34ab83b4484d57",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/texture-config.js.meta",
          "sha256": "344300850a528721b1e574f7f9c3950410a7af77ad8984b1ba0f7f19d87fdf1e",
          "bytes": 166,
          "artifact_id": "artifact-f10b0da382524d10",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-namespace.js",
          "sha256": "e049d40157fae569fba3fa519a62f00b714b6d8de722250e57d32a88ec4628d1",
          "bytes": 9135,
          "artifact_id": "artifact-15701a4052394e60",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-namespace.js.meta",
          "sha256": "6b9ef4463881fdf856749c5829772fbf2488982784b646aec0c49767cf52b083",
          "bytes": 166,
          "artifact_id": "artifact-5eb4f629b70d4cbc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk.meta",
          "sha256": "869cba0322567e997728a90bbb8cff84ec886854eb5f8e2dd458bd9e889e2b45",
          "bytes": 171,
          "artifact_id": "artifact-51677c7c4e754a0b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/TCPSocket.meta",
          "sha256": "45993995d28c46316becd9cfd45061dcd863a3cde0b7a1ecdcc664b7bc82c79e",
          "bytes": 171,
          "artifact_id": "artifact-8bc70edc85f84fe3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/TCPSocket/index.js",
          "sha256": "03616ad9e25e19aede3e7256e894e5b12c6847aec2fc2002b93bf44c8e439852",
          "bytes": 5369,
          "artifact_id": "artifact-477bf4e331254baf",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/TCPSocket/index.js.meta",
          "sha256": "af9f5df7973e4569bf685e1bf6f0a70fa0e6d527bec12a11fc220dbee2757334",
          "bytes": 166,
          "artifact_id": "artifact-0110150e40cc4357",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/UDPSocket.meta",
          "sha256": "bccde3dd05dc240d44b8b399440dba6e4e65bdf1eadf2d5d7b22a2a60697c2ac",
          "bytes": 171,
          "artifact_id": "artifact-fda033824b6140ee",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/UDPSocket/index.js",
          "sha256": "681a1bfdabbdc65ef8c4e8ef389b2844fd3f9a457ea1876c9c19a0b18620fae3",
          "bytes": 6255,
          "artifact_id": "artifact-1b738156d4914232",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/UDPSocket/index.js.meta",
          "sha256": "f17f80b1e73091b2c87f5e981f9716b226421402eba4aa938aef1d9492e209ac",
          "bytes": 166,
          "artifact_id": "artifact-2e09d4471cf54f81",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/ad.js",
          "sha256": "1dafdbe346ef838cd421f3ff887612bcb6c9a7d2f44c1cce26cf3e6e2f005ecd",
          "bytes": 8884,
          "artifact_id": "artifact-350fc04e986b43b1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/ad.js.meta",
          "sha256": "56c1b99f48f959e51c4bc1e466d7775a8f8f39b0d90e7369cc2c05f9b5b15d96",
          "bytes": 166,
          "artifact_id": "artifact-997dea1bdb894897",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio.meta",
          "sha256": "8e25c165562deef0ee57f88f56841f95671fbddd56a06599ce93f8720a02b59e",
          "bytes": 171,
          "artifact_id": "artifact-8c6bd3ecb99a4286",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/common.js",
          "sha256": "daebe0c943184912dbbdfb9abd32fe58cfdfc2fc502a9df869e44b5cf617dc10",
          "bytes": 2230,
          "artifact_id": "artifact-28defacc7ef04265",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/common.js.meta",
          "sha256": "268b6d9122b63488b368292863c781463b686bf857e3bf6ec6c9f48deac9c26d",
          "bytes": 166,
          "artifact_id": "artifact-fb88267a42f545fd",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/const.js",
          "sha256": "31a941dae51b6e28bd78520284ce896dae4307f91e1c70ea2311826e4fcf79b9",
          "bytes": 231,
          "artifact_id": "artifact-282025bc05864f1c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/const.js.meta",
          "sha256": "036cb368fea4a99a2fd817ab339f33902fc466103ff44622c1db0dd0c4b3cf1d",
          "bytes": 166,
          "artifact_id": "artifact-59fd1263ace34aba",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/index.js",
          "sha256": "596885e8def00ca4cbc47421b94d4a9e1d9d8f666c0444733ada04a33f0b27b1",
          "bytes": 184,
          "artifact_id": "artifact-068f32e6c50e420c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/index.js.meta",
          "sha256": "4800ff491ef7498113f3f334a645053d6baa3e14c542e31bae63160e81f1b51b",
          "bytes": 166,
          "artifact_id": "artifact-d541cd4696c54683",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/inner-audio.js",
          "sha256": "41657bb73852f50f7e996d663ce1184fea4b4c5819fec459680db52152588425",
          "bytes": 12247,
          "artifact_id": "artifact-7c5dd99a63c840a7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/inner-audio.js.meta",
          "sha256": "d570d6f43f91d6d2898cc84696d18ea6387b4866edcc6ae3fdadfcfa32f74d43",
          "bytes": 166,
          "artifact_id": "artifact-ccac2161fd524032",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/store.js",
          "sha256": "d52bb7e2ed9de12b8182d48341e15fddd9c36e2f42484c878401206e90624dc7",
          "bytes": 659,
          "artifact_id": "artifact-467d23a3f0f8417e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/store.js.meta",
          "sha256": "c8a131c6ba3ad3058cde02316f5609392ca6b8561ce15e99baf7f6f709fced2d",
          "bytes": 166,
          "artifact_id": "artifact-40cbc1fca41843b9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/unity-audio.js",
          "sha256": "a3a17656bcf86a851287e60f3cbe52119b9c413c94ece1ee11ad052540fcf648",
          "bytes": 47093,
          "artifact_id": "artifact-778184f653124682",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/unity-audio.js.meta",
          "sha256": "d1980abc8639bc701951211ca14b29fb45c14f2aa13d11ea00fa081142359834",
          "bytes": 166,
          "artifact_id": "artifact-8d38b446ecc14372",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/utils.js",
          "sha256": "45d4cc6d39e17301bacf4b00cc8f79491cf5eb6e3a5997493324870e5a66ad98",
          "bytes": 2059,
          "artifact_id": "artifact-c6612c8624b74f97",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/utils.js.meta",
          "sha256": "4024e33a2b1a39b3d64f4495d58520eac966305dc07c811a91ee4a291d47d94e",
          "bytes": 166,
          "artifact_id": "artifact-031cbc74952f4a1e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/authorize.js",
          "sha256": "36f6acce3576ef206fbd7ab7e41adc3613522c987bab8bd16344cef99ad25656",
          "bytes": 734,
          "artifact_id": "artifact-92fdc97fa2074d0e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/authorize.js.meta",
          "sha256": "007b09388d2c135ebea1f7cdaf0e2d55ad633617f73046ea153fde1f5107ec5c",
          "bytes": 166,
          "artifact_id": "artifact-36ed80c3f1bb4c20",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/bluetooth.meta",
          "sha256": "6605ffaf6544f2c4622d779c6b3ee6b0bed6305de58cfa25f4aaba7d18c536fa",
          "bytes": 171,
          "artifact_id": "artifact-143492929c5047f6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/bluetooth/index.js",
          "sha256": "1f664af1d27a75db3a306a1297508584cf4080fc9000d5fb5db67d4195dea09f",
          "bytes": 1288,
          "artifact_id": "artifact-19cda74bc64242e2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/bluetooth/index.js.meta",
          "sha256": "2b998b0cc6403d6b7ec44aea16f01c933c39e1f649e799390e5337844488bbda",
          "bytes": 166,
          "artifact_id": "artifact-7a323160bf8f4270",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/camera.js",
          "sha256": "d745f848ef62da3a0a0052c518647301375437493b4e6e56422f570dd76ef09f",
          "bytes": 2958,
          "artifact_id": "artifact-84fd2c114207406b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/camera.js.meta",
          "sha256": "d6a2bbb858c025f83e0fe6d4255ef33b9d7388a8ef1c32fcc0bb76ca48f0bc26",
          "bytes": 166,
          "artifact_id": "artifact-3ad5c98719e84c23",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/canvas-context.js",
          "sha256": "dfdc1d383529c529afd74d61eb3555ebce0303eae5dddf585c611b27fb783334",
          "bytes": 330,
          "artifact_id": "artifact-4eeb12a5533847f1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/canvas-context.js.meta",
          "sha256": "02385bff32ba550688a643e30259d11ea2ce7ea011a0c74f21fcc65825081b5c",
          "bytes": 166,
          "artifact_id": "artifact-f3e7df7b0caf4d8b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/canvas.js",
          "sha256": "8e4a0fd95e0ab4c4935f58a6201e23468a8ec74c9e3efaeef36329397aef8af5",
          "bytes": 829,
          "artifact_id": "artifact-5e81502d1a884cd0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/canvas.js.meta",
          "sha256": "626c5ecb547aac30d3d2d0498b651b1096c7459e6f9795c234a8da4aca4dffe6",
          "bytes": 166,
          "artifact_id": "artifact-f59cb05c78354369",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/chat.js",
          "sha256": "8cc33a6475217e9fe9fca8ca8a7078b235258defd0e1dd03bb05dbb25365f496",
          "bytes": 5058,
          "artifact_id": "artifact-00dceb3a6c014a2a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/chat.js.meta",
          "sha256": "e60e2dcd4742b7d5d0875706b98b9461940dd919fcab0f796c1e5a4f57c62534",
          "bytes": 166,
          "artifact_id": "artifact-4542a85f3cf24225",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/cloud.js",
          "sha256": "7fa5ff8c105a7b45efbc97a288eb7c82470d8c8f255bff46ae956dfc313d7d52",
          "bytes": 8972,
          "artifact_id": "artifact-06b73815bb544c33",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/cloud.js.meta",
          "sha256": "4c7d3a7837111e7fbbe882f5e52445b2cefdbb594a31d47966d19477ddbd31a1",
          "bytes": 166,
          "artifact_id": "artifact-cd3bda75ab1b4f83",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/conf.js",
          "sha256": "b1dd63338c3f2689ae3077902a39f3720d45e40db163e4f1979cde4f10f03d36",
          "bytes": 50,
          "artifact_id": "artifact-e11f82cee4924b62",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/conf.js.meta",
          "sha256": "be94ab5babcfb8dfb002fb506ef6fda86402e4ad1df02d9c45658fbc523acc82",
          "bytes": 166,
          "artifact_id": "artifact-1c5b04539f5d451e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/file-info.js",
          "sha256": "f0db1cc7c28a00bf1647325d2ade29f9175c3889d97e58a9f4cf5762b2162c4d",
          "bytes": 1574,
          "artifact_id": "artifact-1345a66a28d64662",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/file-info.js.meta",
          "sha256": "f4c7747af79b7ab6083ce4caae94b3b6819fb904a4a2924bbcbb5cb009745c7c",
          "bytes": 166,
          "artifact_id": "artifact-1e6d2cade3b64611",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/fix.js",
          "sha256": "221fbd407c603fed4592ca6df54a4b696ce05eaff97c515caf78bb38fa60aadb",
          "bytes": 2776,
          "artifact_id": "artifact-1813c0ded7b740b7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/fix.js.meta",
          "sha256": "420a8b4d956687a5d3b09a64eef7f91626896eaae70b3c7baa053fcc24c9c65b",
          "bytes": 166,
          "artifact_id": "artifact-f006b2605ea9433e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font.meta",
          "sha256": "7b47ea70be8a6d12816c22fa1a47258e436db79ec3fd0059604dfbef4756215c",
          "bytes": 171,
          "artifact_id": "artifact-6eadee1398954a78",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/fix-cmap.js",
          "sha256": "c56997b41d07c3c09dd1fb222314533c42a3b1fe3878346adb0b1b378bf280bd",
          "bytes": 2239,
          "artifact_id": "artifact-f06ebeeb6b754ac6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/fix-cmap.js.meta",
          "sha256": "1bcd93b21f66601fca595cbcb5f3098aae284a688a0a660c1755cde8e0e54579",
          "bytes": 166,
          "artifact_id": "artifact-f7ca73f4213847f7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/index.js",
          "sha256": "4cf142ac29ce039b770ab59c007b8831e0994870f42b64e04a9f1fd42a6a57ee",
          "bytes": 7025,
          "artifact_id": "artifact-67bfd4b183214e0e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/index.js.meta",
          "sha256": "a92c95a4326eb9093fe0b463d83350520d66c97c07eb3f087fdca3f704738788",
          "bytes": 166,
          "artifact_id": "artifact-46f79c339e7a4ea4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/read-metrics.js",
          "sha256": "1d6e8c0adcda57d1dbca8c2cec56914dc166141ef669d96a8d182b0501d079b7",
          "bytes": 1352,
          "artifact_id": "artifact-2276d1e0fe66459f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/read-metrics.js.meta",
          "sha256": "db9931f78fea3d0b2e3d0ace5e88743bf90ecf8f44cb80eaea1e81b0ba3cf58f",
          "bytes": 166,
          "artifact_id": "artifact-2ebd11de383a43b7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/split-sc.js",
          "sha256": "33f2ae12164e594d13558b28be20a37d148bc591ba05fa6a9e7943bb935be8a1",
          "bytes": 4615,
          "artifact_id": "artifact-db17675ae34c450e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/split-sc.js.meta",
          "sha256": "a47cf402b4dc973244e3f1ec096d48996d830ba231862329ec02cd6fd6c9e951",
          "bytes": 166,
          "artifact_id": "artifact-21755e37b2cd4735",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/util.js",
          "sha256": "e49657878099a688efaea330fb913a1d174c4523a8a25ab67d4206ee7aba0baa",
          "bytes": 462,
          "artifact_id": "artifact-854b1b7305de4492",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/util.js.meta",
          "sha256": "249d9c2b8b9d286409fda69b34b5181776739ccf32f543198d71ac1f68193e76",
          "bytes": 166,
          "artifact_id": "artifact-49b173ae85a244eb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/fs.js",
          "sha256": "d28ff7791372fcdf6ab40c1390131d57b256625e7b7288ca5aa7e3bbc6e95d08",
          "bytes": 16218,
          "artifact_id": "artifact-42ea110de780401f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/fs.js.meta",
          "sha256": "292107577c8accc18e53a8d59c72d6b540b4ec08774dffe8d346743d97c6a0e3",
          "bytes": 166,
          "artifact_id": "artifact-809da36eab484fae",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/game-club.js",
          "sha256": "67c3a4032fe27b02e515455858c9cab59d1c2a8142bd7c060d45f135cda16c90",
          "bytes": 2503,
          "artifact_id": "artifact-0125c949bb9a4fe3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/game-club.js.meta",
          "sha256": "f8b97537e9cf0f6e31aec69f4a27f210ee6d98d3754c0a6b7244a3c4e807eaff",
          "bytes": 166,
          "artifact_id": "artifact-bd5c461307314225",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/game-recorder.js",
          "sha256": "7ae459dd5ac89586b8c595b50b1d525312cba0381297132eff0ed821cc9cb8ba",
          "bytes": 3166,
          "artifact_id": "artifact-20499843a459438f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/game-recorder.js.meta",
          "sha256": "17882615008b2e70252bcf7e1bf034b9a2a56767bd3772c294785ccba8217676",
          "bytes": 166,
          "artifact_id": "artifact-e4d43be081524437",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/gyroscope.meta",
          "sha256": "7154db7e2d2edaf31cccf98085e7c0cf47e5ee9dbe0e008499be22e4b77223d5",
          "bytes": 171,
          "artifact_id": "artifact-6d3532392852464b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/gyroscope/index.js",
          "sha256": "a335e1885729ecd6ee5a4aee1fac32822dc23f8c03c40aab5bb89fcbd7f92d12",
          "bytes": 2549,
          "artifact_id": "artifact-e584d64b12f3421f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/gyroscope/index.js.meta",
          "sha256": "eb57009396b8f8272dd8d98a30e9c9df170f189b579cdc5402b6cb62cdbd7063",
          "bytes": 166,
          "artifact_id": "artifact-7cf296de8fb044e3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/index.js",
          "sha256": "e1fd0f0b10d50c6810749fca34240a0048ce25ccb509f34e36fe3dd5c5952880",
          "bytes": 3016,
          "artifact_id": "artifact-2bf57517471d48bc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/index.js.meta",
          "sha256": "d0e82cc4216c3d0444da4fa35fda81334f8f13fa2a8b314cf6e6ed52b7bad754",
          "bytes": 166,
          "artifact_id": "artifact-d6665604e81b448a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/logger.js",
          "sha256": "7d765a2c857a05424d08fc4cca83ba32393558f79fbc46c653a98bf440b03013",
          "bytes": 620,
          "artifact_id": "artifact-9e82b85e47fd4b76",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/logger.js.meta",
          "sha256": "5997ce0a94effab558dc26bb770ba2e380c150081cda9604bef7e19147baedb0",
          "bytes": 166,
          "artifact_id": "artifact-427b2f07ecbf40cb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/mobileKeyboard.meta",
          "sha256": "38c982147109661a8071158f2ff3d06cb7c5b902371e63b25cebf19981c35a3d",
          "bytes": 171,
          "artifact_id": "artifact-c07682c3f7b84191",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/mobileKeyboard/index.js",
          "sha256": "fff32a092621032170bd2ef8f92bf95d0a2607a239634e659e6b9bee6f01be0b",
          "bytes": 4273,
          "artifact_id": "artifact-a71788caed214b1e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/mobileKeyboard/index.js.meta",
          "sha256": "7129f174e5c0aed66dfe98c638105303b6c2bf1f6d2c46b8e7baef3248c0de52",
          "bytes": 166,
          "artifact_id": "artifact-9f777820fa904178",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/module-helper.js",
          "sha256": "5be6623e72561c69479b279dbb423b09e70041c914a465d9ff2837d03cb8e913",
          "bytes": 355,
          "artifact_id": "artifact-0cf3c87d85e14b1f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/module-helper.js.meta",
          "sha256": "8f0b69ef57dc4b8377ca102e7232bb0304751edcfad518a422cbfbebf6e93f9b",
          "bytes": 166,
          "artifact_id": "artifact-a4287bd7a7be45cd",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/open-data.js",
          "sha256": "40848eeddcfaa064c65c269640a438e86ffa244411806a55d7d6311d8cf2b8a9",
          "bytes": 7648,
          "artifact_id": "artifact-c1c1aa1a4f684fc9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/open-data.js.meta",
          "sha256": "cbded351e34211ce1e049e3d7ff693a46eb95bc964229248dc2a571daf0af509",
          "bytes": 166,
          "artifact_id": "artifact-daf78334b58c48ae",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/recorder.js",
          "sha256": "eaba87090f60fb22bb59bf1e82dd16a5aae134b2e9dc08c403f79c61652b51cf",
          "bytes": 4469,
          "artifact_id": "artifact-9849d1fbe3a94f97",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/recorder.js.meta",
          "sha256": "fb7d26652120c89e358a3696a88a7d6ce85d2b02d5a6556ed9a77c4c5b5cc5a7",
          "bytes": 166,
          "artifact_id": "artifact-0c3f76b5a3294194",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/resType.js",
          "sha256": "e2135586c03404792b961ea032e53d7000f552a4b61cb066896d149b13b6d2cf",
          "bytes": 30137,
          "artifact_id": "artifact-b701a86dd9b444ef",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/resType.js.meta",
          "sha256": "d4cc8d4ddb56429c2770dab0c6badad7b8396229bc00c85ef5faa2cd78e63cf7",
          "bytes": 166,
          "artifact_id": "artifact-9509acde384d4004",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/resTypeOther.js",
          "sha256": "af5e7538fad8f6640239322cdfc7a6c5181995551cd7a6772b8311b2df55b247",
          "bytes": 2293,
          "artifact_id": "artifact-ce7f4309f83e4454",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/resTypeOther.js.meta",
          "sha256": "d5c0b5265af1c10094f1b60d269c6a722c3c6f5f4555d8d702bfd57c43529a9c",
          "bytes": 166,
          "artifact_id": "artifact-fdf5ddd887c54573",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/response.js",
          "sha256": "9bf4b44ef758ec8917aac3a1827de70f27bacc881849bf985cafaab313216304",
          "bytes": 1891,
          "artifact_id": "artifact-338428a16cd1461f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/response.js.meta",
          "sha256": "d8f8575dd24894d152c6c4b92813d36923d44f3b98038a2d2e4d471840a606b7",
          "bytes": 166,
          "artifact_id": "artifact-66177747a24b41d0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/sdk.js",
          "sha256": "8377aca08a51213a937cd6fe0b49837dccaa607e1d73dd1c955d7181324811f1",
          "bytes": 17209,
          "artifact_id": "artifact-8cdcd71a408b45e9",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/sdk.js.meta",
          "sha256": "722a2f3ab338e74f87fb7076b2ab71819e05ffb18083fed0bb25e4bcca76c5fa",
          "bytes": 166,
          "artifact_id": "artifact-750d5d7618ca477d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/share.js",
          "sha256": "d97cc99e4cb4d86a2b55c0a0bcc0f4a331cd64a582c24b11c61161843dbf16c4",
          "bytes": 808,
          "artifact_id": "artifact-21f56040421f418b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/share.js.meta",
          "sha256": "a68b248cbb32d311f382571b35f77943b1ff8c47c7d4dae1abbba2b12e56462b",
          "bytes": 166,
          "artifact_id": "artifact-be8bd54e89df427a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/storage.js",
          "sha256": "17bdfc50d2aef3f61ad34a94dadbb3989a96f4fb148713315c464261c6291948",
          "bytes": 4285,
          "artifact_id": "artifact-e6adfbf3b12f4bc8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/storage.js.meta",
          "sha256": "f9b5191f5574146f084d8484fc7f9ff9518729a91b28e79b42c2fb54147eda6a",
          "bytes": 166,
          "artifact_id": "artifact-d2c3eea920bf46d2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/texture.js",
          "sha256": "f353d9867cf476771572b65fe24a4bd99eb026f721ac98f9a37e58eaa4b9c7a0",
          "bytes": 11059,
          "artifact_id": "artifact-97b2e2f169ae476d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/texture.js.meta",
          "sha256": "d25434b89e79464f8388d3452f343e8c970a974ceab8001d545d8ffb099ae40e",
          "bytes": 166,
          "artifact_id": "artifact-6eda229157e24227",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/touch.meta",
          "sha256": "51d54dafa2e9fa3ca98645d1221fd93008b7354c308302f2f61c27d5257937e7",
          "bytes": 171,
          "artifact_id": "artifact-4b10989577b947f4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/touch/index.js",
          "sha256": "a5917a61534de7bc2f5224a9c9402863a264cf6bd2106d5536eb6018299de53f",
          "bytes": 2301,
          "artifact_id": "artifact-5746c0cf8b3f40bf",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/touch/index.js.meta",
          "sha256": "035ba63a5488e2858c9d650ce29f75bc28e86667b8765f83ad18be8c8632bf4c",
          "bytes": 166,
          "artifact_id": "artifact-c88af6b0d3ed4597",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/upload-file.js",
          "sha256": "3dbfd0ea7257b5c9e7191fd27f9783d86ecfec4e7d9220ebc4c9cf8d6ec4c3d4",
          "bytes": 2657,
          "artifact_id": "artifact-421ce11c689744af",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/upload-file.js.meta",
          "sha256": "1ff8a265fc8dd6457c593b7f46d2ef92929acaba862ec51e77ca6c6383c3e65d",
          "bytes": 166,
          "artifact_id": "artifact-ba8cb9d7bf874170",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/userinfo.js",
          "sha256": "8c9373e5f77ee9b64b08e643e81bc45344d2b57e3aa78c582d2c0c53e099f06c",
          "bytes": 2922,
          "artifact_id": "artifact-74db7a5731dc46a3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/userinfo.js.meta",
          "sha256": "f258b5615381df319ea038cb65082df4084ba4e99fa376272c5ee41fed9d17bb",
          "bytes": 166,
          "artifact_id": "artifact-cd7abaa194794a57",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/util.js",
          "sha256": "a54a33bb6894e642d3cd36f79c506c8a827c51bbdd56f61f0e230aaf1523f24f",
          "bytes": 6407,
          "artifact_id": "artifact-015b7603e3844185",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/util.js.meta",
          "sha256": "adbab5870129a9bdd211d2932fdfd614dff1d422f646073b2d2cedf32d2752be",
          "bytes": 166,
          "artifact_id": "artifact-2352ffcc14d24e73",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/utils.js",
          "sha256": "96bfd23dbd18b840fad664c3e1c5263cb11674c4e0cadc7511637ae166a61c32",
          "bytes": 13273,
          "artifact_id": "artifact-9258d1f9ed784f2e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/utils.js.meta",
          "sha256": "e7fff5da726f89f4ff9f49ad51bb1fe870cf7f3de6f16ba23531b88a0974f1c1",
          "bytes": 166,
          "artifact_id": "artifact-c428f9243b974e3a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video.js",
          "sha256": "b2d156cb1b437508d55bf8a32a9096f6d370ddf196f63bf5113d9c4597e2e128",
          "bytes": 2552,
          "artifact_id": "artifact-6c8901ba47544ba1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video.js.meta",
          "sha256": "a1e0ab3bd71cd4d27737fb3c07bfbc143e48875ba4772ccfc47e3a6437381966",
          "bytes": 166,
          "artifact_id": "artifact-4cd98571338e429b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video.meta",
          "sha256": "3c9735c4e88cf8c19158e96f3db50162d30419f8de68fa0c8cd11813926a416e",
          "bytes": 171,
          "artifact_id": "artifact-5c534fdba689424f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video/index.js",
          "sha256": "de109cc788e8b94020aaa9bc48a8fe38565570962a70791f622e2cc9b2381ff8",
          "bytes": 23528,
          "artifact_id": "artifact-3cb6cb4b57d7437b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video/index.js.meta",
          "sha256": "47c892ccea0089815f76ad801cad95b0272d0d16d3e403d24ce8d8fea0b401b1",
          "bytes": 166,
          "artifact_id": "artifact-12d3fc5d9f984571",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/wasmcode.meta",
          "sha256": "d6637adf8e3b24af3b6a35cca86d96af27451065712dbccb79972100898ed1d3",
          "bytes": 171,
          "artifact_id": "artifact-4cc70743eda84dd6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/wasmcode/game.js",
          "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
          "bytes": 0,
          "artifact_id": "artifact-3687d2d80bb443c0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/wasmcode/game.js.meta",
          "sha256": "021e4b46fcab07238973ab63d422c48073158cee27a75bbd1505b923445dc8f0",
          "bytes": 166,
          "artifact_id": "artifact-cac5a79059204fbe",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/weapp-adapter.js",
          "sha256": "2823cce28ea12bcdc8739eb174bfc85ca85007d540c275630913b2c3a8931a45",
          "bytes": 74701,
          "artifact_id": "artifact-321cb4b8c27b4ae8",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/weapp-adapter.js.meta",
          "sha256": "ce789532602506fc3ef1f847f57332cc8eb7951886debd5682a942ecc3d97c78",
          "bytes": 166,
          "artifact_id": "artifact-764ffd236e5e46de",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/workers.meta",
          "sha256": "e50b024111ef3967f66b5487e12d8938de824ffacf176ac73c8708b006b5f7ee",
          "bytes": 171,
          "artifact_id": "artifact-0902a9d4a24241f4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/workers/response.meta",
          "sha256": "a5583230b037d8807fd3a2d224a28654fcaa2ca336ca3c798acd46e1e0b1d052",
          "bytes": 171,
          "artifact_id": "artifact-61c1c3e3bb454463",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/workers/response/index.js",
          "sha256": "095fe2b7d7252ccd9bd5da0de81143f730a50aea25b1ceb32d796eca1ef1f028",
          "bytes": 2203,
          "artifact_id": "artifact-fb103113ee994c3f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/workers/response/index.js.meta",
          "sha256": "ba096d160f11370538b687763cbc989389efe82d0eb76943495ddc40a57b0311",
          "bytes": 166,
          "artifact_id": "artifact-02c3c26729094c5e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates.meta",
          "sha256": "3b111fd685d2b82c0f8c1e6b04f4ff101658d03c900c2d546bd780f89ca88fb0",
          "bytes": 172,
          "artifact_id": "artifact-987612ac4b8c4e20",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate.meta",
          "sha256": "81b40534f21d83337d244ac7220909f978cb52f10d2db2d4f1cfe98ee262029f",
          "bytes": 172,
          "artifact_id": "artifact-82f91e94f0c84c49",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate/index.html",
          "sha256": "4e2af244554a47481d204fbbc6e4f9fb70769d82a3d3624d81da27b272344068",
          "bytes": 16752,
          "artifact_id": "artifact-a309041dd0724b71",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate/index.html.meta",
          "sha256": "bee81dc01cd6c293f6d2a1f1f7eee3ee1f44e3db5c20364dcb7df4a964ec969c",
          "bytes": 155,
          "artifact_id": "artifact-7690708c4b7b4c2e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate/thumbnail.png",
          "sha256": "3c3be7555ed97f7e90e62bcabbb189d3068593405864ce5645c090781c515875",
          "bytes": 8219,
          "artifact_id": "artifact-77e3c4e886634dd0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate/thumbnail.png.meta",
          "sha256": "68fdd0d9613361b7409814e1d130207cc45de78c2d0d2435549655609340fc7a",
          "bytes": 155,
          "artifact_id": "artifact-d2b09a72a1ab4399",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020.meta",
          "sha256": "342136c6857397a1ebf85cd5c299cbda861d90063ff75efc02422fa27c2a815d",
          "bytes": 172,
          "artifact_id": "artifact-40d039f21b924b1d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020/index.html",
          "sha256": "94c09b1d9eabf083f49ec790c5036850e957b8fd004892a88df4e50c1f3e984c",
          "bytes": 19291,
          "artifact_id": "artifact-71bb9395405e45de",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020/index.html.meta",
          "sha256": "7f2afe373427316f9b4199a646bdb13637d7e97577ad72dd0a7c8ecf52d56932",
          "bytes": 155,
          "artifact_id": "artifact-82953154c98a4293",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020/thumbnail.png",
          "sha256": "09c27d0e20d35546af277a39f65a45d7ff4da7ae82ae904c983dbbd632d61371",
          "bytes": 8266,
          "artifact_id": "artifact-40c28aacb2014bcb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020/thumbnail.png.meta",
          "sha256": "6089a11f61ba0ec16bda8ad33f2dfcaf147dc0e24fd56fd398fb3a583eec7f91",
          "bytes": 155,
          "artifact_id": "artifact-70ee733e07cb4454",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022.meta",
          "sha256": "38870b98fb27d70befa0058851cdc9bba66df985bf17b8d7d05573f88139d722",
          "bytes": 172,
          "artifact_id": "artifact-af2b2637a7f04969",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData.meta",
          "sha256": "eb831fdd083993eb86f98e16ea9bf58c0a729fe55055f96ba4b8bc287bd44d7e",
          "bytes": 172,
          "artifact_id": "artifact-442bcf5d8bec4f58",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/MemoryProfiler.png",
          "sha256": "0ce87be20d0c24ab6cb73b57de2004da261cc2a641e69319c6ae3fe11fc85483",
          "bytes": 665,
          "artifact_id": "artifact-35f84eb1e5ef4ee6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/MemoryProfiler.png.meta",
          "sha256": "f89e227971148f8f67981a97f0a014ab534e35d0fa77a7b5b3fa8e1ed22a57c1",
          "bytes": 3334,
          "artifact_id": "artifact-2ed23373bc0e493b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/favicon.ico",
          "sha256": "9c13beb90ee8f70580d52a21d5233970d1c89e71e4a34a462c22610941c3c77f",
          "bytes": 2305,
          "artifact_id": "artifact-8c29f57d7f364007",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/favicon.ico.meta",
          "sha256": "7c792d1a5eecf414634a628c990e13d9e92a84fd9470c2c83b0701888e758643",
          "bytes": 155,
          "artifact_id": "artifact-517972c53f754748",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/fullscreen-button.png",
          "sha256": "21221581673a54b8139d408d4a3f8d2b879e86827d4b6fc53b995ff7a99ee3e9",
          "bytes": 175,
          "artifact_id": "artifact-82699f7482294f44",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/fullscreen-button.png.meta",
          "sha256": "fe7e7dc1f2995f49daeae6b12309549724d7f9cfcb1efa69c862f5ac7c7e3855",
          "bytes": 3334,
          "artifact_id": "artifact-80860fe7988b4231",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-empty-dark.png",
          "sha256": "bbee7131afe8a3365906240d89184dc86234c119467f390bc4bc6802328fdb4d",
          "bytes": 96,
          "artifact_id": "artifact-c0f0b418b4e14f7c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-empty-dark.png.meta",
          "sha256": "ee0e67498ec155f48a9a78e45aab84ceb74273ad6b4cea923590ab089f199af2",
          "bytes": 3334,
          "artifact_id": "artifact-e19aa05a7a3c4938",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-empty-light.png",
          "sha256": "143696c060ad43d6c30f19ff5f49000927a139a2a1e4c9f45fafaa2b90d7d2be",
          "bytes": 109,
          "artifact_id": "artifact-7f38cbbcbeb44a25",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-empty-light.png.meta",
          "sha256": "7cf7effc63b36c60eefa8d48de2c61d21c72879843bc1a7d2af95f8dd7b6af60",
          "bytes": 3334,
          "artifact_id": "artifact-2356a41daa9543ef",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-full-dark.png",
          "sha256": "3306a6244dcb3926fca38a28e3ced589df8ff1beed955eb17c0bbf01c918bc62",
          "bytes": 74,
          "artifact_id": "artifact-177ba54a814042b4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-full-dark.png.meta",
          "sha256": "4a2e9ede8eabf214a83a7c7646d97ad15d0a950dd272b5feeb15a9d4ba07b20c",
          "bytes": 3334,
          "artifact_id": "artifact-bc413f3f46c04b69",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-full-light.png",
          "sha256": "12395ea785480c5cdf12fade6e6cbf49666d5bb1cef7240c113dbcba6bdc5c87",
          "bytes": 84,
          "artifact_id": "artifact-9a0ca685d1e144a4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-full-light.png.meta",
          "sha256": "5f8b66635991553bc6af39ca26f2890ddd6e32ea8825746a9bf18b2bf400e7d8",
          "bytes": 3334,
          "artifact_id": "artifact-273305f78edd4367",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/style.css",
          "sha256": "f19641da9ff37a7480a77c76491ce539b3fd8126e85f15199bd888f5bb02d1fe",
          "bytes": 1556,
          "artifact_id": "artifact-b7b11f30fbe349c1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/style.css.meta",
          "sha256": "f9fed6966b4c28458a148adae306f6630e2e7ad38cba15d9572e1e81d6697ea1",
          "bytes": 155,
          "artifact_id": "artifact-1838e97b04f048cf",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/unity-logo-dark.png",
          "sha256": "c1b72d26c096487dabc948b54bc203f8dac7ed4e3f5733918798e858acb4b159",
          "bytes": 3042,
          "artifact_id": "artifact-a94752303b934a7d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/unity-logo-dark.png.meta",
          "sha256": "bcacf156cf201d9b856eab6c51e8d8b3964716309826ccf6349ea0edf22d6956",
          "bytes": 3334,
          "artifact_id": "artifact-299c4d3c2c354af3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/unity-logo-light.png",
          "sha256": "002990aea0d946833cadc1519d5b7e50a4570bd537a0517dd79a59d3eec84da7",
          "bytes": 3077,
          "artifact_id": "artifact-b9a009bb5cc645a1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/unity-logo-light.png.meta",
          "sha256": "2ba92b3d7a0accf4e181d5df73e4963cd075d0faf7ee35640e3bed5f77958742",
          "bytes": 3334,
          "artifact_id": "artifact-2244f0e4001a4adc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/webgl-logo.png",
          "sha256": "b30c3af2a4538c6edf5f2411953760641dfa257f2a4cc5b88d671aa243b1f12f",
          "bytes": 2947,
          "artifact_id": "artifact-8adc79c0b4a24e9e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/webgl-logo.png.meta",
          "sha256": "31d06916ed93d8fe2951c664314bcdfa83397d0877714f6573fbbd4a34bd0ca8",
          "bytes": 3334,
          "artifact_id": "artifact-172a2eab571a4c68",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/webmemd-icon.png",
          "sha256": "c56f5494640cedda273aad38d58e6ab9da625d5b3f4d875c0ff92c6192a569c2",
          "bytes": 1670,
          "artifact_id": "artifact-4cfa911495f64dd7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/webmemd-icon.png.meta",
          "sha256": "5cdc06843328841d68050b3588882a8ace7d2df2337d401d90949561449242ec",
          "bytes": 3334,
          "artifact_id": "artifact-850e04fcb99c4950",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/index.html",
          "sha256": "0c155598460924ac9b0e7dd10057feae83959789b7defe08bd3946a97c576969",
          "bytes": 22887,
          "artifact_id": "artifact-7635b785fdd94cbc",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/index.html.meta",
          "sha256": "3b11000012e99eb759530cbac2548440b288e713b3534c0d025619f1a77931a7",
          "bytes": 158,
          "artifact_id": "artifact-c7343e9fe8cf40fe",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/thumbnail.png",
          "sha256": "e5b442bd5fe5283a1a0283408fe87df270e58d7b52171e1ffb7042350b96eaf0",
          "bytes": 5256,
          "artifact_id": "artifact-a90ad5bf577742e6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/thumbnail.png.meta",
          "sha256": "c77a8f81092b51df09cab71ad368e5027b561db3da4b35b3829eb596763473de",
          "bytes": 3334,
          "artifact_id": "artifact-e6e47c433ec54435",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ.meta",
          "sha256": "084b0b107f4cad60103f37d376d215d55a7939307c706372e34ea0c009fea94b",
          "bytes": 172,
          "artifact_id": "artifact-d74e4a0f4684452b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData.meta",
          "sha256": "c890a265431594103bd9a6de11ec58a94fe9fca188c280baf85268249bc79f3f",
          "bytes": 172,
          "artifact_id": "artifact-337444de60044185",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/MemoryProfiler.png",
          "sha256": "0ce87be20d0c24ab6cb73b57de2004da261cc2a641e69319c6ae3fe11fc85483",
          "bytes": 665,
          "artifact_id": "artifact-1e9e0ac523704a77",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/MemoryProfiler.png.meta",
          "sha256": "d091993bb2c30659d67c3f2211be6a29e5a33c0c28acacfa6d2f93de9f03bc49",
          "bytes": 155,
          "artifact_id": "artifact-66b801865158441b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/favicon.ico",
          "sha256": "9c13beb90ee8f70580d52a21d5233970d1c89e71e4a34a462c22610941c3c77f",
          "bytes": 2305,
          "artifact_id": "artifact-370f2e8443604659",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/favicon.ico.meta",
          "sha256": "7d8cea93a62bb9cf8047ad24afbe6ef1de135c6dad36946ce9a8188a13e789b4",
          "bytes": 155,
          "artifact_id": "artifact-b01ce47f79854ff0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/fullscreen-button.png",
          "sha256": "21221581673a54b8139d408d4a3f8d2b879e86827d4b6fc53b995ff7a99ee3e9",
          "bytes": 175,
          "artifact_id": "artifact-7ed29055b5274f51",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/fullscreen-button.png.meta",
          "sha256": "e0586365494a832af44d0fe6e5163018b63e2996a2850a9fb1a970d768402b72",
          "bytes": 155,
          "artifact_id": "artifact-afbfdb8f3f4c4486",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-empty-dark.png",
          "sha256": "bbee7131afe8a3365906240d89184dc86234c119467f390bc4bc6802328fdb4d",
          "bytes": 96,
          "artifact_id": "artifact-c537511e53e54677",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-empty-dark.png.meta",
          "sha256": "d1992e2c30fba633db9accc666d533caa6addb246ca77b2ea797a653ab8f60c1",
          "bytes": 155,
          "artifact_id": "artifact-c7c3095886d74d98",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-empty-light.png",
          "sha256": "143696c060ad43d6c30f19ff5f49000927a139a2a1e4c9f45fafaa2b90d7d2be",
          "bytes": 109,
          "artifact_id": "artifact-2b854c73c84f45dd",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-empty-light.png.meta",
          "sha256": "e0b1f4a228553408e0ac073f0fc69083d89456317256cca633d41ec254903e58",
          "bytes": 155,
          "artifact_id": "artifact-0e55a8bb03ab4672",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-full-dark.png",
          "sha256": "3306a6244dcb3926fca38a28e3ced589df8ff1beed955eb17c0bbf01c918bc62",
          "bytes": 74,
          "artifact_id": "artifact-f34e9ea8e9724910",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-full-dark.png.meta",
          "sha256": "fe52a356805726110dbfef8a1cd17daa75577a3d0a7c0d1bd1696614deae04f3",
          "bytes": 155,
          "artifact_id": "artifact-76241036fba44ced",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-full-light.png",
          "sha256": "12395ea785480c5cdf12fade6e6cbf49666d5bb1cef7240c113dbcba6bdc5c87",
          "bytes": 84,
          "artifact_id": "artifact-a46ff628be3f4ef6",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-full-light.png.meta",
          "sha256": "69a4f30aa8ec6c100d12fa510a27f05a576bf6c0bab022ce6c8d71695c15d972",
          "bytes": 155,
          "artifact_id": "artifact-de359be1a1934fc4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/style.css",
          "sha256": "e8f921fba47cd09e8e752c1dd99305feebfed8f0f04d69e61877b45c6eed6414",
          "bytes": 1468,
          "artifact_id": "artifact-7c77fa7a88854b31",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/style.css.meta",
          "sha256": "adf798b4f73d8190a893d441aa5a2baa96c95efd17fadc942e814c1862265510",
          "bytes": 155,
          "artifact_id": "artifact-1699505a44674ee2",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/tuanjie-logo-dark.png",
          "sha256": "c1b72d26c096487dabc948b54bc203f8dac7ed4e3f5733918798e858acb4b159",
          "bytes": 3042,
          "artifact_id": "artifact-6a9fc84f52ca42cf",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/tuanjie-logo-dark.png.meta",
          "sha256": "33b5024a6e7d42304c5877af922814163731fabf6abcc42ed28eaab0634ea22a",
          "bytes": 155,
          "artifact_id": "artifact-7ffac195d2ad4772",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/tuanjie-logo-light.png",
          "sha256": "002990aea0d946833cadc1519d5b7e50a4570bd537a0517dd79a59d3eec84da7",
          "bytes": 3077,
          "artifact_id": "artifact-84a6a28c55534018",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/tuanjie-logo-light.png.meta",
          "sha256": "2d5c57889432e065486f8ef9d81a06873b5bc904b636f72ff4959e51f4898214",
          "bytes": 155,
          "artifact_id": "artifact-dc4bb8bbf6a943ab",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/webgl-logo.png",
          "sha256": "b30c3af2a4538c6edf5f2411953760641dfa257f2a4cc5b88d671aa243b1f12f",
          "bytes": 2947,
          "artifact_id": "artifact-41f4eec761ae4fa3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/webgl-logo.png.meta",
          "sha256": "32c26d348e7e641711afc7d49b3c6f8c53a1fb0f2e981afd5b5e98cb10d09578",
          "bytes": 155,
          "artifact_id": "artifact-5576d663b4e044c7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/webmemd-icon.png",
          "sha256": "c56f5494640cedda273aad38d58e6ab9da625d5b3f4d875c0ff92c6192a569c2",
          "bytes": 1670,
          "artifact_id": "artifact-8e7e9b26b1f54a19",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/webmemd-icon.png.meta",
          "sha256": "9d6843df135c0fd74475ba6c3f430e71f3e4a79fa82dcb2a9ba77f538686ddfd",
          "bytes": 155,
          "artifact_id": "artifact-ad6219c45f5140c7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/index.html",
          "sha256": "69d17b79e0303ad0b26f08860b12f621ccec8e82ebecdbe7c56cf88250bb7c39",
          "bytes": 30535,
          "artifact_id": "artifact-a342687717b547a7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/index.html.meta",
          "sha256": "32c31e397375a74dfd2805ec309f909ea7d14b6ce18f5d5ef380f97afe94375e",
          "bytes": 155,
          "artifact_id": "artifact-f66604d81599407c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/mainifest.json",
          "sha256": "06d3eb3d7352f5331ebf2b28f6f5c046d04de62fd5249655c79d851a0781fc3a",
          "bytes": 128,
          "artifact_id": "artifact-de140e9a688d4af0",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/mainifest.json.meta",
          "sha256": "eac96b27b59fc64197ca4cfb3d52cba4b27d945e37f6c1138903d3ff2b78798b",
          "bytes": 158,
          "artifact_id": "artifact-ee6caa601555447a",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/package.json",
          "sha256": "4ccaa7aadc4495a286df7cb57b2c2734803fdef6b65d91cf029466e5ff0873fe",
          "bytes": 228,
          "artifact_id": "artifact-526fe21003084c0b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/com.qq.weixin.minigame/package.json.meta",
          "sha256": "c6bca6ccc02118341a4a1e79089a210b00a8fd99cf4a8af0b00b41238152584e",
          "bytes": 158,
          "artifact_id": "artifact-89a87fc1abe4415e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/packages-lock.json",
          "sha256": "a466220c68486629a1c319978bb19cf6ddab7fafdddba36c0a563516e6e4b593",
          "bytes": 958,
          "artifact_id": "artifact-f03680cfec7a4882",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/AudioManager.asset",
          "sha256": "9a554916fe138ebcad8443ee9b079363ecc72795c39d0e80f5f7ca99e2c6f5e7",
          "bytes": 443,
          "artifact_id": "artifact-a1a4459f4c3f410c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/ClusterInputManager.asset",
          "sha256": "40e65dd214bf254955d2c8829ca8ce2130fd3510485560ce012e9a1ddd93e623",
          "bytes": 114,
          "artifact_id": "artifact-ecc6d7015557430f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/DynamicsManager.asset",
          "sha256": "67900c2934d84fa99a3cbc8701e931840f3f008781fa8d7b842a10794f082e65",
          "bytes": 1287,
          "artifact_id": "artifact-68db6fc4f25f47cb",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/EditorSettings.asset",
          "sha256": "1a8e709ae6b71c53bc5d47100501ed71b088dd593a4239f78ac79864e724604d",
          "bytes": 1536,
          "artifact_id": "artifact-15fc4efd88c54f15",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/GraphicsSettings.asset",
          "sha256": "fbf0856b7693639a5388ae693a455baaad354c1d8dc548601de3dc61c4ab12c3",
          "bytes": 2372,
          "artifact_id": "artifact-4df1695add254518",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/InputManager.asset",
          "sha256": "d0f454ddb880912abcae92e3e12957a7446786ba89e24937e019ff3bbc0d4300",
          "bytes": 5816,
          "artifact_id": "artifact-e4546ac4aa664b5c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/MemorySettings.asset",
          "sha256": "1e8379a461b10ae09f3f696911088c16875d63e87ed5f2d95df9e1892703f9fd",
          "bytes": 1192,
          "artifact_id": "artifact-6dfcb6c17069483b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/MultiplayerManager.asset",
          "sha256": "aba89fd1a1ddad182727ad82727e536d27372e90328a8196da2a4560398983dd",
          "bytes": 157,
          "artifact_id": "artifact-bb2ec96453e74aee",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/NavMeshAreas.asset",
          "sha256": "7ebbf8f53b8033104e1878b95ca07a28d0954092d81c220f14196a42f651e658",
          "bytes": 1361,
          "artifact_id": "artifact-7660c716c7404b58",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/Physics2DSettings.asset",
          "sha256": "9b78fdd0bac0f486c052ca65ddc0e5022d322b12874931a49456d407c5bbfc00",
          "bytes": 1820,
          "artifact_id": "artifact-50164b029a1f4524",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/PresetManager.asset",
          "sha256": "29c125923fe6e29f90190d4fce1c82d660e7051546e2c9d02908d18c9cf2436c",
          "bytes": 146,
          "artifact_id": "artifact-12877efa5ce340be",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/QualitySettings.asset",
          "sha256": "5e5f38a7cb2f7d097614ae6cb7ba8d2b95ffd40d73a97230957fbd599b3af6b0",
          "bytes": 9638,
          "artifact_id": "artifact-fcb8869d44a4499f",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/TagManager.asset",
          "sha256": "da4b5eefc927f8b91bcf9e56a186d2b852f49d9cbe91d466638d26c79d6a83ce",
          "bytes": 411,
          "artifact_id": "artifact-c6938d2130ff4d00",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/TimeManager.asset",
          "sha256": "e6d0d31782b4896159d2f7249b1845004a7a9f4eb05bc952a8c64bc72cb34448",
          "bytes": 282,
          "artifact_id": "artifact-2c85018020cf4321",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/UnityConnectSettings.asset",
          "sha256": "d6034acbef6c9e3179866486c31eab81fcbe5e7ed350ce71e0fa2b1449b77109",
          "bytes": 943,
          "artifact_id": "artifact-b79ca1caec3b461d",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/VFXManager.asset",
          "sha256": "9378043da38a17f2198d52a10c17789161d30e3285a387ca3687a71551c32448",
          "bytes": 492,
          "artifact_id": "artifact-7d2ae3ce71054c34",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/VersionControlSettings.asset",
          "sha256": "e199cdfa6a8f2bf33cfd47e549198f13e3eb2a5099e908fdb4e0f14d939ccc6b",
          "bytes": 172,
          "artifact_id": "artifact-c54d1b7f79854d6c",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "build/wechat/IMPLEMENTATION.md",
          "sha256": "291284cd16a71a13c67169406512855f23a3403519d2b563aea239e535a67d7f",
          "bytes": 2374,
          "artifact_id": "artifact-d2d3a56acb5942f3",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "build/wechat/compile-01.log",
          "sha256": "137876729c44781881151875f80c0156792d7d871ae3c23841c70c595ccbe34f",
          "bytes": 81387,
          "artifact_id": "artifact-26960558160a4d8b",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "build/wechat/compile-02.log",
          "sha256": "2c7b0056480e4921270d9fc0adc874c4b90d4fb94dfb5880853b60cdd04d296d",
          "bytes": 122938,
          "artifact_id": "artifact-c9ecfb53f1e34f72",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "build/wechat/compile-03-player.log",
          "sha256": "a4c0932c7edb4cfd28633895b73b9c818acceb392dd5d48f0080a467a425f89b",
          "bytes": 48895,
          "artifact_id": "artifact-a1c9ccb92d634f2e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "build/wechat/compile-04-player.log",
          "sha256": "25513bbede3d7d2c85fbc00bb6adc67d6868bc3ecf4d9ecd7b02a7929806781d",
          "bytes": 31141,
          "artifact_id": "artifact-62413717679b4ce4",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "build/wechat/compile-05-final.log",
          "sha256": "4b33244c75c19659ceb11cce74b2f624de2ab81753951fbffea3837a7db32c1a",
          "bytes": 21033,
          "artifact_id": "artifact-05ee4f2c72be4190",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Bootstrap/Bootstrap.cs",
          "sha256": "a369522573b14e049cc64bda06323b459842187e5e2c1622548c44c57767c274",
          "bytes": 3397,
          "artifact_id": "artifact-44b187dfa2df49ba",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Bootstrap/HotpotSort.Bootstrap.asmdef",
          "sha256": "a9ad9c449852fa61049a7c61850bffe6ca1efe310bf3b8d0e8d5d2a482ed1a83",
          "bytes": 203,
          "artifact_id": "artifact-8dd002afd88445f1",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/Packages/manifest.json",
          "sha256": "cd760c2bb508248c8f565fbf592ddcb88fad226cb8a0e2f945c3f1eb6c89229b",
          "bytes": 197,
          "artifact_id": "artifact-f88d26eb8a844371",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/ProjectSettings.asset",
          "sha256": "70a165ceef3c3b94e9396551093c8350986da16f7ac055af625b87fc8759a41e",
          "bytes": 20082,
          "artifact_id": "artifact-2aca2554343f40f7",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        },
        {
          "path": "Unity/ProjectSettings/ProjectVersion.txt",
          "sha256": "82ecb9975adc9bf786c746365a292ab5a7fe477f0f3fbac35326f5ebb324839f",
          "bytes": 85,
          "artifact_id": "artifact-667914747c42491e",
          "kind": "file",
          "run_id": "run-a7aef77a25184b78",
          "task_revision": 3
        }
      ]
    }
  },
  "history": [
    {
      "at": "2026-09-20T09:12:47.741163+00:00",
      "event": "created"
    },
    {
      "at": "2026-09-20T09:14:54.125180+00:00",
      "event": "contract_changed",
      "revision": 2,
      "decision": "2026-09-20 用户指令‘实行task3’，并回复‘同意’采用默认方案",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:14:59.888075+00:00",
      "event": "dispatched",
      "run_id": "run-d7945ff46a394a21",
      "step": "designer"
    },
    {
      "at": "2026-09-20T09:17:26.556115+00:00",
      "event": "accepted",
      "run_id": "run-d7945ff46a394a21"
    },
    {
      "at": "2026-09-20T09:17:32.651133+00:00",
      "event": "approved",
      "scopes": [
        "build"
      ],
      "decision": "2026-09-20 用户指令‘实行task3’，并回复‘同意’采用已列明默认方案"
    },
    {
      "at": "2026-09-20T09:17:38.551943+00:00",
      "event": "dispatched",
      "run_id": "run-93c8465cef034625",
      "step": "code"
    },
    {
      "at": "2026-09-20T09:21:51.944550+00:00",
      "event": "blocked",
      "reason": "官方 SDK commit a09d4b29daa1dd8358b09b5b5639554ab08cfdc2 的 Editor/WxWasmSDKEditor.asmdef 无条件引用 Unity.InstantGame.Editor，但 package.json dependencies 为空，当前 Unity manifest 也未提供该程序集。需确定该依赖在 Unity 2022.3 中的官方来源及兼容版本，才能继续固定工具链。"
    },
    {
      "at": "2026-09-20T09:21:57.337636+00:00",
      "event": "host_stop_recorded",
      "run_id": "run-93c8465cef034625",
      "evidence": "Code Builder 已返回 NEEDS_CLARIFICATION 并确认停止写入；宿主任务已完成，无在途进程"
    },
    {
      "at": "2026-09-20T09:24:45.506533+00:00",
      "event": "contract_changed",
      "revision": 3,
      "decision": "2026-09-20 用户明确回复‘实行’，授权目标引擎迁移至 Unity 6000.0.26f1 并重新核对/修复微信 SDK 兼容性",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:24:45.721869+00:00",
      "event": "resumed",
      "decision": "用户已授权 Unity 6 迁移；旧 Builder run 已停止并登记，按修订合同使用新 run 恢复"
    },
    {
      "at": "2026-09-20T09:24:50.906790+00:00",
      "event": "dispatched",
      "run_id": "run-3fbb0d66055346ba",
      "step": "designer"
    },
    {
      "at": "2026-09-20T09:29:36.570729+00:00",
      "event": "accepted",
      "run_id": "run-3fbb0d66055346ba"
    },
    {
      "at": "2026-09-20T09:29:36.801536+00:00",
      "event": "approved",
      "scopes": [
        "build"
      ],
      "decision": "2026-09-20 用户回复‘实行’，授权 Unity 6000.0.26f1 迁移及针对官方微信 SDK 的可追踪兼容补丁与最小编译诊断"
    },
    {
      "at": "2026-09-20T09:29:37.037337+00:00",
      "event": "dispatched",
      "run_id": "run-a7aef77a25184b78",
      "step": "code"
    },
    {
      "at": "2026-09-20T09:37:32.471667+00:00",
      "event": "accepted",
      "run_id": "run-a7aef77a25184b78"
    }
  ],
  "task_revision": 3,
  "status": "READY_FOR_RELEASE",
  "contract": {
    "goal": "使用已安装的 Unity 6000.0.26f1 及 WebGL Build Support，让 Daily 游戏在微信小游戏环境中以唯一启动入口创建正确日期的会话，支持暂停恢复、同日重试、真实诊断存储和最终模块组合；不包含上传、提审、发布或体验版激活。",
    "qa_intent": [
      {
        "id": "QI-001",
        "text": "Asia/Shanghai 日界线生成正确 ChallengeId；可信时间优先，不可用时设备回退且如实记录 timeSource/fallbackReason；活动局跨午夜保留原挑战，退出重进才切换当天。",
        "source": "Daily SPEC §6.1/§25；2026-09-20 用户同意默认方案"
      },
      {
        "id": "QI-002",
        "text": "资源、配置和日期就绪后才初始化；无效输入或平台失败明确报错；任何时刻只有一个权威会话。",
        "source": "Daily SPEC §7.1-§7.2；TASK-003 草案 QI-002"
      },
      {
        "id": "QI-003",
        "text": "同日重试保持原 ChallengeId/ContentVersion，使用全新会话实例并递增 retryIndex，清理旧逻辑、动画、物理、统计和过期回调。",
        "source": "Daily SPEC DET 04/§6.4"
      },
      {
        "id": "QI-004",
        "text": "用户暂停与微信前后台原因分开记录，重复回调幂等；暂停不接受游戏点击、不累计活动用时，且不得复活未就绪或终局会话。",
        "source": "Daily SPEC §7.1；2026-09-20 用户同意默认方案"
      },
      {
        "id": "QI-005",
        "text": "向视图交付真实结果字段、用时和失败/异常原因；Aborted 不包装为玩家 Failed，不新增星级段位。",
        "source": "Daily SPEC §18.3"
      },
      {
        "id": "QI-006",
        "text": "版本化诊断/回放包在目标端本地存储的能力、位置、成功与错误都如实返回；未配置远程上报时不得声称上传成功。",
        "source": "Daily SPEC §15/§17-§18；2026-09-20 用户同意本地保存"
      },
      {
        "id": "QI-007",
        "text": "固定候选上支持 Windows、Android和微信目标黄金回放接入，不以编辑器或替身结果冒充目标端结论。",
        "source": "Daily SPEC DET 02/§20.3"
      },
      {
        "id": "QI-008",
        "text": "版本阶段在真实低端与主流设备上保留启动、触控、前后台、失败和重试的人工事实证据。",
        "source": "Daily SPEC §20.4"
      },
      {
        "id": "QI-009",
        "text": "内容、算法、配置、平台和构建身份可观察；旧日期回放路由到匹配实现，不用当前算法覆盖全部历史。",
        "source": "Daily SPEC §25"
      },
      {
        "id": "QI-010",
        "text": "运行数据、缓存、日志与构建输出真实消费独立 RuntimePaths；生产 Boot 只组合真实 A/B 工厂，开发替身不得充当游戏通过证据。",
        "source": "AGENTS.md §4；K0 baseline 4639e070f78d520609d3ae8256d87e202d75cde3"
      }
    ],
    "technical_design_required": true,
    "visual_impact": "none",
    "needs_code": true,
    "needs_art": false,
    "owners": {
      "code_builder": [
        "Unity/Assets/HotpotSort/Runtime/Session",
        "Unity/Assets/HotpotSort/Runtime/Platform/WeChat",
        "Unity/Assets/HotpotSort/Runtime/Bootstrap",
        "Unity/Assets/HotpotSort/Scenes/Boot.unity",
        "Unity/Assets/HotpotSort/Scenes/Boot.unity.meta",
        "Unity/Assets/HotpotSort/Plugins/WeChat",
        "Unity/Assets/HotpotSort/Editor/WeChatBuild",
        "Unity/Packages",
        "Unity/ProjectSettings",
        "build/wechat"
      ],
      "design_art_agent": []
    },
    "references": [
      {
        "path": "docs/task-drafts/TASK-003-wechat-session.md",
        "sha256": "39c47de7bd59714e887c93a20eed2f5266825402aee4a1ecf01c8e28f7d865d7",
        "bytes": 29232
      },
      {
        "path": "docs/DAILY_UNITY_WECHAT_THREE_PARALLEL_TASKS.md",
        "sha256": "dc1d3e902e37762ab66befc17d341d8d356383c80bef6cc19b7e360e9a3f1d29",
        "bytes": 19284
      },
      {
        "path": "Unity/K0_BASELINE.md",
        "sha256": "3b4d9634336cb51b900dbf7586a2965184c057cd0c3d5412eda42628f87e4be3",
        "bytes": 3785
      }
    ],
    "dependencies": [],
    "shared_touchpoints": [
      {
        "resource": "Unity/Assets/HotpotSort/Contracts",
        "region": "All public DTOs and interfaces",
        "coordinator": "PM",
        "resolution": "Read-only in TASK-003; changes require a separately fixed single-writer K0 revision."
      },
      {
        "resource": "Unity/Assets/HotpotSort/Scenes/Gameplay.unity and player UI",
        "region": "All visible layout and bindings",
        "coordinator": "PM",
        "resolution": "TASK-002 owns the scene and layout; TASK-003 supplies lifecycle and viewport facts only."
      },
      {
        "resource": "Unity/Assets/HotpotSort/Scenes/Boot.unity, Unity/Packages and Unity/ProjectSettings",
        "region": "Production entry and platform configuration",
        "coordinator": "TASK-003 PM",
        "resolution": "TASK-003 is the sole writer and performs final sequential composition from fixed A/B commits."
      }
    ],
    "interface_map": [
      {
        "interface": "ChallengeContext",
        "source": "K0 HotpotSort.Contracts",
        "called": false
      },
      {
        "interface": "IGameSessionFactory/IGameSession",
        "source": "K0 HotpotSort.Contracts",
        "called": false
      },
      {
        "interface": "IGameViewFactory/IGameView",
        "source": "K0 HotpotSort.Contracts",
        "called": false
      },
      {
        "interface": "IPlatformLifecycleAdapter",
        "source": "Designer pre-K0 requirement; Builder to implement against fixed WXSDK",
        "called": false
      },
      {
        "interface": "ITimeProvider/IMonotonicClock",
        "source": "Designer pre-K0 requirement; Builder to implement",
        "called": false
      },
      {
        "interface": "IDiagnosticSink",
        "source": "K0 HotpotSort.Contracts",
        "called": false
      },
      {
        "interface": "RuntimePaths",
        "source": "K0 HotpotSort.Contracts",
        "called": false
      },
      {
        "interface": "Unity/WeChat toolchain",
        "source": "2026-09-20 user-authorized migration: Unity 6000.0.26f1 at D:/GameDev/Tools/Unity/6000.0.26f1/Editor with WebGLSupport; WeChat DevTools 2.02.2608070 at C:/Program Files (x86)/Tencent/微信web开发者工具. Official WXSDK and missing InstantGame assembly compatibility must be re-established against Unity 6 before binding.",
        "called": false
      }
    ],
    "asset_contract": [],
    "runtime_isolation": {
      "status": "unconfigured",
      "required": "TASK-003-specific data/cache/log roots and build/wechat output must be consumed by runtime/build adapters before concurrent writers are run"
    },
    "generation_budget": {
      "limit": 0,
      "decision": "No art or image generation is required for this code-only task."
    }
  },
  "approvals": {
    "build": {
      "task_revision": 3,
      "contract_digest": "a41b523b690644f19f20c1c8bde89c4aa741c1076f63f455f4b3b43c03fa9836",
      "decision": "2026-09-20 用户回复‘实行’，授权 Unity 6000.0.26f1 迁移及针对官方微信 SDK 的可追踪兼容补丁与最小编译诊断",
      "artifacts": []
    }
  },
  "artifacts": {
    "artifact-db7aac2939254c90": {
      "path": "Unity/Assets/HotpotSort/Editor/WeChatBuild/HotpotSort.WeChatBuild.asmdef",
      "sha256": "98c3995ad2673a099b3e494c1e51789edb96f388d934baa41cd3aa337d1de830",
      "bytes": 184,
      "artifact_id": "artifact-db7aac2939254c90",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-210f7b2058974777": {
      "path": "Unity/Assets/HotpotSort/Editor/WeChatBuild/HotpotSort.WeChatBuild.asmdef.meta",
      "sha256": "dd3bde40ac14b407e7973f8b4a17c3abb78f8c5fddd6ff6e43d4209509005334",
      "bytes": 166,
      "artifact_id": "artifact-210f7b2058974777",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-be539e1aa9c74c6c": {
      "path": "Unity/Assets/HotpotSort/Editor/WeChatBuild/WeChatBuild.cs",
      "sha256": "e1ad718b456d821e751e1f4e8d1c07131c4a5002b61f5d374920caeec1cd2495",
      "bytes": 3261,
      "artifact_id": "artifact-be539e1aa9c74c6c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-364826d870a7493e": {
      "path": "Unity/Assets/HotpotSort/Editor/WeChatBuild/WeChatBuild.cs.meta",
      "sha256": "fa4d3cdd6323bd45e211c102e45f7e0e1bce1581590e5fb18e4ffca5311f598e",
      "bytes": 59,
      "artifact_id": "artifact-364826d870a7493e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fd44f7fbcf7c4ff2": {
      "path": "Unity/Assets/HotpotSort/Runtime/Bootstrap/ProductionComposition.cs",
      "sha256": "bb25d609fe1a700fdc60aea5f00a7ef5e5ad4c901f3a777544c73b91ac92b69d",
      "bytes": 978,
      "artifact_id": "artifact-fd44f7fbcf7c4ff2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-55f2bdb057564535": {
      "path": "Unity/Assets/HotpotSort/Runtime/Bootstrap/ProductionComposition.cs.meta",
      "sha256": "d5de9efba61a530dce5793f76bc7f04c498e69ae8bef4d2575a0c76e44a7be37",
      "bytes": 59,
      "artifact_id": "artifact-55f2bdb057564535",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-389209a0a2e84b55": {
      "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/HotpotSort.WeChat.asmdef",
      "sha256": "022cde2567211415b45748933813cf7790338c00b4b0fe1072502ae8b11b1229",
      "bytes": 147,
      "artifact_id": "artifact-389209a0a2e84b55",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7541d69f3534403e": {
      "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/HotpotSort.WeChat.asmdef.meta",
      "sha256": "5d9469934aa5ab8854f1ad003ec03c90894f475f9cb541f1a3451270a838111f",
      "bytes": 166,
      "artifact_id": "artifact-7541d69f3534403e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-91949417b0664bef": {
      "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/LocalDiagnostics.cs",
      "sha256": "517be655c694cec61c2ad3da767113eea35565bedbb03ebe0a75285e32556d6a",
      "bytes": 2113,
      "artifact_id": "artifact-91949417b0664bef",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4540e30254c4490e": {
      "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/LocalDiagnostics.cs.meta",
      "sha256": "ca6dc06e3672d912659b339d03d3fd421bf85ca66f2389d25f3f92463014303f",
      "bytes": 59,
      "artifact_id": "artifact-4540e30254c4490e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-dd8830796e084735": {
      "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatPlatform.cs",
      "sha256": "688b4946536bc939cc9c5d51ae01a558cdb6520092c5b1faae360dcd2604b76f",
      "bytes": 3319,
      "artifact_id": "artifact-dd8830796e084735",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-15552b11aa2e47ab": {
      "path": "Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatPlatform.cs.meta",
      "sha256": "588f30a4f97a63a1765d563ab431628ea94e4ae016017ebace8cd8cd1f7ac81e",
      "bytes": 59,
      "artifact_id": "artifact-15552b11aa2e47ab",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ed3398e2d5344cf5": {
      "path": "Unity/Assets/HotpotSort/Runtime/Session/HotpotSort.Session.asmdef",
      "sha256": "94ba3bfa6998551b44f645f65060864b60afadf152d3a3aafd304d229fc769d8",
      "bytes": 142,
      "artifact_id": "artifact-ed3398e2d5344cf5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-35be16ba42a14cca": {
      "path": "Unity/Assets/HotpotSort/Runtime/Session/HotpotSort.Session.asmdef.meta",
      "sha256": "efa78ca47092d44e0092fa2762f0663c703c2edf6e7e3903a7af341311d7db04",
      "bytes": 166,
      "artifact_id": "artifact-35be16ba42a14cca",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-28b8ad71bc1e446e": {
      "path": "Unity/Assets/HotpotSort/Runtime/Session/SessionController.cs",
      "sha256": "c3e0567b1c36222cb73eec5fa5e71db6dde4dfe9a1cdb0d45dd66da53b057243",
      "bytes": 8579,
      "artifact_id": "artifact-28b8ad71bc1e446e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fcb958f336aa4aff": {
      "path": "Unity/Assets/HotpotSort/Runtime/Session/SessionController.cs.meta",
      "sha256": "0263e8fdd492e0bc6567eb8e935d89b874f4f50f05382e2f1d81d94ec04ae0f1",
      "bytes": 59,
      "artifact_id": "artifact-fcb958f336aa4aff",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ed7853cfd8cc4b95": {
      "path": "Unity/Assets/HotpotSort/Runtime/Session/TimeResolver.cs",
      "sha256": "cc5f2bb1a2b56dd8b61f1ad990537b85208c38f9015e1fb682b625da3462a334",
      "bytes": 2054,
      "artifact_id": "artifact-ed7853cfd8cc4b95",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-93dc1b3471ea4856": {
      "path": "Unity/Assets/HotpotSort/Runtime/Session/TimeResolver.cs.meta",
      "sha256": "75daeae913729f957d27d1cbc292973a7cf437cef255e49a2b043d025112a019",
      "bytes": 59,
      "artifact_id": "artifact-93dc1b3471ea4856",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fa8a8ee0586147c9": {
      "path": "Unity/Packages/WXSDK_COMPATIBILITY.md",
      "sha256": "511cdbfb70deab489157564a15f6d03fb91e87a74ff5613a5f4e0e47bc462fc0",
      "bytes": 1533,
      "artifact_id": "artifact-fa8a8ee0586147c9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-27057166be8d47b0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/.gitignore",
      "sha256": "e2eb93a61ffd7877ea5c751abcb3a618e8e2e9a2073a27f66d4114fe10819f86",
      "bytes": 9,
      "artifact_id": "artifact-27057166be8d47b0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e2930154ecba4a8d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/CHANGELOG.md",
      "sha256": "a5c6c66f419a06a747fd5e2e9cfbf421677e5951c891d5d985a052a4d0110b2b",
      "bytes": 34226,
      "artifact_id": "artifact-e2930154ecba4a8d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-703756d5141443f3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/CHANGELOG.md.meta",
      "sha256": "8d62ab1e0ae4800e7d66e68cc6cc00a99a82f0f532024b020385c3b3013a8517",
      "bytes": 158,
      "artifact_id": "artifact-703756d5141443f3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0154eaa109f2410d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor.meta",
      "sha256": "3cd97ae33cf3da367955dea72e53a50f2064542e0a079702acbee3c3d3be5cbe",
      "bytes": 172,
      "artifact_id": "artifact-0154eaa109f2410d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b141a4760efa4617": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli.meta",
      "sha256": "2994f978b60efa0fa3305d3490d4b46a561358283c5293a9a70e815bad3460c4",
      "bytes": 172,
      "artifact_id": "artifact-b141a4760efa4617",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ed5a326fe99240cd": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/LICENSE.txt",
      "sha256": "3d180008e36922a4e8daec11c34c7af264fed5962d07924aea928c38e8663c94",
      "bytes": 1084,
      "artifact_id": "artifact-ed5a326fe99240cd",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-aba67c8d88784625": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/LICENSE.txt.meta",
      "sha256": "81726dfeaadcfef5425947a3db3bd3e4ddb9c1d25ed2705b4cf021f7b9e8774c",
      "bytes": 158,
      "artifact_id": "artifact-aba67c8d88784625",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7a8730ae0c1446e3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/commits.txt",
      "sha256": "4e4808fe2ffdddd0218034374ee03cc4e940bb64801251baf2402d4787a0520a",
      "bytes": 86,
      "artifact_id": "artifact-7a8730ae0c1446e3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1271580a0dbe4346": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/commits.txt.meta",
      "sha256": "4c90ad352a3495966d43ca820eb7710fe648e4806778af8051b9e40ce3260ca3",
      "bytes": 158,
      "artifact_id": "artifact-1271580a0dbe4346",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c0b98eab7dc742b0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64.meta",
      "sha256": "5433569f97fa32945f6c1a7a901932778de4da53e9ee8cc0778941fe80bdde26",
      "bytes": 172,
      "artifact_id": "artifact-c0b98eab7dc742b0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-25dcba7f874043f0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/brotli",
      "sha256": "cef2a41c929821b7abdc2276ffb735f9f7b4b7d913331a215fb45f07ba95ae5d",
      "bytes": 852560,
      "artifact_id": "artifact-25dcba7f874043f0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-986e5528c1824909": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/brotli.meta",
      "sha256": "f128730561ac30af0b68550dd2e00df1af73e0fcee3fe400520c47d1b00101ad",
      "bytes": 155,
      "artifact_id": "artifact-986e5528c1824909",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6a6aa85a33eb47fd": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/libbrotlicommon.so",
      "sha256": "c3e33564197cfad710d7fc11d5c1c85a2b73aac9198ae7208dcd523f3a0c437b",
      "bytes": 143728,
      "artifact_id": "artifact-6a6aa85a33eb47fd",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-936dd045d502498d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/libbrotlicommon.so.meta",
      "sha256": "0a260e4f73b41b185dc3ec3f7992985576c4b04c81eebf557ac1cb78b85e5e08",
      "bytes": 526,
      "artifact_id": "artifact-936dd045d502498d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c39481bc3f9b47a8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/libbrotlienc.so",
      "sha256": "479035010432447c3e98cff954af4ba86c026fe5b0d6776d5917ac248324357f",
      "bytes": 719728,
      "artifact_id": "artifact-c39481bc3f9b47a8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1e3bbc26b8354507": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/linux_x86_64/libbrotlienc.so.meta",
      "sha256": "6c69fb062db76c0ed4affa7ba21fac97683b94496687a0c2385d22771ee3f830",
      "bytes": 526,
      "artifact_id": "artifact-1e3bbc26b8354507",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-39b896528b3e43d2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos.meta",
      "sha256": "4e6a975540a9bb0f5a7a69a16d3ec7753383bd94bea94de16858907fe05b4a6d",
      "bytes": 172,
      "artifact_id": "artifact-39b896528b3e43d2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-02bc15f4acce49e2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/brotli",
      "sha256": "6acd96976472377af3f71786be2bec7c26e733d186ff5a1bbe45c69d9358655d",
      "bytes": 1661624,
      "artifact_id": "artifact-02bc15f4acce49e2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8acf52bf144a4ebe": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/brotli.meta",
      "sha256": "93bb5a08fcf02a6988567f879853d185d240630e40538dd9a1a15f121280c87f",
      "bytes": 155,
      "artifact_id": "artifact-8acf52bf144a4ebe",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d9846e08af9c46e2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/libbrotlicommon.dylib",
      "sha256": "3e2a49ddc705bfaa8ad0879b60bf4ea3c06b63cd84f60dc655746c6172bcd1d0",
      "bytes": 297472,
      "artifact_id": "artifact-d9846e08af9c46e2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-30880e44a72242ea": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/libbrotlicommon.dylib.meta",
      "sha256": "4429d9d9db64d566d6cf60168d70724d837d225827a9c7ecd5b81df08f1733c5",
      "bytes": 526,
      "artifact_id": "artifact-30880e44a72242ea",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1383202e314f4fff": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/libbrotlienc.dylib",
      "sha256": "e610167aca7cffe78baad1f166c6bc4764403fbcf6f1d9be2d29b0eea530f311",
      "bytes": 1507280,
      "artifact_id": "artifact-1383202e314f4fff",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c9c03615e98f4ab7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/macos/libbrotlienc.dylib.meta",
      "sha256": "1f6bb870a160006c2638f258e575543e8b42e8589019cc6ccce5cb062cdee034",
      "bytes": 526,
      "artifact_id": "artifact-c9c03615e98f4ab7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0b2c410068cf432d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64.meta",
      "sha256": "c283bbd19e4e90b24548ea56940bd658c99c44e32cd5988170d602ddaf48af4b",
      "bytes": 172,
      "artifact_id": "artifact-0b2c410068cf432d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-55df4e1319314757": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/brotli.exe",
      "sha256": "26501f636eb41473feb38d05251c71a6e7c4dec6006c459e7c5acf8442cd4394",
      "bytes": 749056,
      "artifact_id": "artifact-55df4e1319314757",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e94febd141ce472d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/brotli.exe.meta",
      "sha256": "a77299dc41dd1a27daf89361720114912b82ab9663165214d9e0db4a5ed29cb8",
      "bytes": 155,
      "artifact_id": "artifact-e94febd141ce472d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ee4a56e21fec4268": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/libbrotlicommon.dll",
      "sha256": "16a4e31f18cf9a8e0bd2ff5093d1ab9dd73f1d28577305ef936813a3f6fddab1",
      "bytes": 378616,
      "artifact_id": "artifact-ee4a56e21fec4268",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-81ffab9cad4c41f8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/libbrotlicommon.dll.meta",
      "sha256": "0126fa8eb85e21cc955df03ec8157d885622e4014ade9d7b052fc24f93136421",
      "bytes": 526,
      "artifact_id": "artifact-81ffab9cad4c41f8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-40a92da519194b74": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/libbrotlienc.dll",
      "sha256": "af79a66fc39ed13ca469a0a51868237d267f40fb42512783c7a1e3b909b6ce35",
      "bytes": 958662,
      "artifact_id": "artifact-40a92da519194b74",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3e6f01e802c84a26": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/libbrotlienc.dll.meta",
      "sha256": "fbc144014d796a11f8a1816ac3f0e4deedfaf65cb45c6ed5e23347f81c281bae",
      "bytes": 526,
      "artifact_id": "artifact-3e6f01e802c84a26",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d7fd59be8d8442af": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140.dll",
      "sha256": "2cc02c5e6654aa9175d5963f811cac222f4a2604dc28553139c675b1a78995a7",
      "bytes": 590112,
      "artifact_id": "artifact-d7fd59be8d8442af",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ca15633d96174d70": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140.dll.meta",
      "sha256": "572a1935751ecd0aee101c6f6f3e4c681b3c73720e8934212ac608bc1ef9db79",
      "bytes": 526,
      "artifact_id": "artifact-ca15633d96174d70",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-75d89258825b4a45": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140_1.dll",
      "sha256": "d8a6c4f6a8da4dfc33cd956d59554cda144d0111d0e750ace8a77555e93365bd",
      "bytes": 31728,
      "artifact_id": "artifact-75d89258825b4a45",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9ed0439ecbd44649": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140_1.dll.meta",
      "sha256": "075d985bd1beb8373039cbe941510da500740db375875382ae31f4d0f16682dc",
      "bytes": 526,
      "artifact_id": "artifact-9ed0439ecbd44649",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f5b79221179c4067": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140_2.dll",
      "sha256": "9cdbab1a0a1b70228d781c5e19a0d055f42c9914cf8d9e93e7fe1709196e3e80",
      "bytes": 192800,
      "artifact_id": "artifact-f5b79221179c4067",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8ed69a69d61a40ee": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/msvcp140_2.dll.meta",
      "sha256": "3cf256d30ad273151f856f16b601c1b2fe1ff83e366bead83c5a6db4775be039",
      "bytes": 526,
      "artifact_id": "artifact-8ed69a69d61a40ee",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-008e10786ec949f8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/vcruntime140.dll",
      "sha256": "263988a0868053b6b01835cd2959c8f71e3f943610421b269da646f2d9e3b333",
      "bytes": 100880,
      "artifact_id": "artifact-008e10786ec949f8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d49d0bdcae554e5f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/vcruntime140.dll.meta",
      "sha256": "0179803739e7353cdc20319f2ab124ae6fce298c698e90c705e89055a2240827",
      "bytes": 526,
      "artifact_id": "artifact-d49d0bdcae554e5f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-900a36dba0f0440a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/vcruntime140_1.dll",
      "sha256": "da5d3440dd53261bffec0f9163a46eb12e46b2a4e1bd72dd1b62c6bca9cca280",
      "bytes": 44320,
      "artifact_id": "artifact-900a36dba0f0440a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e2eded3955de4781": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Brotli/win_x86_64/vcruntime140_1.dll.meta",
      "sha256": "dd6589fc7fec873417b95dea7ad86f0a66f8c3b29d88d7c19b3b4fd3f3b7ad44",
      "bytes": 526,
      "artifact_id": "artifact-e2eded3955de4781",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b5bfdb0c9aed4877": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile.meta",
      "sha256": "1065773ccb7fc9951e971570eb013e2d6e539f922dfedcf864f64eea88f47063",
      "bytes": 171,
      "artifact_id": "artifact-b5bfdb0c9aed4877",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cd153878791843df": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinBuildProfileUpdater.cs",
      "sha256": "5be88115f14529006bf6b8caad934a88b521f4ecb04fdd051120a11d3054b247",
      "bytes": 3594,
      "artifact_id": "artifact-cd153878791843df",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0709b0a3fea14bde": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinBuildProfileUpdater.cs.meta",
      "sha256": "4b07f71822876693fe443d461a3e0f0af3fe9fe2786b38d4bf424ec56e3e12d5",
      "bytes": 243,
      "artifact_id": "artifact-0709b0a3fea14bde",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ef4b8fcb4c694d16": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinMiniGameSettings.cs",
      "sha256": "d7d9aac1c339b08b987c80baa14fbd2ca1124750d8304901c56806b3c140882a",
      "bytes": 5693,
      "artifact_id": "artifact-ef4b8fcb4c694d16",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b406f3c8c9094944": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinMiniGameSettings.cs.meta",
      "sha256": "7d141c00ed34b3999d63d4ad6cef05a1fe2c0036e3f16e6f1ef5d94019e7d4e0",
      "bytes": 146,
      "artifact_id": "artifact-b406f3c8c9094944",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a242c4b7824244d9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinMiniGameSettingsEditor.cs",
      "sha256": "60c60fb97c86bbe722558539db788000b66355ff41784349a66f627470d4f44a",
      "bytes": 1455,
      "artifact_id": "artifact-a242c4b7824244d9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6e7f53d3ca5448ad": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinMiniGameSettingsEditor.cs.meta",
      "sha256": "72f1e3e6da0ea0cc3d3dbe24c2990cdfe51e0513475cb9e12f70c1b3c9064f26",
      "bytes": 146,
      "artifact_id": "artifact-6e7f53d3ca5448ad",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cfb2f294022f40f4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinSubplatformInterface.cs",
      "sha256": "0cfb426bf72e5288d52f663347153453c7521ea5196aac4b57e5edb32b4682a1",
      "bytes": 13924,
      "artifact_id": "artifact-cfb2f294022f40f4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-afe0611ffecb4039": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/WeixinSubplatformInterface.cs.meta",
      "sha256": "b856e96ca11d7cdfa8642a6f2efce90b5859e57392df57f2e4bab5d2b5ca38ef",
      "bytes": 146,
      "artifact_id": "artifact-afe0611ffecb4039",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4ae1e6c0d6144911": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib.meta",
      "sha256": "8d0a58603c9bd26394a1d3d293cb0659d63fbb24ef875f99b6dda044e5932131",
      "bytes": 171,
      "artifact_id": "artifact-4ae1e6c0d6144911",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1cea2452f6614cc3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib/libwx-metal-cpp.bc",
      "sha256": "1d84425ec57197f0f74464e4a9ebedbd1bfa3e40f1949c675e25b98fe2803b5c",
      "bytes": 2779888,
      "artifact_id": "artifact-1cea2452f6614cc3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-de86721d666e4158": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib/libwx-metal-cpp.bc.meta",
      "sha256": "7b359ae4f3a91b41d1dd4fef07bf44aebdd1abb1047f74d662a52f8836f56bc2",
      "bytes": 1806,
      "artifact_id": "artifact-de86721d666e4158",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8b8f85b9d5aa455e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib/mtl_library.jslib",
      "sha256": "983bc417da173678ac912eeea15087f8ffb106dc18b67699de3aab202681ccf5",
      "bytes": 1522,
      "artifact_id": "artifact-8b8f85b9d5aa455e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-710553b8f4e94dd5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/BuildProfile/lib/mtl_library.jslib.meta",
      "sha256": "a6a5e05931c731730805bb4ae3e8a3df50b84ac4cd64962bf841643e85d3a830",
      "bytes": 1806,
      "artifact_id": "artifact-710553b8f4e94dd5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-368003111d9544ea": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker.meta",
      "sha256": "06f792e1abc5d58f9f1861fbb7ffcbfdf56da8c1e5c134de5906c302b3efeed1",
      "bytes": 172,
      "artifact_id": "artifact-368003111d9544ea",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b0bc0e79f94a4417": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/mac.meta",
      "sha256": "fc6737db0757e6a0960a05dcf3f4a416780426afe289e5d31b1517a37a647155",
      "bytes": 172,
      "artifact_id": "artifact-b0bc0e79f94a4417",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5a2bff8c8c944c99": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/mac/newstatehooker.dylib",
      "sha256": "6852d71738c72e626bfd838d498aad883680fb571135a5c94ad375924cc5f7eb",
      "bytes": 35309464,
      "artifact_id": "artifact-5a2bff8c8c944c99",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c97f7ed4b86c49c9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/mac/newstatehooker.dylib.meta",
      "sha256": "b0431c8158822533e540f49cbccf39886f265211e07f3d0d9318304d9602b299",
      "bytes": 1195,
      "artifact_id": "artifact-c97f7ed4b86c49c9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d4fdcff935344c85": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win.meta",
      "sha256": "ce5a804b53581f01a872ae78d3029c212793ee82443a57bb618fa88cfb21e14b",
      "bytes": 172,
      "artifact_id": "artifact-d4fdcff935344c85",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c9f63f1d64104ada": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win/newstatehooker.dll",
      "sha256": "958017e52455d00df130910a671a7661ddde63b66a36e40ea1c7e0babd317c50",
      "bytes": 7734272,
      "artifact_id": "artifact-c9f63f1d64104ada",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8f8c0450ccdf41c8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win/newstatehooker.dll.meta",
      "sha256": "353bd8688c5aee0d54ebb9e56841847d73cdc60d742e90fb503024d766cfaca6",
      "bytes": 1593,
      "artifact_id": "artifact-8f8c0450ccdf41c8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-62eb387fdc1349c9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win/ucrtbased.dll",
      "sha256": "bed98a14f107cabd8e5e4ad43aedd0b357656ca1b577167c22d2829134d4e52e",
      "bytes": 2238056,
      "artifact_id": "artifact-62eb387fdc1349c9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-54046d1edc9c4377": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/LuaHooker/win/ucrtbased.dll.meta",
      "sha256": "3a426ffe785ce20a1fac5a68133e457d268741fe364ddf39881eb3ad57143ae9",
      "bytes": 1593,
      "artifact_id": "artifact-54046d1edc9c4377",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-57a457439d7a4e92": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/MiniGameConfig.asset",
      "sha256": "be9def00391429090a65b5fe821af958025ba789fdabc6ab1c6f43c5d01a117c",
      "bytes": 2338,
      "artifact_id": "artifact-57a457439d7a4e92",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d83754c0019649cd": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/MiniGameConfig.asset.meta",
      "sha256": "6a8bc560cb7d2a84c39f00b23ee5c1d773207962a1c1988984698ec4c1b603a4",
      "bytes": 189,
      "artifact_id": "artifact-d83754c0019649cd",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f0e6ee5ecc504b7f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node.meta",
      "sha256": "c2f922e2f4a38898a4ed8340438dbd5d7ee2cea2ded5cdfda0e89d87efa29c17",
      "bytes": 172,
      "artifact_id": "artifact-f0e6ee5ecc504b7f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-16b5d3dd7f484116": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/dump_wasm_symbol.mjs",
      "sha256": "0d850cfd0ec24c23269d64df10bae33082cf34aa17ab9d31d529050c70dad00d",
      "bytes": 751,
      "artifact_id": "artifact-16b5d3dd7f484116",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c8d47000aac34cb3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/dump_wasm_symbol.mjs.meta",
      "sha256": "607eaa0c6eb05a734730c44b2aa428e6ae3c11739521cc22417acb943e9be429",
      "bytes": 155,
      "artifact_id": "artifact-c8d47000aac34cb3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-dc09568825994148": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules.meta",
      "sha256": "9f2d8609f0d6c0617e269956e9329aad145c89cba15210d7d535b481ad809356",
      "bytes": 172,
      "artifact_id": "artifact-dc09568825994148",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a1f4b86a39db4d71": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen.meta",
      "sha256": "800003b63c20d6b7ffe9bcf0cc8431993f0c0865b4d38f10936cbc579d5bb558",
      "bytes": 172,
      "artifact_id": "artifact-a1f4b86a39db4d71",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c4c46393e5194eae": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/LICENSE",
      "sha256": "c5accbbd8546e94c34aed24afe689a617627d18eed5a6c48277e48db57c23851",
      "bytes": 11356,
      "artifact_id": "artifact-c4c46393e5194eae",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-86d403ab14004814": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/LICENSE.meta",
      "sha256": "aba1be3cc175dfafdb6a8bb05678b621dd05d9c677fc766b5a905a04d59a608b",
      "bytes": 155,
      "artifact_id": "artifact-86d403ab14004814",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7e92c674e74f48ef": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/README.md",
      "sha256": "e79c00d33c7422da9dbdb992dc403f3c72d70a2384a77cf6eff5d15bc633bf03",
      "bytes": 71745,
      "artifact_id": "artifact-7e92c674e74f48ef",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-efe9f45e82864a2b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/README.md.meta",
      "sha256": "452579f86ebbefda7f59c815ac09b33d859f8ec0bb1bd76c448417fa54cc8a42",
      "bytes": 158,
      "artifact_id": "artifact-efe9f45e82864a2b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c2844e2b873e4bba": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin.meta",
      "sha256": "25520dfdcb7f6c70df364b5d29f5f49b72f5f9e7e826cea64f8e1e990f9dec26",
      "bytes": 172,
      "artifact_id": "artifact-c2844e2b873e4bba",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-204fc1a2ce174920": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/package.json",
      "sha256": "8005a3491db7d92f36ac66369861589f9c47123d3a7c71e643fc2c06168cd45a",
      "bytes": 25,
      "artifact_id": "artifact-204fc1a2ce174920",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2b6d00baaaa1496a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/package.json.meta",
      "sha256": "178cdb5d56a392d7edc278065f8018ae95a53f72c31f9d55257199502c78975d",
      "bytes": 158,
      "artifact_id": "artifact-2b6d00baaaa1496a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-942b974f61b7417d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/wasm-opt",
      "sha256": "74d74ff270b483c93b6c768328e1d37593ee604237c05467fb53f2ea5c5c0f8b",
      "bytes": 6080234,
      "artifact_id": "artifact-942b974f61b7417d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-821ce88335634c38": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/wasm-opt.meta",
      "sha256": "9f3917b215e18bc951227bc2ee26836646a7c33a21daae48e23a2ba23665f5ed",
      "bytes": 155,
      "artifact_id": "artifact-821ce88335634c38",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2664128040d34b3c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/wasm2js",
      "sha256": "ee363a2bc1dd668446874c6fd38b723755be9037b2afb8623bdc92d1c1ac06ee",
      "bytes": 5626795,
      "artifact_id": "artifact-2664128040d34b3c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-735b6b8eb6c748c9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/bin/wasm2js.meta",
      "sha256": "babe30789d4623cd7b88e60c3b17286ddb16d8fd49aade770d7dc85cc61b8936",
      "bytes": 155,
      "artifact_id": "artifact-735b6b8eb6c748c9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-67129c63000f4799": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/index.d.ts",
      "sha256": "f9e5f46910a04c95f4f511322aa05f676bfe773d40fd48f0479924dd83dbb96b",
      "bytes": 78510,
      "artifact_id": "artifact-67129c63000f4799",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1a93801a644b44a4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/index.d.ts.meta",
      "sha256": "83d18e9ae6672174bde02b85253637cfcc266a77a8f6de6a84eae63681b8a9b5",
      "bytes": 155,
      "artifact_id": "artifact-1a93801a644b44a4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d5a141c4ab764e95": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/index.js",
      "sha256": "48749b8967512675a107971dcd7d0c76a20e9fdb6b00f3bc3067b4947190f0d6",
      "bytes": 6143047,
      "artifact_id": "artifact-d5a141c4ab764e95",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-605c53e143b24a9a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/index.js.meta",
      "sha256": "9d5ac86121388343379c09fd77890402bff1639959efe32248b8d3d09f9c840c",
      "bytes": 158,
      "artifact_id": "artifact-605c53e143b24a9a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-12858800716840b3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/package.json",
      "sha256": "90a5e5740260046247105210d99f6493d9d7c20dee934dd99fd7115daa176ede",
      "bytes": 1173,
      "artifact_id": "artifact-12858800716840b3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ca31f5ca08c1404f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/node_modules/binaryen/package.json.meta",
      "sha256": "9b9735a3607f75352bca7655f63b356a473da18ea19ac4495088dfaaa4474f6b",
      "bytes": 158,
      "artifact_id": "artifact-ca31f5ca08c1404f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-199cf7ed497d41e0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/package.json",
      "sha256": "89291b451fa8ed256f3bc0969b507acdc606b3e41c728abdfcf49c75447cd609",
      "bytes": 158,
      "artifact_id": "artifact-199cf7ed497d41e0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b380fa6da0de4a86": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Node/package.json.meta",
      "sha256": "5cd3c0b724156137c5d90e32a458847f934ec6b2e08ed6fac6adca04eda05f7a",
      "bytes": 158,
      "artifact_id": "artifact-b380fa6da0de4a86",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7107b16c7ccc49ce": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/PicCompressor.cs",
      "sha256": "851c1eaea2dd7966fb2590c7c41a84a116ed8b6c0a5228833874afc3ff2b0c6e",
      "bytes": 3962,
      "artifact_id": "artifact-7107b16c7ccc49ce",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b26950fc4b9e49ce": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/PicCompressor.cs.meta",
      "sha256": "5cdd4c7a4ac4eac71a23d3b1a64ff32dcc0a0e645925ab162668383d1a6ced98",
      "bytes": 243,
      "artifact_id": "artifact-b26950fc4b9e49ce",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-440efbc51bfd44e7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable.meta",
      "sha256": "dd01f4724e0efeac382050d234d357af58dda3af14a5b56d75c942e5d35122a0",
      "bytes": 172,
      "artifact_id": "artifact-440efbc51bfd44e7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-676689f865754fce": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableConvertCore.cs",
      "sha256": "b900cf7c614a97644e4c5b8df61545e8a3b4903a1980ddd63c1ba916769844ca",
      "bytes": 5711,
      "artifact_id": "artifact-676689f865754fce",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-028ecae2aa634400": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableConvertCore.cs.meta",
      "sha256": "dea962e2c73653b610684620fdf05c32073d513d86c5879693b26923ea54423b",
      "bytes": 243,
      "artifact_id": "artifact-028ecae2aa634400",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-329bfea6811647ba": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableEditorSettingHelper.cs",
      "sha256": "16e9a7e6114ca997a9b9707770c2f9c47651ccbe606e2dc940d797abc35d6252",
      "bytes": 14409,
      "artifact_id": "artifact-329bfea6811647ba",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d46a83d4b7f2456a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableEditorSettingHelper.cs.meta",
      "sha256": "8b3f1915b33a7d9794ba3f9d35ecaab5e6c030028487b006daa47a073e74e7ef",
      "bytes": 243,
      "artifact_id": "artifact-d46a83d4b7f2456a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8263c653a1114259": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableWindow.cs",
      "sha256": "d79724917000adff887b7f047fed7f9ce0a6f84c864cd2a314095ad1040fa69d",
      "bytes": 1286,
      "artifact_id": "artifact-8263c653a1114259",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-24feca781c9d4aa5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/Playable/WXPlayableWindow.cs.meta",
      "sha256": "249b353d061b3759e9cddab6464ecf5e65058efc5b44c9eec0c984148267189e",
      "bytes": 243,
      "artifact_id": "artifact-24feca781c9d4aa5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0041e504f77340d9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor.meta",
      "sha256": "c4ee1f634dcf138d721dd6a9f3f4d78050b0e0b03aeb32f3d7d9232614bfd2f4",
      "bytes": 172,
      "artifact_id": "artifact-0041e504f77340d9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-502ea71a7879401c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node.meta",
      "sha256": "7559a48383d0a3d165f741666416eff8376ee908e83b5af1bb8d174f3f7e627c",
      "bytes": 172,
      "artifact_id": "artifact-502ea71a7879401c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2fca5d6327834ee4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/PVRTexToolCLI",
      "sha256": "438f57a7e1c5384015b38b4b9dd2eb1f824a8747681b6bb83f4b286e5382eee3",
      "bytes": 5546016,
      "artifact_id": "artifact-2fca5d6327834ee4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-585f44f8ffc54996": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/PVRTexToolCLI.exe",
      "sha256": "edfc7866faaa57419223555d25965bf7c9482133a07b8401c138ed2696df94d0",
      "bytes": 4920232,
      "artifact_id": "artifact-585f44f8ffc54996",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3fe48f69e5d04236": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/PVRTexToolCLI.exe.meta",
      "sha256": "5404dbdc551fc564d58ee8d61bbc0a1f21c7989547922dac55b19e4e5ffb20ae",
      "bytes": 155,
      "artifact_id": "artifact-3fe48f69e5d04236",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-448a81c36ab34d7f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/PVRTexToolCLI.meta",
      "sha256": "ed97b00adacda383b1f78129e2a57dfcc8bb803f617fd5481e1df0efb2360f54",
      "bytes": 155,
      "artifact_id": "artifact-448a81c36ab34d7f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c69756a515754e2a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-avx2",
      "sha256": "f98d53f9753c17ea4ba2140832a1a0942cfdd74caf41babe9d950de9c4497b50",
      "bytes": 640544,
      "artifact_id": "artifact-c69756a515754e2a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fee2bdb3991f4fde": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-avx2.exe",
      "sha256": "18b199c44173b1b00ef44a8aa34f97d8218bd384687522dadfe88850513770ee",
      "bytes": 999784,
      "artifact_id": "artifact-fee2bdb3991f4fde",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c037b311d6f4468b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-avx2.exe.meta",
      "sha256": "ce5f878521c42fcc0e6cd4b8ed0495333e5da675f2332116eaf50db36ecb376e",
      "bytes": 155,
      "artifact_id": "artifact-c037b311d6f4468b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c112b7995f2f48c8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-avx2.meta",
      "sha256": "78726c1f94efb6f7b90e63da9c0c356a9b3a30a73b02ea63250e0bcbb9b4a3f7",
      "bytes": 155,
      "artifact_id": "artifact-c112b7995f2f48c8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-78c9dc8e90af4400": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-neon",
      "sha256": "4f29565b8722eac89b698db11773a5c55ead685bea080f6cb30171021cf99b15",
      "bytes": 558784,
      "artifact_id": "artifact-78c9dc8e90af4400",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1c3810c10fbe42a3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-neon.meta",
      "sha256": "fe42a98fcb43fc93bcc67e9d49621cdad46a0ad8432a7168bb732526388006cd",
      "bytes": 155,
      "artifact_id": "artifact-1c3810c10fbe42a3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6da54ecb26f247e6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-sse4.1.exe",
      "sha256": "aeeb7e850ecc91a85bbfd9a06fb5e031bc4a2cbe8621a688ce2e02a5e1cf5161",
      "bytes": 813896,
      "artifact_id": "artifact-6da54ecb26f247e6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-66fbbb3b01f94a88": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/astcenc-sse4.1.exe.meta",
      "sha256": "9742a847ada9f74dfcc718aff8218e544855cb1abdfcd95dfa11bda522fc1225",
      "bytes": 155,
      "artifact_id": "artifact-66fbbb3b01f94a88",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-07f8d928d78c43b2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/pngquant",
      "sha256": "eca7aa3b689b50d7577d9bc9853c644e566b5ec2e13fe32a9bdeb2e60de53463",
      "bytes": 837984,
      "artifact_id": "artifact-07f8d928d78c43b2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3ffe24028dfa42e4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/pngquant.exe",
      "sha256": "76e383f1b3962cb19d3ea322f632b8778a0770a31e5d09dcb1466ddf19e26f7a",
      "bytes": 768512,
      "artifact_id": "artifact-3ffe24028dfa42e4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cd300c0ab1224a6f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/pngquant.exe.meta",
      "sha256": "ebbc4aace90a28ab74bfc274611c1f635b2b77d846ca0e93c55c4ffaa843a1d6",
      "bytes": 155,
      "artifact_id": "artifact-cd300c0ab1224a6f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b1b9238153d043f7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Node/pngquant.meta",
      "sha256": "722b51fd1261e27aa326e8c8df2b1a6d25139bdaabda663505dc36bdf91dd8cf",
      "bytes": 155,
      "artifact_id": "artifact-b1b9238153d043f7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-24ee65367ca9483b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release.meta",
      "sha256": "59a35c705d0b4cc3218e473ac3e1dacf82eb938846b179e9b2ca0d444579f1cb",
      "bytes": 172,
      "artifact_id": "artifact-24ee65367ca9483b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4200781c1ef64e36": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetRipper.TextureDecoder.dll",
      "sha256": "a5aa7cbe87ec00b579ba49832e0d75698ed92973754c8f91e619099ca5d1bbd3",
      "bytes": 116224,
      "artifact_id": "artifact-4200781c1ef64e36",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-905f7be27bc649b6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetRipper.TextureDecoder.dll.meta",
      "sha256": "236d9e9ca03bef0a3a30b2490af2c756a709e773c688484de798348ec9c34f0b",
      "bytes": 1668,
      "artifact_id": "artifact-905f7be27bc649b6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5ffd2581743d4780": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetsTools.NET.Texture.dll",
      "sha256": "d2ccd4e16374e72a0f5135d7845252eb39022c839adfc89a00c8adf88c575d15",
      "bytes": 66560,
      "artifact_id": "artifact-5ffd2581743d4780",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3f599c8c9bd84690": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetsTools.NET.Texture.dll.meta",
      "sha256": "3a18a98107b310729ef1e3dba10eee4b01dcbfb5f1336eabb5c4d10582bebd11",
      "bytes": 1668,
      "artifact_id": "artifact-3f599c8c9bd84690",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-271f81487de44162": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetsTools.NET.dll",
      "sha256": "94ed77a6277d55921618a79cfa54795c235243b69d76be2d81fef1aaa2ec3ef1",
      "bytes": 193024,
      "artifact_id": "artifact-271f81487de44162",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a6bdea9eded8406a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/AssetsTools.NET.dll.meta",
      "sha256": "a5ab2a0d8d9e517da8127d444f404bc8c70e0cb801fc6efa555050ad93c29b62",
      "bytes": 932,
      "artifact_id": "artifact-a6bdea9eded8406a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2d4d1eb37b46487b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Mdb.dll",
      "sha256": "f5104aa2f42fd15d36a3c82c0d60b98c4f4e14e2dd0c027bcaf952ba1ecc1c2a",
      "bytes": 43008,
      "artifact_id": "artifact-2d4d1eb37b46487b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-719280dd614c472e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Mdb.dll.meta",
      "sha256": "4b83e5dac87f26daeea9d203b6896fb71642593f3057221c70b38afc753f8597",
      "bytes": 932,
      "artifact_id": "artifact-719280dd614c472e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a12656d5169748cb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Pdb.dll",
      "sha256": "a4788b2153629c49ab75297fa273ac0ca7ab0b799d36442ada968ed037023c71",
      "bytes": 86528,
      "artifact_id": "artifact-a12656d5169748cb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-dde26dd4636347e9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Pdb.dll.meta",
      "sha256": "712d4f9afc9bb3b1effed1c5e6b72062605c91bcfcb738e6f775c1f874ae0850",
      "bytes": 932,
      "artifact_id": "artifact-dde26dd4636347e9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f1a3acd87e5a4e20": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Rocks.dll",
      "sha256": "31183b063f8043437c07fd3e7168517e0a7ad588b9270190f3cf74556db955ee",
      "bytes": 27648,
      "artifact_id": "artifact-f1a3acd87e5a4e20",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f5108e248cce45f2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.Rocks.dll.meta",
      "sha256": "f45c9bed156c12755c0ed23b5c73128ccf76ce607582175ed26c78c8c8f9cd36",
      "bytes": 932,
      "artifact_id": "artifact-f5108e248cce45f2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-efa8d50e43bc461b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.dll",
      "sha256": "2c91802e6e95afc1d113f486df8c9c03792cb4898da219f0eb212bdd04fb3478",
      "bytes": 339456,
      "artifact_id": "artifact-efa8d50e43bc461b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d61593670eb4472b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/Mono.Cecil.dll.meta",
      "sha256": "d765ccfa758345c0829f7ea571c4e5fd1afa9f21b05f6489df39c06886a26eae",
      "bytes": 932,
      "artifact_id": "artifact-d61593670eb4472b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ed56704df6ad4347": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/SixLabors.ImageSharp.dll",
      "sha256": "69588c5a315ecb02e27fc841465d62c8b41c91ffb0b40c19582bade8fac61451",
      "bytes": 1722880,
      "artifact_id": "artifact-ed56704df6ad4347",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4b8182832950484a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/SixLabors.ImageSharp.dll.meta",
      "sha256": "8ce2aa3cc7fa07cdea4a78e4357884931f77e5d3f50bc413b15b828d7829ff8f",
      "bytes": 932,
      "artifact_id": "artifact-4b8182832950484a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8629c381ac5c4ffb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/SixLabors.ImageSharp.xml",
      "sha256": "f255e023999c3f89c95ed52805f51f5d94a9e01cb4e886abf8819536f67bc305",
      "bytes": 3822252,
      "artifact_id": "artifact-8629c381ac5c4ffb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-29cae5f99eed4823": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/SixLabors.ImageSharp.xml.meta",
      "sha256": "c39847eb64dc1dddb58b021b4ff23c1f6caf0c3ca8a6166d38a068dc7a61e5a4",
      "bytes": 158,
      "artifact_id": "artifact-29cae5f99eed4823",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4bbb8708dd7a4393": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Buffers.dll",
      "sha256": "accccfbe45d9f08ffeed9916e37b33e98c65be012cfff6e7fa7b67210ce1fefb",
      "bytes": 20856,
      "artifact_id": "artifact-4bbb8708dd7a4393",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f99f7f1e6ea745c7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Buffers.dll.meta",
      "sha256": "d52866eae8be25e99b82481ce34a6028da4a756f0a403c3c1e8e60a6e4a9a204",
      "bytes": 932,
      "artifact_id": "artifact-f99f7f1e6ea745c7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c752e5c022b94619": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Buffers.xml",
      "sha256": "e6191e340a6b07a65d0e59c672c37bfa26095d7ef5c5a95b6e15a6c89c45f733",
      "bytes": 3444,
      "artifact_id": "artifact-c752e5c022b94619",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-261e1918f53e4aa2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Buffers.xml.meta",
      "sha256": "2fb5ca699f6cad72b2c7adaa03a648e1e5c57f672499280d2f9b9f78517f26a1",
      "bytes": 158,
      "artifact_id": "artifact-261e1918f53e4aa2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2efdf4d3b8404d59": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Half.dll",
      "sha256": "b1fb190a77169e4a561acfc7883adca479ba43d36f95d85ec36db1b0fa11d0c0",
      "bytes": 13312,
      "artifact_id": "artifact-2efdf4d3b8404d59",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5290832cfae645c8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Half.dll.meta",
      "sha256": "682100ff915b21863dd66a795f3d57c2a85f16b3b32b4b8ec06cbd86368403b5",
      "bytes": 1668,
      "artifact_id": "artifact-5290832cfae645c8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b014a387e58e4848": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Memory.dll",
      "sha256": "bf3fb84664f4097f1a8a9bc71a51dcf8cf1a905d4080a4d290da1730866e856f",
      "bytes": 142240,
      "artifact_id": "artifact-b014a387e58e4848",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ab15c9aa94074308": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Memory.dll.meta",
      "sha256": "d0368188134cdb824a9183872f6c114b483c4941eb0c1850a0577ddb2fcaa38b",
      "bytes": 932,
      "artifact_id": "artifact-ab15c9aa94074308",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-40a7b58891f84c4d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Memory.xml",
      "sha256": "5d68c92d2372f23da7cb9ecd61ea6da6ebcb3eb5145f97463e64b4845cd8fd0b",
      "bytes": 13596,
      "artifact_id": "artifact-40a7b58891f84c4d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-516a593cb0a04dec": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Memory.xml.meta",
      "sha256": "a4d409f314e3ff63ff91b78b663647153c049476f5f361be3a2d13e4e3802415",
      "bytes": 158,
      "artifact_id": "artifact-516a593cb0a04dec",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9ae806743d0d4228": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Numerics.Vectors.dll",
      "sha256": "1d3ef8698281e7cf7371d1554afef5872b39f96c26da772210a33da041ba1183",
      "bytes": 115856,
      "artifact_id": "artifact-9ae806743d0d4228",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-855551a3d43b42ff": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Numerics.Vectors.dll.meta",
      "sha256": "53798ee71684af56ed27327f8f9ab0882b9e2027231a97f580765e821f2ee16b",
      "bytes": 932,
      "artifact_id": "artifact-855551a3d43b42ff",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-487d2cb468724235": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Numerics.Vectors.xml",
      "sha256": "ee6c0cd10f585f83711e58a377284c883b12f2d77fb35c1b0fe6aef2f1259673",
      "bytes": 180864,
      "artifact_id": "artifact-487d2cb468724235",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-15bc2ea7824b418c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Numerics.Vectors.xml.meta",
      "sha256": "1b10b27eb7554fc3a19add4c90fee9fd9c6192aabb4ab1425572ace60042b521",
      "bytes": 158,
      "artifact_id": "artifact-15bc2ea7824b418c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ae5d8c979ded48d6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Runtime.CompilerServices.Unsafe.dll",
      "sha256": "66409f670315afe8610f17a4d3a1ee52d72b6a46c544cec97544e8385f90ad74",
      "bytes": 16768,
      "artifact_id": "artifact-ae5d8c979ded48d6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-61bea9045fd94bba": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Runtime.CompilerServices.Unsafe.dll.meta",
      "sha256": "510818ccfff1f01511fa57d6d4010b151f5e15e9e902b24679b7322cd7427228",
      "bytes": 932,
      "artifact_id": "artifact-61bea9045fd94bba",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-52f673e6db7846d4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Runtime.CompilerServices.Unsafe.xml",
      "sha256": "ac87b3d560d5c1b65d2f3a1c13127d3bf194af3ea6f49cd27e3e75e6ec772d85",
      "bytes": 17741,
      "artifact_id": "artifact-52f673e6db7846d4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9675eb12833140f1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Runtime.CompilerServices.Unsafe.xml.meta",
      "sha256": "d88c236fdc1ef62344e3dbae4d7c4c66c34ad6ae2e790b233564c3327aff55ee",
      "bytes": 158,
      "artifact_id": "artifact-9675eb12833140f1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-889f5c524ccd4914": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Text.Encoding.CodePages.dll",
      "sha256": "93da6a111239a3804da2efd6a6faa92bf5cbfe3b2b079ad3c04be643179f4088",
      "bytes": 758664,
      "artifact_id": "artifact-889f5c524ccd4914",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-df569c0c462e4a6d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Text.Encoding.CodePages.dll.meta",
      "sha256": "258a946162f3648e1e22050ecde176e296d77da89516be06d8e17713beb78559",
      "bytes": 932,
      "artifact_id": "artifact-df569c0c462e4a6d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-01e68465d886483c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Text.Encoding.CodePages.xml",
      "sha256": "45c2e87787ea8aca93fe1d670bec6e7c7f7fa1ff2ba8ddbbd61b9d3180081d89",
      "bytes": 2047,
      "artifact_id": "artifact-01e68465d886483c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c13b190740014543": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/System.Text.Encoding.CodePages.xml.meta",
      "sha256": "017745d55bff2b1925d8399996910a7b8738e45519f41843ad32739bdc3f260e",
      "bytes": 158,
      "artifact_id": "artifact-c13b190740014543",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4e722eaa7e544321": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/WXTextureTools.exe",
      "sha256": "b3087b50f2616dada9ecfa3748bcc483a52acc329e363da921fd297e3cf8d2b6",
      "bytes": 137728,
      "artifact_id": "artifact-4e722eaa7e544321",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-272eb7e157a64cd4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/WXTextureTools.exe.config",
      "sha256": "e438dc9a13e51181843a433f54e47e9b49e14a80efa5f969397cf54347ff29de",
      "bytes": 540,
      "artifact_id": "artifact-272eb7e157a64cd4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-16ae8ba2a7be40ce": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/WXTextureTools.exe.config.meta",
      "sha256": "a67372ea9d814b5131f34cdd3a47609590ea2f065213f853792a0f8133f282be",
      "bytes": 813,
      "artifact_id": "artifact-16ae8ba2a7be40ce",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cb206842475d4328": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/WXTextureTools.exe.meta",
      "sha256": "beadcd4edb3a32b5d4e3d4950e507c9aaabeb2e12f191cc8927455edd4d4b40a",
      "bytes": 155,
      "artifact_id": "artifact-cb206842475d4328",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bc325887f6d74261": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/wxlog.dll",
      "sha256": "7557aeaab63d8e3315c6466afd8dae98d7473cb079a69b3e2acda8c65937e469",
      "bytes": 12288,
      "artifact_id": "artifact-bc325887f6d74261",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-81e8746c3c8b4753": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/Release/wxlog.dll.meta",
      "sha256": "ea83e3ebd2b7519d9f3904e0031defafdc4eadd0f88f1ec360948e55dcfdcf73",
      "bytes": 932,
      "artifact_id": "artifact-81e8746c3c8b4753",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a0fe3f0523704623": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/classdata.tpk",
      "sha256": "4d7611e9bf424764824861e678616c234fd06b4ec49a9215f4decfedb0e49398",
      "bytes": 1134474,
      "artifact_id": "artifact-a0fe3f0523704623",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8623b484e4ac4886": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/classdata.tpk.meta",
      "sha256": "0a5b0ba313ebdf3860ef921c8876837e6030bb3bc758a791ba30736f66cba635",
      "bytes": 155,
      "artifact_id": "artifact-8623b484e4ac4886",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2063f33513c34b27": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/slim.conf",
      "sha256": "0a282ee44ffe3c60595502ca4be5b77b6fdeab2ac6c5b5a97cc00382d9fe7f89",
      "bytes": 4976,
      "artifact_id": "artifact-2063f33513c34b27",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-88d779e0a13b4f70": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/TextureEditor/slim.conf.meta",
      "sha256": "73798739480aabf63a9dbb9029ca302d3cf6fa6ebdf95a46cb4d0ed945ea4e52",
      "bytes": 155,
      "artifact_id": "artifact-88d779e0a13b4f70",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-564d355fa5174d2f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXAssetPostprocessor.cs",
      "sha256": "727b5dd3c9f51840d9aeac7ca2cccf1223eef00c8577cb272795a96de0470244",
      "bytes": 5521,
      "artifact_id": "artifact-564d355fa5174d2f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-81abdb3d8ac648ea": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXAssetPostprocessor.cs.meta",
      "sha256": "9fcc1bb398b282908e62926ef0f48da437eb70b33866b2817fa50bef343d921f",
      "bytes": 146,
      "artifact_id": "artifact-81abdb3d8ac648ea",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6054b0cf5fe24b17": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXConvertCore.cs",
      "sha256": "b096efa0ed37dbf2ef94d06242991a3e74d350308043f0902eb98851bbd39ef7",
      "bytes": 107026,
      "artifact_id": "artifact-6054b0cf5fe24b17",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-38505b46ef1141f3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXConvertCore.cs.meta",
      "sha256": "7739d6a7f11c297b93900502a5217036bda1f046a8d625c0f24b90f0466cd301",
      "bytes": 243,
      "artifact_id": "artifact-38505b46ef1141f3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4c1f7dcbe1fe49f3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXEditorSettingHelper.cs",
      "sha256": "e2dd228c46ecf62299cf49490cd52fdc940b96fd1f2d87a39617bcc28e0314d9",
      "bytes": 70652,
      "artifact_id": "artifact-4c1f7dcbe1fe49f3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-41edc41b52b54e80": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXEditorSettingHelper.cs.meta",
      "sha256": "67232ee762fa708edd6940ad3506f3917f75fc3c56cfa6bea3b1f6ed4561f63e",
      "bytes": 243,
      "artifact_id": "artifact-41edc41b52b54e80",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ea11d97166794339": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXEditorWindow.cs",
      "sha256": "900590d5c421d0e658d18a1a1b8a345006d7196cba265cd94395a6a1a8ef12fe",
      "bytes": 1222,
      "artifact_id": "artifact-ea11d97166794339",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0877223c46f64187": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXEditorWindow.cs.meta",
      "sha256": "971bbe65af3732b524ca2712dc82508f65e841a9f9991273ed19c1233ac4e631",
      "bytes": 243,
      "artifact_id": "artifact-0877223c46f64187",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fbb0ae947b2545a1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXExtDef.cs",
      "sha256": "68e27312cf0a519ddaa86dac8702f97feb7c4d0eb00db3c9209e81d41daca6ed",
      "bytes": 5364,
      "artifact_id": "artifact-fbb0ae947b2545a1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2ffd425c064142a6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXExtDef.cs.meta",
      "sha256": "2b9372bad36e65f18b860667aac6774e2e09dacf6602fc719b1616f177401240",
      "bytes": 243,
      "artifact_id": "artifact-2ffd425c064142a6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-893c5ab3820b4bfa": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXMultiPackageMergeWindow.cs",
      "sha256": "58c07d1a8ab8b7a893147be9731b6e40a0730cc78ee67815d714cf0aa980632d",
      "bytes": 13413,
      "artifact_id": "artifact-893c5ab3820b4bfa",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a5393964eff648c5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXMultiPackageMergeWindow.cs.meta",
      "sha256": "d2362f6ae909156d73dfd5532a94031536c586ecbd86895544cd5b8ca51c9318",
      "bytes": 243,
      "artifact_id": "artifact-a5393964eff648c5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d440705401654678": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXPluginVersion.cs",
      "sha256": "8e60192be5e56e49efff9543b76208652e4f3d4ac1cdce718b4f4a7d75546302",
      "bytes": 295,
      "artifact_id": "artifact-d440705401654678",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-006eb2642bc2469a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WXPluginVersion.cs.meta",
      "sha256": "617bf7ed4337087ba9c002ef1e00beb7d2fd37545b74d51bb7e17a802a0c7de8",
      "bytes": 243,
      "artifact_id": "artifact-006eb2642bc2469a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-33a489617d504652": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WxWasmSDKEditor.asmdef",
      "sha256": "1d3f4e9b0ab4646df0c0448935ed4302152f26b3a32c55bdb2a683e2108eff9d",
      "bytes": 529,
      "artifact_id": "artifact-33a489617d504652",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-dd7b87237d1c4181": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/WxWasmSDKEditor.asmdef.meta",
      "sha256": "3feedcb2bce06dee95eea1e7c15e97203041b8d5685627c7948b7ac27ac1dfa4",
      "bytes": 166,
      "artifact_id": "artifact-dd7b87237d1c4181",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-74b86741eacf431b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/convert.exe",
      "sha256": "ae0eb6b1c5a20c3482e60b49a3e4468ad6f8e9dc71f7efb3f5ed5a11f6aa9769",
      "bytes": 12839936,
      "artifact_id": "artifact-74b86741eacf431b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f1894773578a4909": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/convert.exe.meta",
      "sha256": "d5779b05a68f7c01ef2a8b80d355e60c8e6d8e21d93a8d8aea1c6c82ceab723f",
      "bytes": 155,
      "artifact_id": "artifact-f1894773578a4909",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-531d7fab0218478f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/wx-editor.dll",
      "sha256": "a7569ee4dd0526b5a9841092d4bdb73fad8117f1cef3ae300b20c328bd24e56a",
      "bytes": 282112,
      "artifact_id": "artifact-531d7fab0218478f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-383c40ae3f20408a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/wx-editor.dll.meta",
      "sha256": "3fcd6fca2c8c6ef55732e1d5f7f40f6a6b41981427f982b78624fab9eadf789f",
      "bytes": 645,
      "artifact_id": "artifact-383c40ae3f20408a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1e6c6a0423da4c70": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/wx-editor.xml",
      "sha256": "bcd92b6606c7dc1e1eed112f738b7c099c6b07ef68baaf0619c389909cff8259",
      "bytes": 45787,
      "artifact_id": "artifact-1e6c6a0423da4c70",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1ac63b709a814c47": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Editor/wx-editor.xml.meta",
      "sha256": "19db4632b0d978738e9794846c5ae415f5a55c3f4232fb19ac8de1db9d644589",
      "bytes": 146,
      "artifact_id": "artifact-1ac63b709a814c47",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ee1cc1a01d6647cc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/LICENSE",
      "sha256": "9223170188b2dfafcafc2b89d96da21ffa8396fd97485671fc579756999aeb21",
      "bytes": 1074,
      "artifact_id": "artifact-ee1cc1a01d6647cc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8cef04e6c0304268": {
      "path": "Unity/Packages/com.qq.weixin.minigame/LICENSE.meta",
      "sha256": "c05ca1d54e899ac55651ca17c9cc86e5962d02fb9252726eecfadad6554fb009",
      "bytes": 155,
      "artifact_id": "artifact-8cef04e6c0304268",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-98d9b6f3ae2d42ee": {
      "path": "Unity/Packages/com.qq.weixin.minigame/README.md",
      "sha256": "74dbf8cfb3c1c50559d00637960c25ec53cbb2018b179b1b67c72ac5e56c3b3e",
      "bytes": 873,
      "artifact_id": "artifact-98d9b6f3ae2d42ee",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-94caddb9cb2742e6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/README.md.meta",
      "sha256": "b777b92e82cc921c563aab89cf1c1ab76d3ca72347e44abcf246513c0dd279d8",
      "bytes": 158,
      "artifact_id": "artifact-94caddb9cb2742e6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-10fd0ac03d1f4a84": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime.meta",
      "sha256": "cce01f53953522b7800f9beac790ebf8309413985671ef07f6ff8dfc4ab20eb7",
      "bytes": 172,
      "artifact_id": "artifact-10fd0ac03d1f4a84",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e97a17abb0ac464a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/DisableKeyboardInput.cs",
      "sha256": "62c5f624ea24c1d7a9d4297bd2fda820240c0961d52d7fdfc9eb2bf764413ca2",
      "bytes": 601,
      "artifact_id": "artifact-e97a17abb0ac464a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4f3b5d5627ec4e7c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/DisableKeyboardInput.cs.meta",
      "sha256": "e7d944dc27dee231654396f5313f793c23155028a28907047e32609595a78b56",
      "bytes": 146,
      "artifact_id": "artifact-4f3b5d5627ec4e7c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f7ef98b3a0ab48ee": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/HideLoadingPage.cs",
      "sha256": "78ffc1485b6e992299c331bfa5b4f5b4538e6a0f2e053c17dff06e7479b9d2d5",
      "bytes": 764,
      "artifact_id": "artifact-f7ef98b3a0ab48ee",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9400f72a833d4a6c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/HideLoadingPage.cs.meta",
      "sha256": "41f8b0c804f0cb31c484775a678bb796704bb53c1563163b7117019d160e8d0d",
      "bytes": 243,
      "artifact_id": "artifact-9400f72a833d4a6c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6950155e778f4578": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins.meta",
      "sha256": "5a332633226339fe8e8f9a162df1a9141cd5efe0c3c3668d38350175fc2e970b",
      "bytes": 172,
      "artifact_id": "artifact-6950155e778f4578",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6b2f9f6d4ae94753": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/AES.jslib",
      "sha256": "c2bfa77b6efedc5a788ec803f76afce4206a2c78c3004b01b1125eb2c6a277ca",
      "bytes": 3055,
      "artifact_id": "artifact-6b2f9f6d4ae94753",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a1d85e314e8348f6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/AES.jslib.meta",
      "sha256": "c52aa8372ed3b535c9b6ee757b12d7c68abd893f26483a37bd71f1389d34a85c",
      "bytes": 1029,
      "artifact_id": "artifact-a1d85e314e8348f6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8118bcda7de543c5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Gyroscope.jslib",
      "sha256": "bd4296b8c4c4c8a2ade3a8439ce6c2766c33bd7ecb0dec854d356b58fa727347",
      "bytes": 1015,
      "artifact_id": "artifact-8118bcda7de543c5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d0dc2d1d9556493b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Gyroscope.jslib.meta",
      "sha256": "41399f074ac5a6ffdda9b3da0e59925680f86d85ba201eed1ee5a452e9f0764d",
      "bytes": 1456,
      "artifact_id": "artifact-d0dc2d1d9556493b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-df5ccae06f2a4b57": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LitJson.dll",
      "sha256": "c1d86a5fc075734207fc08c6ef8a7db2a8f8f3c3c82c1d15f1972a0cab2a0c04",
      "bytes": 60416,
      "artifact_id": "artifact-df5ccae06f2a4b57",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d6aea6e0ccd34faf": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LitJson.dll.meta",
      "sha256": "35fdaa9834e1f562d5e7ee80473ae11c3b9e14aa95fc05abd3fa28a40173fc7f",
      "bytes": 645,
      "artifact_id": "artifact-d6aea6e0ccd34faf",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d2c16ba82f744041": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor.meta",
      "sha256": "2d2be814b26658798bbcb5d2ef801f1dbea88864e017c5f1a800f59378e66b62",
      "bytes": 172,
      "artifact_id": "artifact-d2c16ba82f744041",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ce15b8253c9046ec": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_501.c",
      "sha256": "2755cabd191bde734a9e1a13b48542f820564077fe08331e8456a6850eb631e2",
      "bytes": 3010,
      "artifact_id": "artifact-ce15b8253c9046ec",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-252542c9fedd4570": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_501.c.meta",
      "sha256": "d3f1d42b5bae5b0a0320389ebec142a468f84fa2e97c0cb2166d2d4bf40f3e89",
      "bytes": 1363,
      "artifact_id": "artifact-252542c9fedd4570",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-048d4bb51e834cda": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_503.c",
      "sha256": "bb8d12e4bf0d6cd81f057f013300118721bcc8e83d17793155800997d90d56ce",
      "bytes": 3596,
      "artifact_id": "artifact-048d4bb51e834cda",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0ea4d6dfd6294cab": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_503.c.meta",
      "sha256": "1e1deddcfb7089b77bf48243655b2be9860729aae3025f17c17fa06745676b7c",
      "bytes": 1363,
      "artifact_id": "artifact-0ea4d6dfd6294cab",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-11755a36ee3746b0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_comm.c",
      "sha256": "7413519d7494c5ef23608aff43711051373efc7b1acc3c5159ddbaedd47108ba",
      "bytes": 1094,
      "artifact_id": "artifact-11755a36ee3746b0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e99f099df7214812": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_comm.c.meta",
      "sha256": "a08136064708e0c3e47659689fa949202c4fd04447b7f7cf3bb63fb4ed965d4b",
      "bytes": 1363,
      "artifact_id": "artifact-e99f099df7214812",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5d85c0ba14a2494e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_import.h",
      "sha256": "ef1f90889f85eb4d759ddabed7fd31b6c8a47df4eb61936019d2d06cfa0b85dd",
      "bytes": 822,
      "artifact_id": "artifact-5d85c0ba14a2494e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-52240bc526f14853": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/LuaAdaptor/lua_adaptor_import.h.meta",
      "sha256": "1530168e5454f377c92c2bb3a7c1ab7b635c1311a35e8df74180e0dac4d9a6c8",
      "bytes": 1363,
      "artifact_id": "artifact-52240bc526f14853",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d8314b876a0d481b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/MD5.jslib",
      "sha256": "0c3432a05ef50a2137927deab8eec3b5857d72991b09a54bef16562a5dc8411f",
      "bytes": 320,
      "artifact_id": "artifact-d8314b876a0d481b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-52598ea8eb7e48a5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/MD5.jslib.meta",
      "sha256": "87a25c61455599175c91c059425524b851a4c51cee4c82fea48214f4dc5399c4",
      "bytes": 1029,
      "artifact_id": "artifact-52598ea8eb7e48a5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c2822167b1fb4687": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-Call-JS-Old.jslib",
      "sha256": "0bf5cb0e3204a613bf9cdcb741f8b7cb4f588a05a883fb786d4abd1728e60d6a",
      "bytes": 52626,
      "artifact_id": "artifact-c2822167b1fb4687",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-da65d10cdb734959": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-Call-JS-Old.jslib.meta",
      "sha256": "4d8ddb33ff8cf4ebd30743bdb695b1bacb91f61bf2ebfaf3bb2daa833f674d18",
      "bytes": 1375,
      "artifact_id": "artifact-da65d10cdb734959",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-24768ce3eeb54070": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-Call-JS.jslib",
      "sha256": "14da4f1bf1d0a98ce3f6ad379e919c25fde0ee7197d9fec4e10ccf2fc4afb7df",
      "bytes": 10233,
      "artifact_id": "artifact-24768ce3eeb54070",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c6c2b3929a2141f7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-Call-JS.jslib.meta",
      "sha256": "42b148bf592c75ab1eb594cec5a1b4fd5db630d9357ce460b42c3ec43f9b32ef",
      "bytes": 1375,
      "artifact_id": "artifact-c6c2b3929a2141f7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2dbef6e351e949ae": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL1.jslib",
      "sha256": "50872d7e076fdeb72227180c2c6cb172cf35f0693f3eb7f8ae664549dd7f598d",
      "bytes": 14567,
      "artifact_id": "artifact-2dbef6e351e949ae",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-61d245524f1c46a1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL1.jslib.meta",
      "sha256": "fc8191193178d71dda4f346ff7c15ba3faf8dd0b115d12d58f2f794ac04c602d",
      "bytes": 700,
      "artifact_id": "artifact-61d245524f1c46a1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b3a37afed8694c72": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL2-Linear.jslib",
      "sha256": "8528581a78713521112885ab7224c4ba7aab98ee72d696c2b81ef7b2ff74225c",
      "bytes": 14756,
      "artifact_id": "artifact-b3a37afed8694c72",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4eb561dc0feb4094": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL2-Linear.jslib.meta",
      "sha256": "03aa502c4d2361f1a7f849164693dd73868cc67bfdb62b15cf378f028acc9fb5",
      "bytes": 604,
      "artifact_id": "artifact-4eb561dc0feb4094",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f64edc1aa6184f6c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL2.jslib",
      "sha256": "bc6a0c6c6451ac5fb5bb20484d42d418f88d27a6525833ea8ab55a68c89f4d4b",
      "bytes": 14567,
      "artifact_id": "artifact-f64edc1aa6184f6c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d8f23f7ccd014c6b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/SDK-WX-TextureMin-JS-WEBGL2.jslib.meta",
      "sha256": "b33f2b1051167dd32e1acb446747d5e034475489017f1b2c19a0464d5090f0a9",
      "bytes": 604,
      "artifact_id": "artifact-d8f23f7ccd014c6b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8eac92a55bdf4ac5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/TCPSocket.jslib",
      "sha256": "3c33d91b8fa4c1b96f2dd7c9656028074ec30eb8f0b66ea6721b96ac301efa04",
      "bytes": 2637,
      "artifact_id": "artifact-8eac92a55bdf4ac5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5fd6782222dc4126": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/TCPSocket.jslib.meta",
      "sha256": "747e4b3fde19be5f6a701fe8a1d23f736af5b7c3cb1a1f0564599bf40dba46d3",
      "bytes": 1456,
      "artifact_id": "artifact-5fd6782222dc4126",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b79528de147c4a77": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Touch.jslib",
      "sha256": "56b6dca0ac799535e486d51a23b15425ccc2d44b601a1727829e9e749fc30e6a",
      "bytes": 1206,
      "artifact_id": "artifact-b79528de147c4a77",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0abb68634b534a5e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Touch.jslib.meta",
      "sha256": "dc7d270609af4b63e17cd5765c6f15c3cdfc0e2344b7f9d0f84eb32ee1316ddb",
      "bytes": 1456,
      "artifact_id": "artifact-0abb68634b534a5e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9105cc4908cd4e94": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/UDPSocket.jslib",
      "sha256": "c21cb749084ad1adb91feb3d5740ef14c160abeb775544aa2c73904e651696d0",
      "bytes": 3024,
      "artifact_id": "artifact-9105cc4908cd4e94",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-de764966dc554e99": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/UDPSocket.jslib.meta",
      "sha256": "ce7d96a71e917bdb11eeeac6b3677296ec197c594e191153ee181e2a539faf81",
      "bytes": 1375,
      "artifact_id": "artifact-de764966dc554e99",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-13580b24d17c4233": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Unity.FontABTool.dll",
      "sha256": "88c7409cefe05324bd126c1c164884c9b510e926c156b938e1fcb5c8956d0d96",
      "bytes": 20992,
      "artifact_id": "artifact-13580b24d17c4233",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7a9d4580a94941b9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/Unity.FontABTool.dll.meta",
      "sha256": "5ab0e3a12b7821314e8a30033fc0bd088637988d3128c10e7473ea2f8e9d0517",
      "bytes": 645,
      "artifact_id": "artifact-7a9d4580a94941b9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3cdaf9549e0b4097": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WXAssetBundle.jslib",
      "sha256": "2f60c79e9271d8ac3abe9c6bea016e38e2f09d7b54b5bbbfe2bdef461ae30b89",
      "bytes": 15746,
      "artifact_id": "artifact-3cdaf9549e0b4097",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-467ddb85854746cc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WXAssetBundle.jslib.meta",
      "sha256": "06c300062c1fad0390ed3fe9d3e4de3fe60ab5ae11ed4e979d08817a375f40d4",
      "bytes": 1456,
      "artifact_id": "artifact-467ddb85854746cc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0a04bc13d1664dae": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WXTouch.jslib",
      "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "bytes": 0,
      "artifact_id": "artifact-0a04bc13d1664dae",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ab97e617e70142f7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WXTouch.jslib.meta",
      "sha256": "7172fc83eb341634269ba258a937687b02449607522cd4c377ee0a94a83d0868",
      "bytes": 1400,
      "artifact_id": "artifact-ab97e617e70142f7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-750a3fef1a244ea0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WxGameDataMonitor.jslib",
      "sha256": "5e46ea8a1b4af028abbbddff9abd15292d1ffb02ed981ceac60103eb3e5722c3",
      "bytes": 2941,
      "artifact_id": "artifact-750a3fef1a244ea0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c39a7cb3e4b14bf5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WxGameDataMonitor.jslib.meta",
      "sha256": "a5750ea75db022dc49fec67ea229ef29d35925d34624d87436a35f0f7ccc385b",
      "bytes": 1456,
      "artifact_id": "artifact-c39a7cb3e4b14bf5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5aa7af32aa064a8b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WxPerfJsBridge.jslib",
      "sha256": "7b2fb61fde0a8e3ae58600090f83cad2cc8daa07fcc2b599604b7478cb0494d7",
      "bytes": 6223,
      "artifact_id": "artifact-5aa7af32aa064a8b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-26e5fa6c78e249df": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/WxPerfJsBridge.jslib.meta",
      "sha256": "596add03ac1430c0b66205617dce38e674e6e26836e16fb7f28435602821d77f",
      "bytes": 1456,
      "artifact_id": "artifact-26e5fa6c78e249df",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e8f141b7309546f3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/crypto.jspre",
      "sha256": "737ae84200b8f41f0d8c4015f7275ec1649c73c7fec550423f7c7039476f6e12",
      "bytes": 88752,
      "artifact_id": "artifact-e8f141b7309546f3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ed44b430a16a4c7d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/crypto.jspre.meta",
      "sha256": "eb7862e432fc8b42bdfc697362845e570d5bef4cdc4b27b71a0c2d5ac10ce32f",
      "bytes": 1029,
      "artifact_id": "artifact-ed44b430a16a4c7d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9103b9436d4b4181": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/dumper.jslib",
      "sha256": "1946d42325f777e06e4909ed983a510b1ae8a360fa5af837645e541f41ccdb80",
      "bytes": 1300,
      "artifact_id": "artifact-9103b9436d4b4181",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ac124818b45844d4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/dumper.jslib.meta",
      "sha256": "c8bdc6be790181e390901a6106512b0ab8e529911d83822ad14b9be25acc98b7",
      "bytes": 1375,
      "artifact_id": "artifact-ac124818b45844d4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a6454ed446a64f88": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx-mt-wasmex.a",
      "sha256": "4a93905a997312d991eb9a271583218a64c5312b14999c8387d14a545977ec4a",
      "bytes": 4334640,
      "artifact_id": "artifact-a6454ed446a64f88",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-82440ac8a5a6471d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx-mt-wasmex.a.meta",
      "sha256": "33896b6038079e610344a68adcf54ce56371721f3be81f6d132a25eaeb0d5246",
      "bytes": 1801,
      "artifact_id": "artifact-82440ac8a5a6471d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0c77b32f9d7948e1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx-mt.a",
      "sha256": "3570aca33bd7d253fb00cd44109328699e48c79b05c8f42db7989160b1e06753",
      "bytes": 4305308,
      "artifact_id": "artifact-0c77b32f9d7948e1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c77ed1f051e64bfd": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx-mt.a.meta",
      "sha256": "b59ff0b3d95f3dfe77ca9937f777315116a97a655f7573401023a059f955c98a",
      "bytes": 1801,
      "artifact_id": "artifact-c77ed1f051e64bfd",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fefc49ebf7ec4922": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx.a",
      "sha256": "c9bdf98461da257f1aa8d13dd6c7f2233890519aba6702acdd458b946143ff27",
      "bytes": 4313240,
      "artifact_id": "artifact-fefc49ebf7ec4922",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-76150d0e85714200": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx.a.meta",
      "sha256": "4efd49e21024b884b2c264f78f4535deaf1627b7b524ea7b3af5bb1a6e9dba3f",
      "bytes": 1380,
      "artifact_id": "artifact-76150d0e85714200",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6f470bfd9c2f45b1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx_2021.a",
      "sha256": "65ed8fd8e643bf441197e62126770ac6f57af5284fdd443f709cdb8091678f9c",
      "bytes": 4369042,
      "artifact_id": "artifact-6f470bfd9c2f45b1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8e1282be590241c1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/libemscriptenglx_2021.a.meta",
      "sha256": "e12219c82fe3d338bdd6144d8e8d599fb42300bc237239bb11ee73f029eff526",
      "bytes": 1801,
      "artifact_id": "artifact-8e1282be590241c1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-edc3a59e895e4391": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/link.xml",
      "sha256": "acee5d8011af33a39bc32a1a810c1ece66ab47bdffcc94b21b0a4ab5ca81483c",
      "bytes": 160,
      "artifact_id": "artifact-edc3a59e895e4391",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a2e22fbac4fb4c1e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/link.xml.meta",
      "sha256": "ddbb198fbde41fbfe20bc7938a844e620760bcd8f7fa6a177b9be6aff9644d22",
      "bytes": 158,
      "artifact_id": "artifact-a2e22fbac4fb4c1e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7c88eeb09e9a41a3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-perf.dll",
      "sha256": "e555891264c35b19637a218582460e2f44e8788746666e9ef389509e6f695f49",
      "bytes": 55808,
      "artifact_id": "artifact-7c88eeb09e9a41a3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4c4a7af16e5841a3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-perf.dll.meta",
      "sha256": "143e1d2ddee2cdafdef6c01a59264100c1190b3fa16a17259ecc8118466bb781",
      "bytes": 1482,
      "artifact_id": "artifact-4c4a7af16e5841a3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e1a21395ae594e1d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-perf.xml",
      "sha256": "ed1353b6f108983c8ff72c45a5d155eae2a3870e3d9116cd2cf62588d35b0063",
      "bytes": 1941,
      "artifact_id": "artifact-e1a21395ae594e1d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d7bec6fae8164cbc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-perf.xml.meta",
      "sha256": "951c5bcc3328103f7061b8473af05abb32884dde0b9b527ed292a082b569b692",
      "bytes": 158,
      "artifact_id": "artifact-d7bec6fae8164cbc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a664366bfe7c49ef": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime-editor.dll",
      "sha256": "f65a17ff5138a028a2de8a6033e6224e904c4ce13307c65f3d8682b997c7d5cd",
      "bytes": 275968,
      "artifact_id": "artifact-a664366bfe7c49ef",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7fa01bb69ad34635": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime-editor.dll.meta",
      "sha256": "119a9e4ebd0771d11f5e81413a7cd2d4ce3966f64a9783e087b17be136c4934e",
      "bytes": 1775,
      "artifact_id": "artifact-7fa01bb69ad34635",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-41249f6fba67429e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime-editor.xml",
      "sha256": "af19e7fbb91ac07af3cee6481063803af6d4895875e0b7e28e6c1046a3920d08",
      "bytes": 427720,
      "artifact_id": "artifact-41249f6fba67429e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6d2486c4a9ba4233": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime-editor.xml.meta",
      "sha256": "c4769557f17901a733b4cde647844a10f8590cbb592ced1e68562200968c5483",
      "bytes": 146,
      "artifact_id": "artifact-6d2486c4a9ba4233",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-28928fbc0b8a401c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime.dll",
      "sha256": "3fcea021200e27a3fe392833e544d96a20434292a5ecf7263c59e79a6d00d4c5",
      "bytes": 250880,
      "artifact_id": "artifact-28928fbc0b8a401c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-655b8500d4f44a85": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime.dll.meta",
      "sha256": "4c9e10dad4b91d1a189eb07c5edec135d122d6779c50c1e6f605c6c61fed42c0",
      "bytes": 1983,
      "artifact_id": "artifact-655b8500d4f44a85",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-db72d0fe27174656": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime.xml",
      "sha256": "17765fe4dad06169883cb034bb9448877101378f53ac0c29bacc9ce6b1342849",
      "bytes": 427965,
      "artifact_id": "artifact-db72d0fe27174656",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c309fc203db9424b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx-runtime.xml.meta",
      "sha256": "68d93fc8ab5b31dd6631a3d1d17e1e493ffa224011a364ee4238315d1ef149be",
      "bytes": 146,
      "artifact_id": "artifact-c309fc203db9424b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cf8cde1b105348ab": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx_perf_2021.a",
      "sha256": "e2ee123bd1e1514fb03e1eaa73e5ea5301b8f312662f631bdeccc430bf86a80b",
      "bytes": 2756530,
      "artifact_id": "artifact-cf8cde1b105348ab",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2d93581ffd104012": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx_perf_2021.a.meta",
      "sha256": "67ae92e387e933dbea29c6f940578e7b768504c06934f9cfa1f162926ab1f4bf",
      "bytes": 1807,
      "artifact_id": "artifact-2d93581ffd104012",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f83f6c02c5a8476a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx_perf_2022.a",
      "sha256": "a57690e4f4c897de51fdf7152293800320617ec0a4272e9380806f29786ce753",
      "bytes": 2649934,
      "artifact_id": "artifact-f83f6c02c5a8476a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2770004aec8b4eee": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/Plugins/wx_perf_2022.a.meta",
      "sha256": "d2fdb1716d4c3c774124c1fc259717b84f1dfec3c0301ad9fe0b8e3c6a30ee32",
      "bytes": 1363,
      "artifact_id": "artifact-2770004aec8b4eee",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-814491389a1d4491": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WX.cs",
      "sha256": "7780554641e592282908dc5d15ac8e75b5cabf2f262d4e8062c71d66435c4526",
      "bytes": 223011,
      "artifact_id": "artifact-814491389a1d4491",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3675299b26324f7f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WX.cs.meta",
      "sha256": "e415f7b7a9c6a0dcf4d0a4648149f309fc73fda563841584a018bea7a3cbea07",
      "bytes": 243,
      "artifact_id": "artifact-3675299b26324f7f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a9395bd6673d404c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXBase.cs",
      "sha256": "3649f43a8ba0d99487994b9edaa5dffc49e3c41a7eb0821c3234a008fbe8f20b",
      "bytes": 48909,
      "artifact_id": "artifact-a9395bd6673d404c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e8a59dd469824260": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXBase.cs.meta",
      "sha256": "bc019e8d0fd5da510a1a26f69274eca30ce4557c04fc8ddb61370453eee57344",
      "bytes": 243,
      "artifact_id": "artifact-e8a59dd469824260",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e6d2203cb5784a9e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXProfileStatsScript.cs",
      "sha256": "4272d299ab608c397b9432a0700fa18fc9d8042133693b247b68c3c64cf51251",
      "bytes": 18497,
      "artifact_id": "artifact-e6d2203cb5784a9e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-81074bc989f148cc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXProfileStatsScript.cs.meta",
      "sha256": "93e34a09c9ef172cda75e3da18e9dd02b814f5aabf2ed10257618dd524066956",
      "bytes": 243,
      "artifact_id": "artifact-81074bc989f148cc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8992c054ef6540ba": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXRuntimeExtDef.cs",
      "sha256": "aedec5c2055416f57edb9fe04599b63eefee00616ebd2471fa6b609fbe94c7f9",
      "bytes": 4246,
      "artifact_id": "artifact-8992c054ef6540ba",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9b83fa65f37e47ba": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXRuntimeExtDef.cs.meta",
      "sha256": "7674f27cd1aa4641eda55532323384df7f65b6b4df68b7fe43d0f7235b0d877c",
      "bytes": 146,
      "artifact_id": "artifact-9b83fa65f37e47ba",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fe7bdbe008154f09": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXSDKPerf.meta",
      "sha256": "45177aed7c785bf1bdec6f066cd2cea78ad3f688f1ce038366217f62181d0594",
      "bytes": 172,
      "artifact_id": "artifact-fe7bdbe008154f09",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-16a1ecabb506486b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXSDKPerf/WXPerfEngine.cs",
      "sha256": "231dccd4ffd5da40208e0c3b730bd8d16f4e9d8651058d671c9e277c8e1751a1",
      "bytes": 8154,
      "artifact_id": "artifact-16a1ecabb506486b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6e829b183ccb4852": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXSDKPerf/WXPerfEngine.cs.meta",
      "sha256": "3bee556d73d9a6cd334469c29ce2b6b07900bb1c29f1e3bb5f91d4f365c784bb",
      "bytes": 243,
      "artifact_id": "artifact-6e829b183ccb4852",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d55dc39dd5784747": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXTouchInputOverride.cs",
      "sha256": "09980d6c4ee8c0aea32c1740401a9fd557fefda89647ecfab895ac3bcb9c6902",
      "bytes": 8833,
      "artifact_id": "artifact-d55dc39dd5784747",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ffa43e9d0ebb46dc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WXTouchInputOverride.cs.meta",
      "sha256": "8d503d02bca41229475dd9ebd5c3945df75e72f363c75433edad75250daa09b3",
      "bytes": 243,
      "artifact_id": "artifact-ffa43e9d0ebb46dc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-26f8eccfb3284d6a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WebAES.cs",
      "sha256": "ffbc138771bf2c6be130db707ea7d1f1087ca5aa4f06071f3926d026a88008ed",
      "bytes": 3950,
      "artifact_id": "artifact-26f8eccfb3284d6a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cf4f95c2f9db489a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WebAES.cs.meta",
      "sha256": "1cdc3b73e7d9e89c26067bcb5e717239bacbec0270b3918bc1df036022002d38",
      "bytes": 86,
      "artifact_id": "artifact-cf4f95c2f9db489a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-87b4113a046d48ea": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WebMD5.cs",
      "sha256": "f3d430825013c9de8b902deef8ba7c4a0647c0737884c398c4a409dfa567d6ec",
      "bytes": 912,
      "artifact_id": "artifact-87b4113a046d48ea",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6e54bcf5a6814752": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WebMD5.cs.meta",
      "sha256": "8b6cba93f17ff78de498a39280ceb584cae3dbc4d9c6ab051d0ce35be307ffb6",
      "bytes": 86,
      "artifact_id": "artifact-6e54bcf5a6814752",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a85353b2e42b4c4a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WxWasmSDKRuntime.asmdef",
      "sha256": "dd9c976a29b94c20924ed2f5f4ca938d3963f60c08fed783d5c83c9e942167e4",
      "bytes": 366,
      "artifact_id": "artifact-a85353b2e42b4c4a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5f212e6f8a9f4f24": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/WxWasmSDKRuntime.asmdef.meta",
      "sha256": "43d2c99bbde5f618fd787686e9e2c1580f9e36fcb9c12b12e8a66b1203d1b6a6",
      "bytes": 166,
      "artifact_id": "artifact-5f212e6f8a9f4f24",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7494601614964faa": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default.meta",
      "sha256": "a57c0bcc7f17754df09d5b3d4f673e599490c17681553d5ae87df5b38cc84479",
      "bytes": 172,
      "artifact_id": "artifact-7494601614964faa",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8fd98e4997484578": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/check-version.js",
      "sha256": "599b2656f6b77962c358e0f4be6e1f3e6536a7eaa804ce76a58d028a0d007dc8",
      "bytes": 7689,
      "artifact_id": "artifact-8fd98e4997484578",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ba5af8f136504b5b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/check-version.js.meta",
      "sha256": "5961a8a9d3508ed476604153b20d4c405b8cb431570a65e048e38ecc9a1e148f",
      "bytes": 166,
      "artifact_id": "artifact-ba5af8f136504b5b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-573cbef2ef8249d3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/data-package.meta",
      "sha256": "c87d5d3672ddce321df2ab77ca18669ab0472d50594e1b368dc6e96ff328ef6f",
      "bytes": 171,
      "artifact_id": "artifact-573cbef2ef8249d3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7cfe09a07ef84d99": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/data-package/game.js",
      "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "bytes": 0,
      "artifact_id": "artifact-7cfe09a07ef84d99",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-beb6aa7cce974f71": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/data-package/game.js.meta",
      "sha256": "518b4d5bd8665c2ee916342e097ba92cc4c6744df490440ffc3fa54259a3f132",
      "bytes": 166,
      "artifact_id": "artifact-beb6aa7cce974f71",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e7f82a8c42924ddb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/events.js",
      "sha256": "0e8d53bd4cd51e8833018558abf50a27e28c6294faf16da100142e7b27a41681",
      "bytes": 1545,
      "artifact_id": "artifact-e7f82a8c42924ddb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-20792ed4ab67481d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/events.js.meta",
      "sha256": "ced94a33e69c061cd46b9e8370a07394fc691e396429cc18c46a9b4afac39a7b",
      "bytes": 166,
      "artifact_id": "artifact-20792ed4ab67481d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cb43ae0737de4abe": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/game.js",
      "sha256": "ff1ed459ad39ed127a1dbbd972b6879d0a3d7bdab51c7f5290a5d81efb5f9b53",
      "bytes": 7987,
      "artifact_id": "artifact-cb43ae0737de4abe",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-82ef6733dff848f1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/game.js.meta",
      "sha256": "03692a463ccb88d8a6df850b4c62994ada5de755549a9a8dd0da44c0b856c0f3",
      "bytes": 166,
      "artifact_id": "artifact-82ef6733dff848f1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-db98eb2e16af410a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/game.json",
      "sha256": "a8b4ca92b89dc6849c1c03d3d1e9636099669efb4a015260096c4783ee6a06c0",
      "bytes": 134,
      "artifact_id": "artifact-db98eb2e16af410a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6e2fbe12fb6447f0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/game.json.meta",
      "sha256": "6c351739ff12ccc3c3ec46227b35a8ac7651c39fcc52fc987d5ed4d5e8779a9c",
      "bytes": 166,
      "artifact_id": "artifact-6e2fbe12fb6447f0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4b3c2d2b605e4028": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/playable-fix.js",
      "sha256": "4a90a197cb694e97c09274f3bc818a2eacea25b1939e16a9d9f37c0ce1059ac8",
      "bytes": 1077,
      "artifact_id": "artifact-4b3c2d2b605e4028",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ec79afe3e87b475e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/playable-fix.js.meta",
      "sha256": "cf0b58ebd94cafa85c0420618d12ce1390e56e091533ed310369a57a3f27c750",
      "bytes": 166,
      "artifact_id": "artifact-ec79afe3e87b475e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-988a2c5d3c1f4a53": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugin-config.js",
      "sha256": "00fc1c1d6765f328b5cea31de6aa093e493a78bb6c70fc1ff4f20f2f5b47d658",
      "bytes": 487,
      "artifact_id": "artifact-988a2c5d3c1f4a53",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5617e077284d430e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugin-config.js.meta",
      "sha256": "0f3bee3b92bf95aea2459b73e26e8f5d8b8e7bee7791d6f19595c25c17dd9692",
      "bytes": 166,
      "artifact_id": "artifact-5617e077284d430e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0c9686b5e83d45cf": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins.meta",
      "sha256": "424cc3c994efb98de546facdb2599133140c716b5245e1deffdf62d3328de147",
      "bytes": 171,
      "artifact_id": "artifact-0c9686b5e83d45cf",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fcfed57a98944189": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins/check-update.js",
      "sha256": "8723f7b849d6ff531301139470105d22abbc4e1022b3971942c2d5e945a75d71",
      "bytes": 739,
      "artifact_id": "artifact-fcfed57a98944189",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b1c06441db5c45df": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins/check-update.js.meta",
      "sha256": "c0b39ec84a8b5b415602ea3a8502e50d21ca44eaca07f2ee234211f525d5d5fa",
      "bytes": 166,
      "artifact_id": "artifact-b1c06441db5c45df",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-acbe0d23d8e04274": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins/screen-adapter.js",
      "sha256": "14162739f3761ed1883ea76df0103259687b646bc2b5bdf5933ed22912e49527",
      "bytes": 325,
      "artifact_id": "artifact-acbe0d23d8e04274",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-db5c4808d9e847fe": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/plugins/screen-adapter.js.meta",
      "sha256": "29af925419e60aa0db3b204a7830ab8f31492fd3abb5964b0a215f65e737b41f",
      "bytes": 166,
      "artifact_id": "artifact-db5c4808d9e847fe",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e5d559a8da7c41a9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/project.config.json",
      "sha256": "80ee6721578830cdef6f812ebc3b34480a5a4060e548f38b3ef627082979847f",
      "bytes": 1646,
      "artifact_id": "artifact-e5d559a8da7c41a9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8d166f0fe63f43e2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/project.config.json.meta",
      "sha256": "ceecd2bbdbfebdbcf68b471403a3145407680d343c8a672b44a5202761f040a9",
      "bytes": 166,
      "artifact_id": "artifact-8d166f0fe63f43e2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-741df1d3e2334852": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/texture-config.js",
      "sha256": "7f7726142203ab208fd7a062af4bb04951ab8296d634b28dff7a63a65688e515",
      "bytes": 122,
      "artifact_id": "artifact-741df1d3e2334852",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c7a7e13038914f9a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/texture-config.js.meta",
      "sha256": "6077c1956148e4fc0258914dc870573b249f7771dc426f42ca23c18e4fb9b0ec",
      "bytes": 166,
      "artifact_id": "artifact-c7a7e13038914f9a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4a0be36bc04d4b71": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-namespace.js",
      "sha256": "57a8941c93d7a3a9857e0ce98c14c9f98411297b656f052045dbbbeb2be3fd7f",
      "bytes": 8603,
      "artifact_id": "artifact-4a0be36bc04d4b71",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-58f1de6b687343d2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-namespace.js.meta",
      "sha256": "d3e1634bb1c987a5961b5df9682f590bbe5e0b3ea7e9c7a5117872a8e3f845fe",
      "bytes": 166,
      "artifact_id": "artifact-58f1de6b687343d2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a77d2642d65c49cb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-playable-plugin.meta",
      "sha256": "1f4a7c4668274d448c9d2869b8996031b6e08a8af692258a951ffd100d9a9929",
      "bytes": 171,
      "artifact_id": "artifact-a77d2642d65c49cb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e73181491c4f4e99": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-playable-plugin/index.js",
      "sha256": "53df65173e4e24d99731cb3ce9ddd0e82d2e86aa4912a6009c6122eb4c79aa26",
      "bytes": 69089,
      "artifact_id": "artifact-e73181491c4f4e99",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9abaaee82d8047c7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-playable-plugin/index.js.meta",
      "sha256": "b0be81d40ec9bba0d551d05d634892fae9b89e58ba7230857ed557601f334e22",
      "bytes": 166,
      "artifact_id": "artifact-9abaaee82d8047c7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7f299eb83ddb44f0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk.meta",
      "sha256": "be5133ba8e50a22bac8bb601126f306b65db3c0551807bfbacc73ce990b42507",
      "bytes": 171,
      "artifact_id": "artifact-7f299eb83ddb44f0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bdb95450f9484af0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio.meta",
      "sha256": "c6c20df58db056781222871d0840bc660440b7777ab22fe4b0c2c5a5f355e698",
      "bytes": 171,
      "artifact_id": "artifact-bdb95450f9484af0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3be02664aa5e45d5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/common.js",
      "sha256": "daebe0c943184912dbbdfb9abd32fe58cfdfc2fc502a9df869e44b5cf617dc10",
      "bytes": 2230,
      "artifact_id": "artifact-3be02664aa5e45d5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8f19c2af050548bb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/common.js.meta",
      "sha256": "0ffa3761455877c9107d401375d8f9b35797d6c860bb18f97c1e20ac2fa16bc7",
      "bytes": 166,
      "artifact_id": "artifact-8f19c2af050548bb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9df8db3e7d93487a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/const.js",
      "sha256": "31a941dae51b6e28bd78520284ce896dae4307f91e1c70ea2311826e4fcf79b9",
      "bytes": 231,
      "artifact_id": "artifact-9df8db3e7d93487a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6ca3663017394838": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/const.js.meta",
      "sha256": "b46e5d7ae62c160f8f652fed2d4287cbe47c5412cf99bf305cb6a1074f9d357d",
      "bytes": 166,
      "artifact_id": "artifact-6ca3663017394838",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d33f32f8155547af": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/index.js",
      "sha256": "596885e8def00ca4cbc47421b94d4a9e1d9d8f666c0444733ada04a33f0b27b1",
      "bytes": 184,
      "artifact_id": "artifact-d33f32f8155547af",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f599ed96e9a24c17": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/index.js.meta",
      "sha256": "63955ba03d6813741ddf44875e2bd40c5b62d8da15be8bc835f7564a374b281c",
      "bytes": 166,
      "artifact_id": "artifact-f599ed96e9a24c17",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-863546944874419e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/inner-audio.js",
      "sha256": "41657bb73852f50f7e996d663ce1184fea4b4c5819fec459680db52152588425",
      "bytes": 12247,
      "artifact_id": "artifact-863546944874419e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2d4c554d4d714852": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/inner-audio.js.meta",
      "sha256": "6b17f560398dbf0160f186bc6b9fd179ddc9a0cf25d8b7fc517f43b6486bdeb1",
      "bytes": 166,
      "artifact_id": "artifact-2d4c554d4d714852",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1b0c26f070694c98": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/store.js",
      "sha256": "d52bb7e2ed9de12b8182d48341e15fddd9c36e2f42484c878401206e90624dc7",
      "bytes": 659,
      "artifact_id": "artifact-1b0c26f070694c98",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-68d8209021be4cbc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/store.js.meta",
      "sha256": "d40bb2c38a2574a0fd634c318245adad0ead16dee20c39d14ce74a33b8f4be75",
      "bytes": 166,
      "artifact_id": "artifact-68d8209021be4cbc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b7cf1c04c34c4d34": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/unity-audio.js",
      "sha256": "de7aed485a4b0628ee12e2d28a8b6aabeef2088ef63fd8a67b6e4c6c41955afa",
      "bytes": 45362,
      "artifact_id": "artifact-b7cf1c04c34c4d34",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c5c5539e148c4629": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/unity-audio.js.meta",
      "sha256": "51c9246f86fb0caae962e64dba7c8df5fe0cc871be0f703300a170471353478f",
      "bytes": 166,
      "artifact_id": "artifact-c5c5539e148c4629",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-18705f8ea559441f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/utils.js",
      "sha256": "401b07b84ab2745020ba15b0deb9fd68e8b96f5e4e4ecdc6aabe20fc2364e780",
      "bytes": 1974,
      "artifact_id": "artifact-18705f8ea559441f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-af0d0ae83bb943af": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/audio/utils.js.meta",
      "sha256": "c18939faa7d02a18d25e2f718877b71fa34eb3c9df0f3b19bd53b7c210fb1eb4",
      "bytes": 166,
      "artifact_id": "artifact-af0d0ae83bb943af",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b9bc36e2d11f4978": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/canvas-context.js",
      "sha256": "dfdc1d383529c529afd74d61eb3555ebce0303eae5dddf585c611b27fb783334",
      "bytes": 330,
      "artifact_id": "artifact-b9bc36e2d11f4978",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-12a78e5a60134c27": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/canvas-context.js.meta",
      "sha256": "3f869f0270693b0e76c15ab3d1f2e70c50962d3f18fbf2ac1d5fbb1b862e6eb5",
      "bytes": 166,
      "artifact_id": "artifact-12a78e5a60134c27",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-33f46dab62e947b6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/canvas.js",
      "sha256": "8e4a0fd95e0ab4c4935f58a6201e23468a8ec74c9e3efaeef36329397aef8af5",
      "bytes": 829,
      "artifact_id": "artifact-33f46dab62e947b6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a431783b8cef4957": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/canvas.js.meta",
      "sha256": "a0c129dbc2b9965681c84631a1f764ec6eeb5753b120844932fe9ae9cd65ccc6",
      "bytes": 166,
      "artifact_id": "artifact-a431783b8cef4957",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ffb2852d4e294621": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/conf.js",
      "sha256": "b1dd63338c3f2689ae3077902a39f3720d45e40db163e4f1979cde4f10f03d36",
      "bytes": 50,
      "artifact_id": "artifact-ffb2852d4e294621",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e91bfd3944a6406b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/conf.js.meta",
      "sha256": "c0661016df709c156fa6b8573db28e1a4f8e699df900946db9dd843299db8b51",
      "bytes": 166,
      "artifact_id": "artifact-e91bfd3944a6406b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1f9446ab466e4e9c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/file-info.js",
      "sha256": "f0db1cc7c28a00bf1647325d2ade29f9175c3889d97e58a9f4cf5762b2162c4d",
      "bytes": 1574,
      "artifact_id": "artifact-1f9446ab466e4e9c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-173def5509ca446a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/file-info.js.meta",
      "sha256": "d96cad7b68cd3d179a0aba8e49dc787d0ce75f1b2522b5c1dfd75d4c97dd7002",
      "bytes": 166,
      "artifact_id": "artifact-173def5509ca446a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2c70eff88471478d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/fix.js",
      "sha256": "221fbd407c603fed4592ca6df54a4b696ce05eaff97c515caf78bb38fa60aadb",
      "bytes": 2776,
      "artifact_id": "artifact-2c70eff88471478d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-729018fd8a104026": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/fix.js.meta",
      "sha256": "bf79658902e06ef68b8da4c54007397e18c9b428ce6907c2794a8bbcd601028c",
      "bytes": 166,
      "artifact_id": "artifact-729018fd8a104026",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c6b924bbd1c24a17": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/fs.js",
      "sha256": "d28ff7791372fcdf6ab40c1390131d57b256625e7b7288ca5aa7e3bbc6e95d08",
      "bytes": 16218,
      "artifact_id": "artifact-c6b924bbd1c24a17",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4d3ccd1755fe42e5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/fs.js.meta",
      "sha256": "c7fd5d5a2d8830052f65efb3b2386ff74e52eef256ae6a8b9127f89073635c24",
      "bytes": 166,
      "artifact_id": "artifact-4d3ccd1755fe42e5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-af064381f0cb4531": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/index.js",
      "sha256": "fe53546589f071aebb4df4dd14f018cded72e9421be3ec49c2fa7bbf479f865b",
      "bytes": 2159,
      "artifact_id": "artifact-af064381f0cb4531",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-38b6266f10044a02": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/index.js.meta",
      "sha256": "e7afe4590049a3abafa55cfc55ef64c2a09f863a42df70fb880f0af647c686d0",
      "bytes": 166,
      "artifact_id": "artifact-38b6266f10044a02",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2eba68b22e9b47d9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/logger.js",
      "sha256": "7d765a2c857a05424d08fc4cca83ba32393558f79fbc46c653a98bf440b03013",
      "bytes": 620,
      "artifact_id": "artifact-2eba68b22e9b47d9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5d542a60844548a6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/logger.js.meta",
      "sha256": "0b0b536d48dacc871d41a8e88ce3386a3357f14822bb3d60795d77fdd9126f3f",
      "bytes": 166,
      "artifact_id": "artifact-5d542a60844548a6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2e606830fdc8477e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/mobileKeyboard.meta",
      "sha256": "8ba6b8516d7a2c0499878ce887fca28e751d83294e2e628274ecd3cad8cc35c3",
      "bytes": 171,
      "artifact_id": "artifact-2e606830fdc8477e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-055db8d47bde4733": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/mobileKeyboard/index.js",
      "sha256": "fff32a092621032170bd2ef8f92bf95d0a2607a239634e659e6b9bee6f01be0b",
      "bytes": 4273,
      "artifact_id": "artifact-055db8d47bde4733",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-965e6cfa81ee4cf9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/mobileKeyboard/index.js.meta",
      "sha256": "7b22d69248c2bb7e3396a774dd9692c324a6a4850ab461bf6cb50868bbef3d71",
      "bytes": 166,
      "artifact_id": "artifact-965e6cfa81ee4cf9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0a6c776c95f54498": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/module-helper.js",
      "sha256": "5be6623e72561c69479b279dbb423b09e70041c914a465d9ff2837d03cb8e913",
      "bytes": 355,
      "artifact_id": "artifact-0a6c776c95f54498",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6d4ccf49a2584272": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/module-helper.js.meta",
      "sha256": "c1c280179e7f9a5e87bbc71db4f643e469155d80818f06149edeaf7ff9ceb614",
      "bytes": 166,
      "artifact_id": "artifact-6d4ccf49a2584272",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9d59a139e9214b03": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/recorder.js",
      "sha256": "eaba87090f60fb22bb59bf1e82dd16a5aae134b2e9dc08c403f79c61652b51cf",
      "bytes": 4469,
      "artifact_id": "artifact-9d59a139e9214b03",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0cb1ea0bb5964aa3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/recorder.js.meta",
      "sha256": "13a829d1c0acd26c69cf3c7166fa92cfa731b193d084ffb44731616d79722d09",
      "bytes": 166,
      "artifact_id": "artifact-0cb1ea0bb5964aa3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f4a6cf6db1714c27": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/resType.js",
      "sha256": "e2135586c03404792b961ea032e53d7000f552a4b61cb066896d149b13b6d2cf",
      "bytes": 30137,
      "artifact_id": "artifact-f4a6cf6db1714c27",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2a215998c6a04ec2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/resType.js.meta",
      "sha256": "4f7a2d6b7310798709f5c3be9a3c116721d86a6e4c6a706717565175dc6a2646",
      "bytes": 166,
      "artifact_id": "artifact-2a215998c6a04ec2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-88d012e1128849a9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/resTypeOther.js",
      "sha256": "af5e7538fad8f6640239322cdfc7a6c5181995551cd7a6772b8311b2df55b247",
      "bytes": 2293,
      "artifact_id": "artifact-88d012e1128849a9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4dcd0b763d2b456a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/resTypeOther.js.meta",
      "sha256": "659bfc0974df7c23aa972298bf8a1a493eef79d40fd2caf68b484ee78b6d162d",
      "bytes": 166,
      "artifact_id": "artifact-4dcd0b763d2b456a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-964cc740f6af4d9b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/response.js",
      "sha256": "9bf4b44ef758ec8917aac3a1827de70f27bacc881849bf985cafaab313216304",
      "bytes": 1891,
      "artifact_id": "artifact-964cc740f6af4d9b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-60e4e6803e56482d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/response.js.meta",
      "sha256": "447d6df2a8f0a822911666ed6558e03655bfaffa58d0b9fb09e5f7f560873c62",
      "bytes": 166,
      "artifact_id": "artifact-60e4e6803e56482d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-83a162bedaff4bcc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/sdk.js",
      "sha256": "8377aca08a51213a937cd6fe0b49837dccaa607e1d73dd1c955d7181324811f1",
      "bytes": 17209,
      "artifact_id": "artifact-83a162bedaff4bcc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f67d2782202f4745": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/sdk.js.meta",
      "sha256": "b77a286dcb3bf4ea4839d2718fc78af8f473eab3edca5a05a55494f0a80e8845",
      "bytes": 166,
      "artifact_id": "artifact-f67d2782202f4745",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-488c4686a90b474c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/special-callbacks.js",
      "sha256": "30bcf71dab5024efd455339a9e4e2fd0aa56bd918fe6169f974ae8a246012919",
      "bytes": 1345,
      "artifact_id": "artifact-488c4686a90b474c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e673b9b7c0174985": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/special-callbacks.js.meta",
      "sha256": "afa6b7de060146a698df759af34538605745d6118f6b30daa60ecfa0812bf4ae",
      "bytes": 166,
      "artifact_id": "artifact-e673b9b7c0174985",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-adb8172a9d08443b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/texture.js",
      "sha256": "ff27b378812ef12f39cacc4a2605355d74141d67adfaf83528df597adad77aac",
      "bytes": 10740,
      "artifact_id": "artifact-adb8172a9d08443b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-abb8d9957ea1489b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/texture.js.meta",
      "sha256": "166bcd33143f4fc4055fbdcb8b4afe80ff48936459c3180788b86b9a1a52446d",
      "bytes": 166,
      "artifact_id": "artifact-abb8d9957ea1489b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-deeb3fa17c2c438b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/touch.meta",
      "sha256": "3047e978a9f8f217bebe722afabf16918308fb0cc8b0c739b105033392a84fb8",
      "bytes": 171,
      "artifact_id": "artifact-deeb3fa17c2c438b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-72a19baefbe546f0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/touch/index.js",
      "sha256": "a5917a61534de7bc2f5224a9c9402863a264cf6bd2106d5536eb6018299de53f",
      "bytes": 2301,
      "artifact_id": "artifact-72a19baefbe546f0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e161f5c743d0462f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/touch/index.js.meta",
      "sha256": "4817217627f375988f6cb94c0293e86196e57edede1a9e7a8d6bdfdeece2455a",
      "bytes": 166,
      "artifact_id": "artifact-e161f5c743d0462f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-34eeb9cffd504e12": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/util.js",
      "sha256": "f7694b65a403bfa09e90458b33414f0a48afeab225657e2ee6d256f580388312",
      "bytes": 6193,
      "artifact_id": "artifact-34eeb9cffd504e12",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2d210b9bcd1344c7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/util.js.meta",
      "sha256": "491cb8c6359f52a306b9176fc82e12a9a8f14e8eda2cd4f7e163c0a2a7b0193f",
      "bytes": 166,
      "artifact_id": "artifact-2d210b9bcd1344c7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ad698da503ef458d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/utils.js",
      "sha256": "fa04582ad3583d37db517798364eae68075b884906d93beeb3b067f697cabd5c",
      "bytes": 12987,
      "artifact_id": "artifact-ad698da503ef458d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e1cad2a018c34241": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/utils.js.meta",
      "sha256": "9ad8faab707b2e80efaf972935bf70a80693e904e7e236d769444755d97a66b8",
      "bytes": 166,
      "artifact_id": "artifact-e1cad2a018c34241",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-dd488b3949ce428b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video.js",
      "sha256": "b2d156cb1b437508d55bf8a32a9096f6d370ddf196f63bf5113d9c4597e2e128",
      "bytes": 2552,
      "artifact_id": "artifact-dd488b3949ce428b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-39dccc3f34e84708": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video.js.meta",
      "sha256": "1bc652235dbdd6b56c78715aae87dfc2e5625f40c0ecbb043b038ce05421255c",
      "bytes": 166,
      "artifact_id": "artifact-39dccc3f34e84708",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1397ff9b51f74461": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video.meta",
      "sha256": "2667056c792e591d58a5926a2617c12e05b1be95297c145550809cdb4e2c0b0b",
      "bytes": 171,
      "artifact_id": "artifact-1397ff9b51f74461",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-15f8d379fe7d494d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video/index.js",
      "sha256": "b70a96625b7f87d0556f16ca2cb8bef1ca0e7638330a433a66003e627c9b24ac",
      "bytes": 16149,
      "artifact_id": "artifact-15f8d379fe7d494d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-054f5df9e96d42ad": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/unity-sdk/video/index.js.meta",
      "sha256": "61a3194948e1d7539f082c49d3f97fb71787705d6be4018c4be7dec3b02d153a",
      "bytes": 166,
      "artifact_id": "artifact-054f5df9e96d42ad",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7f260c0901774137": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/wasmcode.meta",
      "sha256": "ac46a4dad77e83ffe13b6b8d08d9571665907009029dde9265c601af75be238f",
      "bytes": 171,
      "artifact_id": "artifact-7f260c0901774137",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-349d74e68ce74e01": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/wasmcode/game.js",
      "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "bytes": 0,
      "artifact_id": "artifact-349d74e68ce74e01",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bbc259f482cd4075": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/wasmcode/game.js.meta",
      "sha256": "5d49180cc8a3313700be9bc041c347576ee10c63028049b4e5812a12a87fde07",
      "bytes": 166,
      "artifact_id": "artifact-bbc259f482cd4075",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-10fdff2a4c144d5a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/weapp-adapter.js",
      "sha256": "2787e9f449849de9b4fb632841bf94b9f405d03446e4d54a37e50ea9d3d65a75",
      "bytes": 72552,
      "artifact_id": "artifact-10fdff2a4c144d5a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-780fccbbe73e41e3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/playable-default/weapp-adapter.js.meta",
      "sha256": "d22fe5c7f8be7d5677521792c46773e16fe92ea1a169c99ff253e69af5204994",
      "bytes": 166,
      "artifact_id": "artifact-780fccbbe73e41e3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e70f40edac5347b5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default.meta",
      "sha256": "991d2fa8857422a650828bee21d04f4a0442cdc93b365bf702aba47e1d36595f",
      "bytes": 172,
      "artifact_id": "artifact-e70f40edac5347b5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5026402d42dd45f8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/check-version.js",
      "sha256": "0cd6a4e91506b41a7d1cf1b87474e79685fe577ffe1e8c3f0b3b66f75c8e8242",
      "bytes": 8264,
      "artifact_id": "artifact-5026402d42dd45f8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bfe2c2d29f524a0c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/check-version.js.meta",
      "sha256": "e41269d888890ef6c66ef44a172a1dec74d161f9b6c4d359845218c6df4aa8ef",
      "bytes": 166,
      "artifact_id": "artifact-bfe2c2d29f524a0c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fcf9efa962aa4636": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/data-package.meta",
      "sha256": "36cd46b6904899aabd744d59ac7f8cad6462ee7625bf214aee273636505f6eb6",
      "bytes": 171,
      "artifact_id": "artifact-fcf9efa962aa4636",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cd5fc020e6a24501": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/data-package/game.js",
      "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "bytes": 0,
      "artifact_id": "artifact-cd5fc020e6a24501",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f7d41e3479064190": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/data-package/game.js.meta",
      "sha256": "df3311bd85471c23b5445eacbbf1d699c40674b958fcd2da1120e7ec7651ec9d",
      "bytes": 166,
      "artifact_id": "artifact-f7d41e3479064190",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-90d5bc9c2ff84f3a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/events.js",
      "sha256": "0e8d53bd4cd51e8833018558abf50a27e28c6294faf16da100142e7b27a41681",
      "bytes": 1545,
      "artifact_id": "artifact-90d5bc9c2ff84f3a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6825fc5171994192": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/events.js.meta",
      "sha256": "f6daf9de83b0a5da30e284c3bca883dddaab3fd7e9b72b20894cd994874a1d09",
      "bytes": 166,
      "artifact_id": "artifact-6825fc5171994192",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-945050864fa84f7d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/game.js",
      "sha256": "da25420acad9ae5e62fbc93d9408634bc04b2ef8957f28355ab1a757b725ea7a",
      "bytes": 8141,
      "artifact_id": "artifact-945050864fa84f7d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b9ef7e3485a5474f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/game.js.meta",
      "sha256": "b9a69f5a60753737f2e6aaac9a3caddc9599fbea89422cd2fe1c03ac1dc1a240",
      "bytes": 166,
      "artifact_id": "artifact-b9ef7e3485a5474f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-182f22d1d9ec4d57": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/game.json",
      "sha256": "33ce331329781c2704935c796fc3ef4ad763824b058d17c2ffbe6be2cf534bd3",
      "bytes": 1014,
      "artifact_id": "artifact-182f22d1d9ec4d57",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4c1faa7a4e7544e5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/game.json.meta",
      "sha256": "9d91efe5a9b7d4a081ae146b01c150b667ebe84390bfb78959ff7d05b7920b1b",
      "bytes": 166,
      "artifact_id": "artifact-4c1faa7a4e7544e5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-94b1c9cd928c4e3c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images.meta",
      "sha256": "27cec5c2cb92e37fdf1e69daf1ea3c8f23c30d5ad9b067d2494516873f3b9d96",
      "bytes": 171,
      "artifact_id": "artifact-94b1c9cd928c4e3c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-36946001a0614ea4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images/background.jpg",
      "sha256": "ee88b4672db843d57b164c5deeaf62e096e3bf91881bdba71a85dd980687db80",
      "bytes": 8917,
      "artifact_id": "artifact-36946001a0614ea4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5863f3c41be1476c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images/background.jpg.meta",
      "sha256": "9f63d1c0b7ec63b3578bb79afd19540e055f1c09c753287a7292267ac5ef4c8e",
      "bytes": 166,
      "artifact_id": "artifact-5863f3c41be1476c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4fde62f7ee874895": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images/unity_logo.png",
      "sha256": "34799ce1b7a341e39c6c6ea7fa439f170989f3ecc5d0cd3681573569b4b6d239",
      "bytes": 1216,
      "artifact_id": "artifact-4fde62f7ee874895",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ca174fe43a494b32": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/images/unity_logo.png.meta",
      "sha256": "a9f0edc0331aeeac5e7973e9e9f4cfea78c98079a45883676cdc610b162653db",
      "bytes": 166,
      "artifact_id": "artifact-ca174fe43a494b32",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ec649f0596814812": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data.meta",
      "sha256": "1808ef2224daae0c7540139c16b03ff68b70d2f304fddb8c15c7efe2626339f6",
      "bytes": 171,
      "artifact_id": "artifact-ec649f0596814812",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-895ab2c8498f4e64": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data.meta",
      "sha256": "f1105aa903f989e01c1af4bfcc91893d5bf0a42f38495aa6dccef9194b89ba7d",
      "bytes": 171,
      "artifact_id": "artifact-895ab2c8498f4e64",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-909eae2c75234c09": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data/index.js",
      "sha256": "cf9f355257a4b6cdad59436962371b816847fa7df294fe0ba2fce7eb22b87552",
      "bytes": 5017,
      "artifact_id": "artifact-909eae2c75234c09",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1aeb548d80ad4881": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data/index.js.meta",
      "sha256": "73d48412cffe027f3bdce4b05903adb6108c225403368e9e6102e89958c61403",
      "bytes": 166,
      "artifact_id": "artifact-1aeb548d80ad4881",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a6d1f7c7ac644af4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data/utils.js",
      "sha256": "5b154de8c3812effc187a5d32969aa41c3d0ce471e0cee803d1de0820dd2cdc9",
      "bytes": 285,
      "artifact_id": "artifact-a6d1f7c7ac644af4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4a1240a9638b4d92": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/data/utils.js.meta",
      "sha256": "cc3c1f56e6ab10d6d95bbf08906d6433964cb2366ee46af1a18ab94a77efcb68",
      "bytes": 166,
      "artifact_id": "artifact-4a1240a9638b4d92",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-eb7dc49408e340b8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/index.js",
      "sha256": "df850050fcf461dc5ad74f5bfb4da3ba2841b30a7b410a3ff17a7945becc0648",
      "bytes": 5249,
      "artifact_id": "artifact-eb7dc49408e340b8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-650cbbdd93c24975": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/index.js.meta",
      "sha256": "e708a2305390e2c4d7275f7820f7d3b4426f1e30bb84e270b4bc70951e3d7f96",
      "bytes": 166,
      "artifact_id": "artifact-650cbbdd93c24975",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3d643d51a20f41b5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/loading.js",
      "sha256": "c6eb83fb34a7532a7490a80fd3040636776f7a32c7719ebc5a9d194483155d38",
      "bytes": 868,
      "artifact_id": "artifact-3d643d51a20f41b5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4b0dd1bf80134514": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/loading.js.meta",
      "sha256": "b1a8ad7aca4371db776e710528e56a67ba40418de85104f75601f732e614c7de",
      "bytes": 166,
      "artifact_id": "artifact-4b0dd1bf80134514",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-45ed3d4687484d4b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render.meta",
      "sha256": "dc502c7ec9d41193cc0cde2d789293a917575c29584cba5ea27c5f371a5001d1",
      "bytes": 171,
      "artifact_id": "artifact-45ed3d4687484d4b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9490633fb10947cb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image.meta",
      "sha256": "561e5e55760533e8914206cca18b91f478a53b6d0b9917784cf63f727cc2a44d",
      "bytes": 171,
      "artifact_id": "artifact-9490633fb10947cb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-098f4b136a714eae": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/avatar.png",
      "sha256": "63196c60196e8526399f4c4b4421c9844718738a32bbdaa7767ebac2864e85b8",
      "bytes": 5026,
      "artifact_id": "artifact-098f4b136a714eae",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-34d08816d75340fc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/avatar.png.meta",
      "sha256": "b1d17fc500694d14a291337ebfb26c845ee855298327174a501f595f692d08d8",
      "bytes": 166,
      "artifact_id": "artifact-34d08816d75340fc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-14de44e43990439b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button1.png",
      "sha256": "694daefae30a8ce1900ac09b894b100cc8f5bd14b5bc9bc5da21d77515b4717c",
      "bytes": 870,
      "artifact_id": "artifact-14de44e43990439b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0098856dc62f43f9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button1.png.meta",
      "sha256": "66032004fdee0c6079fe6df4e0a0f9da2b5137e593a475ae13e817bdb9fcfecd",
      "bytes": 166,
      "artifact_id": "artifact-0098856dc62f43f9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-93ae4e8364a34878": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button2.png",
      "sha256": "6375e5e4136df1f6a36eafbc9c5199fcf5e0572e607b4e6afc234bb4fe433323",
      "bytes": 816,
      "artifact_id": "artifact-93ae4e8364a34878",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ece802153cf04752": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button2.png.meta",
      "sha256": "a3b94d41930f40d735128507d81580d1b5018002b163ecd0d12c9c7aabd6309a",
      "bytes": 166,
      "artifact_id": "artifact-ece802153cf04752",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b19fc19710a04a4d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button3.png",
      "sha256": "7b79d41816520bade57466ccdcc7fbbf1b423166c5cad02c705b3e9ab513b3f6",
      "bytes": 8880,
      "artifact_id": "artifact-b19fc19710a04a4d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cf15aa834c15459d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/button3.png.meta",
      "sha256": "244e726d9cbdb72ef3c744b942bbb099d11d2d5ac429345bb9ff55b3e29cb8aa",
      "bytes": 166,
      "artifact_id": "artifact-cf15aa834c15459d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b3635ed780c8499d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/loading.png",
      "sha256": "ced01531e93259285e4ce9f440699921f7d7ad2616084ba6caff7af24eee43eb",
      "bytes": 6244,
      "artifact_id": "artifact-b3635ed780c8499d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0d9be0fbc81246ee": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/loading.png.meta",
      "sha256": "a65ea8e04e84d0617cdbab3d66a539e109d46e40b5dc93999f135c4d1ab2c780",
      "bytes": 166,
      "artifact_id": "artifact-0d9be0fbc81246ee",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-377024368ce94555": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/nameBg.png",
      "sha256": "9aad869319d994c9cd1c417ab8e965c51765af7691c77cb5bd638cafe6c8770f",
      "bytes": 339,
      "artifact_id": "artifact-377024368ce94555",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-629106610e614415": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/nameBg.png.meta",
      "sha256": "d820643240d4200265b5c1d3e1859a198e2c97934e425689f3f8542afa9f5254",
      "bytes": 166,
      "artifact_id": "artifact-629106610e614415",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-dd17f86131c94cfb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/rankAvatar.png",
      "sha256": "0fcbc7c8ad88eee4e3fdc241fc13e7c625220b20b3a894663de329d5500e4af0",
      "bytes": 7121,
      "artifact_id": "artifact-dd17f86131c94cfb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2608b46b1ed342cb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/rankAvatar.png.meta",
      "sha256": "e6488adbb97bfc924e5ac6a1000d61528c1a943df5e78f891bc3ae9c80066d1c",
      "bytes": 166,
      "artifact_id": "artifact-2608b46b1ed342cb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a272be7bd4b54ab5": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/rankBg.png",
      "sha256": "a8e4cc0b6fd9878e7243526b435b661ec91c5bc7c95b666d453d85d81cf1ae56",
      "bytes": 61065,
      "artifact_id": "artifact-a272be7bd4b54ab5",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3796270072e64b36": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/rankBg.png.meta",
      "sha256": "1560d64cbf7f77087d589e90da3982df6b89c6371a051c359dd5fcead1f2e8a9",
      "bytes": 166,
      "artifact_id": "artifact-3796270072e64b36",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-02c8c0ff77494d0e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/shareBg.png",
      "sha256": "d666d8a12d78635b0c97dd448101d78324cec1f981a74f37978e5520d936d8c3",
      "bytes": 9008,
      "artifact_id": "artifact-02c8c0ff77494d0e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bab23c9f1f664f8a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/shareBg.png.meta",
      "sha256": "7de11d365f94e3a8c290813c1c44e2e9f799c6bddf1312f3880e918354e38d2f",
      "bytes": 166,
      "artifact_id": "artifact-bab23c9f1f664f8a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3fce5430aeee4558": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/shareBg2.png",
      "sha256": "db92ef8013c61365ada8917d015530345262e7611cc3f83c5aac75a80bfa92be",
      "bytes": 13432,
      "artifact_id": "artifact-3fce5430aeee4558",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-70888ba0b3f448d7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/image/shareBg2.png.meta",
      "sha256": "13216ba15b311962aaca5d7877c05daa57b82aa4168f63720c33ae4229a1ba7f",
      "bytes": 166,
      "artifact_id": "artifact-70888ba0b3f448d7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-23fff7b2f859412c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles.meta",
      "sha256": "e8e97600b8c4557be406e2626a133005580a15080a5834a70e54ab0a1edb061a",
      "bytes": 171,
      "artifact_id": "artifact-23fff7b2f859412c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e9f2c3200dbf4a2c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles/friendRank.js",
      "sha256": "ed202f428d6e32ec47a46b94c910102cea0bbad04c49d87f476041b374e275ea",
      "bytes": 4552,
      "artifact_id": "artifact-e9f2c3200dbf4a2c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e6b9fcb2f9a447c3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles/friendRank.js.meta",
      "sha256": "e8753c6d3085b4715adde0a97d3ef3a7ed3aa7844f826bb987e2e82f89abdbfb",
      "bytes": 166,
      "artifact_id": "artifact-e6b9fcb2f9a447c3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6b9d908332314811": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles/tips.js",
      "sha256": "cbb306f11d4bcd6f2ed7e1dd9e6d1058d15233b91e592c33a32f3c6c2ee305bf",
      "bytes": 436,
      "artifact_id": "artifact-6b9d908332314811",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-96e055075a6c40c9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/styles/tips.js.meta",
      "sha256": "d98a5fad9a14cd47af201dcc4680c79521aa79b0ddd0fe494410548110c8939e",
      "bytes": 166,
      "artifact_id": "artifact-96e055075a6c40c9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ea5eeca41da64e62": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls.meta",
      "sha256": "fc237ef0250834d27f5271ff5f2b0b695189d0bd406abcede090ea3dfdc825b4",
      "bytes": 171,
      "artifact_id": "artifact-ea5eeca41da64e62",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-80c4b499cc024908": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls/friendRank.js",
      "sha256": "7f0d1eb610da78c0723f40a5a3d0ffbe698b0f50ed4ad9d7729f140ed424176a",
      "bytes": 2803,
      "artifact_id": "artifact-80c4b499cc024908",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5595a888d74d4c94": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls/friendRank.js.meta",
      "sha256": "4ef2f2ceb2da79170523b5455a7325023200a309e12c29b6e0b0b152ab7e0179",
      "bytes": 166,
      "artifact_id": "artifact-5595a888d74d4c94",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-65011e01396447da": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls/tips.js",
      "sha256": "3d8ad929216eec1234e36cb7e13653f58889b23d33034b1eff867a1641ac3a4e",
      "bytes": 1032,
      "artifact_id": "artifact-65011e01396447da",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-841b5ef8cb4a459e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/open-data/render/tpls/tips.js.meta",
      "sha256": "863d57e359d90306242aa14f507261b661bab4c46e1f312b5831771e9d397892",
      "bytes": 166,
      "artifact_id": "artifact-841b5ef8cb4a459e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bdfc334487334f92": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugin-config.js",
      "sha256": "00fc1c1d6765f328b5cea31de6aa093e493a78bb6c70fc1ff4f20f2f5b47d658",
      "bytes": 487,
      "artifact_id": "artifact-bdfc334487334f92",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8e5e4c8f58444324": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugin-config.js.meta",
      "sha256": "2a7fa239f5f17247f8ea923c78fe016f2b91c55a1a9efb7f5220cd25b377b89a",
      "bytes": 166,
      "artifact_id": "artifact-8e5e4c8f58444324",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-987420e6163c4b1c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins.meta",
      "sha256": "f22671a4cf1d7d0c2f391836f0ec4b72a7c630ce0348852dbad2cd3ca542ba62",
      "bytes": 171,
      "artifact_id": "artifact-987420e6163c4b1c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d0baf6aa10e34632": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins/check-update.js",
      "sha256": "8723f7b849d6ff531301139470105d22abbc4e1022b3971942c2d5e945a75d71",
      "bytes": 739,
      "artifact_id": "artifact-d0baf6aa10e34632",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-84f38528cae844b6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins/check-update.js.meta",
      "sha256": "52f32dbadd8ad8ea673faa330e4858a10e28e55bb2d445849d034ff69c4f38f7",
      "bytes": 166,
      "artifact_id": "artifact-84f38528cae844b6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b3e8f1b139184787": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins/screen-adapter.js",
      "sha256": "14162739f3761ed1883ea76df0103259687b646bc2b5bdf5933ed22912e49527",
      "bytes": 325,
      "artifact_id": "artifact-b3e8f1b139184787",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-02a7910e14a14765": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/plugins/screen-adapter.js.meta",
      "sha256": "392115a55763b54935ddde75253808bc922062d3a0be6b4467cbc95cf2357cf4",
      "bytes": 166,
      "artifact_id": "artifact-02a7910e14a14765",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5e081488f02c4554": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/project.config.json",
      "sha256": "a9b5e250e47455f3b4db07d41b6ca338c9d62215fdc1c750f0e2bc6e9fa3127d",
      "bytes": 1647,
      "artifact_id": "artifact-5e081488f02c4554",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ef4a089d812241e9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/project.config.json.meta",
      "sha256": "9421a371074e7da969b7505f4be035bb0f8f3bf41efa25f952c8f8fe2bf4d3d5",
      "bytes": 166,
      "artifact_id": "artifact-ef4a089d812241e9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8c34ab83b4484d57": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/texture-config.js",
      "sha256": "7f7726142203ab208fd7a062af4bb04951ab8296d634b28dff7a63a65688e515",
      "bytes": 122,
      "artifact_id": "artifact-8c34ab83b4484d57",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f10b0da382524d10": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/texture-config.js.meta",
      "sha256": "344300850a528721b1e574f7f9c3950410a7af77ad8984b1ba0f7f19d87fdf1e",
      "bytes": 166,
      "artifact_id": "artifact-f10b0da382524d10",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-15701a4052394e60": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-namespace.js",
      "sha256": "e049d40157fae569fba3fa519a62f00b714b6d8de722250e57d32a88ec4628d1",
      "bytes": 9135,
      "artifact_id": "artifact-15701a4052394e60",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5eb4f629b70d4cbc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-namespace.js.meta",
      "sha256": "6b9ef4463881fdf856749c5829772fbf2488982784b646aec0c49767cf52b083",
      "bytes": 166,
      "artifact_id": "artifact-5eb4f629b70d4cbc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-51677c7c4e754a0b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk.meta",
      "sha256": "869cba0322567e997728a90bbb8cff84ec886854eb5f8e2dd458bd9e889e2b45",
      "bytes": 171,
      "artifact_id": "artifact-51677c7c4e754a0b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8bc70edc85f84fe3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/TCPSocket.meta",
      "sha256": "45993995d28c46316becd9cfd45061dcd863a3cde0b7a1ecdcc664b7bc82c79e",
      "bytes": 171,
      "artifact_id": "artifact-8bc70edc85f84fe3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-477bf4e331254baf": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/TCPSocket/index.js",
      "sha256": "03616ad9e25e19aede3e7256e894e5b12c6847aec2fc2002b93bf44c8e439852",
      "bytes": 5369,
      "artifact_id": "artifact-477bf4e331254baf",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0110150e40cc4357": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/TCPSocket/index.js.meta",
      "sha256": "af9f5df7973e4569bf685e1bf6f0a70fa0e6d527bec12a11fc220dbee2757334",
      "bytes": 166,
      "artifact_id": "artifact-0110150e40cc4357",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fda033824b6140ee": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/UDPSocket.meta",
      "sha256": "bccde3dd05dc240d44b8b399440dba6e4e65bdf1eadf2d5d7b22a2a60697c2ac",
      "bytes": 171,
      "artifact_id": "artifact-fda033824b6140ee",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1b738156d4914232": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/UDPSocket/index.js",
      "sha256": "681a1bfdabbdc65ef8c4e8ef389b2844fd3f9a457ea1876c9c19a0b18620fae3",
      "bytes": 6255,
      "artifact_id": "artifact-1b738156d4914232",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2e09d4471cf54f81": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/UDPSocket/index.js.meta",
      "sha256": "f17f80b1e73091b2c87f5e981f9716b226421402eba4aa938aef1d9492e209ac",
      "bytes": 166,
      "artifact_id": "artifact-2e09d4471cf54f81",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-350fc04e986b43b1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/ad.js",
      "sha256": "1dafdbe346ef838cd421f3ff887612bcb6c9a7d2f44c1cce26cf3e6e2f005ecd",
      "bytes": 8884,
      "artifact_id": "artifact-350fc04e986b43b1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-997dea1bdb894897": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/ad.js.meta",
      "sha256": "56c1b99f48f959e51c4bc1e466d7775a8f8f39b0d90e7369cc2c05f9b5b15d96",
      "bytes": 166,
      "artifact_id": "artifact-997dea1bdb894897",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8c6bd3ecb99a4286": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio.meta",
      "sha256": "8e25c165562deef0ee57f88f56841f95671fbddd56a06599ce93f8720a02b59e",
      "bytes": 171,
      "artifact_id": "artifact-8c6bd3ecb99a4286",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-28defacc7ef04265": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/common.js",
      "sha256": "daebe0c943184912dbbdfb9abd32fe58cfdfc2fc502a9df869e44b5cf617dc10",
      "bytes": 2230,
      "artifact_id": "artifact-28defacc7ef04265",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fb88267a42f545fd": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/common.js.meta",
      "sha256": "268b6d9122b63488b368292863c781463b686bf857e3bf6ec6c9f48deac9c26d",
      "bytes": 166,
      "artifact_id": "artifact-fb88267a42f545fd",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-282025bc05864f1c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/const.js",
      "sha256": "31a941dae51b6e28bd78520284ce896dae4307f91e1c70ea2311826e4fcf79b9",
      "bytes": 231,
      "artifact_id": "artifact-282025bc05864f1c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-59fd1263ace34aba": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/const.js.meta",
      "sha256": "036cb368fea4a99a2fd817ab339f33902fc466103ff44622c1db0dd0c4b3cf1d",
      "bytes": 166,
      "artifact_id": "artifact-59fd1263ace34aba",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-068f32e6c50e420c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/index.js",
      "sha256": "596885e8def00ca4cbc47421b94d4a9e1d9d8f666c0444733ada04a33f0b27b1",
      "bytes": 184,
      "artifact_id": "artifact-068f32e6c50e420c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d541cd4696c54683": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/index.js.meta",
      "sha256": "4800ff491ef7498113f3f334a645053d6baa3e14c542e31bae63160e81f1b51b",
      "bytes": 166,
      "artifact_id": "artifact-d541cd4696c54683",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7c5dd99a63c840a7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/inner-audio.js",
      "sha256": "41657bb73852f50f7e996d663ce1184fea4b4c5819fec459680db52152588425",
      "bytes": 12247,
      "artifact_id": "artifact-7c5dd99a63c840a7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ccac2161fd524032": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/inner-audio.js.meta",
      "sha256": "d570d6f43f91d6d2898cc84696d18ea6387b4866edcc6ae3fdadfcfa32f74d43",
      "bytes": 166,
      "artifact_id": "artifact-ccac2161fd524032",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-467d23a3f0f8417e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/store.js",
      "sha256": "d52bb7e2ed9de12b8182d48341e15fddd9c36e2f42484c878401206e90624dc7",
      "bytes": 659,
      "artifact_id": "artifact-467d23a3f0f8417e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-40cbc1fca41843b9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/store.js.meta",
      "sha256": "c8a131c6ba3ad3058cde02316f5609392ca6b8561ce15e99baf7f6f709fced2d",
      "bytes": 166,
      "artifact_id": "artifact-40cbc1fca41843b9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-778184f653124682": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/unity-audio.js",
      "sha256": "a3a17656bcf86a851287e60f3cbe52119b9c413c94ece1ee11ad052540fcf648",
      "bytes": 47093,
      "artifact_id": "artifact-778184f653124682",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8d38b446ecc14372": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/unity-audio.js.meta",
      "sha256": "d1980abc8639bc701951211ca14b29fb45c14f2aa13d11ea00fa081142359834",
      "bytes": 166,
      "artifact_id": "artifact-8d38b446ecc14372",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c6612c8624b74f97": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/utils.js",
      "sha256": "45d4cc6d39e17301bacf4b00cc8f79491cf5eb6e3a5997493324870e5a66ad98",
      "bytes": 2059,
      "artifact_id": "artifact-c6612c8624b74f97",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-031cbc74952f4a1e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/audio/utils.js.meta",
      "sha256": "4024e33a2b1a39b3d64f4495d58520eac966305dc07c811a91ee4a291d47d94e",
      "bytes": 166,
      "artifact_id": "artifact-031cbc74952f4a1e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-92fdc97fa2074d0e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/authorize.js",
      "sha256": "36f6acce3576ef206fbd7ab7e41adc3613522c987bab8bd16344cef99ad25656",
      "bytes": 734,
      "artifact_id": "artifact-92fdc97fa2074d0e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-36ed80c3f1bb4c20": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/authorize.js.meta",
      "sha256": "007b09388d2c135ebea1f7cdaf0e2d55ad633617f73046ea153fde1f5107ec5c",
      "bytes": 166,
      "artifact_id": "artifact-36ed80c3f1bb4c20",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-143492929c5047f6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/bluetooth.meta",
      "sha256": "6605ffaf6544f2c4622d779c6b3ee6b0bed6305de58cfa25f4aaba7d18c536fa",
      "bytes": 171,
      "artifact_id": "artifact-143492929c5047f6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-19cda74bc64242e2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/bluetooth/index.js",
      "sha256": "1f664af1d27a75db3a306a1297508584cf4080fc9000d5fb5db67d4195dea09f",
      "bytes": 1288,
      "artifact_id": "artifact-19cda74bc64242e2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7a323160bf8f4270": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/bluetooth/index.js.meta",
      "sha256": "2b998b0cc6403d6b7ec44aea16f01c933c39e1f649e799390e5337844488bbda",
      "bytes": 166,
      "artifact_id": "artifact-7a323160bf8f4270",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-84fd2c114207406b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/camera.js",
      "sha256": "d745f848ef62da3a0a0052c518647301375437493b4e6e56422f570dd76ef09f",
      "bytes": 2958,
      "artifact_id": "artifact-84fd2c114207406b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3ad5c98719e84c23": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/camera.js.meta",
      "sha256": "d6a2bbb858c025f83e0fe6d4255ef33b9d7388a8ef1c32fcc0bb76ca48f0bc26",
      "bytes": 166,
      "artifact_id": "artifact-3ad5c98719e84c23",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4eeb12a5533847f1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/canvas-context.js",
      "sha256": "dfdc1d383529c529afd74d61eb3555ebce0303eae5dddf585c611b27fb783334",
      "bytes": 330,
      "artifact_id": "artifact-4eeb12a5533847f1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f3e7df7b0caf4d8b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/canvas-context.js.meta",
      "sha256": "02385bff32ba550688a643e30259d11ea2ce7ea011a0c74f21fcc65825081b5c",
      "bytes": 166,
      "artifact_id": "artifact-f3e7df7b0caf4d8b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5e81502d1a884cd0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/canvas.js",
      "sha256": "8e4a0fd95e0ab4c4935f58a6201e23468a8ec74c9e3efaeef36329397aef8af5",
      "bytes": 829,
      "artifact_id": "artifact-5e81502d1a884cd0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f59cb05c78354369": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/canvas.js.meta",
      "sha256": "626c5ecb547aac30d3d2d0498b651b1096c7459e6f9795c234a8da4aca4dffe6",
      "bytes": 166,
      "artifact_id": "artifact-f59cb05c78354369",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-00dceb3a6c014a2a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/chat.js",
      "sha256": "8cc33a6475217e9fe9fca8ca8a7078b235258defd0e1dd03bb05dbb25365f496",
      "bytes": 5058,
      "artifact_id": "artifact-00dceb3a6c014a2a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4542a85f3cf24225": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/chat.js.meta",
      "sha256": "e60e2dcd4742b7d5d0875706b98b9461940dd919fcab0f796c1e5a4f57c62534",
      "bytes": 166,
      "artifact_id": "artifact-4542a85f3cf24225",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-06b73815bb544c33": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/cloud.js",
      "sha256": "7fa5ff8c105a7b45efbc97a288eb7c82470d8c8f255bff46ae956dfc313d7d52",
      "bytes": 8972,
      "artifact_id": "artifact-06b73815bb544c33",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cd3bda75ab1b4f83": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/cloud.js.meta",
      "sha256": "4c7d3a7837111e7fbbe882f5e52445b2cefdbb594a31d47966d19477ddbd31a1",
      "bytes": 166,
      "artifact_id": "artifact-cd3bda75ab1b4f83",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e11f82cee4924b62": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/conf.js",
      "sha256": "b1dd63338c3f2689ae3077902a39f3720d45e40db163e4f1979cde4f10f03d36",
      "bytes": 50,
      "artifact_id": "artifact-e11f82cee4924b62",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1c5b04539f5d451e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/conf.js.meta",
      "sha256": "be94ab5babcfb8dfb002fb506ef6fda86402e4ad1df02d9c45658fbc523acc82",
      "bytes": 166,
      "artifact_id": "artifact-1c5b04539f5d451e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1345a66a28d64662": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/file-info.js",
      "sha256": "f0db1cc7c28a00bf1647325d2ade29f9175c3889d97e58a9f4cf5762b2162c4d",
      "bytes": 1574,
      "artifact_id": "artifact-1345a66a28d64662",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1e6d2cade3b64611": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/file-info.js.meta",
      "sha256": "f4c7747af79b7ab6083ce4caae94b3b6819fb904a4a2924bbcbb5cb009745c7c",
      "bytes": 166,
      "artifact_id": "artifact-1e6d2cade3b64611",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1813c0ded7b740b7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/fix.js",
      "sha256": "221fbd407c603fed4592ca6df54a4b696ce05eaff97c515caf78bb38fa60aadb",
      "bytes": 2776,
      "artifact_id": "artifact-1813c0ded7b740b7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f006b2605ea9433e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/fix.js.meta",
      "sha256": "420a8b4d956687a5d3b09a64eef7f91626896eaae70b3c7baa053fcc24c9c65b",
      "bytes": 166,
      "artifact_id": "artifact-f006b2605ea9433e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6eadee1398954a78": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font.meta",
      "sha256": "7b47ea70be8a6d12816c22fa1a47258e436db79ec3fd0059604dfbef4756215c",
      "bytes": 171,
      "artifact_id": "artifact-6eadee1398954a78",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f06ebeeb6b754ac6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/fix-cmap.js",
      "sha256": "c56997b41d07c3c09dd1fb222314533c42a3b1fe3878346adb0b1b378bf280bd",
      "bytes": 2239,
      "artifact_id": "artifact-f06ebeeb6b754ac6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f7ca73f4213847f7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/fix-cmap.js.meta",
      "sha256": "1bcd93b21f66601fca595cbcb5f3098aae284a688a0a660c1755cde8e0e54579",
      "bytes": 166,
      "artifact_id": "artifact-f7ca73f4213847f7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-67bfd4b183214e0e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/index.js",
      "sha256": "4cf142ac29ce039b770ab59c007b8831e0994870f42b64e04a9f1fd42a6a57ee",
      "bytes": 7025,
      "artifact_id": "artifact-67bfd4b183214e0e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-46f79c339e7a4ea4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/index.js.meta",
      "sha256": "a92c95a4326eb9093fe0b463d83350520d66c97c07eb3f087fdca3f704738788",
      "bytes": 166,
      "artifact_id": "artifact-46f79c339e7a4ea4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2276d1e0fe66459f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/read-metrics.js",
      "sha256": "1d6e8c0adcda57d1dbca8c2cec56914dc166141ef669d96a8d182b0501d079b7",
      "bytes": 1352,
      "artifact_id": "artifact-2276d1e0fe66459f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2ebd11de383a43b7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/read-metrics.js.meta",
      "sha256": "db9931f78fea3d0b2e3d0ace5e88743bf90ecf8f44cb80eaea1e81b0ba3cf58f",
      "bytes": 166,
      "artifact_id": "artifact-2ebd11de383a43b7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-db17675ae34c450e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/split-sc.js",
      "sha256": "33f2ae12164e594d13558b28be20a37d148bc591ba05fa6a9e7943bb935be8a1",
      "bytes": 4615,
      "artifact_id": "artifact-db17675ae34c450e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-21755e37b2cd4735": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/split-sc.js.meta",
      "sha256": "a47cf402b4dc973244e3f1ec096d48996d830ba231862329ec02cd6fd6c9e951",
      "bytes": 166,
      "artifact_id": "artifact-21755e37b2cd4735",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-854b1b7305de4492": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/util.js",
      "sha256": "e49657878099a688efaea330fb913a1d174c4523a8a25ab67d4206ee7aba0baa",
      "bytes": 462,
      "artifact_id": "artifact-854b1b7305de4492",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-49b173ae85a244eb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/font/util.js.meta",
      "sha256": "249d9c2b8b9d286409fda69b34b5181776739ccf32f543198d71ac1f68193e76",
      "bytes": 166,
      "artifact_id": "artifact-49b173ae85a244eb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-42ea110de780401f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/fs.js",
      "sha256": "d28ff7791372fcdf6ab40c1390131d57b256625e7b7288ca5aa7e3bbc6e95d08",
      "bytes": 16218,
      "artifact_id": "artifact-42ea110de780401f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-809da36eab484fae": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/fs.js.meta",
      "sha256": "292107577c8accc18e53a8d59c72d6b540b4ec08774dffe8d346743d97c6a0e3",
      "bytes": 166,
      "artifact_id": "artifact-809da36eab484fae",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0125c949bb9a4fe3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/game-club.js",
      "sha256": "67c3a4032fe27b02e515455858c9cab59d1c2a8142bd7c060d45f135cda16c90",
      "bytes": 2503,
      "artifact_id": "artifact-0125c949bb9a4fe3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bd5c461307314225": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/game-club.js.meta",
      "sha256": "f8b97537e9cf0f6e31aec69f4a27f210ee6d98d3754c0a6b7244a3c4e807eaff",
      "bytes": 166,
      "artifact_id": "artifact-bd5c461307314225",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-20499843a459438f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/game-recorder.js",
      "sha256": "7ae459dd5ac89586b8c595b50b1d525312cba0381297132eff0ed821cc9cb8ba",
      "bytes": 3166,
      "artifact_id": "artifact-20499843a459438f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e4d43be081524437": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/game-recorder.js.meta",
      "sha256": "17882615008b2e70252bcf7e1bf034b9a2a56767bd3772c294785ccba8217676",
      "bytes": 166,
      "artifact_id": "artifact-e4d43be081524437",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6d3532392852464b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/gyroscope.meta",
      "sha256": "7154db7e2d2edaf31cccf98085e7c0cf47e5ee9dbe0e008499be22e4b77223d5",
      "bytes": 171,
      "artifact_id": "artifact-6d3532392852464b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e584d64b12f3421f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/gyroscope/index.js",
      "sha256": "a335e1885729ecd6ee5a4aee1fac32822dc23f8c03c40aab5bb89fcbd7f92d12",
      "bytes": 2549,
      "artifact_id": "artifact-e584d64b12f3421f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7cf296de8fb044e3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/gyroscope/index.js.meta",
      "sha256": "eb57009396b8f8272dd8d98a30e9c9df170f189b579cdc5402b6cb62cdbd7063",
      "bytes": 166,
      "artifact_id": "artifact-7cf296de8fb044e3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2bf57517471d48bc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/index.js",
      "sha256": "e1fd0f0b10d50c6810749fca34240a0048ce25ccb509f34e36fe3dd5c5952880",
      "bytes": 3016,
      "artifact_id": "artifact-2bf57517471d48bc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d6665604e81b448a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/index.js.meta",
      "sha256": "d0e82cc4216c3d0444da4fa35fda81334f8f13fa2a8b314cf6e6ed52b7bad754",
      "bytes": 166,
      "artifact_id": "artifact-d6665604e81b448a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9e82b85e47fd4b76": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/logger.js",
      "sha256": "7d765a2c857a05424d08fc4cca83ba32393558f79fbc46c653a98bf440b03013",
      "bytes": 620,
      "artifact_id": "artifact-9e82b85e47fd4b76",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-427b2f07ecbf40cb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/logger.js.meta",
      "sha256": "5997ce0a94effab558dc26bb770ba2e380c150081cda9604bef7e19147baedb0",
      "bytes": 166,
      "artifact_id": "artifact-427b2f07ecbf40cb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c07682c3f7b84191": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/mobileKeyboard.meta",
      "sha256": "38c982147109661a8071158f2ff3d06cb7c5b902371e63b25cebf19981c35a3d",
      "bytes": 171,
      "artifact_id": "artifact-c07682c3f7b84191",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a71788caed214b1e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/mobileKeyboard/index.js",
      "sha256": "fff32a092621032170bd2ef8f92bf95d0a2607a239634e659e6b9bee6f01be0b",
      "bytes": 4273,
      "artifact_id": "artifact-a71788caed214b1e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9f777820fa904178": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/mobileKeyboard/index.js.meta",
      "sha256": "7129f174e5c0aed66dfe98c638105303b6c2bf1f6d2c46b8e7baef3248c0de52",
      "bytes": 166,
      "artifact_id": "artifact-9f777820fa904178",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0cf3c87d85e14b1f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/module-helper.js",
      "sha256": "5be6623e72561c69479b279dbb423b09e70041c914a465d9ff2837d03cb8e913",
      "bytes": 355,
      "artifact_id": "artifact-0cf3c87d85e14b1f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a4287bd7a7be45cd": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/module-helper.js.meta",
      "sha256": "8f0b69ef57dc4b8377ca102e7232bb0304751edcfad518a422cbfbebf6e93f9b",
      "bytes": 166,
      "artifact_id": "artifact-a4287bd7a7be45cd",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c1c1aa1a4f684fc9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/open-data.js",
      "sha256": "40848eeddcfaa064c65c269640a438e86ffa244411806a55d7d6311d8cf2b8a9",
      "bytes": 7648,
      "artifact_id": "artifact-c1c1aa1a4f684fc9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-daf78334b58c48ae": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/open-data.js.meta",
      "sha256": "cbded351e34211ce1e049e3d7ff693a46eb95bc964229248dc2a571daf0af509",
      "bytes": 166,
      "artifact_id": "artifact-daf78334b58c48ae",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9849d1fbe3a94f97": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/recorder.js",
      "sha256": "eaba87090f60fb22bb59bf1e82dd16a5aae134b2e9dc08c403f79c61652b51cf",
      "bytes": 4469,
      "artifact_id": "artifact-9849d1fbe3a94f97",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0c3f76b5a3294194": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/recorder.js.meta",
      "sha256": "fb7d26652120c89e358a3696a88a7d6ce85d2b02d5a6556ed9a77c4c5b5cc5a7",
      "bytes": 166,
      "artifact_id": "artifact-0c3f76b5a3294194",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b701a86dd9b444ef": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/resType.js",
      "sha256": "e2135586c03404792b961ea032e53d7000f552a4b61cb066896d149b13b6d2cf",
      "bytes": 30137,
      "artifact_id": "artifact-b701a86dd9b444ef",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9509acde384d4004": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/resType.js.meta",
      "sha256": "d4cc8d4ddb56429c2770dab0c6badad7b8396229bc00c85ef5faa2cd78e63cf7",
      "bytes": 166,
      "artifact_id": "artifact-9509acde384d4004",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ce7f4309f83e4454": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/resTypeOther.js",
      "sha256": "af5e7538fad8f6640239322cdfc7a6c5181995551cd7a6772b8311b2df55b247",
      "bytes": 2293,
      "artifact_id": "artifact-ce7f4309f83e4454",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fdf5ddd887c54573": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/resTypeOther.js.meta",
      "sha256": "d5c0b5265af1c10094f1b60d269c6a722c3c6f5f4555d8d702bfd57c43529a9c",
      "bytes": 166,
      "artifact_id": "artifact-fdf5ddd887c54573",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-338428a16cd1461f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/response.js",
      "sha256": "9bf4b44ef758ec8917aac3a1827de70f27bacc881849bf985cafaab313216304",
      "bytes": 1891,
      "artifact_id": "artifact-338428a16cd1461f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-66177747a24b41d0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/response.js.meta",
      "sha256": "d8f8575dd24894d152c6c4b92813d36923d44f3b98038a2d2e4d471840a606b7",
      "bytes": 166,
      "artifact_id": "artifact-66177747a24b41d0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8cdcd71a408b45e9": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/sdk.js",
      "sha256": "8377aca08a51213a937cd6fe0b49837dccaa607e1d73dd1c955d7181324811f1",
      "bytes": 17209,
      "artifact_id": "artifact-8cdcd71a408b45e9",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-750d5d7618ca477d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/sdk.js.meta",
      "sha256": "722a2f3ab338e74f87fb7076b2ab71819e05ffb18083fed0bb25e4bcca76c5fa",
      "bytes": 166,
      "artifact_id": "artifact-750d5d7618ca477d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-21f56040421f418b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/share.js",
      "sha256": "d97cc99e4cb4d86a2b55c0a0bcc0f4a331cd64a582c24b11c61161843dbf16c4",
      "bytes": 808,
      "artifact_id": "artifact-21f56040421f418b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-be8bd54e89df427a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/share.js.meta",
      "sha256": "a68b248cbb32d311f382571b35f77943b1ff8c47c7d4dae1abbba2b12e56462b",
      "bytes": 166,
      "artifact_id": "artifact-be8bd54e89df427a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e6adfbf3b12f4bc8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/storage.js",
      "sha256": "17bdfc50d2aef3f61ad34a94dadbb3989a96f4fb148713315c464261c6291948",
      "bytes": 4285,
      "artifact_id": "artifact-e6adfbf3b12f4bc8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d2c3eea920bf46d2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/storage.js.meta",
      "sha256": "f9b5191f5574146f084d8484fc7f9ff9518729a91b28e79b42c2fb54147eda6a",
      "bytes": 166,
      "artifact_id": "artifact-d2c3eea920bf46d2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-97b2e2f169ae476d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/texture.js",
      "sha256": "f353d9867cf476771572b65fe24a4bd99eb026f721ac98f9a37e58eaa4b9c7a0",
      "bytes": 11059,
      "artifact_id": "artifact-97b2e2f169ae476d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6eda229157e24227": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/texture.js.meta",
      "sha256": "d25434b89e79464f8388d3452f343e8c970a974ceab8001d545d8ffb099ae40e",
      "bytes": 166,
      "artifact_id": "artifact-6eda229157e24227",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4b10989577b947f4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/touch.meta",
      "sha256": "51d54dafa2e9fa3ca98645d1221fd93008b7354c308302f2f61c27d5257937e7",
      "bytes": 171,
      "artifact_id": "artifact-4b10989577b947f4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5746c0cf8b3f40bf": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/touch/index.js",
      "sha256": "a5917a61534de7bc2f5224a9c9402863a264cf6bd2106d5536eb6018299de53f",
      "bytes": 2301,
      "artifact_id": "artifact-5746c0cf8b3f40bf",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c88af6b0d3ed4597": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/touch/index.js.meta",
      "sha256": "035ba63a5488e2858c9d650ce29f75bc28e86667b8765f83ad18be8c8632bf4c",
      "bytes": 166,
      "artifact_id": "artifact-c88af6b0d3ed4597",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-421ce11c689744af": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/upload-file.js",
      "sha256": "3dbfd0ea7257b5c9e7191fd27f9783d86ecfec4e7d9220ebc4c9cf8d6ec4c3d4",
      "bytes": 2657,
      "artifact_id": "artifact-421ce11c689744af",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ba8cb9d7bf874170": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/upload-file.js.meta",
      "sha256": "1ff8a265fc8dd6457c593b7f46d2ef92929acaba862ec51e77ca6c6383c3e65d",
      "bytes": 166,
      "artifact_id": "artifact-ba8cb9d7bf874170",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-74db7a5731dc46a3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/userinfo.js",
      "sha256": "8c9373e5f77ee9b64b08e643e81bc45344d2b57e3aa78c582d2c0c53e099f06c",
      "bytes": 2922,
      "artifact_id": "artifact-74db7a5731dc46a3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cd7abaa194794a57": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/userinfo.js.meta",
      "sha256": "f258b5615381df319ea038cb65082df4084ba4e99fa376272c5ee41fed9d17bb",
      "bytes": 166,
      "artifact_id": "artifact-cd7abaa194794a57",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-015b7603e3844185": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/util.js",
      "sha256": "a54a33bb6894e642d3cd36f79c506c8a827c51bbdd56f61f0e230aaf1523f24f",
      "bytes": 6407,
      "artifact_id": "artifact-015b7603e3844185",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2352ffcc14d24e73": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/util.js.meta",
      "sha256": "adbab5870129a9bdd211d2932fdfd614dff1d422f646073b2d2cedf32d2752be",
      "bytes": 166,
      "artifact_id": "artifact-2352ffcc14d24e73",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9258d1f9ed784f2e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/utils.js",
      "sha256": "96bfd23dbd18b840fad664c3e1c5263cb11674c4e0cadc7511637ae166a61c32",
      "bytes": 13273,
      "artifact_id": "artifact-9258d1f9ed784f2e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c428f9243b974e3a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/utils.js.meta",
      "sha256": "e7fff5da726f89f4ff9f49ad51bb1fe870cf7f3de6f16ba23531b88a0974f1c1",
      "bytes": 166,
      "artifact_id": "artifact-c428f9243b974e3a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6c8901ba47544ba1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video.js",
      "sha256": "b2d156cb1b437508d55bf8a32a9096f6d370ddf196f63bf5113d9c4597e2e128",
      "bytes": 2552,
      "artifact_id": "artifact-6c8901ba47544ba1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4cd98571338e429b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video.js.meta",
      "sha256": "a1e0ab3bd71cd4d27737fb3c07bfbc143e48875ba4772ccfc47e3a6437381966",
      "bytes": 166,
      "artifact_id": "artifact-4cd98571338e429b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5c534fdba689424f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video.meta",
      "sha256": "3c9735c4e88cf8c19158e96f3db50162d30419f8de68fa0c8cd11813926a416e",
      "bytes": 171,
      "artifact_id": "artifact-5c534fdba689424f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3cb6cb4b57d7437b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video/index.js",
      "sha256": "de109cc788e8b94020aaa9bc48a8fe38565570962a70791f622e2cc9b2381ff8",
      "bytes": 23528,
      "artifact_id": "artifact-3cb6cb4b57d7437b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-12d3fc5d9f984571": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/unity-sdk/video/index.js.meta",
      "sha256": "47c892ccea0089815f76ad801cad95b0272d0d16d3e403d24ce8d8fea0b401b1",
      "bytes": 166,
      "artifact_id": "artifact-12d3fc5d9f984571",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4cc70743eda84dd6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/wasmcode.meta",
      "sha256": "d6637adf8e3b24af3b6a35cca86d96af27451065712dbccb79972100898ed1d3",
      "bytes": 171,
      "artifact_id": "artifact-4cc70743eda84dd6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-3687d2d80bb443c0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/wasmcode/game.js",
      "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "bytes": 0,
      "artifact_id": "artifact-3687d2d80bb443c0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-cac5a79059204fbe": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/wasmcode/game.js.meta",
      "sha256": "021e4b46fcab07238973ab63d422c48073158cee27a75bbd1505b923445dc8f0",
      "bytes": 166,
      "artifact_id": "artifact-cac5a79059204fbe",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-321cb4b8c27b4ae8": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/weapp-adapter.js",
      "sha256": "2823cce28ea12bcdc8739eb174bfc85ca85007d540c275630913b2c3a8931a45",
      "bytes": 74701,
      "artifact_id": "artifact-321cb4b8c27b4ae8",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-764ffd236e5e46de": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/weapp-adapter.js.meta",
      "sha256": "ce789532602506fc3ef1f847f57332cc8eb7951886debd5682a942ecc3d97c78",
      "bytes": 166,
      "artifact_id": "artifact-764ffd236e5e46de",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0902a9d4a24241f4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/workers.meta",
      "sha256": "e50b024111ef3967f66b5487e12d8938de824ffacf176ac73c8708b006b5f7ee",
      "bytes": 171,
      "artifact_id": "artifact-0902a9d4a24241f4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-61c1c3e3bb454463": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/workers/response.meta",
      "sha256": "a5583230b037d8807fd3a2d224a28654fcaa2ca336ca3c798acd46e1e0b1d052",
      "bytes": 171,
      "artifact_id": "artifact-61c1c3e3bb454463",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fb103113ee994c3f": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/workers/response/index.js",
      "sha256": "095fe2b7d7252ccd9bd5da0de81143f730a50aea25b1ceb32d796eca1ef1f028",
      "bytes": 2203,
      "artifact_id": "artifact-fb103113ee994c3f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-02c3c26729094c5e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/Runtime/wechat-default/workers/response/index.js.meta",
      "sha256": "ba096d160f11370538b687763cbc989389efe82d0eb76943495ddc40a57b0311",
      "bytes": 166,
      "artifact_id": "artifact-02c3c26729094c5e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-987612ac4b8c4e20": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates.meta",
      "sha256": "3b111fd685d2b82c0f8c1e6b04f4ff101658d03c900c2d546bd780f89ca88fb0",
      "bytes": 172,
      "artifact_id": "artifact-987612ac4b8c4e20",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-82f91e94f0c84c49": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate.meta",
      "sha256": "81b40534f21d83337d244ac7220909f978cb52f10d2db2d4f1cfe98ee262029f",
      "bytes": 172,
      "artifact_id": "artifact-82f91e94f0c84c49",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a309041dd0724b71": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate/index.html",
      "sha256": "4e2af244554a47481d204fbbc6e4f9fb70769d82a3d3624d81da27b272344068",
      "bytes": 16752,
      "artifact_id": "artifact-a309041dd0724b71",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7690708c4b7b4c2e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate/index.html.meta",
      "sha256": "bee81dc01cd6c293f6d2a1f1f7eee3ee1f44e3db5c20364dcb7df4a964ec969c",
      "bytes": 155,
      "artifact_id": "artifact-7690708c4b7b4c2e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-77e3c4e886634dd0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate/thumbnail.png",
      "sha256": "3c3be7555ed97f7e90e62bcabbb189d3068593405864ce5645c090781c515875",
      "bytes": 8219,
      "artifact_id": "artifact-77e3c4e886634dd0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d2b09a72a1ab4399": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate/thumbnail.png.meta",
      "sha256": "68fdd0d9613361b7409814e1d130207cc45de78c2d0d2435549655609340fc7a",
      "bytes": 155,
      "artifact_id": "artifact-d2b09a72a1ab4399",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-40d039f21b924b1d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020.meta",
      "sha256": "342136c6857397a1ebf85cd5c299cbda861d90063ff75efc02422fa27c2a815d",
      "bytes": 172,
      "artifact_id": "artifact-40d039f21b924b1d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-71bb9395405e45de": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020/index.html",
      "sha256": "94c09b1d9eabf083f49ec790c5036850e957b8fd004892a88df4e50c1f3e984c",
      "bytes": 19291,
      "artifact_id": "artifact-71bb9395405e45de",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-82953154c98a4293": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020/index.html.meta",
      "sha256": "7f2afe373427316f9b4199a646bdb13637d7e97577ad72dd0a7c8ecf52d56932",
      "bytes": 155,
      "artifact_id": "artifact-82953154c98a4293",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-40c28aacb2014bcb": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020/thumbnail.png",
      "sha256": "09c27d0e20d35546af277a39f65a45d7ff4da7ae82ae904c983dbbd632d61371",
      "bytes": 8266,
      "artifact_id": "artifact-40c28aacb2014bcb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-70ee733e07cb4454": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2020/thumbnail.png.meta",
      "sha256": "6089a11f61ba0ec16bda8ad33f2dfcaf147dc0e24fd56fd398fb3a583eec7f91",
      "bytes": 155,
      "artifact_id": "artifact-70ee733e07cb4454",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-af2b2637a7f04969": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022.meta",
      "sha256": "38870b98fb27d70befa0058851cdc9bba66df985bf17b8d7d05573f88139d722",
      "bytes": 172,
      "artifact_id": "artifact-af2b2637a7f04969",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-442bcf5d8bec4f58": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData.meta",
      "sha256": "eb831fdd083993eb86f98e16ea9bf58c0a729fe55055f96ba4b8bc287bd44d7e",
      "bytes": 172,
      "artifact_id": "artifact-442bcf5d8bec4f58",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-35f84eb1e5ef4ee6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/MemoryProfiler.png",
      "sha256": "0ce87be20d0c24ab6cb73b57de2004da261cc2a641e69319c6ae3fe11fc85483",
      "bytes": 665,
      "artifact_id": "artifact-35f84eb1e5ef4ee6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2ed23373bc0e493b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/MemoryProfiler.png.meta",
      "sha256": "f89e227971148f8f67981a97f0a014ab534e35d0fa77a7b5b3fa8e1ed22a57c1",
      "bytes": 3334,
      "artifact_id": "artifact-2ed23373bc0e493b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8c29f57d7f364007": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/favicon.ico",
      "sha256": "9c13beb90ee8f70580d52a21d5233970d1c89e71e4a34a462c22610941c3c77f",
      "bytes": 2305,
      "artifact_id": "artifact-8c29f57d7f364007",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-517972c53f754748": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/favicon.ico.meta",
      "sha256": "7c792d1a5eecf414634a628c990e13d9e92a84fd9470c2c83b0701888e758643",
      "bytes": 155,
      "artifact_id": "artifact-517972c53f754748",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-82699f7482294f44": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/fullscreen-button.png",
      "sha256": "21221581673a54b8139d408d4a3f8d2b879e86827d4b6fc53b995ff7a99ee3e9",
      "bytes": 175,
      "artifact_id": "artifact-82699f7482294f44",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-80860fe7988b4231": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/fullscreen-button.png.meta",
      "sha256": "fe7e7dc1f2995f49daeae6b12309549724d7f9cfcb1efa69c862f5ac7c7e3855",
      "bytes": 3334,
      "artifact_id": "artifact-80860fe7988b4231",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c0f0b418b4e14f7c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-empty-dark.png",
      "sha256": "bbee7131afe8a3365906240d89184dc86234c119467f390bc4bc6802328fdb4d",
      "bytes": 96,
      "artifact_id": "artifact-c0f0b418b4e14f7c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e19aa05a7a3c4938": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-empty-dark.png.meta",
      "sha256": "ee0e67498ec155f48a9a78e45aab84ceb74273ad6b4cea923590ab089f199af2",
      "bytes": 3334,
      "artifact_id": "artifact-e19aa05a7a3c4938",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7f38cbbcbeb44a25": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-empty-light.png",
      "sha256": "143696c060ad43d6c30f19ff5f49000927a139a2a1e4c9f45fafaa2b90d7d2be",
      "bytes": 109,
      "artifact_id": "artifact-7f38cbbcbeb44a25",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2356a41daa9543ef": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-empty-light.png.meta",
      "sha256": "7cf7effc63b36c60eefa8d48de2c61d21c72879843bc1a7d2af95f8dd7b6af60",
      "bytes": 3334,
      "artifact_id": "artifact-2356a41daa9543ef",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-177ba54a814042b4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-full-dark.png",
      "sha256": "3306a6244dcb3926fca38a28e3ced589df8ff1beed955eb17c0bbf01c918bc62",
      "bytes": 74,
      "artifact_id": "artifact-177ba54a814042b4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bc413f3f46c04b69": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-full-dark.png.meta",
      "sha256": "4a2e9ede8eabf214a83a7c7646d97ad15d0a950dd272b5feeb15a9d4ba07b20c",
      "bytes": 3334,
      "artifact_id": "artifact-bc413f3f46c04b69",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-9a0ca685d1e144a4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-full-light.png",
      "sha256": "12395ea785480c5cdf12fade6e6cbf49666d5bb1cef7240c113dbcba6bdc5c87",
      "bytes": 84,
      "artifact_id": "artifact-9a0ca685d1e144a4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-273305f78edd4367": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/progress-bar-full-light.png.meta",
      "sha256": "5f8b66635991553bc6af39ca26f2890ddd6e32ea8825746a9bf18b2bf400e7d8",
      "bytes": 3334,
      "artifact_id": "artifact-273305f78edd4367",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b7b11f30fbe349c1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/style.css",
      "sha256": "f19641da9ff37a7480a77c76491ce539b3fd8126e85f15199bd888f5bb02d1fe",
      "bytes": 1556,
      "artifact_id": "artifact-b7b11f30fbe349c1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1838e97b04f048cf": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/style.css.meta",
      "sha256": "f9fed6966b4c28458a148adae306f6630e2e7ad38cba15d9572e1e81d6697ea1",
      "bytes": 155,
      "artifact_id": "artifact-1838e97b04f048cf",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a94752303b934a7d": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/unity-logo-dark.png",
      "sha256": "c1b72d26c096487dabc948b54bc203f8dac7ed4e3f5733918798e858acb4b159",
      "bytes": 3042,
      "artifact_id": "artifact-a94752303b934a7d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-299c4d3c2c354af3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/unity-logo-dark.png.meta",
      "sha256": "bcacf156cf201d9b856eab6c51e8d8b3964716309826ccf6349ea0edf22d6956",
      "bytes": 3334,
      "artifact_id": "artifact-299c4d3c2c354af3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b9a009bb5cc645a1": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/unity-logo-light.png",
      "sha256": "002990aea0d946833cadc1519d5b7e50a4570bd537a0517dd79a59d3eec84da7",
      "bytes": 3077,
      "artifact_id": "artifact-b9a009bb5cc645a1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2244f0e4001a4adc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/unity-logo-light.png.meta",
      "sha256": "2ba92b3d7a0accf4e181d5df73e4963cd075d0faf7ee35640e3bed5f77958742",
      "bytes": 3334,
      "artifact_id": "artifact-2244f0e4001a4adc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8adc79c0b4a24e9e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/webgl-logo.png",
      "sha256": "b30c3af2a4538c6edf5f2411953760641dfa257f2a4cc5b88d671aa243b1f12f",
      "bytes": 2947,
      "artifact_id": "artifact-8adc79c0b4a24e9e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-172a2eab571a4c68": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/webgl-logo.png.meta",
      "sha256": "31d06916ed93d8fe2951c664314bcdfa83397d0877714f6573fbbd4a34bd0ca8",
      "bytes": 3334,
      "artifact_id": "artifact-172a2eab571a4c68",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4cfa911495f64dd7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/webmemd-icon.png",
      "sha256": "c56f5494640cedda273aad38d58e6ab9da625d5b3f4d875c0ff92c6192a569c2",
      "bytes": 1670,
      "artifact_id": "artifact-4cfa911495f64dd7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-850e04fcb99c4950": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/TemplateData/webmemd-icon.png.meta",
      "sha256": "5cdc06843328841d68050b3588882a8ace7d2df2337d401d90949561449242ec",
      "bytes": 3334,
      "artifact_id": "artifact-850e04fcb99c4950",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7635b785fdd94cbc": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/index.html",
      "sha256": "0c155598460924ac9b0e7dd10057feae83959789b7defe08bd3946a97c576969",
      "bytes": 22887,
      "artifact_id": "artifact-7635b785fdd94cbc",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c7343e9fe8cf40fe": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/index.html.meta",
      "sha256": "3b11000012e99eb759530cbac2548440b288e713b3534c0d025619f1a77931a7",
      "bytes": 158,
      "artifact_id": "artifact-c7343e9fe8cf40fe",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a90ad5bf577742e6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/thumbnail.png",
      "sha256": "e5b442bd5fe5283a1a0283408fe87df270e58d7b52171e1ffb7042350b96eaf0",
      "bytes": 5256,
      "artifact_id": "artifact-a90ad5bf577742e6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e6e47c433ec54435": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022/thumbnail.png.meta",
      "sha256": "c77a8f81092b51df09cab71ad368e5027b561db3da4b35b3829eb596763473de",
      "bytes": 3334,
      "artifact_id": "artifact-e6e47c433ec54435",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d74e4a0f4684452b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ.meta",
      "sha256": "084b0b107f4cad60103f37d376d215d55a7939307c706372e34ea0c009fea94b",
      "bytes": 172,
      "artifact_id": "artifact-d74e4a0f4684452b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-337444de60044185": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData.meta",
      "sha256": "c890a265431594103bd9a6de11ec58a94fe9fca188c280baf85268249bc79f3f",
      "bytes": 172,
      "artifact_id": "artifact-337444de60044185",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1e9e0ac523704a77": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/MemoryProfiler.png",
      "sha256": "0ce87be20d0c24ab6cb73b57de2004da261cc2a641e69319c6ae3fe11fc85483",
      "bytes": 665,
      "artifact_id": "artifact-1e9e0ac523704a77",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-66b801865158441b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/MemoryProfiler.png.meta",
      "sha256": "d091993bb2c30659d67c3f2211be6a29e5a33c0c28acacfa6d2f93de9f03bc49",
      "bytes": 155,
      "artifact_id": "artifact-66b801865158441b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-370f2e8443604659": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/favicon.ico",
      "sha256": "9c13beb90ee8f70580d52a21d5233970d1c89e71e4a34a462c22610941c3c77f",
      "bytes": 2305,
      "artifact_id": "artifact-370f2e8443604659",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b01ce47f79854ff0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/favicon.ico.meta",
      "sha256": "7d8cea93a62bb9cf8047ad24afbe6ef1de135c6dad36946ce9a8188a13e789b4",
      "bytes": 155,
      "artifact_id": "artifact-b01ce47f79854ff0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7ed29055b5274f51": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/fullscreen-button.png",
      "sha256": "21221581673a54b8139d408d4a3f8d2b879e86827d4b6fc53b995ff7a99ee3e9",
      "bytes": 175,
      "artifact_id": "artifact-7ed29055b5274f51",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-afbfdb8f3f4c4486": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/fullscreen-button.png.meta",
      "sha256": "e0586365494a832af44d0fe6e5163018b63e2996a2850a9fb1a970d768402b72",
      "bytes": 155,
      "artifact_id": "artifact-afbfdb8f3f4c4486",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c537511e53e54677": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-empty-dark.png",
      "sha256": "bbee7131afe8a3365906240d89184dc86234c119467f390bc4bc6802328fdb4d",
      "bytes": 96,
      "artifact_id": "artifact-c537511e53e54677",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c7c3095886d74d98": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-empty-dark.png.meta",
      "sha256": "d1992e2c30fba633db9accc666d533caa6addb246ca77b2ea797a653ab8f60c1",
      "bytes": 155,
      "artifact_id": "artifact-c7c3095886d74d98",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2b854c73c84f45dd": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-empty-light.png",
      "sha256": "143696c060ad43d6c30f19ff5f49000927a139a2a1e4c9f45fafaa2b90d7d2be",
      "bytes": 109,
      "artifact_id": "artifact-2b854c73c84f45dd",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-0e55a8bb03ab4672": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-empty-light.png.meta",
      "sha256": "e0b1f4a228553408e0ac073f0fc69083d89456317256cca633d41ec254903e58",
      "bytes": 155,
      "artifact_id": "artifact-0e55a8bb03ab4672",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f34e9ea8e9724910": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-full-dark.png",
      "sha256": "3306a6244dcb3926fca38a28e3ced589df8ff1beed955eb17c0bbf01c918bc62",
      "bytes": 74,
      "artifact_id": "artifact-f34e9ea8e9724910",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-76241036fba44ced": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-full-dark.png.meta",
      "sha256": "fe52a356805726110dbfef8a1cd17daa75577a3d0a7c0d1bd1696614deae04f3",
      "bytes": 155,
      "artifact_id": "artifact-76241036fba44ced",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a46ff628be3f4ef6": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-full-light.png",
      "sha256": "12395ea785480c5cdf12fade6e6cbf49666d5bb1cef7240c113dbcba6bdc5c87",
      "bytes": 84,
      "artifact_id": "artifact-a46ff628be3f4ef6",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-de359be1a1934fc4": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/progress-bar-full-light.png.meta",
      "sha256": "69a4f30aa8ec6c100d12fa510a27f05a576bf6c0bab022ce6c8d71695c15d972",
      "bytes": 155,
      "artifact_id": "artifact-de359be1a1934fc4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7c77fa7a88854b31": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/style.css",
      "sha256": "e8f921fba47cd09e8e752c1dd99305feebfed8f0f04d69e61877b45c6eed6414",
      "bytes": 1468,
      "artifact_id": "artifact-7c77fa7a88854b31",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-1699505a44674ee2": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/style.css.meta",
      "sha256": "adf798b4f73d8190a893d441aa5a2baa96c95efd17fadc942e814c1862265510",
      "bytes": 155,
      "artifact_id": "artifact-1699505a44674ee2",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6a9fc84f52ca42cf": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/tuanjie-logo-dark.png",
      "sha256": "c1b72d26c096487dabc948b54bc203f8dac7ed4e3f5733918798e858acb4b159",
      "bytes": 3042,
      "artifact_id": "artifact-6a9fc84f52ca42cf",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7ffac195d2ad4772": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/tuanjie-logo-dark.png.meta",
      "sha256": "33b5024a6e7d42304c5877af922814163731fabf6abcc42ed28eaab0634ea22a",
      "bytes": 155,
      "artifact_id": "artifact-7ffac195d2ad4772",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-84a6a28c55534018": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/tuanjie-logo-light.png",
      "sha256": "002990aea0d946833cadc1519d5b7e50a4570bd537a0517dd79a59d3eec84da7",
      "bytes": 3077,
      "artifact_id": "artifact-84a6a28c55534018",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-dc4bb8bbf6a943ab": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/tuanjie-logo-light.png.meta",
      "sha256": "2d5c57889432e065486f8ef9d81a06873b5bc904b636f72ff4959e51f4898214",
      "bytes": 155,
      "artifact_id": "artifact-dc4bb8bbf6a943ab",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-41f4eec761ae4fa3": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/webgl-logo.png",
      "sha256": "b30c3af2a4538c6edf5f2411953760641dfa257f2a4cc5b88d671aa243b1f12f",
      "bytes": 2947,
      "artifact_id": "artifact-41f4eec761ae4fa3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-5576d663b4e044c7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/webgl-logo.png.meta",
      "sha256": "32c26d348e7e641711afc7d49b3c6f8c53a1fb0f2e981afd5b5e98cb10d09578",
      "bytes": 155,
      "artifact_id": "artifact-5576d663b4e044c7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8e7e9b26b1f54a19": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/webmemd-icon.png",
      "sha256": "c56f5494640cedda273aad38d58e6ab9da625d5b3f4d875c0ff92c6192a569c2",
      "bytes": 1670,
      "artifact_id": "artifact-8e7e9b26b1f54a19",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ad6219c45f5140c7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/TemplateData/webmemd-icon.png.meta",
      "sha256": "9d6843df135c0fd74475ba6c3f430e71f3e4a79fa82dcb2a9ba77f538686ddfd",
      "bytes": 155,
      "artifact_id": "artifact-ad6219c45f5140c7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a342687717b547a7": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/index.html",
      "sha256": "69d17b79e0303ad0b26f08860b12f621ccec8e82ebecdbe7c56cf88250bb7c39",
      "bytes": 30535,
      "artifact_id": "artifact-a342687717b547a7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f66604d81599407c": {
      "path": "Unity/Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022TJ/index.html.meta",
      "sha256": "32c31e397375a74dfd2805ec309f909ea7d14b6ce18f5d5ef380f97afe94375e",
      "bytes": 155,
      "artifact_id": "artifact-f66604d81599407c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-de140e9a688d4af0": {
      "path": "Unity/Packages/com.qq.weixin.minigame/mainifest.json",
      "sha256": "06d3eb3d7352f5331ebf2b28f6f5c046d04de62fd5249655c79d851a0781fc3a",
      "bytes": 128,
      "artifact_id": "artifact-de140e9a688d4af0",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ee6caa601555447a": {
      "path": "Unity/Packages/com.qq.weixin.minigame/mainifest.json.meta",
      "sha256": "eac96b27b59fc64197ca4cfb3d52cba4b27d945e37f6c1138903d3ff2b78798b",
      "bytes": 158,
      "artifact_id": "artifact-ee6caa601555447a",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-526fe21003084c0b": {
      "path": "Unity/Packages/com.qq.weixin.minigame/package.json",
      "sha256": "4ccaa7aadc4495a286df7cb57b2c2734803fdef6b65d91cf029466e5ff0873fe",
      "bytes": 228,
      "artifact_id": "artifact-526fe21003084c0b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-89a87fc1abe4415e": {
      "path": "Unity/Packages/com.qq.weixin.minigame/package.json.meta",
      "sha256": "c6bca6ccc02118341a4a1e79089a210b00a8fd99cf4a8af0b00b41238152584e",
      "bytes": 158,
      "artifact_id": "artifact-89a87fc1abe4415e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f03680cfec7a4882": {
      "path": "Unity/Packages/packages-lock.json",
      "sha256": "a466220c68486629a1c319978bb19cf6ddab7fafdddba36c0a563516e6e4b593",
      "bytes": 958,
      "artifact_id": "artifact-f03680cfec7a4882",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a1a4459f4c3f410c": {
      "path": "Unity/ProjectSettings/AudioManager.asset",
      "sha256": "9a554916fe138ebcad8443ee9b079363ecc72795c39d0e80f5f7ca99e2c6f5e7",
      "bytes": 443,
      "artifact_id": "artifact-a1a4459f4c3f410c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-ecc6d7015557430f": {
      "path": "Unity/ProjectSettings/ClusterInputManager.asset",
      "sha256": "40e65dd214bf254955d2c8829ca8ce2130fd3510485560ce012e9a1ddd93e623",
      "bytes": 114,
      "artifact_id": "artifact-ecc6d7015557430f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-68db6fc4f25f47cb": {
      "path": "Unity/ProjectSettings/DynamicsManager.asset",
      "sha256": "67900c2934d84fa99a3cbc8701e931840f3f008781fa8d7b842a10794f082e65",
      "bytes": 1287,
      "artifact_id": "artifact-68db6fc4f25f47cb",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-15fc4efd88c54f15": {
      "path": "Unity/ProjectSettings/EditorSettings.asset",
      "sha256": "1a8e709ae6b71c53bc5d47100501ed71b088dd593a4239f78ac79864e724604d",
      "bytes": 1536,
      "artifact_id": "artifact-15fc4efd88c54f15",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-4df1695add254518": {
      "path": "Unity/ProjectSettings/GraphicsSettings.asset",
      "sha256": "fbf0856b7693639a5388ae693a455baaad354c1d8dc548601de3dc61c4ab12c3",
      "bytes": 2372,
      "artifact_id": "artifact-4df1695add254518",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-e4546ac4aa664b5c": {
      "path": "Unity/ProjectSettings/InputManager.asset",
      "sha256": "d0f454ddb880912abcae92e3e12957a7446786ba89e24937e019ff3bbc0d4300",
      "bytes": 5816,
      "artifact_id": "artifact-e4546ac4aa664b5c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-6dfcb6c17069483b": {
      "path": "Unity/ProjectSettings/MemorySettings.asset",
      "sha256": "1e8379a461b10ae09f3f696911088c16875d63e87ed5f2d95df9e1892703f9fd",
      "bytes": 1192,
      "artifact_id": "artifact-6dfcb6c17069483b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-bb2ec96453e74aee": {
      "path": "Unity/ProjectSettings/MultiplayerManager.asset",
      "sha256": "aba89fd1a1ddad182727ad82727e536d27372e90328a8196da2a4560398983dd",
      "bytes": 157,
      "artifact_id": "artifact-bb2ec96453e74aee",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7660c716c7404b58": {
      "path": "Unity/ProjectSettings/NavMeshAreas.asset",
      "sha256": "7ebbf8f53b8033104e1878b95ca07a28d0954092d81c220f14196a42f651e658",
      "bytes": 1361,
      "artifact_id": "artifact-7660c716c7404b58",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-50164b029a1f4524": {
      "path": "Unity/ProjectSettings/Physics2DSettings.asset",
      "sha256": "9b78fdd0bac0f486c052ca65ddc0e5022d322b12874931a49456d407c5bbfc00",
      "bytes": 1820,
      "artifact_id": "artifact-50164b029a1f4524",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-12877efa5ce340be": {
      "path": "Unity/ProjectSettings/PresetManager.asset",
      "sha256": "29c125923fe6e29f90190d4fce1c82d660e7051546e2c9d02908d18c9cf2436c",
      "bytes": 146,
      "artifact_id": "artifact-12877efa5ce340be",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-fcb8869d44a4499f": {
      "path": "Unity/ProjectSettings/QualitySettings.asset",
      "sha256": "5e5f38a7cb2f7d097614ae6cb7ba8d2b95ffd40d73a97230957fbd599b3af6b0",
      "bytes": 9638,
      "artifact_id": "artifact-fcb8869d44a4499f",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c6938d2130ff4d00": {
      "path": "Unity/ProjectSettings/TagManager.asset",
      "sha256": "da4b5eefc927f8b91bcf9e56a186d2b852f49d9cbe91d466638d26c79d6a83ce",
      "bytes": 411,
      "artifact_id": "artifact-c6938d2130ff4d00",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2c85018020cf4321": {
      "path": "Unity/ProjectSettings/TimeManager.asset",
      "sha256": "e6d0d31782b4896159d2f7249b1845004a7a9f4eb05bc952a8c64bc72cb34448",
      "bytes": 282,
      "artifact_id": "artifact-2c85018020cf4321",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-b79ca1caec3b461d": {
      "path": "Unity/ProjectSettings/UnityConnectSettings.asset",
      "sha256": "d6034acbef6c9e3179866486c31eab81fcbe5e7ed350ce71e0fa2b1449b77109",
      "bytes": 943,
      "artifact_id": "artifact-b79ca1caec3b461d",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-7d2ae3ce71054c34": {
      "path": "Unity/ProjectSettings/VFXManager.asset",
      "sha256": "9378043da38a17f2198d52a10c17789161d30e3285a387ca3687a71551c32448",
      "bytes": 492,
      "artifact_id": "artifact-7d2ae3ce71054c34",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c54d1b7f79854d6c": {
      "path": "Unity/ProjectSettings/VersionControlSettings.asset",
      "sha256": "e199cdfa6a8f2bf33cfd47e549198f13e3eb2a5099e908fdb4e0f14d939ccc6b",
      "bytes": 172,
      "artifact_id": "artifact-c54d1b7f79854d6c",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-d2d3a56acb5942f3": {
      "path": "build/wechat/IMPLEMENTATION.md",
      "sha256": "291284cd16a71a13c67169406512855f23a3403519d2b563aea239e535a67d7f",
      "bytes": 2374,
      "artifact_id": "artifact-d2d3a56acb5942f3",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-26960558160a4d8b": {
      "path": "build/wechat/compile-01.log",
      "sha256": "137876729c44781881151875f80c0156792d7d871ae3c23841c70c595ccbe34f",
      "bytes": 81387,
      "artifact_id": "artifact-26960558160a4d8b",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-c9ecfb53f1e34f72": {
      "path": "build/wechat/compile-02.log",
      "sha256": "2c7b0056480e4921270d9fc0adc874c4b90d4fb94dfb5880853b60cdd04d296d",
      "bytes": 122938,
      "artifact_id": "artifact-c9ecfb53f1e34f72",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-a1c9ccb92d634f2e": {
      "path": "build/wechat/compile-03-player.log",
      "sha256": "a4c0932c7edb4cfd28633895b73b9c818acceb392dd5d48f0080a467a425f89b",
      "bytes": 48895,
      "artifact_id": "artifact-a1c9ccb92d634f2e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-62413717679b4ce4": {
      "path": "build/wechat/compile-04-player.log",
      "sha256": "25513bbede3d7d2c85fbc00bb6adc67d6868bc3ecf4d9ecd7b02a7929806781d",
      "bytes": 31141,
      "artifact_id": "artifact-62413717679b4ce4",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-05ee4f2c72be4190": {
      "path": "build/wechat/compile-05-final.log",
      "sha256": "4b33244c75c19659ceb11cce74b2f624de2ab81753951fbffea3837a7db32c1a",
      "bytes": 21033,
      "artifact_id": "artifact-05ee4f2c72be4190",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-44b187dfa2df49ba": {
      "path": "Unity/Assets/HotpotSort/Runtime/Bootstrap/Bootstrap.cs",
      "sha256": "a369522573b14e049cc64bda06323b459842187e5e2c1622548c44c57767c274",
      "bytes": 3397,
      "artifact_id": "artifact-44b187dfa2df49ba",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-8dd002afd88445f1": {
      "path": "Unity/Assets/HotpotSort/Runtime/Bootstrap/HotpotSort.Bootstrap.asmdef",
      "sha256": "a9ad9c449852fa61049a7c61850bffe6ca1efe310bf3b8d0e8d5d2a482ed1a83",
      "bytes": 203,
      "artifact_id": "artifact-8dd002afd88445f1",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-f88d26eb8a844371": {
      "path": "Unity/Packages/manifest.json",
      "sha256": "cd760c2bb508248c8f565fbf592ddcb88fad226cb8a0e2f945c3f1eb6c89229b",
      "bytes": 197,
      "artifact_id": "artifact-f88d26eb8a844371",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-2aca2554343f40f7": {
      "path": "Unity/ProjectSettings/ProjectSettings.asset",
      "sha256": "70a165ceef3c3b94e9396551093c8350986da16f7ac055af625b87fc8759a41e",
      "bytes": 20082,
      "artifact_id": "artifact-2aca2554343f40f7",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    },
    "artifact-667914747c42491e": {
      "path": "Unity/ProjectSettings/ProjectVersion.txt",
      "sha256": "82ecb9975adc9bf786c746365a292ab5a7fe477f0f3fbac35326f5ebb324839f",
      "bytes": 85,
      "artifact_id": "artifact-667914747c42491e",
      "kind": "file",
      "run_id": "run-a7aef77a25184b78",
      "task_revision": 3
    }
  },
  "completed": {
    "designer": "run-3fbb0d66055346ba",
    "code": "run-a7aef77a25184b78"
  },
  "block": null
}
```

## 0. 任务标识

| 字段 | 内容 |
|---|---|
| Task ID | <从机器块读取，不在此另立值> |
| task_revision | <从机器块读取> |
| Status | <从机器块读取> |
| 负责 PM／会话 | <唯一业务协调者> |
| 类型／风险 | <Feature / Bug / Tuning / Tooling / Art>；<Low / Medium / High，说明实际风险> |
| 实现内容 | <Code Only / Art Only / Code + Art> |
| 视觉影响 | <None / Asset Only / Screen or Layout / Major Redesign> |
| 目标版本 | <尚未指定或 Release ID；最终纳入以版本记录为准> |
| 创建日期 | <YYYY-MM-DD> |

Status 只由机器块和 `harness.py` 维护：`DRAFT / READY / BUILDING / BLOCKED / READY_FOR_RELEASE / CANCELLED`；本节仅作可读索引。
`READY` 表示必要合同与构建授权齐全；`READY_FOR_RELEASE` 只表示实现交接完成，不表示 QA 通过或获得发布授权。

## 1. 需求、范围与约束

### 1.1 用户请求与目标

- 原始请求／忠实摘要：<保留真正提出的需求，不自行补充新目标>
- 来源：<用户消息、批注或已批准需求的可定位引用>
- 当前问题：<现在发生什么，为什么需要修改>
- 目标与用户可观察结果：<完成后用户能看到或做到什么>

### 1.2 功能范围

- 必须实现：<包含哪些行为、页面、状态和资源>
- 明确不做：<排除的功能、页面、重构或变体>
- 必须保留：<不允许改变的既有行为、数据、布局或美术元素>
- 硬约束：<平台、性能、兼容性、预算、依赖等；每项标明来源>
- 已确认前提：<前提及用户确认依据；没有则写“无”>

### 1.3 业务规则与状态变化

| 规则／场景 | 触发条件与输入 | 预期行为／状态变化 | 异常、空值或限制 | 确认依据 |
|---|---|---|---|---|
| <规则> | | | | |

尚未决定的产品行为记入第 6 节，不作为隐含默认值交给 Builder。

## 2. 验收意图／QA Intent

本节是 QA Intent 的人工起草／可读视图，由 PM 忠实汇总；确认后必须通过 `task contract` 写入机器块的 `contract.qa_intent`，后者才是唯一权威源。这里只定义“要验证什么”，不提前写完整测试步骤、坐标、等待时长或脚本。

| Intent ID | 场景／前置条件 | 应验证的结果或受保护行为 | 来源／决定依据 | 版本验证性质 |
|---|---|---|---|---|
| QI-001 | <场景> | <可观察预期> | <用户确认或既有规则> | <自动化候选 / 人工 / 待评估> |

- 需覆盖的边界与失败路径：<已知边界及对应 Intent ID；没有则说明>
- 跨功能交互与兼容性：<关联 Task、共享状态、存档或公开接口及对应 Intent ID>
- 用户特别关注项：<引用 Intent ID，不再复制另一套断言>
- 人工体验／视觉判断：<引用 Intent ID，并说明不能仅由数值或比图代替的部分>
- 待确认的回归候选：<技术建议、依据及待决定内容；未确认前不视为新增验收门槛>
- 已知历史问题：<症状、来源证据及其与本次需求的关系；未知不得写成“历史遗留”>

不得凭空增加阈值、设备范围或产品规则。对验收含义有歧义时，由 PM 询问用户；Builder 不得根据“当前代码恰好如此”反推正确预期。

## 3. 技术设计与接口（按需）

技术调查是否需要：<需要及原因 / 不需要及理由>。简单任务可引用已确认接口，不强制为每个 Task 再启动一次 Designer。

### 3.1 当前实现与推荐方案

- 已调查的入口、模块、场景与资源：<路径、符号和必要的固定提交>
- 当前状态／数据流及确认缺口：<区分代码事实、运行观察和未验证推断>
- 推荐实现：<最小可行方案及选择理由>
- 可逆的内部技术取舍：<仅记录影响实现的重要选择>
- 数据／存档／公开接口兼容性：<迁移、版本兼容和回退要求>
- 仍待用户决定的产品变化：<无；或引用第 6 节问题>

### 3.2 功能接口与归属

| 接口／事件／绑定键 | 提供方 → 使用方 | 参数／前置状态 | 返回／可观察变化／失败行为 | 读写性质与授权 | 实现或文档引用 |
|---|---|---|---|---|---|
| <接口> | | | | <查询 / 会修改状态> | |

查询接口与修改接口分开。领取奖励、写存档、改数据库等操作不得作为无副作用查询直接执行；缺少接口由 Builder 在确认范围内实现。

### 3.3 实现需要保留的可测试性

- 稳定的 UI／节点／事件标识：<标识及必要性>
- 页面状态、测试数据或存档的构造入口：<接口与限制>
- 随机种子／时钟控制：<需要的最小范围或不适用原因>
- 可观察输出：<供版本断言读取的字段、事件或日志>
- 测试入口在生产中的限制：<禁用、隔离或权限要求>
- 已有测试／Runner 的复用线索：<路径；尚未调查则注明>

这些是实现接口要求，不是本 Task 提前编写或运行完整测试套件的任务。

## 4. 功能隔离与并行协作

### 4.1 工作区身份（只读索引）

- Worktree 根目录：<从机器块读取>
- 分支：<从机器块读取>
- 基线提交：<从机器块读取真实完整 commit hash>
- 本 Task 文件：<路径>
- 本地 Git 操作授权：<未授权 / 已授权的提交、合并等操作与范围>

### 4.2 文件和功能归属

| 负责方 | 交付内容 | 允许写入的路径 | 禁止写入／交接边界 |
|---|---|---|---|
| PM | Task、决定与交接记录 | <文档路径> | <其他 Task 的记录> |
| Designer | 技术方案；交付阶段的 QA 计划 | <返回 PM 归档；工具写入另按授权> | 业务代码、可执行测试脚本、未经授权的状态修改 |
| Design-Art | 预览、概念；批准后的正式资产 | <本 Task 预览／概念目录、明确归属的正式资源路径> | Task、业务代码、最终场景绑定、其他工作区 |
| Code Builder | 代码、最终绑定；交付阶段的 QA 脚本 | <功能路径；Release 测试目录在版本计划中指定> | 未授权资源、其他工作区、擅自修改验收标准 |

同 Task 的并行写入路径不重叠；正式资产交接后，最终场景、导入元数据和资源绑定由 Code Builder 顺序完成。不允许同时操作共享 Git 索引。

### 4.3 依赖与共享触点

| 依赖 Task／模块 | 固定修订／提交 | 所需接口或产物 | 集成顺序／是否共同交付 | 协调者 |
|---|---|---|---|---|
| <无或具体依赖> | | | | |

| 共享文件／页面区域／主题 | 本 Task 的改动与占用范围 | 其他 Task／负责人 | 已固定的共同基准与协调方案 |
|---|---|---|---|
| <含锚点、层级、输入优先级或公共样式> | | | |

不能依赖其他活动工作区的未提交产物或漂移的“最新分支”。文件无冲突不等于页面不冲突；共享布局、主题和接口先协调，再并行生产。

### 4.4 运行数据隔离

| 对象 | 独立位置／命名空间 | 真正采用它的启动参数或适配入口 | 当前情况 |
|---|---|---|---|
| 存档／用户数据 | | | <未配置 / 已适配 / 不使用> |
| 缓存／临时文件／日志 | | | |
| 构建／导出产物 | | | |
| 数据库／账号／端口／外部服务 | | | |

仅创建目录不代表引擎已经使用该目录。未完成适配前可并行编辑，不能并行运行会写同一共享状态的实例；本文件不保存密钥或真实用户凭据。

## 5. 实机预览、资产概念与生产合同（有视觉改动时）

适用范围：<无视觉变化则写“不适用＋原因”；仅资产变化可只保留相关概念与生产合同>。

### 5.1 实机底图与编辑边界

- 底图 artifact_id／固定路径／实际 SHA-256：<引用真实存在的文件；摘要由工具取得>
- 来源：<实机截图 / 当前构建截图 / 录屏帧 / 用户明确批准的替代材料>
- 对应提交或构建、页面／游戏状态：<已知事实；未知项明确标出>
- 原始像素、视口、设备／安全区：<实际资料，不从评审拼图猜测>
- 已进行的裁剪、缩放或合成：<无或具体说明>
- 允许修改的区域与元素：<明确边界>
- 必须保留的区域与元素：<背景、Logo、镜头、文字、既有按钮等>
- 缺失材料／替代方案限制及批准依据：<无或引用第 6 节>
- 是否需要重新取图：<原因、最小范围及授权；优先复用可靠原图>

缺少可用底图先交 PM 索取或确认替代材料，不自动从空白重画。默认保留原图、局部制作、分层合成。

### 5.2 高精度预览交付

| artifact_id | 页面／状态与目标像素 | 引用底图 | 修改点与布局规则 | 固定文件／SHA-256 | 可编辑源／合成配方 |
|---|---|---|---|---|---|
| <PREVIEW-001> | | | <锚点、安全区、动态文字规则> | | |

主交付是可独立查看的原尺寸高精度画面；评审拼图仅为辅助。模拟尺寸标为“设计合成”，不能作为该设备实际适配通过的证据。未变化内容不重复生成。

### 5.3 资产概念与生产规格

| 概念 artifact_id | 对应资产／家族 | 需要确认的造型、配色、材质与状态 | 固定文件／SHA-256 | 尚未决定的内容 |
|---|---|---|---|---|
| <CONCEPT-001> | <ART-001> | | | <无或引用问题> |

每个新建／修改资产，或明确共用规格的资产家族，复制填写以下规格块：

#### <ART-001：资产名称／家族>

- 用途与生产方式：<复用现有 / 原生 UI / 修改资产 / 新建资产；说明运行用途>
- 关联概念与样式依据：<固定 artifact_id；复用或原生 UI 可写不需要概念及原因>
- 尺寸、宽高比与缩放规则：<明确数值／范围和单位>
- 格式、透明度与导出要求：<PNG / SVG / 序列帧等；是否 Alpha>
- 视角、构图、风格与必须保留项：<足以指导生产的要求>
- 状态与动画：<状态名称、帧数／时序；不需要则说明>
- 锚点、留白、切片／裁切边界：<所需规格或不适用>
- 文字、动态数值与本地化：<由控件渲染；烘焙文字必须有确认依据>
- 正式资源路径、源文件与导出方式：<项目资源目录；独立于整屏预览>
- 消费接口／绑定槽位、回退方案与集成者：<明确到 Code Builder 可接入>
- 来源／使用限制：<原创、已有资源或授权材料的可定位依据>
- 禁止项：<不允许的改动、风格或交付方式>

概念须足以确定生产方向，不能以未定占位图代替。正式资产在构建阶段独立生产，不默认从整屏预览裁图交付。

### 5.4 生成预算与交接

- 本次必需产物／主方向：<只列必要预览、概念与正式资产>
- 生成次数、额外变体、重试与并发上限：<有限预算及批准依据；未确定不得无限重试>
- 局部迭代与复用策略：<反馈改哪里；哪些已有产物不重新生成>
- 正式资产 Manifest：<生产后由工具生成的清单路径；无正式资产则不适用>

工具生成清单中的路径、摘要、格式和可识别尺寸；Art 提供用途、状态、锚点等语义，并声明删除／替换关系。清单不证明美术合格或绑定通过。

## 6. 澄清、批准与变更

### 6.1 未决问题与用户决定

| 问题／冲突 | 受影响范围与可选方案 | PM 向用户反馈的引用 | 用户决定与依据 | 对合同／已有产物的影响 |
|---|---|---|---|---|
| <无或具体问题> | | | <待决定不能写成默认方案> | |

任何真实业务歧义：子代理立即停止并反馈 PM，不直接询问用户、不自行派生代理；PM 暂停整个相关 Task，向用户反馈并等待选择。无依赖的其他 Task 可以继续。

### 6.2 当前批准依据（机器 approvals 的可读索引）

| 批准范围 | 对应 task_revision | 具体产物 artifact_id／固定 SHA-256 或合同修订 | 用户决定引用 | 当前结论 |
|---|---:|---|---|---|
| 页面／场景预览 | | | | <待确认 / 已批准 / 不需要及原因> |
| 必需资产概念 | | <逐项列明，不以一个“通过”代替不同概念> | | |
| 构建授权 | | <明确授权的工作范围与合同修订> | | <待确认 / 已授权> |

同一条明确用户回复可以覆盖多项批准；批准布局不自动批准全部资产或开工。实际批准只通过 `harness.py approve` 写入机器块，本表不单独授予权限。哈希只用于固定文件内容，不代替用户同意。

### 6.3 暂停与恢复（发生时填写）

- 暂停原因与原阶段：<引用问题，不再维护第二个主 Status>
- 受影响的在途 run_id／外部作业：<调度记录引用；没有工具时记录实际作业身份>
- 停止请求与实际停止情况：<已确认停止 / 仍在运行 / 无法确认；附依据>
- 已写入文件与晚到产物处理：<保留、撤销或隔离；不能自动当作当前交付>
- 用户决定与明确恢复授权：<来源及范围>
- 重新派发前处置：<旧写入者已停止或可靠隔离；新运行使用新 run_id>

撤销结果接收资格、停止进程、撤销已写文件是不同操作；没有实际证据不能声称均已完成。

## 7. 实现交付与 Release Handoff

由 PM 根据 Builder／Design-Art 返回的事实更新，不把未执行的 QA 写成通过。

### 7.1 已交付内容

- 实现摘要与对应需求／Intent ID：<完成了什么，不复制全部需求历史>
- 主要修改文件与功能入口：<路径／符号>
- 正式资源、源文件、Manifest：<固定引用；无则说明>
- 实际采用的预览／概念：<引用第 6 节已批准产物；存在偏离则明确列出>
- 场景导入、绑定与集成情况：<完成项和未完成项，不以“有文件”代替已接入>
- 真实实现提交与工作区情况：<完整提交／尚未授权提交；未提交修改如实记录>
- 依赖／共享修改交接：<固定产物、集成顺序和协调结论>
- 派发／接收记录：<工具记录引用或实际交接依据，不手工复制一套运行状态>

### 7.2 交付阶段 QA 所需的实现事实

| Intent ID | 实际接口／稳定标识／状态构造入口 | 可观察字段与相关路径 | 复用线索／限制／缺失项 |
|---|---|---|---|
| QI-001 | | | |

Builder 只补事实和实现限制，不据此改变验收含义。仅补路径无需重新设计全部 QA；预期、边界或覆盖含义变化交 PM 协调 Designer，涉及产品取舍由用户决定。

### 7.3 已知问题与未验证内容

| 问题／未验证项 | 已知证据或缺失证据 | 影响与归因 | 后续处理／是否阻断 |
|---|---|---|---|
| <具体内容> | | <本次引入 / 已有证据的历史问题 / 未确定> | |

默认不运行固定冒烟、完整回归、重复基线或多分辨率 QA。确有实现阻断、明确请求或必要风险诊断时，只记录实际进行的最小检查：

| 原因与授权 | 实际命令／范围 | 结果与证据 | 仍未覆盖的内容 |
|---|---|---|---|
| <未执行则写“不适用：交付阶段集中 QA”> | | | |

### 7.4 实现就绪交接

进入 `READY_FOR_RELEASE` 前，只确认合同与实现交接，不新增游戏测试门槛：

- [ ] 必需实现及绑定已交接，范围偏离已解决；未把未完成项标为完成。
- [ ] 预览、概念与构建授权覆盖实际采用的内容；没有未解决歧义或未经处置的旧写入者。
- [ ] QA Intent 有来源，实际入口、已知错误与未验证项已经披露。
- [ ] 实现快照、依赖、共享触点与必要资产信息明确，可供版本集成固定输入。

PM 交接依据／日期：<引用证据；Status 由工具更新机器块，第 0 节仅同步展示，不另存“已验证”结论>。

## 8. 版本 QA 关联（纳入版本后填写）

| 版本产物 | 责任归属 | 固定引用 |
|---|---|---|
| 纳入的 Task 修订与最终输入提交 | PM 在版本记录中固定 | <Release ID／记录路径> |
| QA_BACKLOG | 工具汇总本 Task 第 2 节，PM 核对来源 | <带修订和 Intent ID 的快照> |
| QA_PLAN | Designer：用例、断言、覆盖映射、接口／夹具需求 | <计划修订／固定引用> |
| 测试脚本与夹具实现 | Code Builder：依据计划编写、复用或适配 | <脚本／夹具及固定版本> |
| 执行结果与证据 | 普通 Runner；PM 总结，Reporter 仅按需辅助 | <候选版本、运行结果与报告引用> |

QA 计划不在本 Task 再复制一份。Builder 不擅自放宽断言、删减覆盖或修改标准；计划问题交 Designer，产品歧义经 PM 向用户确认。脚本编写不等于已获执行授权。

公共 setup 和已有测试可以复用，但被修改的运行状态须重置或隔离。零用例、遗漏必需项、跳过项或旧候选结果都不能冒充全部通过；版本结论以版本记录为准。

## 9. 合同修订说明（机器 history 的可读索引）

| task_revision | 日期 | 有效内容变化与原因 | 用户／PM 决定依据 | 受影响产物、批准和在途工作 |
|---:|---|---|---|---|
| 1 | <YYYY-MM-DD> | 初始草案 | | 无 |

需求范围、验收含义、接口合同、资产规格或写入边界改变时，通过 `task contract` 更新机器合同与 task_revision；本表只解释原因和影响，不手工推进修订。仅追加日志或非语义实现事实不制造新需求版本。

重要限制和决定必须进入对应合同区，不能只藏在随手备注、聊天历史或交接日志中。
