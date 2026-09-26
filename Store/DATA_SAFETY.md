# Play Console — Data safety form answers

Use these answers when filling the **Data safety** section in Play Console
(App content → Data safety). Emberforge collects nothing.

| Question | Answer |
|---|---|
| Does your app collect or share any of the required user data types? | **No** |
| Is all of the user data collected by your app encrypted in transit? | Not applicable — no data transmitted |
| Do you provide a way for users to request that their data is deleted? | Not applicable — no data collected; uninstalling deletes all local saves |

## App content declarations

- **Privacy policy URL**: host `Store/PRIVACY_POLICY.md` (e.g. GitHub Pages on this repo) and paste the URL.
- **Ads**: No — the app contains no ads.
- **Target audience**: Everyone (no children-directed content flags needed).
- **News apps / COVID / health declarations**: not applicable.
- **Content rating (IARC)**: Everyone — no violence, no user interaction, no purchases, no sharing of user location.

## Release checklist

1. Host `PRIVACY_POLICY.md` somewhere public (Settings → Pages on GitHub works free).
2. Upload `Emberforge.aab` to a release track.
3. Fill Data safety with the table above.
4. Screenshots: `Store/screenshots/` + feature graphic `Store/feature_graphic.png`.
5. For production signing, build with the `EMBERFORGE_KEYSTORE` env vars set (see `BuildAndroid.cs`).
