# Art Bible

> 只记录长期稳定、已经确认的视觉规则。没有确认时写 `Not documented`。

## Visual Goal

- Target feeling: 热闹、温暖、有食欲；点击食材下锅和完成订单时要有明确的整理爽感。
- Visual keywords: 传统火锅店、正俯视、红汤铜锅、木桌、白瓷盘、2D/2.5D、真实食物感、适度卡通化、细深棕描边。
- References: 用户提供的企业微信截图只作为食材质感、饱满度、俯视观察和鲜明色彩的参考；不作为需要复刻的完整界面。
- Prohibited directions: 写实摄影感、厚重油腻的材质、血腥生肉感、赛博风、过度儿童化、灰暗低对比配色、食材粘连难辨、带餐盘或界面残留的独立食材图。

## Camera and Perspective

- Camera: 正俯视。
- Perspective: 2D/2.5D 绘制出的材质、明暗和体积感；不依赖实时 3D 模型。
- Field of view / framing: 竖屏桌面布局，上方为订单小锅，中间为五个白瓷暂存小碟，下方为同平面碰撞、互不重叠的食材盘。
- Background depth: 首版只使用干净木质桌面，不加入店内人物、灯笼、窗格、招牌或其他室内装饰。

## Shape Language

- Characters: 当前不包含角色；`Not documented`。
- Environment: 深色暖木桌面作为统一背景；结构简洁，避免与食材争夺注意力。
- Props: 传统铜锅，统一红汤锅底；白瓷圆盘承载食材；五个白瓷小碟组成暂存区；未解锁锅位为未开火铜锅。
- UI: 订单牌悬于每口锅上方，显示食材图标及0/3至3/3数字计数，不使用三个圆点；圆角、传统招牌感的红金按钮与牌匾承载主要操作。

## Character Proportions

- Head-to-body ratio: 当前不包含角色；`Not documented`。
- Silhouette rules: 食材首先依靠可辨的外轮廓、典型形状和主色区分，不依赖小字或极细纹理。
- Scale relationships: 食材必须在盘内具有清楚可点击的尺寸；暂存小碟小于下方食材盘；订单小锅和订单牌在上方形成主要目标区。
- Must-preserve features: 正俯视、白瓷容器、铜锅红汤、细深棕描边、食材的独立识别度。

## Color System

- Core palette: 深红、木棕、铜金、暖象牙白；食材使用鲜明但自然的暖色、绿色、米白和棕色。
- Character / team colors: 当前不包含角色或阵营；`Not documented`。
- Background saturation: 木桌保持中低饱和和稳定纹理；红汤、订单牌、关键按钮和完成反馈承担高饱和重点。
- Contrast rules: 深棕描边用于食材外轮廓、锅沿和关键交互边界；文字、进度和可点击食材必须在手机尺寸下清楚可读。
- Accessibility considerations: 不只通过颜色区分食材，必须同时依赖形状、轮廓和订单图标；数字进度保持高对比。

## Line, Material and Lighting

- Outline: 使用克制的细深棕描边，不使用粗黑描边或无边界的糊状塑形。
- Material language: 食材接近真实食物，但以简化纹理和清晰色块表现；铜锅有温润金属感，白瓷有干净光泽，红汤有红油层次。
- Light direction: 使用统一、柔和的顶光；具体方向为 `Not documented`。
- Shadow style: 独立食材图不烘焙整体投影；只保留自身材质明暗和轻微接触暗部。运行时容器与物体关系可产生独立的轻量阴影。
- Texture detail: 纹理服务于食材辨识和食欲，不使用缩小后会形成噪点的细碎纹理。

## UI Style

- Layout principles: 订单目标始终在上方，暂存状态位于中间，玩家可操作的同平面食材盘位于下方；未解锁锅位提前可见，避免布局跳变。
- Button style: 深红底、铜金边框、传统牌匾或招牌感；主要操作区足够大，适合单手点击。
- Typography: 整体使用招牌手写体；数字和进度使用同风格但更规整、在手机小尺寸下易读的字形。
- Timer: 顶部倒计时显示阿拉伯数字MM:SS（如10:00、09:59），替代指针时钟图标；与订单n/3计数分区显示。
- Icon style: 订单以对应食材图标表达；道具和状态图标沿用铜金、白瓷、红色体系，保持清晰轮廓。
- Safe areas: 所有操作、订单牌、倒计时和重要数值必须位于微信真实安全区内。
- Nine-slice rules: 面板、订单牌和按钮需要可缩放边框；具体九宫格切片规范为 `Not documented`。

## Animation and VFX

- Animation style: 短促、清晰、有食物重量感；避免夸张的弹跳拖慢整理节奏。
- Timing language: 点击后快速飞入目标锅或暂存小碟；锅完成后整锅端走，再补入新锅。
- VFX density: 食材下锅使用水花、红油涟漪和蒸汽；订单完成使用更明显的热气和金色完成反馈。特效必须不遮挡后续可点击食材。
- Hit / movement feedback: 点击食材后立即显示去向；暂存自动匹配时从小碟滑入锅中；空盘被收走时需清楚腾出空间。

## Asset Production Rules

- Default formats: 独立食材为带 Alpha 的 PNG；容器、UI 和特效的最终格式按 Unity 导入需求确定。
- Transparency: 独立食材必须使用真透明背景；不得带餐盘、锅具、文字、数量标记、水印或整图背景。
- Naming convention: 沿用 Unity 资源目录的稳定命名；新资产不得覆盖已存在资源。具体命名规则为 `Not documented`。
- Source-file requirements: `Not documented`。
- Export sizes: `Not documented`；必须以微信手机实际显示尺寸验证清晰度和性能。
- Sprite-sheet rules: `Not documented`。
- Padding / pivot: 独立食材主体居中并保留安全边距；最终 Pivot 规则按 Unity 盘内摆放接口确定。

## Accepted Visual Decisions

- 全局采用正俯视的传统热闹火锅店视觉方向。
- 首版背景为干净木质桌面。
- 订单锅为统一红汤的传统铜锅。
- 食材盘为白瓷圆盘，暂存区为五个白瓷小碟。
- 食材使用真实食物感、适度简化纹理的 2D/2.5D 卡通画法。
- 食材使用细深棕描边，不烘焙整体投影。
- 订单完成时整锅端走；未解锁锅位提前显示为未开火铜锅。
- 文字使用招牌手写体，数字使用易读的规整变体。

## Rejected Visual Directions

- 纯正交、没有体积感的平面图标风。
- 明显无描边导致食材轮廓在混装盘内不清楚的方向。
- 独立食材资产带整体投影、餐盘、锅、文字、界面或水印。
- `.harness/previews/TASK-006/hotpot-food-plates-regenerated-v2.png` 已作废，不得再作为参考或输入。

## Confirmed Audio Direction

- 品质目标：定制品质原创音乐与音效；必须提供真实音频试听及游戏内混音人审，不能仅以生成成功或文件规格验收。
- 音乐：传统火锅店氛围，民乐打击与拨弦结合现代休闲节奏，90–105 BPM，无人声，60–90 秒无缝循环。
- 环境：轻微汤锅沸腾与店内远景声，低于音乐，避免盖住操作反馈。
- 操作：陶瓷轻碰、食材弹起/飞行、下锅扑通和滋响、上行订单完成音与端锅声、按钮及胜负短反馈，无语音播报。
- 设置：音乐、音效分别提供开关和音量。
- 音频生产来源、音源与授权、工程格式及混音规格：待实现调查确认。

## Remaining Art Production Questions

- 独立食材的最终画布尺寸、每种食材的主体占比和导出分辨率：`Not documented`。
- 锅内未完成订单的具体红汤、油花和蒸汽密度：`Not documented`。
- 手写字体的授权来源、嵌入文件和微信真机回退方案：`Not documented`。
- 店内装饰、角色、背景主题的后续扩展：`Not documented`。
