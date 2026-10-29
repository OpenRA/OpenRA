## 按钮
button-cancel = 取消
button-retry = 重试
button-back = 返回
button-continue = 继续
button-quit = 退出

## 服务器命令
notification-custom-rules = 此地图包含自定义规则。游戏体验可能会改变。
notification-two-humans-required = 此服务器需要至少两名人类玩家才能开始比赛。
notification-unknown-server-command = 未知服务器命令：{ $command }。
notification-admin-start-game = 只有主机可以开始游戏。
notification-no-start-until-required-slots-full = 在必需的槽位满之前无法开始游戏。
notification-no-start-without-players = 没有玩家无法开始游戏。
notification-insufficient-enabled-spawn-points = 在启用更多出生点之前无法开始游戏。
notification-malformed-command = 错误格式的 { $command } 命令。
notification-state-unchanged-ready = 标记为准备状态时无法更改状态。
notification-invalid-faction-selected = 无效的派系选择：{ $faction }。
notification-state-unchanged-game-started = 游戏开始后状态无法更改（{ $command }）。
notification-requires-host = 只有主机可以这样做。
notification-invalid-bot-slot = 无法将机器人添加到其他客户端的槽位。
notification-invalid-bot-type = 无效的机器人类型。
notification-admin-change-map = 只有主机可以更改地图。
notification-player-disconnected = { $player } 已断开连接。
notification-team-player-disconnected = { $player } (队伍 { $team }) 已断开连接。
notification-observer-disconnected = { $player } (观众) 已断开连接。
notification-unknown-map = 服务器上未找到地图。
notification-searching-map = 正在资源中心搜索地图...
notification-admin-change-configuration = 只有主机可以更改配置。
notification-changed-map = { $player } 将地图更改为 { $map }。
notification-you-were-kicked = 您已被从服务器踢出。
notification-admin-kicked = { $admin } 从服务器踢出了 { $player }。
notification-kicked = { $player } 被从服务器踢出。
notification-temp-ban = { $admin } 临时禁止 { $player } 从服务器。
notification-admin-transfer-admin = 只有管理员可以将管理员权限转移给其他玩家。
notification-admin-move-spectators = 只有主机可以将玩家移至观众。
notification-empty-slot = 此槽位无人。
notification-move-spectators = { $admin } 将 { $player } 移至观众。
notification-nick-changed = { $player } 现在称为 { $name }。
notification-player-dropped = 玩家在超时后被断开连接。
notification-connection-problems = { $player } 正在经历连接问题。
notification-timeout-dropped = { $player } 在超时后被断开连接。
notification-timeout-dropped-in =
    { $timeout ->
        [one] { $player } 将在 { $timeout } 秒后被断开连接。
       *[other] { $player } 将在 { $timeout } 秒后被断开连接。
    }
notification-error-game-started = 游戏已经开始了。
notification-requires-password = 服务器需要密码。
notification-incorrect-password = 密码错误。
notification-incompatible-mod = 服务器正在运行不兼容的模组。
notification-incompatible-version = 服务器正在运行不兼容的版本。
notification-incompatible-protocol = 服务器正在运行不兼容的协议。
notification-you-were-banned = 您已被禁止从服务器访问。
notification-you-were-temp-banned = 您已被临时禁止从服务器访问。
notification-game-full = 游戏已满。
notification-new-admin = { $player } 现在是管理员。
notification-invalid-configuration-command = 无效的配置命令。
notification-admin-option = 只有主机可以设置该选项。
notification-error-number-teams = 无法解析队伍数量：{ $raw }。
notification-admin-kick = 只有主机可以踢出玩家。
notification-kick-self = 主机无法踢自己。
notification-kick-none = 此槽位无人。
notification-no-kick-game-started = 游戏开始后只能踢出观众和失败的玩家。
notification-admin-clear-spawn = 只有管理员可以清除出生点。
notification-spawn-occupied = 您无法占用与其他玩家相同的出生点。
notification-spawn-locked = 出生点已被锁定到其他玩家槽位。
notification-admin-lobby-info = 只有主机可以设置大厅信息。
notification-invalid-lobby-info = 发送了无效的大厅信息。
notification-player-color-terrain = 颜色已调整为与地形不太相似。
notification-player-color-player = 颜色已调整为与另一玩家不太相似。
notification-invalid-player-color = 无法确定有效的玩家颜色。已选择随机颜色。
notification-invalid-error-code = 解析错误消息失败。
notification-master-server-connected = 主服务器通信已建立。
notification-master-server-error = 主服务器通信失败。
notification-game-offline = 游戏未在线广播。
notification-no-port-forward = 服务器端口无法从互联网访问。
notification-blacklisted-server-name = 服务器名称包含黑名单词汇。
notification-requires-authentication = 服务器要求玩家拥有OpenRA论坛账户。
notification-no-permission-to-join = 您没有权限加入此服务器。
notification-slot-closed = 您的槽位被主机关闭了。

## 服务器订单，单位订单
notification-joined = { $player } 已加入游戏。
notification-lobby-disconnected = { $player } 已离开。

## 单位订单
notification-game-has-started = 游戏已开始。
notification-game-paused = 游戏已被 { $player } 暂停。
notification-game-unpaused = 游戏已被 { $player } 取消暂停。

## 服务器
notification-game-started = 游戏开始。

## 玩家消息追踪器
notification-chat-temp-disabled =
    { $remaining ->
        [one] 聊天已禁用。请在 { $remaining } 秒后重试。
       *[other] 聊天已禁用。请在 { $remaining } 秒后重试。
    }

## 投票踢人追踪器
notification-unable-to-start-a-vote = 无法启动投票。
notification-insufficient-votes-to-kick = 踢出玩家 { $kickee } 的票数不足。
notification-kick-already-voted = 您已经投票。
notification-vote-kick-started = 玩家 { $kicker } 已启动投票踢出玩家 { $kickee }。
notification-vote-kick-in-progress = { $percentage }% 的玩家已投票踢出玩家 { $kickee }。
notification-vote-kick-ended = 踢出玩家 { $kickee } 的投票失败。

## 单位编辑逻辑
label-duplicate-actor-id = 重复的单位ID
label-actor-id = 输入单位ID
label-actor-owner = 拥有者

## 单位选择器逻辑
label-actor-type = 类型：{ $actorType }

## 共同选择器逻辑
options-common-selector =
    .search-results = 搜索结果
    .all = 全部
    .multiple = 多个
    .none = 无

## 保存地图逻辑
label-unpacked-map = 未打包

dialog-save-map-failed =
    .title = 保存地图失败
    .prompt = 详情请查看 debug.log。
    .confirm = 确定

dialog-overwrite-map-failed =
    .title = 警告
    .prompt = 保存将覆盖
    已存在的地图。
    .confirm = 保存

dialog-overwrite-map-outside-edit =
    .title = 警告
    .prompt = 地图已在编辑器外编辑。
    保存可能覆盖进度。
    .confirm = 保存

notification-save-current-map = 已保存当前地图。

## 游戏信息逻辑
menu-game-info =
    .objectives = 目标
    .briefing = 简述
    .options = 选项
    .debug = 调试
    .chat = 聊天

## 游戏信息目标逻辑，游戏信息统计逻辑
label-mission-in-progress = 进行中
label-mission-accomplished = 完成
label-mission-failed = 失败

## 游戏信息统计逻辑
label-mute-player = 静音此玩家
label-unmute-player = 取消静音此玩家
button-kick-player = 踢出此玩家
button-vote-kick-player = 投票踢出此玩家

dialog-kick =
    .title = 踢出 { $player }?
    .prompt = 此玩家将无法重新加入游戏。
    .confirm = 踢出

dialog-vote-kick =
    .title = 投票踢出 { $player }?
    .prompt = 此玩家将无法重新加入游戏。
    .prompt-break-bots =
    { $bots ->
        [one] 踢出游戏管理员的同时也将踢出 1 个机器人。
       *[other] 踢出游戏管理员的同时也将踢出 { $bots } 个机器人。
    }
    .vote-start = 启动投票
    .vote-for = 投票赞成
    .vote-against = 投票反对
    .vote-cancel = 放弃投票

notification-vote-kick-disabled = 此服务器上禁用了投票踢出。

## 游戏计时器逻辑
label-paused = 已暂停
label-max-speed = 最大速度
label-replay-speed = { $percentage }% 速度
label-replay-complete = { $percentage }% 完成

## 大厅逻辑，游戏中聊天逻辑
label-chat-disabled = 聊天已禁用
label-chat-availability =
    { $seconds ->
        [one] 聊天在 { $seconds } 秒后可用...
       *[other] 聊天在 { $seconds } 秒后可用...
    }

## 大厅逻辑，服务器列表逻辑
label-bot-player = AI 玩家

## 大厅逻辑
notification-lobby-option = { $name }: { $value }。
notification-lobby-option-changed = { $name } 已更改为 { $value }。
notification-map-bots-disabled = 此地图上的机器人已被禁用。

## 游戏内菜单逻辑
menu-ingame =
    .leave = 离开
    .abort = 中止任务
    .restart = 重新开始
    .surrender = 投降
    .load-game = 加载游戏
    .save-game = 保存游戏
    .music = 音乐
    .settings = 设置
    .return-to-map = 返回地图
    .resume = 恢复
    .save-map = 保存地图
    .exit-map = 退出地图编辑器

dialog-leave-mission =
    .title = 离开任务
    .prompt = 离开此游戏并返回菜单？
    .confirm = 离开
    .cancel = 停留

dialog-restart-mission =
    .title = 重新开始
    .prompt = 您确定要重新开始？
    .confirm = 重新开始
    .cancel = 停留

dialog-surrender =
    .title = 投降
    .prompt = 您确定要投降？
    .confirm = 投降
    .cancel = 停留

dialog-error-max-player =
    .title = 错误：最大玩家数超出
    .prompt = 已定义了过多玩家（{ $players }/{ $max }）。
    .confirm = 返回

dialog-exit-map-editor =
    .title = 退出地图编辑器
    .prompt-unsaved = 退出并丢失所有未保存的更改？
    .prompt-deleted = 地图可能已在编辑器外被删除
    .confirm-anyway = 无论如何都退出
    .confirm = 退出

dialog-play-map-warning =
    .title = 警告
    .prompt = 地图可能已被删除或包含
    错误导致无法加载。
    .cancel = 确定

dialog-exit-to-map-editor =
    .title = 离开任务
    .prompt = 离开此游戏并返回编辑器？
    .confirm = 回到编辑器
    .cancel = 停留

## 游戏内电源条逻辑
## 游戏内电源计数器逻辑
label-power-usage = 电力消耗：{ $usage }/{ $capacity }
label-infinite-power = 无限电力

## 游戏内储藏室条逻辑
## 游戏内现金计数器逻辑
label-silo-usage = 储藏室使用情况：{ $usage }/{ $capacity }

## 观察者迷雾选择器逻辑
options-shroud-selector =
    .all-players = 所有玩家
    .disable-shroud = 禁用迷雾
    .other = 其他

## 观察者统计逻辑
options-observer-stats =
    .none = 信息：无
    .basic = 基础
    .economy = 经济
    .production = 生产
    .support-powers = 支援能力
    .combat = 战斗
    .army = 军队
    .earnings-graph = 收入(图表)
    .army-graph = 军队(图表)

## 世界小工具提示逻辑
label-unrevealed-terrain = 未揭示地形

## 踢出客户端逻辑
dialog-kick-client =
    .prompt = 踢出 { $player }?

## 踢出观众逻辑
dialog-kick-spectators =
    .prompt =
    { $count ->
        [one] 您确定要踢出一位观众吗？
       *[other] 您确定要踢出 { $count } 位观众吗？
    }

## 大厅逻辑
options-slot-admin =
    .add-bots = 添加
    .remove-bots = 移除
    .configure-bots = 配置机器人
    .teams-count = { $count } 队
    .humans-vs-bots = 人类对机器人
    .free-for-all = 自由对战
    .configure-teams = 配置队伍

## 大厅逻辑，游戏中聊天逻辑
button-general-chat = 全体
button-team-chat = 队伍

## 大厅选项逻辑，任务浏览器逻辑
label-not-available = 不可用

## 大厅工具
options-lobby-slot =
    .slot = 槽位
    .open = 开放
    .closed = 关闭
    .bots = 机器人
    .bots-disabled = 机器人已禁用

## 地图预览逻辑
label-connecting = 正在连接...
label-downloading-map = 正在下载 { $size } kB
label-downloading-map-progress = 正在下载 { $size } kB ({ $progress }%)
button-retry-install = 重试安装
button-retry-search = 重试搜索
## 地图选择器逻辑也适用
label-created-by = 由 { $author } 创建

## 出生点选择器提示逻辑
label-disabled-spawn = 禁用出生点
label-available-spawn = 可用出生点

## 显示设置逻辑
options-camera =
    .close = 近距离
    .medium = 中等距离
    .far = 远距离
    .furthest = 最远距离

options-display-mode =
    .windowed = 窗口模式
    .legacy-fullscreen = 全屏(传统)
    .fullscreen = 全屏

label-video-display-index = 显示器 { $number }

options-status-bars =
    .standard = 标准
    .show-on-damage = 受伤时显示
    .always-show = 总是显示

options-target-lines =
    .automatic = 自动
    .manual = 手动
    .disabled = 禁用

checkbox-frame-limiter = 启用帧率限制 ({ $fps } FPS)

## 热键设置逻辑
label-original-notice = 默认为 "{ $key }"
label-duplicate-notice = 这已在 { $context } 上下文中用作 "{ $key }"
hotkey-context-any = 任意

## 游戏玩法设置逻辑
auto-save-interval =
    .disabled = 禁用
    .options =
        { $seconds ->
            [one] 1 秒
           *[other] { $seconds } 秒
        }
    .minute-options =
        { $minutes ->
            [one] 1 分钟
           *[other] { $minutes } 分钟
        }

auto-save-max-file-number = { $saves } 个保存档

## 输入设置逻辑
options-mouse-scroll-type =
    .disabled = 禁用
    .standard = 标准
    .inverted = 反向
    .joystick = 操纵杆

## 输入设置逻辑，介绍提示逻辑
options-control-scheme =
    .classic = 经典
    .modern = 现代
    .otherrts = 其他RTS

## 设置逻辑
dialog-settings-save =
    .title = 需要重启
    .prompt = 某些更改在游戏重启前不会生效。
    .cancel = 继续

dialog-settings-restart =
    .title = 立即重启？
    .prompt = 某些更改在游戏重启前不会生效。立即重启？
    .confirm = 立即重启
    .cancel = 稍后重启

dialog-settings-reset =
    .title = 重置 { $panel }
    .prompt = 您确定要重置此面板中的所有设置吗？
    .confirm = 重置
    .cancel = 取消

## 资源浏览器逻辑
label-all-packages = 所有包
label-length-in-seconds = { $length } 秒

## 连接逻辑
label-connecting-to-endpoint = 正在连接到 { $endpoint }...
label-could-not-connect-to-target = 无法连接到 { $target }
label-unknown-error = 未知错误
label-password-required = 需要密码
label-connection-failed = 连接失败
notification-mod-switch-failed = 切换模组失败。

## 游戏保存浏览器逻辑
dialog-rename-save =
    .title = 重命名保存
    .prompt = 输入新文件名:
    .confirm = 重命名

dialog-delete-save =
    .title = 删除选定的游戏保存吗？
    .prompt = 删除 '{ $save }'。
    .confirm = 删除

dialog-delete-all-saves =
    .title = 删除所有游戏保存吗？
    .prompt =
    { $count ->
        [one] 删除 { $count } 个保存档。
       *[other] 删除 { $count } 个保存档。
    }
    .confirm = 删除全部

notification-save-deletion-failed = 删除保存文件 '{ $savePath }' 失败。详情请见日志。

dialog-overwrite-save =
    .title = 覆盖已保存的游戏吗？
    .prompt = 覆盖 { $file }?
    .confirm = 覆盖

## 主菜单逻辑
label-loading-news = 正在加载新闻
label-news-retrieval-failed = 无法获取新闻: { $message }
label-news-parsing-failed = 解析新闻失败: { $message }
label-author-datetime = 作者 { $author } 在 { $datetime } 发布

## 地图选择器逻辑
label-all-maps = 所有地图
label-no-matches = 无匹配项
label-player-count =
    { $players ->
        [one] { $players } 位玩家
       *[other] { $players } 位玩家
    }
label-map-size-huge = 巨大
label-map-size-large = 大型
label-map-size-medium = 中型
label-map-size-small = 小型
label-map-searching-count =
    { $count ->
        [one] 正在OpenRA资源中心搜索 { $count } 张地图...
       *[other] 正在OpenRA资源中心搜索 { $count } 张地图...
    }
label-map-unavailable-count =
    { $count ->
        [one] { $count } 张地图在OpenRA资源中心未找到
       *[other] { $count } 张地图在OpenRA资源中心未找到
    }

notification-map-deletion-failed = 删除地图 '{ $map }' 失败。详情请见debug.log文件。

dialog-delete-map =
    .title = 删除地图
    .prompt = 删除地图 '{ $title }'?
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

button-mapchooser-system-maps-tab = 官方地图
button-mapchooser-remote-maps-tab = 服务器地图
button-mapchooser-user-maps-tab = 自定义地图
button-mapchooser-generated-maps-tab = 生成地图

## 任务浏览器逻辑
dialog-no-video =
    .title = 未安装视频
    .prompt =
        可从"管理内容"菜单安装游戏视频。
    .cancel = 返回

dialog-cant-play-video =
    .title = 无法播放视频
    .prompt = 视频播放过程中出现错误。
    .cancel = 返回

## 音乐播放器逻辑
label-sound-muted = 音频已在设置中静音。
label-no-song-playing = 没有正在播放的歌曲

## 静音热键逻辑
label-audio-muted = 音频已静音。
label-audio-unmuted = 音频已取消静音。

## 玩家资料逻辑
label-loading-player-profile = 正在加载玩家资料...
label-loading-player-profile-failed = 加载玩家资料失败。

## 生产小工具提示逻辑，百科全书逻辑
label-requires = 需要 { $prerequisites }。
## 回放浏览器逻辑
label-duration = 持续时间: { $time }

options-replay-type =
    .singleplayer = 单人游戏
    .multiplayer = 多人游戏

options-winstate =
    .victory = 胜利
    .defeat = 失败

options-save-type =
    .autosave = 自动保存
    .manual = 手动保存

options-replay-date =
    .today = 今天
    .last-week = 最近7天
    .last-fortnight = 最近14天
    .last-month = 最近30天

options-replay-duration =
    .very-short = 少于5分钟
    .short = 短(10分钟)
    .medium = 中(30分钟)
    .long = 长(60+分钟)

dialog-rename-replay =
    .title = 重命名回放
    .prompt = 输入新文件名:
    .confirm = 重命名

dialog-delete-replay =
    .title = 删除选定的回放？
    .prompt = 删除回放 { $replay }?
    .confirm = 删除

dialog-delete-all-replays =
    .title = 删除所有选定的回放?
    .prompt =
    { $count ->
        [one] 删除 { $count } 个回放。
       *[other] 删除 { $count } 个回放。
    }
    .confirm = 全部删除

notification-replay-deletion-failed = 删除回放文件 '{ $file }' 失败。详情请见 debug.log 文件。

## 回放工具
-incompatible-replay-recorded = 回放录制时使用的

dialog-incompatible-replay =
    .title = 不兼容的回放
    .prompt = 无法读取回放元数据。
    .confirm = 确定
    .prompt-unknown-version = { -incompatible-replay-recorded }未知版本。
    .prompt-unknown-mod = { -incompatible-replay-recorded }未知模组。
    .prompt-unavailable-mod = { -incompatible-replay-recorded }无法使用的模组: { $mod }。
    .prompt-incompatible-version = { -incompatible-replay-recorded }不兼容的版本:
    { $version }。
    .prompt-unavailable-map = { -incompatible-replay-recorded }无法使用的地图:
    { $map }。

# 按类型选择单位热键逻辑
nothing-selected = 未选择任何内容。

## 按类型选择单位热键逻辑，选择所有单位热键逻辑
selected-units-across-screen =
    { $units ->
        [one] 选择屏幕上的一个单位。
       *[other] 选择屏幕上的 { $units } 个单位。
    }

selected-units-across-map =
    { $units ->
        [one] 选择地图上的一个单位。
       *[other] 选择地图上的 { $units } 个单位。
    }

## 服务器创建逻辑
label-internet-server-nat-A = 互联网服务器 (UPnP/NAT-PMP
label-internet-server-nat-B-enabled = 已启用
label-internet-server-nat-B-not-supported = 不支持
label-internet-server-nat-B-disabled = 已禁用
label-internet-server-nat-C = ):

label-local-server = 本地服务器:

dialog-server-creation-failed =
    .prompt = 无法在端口 { $port } 上监听。
    .prompt-port-used = 检查端口是否已经被使用。
    .prompt-error = 错误是: "{ $message }" ({ $code })。
    .title = 服务器创建失败
    .cancel = 返回

## 服务器列表逻辑
label-players-online-count =
    { $players ->
        [one] { $players } 位在线玩家
       *[other] { $players } 位在线玩家
    }

label-search-status-failed = 查询服务器列表失败。
label-search-status-no-games = 找不到游戏。尝试更改过滤器。
label-no-server-selected = 未选择服务器

label-map-status-searching = 正在搜索...
label-map-classification-unknown = 未知地图

label-players-count =
    { $players ->
        [0] 无玩家
        [one] 一位玩家
       *[other] { $players } 位玩家
    }

label-bots-count =
    { $bots ->
        [0] 无机器人
        [one] 一个机器人
       *[other] { $bots } 个机器人
    }

## 服务器列表逻辑，回放浏览器逻辑，观察者迷雾选择器逻辑
label-players = 玩家

## 服务器列表逻辑，游戏信息统计逻辑
label-spectators = 观众
label-spectators-count =
    { $spectators ->
        [0] 无观众
        [one] 一位观众
       *[other] { $spectators } 位观众
    }

## 服务器列表逻辑、游戏信息统计逻辑、观察者迷雾选择器逻辑、出生点选择器提示逻辑、回放浏览器逻辑
label-team-name = 队伍 { $team }
label-no-team = 无队伍

label-playing = 游戏中
label-waiting = 等待中

label-other-players-count =
    { $players ->
        [one] 一位其他玩家
       *[other] { $players } 位其他玩家
    }

label-in-progress-for =
    { $minutes ->
        [0] 任务进行少于一分钟。
        [one] 任务进行 { $minutes } 分钟。
       *[other] 任务进行 { $minutes } 分钟。
    }

label-password-protected = 密码保护
label-waiting-for-players = 等待玩家
label-server-shutting-down = 服务器正在关闭
label-unknown-server-state = 未知服务器状态

## 游戏
notification-saved-screenshot = 已保存截图 { $filename }

## 聊天命令
notification-invalid-command = { $name } 不是有效的命令。

## 调试菜单逻辑
tooltip-debug-command = 调试命令: { $command }

## 调试可视化命令
description-combat-geometry = 切换战斗几何叠加。
description-render-geometry = 切换渲染几何叠加。
description-screen-map-overlay = 切换屏幕地图叠加。
description-depth-buffer = 切换深度缓冲叠加。
description-actor-tags-overlay = 切换实体标签叠加。

## 开发者命令
notification-invalid-cash-amount = 无效的现金金额。
description-toggle-visibility = 切换可见性检查和小地图。
description-give-cash = 给予默认或指定数量的金钱。
description-give-cash-all = 给予默认或指定数量的金钱给所有玩家和AI。
description-instant-building = 切换即时建造。
description-build-anywhere = 切换任意建造能力。
description-unlimited-power = 切换无限电力。
description-enable-tech = 切换建造所有物品的能力。
description-fast-charge = 切换接近即时的支援能力充电。
description-dev-cheat-all = 切换所有作弊并给予一些金钱。
description-dev-crash = 游戏崩溃。
description-levelup-actor = 为选定的实体添加指定数量的等级。
description-player-experience = 为本地玩家添加指定数量的玩家经验。
description-power-outage = 造成本地玩家5秒的断电。
description-grow-resources = 在地图上生长资源。
description-clear-shroud = 揭示整个地图。
description-reset-shroud = 隐藏整个地图。
description-heal-selected-actors = 治疗选定的实体。
description-kill-selected-actors = 杀死选定的实体。
description-dispose-selected-actors = 处理选定的实体。

## 开发者命令、调试可视化命令、自定义地形调试叠加、实体地图叠加、单元触发叠加、退出调试叠加管理器、层次化路径查找器叠加、路径查找器叠加、地形几何叠加
notification-cheats-disabled = 调试已禁用。

## 帮助命令
notification-available-commands = 这里是可用的命令:
description-no-description = 无可用描述。
description-help-description = 提供关于各种命令的有用信息。

## 玩家命令
description-pause-description = 暂停或取消暂停游戏。
description-surrender-description = 自毁所有东西并输掉游戏。

## 开发者模式，获得经验，电力管理器
notification-cheat-used = 使用了作弊: { $cheat } 由 { $player }{ $suffix }。

## 开发者模式，调试可视化命令、自定义地形调试叠加、实体地图叠加、单元触发叠加、退出调试叠加管理器、层次化路径查找器叠加、路径查找器叠加、地形几何叠加
notification-cheat-enabled = 启用了作弊: { $cheat } 由 { $player }。
notification-cheat-disabled = 禁用了作弊: { $cheat } 由 { $player }。

## 自定义地形调试叠加
description-custom-terrain-debug-overlay = 切换自定义地形调试叠加。

## 单元触发叠加
description-cell-triggers-overlay = 切换脚本触发叠加。

## 层次化路径查找器叠加
description-hpf-debug-overlay = 切换层次化路径查找器叠加。

## 路径查找器叠加
description-path-debug-overlay = 切换路径搜索的可视化。

## 地形几何叠加
description-terrain-geometry-overlay = 切换地形几何叠加。

## 实体地图叠加
description-actor-map-overlay = 切换实体地图叠加。

## 地图选项，任务浏览器逻辑
options-game-speed =
    .slowest = 最慢
    .slower = 更慢
    .normal = 正常
    .fast = 快速
    .faster = 更快
    .fastest = 最快

## 时间限制管理器
options-time-limit =
    .no-limit = 无限制
    .options =
        { $minutes ->
            [one] { $minutes } 分钟
           *[other] { $minutes } 分钟
        }

notification-time-limit-expired = 时间限制已过期。

## 编辑器实体刷
notification-added-actor = 已添加 { $name } ({ $id })

## 编辑器复制粘贴刷
notification-copied-tiles = 已复制 { $tiles } 个图块
notification-copied-actors = 已复制 { $actors } 个实体
notification-copied-tiles-actors = 已复制 { $tiles } 个图块和 { $actors } 个实体

## 编辑器默认刷
notification-selected-area = 已选择区域 { $x },{ $y } ({ $width },{ $height })
notification-removed-area = 已移除区域 { $x },{ $y } ({ $width },{ $height })
notification-selected-actor = 已选择实体 { $id }
notification-cleared-selection = 已清空选择
notification-removed-actor = 已移除 { $name } ({ $id })
notification-removed-resource = 已移除 { $type }
notification-moved-actor = 已将 { $id } 从 { $x1 },{ $y1 } 移动到 { $x2 },{ $y2 }

## 编辑器资源刷
notification-added-resource =
    { $count ->
       [one] 已添加一个 { $type } 单元
      *[other] 已添加 { $count } 个 { $type } 单元
    }

## 编辑器图块刷
notification-added-tile = 已添加图块 { $id }
notification-filled-tile = 已用图块 { $id } 填充

## 编辑器标记层刷
notification-added-marker-tiles-markers =
    .red = 红色
    .orange = 橙色
    .yellow = 黄色
    .green = 绿色
    .cyan = 青色
    .blue = 蓝色
    .purple = 紫色
    .magenta = 洋红色
notification-added-marker-tiles =
    { $count ->
       [one] 已添加 { $type } 标记图块
      *[other] 已添加 { $count } 个 { $type } 标记图块
    }
notification-removed-marker-tiles =
    { $count ->
       [one] 已移除标记图块
      *[other] 已移除 { $count } 个标记图块
    }
notification-cleared-selected-marker-tiles =
    { $count ->
       [one] 已清除 { $type } 标记图块
      *[other] 已清除 { $count } 个 { $type } 标记图块
    }
notification-cleared-all-marker-tiles = 已清除 { $count } 个标记图块

## 编辑器操作管理器
notification-opened = 已打开

## 地图叠加逻辑
mirror-mode =
    .none = 无
    .flip = 翻转
    .rotate = 旋转

## 实体编辑逻辑
notification-edited-actor = 已编辑 { $name } ({ $id })
notification-edited-actor-id = 已编辑 { $name } ({ $old-id }-> { $new-id })

## 征服胜利条件，战略胜利条件
notification-player-is-victorious = { $player } 认为胜利。
notification-player-is-defeated = { $player } 认为失败。

## 命令管理器
notification-desync-compare-logs = 在帧 { $frame } 处不同步。
    请将 syncreport.log 与其它玩家对比。

## 小工具工具
label-win-state-won = 胜利
label-win-state-lost = 失败
label-client-state-disconnected = 已离开

## 玩家
enumerated-bot-name =
    { $name } { $number ->
       *[zero] {""}
        [other] { $number }
    }

## 修饰符扩展
keycode-modifier =
    .alt = Alt
    .ctrl = Ctrl
    .meta = Meta
    .cmd = Cmd
    .shift = Shift
    .none = 无

## 键码扩展
keycode =
    .unknown = 未定义
    .return = 回车
    .escape = ESC
    .backspace = 退格
    .tab = Tab
    .space = 空格
    .exclaim = !
    .quotedbl = "
    .hash = #
    .percent = %
    .dollar = $
    .ampersand = &
    .quote = '
    .leftparen = (
    .rightparen = )
    .asterisk = *
    .plus = +
    .comma = ,
    .minus = -
    .period = .
    .slash = /
    .number_0 = 0
    .number_1 = 1
    .number_2 = 2
    .number_3 = 3
    .number_4 = 4
    .number_5 = 5
    .number_6 = 6
    .number_7 = 7
    .number_8 = 8
    .number_9 = 9
    .colon = :
    .semicolon = ;
    .less = <
    .equals = =
    .greater = >
    .question = ?
    .at = @
    .leftbracket = [
    .backslash = \
    .rightbracket = ]
    .caret = ^
    .underscore = _
    .backquote = `
    .a = A
    .b = B
    .c = C
    .d = D
    .e = E
    .f = F
    .g = G
    .h = H
    .i = I
    .j = J
    .k = K
    .l = L
    .m = M
    .n = N
    .o = O
    .p = P
    .q = Q
    .r = R
    .s = S
    .t = T
    .u = U
    .v = V
    .w = W
    .x = X
    .y = Y
    .z = Z
    .capslock = CapsLock
    .f1 = F1
    .f2 = F2
    .f3 = F3
    .f4 = F4
    .f5 = F5
    .f6 = F6
    .f7 = F7
    .f8 = F8
    .f9 = F9
    .f10 = F10
    .f11 = F11
    .f12 = F12
    .printscreen = PrintScreen
    .scrolllock = ScrollLock
    .pause = Pause
    .insert = Insert
    .home = Home
    .pageup = PageUp
    .delete = Delete
    .end = End
    .pagedown = PageDown
    .right = 右
    .left = 左
    .down = 下
    .up = 上
    .numlockclear = Numlock
    .kp_divide = 数字键 /
    .kp_multiply = 数字键 *
    .kp_minus = 数字键 -
    .kp_plus = 数字键 +
    .kp_enter = 数字键回车
    .kp_1 = 数字键 1
    .kp_2 = 数字键 2
    .kp_3 = 数字键 3
    .kp_4 = 数字键 4
    .kp_5 = 数字键 5
    .kp_6 = 数字键 6
    .kp_7 = 数字键 7
    .kp_8 = 数字键 8
    .kp_9 = 数字键 9
    .kp_0 = 数字键 0
    .kp_period = 数字键 .
    .application = 应用程序
    .power = 电源
    .kp_equals = 数字键 =
    .f13 = F13
    .f14 = F14
    .f15 = F15
    .f16 = F16
    .f17 = F17
    .f18 = F18
    .f19 = F19
    .f20 = F20
    .f21 = F21
    .f22 = F22
    .f23 = F23
    .f24 = F24
    .execute = 执行
    .help = 帮助
    .menu = 菜单
    .select = 选择
    .stop = 停止
    .again = 再次
    .undo = 撤消
    .cut = 剪切
    .copy = 复制
    .paste = 粘贴
    .find = 查找
    .mute = 静音
    .volumeup = 音量+
    .volumedown = 音量-
    .kp_comma = 数字键 ,
    .kp_equalsas400 = 数字键 (AS400)
    .alterase = AltErase
    .sysreq = SysReq
    .cancel = 取消
    .clear = 清除
    .prior = Prior
    .return2 = 回车
    .separator = 分隔符
    .out = Out
    .oper = Oper
    .clearagain = 清除/再次
    .crsel = CrSel
    .exsel = ExSel
    .kp_00 = 数字键 00
    .kp_000 = 数字键 000
    .thousandsseparator = 千位分隔符
    .decimalseparator = 小数分隔符
    .currencyunit = 货币单位
    .currencysubunit = 货币分单位
    .kp_leftparen = 数字键 (
    .kp_rightparen = 数字键 )
    .kp_leftbrace = 数字键 {"{"}
    .kp_rightbrace = 数字键 {"}"}
    .kp_tab = 数字键 Tab
    .kp_backspace = 数字键 Backspace
    .kp_a = 数字键 A
    .kp_b = 数字键 B
    .kp_c = 数字键 C
    .kp_d = 数字键 D
    .kp_e = 数字键 E
    .kp_f = 数字键 F
    .kp_xor = 数字键 XOR
    .kp_power = 数字键 ^
    .kp_percent = 数字键 %
    .kp_less = 数字键 <
    .kp_greater = 数字键 >
    .kp_ampersand = 数字键 &
    .kp_dblampersand = 数字键 &&
    .kp_verticalbar = 数字键 |
    .kp_dblverticalbar = 数字键 ||
    .kp_colon = 数字键 :
    .kp_hash = 数字键 #
    .kp_space = 数字键 Space
    .kp_at = 数字键 @
    .kp_exclam = 数字键 !
    .kp_memstore = 数字键 MemStore
    .kp_memrecall = 数字键 MemRecall
    .kp_memclear = 数字键 MemClear
    .kp_memadd = 数字键 MemAdd
    .kp_memsubtract = 数字键 MemSubtract
    .kp_memmultiply = 数字键 MemMultiply
    .kp_memdivide = 数字键 MemDivide
    .kp_plusminus = 数字键 +/-
    .kp_clear = 数字键 Clear
    .kp_clearentry = 数字键 ClearEntry
    .kp_binary = 数字键 二进制
    .kp_octal = 数字键 八进制
    .kp_decimal = 数字键 十进制
    .kp_hexadecimal = 数字键 十六进制
    .lctrl = 左Ctrl
    .lshift = 左Shift
    .lalt = 左Alt
    .lgui = 左GUI
    .rctrl = 右Ctrl
    .rshift = 右Shift
    .ralt = 右Alt
    .rgui = 右GUI
    .mode = 模式切换
    .audionext = 音频下一曲
    .audioprev = 音频上一曲
    .audiostop = 音频停止
    .audioplay = 音频播放
    .audiomute = 音频静音
    .mediaselect = 媒体选择
    .www = 网页浏览
    .mail = 邮件
    .calculator = 计算器
    .computer = 计算机
    .ac_search = AC搜索
    .ac_home = AC主页
    .ac_back = AC后退
    .ac_forward = AC前进
    .ac_stop = AC停止
    .ac_refresh = AC刷新
    .ac_bookmarks = AC收藏
    .brightnessdown = 亮度下调
    .brightnessup = 亮度上调
    .displayswitch = 显示器切换
    .kbdillumtoggle = 键盘照明切换
    .kbdillumdown = 键盘照明下调
    .kbdillumup = 键盘照明上调
    .eject = 弹出
    .sleep = 睡眠
    .mouse4 = 鼠标4
    .mouse5 = 鼠标5

## 地图生成器工具逻辑
notification-map-generator-generated = 使用 { $name } 生成
## 地图生成失败
dialog-notification-map-generator-failed =
    .title = 地图生成失败
    .prompt = 详情请查看 debug.log。
    .cancel = 关闭

## 编辑器平铺路径刷
notification-tiling-path-started = 已开始平铺路径
notification-tiling-path-updated = 已更新平铺路径
notification-tiling-path-reset = 已丢弃平铺路径
notification-tiling-path-painted = 已绘制平铺路径
