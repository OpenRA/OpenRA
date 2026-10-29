## Buttons
button-cancel = 取消
button-retry = 重試
button-back = 返回
button-continue = 繼續
button-quit = 離開

## Server Orders
notification-custom-rules = 此地圖含有自訂規則。遊戲體驗可能會改變。
notification-two-humans-required = 此伺服器至少需要兩名人類玩家才能開始對戰。
notification-unknown-server-command = 未知的伺服器指令：{ $command }。
notification-admin-start-game = 只有房主可以開始遊戲。
notification-no-start-until-required-slots-full = 必要欄位尚未滿員，無法開始遊戲。
notification-no-start-without-players = 沒有玩家就無法開始遊戲。
notification-insufficient-enabled-spawn-points = 需要啟用更多出生點才能開始遊戲。
notification-malformed-command = { $command } 指令格式錯誤。
notification-state-unchanged-ready = 已標記為準備完成時，無法變更狀態。
notification-invalid-faction-selected = 選取的陣營無效：{ $faction }。
notification-state-unchanged-game-started = 遊戲開始後無法變更狀態（{ $command }）。
notification-requires-host = 只有房主可以這麼做。
notification-invalid-bot-slot = 無法將電腦加入已有其他客戶端的欄位。
notification-invalid-bot-type = 電腦類型無效。
notification-admin-change-map = 只有房主可以更換地圖。
notification-player-disconnected = { $player } 已斷線。
notification-team-player-disconnected = { $player }（隊伍 { $team }）已斷線。
notification-observer-disconnected = { $player }（觀戰）已斷線。
notification-unknown-map = 伺服器上找不到地圖。
notification-searching-map = 正在資源中心搜尋地圖……
notification-admin-change-configuration = 只有房主可以變更設定。
notification-changed-map = { $player } 將地圖換成 { $map }。
notification-hidden-map-has-been-previewed = 這張產生的地圖在建立時已被管理員預覽。
notification-you-were-kicked = 你已被踢出伺服器。
notification-admin-kicked = { $admin } 將 { $player } 踢出伺服器。
notification-kicked = { $player } 被踢出伺服器。
notification-temp-ban = { $admin } 已暫時封鎖 { $player }。
notification-admin-transfer-admin = 只有管理員可以將管理員權限轉移給其他玩家。
notification-admin-move-spectators = 只有房主可以將玩家移至觀戰。
notification-empty-slot = 該欄位沒有人。
notification-move-spectators = { $admin } 將 { $player } 移至觀戰。
notification-nick-changed = { $player } 現在改名為 { $name }。
notification-player-dropped = 有玩家因逾時而斷線。
notification-connection-problems = { $player } 的連線不穩。
notification-timeout-dropped = { $player } 因逾時而斷線。
notification-timeout-dropped-in = { $timeout -> [one] { $player } 將在 { $timeout } 秒後斷線。 *[other] { $player } 將在 { $timeout } 秒後斷線。 }
notification-error-game-started = 遊戲已經開始。
notification-requires-password = 伺服器需要密碼。
notification-incorrect-password = 密碼不正確。
notification-incompatible-mod = 伺服器執行的模組不相容。
notification-incompatible-version = 伺服器執行的版本不相容。
notification-incompatible-protocol = 伺服器使用的通訊協定不相容。
notification-you-were-banned = 你已被此伺服器封鎖。
notification-you-were-temp-banned = 你已被此伺服器暫時封鎖。
notification-game-full = 遊戲已滿。
notification-new-admin = { $player } 現在是管理員。
notification-invalid-configuration-command = 設定指令無效。
notification-admin-option = 只有房主可以設定該選項。
notification-error-number-teams = 無法解析隊伍數量：{ $raw }。
notification-admin-kick = 只有房主可以踢出玩家。
notification-kick-self = 房主無法踢出自己。
notification-kick-none = 該欄位沒有人。
notification-no-kick-game-started = 遊戲開始後，只能踢出觀戰者與已失敗的玩家。
notification-admin-clear-spawn = 只有管理員可以清除出生點。
notification-spawn-occupied = 你不能與其他玩家使用同一個出生點。
notification-spawn-locked = 此出生點已鎖定給另一個玩家欄位。
notification-admin-lobby-info = 只有房主可以設定大廳資訊。
notification-invalid-lobby-info = 送出的大廳資訊無效。
notification-player-color-terrain = 已調整顏色，使其與地形較不相似。
notification-player-color-player = 已調整顏色，使其與另一位玩家較不相似。
notification-invalid-player-color = 無法決定有效的玩家顏色。已選取隨機顏色。
notification-invalid-error-code = 無法解析錯誤訊息。
notification-master-server-connected = 已與主伺服器建立通訊。
notification-master-server-error = 與主伺服器通訊失敗。
notification-game-offline = 遊戲尚未公告到線上。
notification-no-port-forward = 無法從網際網路連入伺服器連接埠。
notification-blacklisted-server-name = 伺服器名稱包含禁用詞。
notification-requires-authentication = 伺服器要求玩家擁有 OpenRA 論壇帳號。
notification-no-permission-to-join = 你沒有加入此伺服器的權限。
notification-slot-closed = 房主已關閉你的欄位。

## ServerOrders, UnitOrders
notification-joined = { $player } 已加入遊戲。
notification-lobby-disconnected = { $player } 已離開。

## UnitOrders
notification-game-has-started = 遊戲已開始。
notification-game-paused = 遊戲已被 { $player } 暫停。
notification-game-unpaused = 遊戲已被 { $player } 取消暫停。

## Server
notification-game-started = 遊戲已開始。

## PlayerMessageTracker
notification-chat-temp-disabled = { $remaining -> [one] 聊天已停用。請在 { $remaining } 秒後再試。 *[other] 聊天已停用。請在 { $remaining } 秒後再試。 }

## VoteKickTracker
notification-unable-to-start-a-vote = 無法開始投票。
notification-insufficient-votes-to-kick = 票數不足以踢出玩家 { $kickee }。
notification-kick-already-voted = 你已經投過票。
notification-vote-kick-started = 玩家 { $kicker } 已發起投票，要踢出玩家 { $kickee }。
notification-vote-kick-in-progress = 已有 { $percentage }% 的玩家投票踢出玩家 { $kickee }。
notification-vote-kick-ended = 踢出玩家 { $kickee } 的投票未通過。

## ActorEditLogic
label-duplicate-actor-id = 重複的單位 ID
label-actor-id = 請輸入單位 ID
label-actor-owner = 擁有者

## ActorSelectorLogic
label-actor-type = 類型：{ $actorType }

## CommonSelectorLogic
options-common-selector =
    .search-results = 搜尋結果
    .all = 全部
    .multiple = 多個
    .none = 無

## SaveMapLogic
label-unpacked-map = 未封裝

dialog-save-map-failed =
    .title = 儲存地圖失敗
    .prompt = 詳情請見 debug.log。
    .confirm = 確定

dialog-overwrite-map-failed =
    .title = 警告
    .prompt = 儲存將會覆寫已存在的地圖。
    .confirm = 儲存

dialog-overwrite-map-outside-edit =
    .title = 警告
    .prompt = 地圖已在編輯器外被修改。儲存可能會覆寫進度。
    .confirm = 儲存

notification-save-current-map = 已儲存目前的地圖。

## GameInfoLogic
menu-game-info =
    .objectives = 目標
    .briefing = 任務簡報
    .options = 選項
    .debug = 作弊
    .chat = 聊天

## GameInfoObjectivesLogic, GameInfoStatsLogic
label-mission-in-progress = 進行中
label-mission-accomplished = 已完成
label-mission-failed = 失敗

## GameInfoStatsLogic
label-mute-player = 將此玩家靜音
label-unmute-player = 取消此玩家靜音
button-kick-player = 踢出此玩家
button-vote-kick-player = 投票踢出此玩家

dialog-kick =
    .title = 踢出 { $player }？
    .prompt = 此玩家將無法重新加入遊戲。
    .confirm = 踢出

dialog-vote-kick =
    .title = 投票踢出 { $player }？
    .prompt = 此玩家將無法重新加入遊戲。
    .prompt-break-bots = { $bots -> [one] 踢出遊戲管理員也會一併踢出 1 個電腦。 *[other] 踢出遊戲管理員也會一併踢出 { $bots } 個電腦。 }
    .vote-start = 開始投票
    .vote-for = 贊成
    .vote-against = 反對
    .vote-cancel = 棄權

notification-vote-kick-disabled = 此伺服器已停用投票踢出。

## GameTimerLogic
label-paused = 已暫停
label-max-speed = 最高速度
label-replay-speed = { $percentage }% 速度
label-replay-complete = 已完成 { $percentage }%

## LobbyLogic, InGameChatLogic
label-chat-disabled = 聊天已停用
label-chat-availability = { $seconds -> [one] 聊天將在 { $seconds } 秒後可用…… *[other] 聊天將在 { $seconds } 秒後可用…… }

## LobbyLogic, ServerListLogic
label-bot-player = 電腦玩家

## LobbyLogic
notification-lobby-option = { $name }：{ $value }。
notification-lobby-option-changed = { $name } 已變更為 { $value }。
notification-map-bots-disabled = 此地圖已停用電腦。

## IngameMenuLogic
menu-ingame =
    .leave = 離開
    .abort = 放棄任務
    .restart = 重新開始
    .surrender = 投降
    .load-game = 載入遊戲
    .save-game = 儲存遊戲
    .music = 音樂
    .settings = 設定
    .return-to-map = 返回地圖
    .resume = 繼續
    .save-map = 儲存地圖
    .exit-map = 離開地圖編輯器

dialog-leave-mission =
    .title = 離開任務
    .prompt = 要離開這場遊戲並返回選單嗎？
    .confirm = 離開
    .cancel = 留下

dialog-restart-mission =
    .title = 重新開始
    .prompt = 確定要重新開始嗎？
    .confirm = 重新開始
    .cancel = 留下

dialog-surrender =
    .title = 投降
    .prompt = 確定要投降嗎？
    .confirm = 投降
    .cancel = 留下

dialog-error-max-player =
    .title = 錯誤：超過玩家人數上限
    .prompt = 定義的玩家過多（{ $players }/{ $max }）。
    .confirm = 返回

dialog-exit-map-editor =
    .title = 離開地圖編輯器
    .prompt-unsaved = 要離開並放棄所有未儲存的變更嗎？
    .prompt-deleted = 地圖可能已在編輯器外被刪除
    .confirm-anyway = 仍要離開
    .confirm = 離開

dialog-play-map-warning =
    .title = 警告
    .prompt = 地圖可能已被刪除，或含有無法載入的錯誤。
    .cancel = 好

dialog-exit-to-map-editor =
    .title = 離開任務
    .prompt = 要離開這場遊戲並返回編輯器嗎？
    .confirm = 返回編輯器
    .cancel = 留下

## IngamePowerBarLogic
## IngamePowerCounterLogic
label-power-usage = 電力用量：{ $usage }/{ $capacity }
label-infinite-power = 無限

## IngameSiloBarLogic
## IngameCashCounterLogic
label-silo-usage = 儲存槽用量：{ $usage }/{ $capacity }

## ObserverShroudSelectorLogic
options-shroud-selector =
    .all-players = 所有玩家
    .disable-shroud = 關閉戰爭迷霧
    .other = 其他

## ObserverStatsLogic
options-observer-stats =
    .none = 資訊：無
    .basic = 基本
    .economy = 經濟
    .production = 生產
    .support-powers = 支援能力
    .combat = 戰鬥
    .army = 軍隊
    .earnings-graph = 收入（圖表）
    .army-graph = 軍隊（圖表）

## WorldTooltipLogic
label-unrevealed-terrain = 未揭開的地形

## KickClientLogic
dialog-kick-client =
    .prompt = 踢出 { $player }？

## KickSpectatorsLogic
dialog-kick-spectators =
    .prompt = { $count -> [one] 確定要踢出一位觀戰者嗎？ *[other] 確定要踢出 { $count } 位觀戰者嗎？ }

## LobbyLogic
options-slot-admin =
    .add-bots = 新增
    .remove-bots = 移除
    .configure-bots = 設定電腦
    .teams-count = { $count } 個隊伍
    .humans-vs-bots = 人類對電腦
    .free-for-all = 自由混戰
    .configure-teams = 設定隊伍

## LobbyLogic, InGameChatLogic
button-general-chat = 全員
button-team-chat = 隊伍

## LobbyOptionsLogic, MissionBrowserLogic
label-not-available = 無法使用

## LobbyUtils
options-lobby-slot =
    .slot = 欄位
    .open = 開放
    .closed = 關閉
    .bots = 電腦
    .bots-disabled = 電腦已停用

## MapPreviewLogic
label-connecting = 連線中……
label-downloading-map = 正在下載 { $size } kB
label-downloading-map-progress = 正在下載 { $size } kB（{ $progress }%）
button-retry-install = 重試安裝
button-retry-search = 重試搜尋
## also MapChooserLogic
label-created-by = 製作者：{ $author }

## SpawnSelectorTooltipLogic
label-disabled-spawn = 已停用出生點 { $spawn }
label-available-spawn = 可用出生點 { $spawn }
label-spawn-name = 出生點 { $spawn }
label-team-spawn = 隊伍 { $team } 出生點 { $spawn }

## DisplaySettingsLogic
options-camera =
    .close = 近
    .medium = 中
    .far = 遠
    .furthest = 最遠

options-display-mode =
    .windowed = 視窗模式
    .legacy-fullscreen = 全螢幕（舊版）
    .fullscreen = 全螢幕

label-video-display-index = 螢幕 { $number }

options-status-bars =
    .standard = 標準
    .show-on-damage = 受傷時顯示
    .always-show = 總是顯示

options-target-lines =
    .automatic = 自動
    .manual = 手動
    .disabled = 停用

checkbox-frame-limiter = 啟用幀數限制（{ $fps } FPS）

## HotkeysSettingsLogic
label-original-notice = 預設為「{ $key }」
label-duplicate-notice = 已用於「{ $key }」，所屬情境為 { $context }
hotkey-context-any = 任何

## GameplaySettingsLogic
auto-save-interval =
    .disabled = 停用
    .options = { $seconds -> [one] 1 秒 *[other] { $seconds } 秒 }
    .minute-options = { $minutes -> [one] 1 分鐘 *[other] { $minutes } 分鐘 }

auto-save-max-file-number = { $saves } 個存檔

## InputSettingsLogic
options-mouse-scroll-type =
    .disabled = 停用
    .standard = 標準
    .inverted = 反向
    .joystick = 搖桿

## InputSettingsLogic, IntroductionPromptLogic
options-control-scheme =
    .classic = 經典
    .modern = 現代
    .otherrts = 其他即時戰略

## SettingsLogic
dialog-settings-save =
    .title = 需要重新啟動
    .prompt = 部分變更要等遊戲重新啟動後才會套用。
    .cancel = 繼續

dialog-settings-restart =
    .title = 要立即重新啟動嗎？
    .prompt = 部分變更要等遊戲重新啟動後才會套用。要立即重新啟動嗎？
    .confirm = 立即重新啟動
    .cancel = 稍後重新啟動

dialog-settings-reset =
    .title = 重設 { $panel }
    .prompt = 確定要重設此面板的所有設定嗎？
    .confirm = 重設
    .cancel = 取消

## AssetBrowserLogic
label-all-packages = 所有套件
label-length-in-seconds = { $length } 秒

## ConnectionLogic
label-connecting-to-endpoint = 正在連線至 { $endpoint }……
label-could-not-connect-to-target = 無法連線至 { $target }
label-unknown-error = 未知錯誤
label-password-required = 需要密碼
label-connection-failed = 連線失敗
notification-mod-switch-failed = 切換模組失敗。

## GameSaveBrowserLogic
dialog-rename-save =
    .title = 重新命名存檔
    .prompt = 請輸入新的檔案名稱：
    .confirm = 重新命名

dialog-delete-save =
    .title = 要刪除選取的遊戲存檔嗎？
    .prompt = 刪除「{ $save }」。
    .confirm = 刪除

dialog-delete-all-saves =
    .title = 要刪除所有遊戲存檔嗎？
    .prompt = { $count -> [one] 刪除 { $count } 個存檔。 *[other] 刪除 { $count } 個存檔。 }
    .confirm = 全部刪除

notification-save-deletion-failed = 無法刪除存檔「{ $savePath }」。詳情請見紀錄。

dialog-overwrite-save =
    .title = 要覆寫遊戲存檔嗎？
    .prompt = 要覆寫 { $file } 嗎？
    .confirm = 覆寫

## MainMenuLogic
label-loading-news = 正在載入新聞
label-news-retrieval-failed = 取得新聞失敗：{ $message }
label-news-parsing-failed = 解析新聞失敗：{ $message }
label-author-datetime = 由 { $author } 於 { $datetime }

## MapChooserLogic
label-all-maps = 所有地圖
label-no-matches = 沒有符合的項目
label-player-count = { $players -> [one] { $players } 位玩家 *[other] { $players } 位玩家 }
label-map-size-huge = 巨大
label-map-size-large = 大
label-map-size-medium = 中
label-map-size-small = 小
label-map-searching-count = { $count -> [one] 正在 OpenRA 資源中心搜尋 { $count } 張地圖…… *[other] 正在 OpenRA 資源中心搜尋 { $count } 張地圖…… }
label-map-unavailable-count = { $count -> [one] 在 OpenRA 資源中心找不到 { $count } 張地圖 *[other] 在 OpenRA 資源中心找不到 { $count } 張地圖 }

notification-map-deletion-failed = 無法刪除地圖「{ $map }」。詳情請見 debug.log。

dialog-delete-map =
    .title = 刪除地圖
    .prompt = 要刪除地圖「{ $title }」嗎？
    .confirm = 刪除

dialog-delete-all-maps =
    .title = 刪除地圖
    .prompt = 要刪除此頁的所有地圖嗎？
    .confirm = 刪除

options-order-maps =
    .player-count = 玩家
    .title = 標題
    .date = 日期
    .size = 大小

button-mapchooser-system-maps-tab = 官方地圖
button-mapchooser-remote-maps-tab = 伺服器地圖
button-mapchooser-user-maps-tab = 自訂地圖
button-mapchooser-generated-maps-tab = 產生地圖

## MissionBrowserLogic
dialog-no-video =
    .title = 尚未安裝影片
    .prompt = 遊戲影片可從「管理內容」選單安裝。
    .cancel = 返回

dialog-cant-play-video =
    .title = 無法播放影片
    .prompt = 播放影片時發生問題。
    .cancel = 返回

## MusicPlayerLogic
label-sound-muted = 已在設定中將音效靜音。
label-no-song-playing = 目前沒有播放曲目

## MuteHotkeyLogic
label-audio-muted = 音效已靜音。
label-audio-unmuted = 已取消音效靜音。

## PlayerProfileLogic
label-loading-player-profile = 正在載入玩家資料……
label-loading-player-profile-failed = 載入玩家資料失敗。

## ProductionTooltipLogic, EncyclopediaLogic
label-requires = 需要 { $prerequisites }。

## ReplayBrowserLogic
label-duration = 時長：{ $time }

options-replay-type =
    .singleplayer = 單人遊戲
    .multiplayer = 多人連線

options-winstate =
    .victory = 勝利
    .defeat = 失敗

options-save-type =
    .autosave = 自動存檔
    .manual = 手動存檔

options-replay-date =
    .today = 今天
    .last-week = 最近 7 天
    .last-fortnight = 最近 14 天
    .last-month = 最近 30 天

options-replay-duration =
    .very-short = 5 分鐘以內
    .short = 短（10 分鐘）
    .medium = 中（30 分鐘）
    .long = 長（60 分鐘以上）

dialog-rename-replay =
    .title = 重新命名重播
    .prompt = 請輸入新的檔案名稱：
    .confirm = 重新命名

dialog-delete-replay =
    .title = 要刪除選取的重播嗎？
    .prompt = 要刪除重播 { $replay } 嗎？
    .confirm = 刪除

dialog-delete-all-replays =
    .title = 要刪除所有選取的重播嗎？
    .prompt = { $count -> [one] 刪除 { $count } 個重播。 *[other] 刪除 { $count } 個重播。 }
    .confirm = 全部刪除

notification-replay-deletion-failed = 無法刪除重播檔「{ $file }」。詳情請見 debug.log。

## ReplayUtils
-incompatible-replay-recorded = 錄製時使用的是

dialog-incompatible-replay =
    .title = 不相容的重播
    .prompt = 無法讀取重播的中繼資料。
    .confirm = 確定
    .prompt-unknown-version = { -incompatible-replay-recorded }未知版本。
    .prompt-unknown-mod = { -incompatible-replay-recorded }未知模組。
    .prompt-unavailable-mod = { -incompatible-replay-recorded }無法使用的模組：{ $mod }。
    .prompt-incompatible-version = { -incompatible-replay-recorded }不相容的版本：{ $version }。
    .prompt-unavailable-map = { -incompatible-replay-recorded }無法使用的地圖：{ $map }。

# SelectUnitsByTypeHotkeyLogic
nothing-selected = 沒有選取任何單位。

## SelectUnitsByTypeHotkeyLogic, SelectAllUnitsHotkeyLogic
selected-units-across-screen = { $units -> [one] 已選取畫面中的一個單位。 *[other] 已選取畫面中的 { $units } 個單位。 }

selected-units-across-map = { $units -> [one] 已選取地圖上的一個單位。 *[other] 已選取地圖上的 { $units } 個單位。 }

## ServerCreationLogic
label-internet-server-nat-A = 網際網路伺服器（UPnP/NAT-PMP
label-internet-server-nat-B-enabled = 已啟用
label-internet-server-nat-B-not-supported = 不支援
label-internet-server-nat-B-disabled = 已停用
label-internet-server-nat-C = ）：

label-local-server = 區網伺服器：

dialog-server-creation-failed =
    .prompt = 無法監聽連接埠 { $port }。
    .prompt-port-used = 請檢查連接埠是否已被佔用。
    .prompt-error = 錯誤為：「{ $message }」（{ $code }）。
    .title = 建立伺服器失敗
    .cancel = 返回

## ServerListLogic
label-players-online-count = { $players -> [one] { $players } 位玩家在線上 *[other] { $players } 位玩家在線上 }

label-search-status-failed = 查詢伺服器清單失敗。
label-search-status-no-games = 找不到遊戲。請試著變更篩選條件。
label-no-server-selected = 未選取伺服器

label-map-status-searching = 搜尋中……
label-map-classification-unknown = 未知地圖

label-players-count = { $players -> [0] 沒有玩家 [one] 一位玩家 *[other] { $players } 位玩家 }

label-bots-count = { $bots -> [0] 沒有電腦 [one] 一個電腦 *[other] { $bots } 個電腦 }

## ServerListLogic, ReplayBrowserLogic, ObserverShroudSelectorLogic
label-players = 玩家

## ServerListLogic, GameInfoStatsLogic
label-spectators = 觀戰者
label-spectators-count = { $spectators -> [0] 沒有觀戰者 [one] 一位觀戰者 *[other] { $spectators } 位觀戰者 }

## ServerlistLogic, GameInfoStatsLogic, ObserverShroudSelectorLogic, ReplayBrowserLogic
label-team-name = 隊伍 { $team }
label-no-team = 無隊伍

label-playing = 遊戲中
label-waiting = 等待中

label-other-players-count = { $players -> [one] 另外一位玩家 *[other] 另外 { $players } 位玩家 }

label-in-progress-for = { $minutes -> [0] 進行不到一分鐘。 [one] 已進行 { $minutes } 分鐘。 *[other] 已進行 { $minutes } 分鐘。 }

label-password-protected = 有密碼保護
label-waiting-for-players = 正在等待玩家
label-server-shutting-down = 伺服器正在關閉
label-unknown-server-state = 未知的伺服器狀態

## Game
notification-saved-screenshot = 已儲存螢幕截圖 { $filename }

## ChatCommands
notification-invalid-command = { $name } 不是有效的指令。

## DebugMenuLogic
tooltip-debug-command = 作弊指令：{ $command }

## DebugVisualizationCommands
description-combat-geometry = 切換戰鬥幾何疊加層。
description-render-geometry = 切換繪製幾何疊加層。
description-screen-map-overlay = 切換螢幕地圖疊加層。
description-depth-buffer = 切換深度緩衝疊加層。
description-actor-tags-overlay = 切換單位標籤疊加層。

## DevCommands
notification-invalid-cash-amount = 資金數額無效。
description-toggle-visibility = 切換視野檢查與小地圖。
description-give-cash = 給予預設或指定數額的資金。
description-give-cash-all = 給予所有玩家與電腦預設或指定數額的資金。
description-instant-building = 切換立即建造。
description-build-anywhere = 切換在任意地點建造的能力。
description-unlimited-power = 切換無限電力。
description-enable-tech = 切換建造所有東西的能力。
description-fast-charge = 切換支援能力近乎立即充能。
description-dev-cheat-all = 切換所有作弊，並給你一些資金作為補償。
description-dev-crash = 使遊戲當機。
description-levelup-actor = 為選取的單位增加指定的等級數。
description-player-experience = 為本機玩家增加指定數量的玩家經驗。
description-power-outage = 使本機玩家停電 5 秒。
description-grow-resources = 讓地圖上的資源生長。
description-clear-shroud = 揭開整張地圖。
description-reset-shroud = 隱藏整張地圖。
description-heal-selected-actors = 治療選取的單位。
description-kill-selected-actors = 消滅選取的單位。
description-dispose-selected-actors = 移除選取的單位。

## DevCommands, DebugVisualizationCommands, CustomTerrainDebugOverlay, ActorMapOverlay, CellTriggerOverlay, ExitsDebugOverlayManager, HierarchicalPathFinderOverlay, PathFinderOverlay, TerrainGeometryOverlay
notification-cheats-disabled = 作弊已停用。

## HelpCommands
notification-available-commands = 可用的指令如下：
description-no-description = 沒有可用的說明。
description-help-description = 提供各項指令的實用資訊。

## PlayerCommands
description-pause-description = 暫停或繼續遊戲。
description-surrender-description = 自毀一切並輸掉遊戲。

## DeveloperMode, GainsExperience, PowerManager
notification-cheat-used = 已使用作弊：{ $cheat }，由 { $player }{ $suffix }。

## DeveloperMode, DebugVisualizationCommands, CustomTerrainDebugOverlay, ActorMapOverlay, CellTriggerOverlay, ExitsDebugOverlayManager, HierarchicalPathFinderOverlay, PathFinderOverlay, TerrainGeometryOverlay
notification-cheat-enabled = 已啟用作弊：{ $cheat }，由 { $player }。
notification-cheat-disabled = 已停用作弊：{ $cheat }，由 { $player }。

## CustomTerrainDebugOverlay
description-custom-terrain-debug-overlay = 切換自訂地形除錯疊加層。

## CellTriggerOverlay
description-cell-triggers-overlay = 切換腳本觸發器疊加層。

## HierarchicalPathFinderOverlay
description-hpf-debug-overlay = 切換階層式尋路疊加層。

## PathFinderOverlay
description-path-debug-overlay = 切換尋路過程的視覺化。

## TerrainGeometryOverlay
description-terrain-geometry-overlay = 切換地形幾何疊加層。

## ActorMapOverlay
description-actor-map-overlay = 切換物件地圖疊加層。

## MapOptions, MissionBrowserLogic
options-game-speed =
    .slowest = 最慢
    .slower = 較慢
    .normal = 普通
    .fast = 快
    .faster = 較快
    .fastest = 最快

## TimeLimitManager
options-time-limit =
    .no-limit = 無限制
    .options = { $minutes -> [one] { $minutes } 分鐘 *[other] { $minutes } 分鐘 }

notification-time-limit-expired = 時間限制已到。

## EditorActorBrush
notification-added-actor = 已新增 { $name }（{ $id }）

## EditorCopyPasteBrush
notification-copied-tiles = 已複製 { $tiles } 個圖塊
notification-copied-actors = 已複製 { $actors } 個物件
notification-copied-tiles-actors = 已複製 { $tiles } 個圖塊與 { $actors } 個物件

## EditorDefaultBrush
notification-selected-area = 已選取區域 { $x },{ $y }（{ $width },{ $height }）
notification-removed-area = 已移除區域 { $x },{ $y }（{ $width },{ $height }）
notification-selected-actor = 已選取單位 { $id }
notification-cleared-selection = 已清除選取
notification-removed-actor = 已移除 { $name }（{ $id }）
notification-removed-resource = 已移除 { $type }
notification-moved-actor = 已將 { $id } 從 { $x1 },{ $y1 } 移至 { $x2 },{ $y2 }

## EditorResourceBrush
notification-added-resource = { $count -> [one] 已新增一格 { $type } *[other] 已新增 { $count } 格 { $type } }

## EditorTileBrush
notification-added-tile = 已新增圖塊 { $id }
notification-filled-tile = 已填入圖塊 { $id }

## EditorMarkerLayerBrush
notification-added-marker-tiles-markers =
    .red = 紅色
    .orange = 橘色
    .yellow = 黃色
    .green = 綠色
    .cyan = 青色
    .blue = 藍色
    .purple = 紫色
    .magenta = 洋紅色
notification-added-marker-tiles = { $count -> [one] 已新增 { $type } 標記圖塊 *[other] 已新增 { $count } 個 { $type } 標記圖塊 }
notification-removed-marker-tiles = { $count -> [one] 已移除標記圖塊 *[other] 已移除 { $count } 個標記圖塊 }
notification-cleared-selected-marker-tiles = { $count -> [one] 已清除 { $type } 標記圖塊 *[other] 已清除 { $count } 個 { $type } 標記圖塊 }
notification-cleared-all-marker-tiles = 已清除 { $count } 個標記圖塊

## EditorActionManager
notification-opened = 已開啟

## MapOverlaysLogic
mirror-mode =
    .none = 無
    .flip = 翻轉
    .rotate = 旋轉

## ActorEditLogic
notification-edited-actor = 已編輯 { $name }（{ $id }）
notification-edited-actor-id = 已編輯 { $name }（{ $old-id }-> { $new-id }）

## ConquestVictoryConditions, StrategicVictoryConditions
notification-player-is-victorious = { $player } 獲得勝利。
notification-player-is-defeated = { $player } 已被擊敗。

## OrderManager
notification-desync-compare-logs = 在第 { $frame } 幀失去同步。請與其他玩家比對 syncreport.log。

## WidgetUtils
label-win-state-won = 獲勝
label-win-state-lost = 落敗
label-client-state-disconnected = 已離開

## Player
enumerated-bot-name = { $name } { $number -> *[zero] {""} [other] { $number } }

## ModifiersExts
keycode-modifier =
    .alt = Alt
    .ctrl = Ctrl
    .meta = Meta
    .cmd = Cmd
    .shift = Shift
    .none = 無

## KeycodeExts
keycode =
    .unknown = 未定義
    .return = Enter
    .escape = Esc
    .backspace = 倒退
    .tab = Tab
    .space = 空白鍵
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
    .pause = 暫停
    .insert = 插入
    .home = 行首
    .pageup = 上一頁
    .delete = 刪除
    .end = 行尾
    .pagedown = 下一頁
    .right = 右
    .left = 左
    .down = 下
    .up = 上
    .numlockclear = Numlock
    .kp_divide = 數字鍵盤 /
    .kp_multiply = 數字鍵盤 *
    .kp_minus = 數字鍵盤 -
    .kp_plus = 數字鍵盤 +
    .kp_enter = 數字鍵盤 Enter
    .kp_1 = 數字鍵盤 1
    .kp_2 = 數字鍵盤 2
    .kp_3 = 數字鍵盤 3
    .kp_4 = 數字鍵盤 4
    .kp_5 = 數字鍵盤 5
    .kp_6 = 數字鍵盤 6
    .kp_7 = 數字鍵盤 7
    .kp_8 = 數字鍵盤 8
    .kp_9 = 數字鍵盤 9
    .kp_0 = 數字鍵盤 0
    .kp_period = 數字鍵盤 .
    .application = Application
    .power = Power
    .kp_equals = 數字鍵盤 =
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
    .execute = Execute
    .help = 說明
    .menu = 選單
    .select = 選取
    .stop = 停止
    .again = 再一次
    .undo = 復原
    .cut = 剪下
    .copy = 複製
    .paste = 貼上
    .find = 尋找
    .mute = 靜音
    .volumeup = 音量加
    .volumedown = 音量減
    .kp_comma = 數字鍵盤 ,
    .kp_equalsas400 = 數字鍵盤 (AS400)
    .alterase = AltErase
    .sysreq = SysReq
    .cancel = 取消
    .clear = Clear
    .prior = Prior
    .return2 = Enter
    .separator = Separator
    .out = Out
    .oper = Oper
    .clearagain = 清除 / 再一次
    .crsel = CrSel
    .exsel = ExSel
    .kp_00 = 數字鍵盤 00
    .kp_000 = 數字鍵盤 000
    .thousandsseparator = ThousandsSeparator
    .decimalseparator = DecimalSeparator
    .currencyunit = CurrencyUnit
    .currencysubunit = CurrencySubUnit
    .kp_leftparen = 數字鍵盤 (
    .kp_rightparen = 數字鍵盤 )
    .kp_leftbrace = 數字鍵盤 {"{"}
    .kp_rightbrace = 數字鍵盤 {"}"}
    .kp_tab = 數字鍵盤 Tab
    .kp_backspace = 數字鍵盤 倒退
    .kp_a = 數字鍵盤 A
    .kp_b = 數字鍵盤 B
    .kp_c = 數字鍵盤 C
    .kp_d = 數字鍵盤 D
    .kp_e = 數字鍵盤 E
    .kp_f = 數字鍵盤 F
    .kp_xor = 數字鍵盤 XOR
    .kp_power = 數字鍵盤 ^
    .kp_percent = 數字鍵盤 %
    .kp_less = 數字鍵盤 <
    .kp_greater = 數字鍵盤 >
    .kp_ampersand = 數字鍵盤 &
    .kp_dblampersand = 數字鍵盤 &&
    .kp_verticalbar = 數字鍵盤 |
    .kp_dblverticalbar = 數字鍵盤 ||
    .kp_colon = 數字鍵盤 :
    .kp_hash = 數字鍵盤 #
    .kp_space = 數字鍵盤 空白鍵
    .kp_at = 數字鍵盤 @
    .kp_exclam = 數字鍵盤 !
    .kp_memstore = 數字鍵盤 MemStore
    .kp_memrecall = 數字鍵盤 MemRecall
    .kp_memclear = 數字鍵盤 MemClear
    .kp_memadd = 數字鍵盤 MemAdd
    .kp_memsubtract = 數字鍵盤 MemSubtract
    .kp_memmultiply = 數字鍵盤 MemMultiply
    .kp_memdivide = 數字鍵盤 MemDivide
    .kp_plusminus = 數字鍵盤 +/-
    .kp_clear = 數字鍵盤 Clear
    .kp_clearentry = 數字鍵盤 ClearEntry
    .kp_binary = 數字鍵盤 Binary
    .kp_octal = 數字鍵盤 Octal
    .kp_decimal = 數字鍵盤 Decimal
    .kp_hexadecimal = 數字鍵盤 Hexadecimal
    .lctrl = 左 Ctrl
    .lshift = 左 Shift
    .lalt = 左 Alt
    .lgui = 左 GUI
    .rctrl = 右 Ctrl
    .rshift = 右 Shift
    .ralt = 右 Alt
    .rgui = 右 GUI
    .mode = ModeSwitch
    .audionext = AudioNext
    .audioprev = AudioPrev
    .audiostop = AudioStop
    .audioplay = AudioPlay
    .audiomute = AudioMute
    .mediaselect = MediaSelect
    .www = WWW
    .mail = Mail
    .calculator = 計算機
    .computer = 電腦
    .ac_search = AC Search
    .ac_home = AC Home
    .ac_back = AC Back
    .ac_forward = AC Forward
    .ac_stop = AC Stop
    .ac_refresh = AC Refresh
    .ac_bookmarks = AC Bookmarks
    .brightnessdown = BrightnessDown
    .brightnessup = BrightnessUp
    .displayswitch = DisplaySwitch
    .kbdillumtoggle = KBDIllumToggle
    .kbdillumdown = KBDIllumDown
    .kbdillumup = KBDIllumUp
    .eject = 退出
    .sleep = 睡眠
    .mouse4 = 滑鼠 4
    .mouse5 = 滑鼠 5

## MapGeneratorToolLogic
notification-map-generator-generated = 使用 { $name } 產生

dialog-notification-map-generator-failed =
    .title = 地圖產生失敗
    .prompt = 詳情請見 debug.log。
    .cancel = 關閉

## EditorTilingPathBrush
notification-tiling-path-started = 已開始路徑鋪設
notification-tiling-path-updated = 已更新路徑鋪設
notification-tiling-path-reset = 已捨棄路徑鋪設
notification-tiling-path-painted = 已繪製路徑鋪設
