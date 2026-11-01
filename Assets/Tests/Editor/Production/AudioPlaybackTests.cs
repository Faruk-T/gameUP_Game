using System;
using System.Collections;
using GameUp.App;
using GameUp.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameUp.Production.Tests
{
    public sealed class AudioPlaybackTests
    {
        [UnityTest]
        public IEnumerator NarrationPausesWithItsClockAndPrinterStopsAfterOneJob()
        {
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            yield return new EnterPlayMode();
            yield return ExerciseAudio();
            yield return new ExitPlayMode();
        }
        private static AudioSource Channel(AudioManager audio, string name) => (AudioSource)new SerializedObject(audio).FindProperty(name).objectReferenceValue;
        private static IEnumerator ExerciseAudio()
        {
            float deadline = Time.realtimeSinceStartup + 25;
            while (!ApplicationRoot.Current || ApplicationRoot.Current.IsLoading || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Menu")
            { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
            var audio = ApplicationRoot.Require().Audio;
            var voice = Channel(audio, "narration"); var ocean = Channel(audio, "ambience");
            var printer = Channel(audio, "printer"); var effects = Channel(audio, "effects"); var analyzer = Channel(audio, "analyzer");
            Assert.That(ocean.isPlaying, Is.True); Assert.That(voice.isPlaying, Is.False);
            audio.BeginOpening(); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(voice.isPlaying, Is.True);
            audio.SampleOpening(6, false, 0); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(voice.isPlaying, Is.False); Assert.That(voice.time, Is.EqualTo(6).Within(.15f));
            int pausedSample = voice.timeSamples; yield return new WaitForSecondsRealtime(.15f);
            Assert.That(voice.timeSamples, Is.EqualTo(pausedSample), "A paused opening must not consume narration.");
            audio.SampleOpening(6, true, 0); yield return new WaitForSecondsRealtime(.1f);
            Assert.That(voice.isPlaying, Is.True); Assert.That(voice.time, Is.EqualTo(6.1f).Within(.15f));
            audio.SampleOpening(voice.clip.length + .01, true, 0);
            Assert.That(voice.isPlaying, Is.False);
            audio.SampleOpening(0, true, 0); Assert.That(voice.isPlaying, Is.False, "An ended voice must not loop or restart.");
            audio.EnterWorkstation(); Assert.That(ocean.volume, Is.EqualTo(.2f)); Assert.That(ocean.isPlaying, Is.True);
            audio.SetAnalysis(true, false); yield return new WaitForSecondsRealtime(.05f);
            Assert.That(analyzer.isPlaying, Is.True); Assert.That(analyzer.volume, Is.EqualTo(.95f));
            audio.SetAnalysis(true, true); Assert.That(analyzer.isPlaying, Is.False);
            audio.SetAnalysis(true, false); Assert.That(analyzer.isPlaying, Is.True);
            audio.SetAnalysis(false, false); Assert.That(analyzer.isPlaying, Is.False);
            audio.PlayCue(GameAudioCue.Fax); yield return new WaitForSecondsRealtime(.05f);
            Assert.That(printer.isPlaying, Is.True); Assert.That(printer.loop, Is.False);
            Assert.That(effects.isPlaying, Is.False, "Printer must have its own channel.");
            audio.PlayCue(GameAudioCue.Fax);
            yield return new WaitForSecondsRealtime(audio.FaxDuration + .15f);
            Assert.That(printer.isPlaying, Is.False, "Repeated print requests must finish after one current job.");
            audio.PlayCue(GameAudioCue.BookPage); audio.PlayCue(GameAudioCue.Fax);
            yield return new WaitForSecondsRealtime(.05f); audio.StopFax();
            Assert.That(printer.isPlaying, Is.False); Assert.That(effects.isPlaying, Is.True, "Stopping the printer must preserve other effects.");
            audio.BeginOpening(); yield return new WaitForSecondsRealtime(.1f); Assert.That(voice.isPlaying, Is.True);
            audio.EnterMenu(); Assert.That(voice.isPlaying, Is.False); Assert.That(printer.isPlaying, Is.False);
            Assert.That(ocean.isPlaying, Is.True);
        }
    }
}
