using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Story director. Watches game state on a slow poll and plays each dialogue sequence
    /// exactly once, persisting seen ids in the save. Polling (rather than events) means the
    /// beats fire in a sensible order even when several conditions flip in the same frame,
    /// and they always wait until nothing else is on screen.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [System.Serializable]
        public class NamedSprite
        {
            public string key;
            public Sprite sprite;
        }

        [Tooltip("portrait_bram/petra/sable/aldric/nyx sprites, filled by the scene builder")]
        public NamedSprite[] portraits;

        [Tooltip("Seconds between trigger checks")]
        public float pollSeconds = 0.75f;

        /// <summary>Filled by the scene builder; falls back to the singleton.</summary>
        public UIManager ui;

        DialogueSequence[] script;
        DialogueSequence playing;
        readonly System.Collections.Generic.Queue<DialogueSequence> queue =
            new System.Collections.Generic.Queue<DialogueSequence>();
        float timer;

        void Awake()
        {
            Instance = this;
            script = Story();
        }

        Sprite Portrait(string key)
        {
            if (portraits != null)
                foreach (NamedSprite p in portraits)
                    if (p != null && p.key == key) return p.sprite;
            return null;
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = pollSeconds;
            Scan();
        }

        /// <summary>Evaluates every unseen beat; the first whose gate passes is queued.</summary>
        void Scan()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Data == null || script == null) return;
            var ui = Ui();
            if (ui == null || ui.dialoguePanel == null) return;
            // Never interrupt a modal — a queued beat can wait one more poll.
            if (ui.AnyPanelOpen || ui.dialoguePanel.IsOpen || ui.IntroPlaying) return;
            if (!gm.Data.introSeen) return; // the intro cinematic comes before any story
            if (!gm.HasSeenOnboarding) return; // land the tutorial first

            foreach (DialogueSequence seq in script)
            {
                if (seq == null || gm.Data.seenDialogues.Contains(seq.id)) continue;
                if (!Gate(seq.id, gm)) continue;
                gm.Data.seenDialogues.Add(seq.id);
                queue.Enqueue(seq);
                break; // one per poll so simultaneous unlocks read as a conversation, not a pile
            }
            Pump();
        }

        void Pump()
        {
            var ui = Ui();
            if (playing != null || queue.Count == 0 || ui == null || ui.dialoguePanel == null) return;
            playing = queue.Dequeue();
            ui.dialoguePanel.Play(playing, () =>
            {
                playing = null;
                timer = 0.4f; // breathe between queued beats
            });
        }

        /// <summary>What has to be true in the world before a beat may play.</summary>
        static bool Gate(string id, GameManager gm)
        {
            StatBlock s = gm.Data.stats;
            switch (id)
            {
                case "wake": return true;
                case "first_sword": return s != null && s.swordsForged >= 1;
                case "first_sale": return s != null && s.swordsSold >= 1;
                case "helper": return gm.HelperUnlocked;
                case "mine": return gm.buildings.GetLevel(BuildingId.Mine) >= 1;
                case "smithy2": return gm.buildings.GetLevel(BuildingId.Smithy) >= 2;
                case "market": return gm.buildings.GetLevel(BuildingId.Market) >= 1;
                case "gate": return gm.buildings.GetLevel(BuildingId.Gate) >= 1;
                case "expedition": return s != null && s.expeditionsClaimed >= 1;
                case "sanctum": return gm.buildings.GetLevel(BuildingId.Sanctum) >= 1;
                case "furnace": return gm.buildings.GetLevel(BuildingId.Furnace) >= 1;
                case "order": return s != null && s.ordersServed >= 1;
                case "rush": return s != null && s.rushOrdersDone >= 1;
                case "daily": return s != null && s.dailyClaims >= 2;
                case "rare": return s != null && s.bestRarity >= (int)Rarity.Epic;
                case "smithy5": return gm.buildings.GetLevel(BuildingId.Smithy) >= 5;
                case "rekindle": return gm.prestige != null && gm.prestige.Count >= 1;
                default: return false;
            }
        }

        /// <summary>Preview/test hook: play a beat by id ignoring gates and the seen list.</summary>
        public void PreviewPlay(string id)
        {
            var ui = Ui();
            if (ui == null || ui.dialoguePanel == null || script == null) return;
            foreach (DialogueSequence s in script)
                if (s != null && s.id == id)
                {
                    ui.dialoguePanel.Play(s, null);
                    return;
                }
        }

        UIManager Ui() => ui != null ? ui : UIManager.Instance;

        DialogueLine Line(string speaker, string portrait, string text)
            => new DialogueLine { speaker = speaker, text = text, portrait = Portrait(portrait) };

        static DialogueSequence Seq(string id, params DialogueLine[] lines)
            => new DialogueSequence { id = id, lines = lines };

        /// <summary>The cast and the script — portraits resolved on this manager.</summary>
        DialogueSequence[] Story() => new[]
        {
            Seq("wake",
                Line("Bram Ironroot", "bram", "Cold hearth. Empty racks. Just like grandfather left it."),
                Line("Bram Ironroot", "bram", "But the Ember still sleeps in that anvil. I can feel it."),
                Line("Bram Ironroot", "bram", "So — the old vow stands. Strike iron until this forge becomes a legend again.")),

            Seq("first_sword",
                Line("Bram Ironroot", "bram", "Ha! First blade off the anvil — crooked tip, honest weight."),
                Line("Petra Flint", "petra", "I heard the hammer from the ridge! Keep that rhythm and I'll keep the ore coming.")),

            Seq("first_sale",
                Line("Sable", "sable", "A sale already? Oh, I like a smith who earns."),
                Line("Sable", "sable", "Rarer blades bring rarer purses, darling. Fill that rack and watch them queue.")),

            Seq("helper",
                Line("Bram Ironroot", "bram", "Two hammers, one song. Best coin I ever spent."),
                Line("Petra Flint", "petra", "Your apprentice swings true, Bram. We'll have the yard humming by winter.")),

            Seq("mine",
                Line("Petra Flint", "petra", "She's open! The old veins never really dried, you know."),
                Line("Petra Flint", "petra", "Ore while you sleep, ore while you eat. Just keep my lanterns lit.")),

            Seq("smithy2",
                Line("Bram Ironroot", "bram", "Second level and the rafters barely held. This place wants to grow."),
                Line("Sable", "sable", "Bigger forge, bigger legend, bigger cut for me. Everybody wins.")),

            Seq("market",
                Line("Sable", "sable", "My stall, your blades — a beautiful arrangement."),
                Line("Sir Aldric", "aldric", "Soldiers pass through weekly, smith. They'll pay for steel that holds.")),

            Seq("gate",
                Line("Sir Aldric", "aldric", "You reopened the Gate? Bold. Relic ore sleeps below — and so do older things."),
                Line("Sir Aldric", "aldric", "Send my people down with sharp swords and they'll come back with richer pockets.")),

            Seq("expedition",
                Line("Sir Aldric", "aldric", "First delve returned. That blue ore is worth ten times its weight in the right steel."),
                Line("Nyx", "nyx", "Relic ore hums with old fire. Bring it to me, smith... I know what it wants to become.")),

            Seq("sanctum",
                Line("Nyx", "nyx", "A tower of my own. You build quickly for a mortal."),
                Line("Nyx", "nyx", "Runes, talents, second chances — magic has a price, but I invoice monthly.")),

            Seq("rare",
                Line("Nyx", "nyx", "That blade sang when it left the anvil. Epic work — your hands remembered something old."),
                Line("Bram Ironroot", "bram", "Felt it too. Like the forge leaned in to help.")),

            Seq("smithy5",
                Line("Bram Ironroot", "bram", "Five levels. Grandfather's forge was never half this size."),
                Line("Sable", "sable", "Half the kingdom talks about the forge on the hill now. Keep them talking.")),

            Seq("rekindle",
                Line("Nyx", "nyx", "You fed a whole legend to the Ember and it gave you shards. Beautiful."),
                Line("Bram Ironroot", "bram", "Every rekindle the fire burns brighter. We'll build it back — taller this time.")),

            Seq("furnace",
                Line("Bram Ironroot", "bram", "Brick, clay and a hungry mouth of fire. The old furnace breathes again."),
                Line("Petra Flint", "petra", "Forced air, smith! Steel in half the time — the old miners called that a dragon's lung.")),

            Seq("order",
                Line("Sir Aldric", "aldric", "First order filled and the patron paid smiling. Reputation travels faster than any cart."),
                Line("Sable", "sable", "Bulk buyers, premium prices. I taught you well — don't forget my ten percent. (I jest. Mostly.)")),

            Seq("rush",
                Line("Sable", "sable", "Rush hour and you kept up! Half the village left carrying steel."),
                Line("Bram Ironroot", "bram", "When the bell rings, we strike faster. That's the forge's heartbeat, lad.")),

            Seq("daily",
                Line("Nyx", "nyx", "The Ember remembers every day you return to it. Faithfulness has a flavor, smith — it tastes like relics."),
                Line("Bram Ironroot", "bram", "A forge feeds the hand that feeds it. Come back tomorrow; she'll keep the ember warm.")),
        };
    }
}
