# AudioManager API

## 功能

管理背景音乐和音效的播放、暂停、停止、音量控制、资源加载与释放。

## API

| 接口 | 功能 | 调用方 |
|-|-|-|
| InitializeAsync() | 异步初始化音频管理器，加载音频根节点并启动默认 BGM | 启动流程 |
| StartPlayBGM(string) | 通过名称加载并播放背景音乐，自动循环 | 游戏场景/系统 |
| StartPlayBGM(AudioClip) | 直接播放传入的背景音乐资源，自动循环 | 开发/测试 |
| StartPlaySound(string, bool, UnityAction<AudioSource>) | 通过名称加载并播放音效，可选择循环并支持回调 | 游戏事件/交互 |
| StartPlaySound(AudioClip, bool, UnityAction<AudioSource>) | 直接播放传入的音效资源，可选择循环 | 道具/事件/测试 |
| StopPlayBGM() | 停止播放背景音乐并清除 BGM clip | 游戏流程/切换 |
| PausePlayBGM() | 暂停当前背景音乐播放 | 界面/暂停 |
| ResumePlayBGM() | 恢复暂停的背景音乐播放 | 界面/暂停 |
| SetBGMVolume(float) | 设置背景音乐音量，范围 [0,1] | 音量设置 |
| SetSoundVolume(float) | 设置音效音量，范围 [0,1]，更新当前正在播放的音效 | 音量设置 |
| StopPlaySound(AudioSource) | 停止并销毁指定音效源 | 音效管理 |
| StopPlayAllSound() | 停止并销毁所有当前音效 | 场景切换/清理 |
| ReleaseAudioSources() | 释放已缓存的音频资源引用 | 退出/资源清理 |
| BgmVolume | 获取当前背景音乐音量 | 系统/UI |
| SfxVolume | 获取当前音效音量 | 系统/UI |
| IsInitialized | 判断音频管理器是否已初始化完成 | 启动流程/系统检查 |

## 调用规范

所有音频播放、暂停、停止和音量控制必须通过 AudioManager 进行。

通过名称播放的音频资源应使用对应的 Addressables 路径前缀：
- 背景音乐：`audio/bgm/`
- 音效：`audio/sfx/`

禁止：

- 直接操作 `AudioSource` 组件的创建、销毁、播放状态
- 直接修改 `AudioClip` 缓存字典 `audioResources`
- 在未初始化完成前调用播放或音量设置接口
- 忽略资源加载失败或初始化失败的情况
- 直接使用 `GameObject.Destroy` 销毁音效源而绕过管理器