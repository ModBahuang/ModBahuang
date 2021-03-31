use anyhow::{ensure, Context, Result};
use log::LevelFilter;
use regex::bytes::Regex;
use simplelog::{Config, TermLogger, TerminalMode};
use std::convert::TryInto;
use std::io::Read;
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

fn file_exists(f: impl AsRef<Path>) -> bool {
    f.as_ref().exists() && f.as_ref().is_file()
}

struct GameInfo {
    /// Path to `GameAssembly.Dll`
    game_assembly_dll: PathBuf,
    /// Path to `global-metadata.dat`
    global_metadata: PathBuf,
    /// Path to `resources.resource.resdata`
    encrypted_metadata: PathBuf,
}

impl GameInfo {
    fn detect(game_root: impl AsRef<Path>) -> Result<Self> {
        let game_root = game_root.as_ref();

        assert!(game_root.is_dir());

        let game_exe = game_root.join("guigubahuang.exe");
        ensure!(file_exists(&game_exe), "{:?} does not exist", game_exe);

        let game_assembly_dll = game_root.join("GameAssembly.dll");
        ensure!(
            file_exists(&game_assembly_dll),
            "{:?} does not exist",
            game_assembly_dll
        );

        let data_dir = game_root.join("guigubahuang_Data");

        let global_metadata = data_dir.join("il2cpp_data/Metadata/global-metadata.dat");
        ensure!(
            file_exists(&global_metadata),
            "{:?} does not exist",
            global_metadata
        );

        let encrypted_metadata = data_dir.join("resources.resource.resdata");
        ensure!(
            file_exists(&encrypted_metadata),
            "{:?} does not exist",
            encrypted_metadata
        );

        Ok(GameInfo {
            game_assembly_dll,
            global_metadata,
            encrypted_metadata,
        })
    }
}

fn search_key(game_info: &GameInfo) -> Result<[u8; 42]> {
    let regex = Regex::new(r"(?P<key>@F_Gs<>_\+\*\*-%322asAS\*]!%[0-9]{18})\x00")?;

    let bytes =
        std::fs::read(&game_info.game_assembly_dll).context("Unable to read `GameAssembly.dll`")?;

    let mut captures = regex.captures_iter(&bytes);

    let capture = captures.next().context("Key not found")?;
    ensure!(captures.next().is_none(), "Multiple key found");

    let key = capture
        .name("key")
        .expect("group `key` must exist")
        .as_bytes();

    log::debug!("key = {}", std::str::from_utf8(key).unwrap_or_default());

    key.try_into().context("Key malformed")
}

fn decrypt_metadata(game_info: &GameInfo, key: &[u8; 42]) -> Result<()> {
    let bytes = std::fs::read(&game_info.encrypted_metadata)
        .context("Unable to read encrypted metadata")?;
    let data = bytes.into_iter().skip(21);

    let data = data
        .enumerate()
        .map(|(i, b)| b.wrapping_sub(key[i % key.len()]))
        .collect::<Vec<u8>>();

    let sig = u32::from_le_bytes(data[..4].try_into().unwrap());

    // Check metadata signature
    ensure!(
        sig == 0xFAB11BAF,
        "Unexpected metadata signature(0x{:X?} vs 0xFAB11BAF)",
        sig
    );

    let f = &game_info.global_metadata;

    log::info!("备份原文件");
    let timestamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .expect("Time went backwards")
        .as_millis();

    let backup = f
        .parent()
        .expect("The path must have a parent")
        .join(format!("global-metadata.dat.{}.bak", timestamp));

    std::fs::rename(&game_info.global_metadata, &backup)
        .with_context(|| format!("Failed to rename {:?} to {:?}", f, backup))?;

    log::debug!("backup = {:?}", backup);

    log::info!("写出元数据");
    std::fs::write(&game_info.global_metadata, data)
        .with_context(|| format!("Failed to write data to {:?}", f))?;

    Ok(())
}

fn main() {
    fn inner() -> Result<()> {
        log::info!("读取游戏信息");
        let info = GameInfo::detect(
            std::env::current_dir().context("Unable to get game info in current directory.")?,
        )?;

        log::info!("查找解密密钥");
        let key = search_key(&info).context("Failed to find encryption key")?;

        log::info!("解密元数据");
        decrypt_metadata(&info, &key)
    }

    TermLogger::init(LevelFilter::max(), Config::default(), TerminalMode::Mixed).unwrap();

    match inner() {
        Ok(_) => {
            log::info!("完成")
        }
        Err(e) => {
            log::error!("{:?}", e)
        }
    }

    // Keep console alive
    let _ = std::io::stdin().read(&mut [0]).unwrap();
}
