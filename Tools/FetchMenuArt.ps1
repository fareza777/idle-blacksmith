# FetchMenuArt.ps1 — generates menu/splash/onboarding/dungeon artwork via Replicate (flux-schnell).
$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
$logFile = Join-Path $PSScriptRoot 'menuart.log'
function Log($m) { $l = "$(Get-Date -Format 'HH.mm.ss') $m"; Add-Content $logFile $l; Write-Host $l }
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
  @{ name='dungeon_bg'; ratio='9:16'; prompt="Mysterious fantasy dungeon corridor interior, glowing blue crystals in stone brick walls, warm torch light on the left, deep atmospheric perspective, vertical portrait composition for a mobile game panel background, flat stylized illustration, no characters, $noText" }
)

foreach ($a in $art) {
  $out = Join-Path $outDir ($a.name + '.png')
  if ((Test-Path $out) -and ((Get-Item $out).Length -gt 100000)) { Log "art $($a.name) already present"; continue }
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
