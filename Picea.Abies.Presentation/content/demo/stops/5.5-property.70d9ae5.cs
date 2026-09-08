    // ── INV-3 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-3")]
    public async Task INV_3_redo_after_undo_restores_the_model_the_document_and_all_subsequent_behaviour()
    {
        var reached = 0;                                        // 🔴-3's floor; see INV-1

        foreach (var seed in Gen.Corpus)
        {
            var script = Gen.UserActions(seed, length: 24);     // S27: no deliveries here
            using var runtime = await Start<Editor>(NoFeedback);
            foreach (var step in script) await runtime.Dispatch(step.Message);

            if (History.Backward(runtime.Model) is not MovementAvailability.Available) continue;

            var s = runtime.Model;
            var documentAtS = runtime.CurrentDocument!;

            await runtime.Dispatch(new HistoryMessage.Undo());
            if (History.Forward(runtime.Model) is not MovementAvailability.Available) continue;
            await runtime.Dispatch(new HistoryMessage.Redo());
            reached++;

            var roundTripped = runtime.Model;

            // (i) the same model value
            await Assert.That(roundTripped.Present).IsEqualTo(s.Present).Because($"seed {seed}");

            // (ii) a document equal UP TO RENAMING OF EVENT-HANDLER COMMAND IDS
            await Assert.That(DocumentComparer.EqualUpToHandlerIds(runtime.CurrentDocument!, documentAtS))
                .IsTrue().Because($"seed {seed}");

            // (iii) the same subsequent behaviour: every message in the alphabet yields the
            //       same (model, command) pair from s' as it would have from s.
            foreach (var m in EditorMessages.All)
            {
                var (fromS, cmdFromS)   = Editor.Transition(s, m);
                var (fromS2, cmdFromS2) = Editor.Transition(roundTripped, m);
                await Assert.That(fromS2.Present).IsEqualTo(fromS.Present).Because($"seed {seed}, {m}");
                await Assert.That(CommandShape.Of(cmdFromS2)).IsEqualTo(CommandShape.Of(cmdFromS))
                    .Because($"seed {seed}, {m}");
            }
        }

        await Assert.That(reached > 0).IsTrue()
            .Because($"no seed in the corpus reached an undo-then-redo round trip, so this " +
                     $"property is green having asserted nothing (0 of {Gen.Corpus.Length} seeds)");
    }
