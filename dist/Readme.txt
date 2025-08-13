
--| Nuxe 1.0.0
--| https://github.com/JKAnderson/Nuxe
--| By TKGP https://tkgp.neocities.org/

A gamedata unpacker and loose-loading patcher for FromSoftware games; the spiritual successor to UXM.

Nuxe supports all FromSoftware games that use large BinderLight archives:

- Armored Core V (PS3, X360)
- Armored Core Verdict Day (PS3, X360)
- Armored Core VI (PC)
- Dark Souls (PC, PS3, X360)
- Dark Souls Remastered (NS)
- Dark Souls II (PC)
- Dark Souls II Scholar of the First Sin (PC)
- Dark Souls III (PC)
- Elden Ring (PC)
- Elden Ring Nightreign (PC)
- Sekiro + Digital Artwork & Mini Soundtrack (PC)
- Steel Battalion Heavy Armor (X360)

Unpacking is available for all games; exe patching is only available for PC games, at the moment.

If you find this tool useful, you can support me on Patreon:
https://www.patreon.com/c/TKGP


Usage (Basic)
-------------

The Basic tab in the main window uses sensible default options for most people and should be very familiar to users of UXM. Before doing anything else, use the Browse button to locate your game's main executable:
- DARKSOULS.exe, eldenring.exe, etc. for PC games
- EBOOT.BIN for PS3 games
- default.xex for X360 games
- main for NS games

The Unpack button will extract all individual game files from the large archives named dvdbnd.bhd/bdt, Data.bhd/bdt, or similar. Files that already exist will be skipped.

The Patch button will patch the exe to load those individual files directly, instead of from the archives. Patching only needs to be performed once, not every time you modify files.

The Restore button will delete all extracted files and restore a backup of the executable if patched. For games that are still being updated, it's recommended to perform a Restore, then verify integrity in Steam and unpack/patch again after an update to ensure you have the latest files.

The Abort button will cancel any operation in progress.


Usage (Advanced)
----------------

The Advanced tab exposes additional options for more particular use cases and requires manual specification of which game type to use.

For Unpacking (and Restoring and Decrypting), select the game directory directly; this would be the directory containing the game executable, but it isn't required to actually contain an executable, and missing binders will be skipped instead of producing an error.

For Patching, you can select any file, whether or not it's named correctly or found in a complete game directory.

The Decrypt tab, only available in Advanced mode, will decrypt each binder header (if encrypted) and create a decrypted copy named *-dec.bhd in the same directory.


Notes for specific games
------------------------

- Dark Souls: Prepare to Die Edition (PC)
Nuxe supports unpacking and patching PTDE, but it uses the more direct style of modern games, leaving all files in compressed form.
This means that it is NOT compatible with mods created for/with UDSFM or UXM Selective, nor with tools that expect the legacy format (which, at least at the moment, will be almost all of them).
It's up to other authors whether they want to migrate to the modern format, but please don't bug them about it; just keep using UDSFM or UXM Selective if you need to.

- Sekiro (PC)
Sekiro uses SteamDRM, which means that patching the exe will just cause it to crash when started.
If you need to patch Sekiro, use a tool like Steamless to strip out the SteamDRM first.

- Armored Core VI, Elden Ring, Elden Ring Nightreign (PC)
For these games, patching mostly works, but doesn't support files in the "sd" folder.
