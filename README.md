# ModBahuang
Mods for 鬼谷八荒(guigubahuang / Tale of Immortal)

## Mods
### FemaleOnlyExist[^1]
使游戏内只刷新女性 NPC 


### NoMeansYes[^1]
使玩家对 NPC 的双修请求不会被拒绝

### Villain[^2]
使得可以转储或修改游戏内的配置文件，如文字、奇遇、物品、对话等。

[^1]: https://bbs.3dmgame.com/thread-6133942-1-1.html
[^2]: https://bbs.3dmgame.com/thread-6137320-1-1.html

## Build

### metadata_unpacker
0. [安装 Rust](https://www.rust-lang.org/tools/install)
1. `cd metadata_unpacker && cargo build`

### Mods
0. 将 Build 后的 metadata_unpacker 复制到游戏目录并运行。
1. 下载并安装 [MelonLoader](https://github.com/LavaGang/MelonLoader)。 注意，最小需求版本为 v0.3.0
2. 将 `<你的游戏目录>\MelonLoader\` 下的 `MelonLoader.dll` 文件以及 `Managed` 文件夹复制到 `Libs` 目录下。   注意，对于 `Villain`，你需要额外将 [`dnlib`](https://github.com/0xd4d/dnlib) 放入 `Managed` 文件夹。
3. 使用 `Visual Studio 2019` 打开 `toi.sln`。

## 贡献
你可以通过以下方式来参与到该项目：

- 如果在使用中遇到任何问题，请提交[ISSUE](https://github.com/lolligun/ModBahuang/issues)。
- 通过修改 BUG、添加功能或者完成代码中存在的 `TODO` 或者 `FIXME` 注释并提交 [PR](https://github.com/lolligun/ModBahuang/pulls)

## Credits
- [MelonLoader](https://github.com/LavaGang/MelonLoader)
- [Il2CppAssemblyUnhollower](https://github.com/knah/Il2CppAssemblyUnhollower)
- [https://github.com/Perfare/Il2CppDumper](https://github.com/Perfare/Il2CppDumper)

