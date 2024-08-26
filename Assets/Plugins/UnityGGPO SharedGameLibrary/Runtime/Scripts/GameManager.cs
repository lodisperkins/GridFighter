using Codice.Client.Common;
using Pada1.BBCore.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityGGPO;


namespace SharedGame {

    public abstract class GameManager : MonoBehaviour {

        public enum UpdateType {
            /// <summary>
            /// This type is useful when you want to run the game at a consistent frame rate with some allowance for frames to catch up if needed.
            /// </summary>
            VectorWar,
            /// <summary>
            /// Suitable for scenarios where you need the game to process every frame exactly as it comes, ensuring that no frames are skipped or fast-forwarded. 
            /// This might be used for debugging or specific gameplay modes where consistency is critical.
            /// </summary>
            Always,
            /// <summary>
            /// Useful in games where maintaining a consistent frame rate is important, even if it means skipping some frames. 
            /// This is common in networked games where all clients need to stay synchronized.
            /// </summary>
            FixedSkip,
            /// <summary>
            /// Appropriate for games where it's important to process every frame (no skipping), 
            /// but still keep up with the desired frame rate by catching up quickly if necessary.
            /// </summary>
            FixedFastForward,
            /// <summary>
            /// Best for scenarios where a smoother experience is desirable, potentially by sacrificing a bit of frame accuracy in favor of a more stable and visually consistent output.
            /// </summary>
            Smoothed
        }

        [Tooltip("Select the type of update behavior for the game.")]
        public UpdateType updateType = UpdateType.FixedSkip;

        private static GameManager _instance;

        public static GameManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<GameManager>();
                }
                return _instance;
            }
        }

        public event Action<StatusInfo> OnStatus;

        public event Action<bool> OnRunningChanged;

        public event Action OnInit;

        public event Action OnStateChanged;

        public Stopwatch updateWatch = new Stopwatch();

        public bool IsRunning { get; private set; }

        public IGameRunner Runner { get; private set; }

        private double start;
        private double next;
        private int currentFrame;

        private double MsToFrame(double time) {
            return time / 1000.0 * 60.0;
        }

        private double FrameToMs(double ms) {
            return ms * 1000.0 / 60.0;
        }

        public void DisconnectPlayer(int player) {
            if (Runner != null) {
                Runner.DisconnectPlayer(player);
            }
        }

        public void Shutdown() {
            if (Runner != null) {
                Runner.Shutdown();
                Runner = null;
            }
        }

        private void OnDestroy() {
            Shutdown();
            _instance = null;
        }

        protected virtual void OnPreRunFrame() {
        }

        private void Update() {
            if (IsRunning != (Runner != null)) {
                IsRunning = Runner != null;
                OnRunningChanged?.Invoke(IsRunning);
                if (IsRunning) {
                    InitGame();
                }
            }
            if (IsRunning) {
                updateWatch.Start();

                if (updateType == UpdateType.VectorWar) {
                    UpdateVectorwar();
                }
                else if (updateType == UpdateType.Always) {
                    UpdateAlways();
                }
                else if (updateType == UpdateType.FixedSkip) {
                    UpdateFixedSkip();
                }
                else if (updateType == UpdateType.FixedFastForward) {
                    UpdateFixedFastForward();
                }
                else if (updateType == UpdateType.Smoothed) {
                    UpdateSmoothed();
                }

                updateWatch.Stop();

                var statusInfo = Runner.GetStatus(updateWatch);

                OnStatus?.Invoke(statusInfo);
            }
        }

        private void InitGame() {
            OnInit?.Invoke();
            start = (double)Utils.TimeGetTime();
            next = start;
            currentFrame = 0;
        }

        private void Tick() {
            OnPreRunFrame();
            Runner.RunFrame();
            currentFrame++;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// This update type is tailored for a specific use case, tied to a game or simulation named "VectorWar."
        /// </summary>
        private void UpdateVectorwar() {

            /// <summary>
            ///The game checks if it's ahead (FramesAhead > 0), and if so, it sleeps to synchronize.
            ///It then idles briefly and processes a tick every frame.
            ///The next frame is scheduled immediately after processing the current frame.
            /// </summary>

            var now = (double)Utils.TimeGetTime();
            if (Runner.FramesAhead > 0) {
                Utils.Sleep((int)FrameToMs(Runner.FramesAhead));
                Runner.FramesAhead = 0;
            }
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            if (now >= next) {
                Tick();
                next = now + FrameToMs(1);
            }
        }

        /// <summary>
        /// This update type is designed to ensure the game keeps up with the desired frame rate by potentially skipping frames if it falls behind.
        /// </summary>
        private void UpdateFixedSkip() {
            ///It first checks if the game is ahead of schedule and adjusts the next frame's time accordingly.
            ///The game idles until it's time to process the next frame.
            ///If the game falls behind(i.e., now >= next), it processes multiple ticks to catch up to the current time.

            var now = Utils.TimeGetTime();
            if (Runner.FramesAhead > 0) {
                next += FrameToMs(Runner.FramesAhead);
                Runner.FramesAhead = 0;
            }
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            while (now >= next) {
                Tick();
                next += FrameToMs(1);
            }
        }

        /// <summary>
        /// Provides a smoother experience by adjusting the frame timing based on how far the game is ahead or behind.
        /// </summary>
        private void UpdateSmoothed() {
            ///It calculates how far the game is ahead or behind in terms of frames and adjusts the timing accordingly.
            ///The Tick is called, and the next frame is scheduled based on a dynamically adjusted time step, which aims to smooth out the frame rate.

                        var now = (double)Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            if (now >= next) {
                if (Runner.FramesAhead > 0) {
                    start += FrameToMs(Runner.FramesAhead - 1);
                    Runner.FramesAhead = 0;
                }
                var targetFrame = MsToFrame(now - start);
                int nearestTarget = Mathf.RoundToInt((float)targetFrame);
                double d = 1.0;
                if (currentFrame != nearestTarget) {
                    d = 1 - ((targetFrame - currentFrame) / 50f);
                    //Log.Verbose("Smooth Step adjusted: s:{0} t:{1} c:{2}", d, targetFrame, currentFrame);
                }

                next += FrameToMs(d);

                Tick();
            }
        }

        /// <summary>
        /// Similar to FixedSkip, but instead of skipping frames, this type processes additional frames quickly if the game is behind.
        /// </summary>
        private void UpdateFixedFastForward() {
            var now = Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            if (now >= next) {
                Tick();
                next += FrameToMs(1);
                if (Runner.FramesAhead > 0) {
                    next += FrameToMs(1);
                    --Runner.FramesAhead;
                }
            }
        }

        /// <summary>
        /// This update type processes a game tick on every frame without skipping or fast-forwarding.
        /// </summary>
        private void UpdateAlways() {
            ///The game checks if it's ahead (FramesAhead > 0), and if so, it sleeps to synchronize.
            ///It then idles briefly and processes a tick every frame.
            ///The next frame is scheduled immediately after processing the current frame.
            ///
            if (Runner.FramesAhead > 0) {
                Utils.Sleep((int)FrameToMs(Runner.FramesAhead));
                Runner.FramesAhead = 0;
            }
            var now = Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            Tick();
            next += FrameToMs(1);
        }

        public void StartGame(IGameRunner runner) {
            Runner = runner;
        }

        public abstract void StartLocalGame();

        public abstract void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex);

        public void ResetTimers() {
            updateWatch.Reset();
            Runner.ResetTimers();
        }
    }
}