﻿using System.Collections;
using System.Linq;
using IPA.Utilities;
using UnityEngine;

namespace TrickSaber
{
    public class GlobalTrickManager : MonoBehaviour
    {
        public static GlobalTrickManager Instance;

        private Coroutine _applySlowmoCoroutine;
        private Coroutine _endSlowmoCoroutine;

        private bool _slowmoApplied;
        private float _endSlowmoTarget;
        private float _slowmoStepAmount;
        private float _originalTimeScale;

        public AudioTimeSyncController AudioTimeSyncController;
        private AudioSource _audioSource;
        public AudioSource AudioSource
        {
            get
            {
                if(_audioSource==null)
                    _audioSource = AudioTimeSyncController.GetField<AudioSource, AudioTimeSyncController>("_audioSource");
                return _audioSource;
            }
        }

        public BeatmapObjectManager BeatmapObjectManager;
        private float _timeSinceLastNote;

        public SaberTrickManager LeftSaberSaberTrickManager;
        public SaberTrickManager RightSaberSaberTrickManager;

        public bool Enabled
        {
            get
            {
                if (!LeftSaberSaberTrickManager || !RightSaberSaberTrickManager) return false;
                return LeftSaberSaberTrickManager.Enabled || RightSaberSaberTrickManager.Enabled;
            }
            set
            {
                if (!LeftSaberSaberTrickManager || !RightSaberSaberTrickManager) return;
                LeftSaberSaberTrickManager.Enabled = value;
                RightSaberSaberTrickManager.Enabled = value;
            }
        }

        public bool SaberClashCheckerEnabled = true;

        private void Awake()
        {
            Instance = this;
            _slowmoStepAmount = PluginConfig.Instance.SlowmoStepAmount;

            //var scoreController = FindObjectsOfType<ScoreController>().FirstOrDefault();
            //BeatmapObjectManager = scoreController.GetField<BeatmapObjectManager, ScoreController>("_beatmapObjectManager");

            //BeatmapObjectManager.noteWasSpawnedEvent += OnNoteWasSpawned;
            //if (PluginConfig.Instance.DisableIfNotesOnScreen) StartCoroutine(NoteSpawnTimer());
        }

        public void OnTrickStarted(TrickAction trickAction)
        {
            SaberClashCheckerEnabled = false;
            if (trickAction == TrickAction.Throw && PluginConfig.Instance.SlowmoDuringThrow && !_slowmoApplied)
            {
                var timeScale = AudioTimeSyncController.timeScale;
                if (_endSlowmoCoroutine != null)
                {
                    StopCoroutine(_endSlowmoCoroutine);
                    timeScale = _endSlowmoTarget;
                }
                _applySlowmoCoroutine = StartCoroutine(ApplySlowmoSmooth(PluginConfig.Instance.SlowmoAmount, timeScale));
                _slowmoApplied = true;
            }
        }

        public void OnTrickEndRequested(TrickAction trickAction)
        {
            if (trickAction == TrickAction.Throw)
                if (PluginConfig.Instance.SlowmoDuringThrow &&
                    !IsTrickInState(trickAction, TrickState.Started) && _slowmoApplied)
                {
                    if(_applySlowmoCoroutine!=null)StopCoroutine(_applySlowmoCoroutine);
                    _endSlowmoCoroutine = StartCoroutine(EndSlowmoSmooth());
                    _slowmoApplied = false;
                }
        }

        public void OnTrickEnded(TrickAction trickAction)
        {
            if(!IsDoingTrick()) SaberClashCheckerEnabled = true;
        }

        private IEnumerator ApplySlowmoSmooth(float amount, float originalTimescale)
        {
            float timeScale = AudioTimeSyncController.timeScale;
            _originalTimeScale = originalTimescale;
            float targetTimeScale = _originalTimeScale - amount;
            if (targetTimeScale < 0.1f) targetTimeScale = 0.1f;
            while (timeScale > targetTimeScale)
            {
                timeScale -= _slowmoStepAmount;
                SetTimescale(timeScale);
                yield return new WaitForFixedUpdate();
            }

            SetTimescale(targetTimeScale);
        }

        private IEnumerator EndSlowmoSmooth()
        {
            float timeScale = AudioTimeSyncController.timeScale;
            float targetTimeScale = _originalTimeScale;
            _endSlowmoTarget = targetTimeScale;
            while (timeScale < targetTimeScale)
            {
                timeScale += _slowmoStepAmount;
                SetTimescale(timeScale);
                yield return new WaitForFixedUpdate();
            }

            SetTimescale(targetTimeScale);
        }

        void SetTimescale(float timescale)
        {
            AudioTimeSyncController.SetField("_timeScale", timescale);
            AudioSource.pitch = timescale;
        }

        IEnumerator NoteSpawnTimer()
        {
            while (true)
            {
                _timeSinceLastNote += Time.deltaTime;
                yield return null;
            }
        }

        public bool CanDoTrick()
        {
            if (!PluginConfig.Instance.DisableIfNotesOnScreen) return true;
            if (_timeSinceLastNote > 20/GameplayManager.CurrentDifficultyBeatmap.noteJumpMovementSpeed) return true;
            return false;
        }

        void OnNoteWasSpawned(NoteController noteController)
        {
            _timeSinceLastNote = 0;
        }

        public bool IsTrickInState(TrickAction trickAction, TrickState state)
        {
            return LeftSaberSaberTrickManager.IsTrickInState(trickAction, state) ||
                   RightSaberSaberTrickManager.IsTrickInState(trickAction, state);
        }

        public bool IsDoingTrick()
        {
            return LeftSaberSaberTrickManager.IsDoingTrick() || RightSaberSaberTrickManager.IsDoingTrick();
        }
    }
}