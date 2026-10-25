# Webworks 1.1.0

Self-hosted backend for Steamworks-style features, with drop-in shims for Unity
(Steamworks.NET and Facepunch), Unreal Engine and Godot.

| Path | What it is |
| --- | --- |
| `Server/` | The Node.js server |
| `Unity/Webworks.SteamworksNET/` | Steamworks.NET-shaped shim |
| `Unity/Webworks.Facepunch/` | Facepunch.Steamworks-shaped shim |
| `Unreal/WebworksSteam/` | Unreal Engine 5 plugin |
| `Godot/addons/webworks/` | Godot 4 addon, including the GodotSteam-compatible `Steam` singleton |

## Requirements

- Node.js 18 or newer for the server.
- Accounts use the public GWAS worker at `https://gwas.webeam.workers.dev` by default. 
- Optional: a Cloudflare tunnel for players outside your network.

## 1. Host the server

```bash
cd Server
npm install
npm start
```

On the first start the server writes `Server/webworks.config.json` and `Server/data/`, claims
its own server id, and asks once whether to open a Cloudflare quick tunnel. Answer `n` for a
local run, or `y` to get a temporary public `https://….trycloudflare.com` address.

Startup options:

```bash
npm start -- --port 3001                    # another port
npm start -- --host 0.0.0.0                 # every interface
npm start -- --tunnel                       # force a quick tunnel for this run
npm start -- --no-tunnel                    # force no tunnel
npm start -- --no-prompt                    # never ask about the tunnel
```

Environment variables:

| Variable | Effect |
| --- | --- |
| `WEBWORKS_PORT` | Port to listen on |
| `WEBWORKS_HOST` | Interface to bind |
| `WEBWORKS_DATA_DIR` | Where accounts, saves, stats and lobbies are stored (default `Server/data`) |
| `WEBWORKS_ACCOUNT_MODE=local` | Keep accounts in `Server/data/accounts.json` instead of GWAS; no claim, works offline |
| `WEBWORKS_GWAS_URL` | Use your own GWAS worker |
| `WEBWORKS_NETWORKING=local` | Keep all lobbies on this server |
| `WEBWORKS_PUBLIC_URL` | The public address other servers should dial (use instead of a tunnel) |
| `WEBWORKS_LOG=verbose` | Per-request logging |

Every configuration key is listed in `Server/webworks.config.example.json`; paths there are
relative to `Server/`.

Endpoints:

```text
http://localhost:3000/            status page
http://localhost:3000/health      health and live counts
http://localhost:3000/status      same data as JSON
http://localhost:3000/test        browser function tester and HTML5 arena
ws://localhost:3000/realtime      realtime channel for every shim
```



Point the server at it and restart:

```bash
cd ../Server
WEBWORKS_GWAS_URL=https://gwas.<your-subdomain>.workers.dev npm start
```

After a worker code change, `npx wrangler deploy` is the whole procedure. If you change
`schema.sql`, apply it with `npx wrangler d1 execute gwas --remote --file=schema.sql` and
deploy in the same step.

## 3. Unity — Steamworks.NET

1. Remove Steamworks.NET from the project (`com.rlabrecque.steamworks.net` and
   `Assets/Plugins/x86_64/steam_api*.dll`).
2. Copy the contents of `Unity/Webworks.SteamworksNET/Assets/` into your project's `Assets/`.
3. Edit `Assets/StreamingAssets/shim.config.txt`:

   ```ini
   server_url=http://localhost:3000
   engine_path=Unity/SteamworksNET
   request_timeout_ms=8000
   ```

   Use the LAN or tunnel address instead of `localhost` for players on other machines.
   Optional keys: `app_id=`, `persistent_id=true`, `identity_mode=`, `realtime_url=`,
   `accounts_worker_url=`, `device_key=`.
4. Optional account screen: add `Assets/Webworks/Bootstrap/WEBWORKS_BOOTSTRAP.unity` to Build
   Settings, set it as the first scene, and select your game scene in its **Scene To Load**
   field. Players who do not sign in continue as guests.
5. Optional overlay: add `Assets/Webworks/Overlay/WebworksOverlay.prefab` to a scene that
   survives loads. Shift+Tab opens it; `ActivateGameOverlay("Friends")` and the related calls
   open the matching panel.

## 4. Unity — Facepunch

1. Remove Facepunch.Steamworks from the project.
2. Copy the contents of `Unity/Webworks.Facepunch/Assets/` into your project's `Assets/`.
3. Set `engine_path=Unity/Facepunch` in `Assets/StreamingAssets/shim.config.txt` and the same
   `server_url` as above.
4. The bootstrap scene and overlay work the same way:
   `Assets/Webworks/Bootstrap/WEBWORKS_BOOTSTRAP.unity` and `Assets/Webworks/Overlay/`.

## 5. Unreal Engine 5

1. Copy `Unreal/WebworksSteam` into your project's `Plugins/` folder.
2. Enable **WebworksSteam** in the Plugins browser and restart the editor.
3. Add the server settings to `Config/DefaultGame.ini`:

   ```ini
   [/Script/WebworksSteam.WebworksSettings]
   ServerUrl=http://localhost:3000
   EnginePath=Unreal/Steam
   AppId=0
   bPersistAccountToken=True
   ```

`UWebworksSubsystem` is available from a Game Instance.

## 6. Godot 4

1. Remove the GodotSteam extension if it is installed (`addons/godotsteam` and its
   `project.godot` entry).
2. Copy `Godot/addons/webworks` into the project so the addon is at
   `res://addons/webworks/`.
3. Project → Project Settings → Plugins → enable **Webworks**. This adds the `Webworks` and
   `Steam` autoloads.
4. Copy `addons/webworks/webworks.cfg.example` to `res://webworks.cfg` and set the server.
   The URL must be quoted:

   ```ini
   [webworks]
   server_url="http://localhost:3000"
   app_id="480"
   engine_path="Godot/GodotSteam"
   timeout_ms=8000
   persona_name=""

   [steam]
   auto_init=true
   ```

   `user://webworks.cfg` is read as well, and takes priority. `[steam] auto_init=false` makes
   the shim wait for your own `Steam.init()` call.
5. Set `res://addons/webworks/ui/bootstrap.tscn` as the main scene, and pick your game scene
   in the **Scene To Load** field of the `Bootstrap` node in the Inspector.
6. Optional: `res://addons/webworks/ui/steam_overlay.tscn` is the overlay that
   `Steam.activateGameOverlay(...)` opens. Add it to a persistent scene to keep it loaded.

## 7. Cross-server lobbies

Cross-server lobbies work without configuration once accounts come from GWAS. Each node
server needs a public address that other servers can reach: accept the tunnel prompt, or set
`WEBWORKS_PUBLIC_URL=gwas.webeam.workers.dev`. Lobby traffic then goes directly between the two
node servers; the worker only introduces them.
