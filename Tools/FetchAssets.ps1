$ErrorActionPreference = 'Continue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$root = 'E:\Idle Blacksmith Kimi'
$logFile = Join-Path $root 'Tools\fetch.log'
function Log($m) { $t = (Get-Date).ToString('HH:mm:ss'); "$t $m" | Out-File -FilePath $logFile -Append -Encoding utf8 }

'=== FetchAssets started ===' | Out-File $logFile -Encoding utf8

# --- Parse keys ---
$keys = @{}
Get-Content (Join-Path $root 'Game Dev Tools.txt') | ForEach-Object {
  if ($_ -match '^\s*([^:]+?)\s*:\s*(\S+)\s*$') { $keys[$matches[1].Trim()] = $matches[2].Trim() }
}
Log ("Keys parsed: " + ($keys.Keys -join ', '))

# --- 1. PrimeTween (embedded local package) ---
$ptDir = Join-Path $root 'Packages\com.kyrylokuzyk.primetween'
if (-not (Test-Path (Join-Path $ptDir 'package.json'))) {
  $tmp = Join-Path $root 'Tools\tmp\PrimeTween'
  Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
  git clone --depth 1 https://github.com/KyryloKuzyk/PrimeTween.git $tmp 2>&1 | Out-Null
  if (Test-Path (Join-Path $tmp 'package.json')) {
    New-Item -ItemType Directory -Force -Path $ptDir | Out-Null
    Copy-Item "$tmp\*" $ptDir -Recurse -Force
    Remove-Item (Join-Path $ptDir '.git') -Recurse -Force -ErrorAction SilentlyContinue
    Log 'PrimeTween embedded OK'
  } else { Log 'PrimeTween FAILED: no package.json at repo root' }
} else { Log 'PrimeTween already present' }

# --- 2. Font: Fredoka One (OFL) ---
$fontDir = Join-Path $root 'Assets\_IdleBlacksmith\Fonts'
New-Item -ItemType Directory -Force -Path $fontDir | Out-Null
$fontPath = Join-Path $fontDir 'FredokaOne-Regular.ttf'
if (-not (Test-Path $fontPath)) {
  try {
    Invoke-WebRequest 'https://raw.githubusercontent.com/google/fonts/main/ofl/fredokaone/FredokaOne-Regular.ttf' -OutFile $fontPath -UseBasicParsing -TimeoutSec 60
    Log ("Font OK (" + (Get-Item $fontPath).Length + ' bytes)')
  } catch { Log ("Font FAILED: " + $_.Exception.Message) }
} else { Log 'Font already present' }

# --- 3. Recraft UI icons ---
$iconDir = Join-Path $root 'Assets\_IdleBlacksmith\Art\Icons\Raw'
New-Item -ItemType Directory -Force -Path $iconDir | Out-Null
$recraft = $keys['Recraft ai']
$prefix = 'Cute casual mobile game UI icon, flat vector illustration, chunky rounded shapes, warm cozy blacksmith palette (cream, wood brown, honey gold, terracotta), soft subtle shading, single centered object, plain solid white background, no text, no watermark, subject: '
$icons = @(
  @{ name='coin';      prompt='a neat stack of shiny golden coins, one coin leaning against the stack with a star emblem' },
  @{ name='craft';     prompt='a sturdy blacksmith anvil with a wooden-handled hammer resting diagonally on top' },
  @{ name='carry';     prompt='a brown leather boot with a small cream wing on the side and tiny motion lines' },
  @{ name='rack';      prompt='a small wooden weapon rack holding two crossed shiny silver swords' },
  @{ name='helper';    prompt='smiling friendly blacksmith boy face, brown messy hair, leather headband, rosy cheeks' },
  @{ name='sound_on';  prompt='a speaker symbol with three curved sound waves' },
  @{ name='sound_off'; prompt='a speaker symbol with a small diagonal cross mark' },
  @{ name='logo';      prompt='a round badge emblem with a hammer crossed over a sword above a tiny anvil' },
  @{ name='dungeon';   prompt='a mysterious stone dungeon cave entrance arch with glowing blue crystals and a tiny warm torch' },
  @{ name='ore';       prompt='a glowing cyan magic crystal ore chunk embedded in a small dark rock' }
)
if (-not $recraft) { Log 'Recraft key missing, skipping icons' }
foreach ($ic in $icons) {
  $out = Join-Path $iconDir ($ic.name + '.png')
  if ((Test-Path $out) -and ((Get-Item $out).Length -gt 10000)) { Log ("icon " + $ic.name + ' already present'); continue }
  if (-not $recraft) { break }
  $ok = $false
  # Raster styles only: vector_illustration returns SVG bytes, which Unity cannot import.
  $attempts = @(
    @{ model='recraftv3'; style='digital_illustration' },
    @{ style='digital_illustration' },
    @{ model='recraftv3' },
    @{}
  )
  foreach ($a in $attempts) {
    try {
      $body = @{ prompt = ($prefix + $ic.prompt); size = '1024x1024'; n = 1 }
      foreach ($k in $a.Keys) { $body[$k] = $a[$k] }
      $json = $body | ConvertTo-Json
      $resp = Invoke-RestMethod -Uri 'https://external.api.recraft.ai/v1/images/generations' -Method Post -Headers @{ Authorization = "Bearer $recraft" } -ContentType 'application/json' -Body $json -TimeoutSec 240
      $url = $resp.data[0].url
      $b64 = $resp.data[0].b64_json
      if ($url) { Invoke-WebRequest $url -OutFile $out -UseBasicParsing -TimeoutSec 120 }
      elseif ($b64) { [IO.File]::WriteAllBytes($out, [Convert]::FromBase64String($b64)) }
      # Validate PNG magic bytes (89 50 4E 47); Recraft's CDN serves WebP by default,
      # so convert non-PNG payloads locally via Pillow.
      if (Test-Path $out) {
        $bb = [IO.File]::ReadAllBytes($out)
        $isPng = ($bb.Length -gt 4 -and $bb[0] -eq 0x89 -and $bb[1] -eq 0x50 -and $bb[2] -eq 0x4E -and $bb[3] -eq 0x47)
        if (-not $isPng -and $bb.Length -gt 1000) {
          & py -3 -c "from PIL import Image; Image.open(r'$out').convert('RGBA').save(r'$out')" 2>$null
          if (Test-Path $out) { $bb = [IO.File]::ReadAllBytes($out); $isPng = ($bb.Length -gt 4 -and $bb[0] -eq 0x89 -and $bb[1] -eq 0x50) }
        }
        if ($isPng -and $bb.Length -gt 10000) { Log ("icon " + $ic.name + " OK (attempt: " + ($a -join ' ') + ")"); $ok = $true; break }
        Remove-Item $out -Force -ErrorAction SilentlyContinue; Log ("icon " + $ic.name + " attempt returned unusable image, retrying")
      }
    } catch { Log ("icon " + $ic.name + " attempt failed: " + $_.Exception.Message) }
  }
  if (-not $ok) { Log ("icon " + $ic.name + " FAILED all attempts") }
}

# --- 4. ElevenLabs SFX ---
$audioDir = Join-Path $root 'Assets\_IdleBlacksmith\Audio'
New-Item -ItemType Directory -Force -Path $audioDir | Out-Null
$el = $keys['Eleven Labs']
if (-not $el) { $el = $env:ELEVENLABS_API_KEY }
$sfx = @(
  @{ name='hammer';  dur=0.8; prompt='single bright anvil hammer clang, metal strike on anvil, blacksmith forge, short and punchy' },
  @{ name='coin';    dur=0.7; prompt='short bright golden coin chime ding, cheerful casual game gold reward' },
  @{ name='pop';     dur=0.4; prompt='soft cute UI bubble pop click, very short, pleasant' },
  @{ name='upgrade'; dur=1.2; prompt='cheerful rising power-up sparkle arpeggio, casual game upgrade reward, magical chimes' },
  @{ name='hire';    dur=1.5; prompt='short happy cozy tavern fanfare flourish, cheerful bells, welcoming' },
  @{ name='denied';  dur=0.5; prompt='soft low wooden error thunk, gentle negative sound for casual game, not harsh' },
  @{ name='crackle'; dur=3.0; prompt='cozy fireplace fire crackling, soft warm crackle loop, no music, no wind' },
  @{ name='fanfare'; dur=2.0; prompt='triumphant short medieval fanfare, bright brass and bells, adventure quest complete reward, cheerful' }
)
if (-not $el) { Log 'ElevenLabs key missing, skipping sfx' }
foreach ($s in $sfx) {
  $out = Join-Path $audioDir ($s.name + '.mp3')
  if ((Test-Path $out) -and ((Get-Item $out).Length -gt 5000)) { Log ("sfx " + $s.name + ' already present'); continue }
  if (-not $el) { break }
  try {
    $json = @{ text = $s.prompt; duration_seconds = $s.dur; prompt_influence = 0.45 } | ConvertTo-Json
    Invoke-RestMethod -Uri 'https://api.elevenlabs.io/v1/sound-generation' -Method Post -Headers @{ 'xi-api-key' = $el } -ContentType 'application/json' -Body $json -OutFile $out -TimeoutSec 240
    Log ("sfx " + $s.name + " OK (" + (Get-Item $out).Length + " bytes)")
  } catch { Log ("sfx " + $s.name + " FAILED: " + $_.Exception.Message) }
}

Log '=== FetchAssets finished ==='
