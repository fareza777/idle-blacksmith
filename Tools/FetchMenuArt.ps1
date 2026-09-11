# FetchMenuArt.ps1 — generates menu/splash/onboarding/dungeon artwork via Replicate (flux-schnell).
param(
  [switch]$Force,                 # regenerate even when the file already looks valid
  [string[]]$Only = @()           # only these asset names; empty = everything
)

$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
$logFile = Join-Path $PSScriptRoot 'menuart.log'
function Log($m) { $l = "$(Get-Date -Format 'HH.mm.ss') $m"; Add-Content $logFile $l; Write-Host $l }
function Wanted($name) { return ($Only.Count -eq 0) -or ($Only -contains $name) }
Set-Content $logFile "=== FetchMenuArt started ==="

$keys = @{}
foreach ($line in [IO.File]::ReadAllLines((Join-Path $root 'Game Dev Tools.txt'))) {
  if ($line -match '^\s*([^:=#][^:=]*?)\s*[:=]\s*(\S+)\s*$') { $keys[$matches[1].Trim()] = $matches[2].Trim() }
}
$tok = $keys['Replicate']; if (-not $tok) { $tok = $env:REPLICATE_API_TOKEN }
if (-not $tok) { Log 'Replicate token missing'; exit 1 }

$outDir = Join-Path $root 'Assets\_IdleBlacksmith\Art\Menu'
New-Item -ItemType Directory -Force $outDir | Out-Null

$noText = 'absolutely no text, no letters, no words, no watermark, no logo text'
$art = @(
  @{ name='emblem'; ratio='1:1'; prompt="Cute cozy RPG game emblem badge, crossed blacksmith hammer and silver sword over a tiny anvil, flat vector illustration, chunky rounded shapes, warm honey gold and wood brown palette, centered single object, plain solid white background, $noText" },
  @{ name='splash'; ratio='9:16'; prompt="Cozy fantasy blacksmith workshop key art for a mobile idle RPG, cute stylized illustration, warm glowing forge fire, wooden beams, swords on a rack, gold coins, soft evening light, inviting magical atmosphere, vertical portrait composition with calm empty space at the top third, rich detail, $noText" },
  @{ name='onboard_forge'; ratio='1:1'; prompt="Cute chibi blacksmith with leather apron hammering a glowing sword on an anvil, orange sparks flying, cozy warm forge glow behind, flat chunky illustration for mobile game onboarding, warm cozy palette, plain soft cream background, $noText" },
  @{ name='onboard_shop'; ratio='1:1'; prompt="Happy chibi customer buying a shiny silver sword across a wooden shop counter from a smiling chibi blacksmith, gold coins on the counter, cozy medieval shop interior, flat chunky illustration, warm palette, plain soft cream background, $noText" },
  @{ name='onboard_dungeon'; ratio='1:1'; prompt="Cute chibi adventurer holding a sword entering a glowing blue dungeon cave with torches and treasure chest, fireflies, flat chunky illustration for mobile game, adventurous mood, plain dark teal background, $noText" },
  @{ name='dungeon_bg'; ratio='9:16'; prompt="Mysterious fantasy dungeon corridor interior, glowing blue crystals in stone brick walls, warm torch light on the left, deep atmospheric perspective, vertical portrait composition for a mobile game panel background, flat stylized illustration, no characters, $noText" },

  # --- complex expansion ---
  @{ name='dungeon_banner'; ratio='16:9'; prompt="Wide horizontal banner of a fantasy dungeon entrance, stone archway with glowing blue crystals, warm torch light from the sides, treasure and old bones on the floor, dark atmospheric background, flat stylized illustration for a mobile game panel header, no characters, composition works when cropped to a short wide strip, $noText" },
  @{ name='menu_bg'; ratio='9:16'; prompt="Cozy fantasy blacksmith village at golden hour seen from above, warm glowing forge windows, a little mine entrance in a hillside, a market stall and a crooked enchanter tower with floating crystals, soft evening light, inviting magical atmosphere, vertical portrait composition with calm empty space in the middle, rich detail, flat stylized illustration, $noText" },
  @{ name='prestige_art'; ratio='1:1'; prompt="A glowing orange ember phoenix rising from a blacksmith anvil, sparks and embers swirling upward, dark background with a warm radiant glow, symbol of rebirth and renewal, flat stylized illustration, centered single subject, $noText" },

  # --- onboarding pages (3:2 so they sit in a wide card art box without cropping) ---
  @{ name='page_forge'; ratio='3:2'; prompt="Cute chibi blacksmith with leather apron hammering a glowing sword on an anvil, orange sparks flying, cozy warm forge glow, flat chunky illustration for a mobile game onboarding card, warm cozy palette, $noText" },
  @{ name='page_mine'; ratio='3:2'; prompt="Cute chibi miner with a pickaxe and a lantern digging glowing blue crystals inside a wooden mine tunnel, mine cart on rails, warm lantern light on cool blue stone, flat chunky illustration for a mobile game onboarding card, $noText" },
  @{ name='page_market'; ratio='3:2'; prompt="Happy chibi customer buying a shiny silver sword across a wooden shop counter from a smiling chibi blacksmith, gold coins and a striped awning, cozy medieval market stall, flat chunky illustration for a mobile game onboarding card, warm palette, $noText" },
  @{ name='page_dungeon'; ratio='3:2'; prompt="Three cute chibi adventurers with swords and torches walking toward a glowing blue dungeon gate archway, fireflies and treasure, adventurous mood, flat chunky illustration for a mobile game onboarding card, $noText" },
  @{ name='page_prestige'; ratio='3:2'; prompt="A blacksmith anvil surrounded by swirling orange embers and a bright glowing flame, magical runes on the ground, feeling of powerful rebirth and renewal, flat chunky illustration for a mobile game onboarding card, warm dramatic light, $noText" }
)

foreach ($a in $art) {
  $out = Join-Path $outDir ($a.name + '.png')
  if (-not (Wanted $a.name)) { continue }
  if ((Test-Path $out) -and ((Get-Item $out).Length -gt 100000) -and -not $Force) { Log "art $($a.name) already present"; continue }
  $ok = $false
  for ($try = 1; $try -le 2 -and -not $ok; $try++) {
    try {
      $body = @{ input = @{ prompt = $a.prompt; aspect_ratio = $a.ratio; output_format = 'png'; num_outputs = 1 } } | ConvertTo-Json -Depth 5
      $resp = Invoke-RestMethod -Uri 'https://api.replicate.com/v1/models/black-forest-labs/flux-schnell/predictions' -Method Post -Headers @{ Authorization = "Bearer $tok"; Prefer = 'wait=55' } -ContentType 'application/json' -Body $body -TimeoutSec 90
      $deadline = (Get-Date).AddSeconds(150)
      while ($resp.status -ne 'succeeded' -and $resp.status -ne 'failed' -and $resp.status -ne 'canceled' -and (Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 3
        $resp = Invoke-RestMethod -Uri $resp.urls.get -Headers @{ Authorization = "Bearer $tok" } -TimeoutSec 30
      }
      if ($resp.status -ne 'succeeded') { Log "art $($a.name) try $try status=$($resp.status)"; continue }
      $url = $resp.output; if ($url -is [array]) { $url = $url[0] }
      Invoke-WebRequest $url -OutFile $out -UseBasicParsing -TimeoutSec 120
      $b = [IO.File]::ReadAllBytes($out)
      if ($b.Length -gt 100000 -and $b[0] -eq 0x89 -and $b[1] -eq 0x50) { Log "art $($a.name) OK ($($b.Length) bytes)"; $ok = $true }
      else { Remove-Item $out -Force -ErrorAction SilentlyContinue; Log "art $($a.name) try $try invalid payload" }
    } catch { Log "art $($a.name) try $try failed: $($_.Exception.Message)" }
  }
  if (-not $ok) { Log "art $($a.name) FAILED" }
}
Log '=== FetchMenuArt finished ==='
