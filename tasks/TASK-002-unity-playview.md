# Unity 可玩场景与玩家界面

> 使用 `workspace task` 创建，不要直接复制未绑定模板派发。下方唯一 `harness-state` 是状态、合同、批准和运行记录的机器权威源；正文是 PM 的可读工作表与索引，不能覆盖机器块。
> 本文由 PM 维护，子代理返回结果后由 PM 通过统一入口接收。删除不适用的正文区块时保留原因；未知信息写“待确认”，不能把空白或默认值当作用户决定。
> Task 阶段只维护 QA Intent；版本交付时由 **Designer 制定 QA 计划、用例、断言与接口需求，Builder 编写或复用脚本，普通 Runner 执行**。

```harness-state
{
  "schema_version": 1,
  "kind": "task",
  "id": "TASK-002",
  "workspace": "C:\\Users\\charlielu\\Documents\\ChatGPT\\下锅喽-worktrees\\TASK-002-unity-playview",
  "branch": "task/TASK-002-unity-playview",
  "base_commit": "fa5e3d2797f3f29b725727d84e2b1a63af1b91c0",
  "runs": {
    "run-3dd83e5826fe401a": {
      "run_id": "run-3dd83e5826fe401a",
      "step": "design",
      "role": "design_art_agent",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:10:04.806286+00:00",
      "inputs": {
        "task_revision": 2,
        "contract_digest": "cc6966e7422562140d8457773e003909f54cae43ba550c1778701b9523f96eb9"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "medium",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [
        ".harness/previews/TASK-002",
        ".harness/concepts/TASK-002",
        ".harness/references/TASK-002"
      ],
      "stop_confirmed": true,
      "approved_inputs": {},
      "handoff_digest": "aa6ac4233350eebbe6d4a3c66f29bb21d45e2d80bc371c757898dddec321364d",
      "accepted_at": "2026-09-20T09:14:58.324706+00:00",
      "payload": {
        "visual_mapping": {
          "asset_concepts": [],
          "elements": [
            "reuse: 原型独立 food_00..food_15.png / plate.png，中心 pivot，保留暖色食材插画。",
            "native UI: 四容量两启用订单、五格固定暂存、暂停与流程按钮，所有动态文字使用独立控件。",
            "main preview: .harness/previews/TASK-002/v001/gameplay.png，原尺寸 780x1688，Design Composite，去除骨架 A、字母调试标签与种子。",
            "flow preview: .harness/previews/TASK-002/v001/flow-states.png，780x1688 状态板，入口/暂停/结果/异常，分区注释不进入玩家 UI。",
            "source and implementation mapping: .harness/previews/TASK-002/v001/README.md；所有样例数值为排版示意，实际读取会话与核心。",
            "new asset: none；尚未获预览内容批准，不声称真机适配或游戏 QA 通过。"
          ]
        }
      },
      "summary": "基于用户批准的 v0.1 替代截图与现有独立 PNG，完成 780x1688 主玩法及流程状态 Design Composite；附可复现分层脚本与视觉映射。外部生成 0 次，无正式资源写入。",
      "artifacts": [
        {
          "path": ".harness/previews/TASK-002/v001/gameplay.png",
          "sha256": "ecd00b3de7d8c3ea8cfb4346d1332a108c19cbe88c3653a17fa00fe610c6890e",
          "bytes": 618896,
          "artifact_id": "artifact-4872f5782ec442d6",
          "kind": "preview",
          "run_id": "run-3dd83e5826fe401a",
          "task_revision": 2
        },
        {
          "path": ".harness/previews/TASK-002/v001/flow-states.png",
          "sha256": "bf91b16115b0cc715001c5054016da5858d4304946c0e8a3e3d24010c83d8f3e",
          "bytes": 334786,
          "artifact_id": "artifact-13f184ace2704b4c",
          "kind": "preview",
          "run_id": "run-3dd83e5826fe401a",
          "task_revision": 2
        },
        {
          "path": ".harness/previews/TASK-002/v001/compose.py",
          "sha256": "6f31142a826194fac98c283ca67d08bde4d9862b794e9dada94ce882bed6ec8f",
          "bytes": 7311,
          "artifact_id": "artifact-cdeb6b38b4bb45b7",
          "kind": "composition_recipe",
          "run_id": "run-3dd83e5826fe401a",
          "task_revision": 2
        },
        {
          "path": ".harness/previews/TASK-002/v001/README.md",
          "sha256": "e5cc79e02a4708e8daf0019b32e3d4deb79ca7dca63ad70a4daece0cb13d0a4f",
          "bytes": 4102,
          "artifact_id": "artifact-efc8599761aa41e5",
          "kind": "composition_recipe",
          "run_id": "run-3dd83e5826fe401a",
          "task_revision": 2
        },
        {
          "path": ".harness/previews/TASK-002/v001/content-manifest.json",
          "sha256": "c7e6a3209ad4a38359470be81c793dd50393ada5270b10deef057ec2d5ed1711",
          "bytes": 6097,
          "artifact_id": "artifact-946177a198b8478d",
          "kind": "file",
          "run_id": "run-3dd83e5826fe401a",
          "task_revision": 2
        }
      ]
    },
    "run-826b45a1055a4c22": {
      "run_id": "run-826b45a1055a4c22",
      "step": "design",
      "role": "design_art_agent",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:17:26.641347+00:00",
      "inputs": {
        "task_revision": 4,
        "contract_digest": "58e814fdc327b125c5984216de55f555124f046bf0c98d4448c4865860d13b9a"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "medium",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [
        ".harness/previews/TASK-002",
        ".harness/concepts/TASK-002",
        ".harness/references/TASK-002"
      ],
      "stop_confirmed": true,
      "approved_inputs": {},
      "handoff_digest": "5869200d904b9c0bff19e87de7cb6ae0c6241a553c00787eeee6ac0a26c1557f",
      "accepted_at": "2026-09-20T09:20:27.345763+00:00",
      "payload": {
        "visual_mapping": {
          "asset_concepts": [],
          "elements": [
            "reuse: gameplay.png 原样复用 v001；食材/盘子复用原型独立 PNG。",
            "native UI: 入口顶部状态、中央主视觉、两侧信息模块、底部主按钮；暖色奶油/浅绿/珊瑚风格不变。",
            "side regions: PM 已明确仅显示今日规则和玩法提示，非交互、不注册按钮、不发 SessionAction。",
            "layout reference only: ui-style-reference-2026-09.jpg 的色彩/描边/字体/角色/品牌/文案/活动均未采用，像素未用于合成。",
            "dynamic text: 日期、计数和结果读权威事实，独立原生控件。",
            "flow board: 入口主稿 + 暂停/结果/异常辅助分区；未声称完整设备截图或真机验证。",
            "production assets: none；规格/复现/保留区域见 v002/README.md，完整统计语义沿用 v001。"
          ]
        }
      },
      "summary": "完成仅参考纵向布局的 v002 设计合成；入口具有顶部状态、中央食材主视觉、两侧非交互信息区、底部唯一开始操作。保留 v001 暖色 UI 与玩法主图，未使用参考图的美术效果或活动功能。",
      "artifacts": [
        {
          "path": ".harness/previews/TASK-002/v002/gameplay.png",
          "sha256": "ecd00b3de7d8c3ea8cfb4346d1332a108c19cbe88c3653a17fa00fe610c6890e",
          "bytes": 618896,
          "artifact_id": "artifact-dcc42d1b17dc4082",
          "kind": "preview",
          "run_id": "run-826b45a1055a4c22",
          "task_revision": 4
        },
        {
          "path": ".harness/previews/TASK-002/v002/flow-states.png",
          "sha256": "dc5ef69a6b56c47781cc45efad7ce33432da59470fdcf5ca331ef4d6bd91269f",
          "bytes": 332217,
          "artifact_id": "artifact-7d10a9a423674497",
          "kind": "preview",
          "run_id": "run-826b45a1055a4c22",
          "task_revision": 4
        },
        {
          "path": ".harness/previews/TASK-002/v002/compose.py",
          "sha256": "546b1c2f9b9db052cfd12d753e919ec2e1dd912b269c7427b95121c3eeac1aca",
          "bytes": 3559,
          "artifact_id": "artifact-5b1bb502aa4a41ba",
          "kind": "composition_recipe",
          "run_id": "run-826b45a1055a4c22",
          "task_revision": 4
        },
        {
          "path": ".harness/previews/TASK-002/v002/README.md",
          "sha256": "7a2de85d06cdde9aa3278efc17b494ac5e891f0248ad4d2cd0d471c250324083",
          "bytes": 3083,
          "artifact_id": "artifact-73c4c483038448f6",
          "kind": "composition_recipe",
          "run_id": "run-826b45a1055a4c22",
          "task_revision": 4
        },
        {
          "path": ".harness/previews/TASK-002/v002/content-manifest.json",
          "sha256": "3e9943b6ee4ea55ee54277979b338182ce432309a6bc492743878d04c38ea22f",
          "bytes": 6762,
          "artifact_id": "artifact-8cc6503e46b242b3",
          "kind": "file",
          "run_id": "run-826b45a1055a4c22",
          "task_revision": 4
        }
      ]
    },
    "run-fab0d4d476d1443d": {
      "run_id": "run-fab0d4d476d1443d",
      "step": "design",
      "role": "design_art_agent",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:22:35.467953+00:00",
      "inputs": {
        "task_revision": 5,
        "contract_digest": "5216bec43e5484327d84bd2bbfafec15d0d1440bd53634f27c30e029d43bc705"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "medium",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [
        ".harness/previews/TASK-002",
        ".harness/concepts/TASK-002",
        ".harness/references/TASK-002"
      ],
      "stop_confirmed": true,
      "approved_inputs": {},
      "handoff_digest": "e14a1ecd051ca45a4bd52d3aeabd7f9cc03b062920df976eeafadcd095dcb30f",
      "accepted_at": "2026-09-20T09:26:28.319575+00:00",
      "payload": {
        "visual_mapping": {
          "asset_concepts": [],
          "elements": [
            "review format: 深色画板、三列尺寸标题、顶部版本/待评审、底部实现说明、窄屏信息区局部放大。",
            "regular 390x844: 顶部状态、中央食材主视觉、左右非交互规则信息、底部唯一开始按钮。",
            "narrow 320x693: 信息区移到中央主视觉下方形成左右双列，不新增交互。",
            "wide 700x1000: 保留两侧信息，中央食材放大，按钮保持最大宽度。",
            "safeArea: 全部设计示意，不是实测；真实适配需读取平台 safeArea。",
            "reuse/native UI: 原始 food/plate PNG + 奶油/浅绿/珊瑚控件，动态文字独立渲染；无新正式资产。",
            "reference limitation: 不采用参考图中的群岛项目、美术、Logo、按钮文案或实测结论；外部生成 0 次。"
          ]
        }
      },
      "summary": "完成 2186x1645 深色入口布局评审板，并附三种原尺寸入口 Design Composite、可复现布局脚本、README 和程序摘要。仅参考画板格式；保留暖色食材美术，窄屏重排信息区，未冒充真机。",
      "artifacts": [
        {
          "path": ".harness/previews/TASK-002/v003/entry-layout-review-board.png",
          "sha256": "16572f9fea49de14f0229151c3a8803299df2ed5bb2903d2d7703065d2f0aa06",
          "bytes": 870032,
          "artifact_id": "artifact-c2c286948af04237",
          "kind": "preview",
          "run_id": "run-fab0d4d476d1443d",
          "task_revision": 5
        },
        {
          "path": ".harness/previews/TASK-002/v003/entry-390x844.png",
          "sha256": "04c1084b753df0680904ec6ca6fd1f94b62768562e356c7a07eb363135625b7f",
          "bytes": 103093,
          "artifact_id": "artifact-34696f5010f64152",
          "kind": "preview",
          "run_id": "run-fab0d4d476d1443d",
          "task_revision": 5
        },
        {
          "path": ".harness/previews/TASK-002/v003/entry-320x693.png",
          "sha256": "361b4fb932975c19234f7be720dd34969c5622bbb21651503214121dd8b1a0d1",
          "bytes": 96200,
          "artifact_id": "artifact-5aa8d7fcc3f54d1d",
          "kind": "preview",
          "run_id": "run-fab0d4d476d1443d",
          "task_revision": 5
        },
        {
          "path": ".harness/previews/TASK-002/v003/entry-700x1000.png",
          "sha256": "a15912b914fe27ec196c77022a1660ea706abebb7de2083da547993b2b4fab39",
          "bytes": 161243,
          "artifact_id": "artifact-77dcf46ec9a24dd8",
          "kind": "preview",
          "run_id": "run-fab0d4d476d1443d",
          "task_revision": 5
        },
        {
          "path": ".harness/previews/TASK-002/v003/compose.py",
          "sha256": "baf627a47802e998684cacff37e81724323aff8bc5fe59d10ff27b02171e625b",
          "bytes": 6795,
          "artifact_id": "artifact-3a5b144ebba642dd",
          "kind": "composition_recipe",
          "run_id": "run-fab0d4d476d1443d",
          "task_revision": 5
        },
        {
          "path": ".harness/previews/TASK-002/v003/layout-spec.json",
          "sha256": "d5946bda5fcd67d392549b77dbd20a3c52d706d013cec4c6da45ffbb0411d78f",
          "bytes": 761,
          "artifact_id": "artifact-e2a74fbb3cae4117",
          "kind": "composition_recipe",
          "run_id": "run-fab0d4d476d1443d",
          "task_revision": 5
        },
        {
          "path": ".harness/previews/TASK-002/v003/README.md",
          "sha256": "c0f7ecd170f42c3a2811cf30b11d67e6e746781a20e0d459fd606af0c5380f7d",
          "bytes": 3032,
          "artifact_id": "artifact-ca30332c618c45d8",
          "kind": "composition_recipe",
          "run_id": "run-fab0d4d476d1443d",
          "task_revision": 5
        },
        {
          "path": ".harness/previews/TASK-002/v003/content-manifest.json",
          "sha256": "73391e0461e3239b205153ba1b6155711b4e9181e1623b3fe98e663152e0436c",
          "bytes": 7201,
          "artifact_id": "artifact-127da47886524c03",
          "kind": "file",
          "run_id": "run-fab0d4d476d1443d",
          "task_revision": 5
        }
      ]
    },
    "run-9329c81d377641de": {
      "run_id": "run-9329c81d377641de",
      "step": "design",
      "role": "design_art_agent",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:27:57.981754+00:00",
      "inputs": {
        "task_revision": 6,
        "contract_digest": "6e911308ab8ab0592f5783c9c6aa282f952eaa7c29197fbf48fda6fefd10de2c"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "medium",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [
        ".harness/previews/TASK-002",
        ".harness/concepts/TASK-002",
        ".harness/references/TASK-002"
      ],
      "stop_confirmed": true,
      "approved_inputs": {},
      "handoff_digest": "e481e88ebec102b97d5b76bc3e74ad8ccf406af33f89e6e21e553ecbc23771be",
      "accepted_at": "2026-09-20T09:30:09.302527+00:00",
      "payload": {
        "visual_mapping": {
          "asset_concepts": [],
          "elements": [
            "device text whitelist: 每个视口仅一个主按钮文字“开始下火锅”，独立原生 Text 控件意图。",
            "removed: 设备内标题/日期/规则/提示/说明/信息区标签/Design Composite/安全区文字全部删除。",
            "preserved: 390x844、320x693、700x1000 三种构图、暖色背景、空卡装饰、原始盘子食材、按钮形状、无字安全区示意。",
            "noninteractive: 空卡只是装饰，不发 SessionAction，不新增功能。",
            "board: 外围项目标题、版本、尺寸、实现说明、边界与局部放大继续保留。",
            "constraints: Design Composite 非真机；示意留白不是实测；外部生成 0，无正式资产。"
          ]
        }
      },
      "summary": "完成 v004 局部文字反馈：三张设备图仅保留按钮文字“开始下火锅”，其余设备内文字全部移除；保留暖色形状、盘子食材、安全区示意与画板外注释。",
      "artifacts": [
        {
          "path": ".harness/previews/TASK-002/v004/entry-layout-review-board.png",
          "sha256": "a0bb66fa2dbc323a9a8a07b98e2ed254cf26af25b9c29b596fa1f19c91e903d8",
          "bytes": 579628,
          "artifact_id": "artifact-bbd9665b6b6f4b28",
          "kind": "preview",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        },
        {
          "path": ".harness/previews/TASK-002/v004/entry-390x844.png",
          "sha256": "90131307803cab120e017ff565960dc603347ee044f102b53084c39832a41081",
          "bytes": 51212,
          "artifact_id": "artifact-2e7c7042e86e4dfe",
          "kind": "preview",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        },
        {
          "path": ".harness/previews/TASK-002/v004/entry-320x693.png",
          "sha256": "cc9616106489280d7cf96621bb4783536dc08031803529ccc6af6537501267da",
          "bytes": 46162,
          "artifact_id": "artifact-38bf435358514102",
          "kind": "preview",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        },
        {
          "path": ".harness/previews/TASK-002/v004/entry-700x1000.png",
          "sha256": "80de7a030c515b50db7119d458d12e6b18a2d6dc8b33e687df0870def774f0f5",
          "bytes": 97102,
          "artifact_id": "artifact-915bdfba693d4e0f",
          "kind": "preview",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        },
        {
          "path": ".harness/previews/TASK-002/v004/compose.py",
          "sha256": "d8425470e0ada0eac68f2d278f937f566683af4e10dcfccd7243d65344eefe11",
          "bytes": 3173,
          "artifact_id": "artifact-485875327ca547fb",
          "kind": "composition_recipe",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        },
        {
          "path": ".harness/previews/TASK-002/v004/layout-spec.json",
          "sha256": "dc9c8f6751bfbb2fd0d346442973b10014a7b4c2ff175a626733cd8a89430a5c",
          "bytes": 1229,
          "artifact_id": "artifact-981744e9cbae442c",
          "kind": "composition_recipe",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        },
        {
          "path": ".harness/previews/TASK-002/v004/README.md",
          "sha256": "79d3b6fb9dda41df864e3c24d3f3d8a844cbea542d4f7d8e52e85ffaac9b0d81",
          "bytes": 2145,
          "artifact_id": "artifact-5adec3ac25c24df4",
          "kind": "composition_recipe",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        },
        {
          "path": ".harness/previews/TASK-002/v004/text-layer-audit.json",
          "sha256": "0f1de9e4b74aefc3165489ab57716cde945913a270dba04872df85765af9cc4e",
          "bytes": 510,
          "artifact_id": "artifact-6018b7a9f78a4bb9",
          "kind": "file",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        },
        {
          "path": ".harness/previews/TASK-002/v004/content-manifest.json",
          "sha256": "838cc6e75371ae52dcb9cc41f00fc31fcfce948f30ba9179c638f23ab81ead75",
          "bytes": 7725,
          "artifact_id": "artifact-0b17978697a44c92",
          "kind": "file",
          "run_id": "run-9329c81d377641de",
          "task_revision": 6
        }
      ]
    },
    "run-ac27d30bc8ff4e7b": {
      "run_id": "run-ac27d30bc8ff4e7b",
      "step": "design",
      "role": "design_art_agent",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:30:45.735448+00:00",
      "inputs": {
        "task_revision": 7,
        "contract_digest": "1d23184dc2a75ca6e8ebd03aad6ee38b6a4d69d11a67bde005123ee74e3360ae"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "medium",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [
        ".harness/previews/TASK-002",
        ".harness/concepts/TASK-002",
        ".harness/references/TASK-002"
      ],
      "stop_confirmed": true,
      "approved_inputs": {},
      "handoff_digest": "52db28ba4e595ae39b20e34a583e6ec52da251a7373e157fac93369cc043aae3",
      "accepted_at": "2026-09-20T09:32:43.203528+00:00",
      "payload": {
        "visual_mapping": {
          "asset_concepts": [],
          "elements": [
            "removed: 三视口各删除顶部空框、左信息卡、右信息卡；无新增交互。",
            "preserved: 奶油背景、原始盘子食材 PNG、珊瑚主按钮、唯一原生 Text 文案“开始下火锅”。",
            "review guides: 无字安全区虚线仅设计示意，真实 safeArea 未验证。",
            "board: 2186x1645，三张独立视口 390x844、320x693、700x1000；外围标题/边界/说明保留，局部放大展示按钮。",
            "constraints: 旧版只读，无 Unity/Task/正式资产修改；外部生成 0；Design Composite 非真机截图。"
          ]
        }
      },
      "summary": "完成 v005：三张设备图删除顶部空框及两侧/下方空信息卡，仅保留奶油背景、盘子食材、主按钮与“开始下火锅”，画板外围说明及示意安全线保留。",
      "artifacts": [
        {
          "path": ".harness/previews/TASK-002/v005/entry-layout-review-board.png",
          "sha256": "055885f7d15c37af969dd202eb85a8f17d5166a77781fe41298b39200158fc86",
          "bytes": 571907,
          "artifact_id": "artifact-393a627bb2524885",
          "kind": "preview",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        },
        {
          "path": ".harness/previews/TASK-002/v005/entry-390x844.png",
          "sha256": "05ebe4cb1bb38c96bee343085c437d8d51544c68c54df0d0acea86af425cbf4e",
          "bytes": 49710,
          "artifact_id": "artifact-92ab2b4b2367469a",
          "kind": "preview",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        },
        {
          "path": ".harness/previews/TASK-002/v005/entry-320x693.png",
          "sha256": "ddba2585f2057495fce01723051640c259630800d5bc8c674f3a73e4422152a8",
          "bytes": 44731,
          "artifact_id": "artifact-f4b2a6baaee14897",
          "kind": "preview",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        },
        {
          "path": ".harness/previews/TASK-002/v005/entry-700x1000.png",
          "sha256": "0ee2a689820636fe0bee578367d31b45bdd98a0f5349f6aacb71d558a088ff92",
          "bytes": 96124,
          "artifact_id": "artifact-8820cb08561b4238",
          "kind": "preview",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        },
        {
          "path": ".harness/previews/TASK-002/v005/compose.py",
          "sha256": "c54c4a2d2a706642fbd0ea1161e200c020b6f5d60dd831c0267a8885f0a43e49",
          "bytes": 3417,
          "artifact_id": "artifact-ea9e351c015e4df7",
          "kind": "composition_recipe",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        },
        {
          "path": ".harness/previews/TASK-002/v005/layout-spec.json",
          "sha256": "3711413eabd2e5b475193067624e2cce391778fb2cb0e3beebc42fd60c371844",
          "bytes": 1465,
          "artifact_id": "artifact-e3e53bbcb52c47ee",
          "kind": "composition_recipe",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        },
        {
          "path": ".harness/previews/TASK-002/v005/README.md",
          "sha256": "6e1cd3ef8fbf144bfff39d2d4deeb10b998fe6ba985b3fa9ba9a1615091a0e69",
          "bytes": 1926,
          "artifact_id": "artifact-c768bf3fb8264dd5",
          "kind": "composition_recipe",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        },
        {
          "path": ".harness/previews/TASK-002/v005/layer-audit.json",
          "sha256": "bd5b3bdf15777d255dbeb2e2f1843065c91d07c2e735c136b7f9df7ec50417a3",
          "bytes": 1665,
          "artifact_id": "artifact-8e5220458cb542a0",
          "kind": "file",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        },
        {
          "path": ".harness/previews/TASK-002/v005/content-manifest.json",
          "sha256": "62747a3bc6ae06bc37b42cafd9a2e34d7b7a1e8b002bfbf583f6dcb0e9a1fa5f",
          "bytes": 7721,
          "artifact_id": "artifact-1a52e56d61824c46",
          "kind": "file",
          "run_id": "run-ac27d30bc8ff4e7b",
          "task_revision": 7
        }
      ]
    },
    "run-ca718de95951474b": {
      "run_id": "run-ca718de95951474b",
      "step": "code",
      "role": "code_builder",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:35:08.185233+00:00",
      "inputs": {
        "task_revision": 7,
        "contract_digest": "1d23184dc2a75ca6e8ebd03aad6ee38b6a4d69d11a67bde005123ee74e3360ae"
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
        "Unity/Assets/HotpotSort/Runtime/Presentation",
        "Unity/Assets/HotpotSort/Runtime/UnityPhysics",
        "Unity/Assets/HotpotSort/UI",
        "Unity/Assets/HotpotSort/Scenes/Gameplay.unity",
        "Unity/Assets/HotpotSort/Scenes/Gameplay.unity.meta",
        "Unity/Assets/HotpotSort/Prefabs",
        "Unity/Assets/HotpotSort/Art/ImportMetadata",
        "Unity/Assets/HotpotSort/Resources/Hotpot"
      ],
      "stop_confirmed": true,
      "approved_inputs": {
        "preview": {
          "artifacts": [
            "artifact-393a627bb2524885",
            "artifact-92ab2b4b2367469a",
            "artifact-f4b2a6baaee14897",
            "artifact-8820cb08561b4238"
          ],
          "task_revision": 7,
          "decision": "用户 2026-09-20 回复实现：批准 TASK-002 revision 7 的 v005 入口布局评审板、三视口预览和复现配方，并授权按该基准开始 Unity 实现。"
        },
        "build": {
          "task_revision": 7,
          "contract_digest": "1d23184dc2a75ca6e8ebd03aad6ee38b6a4d69d11a67bde005123ee74e3360ae",
          "decision": "用户 2026-09-20 回复实现：批准 TASK-002 revision 7 的 v005 入口布局评审板、三视口预览和复现配方，并授权按该基准开始 Unity 实现。",
          "artifacts": []
        }
      },
      "handoff_digest": "6b718d24255ca9d243540ef6ed29e32df1cf6ea19ddda09f0a8f68ae9a18f5c7",
      "accepted_at": "2026-09-20T09:49:41.635554+00:00",
      "payload": {
        "implementation_facts": {
          "entrypoints": [
            "Unity/Assets/HotpotSort/Scenes/Gameplay.unity -> GameplayView.Awake builds PlayerCanvas/SafeContent; unbound entry contains only hero and 开始下火锅.",
            "GameplayView.Bind(IPresentationPort), Apply(ViewUpdate), SetViewport(Rect), SetForeground(bool), Hide(bool), ResetView(), ShowError(string), DestroyView().",
            "IPresentationPort.Read/Updated/SessionAction/Tap/ObserveSupply are view-local adapter boundaries, not shared K0 contract definitions.",
            "GameplayView.SubmitScreenTap maps actual safe-area screen coordinates to 420x900 board, rejects UI/outside/rim/occluded targets, de-duplicates pending stable item IDs. AcknowledgeTap handles no-update rejections.",
            "GameplayView.Apply converges fixed five-slot buffer and four order positions (two enabled capacity slots), monotonic event sequence/transaction observation, terminal phase and supplied result facts.",
            "PlatePresentationWorld.Reconcile creates Dynamic Rigidbody2D only for committed snapshot plates, with per-body downward force, side/floor colliders and plate contact material. Position/Hit/SpaceAvailable and GameplayView.LateUpdate use current physical positions with stable item IDs.",
            "ViewPlate.motion optional presentation hints select supply spawn, direct restore, and correctionRevision. Changed snapshot x/y or correctionRevision resets position/velocity; unchanged snapshot coordinates preserve ongoing physical motion. Session change resets bodies.",
            "Removed plate/item disables colliders immediately; OccupiedPlateCount excludes removed plates while Remnants supplies collider-free fade/shrink art. SetSimulating freezes physics outside Running/foreground/visible state.",
            "GameplayView.ObserveSupply returns geometry observation only; core owns queue/cooldown/IDs/commits. No autonomous supply mutation.",
            "DevelopmentPresentationPort.Publish explicitly injects snapshots only in editor/development builds. ActionObserved/TapObserved/SupplyObserved expose commands; fixture is not attached to production scene.",
            "UI/INTEGRATION.md documents modules, K0 mapping, font, safeArea, acknowledgement and observation integration requirements.",
            "Resources/Hotpot contains byte-identical original 16 food images, plate, game-data and metadata; game-data not consumed as Daily rules."
          ],
          "known_errors": [
            "Root Unity project baseline (Packages, ProjectSettings, Boot) and real K0/session/platform adapters are absent. Scope forbids creating them; scene is not yet an end-to-end playable daily session.",
            "Reference manifest lacks uGUI/Physics2D modules used by implementation. Baseline owner must supply com.unity.ugui, physics2d and required engine image/font modules before Unity compilation.",
            "Real supply commit/cooldown/queue sequence integration requires K0 adapter; Task-owned dynamic physical presentation is implemented but has not run in Unity.",
            "Production CJK font is not supplied; OS font fallback is development convenience and may not display Chinese in WeChat. Approved font must be bound to playerFont."
          ],
          "untested": [
            "Unity API compilation, scene import, native UI rendering and approved-preview visual fidelity.",
            "Actual K0 acceptance/rejection, stock conservation, chain/transaction timing, daily retry/date/session and supply queue behavior.",
            "Physics movement/settling, platform safeArea/lifecycle and touch on real low-end/mainstream WeChat devices.",
            "Human timing targets and formal version QA; no full QA, build, release or true-device run performed."
          ],
          "validation": [
            "dotnet build Presentation/Diagnostics~/PortCompile.csproj compiled PresentationPort.cs using .NET 10/C# 9: 0 warnings, 0 errors. This is not Unity compilation.",
            "36 copied resource and metadata files compared by SHA-256 with the original reference: 0 mismatches; exactly 16 food PNG files.",
            "Static metadata GUID shape and git diff whitespace checks executed; no formal release assertions were created or run."
          ],
          "nodes": [
            "GameplayView",
            "PlayerCanvas",
            "CreamBackground",
            "SafeContent",
            "EntryHeroPlate",
            "Action_开始下火锅",
            "GameplayBoard",
            "Order_0..3",
            "Buffer_0..4",
            "Plate_<plateId>",
            "Item_<itemId>",
            "FlowOverlay"
          ],
          "scope": "Only authorized Unity owned paths plus the PM-authorized handoff path; no commits, Task state changes, Packages or ProjectSettings edits."
        }
      },
      "summary": "Delivered approved v005 entry presentation, native runtime UI/Gameplay scene, view-local port and development fixture, authoritative snapshot reconciliation, committed-plate dynamic Physics2D entry/falling/settling, hit/space observation, collider-free clearing animation and original resources. Full playable integration remains dependent on missing project/K0/platform baseline; not verified.",
      "artifacts": [
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_00.png",
          "sha256": "0beb6a4df89b27cf3abec4bbf636ff03538ee5f81a9245daa2a7edf9e36075bc",
          "bytes": 7183,
          "artifact_id": "artifact-97c0973f157f4916",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_00.png.meta",
          "sha256": "1539d845ce4519963ba87d2119f8049c128c6912fee0a348aa13f4e7f4b1ecd9",
          "bytes": 548,
          "artifact_id": "artifact-bf3b4c551a6848fe",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_01.png",
          "sha256": "6fbb315bdfbf1db1a1fe2fdb83c68831ed958461e12cc879260b7ba861e08e79",
          "bytes": 8493,
          "artifact_id": "artifact-3399958cc6984a44",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_01.png.meta",
          "sha256": "c4274c79ac8616070911f843f40c3c0ecdd85a1d13d0b8660fc944ffadc8c8a5",
          "bytes": 548,
          "artifact_id": "artifact-81da51b05c0e43ac",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_02.png",
          "sha256": "c386eae16fa591c9af607854a317050434bf25d4c2c33fcca6b0eb25391f0e4e",
          "bytes": 6291,
          "artifact_id": "artifact-99248155b7c0486c",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_02.png.meta",
          "sha256": "d6aef9a9565caa3c779cd30cb1f6bf7cfede2794834b49d98174e44c194167fe",
          "bytes": 548,
          "artifact_id": "artifact-c40bd95d25d3479b",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_03.png",
          "sha256": "f387e5aebd3a3dc73a4af9bc8ed5139eaa43f1a59aff18992b975a6e36255d4a",
          "bytes": 7284,
          "artifact_id": "artifact-eef407ffcca4444e",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_03.png.meta",
          "sha256": "f6d406dc5758dc7207b31e2a4810e60831242d7f31c0beeac4899683e6eb8f97",
          "bytes": 548,
          "artifact_id": "artifact-57ad7565e34c4e9b",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_04.png",
          "sha256": "a72d49cbba152498fc10a211b34de47d559c4cd9e6ea9fabdcda822a797999cd",
          "bytes": 6873,
          "artifact_id": "artifact-c39595b6d279458b",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_04.png.meta",
          "sha256": "d4796c1f792d33591b7999c5eca979cde49a6ce8a3d8c2936c03a49c02632918",
          "bytes": 548,
          "artifact_id": "artifact-a1f3b73d6dcf484b",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_05.png",
          "sha256": "594033016f51cdb74b9f1968136edab924b00cb9264285e70b122c5ca312f2ec",
          "bytes": 5399,
          "artifact_id": "artifact-214b403011d04787",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_05.png.meta",
          "sha256": "df77b0bbf9a2e229ef2220131b8b307cc6877a75f272b45858fa6cf4a7745d91",
          "bytes": 548,
          "artifact_id": "artifact-143ef193613443b3",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_06.png",
          "sha256": "3453d0b4d5523bf300cc536e45a4fe5b74a406dd0b0761a7530c8ae904e22526",
          "bytes": 8084,
          "artifact_id": "artifact-74725b664a514117",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_06.png.meta",
          "sha256": "ac323f72a89efd45c2804ed1ea8f0cfecd829d278077606e80d13fead2217f5f",
          "bytes": 548,
          "artifact_id": "artifact-cdbf335119d14e9e",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_07.png",
          "sha256": "975e2efc8431933d5ff306da4cb303624a749270f8f32e653de19dd752a08549",
          "bytes": 5902,
          "artifact_id": "artifact-4508ba9aab304919",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_07.png.meta",
          "sha256": "8ec4251b0875d552d1ad796535f0a3288ab9b5c3b67ecd80ee76ea047d5adc61",
          "bytes": 548,
          "artifact_id": "artifact-99f18069cf594af2",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_08.png",
          "sha256": "8cd7dc9cf9baa8ec3649d0780e6f1f6de3b3bc04dc326def3a2984fe9dc0522d",
          "bytes": 5682,
          "artifact_id": "artifact-a0f240c92e8146f2",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_08.png.meta",
          "sha256": "cd6ed9493f7f8bc35d8e326eedf324c849692ff105f0a3a790bbd41ee2545a17",
          "bytes": 548,
          "artifact_id": "artifact-f19be4e05fa844e5",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_09.png",
          "sha256": "30bac979677c27b7a4d56820188fd343317e242f78603e5d1e07faebe17b2c9c",
          "bytes": 4981,
          "artifact_id": "artifact-f83203b6838c41e8",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_09.png.meta",
          "sha256": "7dd879e4c506afef22c5c8eeb9f732c6a44188598345cbaeb6ce6cbc1e8862b2",
          "bytes": 548,
          "artifact_id": "artifact-91afddc82081453b",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_10.png",
          "sha256": "b93b65efe233391f271db4706eab52d0d0d4599b443cbca687a33813cccebe66",
          "bytes": 6482,
          "artifact_id": "artifact-db6100ac5f8546b5",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_10.png.meta",
          "sha256": "04a1723dedc5c43cfdd99939253f9e988ab82f16698f6be88ede5c73149d15ee",
          "bytes": 548,
          "artifact_id": "artifact-49f35758c87d48c4",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_11.png",
          "sha256": "45bc2df15f9b0713e66ec6337ae52f656323a1300233bae47ec1088ea83672c5",
          "bytes": 5287,
          "artifact_id": "artifact-a05f9bc58ab44ddb",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_11.png.meta",
          "sha256": "14c1f32202059c0c5b71c3853926728390a100056706194f5158b2c797955c88",
          "bytes": 548,
          "artifact_id": "artifact-798095aebc774411",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_12.png",
          "sha256": "5eb5d998e8f72ac723fe6f07c55cceca30018dc4d0efa13d5659c9361ade10ac",
          "bytes": 5671,
          "artifact_id": "artifact-96471dbc0fd14222",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_12.png.meta",
          "sha256": "e9771671da37c054c2237831d4c9eb26bed91dc9b6410064d6d0aa92145e3e97",
          "bytes": 548,
          "artifact_id": "artifact-ecf3c9f97d104c45",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_13.png",
          "sha256": "49bb9d34442c30a55bcd01e83e335f3a0050b38a03fb9d782f5a309259207d4f",
          "bytes": 5758,
          "artifact_id": "artifact-7f537c2c073c4ada",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_13.png.meta",
          "sha256": "aef9e082916959096b68de67dac57f501ddff10b209d7ab6ea05d8896ba33650",
          "bytes": 548,
          "artifact_id": "artifact-ddd07f86a77b4277",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_14.png",
          "sha256": "82ea9f941de9f42dc345b15f375bd1c7bed98a65af6254b8fd7bf48f5ddb45eb",
          "bytes": 9964,
          "artifact_id": "artifact-7011a7be3f5a429a",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_14.png.meta",
          "sha256": "3c501a64dccf0d7ea86e8f07b7401136330a889123b384d2882e6bb0d15b77b0",
          "bytes": 548,
          "artifact_id": "artifact-857d36e0bb034bcd",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_15.png",
          "sha256": "1e38a195ec019899fc29b19853bf01fa707b71258d16a5769ac498878b8b3f8c",
          "bytes": 5234,
          "artifact_id": "artifact-65b57a3947a04c47",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_15.png.meta",
          "sha256": "93841f00f4606fd8b6119c08c537bd3ad4b072bc4b618b369cbbbd6ee8bd718d",
          "bytes": 548,
          "artifact_id": "artifact-51993795b0794a3e",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/game-data.json",
          "sha256": "55930baff402fed70c618a52144a0d6d828858e7dfe7b78e204fbf5c5deb6982",
          "bytes": 15920,
          "artifact_id": "artifact-8e32f90b47d6487f",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/game-data.json.meta",
          "sha256": "d87b0b9ee38e31562fe049652fd0d0ae6cc2169f04af1fc0fa0efd795aa472bc",
          "bytes": 162,
          "artifact_id": "artifact-53dc83ddda4d49c5",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/plate.png",
          "sha256": "a4e6bb0f78d38585367eb551931fd0d10aa2b251d70d937e17d6b5e0d6201bdb",
          "bytes": 17377,
          "artifact_id": "artifact-47ad6a40175c408e",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Resources/Hotpot/plate.png.meta",
          "sha256": "0f62b8dab5f0dd820cc976003cc76b338ae702ce3fc3cfdcf8310d90016e79ae",
          "bytes": 548,
          "artifact_id": "artifact-6f042593a70349ae",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Presentation/DevelopmentPresentationPort.cs",
          "sha256": "e100f428b331c1a38c04ecafd91f1e31659f33bd4380f1a3da2b9a5891a46d0e",
          "bytes": 1249,
          "artifact_id": "artifact-1839cbb518874a78",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs",
          "sha256": "cae545b58771c4f0e1b4d470993439d7159c62f55baed6e7e77b2fdb4f47db04",
          "bytes": 15474,
          "artifact_id": "artifact-7d8b9b1b61444168",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs.meta",
          "sha256": "753c3f1dacebbf36ffe799b722640285a0f4101d60f65b7bee23305e267785eb",
          "bytes": 240,
          "artifact_id": "artifact-545b0e76d9b540dc",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Presentation/PresentationPort.cs",
          "sha256": "91cb4f16e34e6128add565330764b7cd658c72534b94afd6771d2f90629d90f5",
          "bytes": 2262,
          "artifact_id": "artifact-64c9e0043af440a4",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/.gitignore",
          "sha256": "bed6308a89cccf01b2576cade9bc0e84c47e1b496ad8fc21750722966ce27692",
          "bytes": 12,
          "artifact_id": "artifact-f0aa9eca222e4fed",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/PortCompile.csproj",
          "sha256": "26dae75ac92c9e22fdce2ae844cc9dd2c93911db695e3932193c8c7a1c628471",
          "bytes": 375,
          "artifact_id": "artifact-0d97a6eeed1a473b",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs",
          "sha256": "4261c71262f882bd13478f7fe895e808124e01fa2c4f1081a9d701b694120704",
          "bytes": 8257,
          "artifact_id": "artifact-aabefa6c9a0c46c0",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Scenes/Gameplay.unity",
          "sha256": "87778f3d65dbb6c1f5d47d015cc95b11e30420abce64fc21a283952525233925",
          "bytes": 1134,
          "artifact_id": "artifact-c59bf4099ad347e8",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Scenes/Gameplay.unity.meta",
          "sha256": "5601f52c85a2069211e24cbd4530fa5d6b6a7e73f787c18fc3ae8b184fa425d5",
          "bytes": 152,
          "artifact_id": "artifact-294432f91cc94474",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/UI/INTEGRATION.md",
          "sha256": "5e0b4bd136923f5e871714586055f83b7837fcd874ca1deb484c26a5aadae4df",
          "bytes": 4961,
          "artifact_id": "artifact-a8376cb4fdc84189",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Presentation/DevelopmentPresentationPort.cs.meta",
          "sha256": "832919cf7e278ce1414c48bca9282c31aa204fdec298f2a9e5e539b4d7c70325",
          "bytes": 240,
          "artifact_id": "artifact-b7296d76c2694c5a",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/Presentation/PresentationPort.cs.meta",
          "sha256": "7d9a0e590c163d875fd0a2dd86f3ab643aa7f1a939d3c76faa7e9adfa18f681c",
          "bytes": 240,
          "artifact_id": "artifact-409746d819ef438a",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs.meta",
          "sha256": "1ad78ec4809c7765b7ce2002b7b5f62b6a0d7c7d4d6a96f0765cea39402bc4f7",
          "bytes": 240,
          "artifact_id": "artifact-7e6524df49ce4ef1",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        },
        {
          "path": "Unity/Assets/HotpotSort/Art/ImportMetadata/resource-provenance.json",
          "sha256": "bad3ea385150632af97c4b4ca1b3270b4ef756cac25cdbebed9204b5e9b9e6f7",
          "bytes": 3283,
          "artifact_id": "artifact-6613727434af4c9e",
          "kind": "file",
          "run_id": "run-ca718de95951474b",
          "task_revision": 7
        }
      ]
    }
  },
  "history": [
    {
      "at": "2026-09-20T09:08:45.023790+00:00",
      "event": "created"
    },
    {
      "at": "2026-09-20T09:09:59.806131+00:00",
      "event": "contract_changed",
      "revision": 2,
      "decision": "用户 2026-09-20：实行 task2；随后同意以 v0.1 browser_mobile.png 为替代设计基准，沿用暖色食材插画，首轮复用现有素材与原生 UI，不调用外部图片生成。",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:10:04.811286+00:00",
      "event": "dispatched",
      "run_id": "run-3dd83e5826fe401a",
      "step": "design"
    },
    {
      "at": "2026-09-20T09:14:58.329705+00:00",
      "event": "accepted",
      "run_id": "run-3dd83e5826fe401a"
    },
    {
      "at": "2026-09-20T09:17:01.271658+00:00",
      "event": "contract_changed",
      "revision": 3,
      "decision": "用户 2026-09-20 提供新界面参考图并要求以此为风格参考；不复制图中品牌、角色、文案和活动内容。",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:17:22.016625+00:00",
      "event": "contract_changed",
      "revision": 4,
      "decision": "用户 2026-09-20 澄清：新图片只参考布局，不参考任何美术效果；保留此前暖色食材插画方向。",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:17:26.650351+00:00",
      "event": "dispatched",
      "run_id": "run-826b45a1055a4c22",
      "step": "design"
    },
    {
      "at": "2026-09-20T09:20:27.353761+00:00",
      "event": "accepted",
      "run_id": "run-826b45a1055a4c22"
    },
    {
      "at": "2026-09-20T09:22:31.052776+00:00",
      "event": "contract_changed",
      "revision": 5,
      "decision": "用户 2026-09-20 要求 Design-Art 按新附件格式生成三视口并排、带尺寸和实现说明的评审示意板；附件内项目内容仅作版式示例。",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:22:35.479952+00:00",
      "event": "dispatched",
      "run_id": "run-fab0d4d476d1443d",
      "step": "design"
    },
    {
      "at": "2026-09-20T09:26:28.329577+00:00",
      "event": "accepted",
      "run_id": "run-fab0d4d476d1443d"
    },
    {
      "at": "2026-09-20T09:27:53.557301+00:00",
      "event": "contract_changed",
      "revision": 6,
      "decision": "用户 2026-09-20 要求入口设备界面按钮改为开始下火锅并保留，删除其他用户界面文字；评审板外围标注保留。",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:27:57.994187+00:00",
      "event": "dispatched",
      "run_id": "run-9329c81d377641de",
      "step": "design"
    },
    {
      "at": "2026-09-20T09:30:09.310437+00:00",
      "event": "accepted",
      "run_id": "run-9329c81d377641de"
    },
    {
      "at": "2026-09-20T09:30:45.504639+00:00",
      "event": "contract_changed",
      "revision": 7,
      "decision": "用户 2026-09-20 要求删除入口设备界面中的空白顶部框、侧栏框和下方信息框，仅保留中央食材盘与开始下火锅按钮。",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:30:45.748098+00:00",
      "event": "dispatched",
      "run_id": "run-ac27d30bc8ff4e7b",
      "step": "design"
    },
    {
      "at": "2026-09-20T09:32:43.212120+00:00",
      "event": "accepted",
      "run_id": "run-ac27d30bc8ff4e7b"
    },
    {
      "at": "2026-09-20T09:35:02.912180+00:00",
      "event": "approved",
      "scopes": [
        "preview",
        "build"
      ],
      "decision": "用户 2026-09-20 回复实现：批准 TASK-002 revision 7 的 v005 入口布局评审板、三视口预览和复现配方，并授权按该基准开始 Unity 实现。"
    },
    {
      "at": "2026-09-20T09:35:08.204233+00:00",
      "event": "dispatched",
      "run_id": "run-ca718de95951474b",
      "step": "code"
    },
    {
      "at": "2026-09-20T09:49:41.647554+00:00",
      "event": "accepted",
      "run_id": "run-ca718de95951474b"
    }
  ],
  "task_revision": 7,
  "status": "READY_FOR_RELEASE",
  "contract": {
    "goal": "在 Unity 中实现面向微信小游戏的可玩场景与全部玩家界面：展示盘子、订单和固定五格暂存，处理物理供给与触摸命中，并提供今日入口、暂停、结果、异常和同日重试流程。用户 2026-09-20 新提供图片只用于入口页面布局关系，不参考其色彩、描边、角色、字体或其他美术效果，视觉仍沿用已批准的暖色食材插画方向。入口设备界面仅保留中央盘子食材主视觉和主按钮“开始下火锅”；删除其他文字以及顶部、两侧和下方的空白信息框，评审板外围尺寸与实现标注保留。实现前预览需另提供一张评审示意板，将常规竖屏、窄屏安全区和较宽竖屏并排展示，并标注尺寸、布局变化及实现边界。",
    "qa_intent": [
      {
        "id": "TASK-002.QI-001",
        "text": "空间阻塞时不生成、不跳号、不改队列；恢复后仍按下一盘进入。",
        "source": "Daily SPEC v1.0 SUP 01-02 and section 8"
      },
      {
        "id": "TASK-002.QI-002",
        "text": "末件逻辑离开后移除盘子的核心占用，清除动画不继续占用核心容量。",
        "source": "Daily SPEC v1.0 sections 7.3 and 9.3"
      },
      {
        "id": "TASK-002.QI-003",
        "text": "视口和安全区坐标转换后命中正确稳定 itemId；UI、遮挡、盘缘、空白或不存在目标不提交有效点击。",
        "source": "Daily SPEC v1.0 section 9.1 and prototype hit protection"
      },
      {
        "id": "TASK-002.QI-004",
        "text": "重复点击同一 item 不重复呈现提交；短锁和长连锁表现不改变核心接受条件或库存。",
        "source": "Daily SPEC v1.0 BUF 06 and section 9.4"
      },
      {
        "id": "TASK-002.QI-005",
        "text": "显示两个启用订单和固定五格暂存；吸收后原位置留空，其他格不移动，订单独立补位。",
        "source": "Daily SPEC v1.0 BUF 04 and sections 7.4 and 10"
      },
      {
        "id": "TASK-002.QI-006",
        "text": "普通点击、自动吸收和多次连锁后，事件驱动表现收敛到权威快照，动画不消费 Director 流。",
        "source": "Daily SPEC v1.0 sections 4.3, 6.3 and 9.3"
      },
      {
        "id": "TASK-002.QI-007",
        "text": "暂存 5/5 时不显示失败；只有溢出事件显示失败；Aborted 与玩家失败区分。",
        "source": "Daily SPEC v1.0 BUF 01-03 and section 14"
      },
      {
        "id": "TASK-002.QI-008",
        "text": "今日入口、暂停、继续、结果、同日重试和退出操作可达，并只向会话层发送已定义语义命令。",
        "source": "Daily SPEC v1.0 sections 4.1 and 7.1"
      },
      {
        "id": "TASK-002.QI-009",
        "text": "结果页只展示核心与会话提供的事实字段，不增加星级、段位或虚构成绩。",
        "source": "Daily SPEC v1.0 sections 18.3 and 22"
      },
      {
        "id": "TASK-002.QI-010",
        "text": "16 个互异食材条目的 ID、资源、碰撞和尺寸映射稳定，外观复用不改变同类关系。",
        "source": "Daily SPEC v1.0 sections 5.1 and 16.5"
      },
      {
        "id": "TASK-002.QI-011",
        "text": "实际采用已批准的预览；低端和主流微信设备上的可读性、触控、安全区、失败和重试保留人工证据。",
        "source": "AGENTS.md section 5 and Daily SPEC v1.0 section 20.4"
      },
      {
        "id": "TASK-002.QI-012",
        "text": "分别记录熟练和普通玩家的体验时长；4-7 和 6-10 分钟只作为待真人验证目标，不作为自动通过阈值。",
        "source": "Daily SPEC v1.0 sections 4.2 and 20.4"
      }
    ],
    "technical_design_required": false,
    "visual_impact": "major",
    "needs_code": true,
    "needs_art": false,
    "owners": {
      "code_builder": [
        "Unity/Assets/HotpotSort/Runtime/Presentation",
        "Unity/Assets/HotpotSort/Runtime/UnityPhysics",
        "Unity/Assets/HotpotSort/UI",
        "Unity/Assets/HotpotSort/Scenes/Gameplay.unity",
        "Unity/Assets/HotpotSort/Scenes/Gameplay.unity.meta",
        "Unity/Assets/HotpotSort/Prefabs",
        "Unity/Assets/HotpotSort/Art/ImportMetadata",
        "Unity/Assets/HotpotSort/Resources/Hotpot"
      ],
      "design_art_agent": []
    },
    "references": [
      {
        "path": ".harness/references/TASK-002/review-board-reference.png",
        "sha256": "0ae0a819c838183b0b2e7d6e4bfdeffae727c95c62dee837df3bb35f6c2529f8",
        "bytes": 1285387,
        "source": "User-provided review-board format reference on 2026-09-20",
        "viewport": "2186x1645 reference board",
        "state": "deliverable-format reference only: three annotated viewport composites on a dark review board; embedded project name, artwork, copy and measurements are examples rather than Task 2 requirements"
      },
      {
        "path": ".harness/references/TASK-002/ui-style-reference-2026-09.jpg",
        "sha256": "2a0a20c091d68aa87cf5a839cb8b7d2a3d2541bda2376a6245990d4deb18c015",
        "bytes": 435286,
        "source": "User-provided UI style reference on 2026-09-20",
        "viewport": "1080x2341 source image; device and safe-area identity unknown",
        "state": "layout reference only: top status area, central primary content, peripheral side actions and bottom primary action; all art style, colors, outlines, characters, fonts, branding, text and event content are excluded"
      },
      {
        "path": ".harness/references/prototype-v0.1/HotpotSort_Prototype_v0.1/qa/browser_mobile.png",
        "sha256": "4031796a369527fc187de46d1183b79093c62687fade5e6599aa6d5ba7ba5e24",
        "bytes": 631109,
        "source": "v0.1 browser mobile simulation, explicitly approved by user on 2026-09-20 as a replacement design baseline",
        "viewport": "390x844 CSS viewport represented at 780x1688 pixels",
        "state": "prototype gameplay in progress; Design Composite reference, not a WeChat device capture"
      }
    ],
    "dependencies": [],
    "shared_touchpoints": [
      {
        "resource": "Contracts and public IDs",
        "region": "GameSnapshot, GameEventBatch, commands and viewport DTOs",
        "coordinator": "PM",
        "resolution": "Task 2 consumes the K0 contract as read-only and submits requested changes through PM; it does not create a competing contract copy."
      },
      {
        "resource": "Player-facing pages and theme",
        "region": "Gameplay, entry, pause, result and error views",
        "coordinator": "TASK-002",
        "resolution": "Task 2 owns the complete player-facing visual surface; the platform task supplies viewport facts and mounts IGameView without editing layout."
      }
    ],
    "interface_map": [
      {
        "interface": "IGameView",
        "source": "K0 contract target; not implemented in baseline",
        "called": false,
        "precondition": "platform bootstrap and viewport available",
        "effect": "mount, hide, reset, destroy and show errors"
      },
      {
        "interface": "TapCommand / TapResult",
        "source": "Daily SPEC v1.0 section 9",
        "called": false,
        "precondition": "stable itemId from Unity hit testing",
        "effect": "core decides acceptance; view renders returned events"
      },
      {
        "interface": "SupplyObservation / SupplyCommit",
        "source": "Daily SPEC v1.0 section 8",
        "called": false,
        "precondition": "space and cooldown observation",
        "effect": "core commits queue head; view creates only committed plate presentation"
      },
      {
        "interface": "GameSnapshot / GameEventBatch",
        "source": "Daily SPEC v1.0 sections 15-18",
        "called": false,
        "precondition": "versioned authoritative state",
        "effect": "view renders orders, buffer, plates, statistics and terminal state"
      },
      {
        "interface": "SessionAction",
        "source": "Daily SPEC v1.0 sections 4.1 and 7.1",
        "called": false,
        "precondition": "player presses a flow control",
        "effect": "send semantic start, pause, resume, retry or exit intent to session layer"
      },
      {
        "interface": "Viewport / PlatformLifecycle",
        "source": "WeChat target and Daily SPEC v1.0 section 20.4",
        "called": false,
        "precondition": "platform adapter supplies actual facts",
        "effect": "update safe-area layout and presentation clock behavior"
      }
    ],
    "asset_contract": [],
    "runtime_isolation": {
      "status": "worktree runtime directories created; Unity application adapters are not yet configured, so concurrent state-writing runs remain disabled"
    },
    "generation_budget": {
      "limit": 0,
      "decision": "User approved local reference-driven composition and clarified on 2026-09-20 that the new image is layout-only. Reuse existing food/plate imagery and native UI in the prior warm visual direction; no external image generation calls are authorized."
    }
  },
  "approvals": {
    "preview": {
      "artifacts": [
        "artifact-393a627bb2524885",
        "artifact-92ab2b4b2367469a",
        "artifact-f4b2a6baaee14897",
        "artifact-8820cb08561b4238"
      ],
      "task_revision": 7,
      "decision": "用户 2026-09-20 回复实现：批准 TASK-002 revision 7 的 v005 入口布局评审板、三视口预览和复现配方，并授权按该基准开始 Unity 实现。"
    },
    "build": {
      "task_revision": 7,
      "contract_digest": "1d23184dc2a75ca6e8ebd03aad6ee38b6a4d69d11a67bde005123ee74e3360ae",
      "decision": "用户 2026-09-20 回复实现：批准 TASK-002 revision 7 的 v005 入口布局评审板、三视口预览和复现配方，并授权按该基准开始 Unity 实现。",
      "artifacts": []
    }
  },
  "artifacts": {
    "artifact-4872f5782ec442d6": {
      "path": ".harness/previews/TASK-002/v001/gameplay.png",
      "sha256": "ecd00b3de7d8c3ea8cfb4346d1332a108c19cbe88c3653a17fa00fe610c6890e",
      "bytes": 618896,
      "artifact_id": "artifact-4872f5782ec442d6",
      "kind": "preview",
      "run_id": "run-3dd83e5826fe401a",
      "task_revision": 2
    },
    "artifact-13f184ace2704b4c": {
      "path": ".harness/previews/TASK-002/v001/flow-states.png",
      "sha256": "bf91b16115b0cc715001c5054016da5858d4304946c0e8a3e3d24010c83d8f3e",
      "bytes": 334786,
      "artifact_id": "artifact-13f184ace2704b4c",
      "kind": "preview",
      "run_id": "run-3dd83e5826fe401a",
      "task_revision": 2
    },
    "artifact-cdeb6b38b4bb45b7": {
      "path": ".harness/previews/TASK-002/v001/compose.py",
      "sha256": "6f31142a826194fac98c283ca67d08bde4d9862b794e9dada94ce882bed6ec8f",
      "bytes": 7311,
      "artifact_id": "artifact-cdeb6b38b4bb45b7",
      "kind": "composition_recipe",
      "run_id": "run-3dd83e5826fe401a",
      "task_revision": 2
    },
    "artifact-efc8599761aa41e5": {
      "path": ".harness/previews/TASK-002/v001/README.md",
      "sha256": "e5cc79e02a4708e8daf0019b32e3d4deb79ca7dca63ad70a4daece0cb13d0a4f",
      "bytes": 4102,
      "artifact_id": "artifact-efc8599761aa41e5",
      "kind": "composition_recipe",
      "run_id": "run-3dd83e5826fe401a",
      "task_revision": 2
    },
    "artifact-946177a198b8478d": {
      "path": ".harness/previews/TASK-002/v001/content-manifest.json",
      "sha256": "c7e6a3209ad4a38359470be81c793dd50393ada5270b10deef057ec2d5ed1711",
      "bytes": 6097,
      "artifact_id": "artifact-946177a198b8478d",
      "kind": "file",
      "run_id": "run-3dd83e5826fe401a",
      "task_revision": 2
    },
    "artifact-dcc42d1b17dc4082": {
      "path": ".harness/previews/TASK-002/v002/gameplay.png",
      "sha256": "ecd00b3de7d8c3ea8cfb4346d1332a108c19cbe88c3653a17fa00fe610c6890e",
      "bytes": 618896,
      "artifact_id": "artifact-dcc42d1b17dc4082",
      "kind": "preview",
      "run_id": "run-826b45a1055a4c22",
      "task_revision": 4
    },
    "artifact-7d10a9a423674497": {
      "path": ".harness/previews/TASK-002/v002/flow-states.png",
      "sha256": "dc5ef69a6b56c47781cc45efad7ce33432da59470fdcf5ca331ef4d6bd91269f",
      "bytes": 332217,
      "artifact_id": "artifact-7d10a9a423674497",
      "kind": "preview",
      "run_id": "run-826b45a1055a4c22",
      "task_revision": 4
    },
    "artifact-5b1bb502aa4a41ba": {
      "path": ".harness/previews/TASK-002/v002/compose.py",
      "sha256": "546b1c2f9b9db052cfd12d753e919ec2e1dd912b269c7427b95121c3eeac1aca",
      "bytes": 3559,
      "artifact_id": "artifact-5b1bb502aa4a41ba",
      "kind": "composition_recipe",
      "run_id": "run-826b45a1055a4c22",
      "task_revision": 4
    },
    "artifact-73c4c483038448f6": {
      "path": ".harness/previews/TASK-002/v002/README.md",
      "sha256": "7a2de85d06cdde9aa3278efc17b494ac5e891f0248ad4d2cd0d471c250324083",
      "bytes": 3083,
      "artifact_id": "artifact-73c4c483038448f6",
      "kind": "composition_recipe",
      "run_id": "run-826b45a1055a4c22",
      "task_revision": 4
    },
    "artifact-8cc6503e46b242b3": {
      "path": ".harness/previews/TASK-002/v002/content-manifest.json",
      "sha256": "3e9943b6ee4ea55ee54277979b338182ce432309a6bc492743878d04c38ea22f",
      "bytes": 6762,
      "artifact_id": "artifact-8cc6503e46b242b3",
      "kind": "file",
      "run_id": "run-826b45a1055a4c22",
      "task_revision": 4
    },
    "artifact-c2c286948af04237": {
      "path": ".harness/previews/TASK-002/v003/entry-layout-review-board.png",
      "sha256": "16572f9fea49de14f0229151c3a8803299df2ed5bb2903d2d7703065d2f0aa06",
      "bytes": 870032,
      "artifact_id": "artifact-c2c286948af04237",
      "kind": "preview",
      "run_id": "run-fab0d4d476d1443d",
      "task_revision": 5
    },
    "artifact-34696f5010f64152": {
      "path": ".harness/previews/TASK-002/v003/entry-390x844.png",
      "sha256": "04c1084b753df0680904ec6ca6fd1f94b62768562e356c7a07eb363135625b7f",
      "bytes": 103093,
      "artifact_id": "artifact-34696f5010f64152",
      "kind": "preview",
      "run_id": "run-fab0d4d476d1443d",
      "task_revision": 5
    },
    "artifact-5aa8d7fcc3f54d1d": {
      "path": ".harness/previews/TASK-002/v003/entry-320x693.png",
      "sha256": "361b4fb932975c19234f7be720dd34969c5622bbb21651503214121dd8b1a0d1",
      "bytes": 96200,
      "artifact_id": "artifact-5aa8d7fcc3f54d1d",
      "kind": "preview",
      "run_id": "run-fab0d4d476d1443d",
      "task_revision": 5
    },
    "artifact-77dcf46ec9a24dd8": {
      "path": ".harness/previews/TASK-002/v003/entry-700x1000.png",
      "sha256": "a15912b914fe27ec196c77022a1660ea706abebb7de2083da547993b2b4fab39",
      "bytes": 161243,
      "artifact_id": "artifact-77dcf46ec9a24dd8",
      "kind": "preview",
      "run_id": "run-fab0d4d476d1443d",
      "task_revision": 5
    },
    "artifact-3a5b144ebba642dd": {
      "path": ".harness/previews/TASK-002/v003/compose.py",
      "sha256": "baf627a47802e998684cacff37e81724323aff8bc5fe59d10ff27b02171e625b",
      "bytes": 6795,
      "artifact_id": "artifact-3a5b144ebba642dd",
      "kind": "composition_recipe",
      "run_id": "run-fab0d4d476d1443d",
      "task_revision": 5
    },
    "artifact-e2a74fbb3cae4117": {
      "path": ".harness/previews/TASK-002/v003/layout-spec.json",
      "sha256": "d5946bda5fcd67d392549b77dbd20a3c52d706d013cec4c6da45ffbb0411d78f",
      "bytes": 761,
      "artifact_id": "artifact-e2a74fbb3cae4117",
      "kind": "composition_recipe",
      "run_id": "run-fab0d4d476d1443d",
      "task_revision": 5
    },
    "artifact-ca30332c618c45d8": {
      "path": ".harness/previews/TASK-002/v003/README.md",
      "sha256": "c0f7ecd170f42c3a2811cf30b11d67e6e746781a20e0d459fd606af0c5380f7d",
      "bytes": 3032,
      "artifact_id": "artifact-ca30332c618c45d8",
      "kind": "composition_recipe",
      "run_id": "run-fab0d4d476d1443d",
      "task_revision": 5
    },
    "artifact-127da47886524c03": {
      "path": ".harness/previews/TASK-002/v003/content-manifest.json",
      "sha256": "73391e0461e3239b205153ba1b6155711b4e9181e1623b3fe98e663152e0436c",
      "bytes": 7201,
      "artifact_id": "artifact-127da47886524c03",
      "kind": "file",
      "run_id": "run-fab0d4d476d1443d",
      "task_revision": 5
    },
    "artifact-bbd9665b6b6f4b28": {
      "path": ".harness/previews/TASK-002/v004/entry-layout-review-board.png",
      "sha256": "a0bb66fa2dbc323a9a8a07b98e2ed254cf26af25b9c29b596fa1f19c91e903d8",
      "bytes": 579628,
      "artifact_id": "artifact-bbd9665b6b6f4b28",
      "kind": "preview",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-2e7c7042e86e4dfe": {
      "path": ".harness/previews/TASK-002/v004/entry-390x844.png",
      "sha256": "90131307803cab120e017ff565960dc603347ee044f102b53084c39832a41081",
      "bytes": 51212,
      "artifact_id": "artifact-2e7c7042e86e4dfe",
      "kind": "preview",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-38bf435358514102": {
      "path": ".harness/previews/TASK-002/v004/entry-320x693.png",
      "sha256": "cc9616106489280d7cf96621bb4783536dc08031803529ccc6af6537501267da",
      "bytes": 46162,
      "artifact_id": "artifact-38bf435358514102",
      "kind": "preview",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-915bdfba693d4e0f": {
      "path": ".harness/previews/TASK-002/v004/entry-700x1000.png",
      "sha256": "80de7a030c515b50db7119d458d12e6b18a2d6dc8b33e687df0870def774f0f5",
      "bytes": 97102,
      "artifact_id": "artifact-915bdfba693d4e0f",
      "kind": "preview",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-485875327ca547fb": {
      "path": ".harness/previews/TASK-002/v004/compose.py",
      "sha256": "d8425470e0ada0eac68f2d278f937f566683af4e10dcfccd7243d65344eefe11",
      "bytes": 3173,
      "artifact_id": "artifact-485875327ca547fb",
      "kind": "composition_recipe",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-981744e9cbae442c": {
      "path": ".harness/previews/TASK-002/v004/layout-spec.json",
      "sha256": "dc9c8f6751bfbb2fd0d346442973b10014a7b4c2ff175a626733cd8a89430a5c",
      "bytes": 1229,
      "artifact_id": "artifact-981744e9cbae442c",
      "kind": "composition_recipe",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-5adec3ac25c24df4": {
      "path": ".harness/previews/TASK-002/v004/README.md",
      "sha256": "79d3b6fb9dda41df864e3c24d3f3d8a844cbea542d4f7d8e52e85ffaac9b0d81",
      "bytes": 2145,
      "artifact_id": "artifact-5adec3ac25c24df4",
      "kind": "composition_recipe",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-6018b7a9f78a4bb9": {
      "path": ".harness/previews/TASK-002/v004/text-layer-audit.json",
      "sha256": "0f1de9e4b74aefc3165489ab57716cde945913a270dba04872df85765af9cc4e",
      "bytes": 510,
      "artifact_id": "artifact-6018b7a9f78a4bb9",
      "kind": "file",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-0b17978697a44c92": {
      "path": ".harness/previews/TASK-002/v004/content-manifest.json",
      "sha256": "838cc6e75371ae52dcb9cc41f00fc31fcfce948f30ba9179c638f23ab81ead75",
      "bytes": 7725,
      "artifact_id": "artifact-0b17978697a44c92",
      "kind": "file",
      "run_id": "run-9329c81d377641de",
      "task_revision": 6
    },
    "artifact-393a627bb2524885": {
      "path": ".harness/previews/TASK-002/v005/entry-layout-review-board.png",
      "sha256": "055885f7d15c37af969dd202eb85a8f17d5166a77781fe41298b39200158fc86",
      "bytes": 571907,
      "artifact_id": "artifact-393a627bb2524885",
      "kind": "preview",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-92ab2b4b2367469a": {
      "path": ".harness/previews/TASK-002/v005/entry-390x844.png",
      "sha256": "05ebe4cb1bb38c96bee343085c437d8d51544c68c54df0d0acea86af425cbf4e",
      "bytes": 49710,
      "artifact_id": "artifact-92ab2b4b2367469a",
      "kind": "preview",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-f4b2a6baaee14897": {
      "path": ".harness/previews/TASK-002/v005/entry-320x693.png",
      "sha256": "ddba2585f2057495fce01723051640c259630800d5bc8c674f3a73e4422152a8",
      "bytes": 44731,
      "artifact_id": "artifact-f4b2a6baaee14897",
      "kind": "preview",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-8820cb08561b4238": {
      "path": ".harness/previews/TASK-002/v005/entry-700x1000.png",
      "sha256": "0ee2a689820636fe0bee578367d31b45bdd98a0f5349f6aacb71d558a088ff92",
      "bytes": 96124,
      "artifact_id": "artifact-8820cb08561b4238",
      "kind": "preview",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-ea9e351c015e4df7": {
      "path": ".harness/previews/TASK-002/v005/compose.py",
      "sha256": "c54c4a2d2a706642fbd0ea1161e200c020b6f5d60dd831c0267a8885f0a43e49",
      "bytes": 3417,
      "artifact_id": "artifact-ea9e351c015e4df7",
      "kind": "composition_recipe",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-e3e53bbcb52c47ee": {
      "path": ".harness/previews/TASK-002/v005/layout-spec.json",
      "sha256": "3711413eabd2e5b475193067624e2cce391778fb2cb0e3beebc42fd60c371844",
      "bytes": 1465,
      "artifact_id": "artifact-e3e53bbcb52c47ee",
      "kind": "composition_recipe",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-c768bf3fb8264dd5": {
      "path": ".harness/previews/TASK-002/v005/README.md",
      "sha256": "6e1cd3ef8fbf144bfff39d2d4deeb10b998fe6ba985b3fa9ba9a1615091a0e69",
      "bytes": 1926,
      "artifact_id": "artifact-c768bf3fb8264dd5",
      "kind": "composition_recipe",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-8e5220458cb542a0": {
      "path": ".harness/previews/TASK-002/v005/layer-audit.json",
      "sha256": "bd5b3bdf15777d255dbeb2e2f1843065c91d07c2e735c136b7f9df7ec50417a3",
      "bytes": 1665,
      "artifact_id": "artifact-8e5220458cb542a0",
      "kind": "file",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-1a52e56d61824c46": {
      "path": ".harness/previews/TASK-002/v005/content-manifest.json",
      "sha256": "62747a3bc6ae06bc37b42cafd9a2e34d7b7a1e8b002bfbf583f6dcb0e9a1fa5f",
      "bytes": 7721,
      "artifact_id": "artifact-1a52e56d61824c46",
      "kind": "file",
      "run_id": "run-ac27d30bc8ff4e7b",
      "task_revision": 7
    },
    "artifact-97c0973f157f4916": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_00.png",
      "sha256": "0beb6a4df89b27cf3abec4bbf636ff03538ee5f81a9245daa2a7edf9e36075bc",
      "bytes": 7183,
      "artifact_id": "artifact-97c0973f157f4916",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-bf3b4c551a6848fe": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_00.png.meta",
      "sha256": "1539d845ce4519963ba87d2119f8049c128c6912fee0a348aa13f4e7f4b1ecd9",
      "bytes": 548,
      "artifact_id": "artifact-bf3b4c551a6848fe",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-3399958cc6984a44": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_01.png",
      "sha256": "6fbb315bdfbf1db1a1fe2fdb83c68831ed958461e12cc879260b7ba861e08e79",
      "bytes": 8493,
      "artifact_id": "artifact-3399958cc6984a44",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-81da51b05c0e43ac": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_01.png.meta",
      "sha256": "c4274c79ac8616070911f843f40c3c0ecdd85a1d13d0b8660fc944ffadc8c8a5",
      "bytes": 548,
      "artifact_id": "artifact-81da51b05c0e43ac",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-99248155b7c0486c": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_02.png",
      "sha256": "c386eae16fa591c9af607854a317050434bf25d4c2c33fcca6b0eb25391f0e4e",
      "bytes": 6291,
      "artifact_id": "artifact-99248155b7c0486c",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-c40bd95d25d3479b": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_02.png.meta",
      "sha256": "d6aef9a9565caa3c779cd30cb1f6bf7cfede2794834b49d98174e44c194167fe",
      "bytes": 548,
      "artifact_id": "artifact-c40bd95d25d3479b",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-eef407ffcca4444e": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_03.png",
      "sha256": "f387e5aebd3a3dc73a4af9bc8ed5139eaa43f1a59aff18992b975a6e36255d4a",
      "bytes": 7284,
      "artifact_id": "artifact-eef407ffcca4444e",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-57ad7565e34c4e9b": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_03.png.meta",
      "sha256": "f6d406dc5758dc7207b31e2a4810e60831242d7f31c0beeac4899683e6eb8f97",
      "bytes": 548,
      "artifact_id": "artifact-57ad7565e34c4e9b",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-c39595b6d279458b": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_04.png",
      "sha256": "a72d49cbba152498fc10a211b34de47d559c4cd9e6ea9fabdcda822a797999cd",
      "bytes": 6873,
      "artifact_id": "artifact-c39595b6d279458b",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-a1f3b73d6dcf484b": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_04.png.meta",
      "sha256": "d4796c1f792d33591b7999c5eca979cde49a6ce8a3d8c2936c03a49c02632918",
      "bytes": 548,
      "artifact_id": "artifact-a1f3b73d6dcf484b",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-214b403011d04787": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_05.png",
      "sha256": "594033016f51cdb74b9f1968136edab924b00cb9264285e70b122c5ca312f2ec",
      "bytes": 5399,
      "artifact_id": "artifact-214b403011d04787",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-143ef193613443b3": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_05.png.meta",
      "sha256": "df77b0bbf9a2e229ef2220131b8b307cc6877a75f272b45858fa6cf4a7745d91",
      "bytes": 548,
      "artifact_id": "artifact-143ef193613443b3",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-74725b664a514117": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_06.png",
      "sha256": "3453d0b4d5523bf300cc536e45a4fe5b74a406dd0b0761a7530c8ae904e22526",
      "bytes": 8084,
      "artifact_id": "artifact-74725b664a514117",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-cdbf335119d14e9e": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_06.png.meta",
      "sha256": "ac323f72a89efd45c2804ed1ea8f0cfecd829d278077606e80d13fead2217f5f",
      "bytes": 548,
      "artifact_id": "artifact-cdbf335119d14e9e",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-4508ba9aab304919": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_07.png",
      "sha256": "975e2efc8431933d5ff306da4cb303624a749270f8f32e653de19dd752a08549",
      "bytes": 5902,
      "artifact_id": "artifact-4508ba9aab304919",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-99f18069cf594af2": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_07.png.meta",
      "sha256": "8ec4251b0875d552d1ad796535f0a3288ab9b5c3b67ecd80ee76ea047d5adc61",
      "bytes": 548,
      "artifact_id": "artifact-99f18069cf594af2",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-a0f240c92e8146f2": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_08.png",
      "sha256": "8cd7dc9cf9baa8ec3649d0780e6f1f6de3b3bc04dc326def3a2984fe9dc0522d",
      "bytes": 5682,
      "artifact_id": "artifact-a0f240c92e8146f2",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-f19be4e05fa844e5": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_08.png.meta",
      "sha256": "cd6ed9493f7f8bc35d8e326eedf324c849692ff105f0a3a790bbd41ee2545a17",
      "bytes": 548,
      "artifact_id": "artifact-f19be4e05fa844e5",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-f83203b6838c41e8": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_09.png",
      "sha256": "30bac979677c27b7a4d56820188fd343317e242f78603e5d1e07faebe17b2c9c",
      "bytes": 4981,
      "artifact_id": "artifact-f83203b6838c41e8",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-91afddc82081453b": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_09.png.meta",
      "sha256": "7dd879e4c506afef22c5c8eeb9f732c6a44188598345cbaeb6ce6cbc1e8862b2",
      "bytes": 548,
      "artifact_id": "artifact-91afddc82081453b",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-db6100ac5f8546b5": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_10.png",
      "sha256": "b93b65efe233391f271db4706eab52d0d0d4599b443cbca687a33813cccebe66",
      "bytes": 6482,
      "artifact_id": "artifact-db6100ac5f8546b5",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-49f35758c87d48c4": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_10.png.meta",
      "sha256": "04a1723dedc5c43cfdd99939253f9e988ab82f16698f6be88ede5c73149d15ee",
      "bytes": 548,
      "artifact_id": "artifact-49f35758c87d48c4",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-a05f9bc58ab44ddb": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_11.png",
      "sha256": "45bc2df15f9b0713e66ec6337ae52f656323a1300233bae47ec1088ea83672c5",
      "bytes": 5287,
      "artifact_id": "artifact-a05f9bc58ab44ddb",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-798095aebc774411": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_11.png.meta",
      "sha256": "14c1f32202059c0c5b71c3853926728390a100056706194f5158b2c797955c88",
      "bytes": 548,
      "artifact_id": "artifact-798095aebc774411",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-96471dbc0fd14222": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_12.png",
      "sha256": "5eb5d998e8f72ac723fe6f07c55cceca30018dc4d0efa13d5659c9361ade10ac",
      "bytes": 5671,
      "artifact_id": "artifact-96471dbc0fd14222",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-ecf3c9f97d104c45": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_12.png.meta",
      "sha256": "e9771671da37c054c2237831d4c9eb26bed91dc9b6410064d6d0aa92145e3e97",
      "bytes": 548,
      "artifact_id": "artifact-ecf3c9f97d104c45",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-7f537c2c073c4ada": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_13.png",
      "sha256": "49bb9d34442c30a55bcd01e83e335f3a0050b38a03fb9d782f5a309259207d4f",
      "bytes": 5758,
      "artifact_id": "artifact-7f537c2c073c4ada",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-ddd07f86a77b4277": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_13.png.meta",
      "sha256": "aef9e082916959096b68de67dac57f501ddff10b209d7ab6ea05d8896ba33650",
      "bytes": 548,
      "artifact_id": "artifact-ddd07f86a77b4277",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-7011a7be3f5a429a": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_14.png",
      "sha256": "82ea9f941de9f42dc345b15f375bd1c7bed98a65af6254b8fd7bf48f5ddb45eb",
      "bytes": 9964,
      "artifact_id": "artifact-7011a7be3f5a429a",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-857d36e0bb034bcd": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_14.png.meta",
      "sha256": "3c501a64dccf0d7ea86e8f07b7401136330a889123b384d2882e6bb0d15b77b0",
      "bytes": 548,
      "artifact_id": "artifact-857d36e0bb034bcd",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-65b57a3947a04c47": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_15.png",
      "sha256": "1e38a195ec019899fc29b19853bf01fa707b71258d16a5769ac498878b8b3f8c",
      "bytes": 5234,
      "artifact_id": "artifact-65b57a3947a04c47",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-51993795b0794a3e": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/food_15.png.meta",
      "sha256": "93841f00f4606fd8b6119c08c537bd3ad4b072bc4b618b369cbbbd6ee8bd718d",
      "bytes": 548,
      "artifact_id": "artifact-51993795b0794a3e",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-8e32f90b47d6487f": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/game-data.json",
      "sha256": "55930baff402fed70c618a52144a0d6d828858e7dfe7b78e204fbf5c5deb6982",
      "bytes": 15920,
      "artifact_id": "artifact-8e32f90b47d6487f",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-53dc83ddda4d49c5": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/game-data.json.meta",
      "sha256": "d87b0b9ee38e31562fe049652fd0d0ae6cc2169f04af1fc0fa0efd795aa472bc",
      "bytes": 162,
      "artifact_id": "artifact-53dc83ddda4d49c5",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-47ad6a40175c408e": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/plate.png",
      "sha256": "a4e6bb0f78d38585367eb551931fd0d10aa2b251d70d937e17d6b5e0d6201bdb",
      "bytes": 17377,
      "artifact_id": "artifact-47ad6a40175c408e",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-6f042593a70349ae": {
      "path": "Unity/Assets/HotpotSort/Resources/Hotpot/plate.png.meta",
      "sha256": "0f62b8dab5f0dd820cc976003cc76b338ae702ce3fc3cfdcf8310d90016e79ae",
      "bytes": 548,
      "artifact_id": "artifact-6f042593a70349ae",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-1839cbb518874a78": {
      "path": "Unity/Assets/HotpotSort/Runtime/Presentation/DevelopmentPresentationPort.cs",
      "sha256": "e100f428b331c1a38c04ecafd91f1e31659f33bd4380f1a3da2b9a5891a46d0e",
      "bytes": 1249,
      "artifact_id": "artifact-1839cbb518874a78",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-7d8b9b1b61444168": {
      "path": "Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs",
      "sha256": "cae545b58771c4f0e1b4d470993439d7159c62f55baed6e7e77b2fdb4f47db04",
      "bytes": 15474,
      "artifact_id": "artifact-7d8b9b1b61444168",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-545b0e76d9b540dc": {
      "path": "Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs.meta",
      "sha256": "753c3f1dacebbf36ffe799b722640285a0f4101d60f65b7bee23305e267785eb",
      "bytes": 240,
      "artifact_id": "artifact-545b0e76d9b540dc",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-64c9e0043af440a4": {
      "path": "Unity/Assets/HotpotSort/Runtime/Presentation/PresentationPort.cs",
      "sha256": "91cb4f16e34e6128add565330764b7cd658c72534b94afd6771d2f90629d90f5",
      "bytes": 2262,
      "artifact_id": "artifact-64c9e0043af440a4",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-f0aa9eca222e4fed": {
      "path": "Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/.gitignore",
      "sha256": "bed6308a89cccf01b2576cade9bc0e84c47e1b496ad8fc21750722966ce27692",
      "bytes": 12,
      "artifact_id": "artifact-f0aa9eca222e4fed",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-0d97a6eeed1a473b": {
      "path": "Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/PortCompile.csproj",
      "sha256": "26dae75ac92c9e22fdce2ae844cc9dd2c93911db695e3932193c8c7a1c628471",
      "bytes": 375,
      "artifact_id": "artifact-0d97a6eeed1a473b",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-aabefa6c9a0c46c0": {
      "path": "Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs",
      "sha256": "4261c71262f882bd13478f7fe895e808124e01fa2c4f1081a9d701b694120704",
      "bytes": 8257,
      "artifact_id": "artifact-aabefa6c9a0c46c0",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-c59bf4099ad347e8": {
      "path": "Unity/Assets/HotpotSort/Scenes/Gameplay.unity",
      "sha256": "87778f3d65dbb6c1f5d47d015cc95b11e30420abce64fc21a283952525233925",
      "bytes": 1134,
      "artifact_id": "artifact-c59bf4099ad347e8",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-294432f91cc94474": {
      "path": "Unity/Assets/HotpotSort/Scenes/Gameplay.unity.meta",
      "sha256": "5601f52c85a2069211e24cbd4530fa5d6b6a7e73f787c18fc3ae8b184fa425d5",
      "bytes": 152,
      "artifact_id": "artifact-294432f91cc94474",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-a8376cb4fdc84189": {
      "path": "Unity/Assets/HotpotSort/UI/INTEGRATION.md",
      "sha256": "5e0b4bd136923f5e871714586055f83b7837fcd874ca1deb484c26a5aadae4df",
      "bytes": 4961,
      "artifact_id": "artifact-a8376cb4fdc84189",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-b7296d76c2694c5a": {
      "path": "Unity/Assets/HotpotSort/Runtime/Presentation/DevelopmentPresentationPort.cs.meta",
      "sha256": "832919cf7e278ce1414c48bca9282c31aa204fdec298f2a9e5e539b4d7c70325",
      "bytes": 240,
      "artifact_id": "artifact-b7296d76c2694c5a",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-409746d819ef438a": {
      "path": "Unity/Assets/HotpotSort/Runtime/Presentation/PresentationPort.cs.meta",
      "sha256": "7d9a0e590c163d875fd0a2dd86f3ab643aa7f1a939d3c76faa7e9adfa18f681c",
      "bytes": 240,
      "artifact_id": "artifact-409746d819ef438a",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-7e6524df49ce4ef1": {
      "path": "Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs.meta",
      "sha256": "1ad78ec4809c7765b7ce2002b7b5f62b6a0d7c7d4d6a96f0765cea39402bc4f7",
      "bytes": 240,
      "artifact_id": "artifact-7e6524df49ce4ef1",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    },
    "artifact-6613727434af4c9e": {
      "path": "Unity/Assets/HotpotSort/Art/ImportMetadata/resource-provenance.json",
      "sha256": "bad3ea385150632af97c4b4ca1b3270b4ef756cac25cdbebed9204b5e9b9e6f7",
      "bytes": 3283,
      "artifact_id": "artifact-6613727434af4c9e",
      "kind": "file",
      "run_id": "run-ca718de95951474b",
      "task_revision": 7
    }
  },
  "completed": {
    "design": "run-ac27d30bc8ff4e7b",
    "code": "run-ca718de95951474b"
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

PM Note 2026-09-20: 正式合同依据 docs/task-drafts/TASK-002-unity-playview.md、Daily SPEC v1.0 与用户本会话决定建立。用户批准 v0.1 browser_mobile.png 作为 Design Composite 替代基准，沿用暖色食材插画，首轮复用现有素材与原生 UI，外部图片生成预算为 0。该批准仅覆盖设计基准；预览内容批准与正式实现绑定按实际产物另行记录。
