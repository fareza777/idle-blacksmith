# Taste
- Communicates in casual Indonesian and expects replies in Indonesian. Confidence: 0.8
- Wants ambitious, full-featured scope rather than incremental tweaks — asks for "lengkap dan detail" / "komplit dan lengkap semua fitur" implementations (new systems, meta, content, progression, polish) when working on a project. Confidence: 0.7
- Comfortable delegating large multi-step tasks: gives a high-level goal with little spec and expects the agent to plan, make design decisions and carry it through autonomously. Confidence: 0.6
- Gives terse, symptom-only bug reports (e.g. "crash saat start") and expects the agent to investigate and find the root cause itself instead of asking for details. Confidence: 0.6
- Expects verification against the real running artifact rather than reasoning alone — points the agent at the connected device ("cek aja hp tercolok") and expects adb/logcat-style empirical diagnosis. Confidence: 0.7
- Keeps the project in a GitHub repository and expects the agent to handle git and push commits to the remote. Confidence: 0.6
- Prefers the agent to leverage the project's existing tooling/scripts (e.g. its asset-generation "game dev tools") as part of the work rather than ignoring them. Confidence: 0.55
