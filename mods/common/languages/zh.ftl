## Buttons
button-cancel = 取消
button-retry = 重试
button-back = 返回
button-continue = 继续
button-quit = 退出

## Server Orders
notification-custom-rules = 此地图包含自定义规则。游戏体验可能发生变化。
notification-map-bots-disabled = 此地图上已禁用机器人。
notification-two-humans-required = 此服务器需要至少两名人类玩家才能开始比赛。
notification-unknown-server-command = 未知服务器命令：{ $command }
notification-admin-start-game = 只有主持人可以开始游戏。
notification-no-start-until-required-slots-full = 在所需插槽满之前无法开始游戏。
notification-no-start-without-players = 如果没有玩家，则无法开始游戏。
notification-insufficient-enabled-spawn-points = 在启用更多重生点之前无法开始游戏。
notification-malformed-command = 格式错误的 { $command } 命令
notification-state-unchanged-ready = 标记为就绪时无法更改状态。
notification-invalid-faction-selected = 选择了无效的派系：{ $faction }
notification-supported-factions = 支持的值: { $factions }
notification-state-unchanged-game-started = 游戏开始后无法更改状态。({ $command })
notification-requires-host = 只有主持人可以这样做。
notification-invalid-bot-slot = 无法向已有另一个客户端的插槽添加机器人。
notification-invalid-bot-type = 无效的机器人类型。
notification-admin-change-map = 只有主持人可以更改地图。
notification-player-disconnected = { $player } 已断开连接。
notification-team-player-disconnected = { $player } (队伍 { $team }) 已断开连接。
notification-observer-disconnected = { $player } (观众) 已断开连接。
notification-unknown-map = 服务器上未找到该地图。
notification-searching-map = 正在资源中心搜索地图...
notification-admin-change-configuration = 只有主持人可以更改配置。
notification-changed-map = { $player } 将地图更改为 { $map }
notification-option-changed = { $player } 将 { $name } 更改为 { $value }.
notification-you-were-kicked = 您已被从服务器中踢出。
notification-admin-kicked = { $admin } 将 { $player } 从服务器中踢出。
notification-kicked = { $player } 被从服务器中踢出。
notification-temp-ban = { $admin } 暂时禁止 { $player } 进入服务器。
notification-admin-transfer-admin = 只有管理员才能将管理员权限转让给另一名玩家。
notification-admin-move-spectators = 只有主持人才能将玩家移至观众席。
notification-empty-slot = 该插槽中无人。
notification-move-spectators = { $admin } 将 { $player } 移至观众席。
notification-nick-changed = { $player } 现在称为 { $name }.
notification-player-dropped = 一名玩家因超时而被剔除。
notification-connection-problems = { $player } 正在遇到连接问题。
notification-timeout-dropped = { $player } 因超时而被剔除。
notification-timeout-dropped-in =
    { $timeout ->
        [one] { $player } 将在 { $timeout } 秒后剔除。
       *[other] { $player } 将在 { $timeout } 秒后剔除.
    }
notification-error-game-started = 游戏已经开始。
notification-requires-password = 服务器需要密码。
notification-incorrect-password = 密码错误。
notification-incompatible-mod = 服务器正在运行不兼容的模组。
notification-incompatible-version = 服务器正在运行不兼容的版本。
notification-incompatible-protocol = 服务器正在运行不兼容的协议。
notification-you-were-banned = 您已被禁止进入服务器。
notification-you-were-temp-banned = 您已被暂时禁止进入服务器。
notification-game-full = 游戏已满。
notification-new-admin = { $player } 现在是管理员。
notification-option-locked = 无法更改{ $option }
notification-invalid-configuration-command = 无效的配置命令。
notification-admin-option = 只有主持人可以设置该选项。
notification-error-number-teams = 无法解析队伍数量：{ $raw }
notification-admin-kick = 只有主持人可以踢出玩家。
notification-kick-self = 主持人无法将自己踢出。
notification-kick-none = 该插槽中无人。
notification-no-kick-game-started = 游戏开始后，只能踢出观众和失败的玩家。
notification-admin-clear-spawn = 只有管理员才能清除重生点。
notification-spawn-occupied = 您无法占据与其他玩家相同的重生点。
notification-spawn-locked = 重生点已锁定到另一个玩家插槽。
notification-admin-lobby-info = 只有主持人可以设置大厅信息。
notification-invalid-lobby-info = 发送了无效的大厅信息。
notification-player-color-terrain = 颜色已调整，以减少与地形的相似性。
notification-player-color-player = 颜色已调整，以减少与另一名玩家的相似性。
notification-invalid-player-color = 无法确定有效的玩家颜色。已选择随机颜色。
notification-invalid-error-code = 无法解析错误消息。
notification-master-server-connected = 已建立与主服务器的通信。
notification-master-server-error = “与主服务器的通信失败。”
notification-game-offline = 游戏未在线宣传。
notification-no-port-forward = 服务器端口无法从互联网访问。
notification-blacklisted-server-name = 服务器名称包含黑名单中的词汇。
notification-requires-authentication = 服务器要求玩家拥有OpenRA论坛帐户。
notification-no-permission-to-join = 您没有权限加入此服务器。
notification-slot-closed = 您的插槽已被主持人关闭。

## ServerOrders, UnitOrders
notification-joined = { $player }已加入游戏。
notification-lobby-disconnected = { $player }已离开。

## UnitOrders
notification-game-has-started = 游戏已开始。
notification-game-saved = 游戏已保存。
notification-game-paused = 游戏已被{ $player }暂停
notification-game-unpaused = 游戏已被{ $player }取消暂停

## Server
notification-game-started = 游戏开始

## PlayerMessageTracker
notification-chat-temp-disabled =
    { $remaining ->
        [one] 聊天已禁用。请 { $remaining } 秒后再试。
       *[other] 聊天已禁用。请{ $remaining }秒后再试。
    }

## VoteKickTracker
notification-unable-to-start-a-vote = 无法开始投票。
notification-insufficient-votes-to-kick = 投票踢出玩家 { $kickee } 的票数不足。
notification-kick-already-voted = 您已经投过票。
notification-vote-kick-started = 玩家 { $kicker } 已发起投票以踢出玩家 { $kickee } 。
notification-vote-kick-in-progress = { $percentage }%的玩家已投票踢出玩家 { $kickee }。
notification-vote-kick-ended = 投票踢出玩家 { $kickee } 失败。

## ActorEditLogic
label-duplicate-actor-id = 重复的Actor ID
label-actor-id = 输入Actor ID
label-actor-owner = 所有者

## ActorSelectorLogic
label-actor-type = 类型：{ $actorType }

## CommonSelectorLogic
options-common-selector =
    .search-results = 搜索结果
    .all = 全部
    .multiple = 多个
    .none = 无

## SaveMapLogic
label-unpacked-map = 未打包

dialog-save-map-failed =
    .title = 保存地图失败
    .prompt = 查看debug.log以获取详细信息。
    .confirm = 确定

dialog-overwrite-map-failed =
    .title = 警告
    .prompt = 保存将覆盖
    一个已存在的地图。
    .confirm = 保存

dialog-overwrite-map-outside-edit =
    .title = 警告
    .prompt = 地图已在编辑器外部编辑。
    保存可能会覆盖进度
    .confirm = 保存

notification-save-current-map = 已保存当前地图。


## GameInfoLogic

menu-game-info =
    .objectives = Objectives
    .briefing = Briefing
    .options = Options
    .debug = Debug
    .chat = Chat

## GameInfoObjectivesLogic, GameInfoStatsLogic
label-mission-in-progress = In progress
label-mission-accomplished = Accomplished
label-mission-failed = Failed

## GameInfoStatsLogic
label-client-state-disconnected = 已断开连接
label-mute-player = 静音此玩家
label-unmute-player = 取消静音此玩家
button-kick-player = 踢出此玩家
button-vote-kick-player = 投票踢出此玩家

dialog-kick =
    .title = 踢出 {$player}？
    .prompt = 此玩家将无法重新加入游戏。
    .confirm = 踢出

dialog-vote-kick =
    .title = 投票踢出 { $player} ？
    .prompt = 此玩家将无法重新加入游戏。
    .prompt-break-bots =
    { $bots ->
        [one] 踢出游戏管理员也将踢出 1 个机器人。
        *[other] 踢出游戏管理员也将踢出 { $bots } 个机器人。
    }
    .vote-start = 开始投票
    .vote-for = 赞成
    .vote-against = 反对
    .vote-cancel = 弃权

notification-vote-kick-disabled = 此服务器上禁用了投票踢出功能。

## GameTimerLogic
label-paused = 已暂停
label-max-speed = 最大速度
label-replay-speed = { $percentage }% 速度
label-replay-complete = { $percentage }% 完成

## LobbyLogic, InGameChatLogic
label-chat-disabled = 聊天已禁用
label-chat-availability =
    { $seconds ->
        [one] 聊天将在 { $seconds } 秒后可用...
        *[other] 聊天将在 { $seconds } 秒后可用...
    }

## IngameMenuLogic
menu-ingame =
    .leave = 离开
    .abort = 放弃任务
    .restart = 重新开始
    .surrender = 投降
    .load-game = 加载游戏
    .save-game = 保存游戏
    .music = 音乐
    .settings = 设置
    .return-to-map = 返回地图
    .resume = 继续
    .save-map = 保存地图
    .exit-map = 退出地图编辑器

dialog-leave-mission =
    .title = 离开任务
    .prompt = 离开游戏并返回菜单？
    .confirm = 离开
    .cancel = 留下

dialog-restart-mission =
    .title = 重新开始
    .prompt = 您确定要重新开始吗？
    .confirm = 重新开始
    .cancel = 留下

dialog-surrender =
    .title = 投降
    .prompt = 您确定要投降吗？
    .confirm = 投降
    .cancel = 留下

dialog-error-max-player =
    .title = 错误：玩家人数超限
    .prompt = 定义的玩家过多（({ $players }/{ $max })。
    .confirm = 返回

dialog-exit-map-editor =
    .title = 退出地图编辑器
    .prompt-unsaved = 退出并丢失所有未保存的更改？
    .prompt-deleted = 地图可能已在编辑器外部被删除。
    .confirm-anyway = 无论如何退出
    .confirm = 退出

dialog-play-map-warning =
    .title = 警告
    .prompt = 地图可能已被删除或存在阻止其加载的错误。
    .cancel = 确定

dialog-exit-to-map-editor =
    .title = 离开任务
    .prompt = 离开游戏并返回编辑器？
    .confirm = 返回编辑器
    .cancel = 留下

## IngamePowerBarLogic
## IngamePowerCounterLogic
label-power-usage = 电量使用：{ $usage }/{ $capacity }
label-infinite-power = 无限

## IngameSiloBarLogic
## IngameCashCounterLogic
label-silo-usage = 筒仓使用：{ $usage }/{ $capacity }

## ObserverShroudSelectorLogic
options-shroud-selector =
    .all-players = 所有玩家
    .disable-shroud = 禁用迷雾
    .other = 其他

## ObserverStatsLogic
options-observer-stats =
    .none = 信息：无
    .basic = 基本
    .economy = 经济
    .production = 生产
    .support-powers = 支援力量
    .combat = 战斗
    .army = 军队
    .earnings-graph = 收入（图表）
    .army-graph = 军队（图表）

## WorldTooltipLogic
label-unrevealed-terrain = 未探索的地形

## DownloadPackageLogic
label-downloading = 正在下载 { $title }
label-fetching-mirror-list = 正在获取镜像列表...
label-downloading-from = 正在从 { $host } 下载 { $received } { $suffix }
label-downloading-from-progress = 正在从 { $host }下载 { $received } / { $total } { $suffix } ({ $progress }%)
label-unknown-host = 未知主机
label-verifying-archive = 正在验证归档文件...
label-archive-validation-failed = 归档文件验证失败
label-extracting-archive = 正在提取...
label-extracting-archive-entry = 正在提取 { $entry }
label-archive-extraction-failed = 归档文件提取失败
label-mirror-selection-failed = 在线镜像不可用。请从原始光盘安装。

## InstallFromSourceLogic
label-detecting-sources = 正在检测驱动器
label-checking-sources = 正在检查源
label-searching-source-for = 正在搜索 { $title }
label-content-package-installation = 选择要安装的内容包：
label-game-sources = 游戏源
label-digital-installs = 数字安装
label-game-content-not-found = 未找到游戏内容
label-alternative-content-sources = 请插入或安装以下内容源之一：
label-installing-content = 正在安装内容
label-copying-filename = 正在复制 { $filename }
label-copying-filename-progress = 正在复制 { $filename }({ $progress }%)
label-installation-failed = 安装失败
label-check-install-log = 详情请参考日志目录中的 install.log。
label-extracting-filename = 正在提取 { $filename }
label-extracting-filename-progress = 正在提取 { $filename }({ $progress }%)

## ModContentLogic
button-manual-install = 手动安装

## KickClientLogic
dialog-kick-client =
    .prompt = 踢出 { $player }?

## KickSpectatorsLogic
dialog-kick-spectators =
    .prompt =
    { $count ->
        [one] 您确定要踢出一个观众吗？
       *[other] 您确定要踢出 { $count }  个观众吗？
    }

## LobbyLogic
options-slot-admin =
    .add-bots = 添加
    .remove-bots = 移除
    .configure-bots = 配置机器人
    .teams-count = { $count } 队
    .humans-vs-bots = 人类对战机器人
    .free-for-all = 自由混战
    .configure-teams = 配置队伍

## LobbyLogic, InGameChatLogic
button-general-chat = 全局
button-team-chat = 队伍

## LobbyOptionsLogic, MissionBrowserLogic
label-not-available = 不可用

## LobbyUtils
options-lobby-slot =
    .slot = 槽位
    .open = 开放
    .closed = 关闭
    .bots = 机器人
    .bots-disabled = 机器人已禁用

## MapPreviewLogic
label-connecting = 正在连接...
label-downloading-map = 正在下载 { $size } kB
label-downloading-map-progress = 正在下载 { $size } kB（{ $progress }%）
button-retry-install = 重试安装
button-retry-search = 重试搜索

## also MapChooserLogic
label-created-by = 由 { $author } 创建

## SpawnSelectorTooltipLogic
label-disabled-spawn = 禁用的出生点
label-available-spawn = 可用的出生点

## DisplaySettingsLogic
options-camera =
    .close = 关闭
    .medium = 中等
    .far = 远
    .furthest = 最远

options-display-mode =
    .windowed = 窗口模式
    .legacy-fullscreen = 全屏（旧版）
    .fullscreen = 全屏

label-video-display-index = 显示 { $number }

options-status-bars =
    .standard = 标准
    .show-on-damage = 受伤时显示
    .always-show = 始终显示

options-target-lines =
    .automatic = 自动
    .manual = 手动
    .disabled = 禁用

checkbox-frame-limiter = 启用帧率限制器（{ $fps } FPS）

## HotkeysSettingsLogic
label-original-notice = 默认是"{ $key }"
label-duplicate-notice = 这在 { $context }  上下文中已用于"{ $key }"

## InputSettingsLogic
options-mouse-scroll-type =
    .disabled = 禁用
    .standard = 标准
    .inverted = 反转
    .joystick = 摇杆

## InputSettingsLogic, IntroductionPromptLogic
options-control-scheme =
    .classic = 经典
    .modern = 现代

options-zoom-modifier =
    .alt = Alt
    .ctrl = Ctrl
    .meta = Meta
    .shift = Shift
    .none = 无

## SettingsLogic
dialog-settings-save =
    .title = 需要重启
    .prompt = 部分更改将在游戏重启后生效。
    .cancel = 继续

dialog-settings-restart =
    .title = 现在重启？
    .prompt = 部分更改将在游戏重启后生效。现在重启吗？
    .confirm = 现在重启
    .cancel = 稍后重启

dialog-settings-reset =
    .title = 重置 { $panel }
    .prompt = 您确定要重置此面板中的所有设置吗？
    .confirm = 重置
    .cancel = 取消

## AssetBrowserLogic
label-all-packages = 所有包
label-length-in-seconds = { $length } 秒

## ConnectionLogic
label-connecting-to-endpoint = 正在连接到 { $endpoint }...
label-could-not-connect-to-target = 无法连接到 { $target }
label-unknown-error = 未知错误
label-password-required = 需要密码
label-connection-failed = 连接失败
notification-mod-switch-failed = 切换模组失败。

## GameSaveBrowserLogic
dialog-rename-save =
    .title = 重命名存档
    .prompt = 输入新的文件名：
    .confirm = 重命名

dialog-delete-save =
    .title = 删除选定的游戏存档？
    .prompt = 删除 '{ $save }'
    .confirm = 删除

dialog-delete-all-saves =
    .title = 删除所有游戏存档？
    .prompt =
    { $count ->
        [one] 删除 { count } 个存档。
        *[other] 删除 { $count } 个存档。
    }
    .confirm = 删除全部

notification-save-deletion-failed = 无法删除存档文件 '{ $savePath }'。请查看日志以获取详细信息。

dialog-overwrite-save =
    .title = 覆盖已保存的游戏？
    .prompt = 覆盖 { $file }？
    .confirm = 覆盖

## MainMenuLogic
label-loading-news = 正在加载新闻
label-news-retrieval-failed = 无法检索新闻：{ $message }
label-news-parsing-failed = 无法解析新闻：{ $message }
label-author-datetime = 由 { $author } 在 { $datetime } 发布

## MapChooserLogic
label-all-maps = 所有地图
label-no-matches = 没有匹配项
label-player-count =
    { $players ->
        [one] { $players } 位玩家
        *[other] { $players } 位玩家
    }
label-map-size-huge = （巨大）
label-map-size-large = （大）
label-map-size-medium = （中）
label-map-size-small = （小）
label-map-searching-count =
    { $count ->
        [one] 正在 OpenRA 资源中心搜索 { $count } 个地图...
        *[other] 正在 OpenRA 资源中心搜索 { $count } 个地图...
    }
label-map-unavailable-count =
    { $count ->
        [one] 在 OpenRA 资源中心未找到 { $count } 个地图
        *[other] 在 OpenRA 资源中心未找到 { $count } 个地图
    }

notification-map-deletion-failed = 无法删除地图 '{ $map }'。请查看 debug.log 文件以获取详细信息。

dialog-delete-map =
    .title = 删除地图
    .prompt = 删除地图 '{ $title }'？
    .confirm = 删除

dialog-delete-all-maps =
    .title = 删除地图
    .prompt = 删除此页面上的所有地图？
    .confirm = 删除

options-order-maps =
    .player-count = 玩家数
    .title = 标题
    .date = 日期
    .size = 大小

## MissionBrowserLogic
dialog-no-video =
    .title = 视频未安装
    .prompt = 可以在模组选择器的“管理内容”菜单中安装游戏视频。
    .cancel = 返回

dialog-cant-play-video =
    .title = 无法播放视频
    .prompt = 视频播放时出现问题。
    .cancel = 返回

## MusicPlayerLogic
label-sound-muted = 已在设置中静音。
label-no-song-playing = 当前无歌曲播放

## MuteHotkeyLogic
label-audio-muted = 已静音。
label-audio-unmuted = 已取消静音。

## PlayerProfileLogic
label-loading-player-profile = 正在加载玩家档案...
label-loading-player-profile-failed = 加载玩家档案失败。

## ProductionTooltipLogic
label-requires = 需要 { $prequisites }

## ReplayBrowserLogic
label-duration = 时长：{ $time }

options-replay-type =
    .singleplayer = 单人
    .multiplayer = 多人

options-winstate =
    .victory = 胜利
    .defeat = 失败

options-replay-date =
    .today = 今天
    .last-week = 过去 7 天
    .last-fortnight = 过去 14 天
    .last-month = 过去 30 天

options-replay-duration =
    .very-short = 少于 5 分钟
    .short = 短（10 分钟）
    .medium = 中（30 分钟）
    .long = 长（60+ 分钟）

dialog-rename-replay =
    .title = 重命名回放
    .prompt = 输入新的文件名：
    .confirm = 重命名

dialog-delete-replay =
    .title = 删除选定的回放？
    .prompt = 删除回放 { $replay }？
    .confirm = 删除

dialog-delete-all-replays =
    .title = 删除所有选定的回放？
    .prompt =
    { $count ->
        [one] 删除 { $count } 个回放。
        *[other] 删除 { $count } 个回放。
    }
    .confirm = 删除全部

notification-replay-deletion-failed = 无法删除回放文件 '{ $file }'。请查看 debug.log 文件以获取详细信息。

## ReplayUtils
-incompatible-replay-recorded = 录制时使用的

dialog-incompatible-replay =
    .title = 不兼容的回放
    .prompt = 无法读取回放元数据。
    .confirm = 确定
    .prompt-unknown-version = { -incompatible-replay-recorded } 未知版本。
    .prompt-unknown-mod = { -incompatible-replay-recorded } 未知模组。
    .prompt-unavailable-mod = { -incompatible-replay-recorded } 不可用的模组：{ $mod }。
    .prompt-incompatible-version = { -incompatible-replay-recorded } 不兼容的版本： { $version }。
    .prompt-unavailable-map = { -incompatible-replay-recorded } 不可用的地图：
    { $map }。
# SelectUnitsByTypeHotkeyLogic
nothing-selected = 未选择任何单位。

## SelectUnitsByTypeHotkeyLogic, SelectAllUnitsHotkeyLogic
selected-units-across-screen =
    { $units ->
        [one] 在屏幕上选择了一个单位。
        *[other] 在屏幕上选择了 { $units } 个单位。
    }

selected-units-across-map =
    { $units ->
        [one] 在地图上选择了一个单位。
        *[other] 在地图上选择了 { $units } 个单位。
    }
## ServerCreationLogic
label-internet-server-nat-A = 互联网服务器（UPnP/NAT-PMP
label-internet-server-nat-B-enabled = 已启用
label-internet-server-nat-B-not-supported = 不支持
label-internet-server-nat-B-disabled = 已禁用
label-internet-server-nat-C = )：

label-local-server = 本地服务器：

dialog-server-creation-failed =
    .prompt = 无法在端口 { $port } 上监听
    .prompt-port-used = 检查端口是否已被占用。
    .prompt-error = 错误为：“{ $message }”（{ $code }）
    .title = 服务器创建失败
    .cancel = 返回

## ServerListLogic
label-players-online-count =
    { $players ->
        [one] { $players } 名玩家在线
        *[other] { $players } 名玩家在线
    }

label-search-status-failed = 查询服务器列表失败。
label-search-status-no-games = 未找到游戏。尝试更改过滤器。
label-no-server-selected = 未选择服务器

label-map-status-searching = 正在搜索...
label-map-classification-unknown = 未知地图

label-players-count =
    { $players ->
        [0] 无玩家
        [one] 一名玩家
        *[other] { $players } 名玩家
    }

label-bots-count =
    { $bots ->
        [0] 无机器人
        [one] 一个机器人
        *[other] { $bots } 个机器人
    }
## ServerListLogic, ReplayBrowserLogic, ObserverShroudSelectorLogic
label-players = 玩家

## ServerListLogic, GameInfoStatsLogic
label-spectators = 观众
label-spectators-count =
    { $spectators ->
        [0] 无观众
        [one] 一名观众
       *[other] { $spectators } 名观众
    }

## ServerlistLogic, GameInfoStatsLogic, ObserverShroudSelectorLogic, SpawnSelectorTooltipLogic, ReplayBrowserLogic
label-team-name = 队伍 { $team }
label-no-team = 无队伍

label-playing = 游戏中
label-waiting = 等待中
label-other-players-count =
    { $players ->
        [one] 另一名玩家
        *[other] { $players } 名其他玩家
    }

label-in-progress-for =
    { $minutes ->
    [0] 进行中，不到一分钟。
    [one] 进行中，已 { $minutes } 分钟。
    *[other] 进行中，已 { $minutes } 分钟。
    }

label-password-protected = 密码保护
label-waiting-for-players = 等待玩家
label-server-shutting-down = 服务器正在关闭
label-unknown-server-state = 未知服务器状态

## Game
notification-saved-screenshot = 已保存截图  { $filename }

## ChatCommands
notification-invalid-command = { $name } 不是有效的命令。

## DebugVisualizationCommands
description-combat-geometry = 切换战斗几何图形覆盖层。
description-render-geometry = 切换渲染几何图形覆盖层。
description-screen-map-overlay = 切换屏幕地图覆盖层。
description-depth-buffer = 切换深度缓冲区覆盖层。
description-actor-tags-overlay = 切换角色标签覆盖层。

## DevCommands
notification-cheats-disabled = 作弊已禁用。
notification-invalid-cash-amount = 无效的现金数量。
description-toggle-visibility = 切换可见性检查和小地图。
description-give-cash = 给予默认或指定数量的金钱。
description-give-cash-all = 给予所有玩家和AI默认或指定数量的金钱。
description-instant-building = 切换即时建造。
description-build-anywhere = 切换在任何地方建造的能力。
description-unlimited-power = 切换无限电力。
description-enable-tech = 切换建造所有内容的能力。
description-fast-charge = 切换几乎即时的支援电力充电。
description-dev-cheat-all = 切换所有作弊并给您一些现金作为补偿。
description-dev-crash = 崩溃游戏。
description-levelup-actor = 为选定的角色添加指定数量的等级。
description-player-experience = 为选定角色的所有者添加指定数量的玩家经验。
description-power-outage = 导致选定角色的所有者有5秒的停电。
description-kill-selected-actors = 杀死选定的角色。
description-dispose-selected-actors = 销毁选定的角色。

## HelpCommands
notification-available-commands = 以下是可用的命令：
description-no-description = 没有可用的描述。
description-help-description = 提供有关各种命令的有用信息

## PlayerCommands
description-pause-description = 暂停或取消暂停游戏
description-surrender-description = 自我毁灭一切并输掉游戏

## DeveloperMode
notification-cheat-used = 作弊使用：{ $cheat } 由 { $player }{ $suffix }

## CustomTerrainDebugOverlay
description-custom-terrain-debug-overlay = 切换自定义地形调试覆盖层。

## CellTriggerOverlay
description-cell-triggers-overlay = 切换脚本触发器覆盖层。

## ExitsDebugOverlay
description-exits-overlay = 显示工厂的出口。

## HierarchicalPathFinderOverlay
description-hpf-debug-overlay = 切换分层路径查找器覆盖层。

## PathFinderOverlay
description-path-debug-overlay = 切换路径搜索的可视化。

## TerrainGeometryOverlay
description-terrain-geometry-overlay = 切换地形几何图形覆盖层。

## MapOptions, MissionBrowserLogic
options-game-speed =
    .slowest = 最慢
    .slower = 较慢
    .normal = 正常
    .fast = 较快
    .faster = 更快
    .fastest = 最快

## TimeLimitManager
options-time-limit =
    .no-limit = 无限制
    .options =
        { $minutes ->
            [one] { $minutes } 分钟
            *[other] { $minutes } 分钟
        }
notification-time-limit-expired = 时间限制已到期。

## EditorActorBrush
notification-added-actor = 添加了 { $name } ({ $id })

## EditorCopyPasteBrush
notification-copied-tiles =
    { $amount ->
    [one] 复制了一个瓦片
    *[other] 复制了 { $amount } 个瓦片
    }

## EditorDefaultBrush
notification-selected-area = 选择了区域 { $x },{ $y } ({ $width },{ $height })
notification-selected-actor = 选择了角色 { $id }
notification-cleared-selection = 清除了选择
notification-removed-actor = 移除了 { $name } ({ $id })
notification-removed-resource = 移除了 { $type }
notification-moved-actor = 将 { $id } 从 { $x1 },{ $y1 } 移动到 { $x2 },{ $y2 }

## EditorResourceBrush
notification-added-resource =
    { $amount ->
        [one] 添加了一个 { $type } 单元
        *[other] 添加了 { $amount } 个 { $type } 单元
    }

## EditorTileBrush
notification-added-tile = 添加了瓦片 { $id }
notification-filled-tile = 用瓦片 { $id } 填充

## EditorMarkerLayerBrush
notification-added-marker-tiles =
    { $amount ->
        [one] 添加了一个类型为 { $type } 的标记瓦片
        *[other] 添加了 { $amount } 个类型为 { $type } 的标记瓦片
    }
notification-removed-marker-tiles =
    { $amount ->
        [one] 移除了一个标记瓦片
        *[other] 移除了 { $amount } 个标记瓦片
    }
notification-cleared-selected-marker-tiles = 清除了 { $amount } 个类型为 { $type } 的标记瓦片
notification-cleared-all-marker-tiles = 清除了 { $amount } 个标记瓦片

## EditorActionManager
notification-opened = 已打开

## MapOverlaysLogic
mirror-mode =
    .none = 无
    .flip = 翻转
    .rotate = 旋转

## ActorEditLogic
notification-edited-actor = 编辑了 { $name } ({ $id })
notification-edited-actor-id = 编辑了 { $name } ({ $old-id }->{ $new-id })

## ConquestVictoryConditions, StrategicVictoryConditions
notification-player-is-victorious = { $player } 胜利了。
notification-player-is-defeated = { $player } 失败了。

## OrderManager
notification-desync-compare-logs = 在第 { $frame } 帧出现不同步。
    与其他玩家比较 syncreport.log。

## SupportPowerTimerWidget
support-power-timer = { $player }的 { $support-power }: { $time }

## WidgetUtils
label-win-state-won = 获胜
label-win-state-lost = 失败

## Player
enumerated-bot-name =
    { $name } { $number ->
       *[zero] {""}
        [other] { $number }
    }
