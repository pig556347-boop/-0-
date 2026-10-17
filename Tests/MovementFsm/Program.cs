using System;
using System.Collections.Generic;
using SchoolRuleZero;

internal static class Program
{
    private sealed class Context : IMovementStateContext
    {
        public readonly List<bool> Calls = new List<bool>();
        public void ApplyPlanarMovement(bool useCrouchSpeed) => Calls.Add(useCrouchSpeed);
    }

    private static int passed;

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Test(string name, Action test)
    {
        test();
        passed++;
        Console.WriteLine("PASS " + name);
    }

    private static void Main()
    {
        var origins = new[]
        {
            (MovementStateId.IDLE, new MovementFrame(true, false, false, 0)),
            (MovementStateId.MOVE, new MovementFrame(true, false, true, 0)),
            (MovementStateId.JUMP, new MovementFrame(false, false, true, 7)),
            (MovementStateId.FALL, new MovementFrame(false, false, false, -2)),
            (MovementStateId.CROUCH, new MovementFrame(true, true, true, 0))
        };
        // Explicit expected results cover landing, apex, ledge and posture priorities.
        var targets = new[]
        {
            ("idle", MovementStateId.IDLE, new MovementFrame(true, false, false, 0)),
            ("move", MovementStateId.MOVE, new MovementFrame(true, false, true, 0)),
            ("crouch-idle", MovementStateId.CROUCH, new MovementFrame(true, true, false, 0)),
            ("crouch-move", MovementStateId.CROUCH, new MovementFrame(true, true, true, 0)),
            ("ascending", MovementStateId.JUMP, new MovementFrame(false, false, true, 7)),
            ("descending", MovementStateId.FALL, new MovementFrame(false, false, false, -2)),
            ("apex", MovementStateId.FALL, new MovementFrame(false, false, true, 0)),
            ("crouched-ledge", MovementStateId.FALL, new MovementFrame(false, true, true, -1))
        };
        foreach (var origin in origins)
        foreach (var target in targets)
        {
            Test(origin.Item1 + " -> " + target.Item1, () =>
            {
                var context = new Context();
                var machine = new PlayerMovementStateMachine(context);
                machine.FixedTick(origin.Item2);
                context.Calls.Clear();
                int events = 0;
                machine.StateChanged += (previous, next) =>
                {
                    events++;
                    Assert(previous == origin.Item1, "Wrong previous state");
                    Assert(next == target.Item2, "Wrong next state");
                    Assert(machine.CurrentState == next, "Observer saw stale state");
                    Assert(context.Calls.Count == 1, "Notification preceded motion");
                };
                machine.FixedTick(target.Item3);
                Assert(machine.CurrentState == target.Item2, "Wrong resolved state");
                Assert(events == (origin.Item1 == target.Item2 ? 0 : 1), "Duplicate/missing event");
                Assert(context.Calls.Count == 1, "Not exactly one movement tick");
                Assert(context.Calls[0] == (target.Item2 == MovementStateId.CROUCH), "Wrong speed policy");
            });
        }

        Test("jump lifecycle, landing and blocked stand snapshot", () =>
        {
            var context = new Context();
            var machine = new PlayerMovementStateMachine(context);
            var observed = new List<MovementStateId>();
            machine.StateChanged += (_, next) => observed.Add(next);
            machine.FixedTick(new MovementFrame(true, false, true, 0));
            machine.FixedTick(new MovementFrame(false, false, true, 7));
            machine.FixedTick(new MovementFrame(false, false, true, 3));
            machine.FixedTick(new MovementFrame(false, false, true, 0));
            machine.FixedTick(new MovementFrame(true, true, true, 0));
            machine.FixedTick(new MovementFrame(true, true, false, 0));
            machine.FixedTick(new MovementFrame(true, false, false, 0));
            var expected = new[] { MovementStateId.MOVE, MovementStateId.JUMP,
                MovementStateId.FALL, MovementStateId.CROUCH, MovementStateId.IDLE };
            Assert(observed.Count == expected.Length, "Unexpected repeated transitions");
            for (int i = 0; i < expected.Length; i++)
                Assert(observed[i] == expected[i], "Lifecycle order mismatch");
        });

        Test("reject null context", () =>
        {
            bool rejected = false;
            try { new PlayerMovementStateMachine(null); }
            catch (ArgumentNullException) { rejected = true; }
            Assert(rejected, "Null context was accepted");
        });

        Test("reject recursive ticks without duplicate movement", () =>
        {
            var context = new Context();
            var machine = new PlayerMovementStateMachine(context);
            bool rejected = false;
            machine.StateChanged += (_, next) =>
            {
                try { machine.FixedTick(new MovementFrame(true, false, false, 0)); }
                catch (InvalidOperationException) { rejected = true; }
            };
            machine.FixedTick(new MovementFrame(true, false, true, 0));
            Assert(rejected && context.Calls.Count == 1, "Reentrant tick advanced physics");
            Assert(machine.CurrentState == MovementStateId.MOVE, "Reentrant tick changed state");
        });

        Test("recover tick guard after observer exception", () =>
        {
            var context = new Context();
            var machine = new PlayerMovementStateMachine(context);
            Action<MovementStateId, MovementStateId> listener = (_, next) =>
                throw new InvalidOperationException("Test observer exception");
            machine.StateChanged += listener;
            bool propagated = false;
            try { machine.FixedTick(new MovementFrame(true, false, true, 0)); }
            catch (InvalidOperationException) { propagated = true; }
            machine.StateChanged -= listener;
            machine.FixedTick(new MovementFrame(true, false, false, 0));
            Assert(propagated && machine.CurrentState == MovementStateId.IDLE,
                "Observer exception hid failure or permanently locked the FSM");
        });

        Test("jump release shortens ascent only", () =>
        {
            Assert(JumpRules.CancelAscent(8, true, .5f) == 4, "Ascent not shortened");
            Assert(JumpRules.CancelAscent(-3, true, .5f) == -3, "Fall was modified");
            Assert(JumpRules.CancelAscent(8, false, .5f) == 8, "Held jump was modified");
            Assert(JumpRules.CancelAscent(0, true, .5f) == 0, "Apex was modified");
        });
        Test("damage clamps HP and death rejects further hits", () =>
        {
            var health = new CombatHealth(100);
            Assert(health.Apply(12).Damage == 12 && health.Current == 88, "Damage mismatch");
            Assert(health.Apply(200).Damage == 88 && !health.Alive, "HP underflow");
            Assert(health.Apply(10).Kind == DamageKind.Ignored, "Dead actor received hit");
            health.Reset(); Assert(health.Current == 100, "Reset failed");
        });
        Test("invalid damage cannot heal or poison HP", () =>
        {
            var health = new CombatHealth(100);
            foreach (float damage in new[] { -1f, 0, float.NaN, float.PositiveInfinity })
                Assert(health.Apply(damage).Kind == DamageKind.Ignored, "Invalid damage accepted");
            Assert(health.Current == 100, "Invalid damage modified HP");
        });
        Test("guard parry dodge and rear attack priorities", () =>
        {
            var health = new CombatHealth(100);
            Assert(health.Apply(20, invulnerable: true).Kind == DamageKind.Ignored, "Dodge failed");
            Assert(health.Apply(20, guarding: true, parry: true).Kind == DamageKind.Parried, "Parry failed");
            Assert(health.Current == 100, "Avoided hit dealt damage");
            Assert(health.Apply(20, guarding: true).Damage == 5, "Guard chip mismatch");
            Assert(health.Apply(20, guarding: true, frontal: false).Kind == DamageKind.Hit, "Rear block");
            Assert(health.Apply(20, guarding: true, parry: true, unblockable: true).Damage == 20,
                "Grab incorrectly blocked");
            Assert(health.Apply(20, invulnerable: true, unblockable: true).Kind == DamageKind.Ignored,
                "Grab bypassed dodge invulnerability");
        });
        Test("confirmed combo expires and damage interrupts it", () =>
        {
            var progress = new CombatProgress();
            progress.ConfirmHit(0); progress.ConfirmHit(.5f);
            Assert(progress.Combo == 2, "Combo did not accumulate");
            progress.Tick(2); Assert(progress.Combo == 0, "Combo failed to expire");
            progress.ConfirmHit(3); Assert(progress.Combo == 1, "Combo did not restart");
            progress.BreakCombo(); Assert(progress.Combo == 0, "Hit did not break combo");
        });
        Test("ultimate exact thresholds, cap and one-time consumption", () =>
        {
            var progress = new CombatProgress();
            Assert(progress.ConsumeUltimate() == 0, "Empty ultimate activated");
            progress.ConfirmHit(0, gain: 32); Assert(progress.UltimateTier == 0, "32 tier");
            progress.ConfirmHit(0, gain: 1); Assert(progress.UltimateTier == 1, "33 tier");
            progress.ConfirmHit(0, gain: 32); Assert(progress.UltimateTier == 1, "65 tier");
            progress.ConfirmHit(0, gain: 1); Assert(progress.UltimateTier == 2, "66 tier");
            progress.ConfirmHit(0, gain: 33); Assert(progress.UltimateTier == 2, "99 tier");
            progress.ConfirmHit(0, gain: 100); Assert(progress.Gauge == 100 && progress.UltimateTier == 3, "Cap");
            Assert(progress.ConsumeUltimate() == 3 && progress.Gauge == 0, "Consume failed");
            Assert(progress.ConsumeUltimate() == 0, "Double ultimate spent");
        });
        Test("tutorial cannot advance before opening or with wrong action", () =>
        {
            var tutorial = new TutorialProgress();
            Assert(!tutorial.Accept(TutorialSignal.Moved), "Opening was bypassed");
            tutorial.FinishOpening();
            Assert(!tutorial.Accept(TutorialSignal.UltimateTierThreeHit), "Tutorial steps skipped");
            Assert(tutorial.Step == TutorialStep.Move, "Wrong signal changed step");
        });
        Test("full tutorial requires each action through Q impact", () =>
        {
            var tutorial = new TutorialProgress(); tutorial.FinishOpening();
            var signals = new[] { TutorialSignal.Moved, TutorialSignal.Jumped,
                TutorialSignal.Crouched, TutorialSignal.BasicHit, TutorialSignal.HeavyHit,
                TutorialSignal.Guarded, TutorialSignal.Parried, TutorialSignal.Dodged,
                TutorialSignal.Punched, TutorialSignal.FullyCharged, TutorialSignal.UltimateTierThreeHit };
            foreach (var signal in signals) Assert(tutorial.Accept(signal), "Valid action not accepted");
            Assert(tutorial.Step == TutorialStep.Complete, "Tutorial did not finish");
            Assert(!tutorial.Accept(TutorialSignal.Moved), "Completed tutorial advanced again");
            tutorial.FinishOpening(); Assert(tutorial.Step == TutorialStep.Complete, "Opening reset completion");
        });
        Test("four distinct defeats ignore training, duplicates and invalid targets", () =>
        {
            var encounter = new EncounterProgress();
            Assert(!encounter.RecordDefeat(0) && encounter.Count == 0, "Practice kill counted");
            encounter.Begin();
            Assert(!encounter.RecordDefeat(-1) && !encounter.RecordDefeat(4), "Unknown enemy counted");
            Assert(encounter.RecordDefeat(0) && !encounter.RecordDefeat(0), "Duplicate death counted");
            encounter.Begin(); Assert(encounter.Count == 1, "Repeated start reset progress");
            for (int id = 1; id < 4; id++) Assert(encounter.RecordDefeat(id), "Distinct enemy ignored");
            Assert(encounter.Complete && encounter.Count == 4 && !encounter.RecordDefeat(0), "Quest cap");
        });
        Test("basic combo lesson requires three timely hits and resets after interruption", () =>
        {
            var lesson = new TutorialComboLesson();
            Assert(!lesson.ConfirmBasicHit(0) && !lesson.ConfirmBasicHit(.4f), "Lesson completed early");
            Assert(lesson.ConfirmBasicHit(.8f), "Third hit failed lesson");
            lesson.Tick(3); Assert(lesson.Hits == 0, "Expired count stayed visible");
            Assert(!lesson.ConfirmBasicHit(4), "Expired chain continued");
            lesson.Break(); Assert(lesson.Hits == 0, "Damage did not interrupt lesson");
        });
        Test("campaign matches all eleven approved stage populations and boss placement", () =>
        {
            int[] expected = { 3, 4, 4, 5, 8, 10, 10, 12, 12, 11, 16 };
            int total = 0, areas = 0, bosses = 0;
            for (int stage = 1; stage <= SchoolCampaign.LastStage; stage++)
            {
                int stageTotal = 0;
                for (int area = 1; area <= SchoolCampaign.AreaCount(stage); area++)
                {
                    int count = SchoolCampaign.EnemyCount(stage, area);
                    stageTotal += count; areas++;
                    if (SchoolCampaign.IsBossRound(stage, area))
                    { bosses++; Assert(count == 1, "Boss not alone"); }
                }
                Assert(stageTotal == expected[stage-1], "Wrong population for stage " + stage);
                total += stageTotal;
            }
            Assert(total == 95 && areas == 22 && bosses == 3, "Campaign totals");
            Assert(SchoolCampaign.BossName(5) == "도재학" && SchoolCampaign.BossName(10) == "한승빈" &&
                SchoolCampaign.BossName(11) == "황빈", "Boss order");
            Assert(SchoolCampaign.HP(5, true) == 300 && SchoolCampaign.HP(10, true) == 300 &&
                SchoolCampaign.HP(11, true) == 360, "Boss HP");
        });
        Test("opening five stages introduce pressure with readable warnings and live caps", () =>
        {
            for (int stage = 1; stage <= 11; stage++)
            {
                Assert(SchoolCampaign.Telegraph(stage) == (stage <= 5), "Wrong telegraph stage");
                Assert(SchoolCampaign.VisibleLimit(stage) == (stage <= 2 ? 2 : stage <= 5 ? 3 : 4), "Visible cap");
                Assert(SchoolCampaign.AttackLimit(stage) == (stage <= 3 ? 1 : 2), "Attack cap");
                if (stage >= 6 && stage <= 9)
                    Assert(SchoolCampaign.Damage(stage, false) == 10, "Later officer damage changed");
            }
        });
        Test("reinforcements do not prematurely finish a wave or exceed live limits", () =>
        {
            var wave = new WaveProgress(6);
            for (int i = 0; i < 4; i++) Assert(wave.Spawn(4), "Initial spawn failed");
            Assert(!wave.Spawn(4) && wave.Alive == 4 && wave.Remaining == 6, "Cap ignored");
            Assert(wave.Defeat() && wave.Spawn(4), "Reinforcement not admitted");
            Assert(wave.Defeat() && wave.Spawn(4) && !wave.Spawn(4), "Total exceeded");
            for (int i = 0; i < 4; i++) Assert(wave.Defeat(), "Remaining enemy ignored");
            Assert(wave.Complete && wave.Remaining == 0 && !wave.Defeat(), "Wave completion");
        });
        Test("round healing clamps and boss floor does not reduce high HP", () =>
        {
            var health = new CombatHealth(120); health.Apply(100); health.Heal(18);
            Assert(health.Current == 38, "Round heal");
            health.Heal(Math.Max(0, 96 - health.Current)); Assert(health.Current == 96, "Boss floor");
            health.Heal(1000); Assert(health.Current == 120, "Heal exceeded max");
            health.Heal(Math.Max(0, 96 - health.Current)); Assert(health.Current == 120, "Boss floor reduced HP");
            health.Heal(float.NaN); Assert(health.Current == 120, "NaN poisoned health");
            health.Apply(120); health.Heal(18); Assert(!health.Alive, "Heal revived defeated actor");
        });
        Console.WriteLine($"{passed} tests passed; 0 failed.");
    }
}
