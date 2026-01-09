# Deco Tools Helper
**Deco Tools Helper** is a lightweight, local companion application for **GW2DecoTools.com** and other privately developed Guild Wars 2 decoration tools.

It runs quietly in your system tray and provides safe, local access to:
- Live in-game position data (via MumbleLink)
- Selected Guild Wars 2 API endpoints
- Decoration unlock counts for Homesteads and Guild Halls

No browser extensions.
No exposed API keys.
No in-game configuration required.



## 🔹 What This Tool Is (In Simple Terms)
This helper acts as a **bridge** between:
- Your Guild Wars 2 game client
- Your web browser
- Tools like **GW2DecoTools.com**

Because browsers cannot directly access:
- GW2’s MumbleLink shared memory
- Local files
- Secure API keys

…this helper runs locally on your computer and safely exposes only what’s needed through `http://127.0.0.1`.



## 🟢 What It Enables

With the helper running, web tools can:
📍 Read your live in-game position
🗺️ Detect your current map
🧱 Check decoration unlock counts
🏠 Work with Homestead layouts
🏰 Work with Guild Hall layouts
🔁 Move decoration XML layouts directly to your character’s location

This is what powers features like:
- Interactive Move Tool
- Auto Map Swap
- Decoration availability checks


## 🚀 How to Use (For Players)
1️⃣ Run the Helper
- Launch DecoToolsHelper.exe
- The app starts in the system tray

2️⃣ First Launch Setup
On first run, a small window appears:
- Paste your GW2 API key (optional but recommended)
- Choose default save folders (optional)

Required API permissions (if used):
- Account
- Guilds
- Unlocks

3️⃣ Close the Window
You can safely close the window at any time.
The helper continues running in the system tray.

4️⃣ Use GW2DecoTools.com

Open GW2DecoTools.com in your browser.

If the helper is running:
- The site automatically detects it
- Live position & map features become available


## 🔍 Verifying It’s Working
You can manually check the helper by opening:
`http://127.0.0.1:61337/status`

Or view live Mumble data:
`http://127.0.0.1:61337/mumble`

When in-game on a live map, you should see:
`"available": true`


## 🔐 Security & Privacy
- API keys are stored locally only
- Keys are never sent to any external service
- The helper listens only on localhost
- No data leaves your computer unless you explicitly upload files

You can reset everything by:
**1.** Fully exiting the helper
**2.** Deleting config.json next to the EXE


===================================================================


# 🧑‍💻 Developer Documentation
This section is for developers who want to:
- Integrate with the helper
- Build their own decoration tools
- Extend or fork the project


## 🌐 Local Server Overview
The helper runs a local HTTP server at:
`http://localhost:61337`

All endpoints:
- Return JSON
- Are CORS-enabled for localhost
- Are read-only or strictly scoped


## 📡 Available Endpoints
### 🔹 Status
`GET /status`


Returns:
`{
  "running": true,
  "version": "1.1.0",
  "apiKeyPresent": true,
  "mumbleAvailable": true
}`


### 🔹 MumbleLink (Live Position)
`GET /mumble`

Returns when available:
`{
  "available": true,
  "mapId": 1234,
  "position": { "x": 10.5, "y": 3.2, "z": -1.1 }
}`

Used for:
- Player-relative placement
- Map detection
- Interactive move tools


### 🔹 Account Guild List
`GET /guilds`

Requires API key.
Returns:
`[
  { "id": "...", "name": "...", "tag": "ABC" }
]`


### 🔹 Homestead Decoration Unlocks
`GET /decos/homestead`
Requires API key.
Returns:
`{
  "12345": 3,
  "67890": 12
}`
Keys correspond to decoration IDs used in XML files.


### 🔹 Guild Decoration Storage (Targeted)
`POST /decos/guild/{guildId}`
Request body:
`{
  "ids": [123, 456, 789]
}`

Response:
`{
  "123": 1,
  "456": 0,
  "789": 2
}`


### 🔹 Decoration ID Variants
`GET /decorations`
**Used For:**
- Mapping <prop name=""> → correct target id
- Determining whether a decoration has
-- a Homestead counterpart
-- a Guild Hall counterpart
- Excluding decorations that cannot exist on the target map

Response:
`{
  "Decorations": [
    {
      "Name": "Basic Chair",
      "HomesteadId": 12345,
      "GuildUpgradeId": 67890
    }
  ]
}`


### 🔹 Decoration ID Lookup (optional / auxiliary)
`GET /decorations/lookup`
**Used For:**
- Debugging
- Validation



🔒 **Important:**
This endpoint intentionally:
- Requires explicit IDs
- Does NOT allow bulk scanning


## 🧠 Design Philosophy (For Contributors)
- Explicit > Automatic
- Safe > Convenient
- Local-first > Cloud
- Tools should never require developer-level setup for players

This helper exists to enable tools, not to be one itself.


## 🔗 Relationship to GW2DecoTools.com
- GW2DecoTools.com is a browser-based UI
- Deco Tools Helper provides local capabilities browsers cannot access
- The two are designed to work together, but are not tightly coupled

You are free to:
- Build your own tools against these endpoints
- Fork the helper
- Extend it for other GW2 tooling needs


