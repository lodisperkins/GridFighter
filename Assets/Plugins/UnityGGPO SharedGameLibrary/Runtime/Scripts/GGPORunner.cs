using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityGGPO;
using Unity.Logging;

namespace SharedGame {

    public class GGPORunner : IGameRunner {
        public static int SyncTestRollbackWindow = 8;
        public delegate void ResimulationStartedEvent(int rollbackFrame, int targetFrame);
        public delegate void ResimulationCompleteEvent(int framesResimulated);
        public static event ResimulationStartedEvent OnResimulationStarted;
        public static event ResimulationCompleteEvent OnResimulationComplete;
        public event Action<string> OnGGPOLogMessage;
        public int PlayerIndex { get; set; }
        public int FramesAhead { get; set; }

        public const int MAX_PLAYERS = 2;
        private const int FRAME_DELAY = 2;

        public string Name { get; private set; }
        public IGame Game { get; private set; }
        public GameInfo GameInfo { get; private set; }
        public IPerfUpdate Perf { get; private set; }

        private Stopwatch frameWatch = new Stopwatch();
        private Stopwatch idleWatch = new Stopwatch();
        private bool _isResimulating;
        private int _resimulationTargetFrame;
        private int _loadedRollbackFrame;

        public bool IsResimulating => _isResimulating;

        /*
         * The begin game callback.  We don't need to do anything special here,
         * so just return true.
         */

        private bool OnBeginGameCallback(string name) {
            Log.Debug("OnBeginGameCallback {0}", name);
            return true;
        }

        /*
         * Notification from GGPO that something has happened.  Update the status
         * text at the bottom of the screen to notify the user.
         */

        private bool OnEventConnectedToPeerDelegate(int connected_player) {
            Log.Debug("OnEventConnectedToPeerDelegate {0}", connected_player);
            GameInfo.SetConnectState(connected_player, PlayerConnectState.Synchronizing);
            return true;
        }

        public bool OnEventSynchronizingWithPeerDelegate(int synchronizing_player, int synchronizing_count, int synchronizing_total) {
            Log.Debug("OnEventSynchronizingWithPeerDelegate {0}, {1}, {2}", synchronizing_player, synchronizing_count, synchronizing_total);
            var progress = 100 * synchronizing_count / synchronizing_total;
            GameInfo.UpdateConnectProgress(synchronizing_player, progress);
            return true;
        }

        public bool OnEventSynchronizedWithPeerDelegate(int synchronized_player) {
            Log.Debug("OnEventSynchronizedWithPeerDelegate {0}", synchronized_player);
            GameInfo.UpdateConnectProgress(synchronized_player, 100);
            return true;
        }

        public bool OnEventRunningDelegate() {
            Log.Debug("OnEventRunningDelegate");
            GameInfo.SetConnectState(PlayerConnectState.Running);
            SetStatusText("Running so I guess connected?");
            return true;
        }

        public bool OnEventConnectionInterruptedDelegate(int connection_interrupted_player, int connection_interrupted_disconnect_timeout) {
            Log.Debug("OnEventConnectionInterruptedDelegate {0}, {1}", connection_interrupted_player, connection_interrupted_disconnect_timeout);
            GameInfo.SetDisconnectTimeout(connection_interrupted_player,
                Utils.TimeGetTime(),
                connection_interrupted_disconnect_timeout);
            return true;
        }

        public bool OnEventConnectionResumedDelegate(int connection_resumed_player) {
            Log.Debug("OnEventConnectionResumedDelegate {0}", connection_resumed_player);
            GameInfo.SetConnectState(connection_resumed_player, PlayerConnectState.Running);
            return true;
        }

        public bool OnEventDisconnectedFromPeerDelegate(int disconnected_player) {
            Log.Debug("OnEventDisconnectedFromPeerDelegate {0}", disconnected_player);
            GameInfo.SetConnectState(disconnected_player, PlayerConnectState.Disconnected);
            return true;
        }

        public bool OnEventEventcodeTimesyncDelegate(int timesync_frames_ahead) {
            Log.Debug("OnEventEventcodeTimesyncDelegate {0}", timesync_frames_ahead);
            FramesAhead = timesync_frames_ahead;
            return true;
        }

        public bool OnSyncErrorDelegate(int errorCode, string text) {
            var message = $"SyncTest error {GGPO.GetErrorCodeMessage(errorCode)}: {text}";
            SetStatusText(message);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return true;
        }

        /*
         * Notification from GGPO we should step foward exactly 1 frame
         * during a rollback.
         */

        private bool OnAdvanceFrameCallback(int flags) {
            Log.Debug("OnAdvanceFrameCallback {0}", flags);

            // Make sure we fetch new inputs from GGPO and use those to update the game state
            // instead of reading from the keyboard.
            var inputs = GGPO.Session.SynchronizeInput(MAX_PLAYERS, out var disconnect_flags);

            AdvanceFrame(inputs, disconnect_flags);

            if (_isResimulating && Game.Framenumber >= _resimulationTargetFrame)
            {
                int framesResimulated = Math.Max(0, _resimulationTargetFrame - _loadedRollbackFrame);
                _isResimulating = false;
                OnResimulationComplete?.Invoke(framesResimulated);
            }
            return true;
        }

        /*
         * Makes our current state match the state passed in by GGPO.
         */

        private bool OnLoadGameStateCallback(NativeArray<byte> data) {
            Log.Debug("OnLoadGameStateCallback {0}", data.Length);
            _isResimulating = true;
            _resimulationTargetFrame = Game.Framenumber;
            Game.FromBytes(data);
            _loadedRollbackFrame = Game.Framenumber;
            OnResimulationStarted?.Invoke(_loadedRollbackFrame, _resimulationTargetFrame);
            return true;
        }

        private bool OnGGPOLogMessageCallback(string text) {
            Log.Debug("OnGGPOLogMessageCallback {0}", text);
            OnGGPOLogMessage?.Invoke(text);
            return true;
        }

        /*
         * Save the current state to a buffer and return it to GGPO via the
         * buffer and len parameters.
         */

        private bool OnSaveGameStateCallback(out NativeArray<byte> data, out int checksum, int frame) {
            Log.Verbose("OnSaveGameStateCallback {0}", frame);
            data = Game.ToBytes();
            checksum = Utils.CalcFletcher32(data);
            //checksum = Game.Checksum;
            return true;
        }

        /*
         * Log the gamestate.  Used by the synctest debugging tool.
         */

        private bool OnLogGameState(string filename, NativeArray<byte> data) {
            Log.Debug("OnLogGameState {0}", filename);
            Game.FromBytes(data);
            Game.LogInfo(filename);
            return true;
        }

        private void OnFreeBufferCallback(NativeArray<byte> data) {
            Log.Verbose("OnFreeBufferCallback");
            Game.FreeBytes(data);
        }

        /// <summary>
        /// </summary>
        /// <param name="perfPanel"></param>
        /// <param name="callback"></param>

        public GGPORunner(string name, IGame game, IPerfUpdate perfPanel) {
            Name = name;
            Game = game;
            Perf = perfPanel;
            GGPO.SetLogDelegate(LogPlugin);
            Log.Debug("GGPORunner {0}", name);
            GameInfo = new GameInfo();
        }

        public void Init(IList<Connections> connections, int playerIndex)
        {
            var remote_index = -1;
            var num_spectators = 0;
            var num_players = 0;

            for (int i = 0; i < connections.Count; ++i)
            {
                if (i != playerIndex && remote_index == -1)
                {
                    remote_index = i;
                }

                if (connections[i].spectator)
                {
                    ++num_spectators;
                }
                else
                {
                    ++num_players;
                }
            }
            if (connections[playerIndex].spectator)
            {
                InitSpectator(connections[playerIndex].port, num_players, connections[remote_index].ip, connections[remote_index].port);
            }
            else
            {
                var players = new List<GGPOPlayer>();
                for (int i = 0; i < connections.Count; ++i)
                {
                    var player = new GGPOPlayer
                    {
                        player_num = players.Count + 1,
                    };
                    if (playerIndex == i)
                    {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL;
                        player.ip_address = "";
                        player.port = 0;
                    }
                    else if (connections[i].spectator)
                    {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_SPECTATOR;
                        player.ip_address = connections[remote_index].ip;
                        player.port = connections[remote_index].port;
                    }
                    else
                    {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE;
                        player.ip_address = connections[remote_index].ip;
                        player.port = connections[remote_index].port;
                    }
                    players.Add(player);
                }
                Init(connections[playerIndex].port, num_players, players, num_spectators);
            }
        }

        /*
         * Initialize the game.  This initializes the game state and
         * the video renderer and creates a new network session.
         */

        public void Init(int localport, int num_players, IList<GGPOPlayer> players, int num_spectators) {
            Log.Debug("Init {0}, {1}, {2}, {3}",
                localport,
                num_players,
                string.Join("|", players),
                num_spectators);
            // Initialize the game state

#if SYNC_TEST
            var result = GGPO.Session.StartSyncTest(
                    OnBeginGameCallback,
                    OnAdvanceFrameCallback,
                    OnLoadGameStateCallback,
                    OnLogGameState,
                    OnGGPOLogMessageCallback,
                    OnSaveGameStateCallback,
                    OnFreeBufferCallback,
                    OnEventConnectedToPeerDelegate,
                    OnEventSynchronizingWithPeerDelegate,
                    OnEventSynchronizedWithPeerDelegate,
                    OnEventRunningDelegate,
                    OnEventConnectionInterruptedDelegate,
                    OnEventConnectionResumedDelegate,
                    OnEventDisconnectedFromPeerDelegate,
                    OnEventEventcodeTimesyncDelegate,
                    OnSyncErrorDelegate,
                    Name, num_players, SyncTestRollbackWindow);
#else
            var result = GGPO.Session.StartSession(
                    OnBeginGameCallback,
                    OnAdvanceFrameCallback,
                    OnLoadGameStateCallback,
                    OnLogGameState,
                    OnGGPOLogMessageCallback,
                    OnSaveGameStateCallback,
                    OnFreeBufferCallback,
                    OnEventConnectedToPeerDelegate,
                    OnEventSynchronizingWithPeerDelegate,
                    OnEventSynchronizedWithPeerDelegate,
                    OnEventRunningDelegate,
                    OnEventConnectionInterruptedDelegate,
                    OnEventConnectionResumedDelegate,
                    OnEventDisconnectedFromPeerDelegate,
                    OnEventEventcodeTimesyncDelegate,
                    Name, num_players, localport);

            // automatically disconnect clients after 3000 ms and start our count-down timer for
            // disconnects after 1000 ms. To completely disable disconnects, simply use a value of 0
            // for ggpo_set_disconnect_timeout.
            CheckAndReport(GGPO.Session.SetDisconnectTimeout(0));
            CheckAndReport(GGPO.Session.SetDisconnectNotifyStart(1000));

#endif
            CheckAndReport(result);


            int controllerId = 0;
            int playerIndex = 0;
            GameInfo.players = new PlayerConnectionInfo[num_players];
            for (int i = 0; i < players.Count; i++) {
                CheckAndReport(GGPO.Session.AddPlayer(players[i], out int handle));

                if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                    var playerInfo = new PlayerConnectionInfo();
                    playerInfo.handle = handle;
                    playerInfo.type = players[i].type;
                    playerInfo.connect_progress = 100;
                    playerInfo.controllerId = playerIndex;
                    GameInfo.players[playerIndex++] = playerInfo;
                    GameInfo.SetConnectState(handle, PlayerConnectState.Connecting);

#if !SYNC_TEST
                    CheckAndReport(GGPO.Session.SetFrameDelay(handle, FRAME_DELAY));
#endif
                }
                else if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE) {
                    var playerInfo = new PlayerConnectionInfo();
                    playerInfo.handle = handle;
                    playerInfo.type = players[i].type;
                    playerInfo.connect_progress = 0;
                    GameInfo.players[playerIndex++] = playerInfo;
                }
            }

            SetStatusText("Connecting to peers.");
        }

        /*
         * Create a new spectator session
         */

        public void InitSpectator(int localport, int num_players, string host_ip, int host_port) {
            Log.Debug("InitSpectator {0} {1} {2} {3}", localport, num_players, host_ip, host_port);

            // Initialize the game state
            GameInfo.players = Array.Empty<PlayerConnectionInfo>();

            // Fill in a ggpo callbacks structure to pass to start_session.
            var result = GGPO.Session.StartSpectating(
                   OnBeginGameCallback,
                   OnAdvanceFrameCallback,
                   OnLoadGameStateCallback,
                   OnLogGameState,
                   OnGGPOLogMessageCallback,
                   OnSaveGameStateCallback,
                   OnFreeBufferCallback,
                   OnEventConnectedToPeerDelegate,
                   OnEventSynchronizingWithPeerDelegate,
                   OnEventSynchronizedWithPeerDelegate,
                   OnEventRunningDelegate,
                   OnEventConnectionInterruptedDelegate,
                   OnEventConnectionResumedDelegate,
                   OnEventDisconnectedFromPeerDelegate,
                   OnEventEventcodeTimesyncDelegate,
                   Name, num_players, localport, host_ip, host_port);

            CheckAndReport(result);

            SetStatusText("Starting new spectator session");
        }

        /*
         * Disconnects a player from this session.
         */

        public void DisconnectPlayer(int playerIndex) {
            Log.Debug("DisconnectPlayer {0}", playerIndex);

            if (playerIndex < GameInfo.players.Length) {
                string logbuf;
                var result = GGPO.Session.DisconnectPlayer(GameInfo.players[playerIndex].handle);
                if (GGPO.SUCCEEDED(result)) {
                    logbuf = $"Disconnected player {playerIndex}.";
                }
                else {
                    logbuf = $"Error while disconnecting player (err:{result}).";
                }
                SetStatusText(logbuf);
            }
        }

        /*
         * Advances the game state by exactly 1 frame using the inputs specified
         * for player 1 and player 2.
         */

        private void AdvanceFrame(long[] inputs, int disconnect_flags) {
            if (Game == null) {
                Log.Debug("GameState is null what?");
            }
            Game.Update(inputs, disconnect_flags);

            // update the checksums to display in the top of the window. this helps to detect desyncs.
            GameInfo.now.framenumber = Game.Framenumber;
            GameInfo.now.checksum = Game.Checksum;
            if ((Game.Framenumber % 90) == 0) {
                GameInfo.periodic = GameInfo.now;
            }

            try
            {
                // Notify ggpo that we've moved forward exactly 1 frame.
                CheckAndReport(GGPO.Session.AdvanceFrame());
            }
            catch (Exception e)
            {
                Log.Debug("Error while advancing frame: {0}", e);

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            // Update the performance monitor display.
            int[] handles = new int[MAX_PLAYERS];
            int count = 0;
            for (int i = 0; i < GameInfo.players.Length; i++) {
                if (GameInfo.players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE) {
                    handles[count++] = GameInfo.players[i].handle;
                }
            }

            var statss = new GGPONetworkStats[count];
            for (int i = 0; i < count; ++i) {
                CheckAndReport(GGPO.Session.GetNetworkStats(handles[i], out statss[i]));
            }
            Perf?.ggpoutil_perfmon_update(statss);
        }

        /*
        * Run a single frame of the game.
        */
        private long[] localInputs = new long[7];
        private long localInputIndex = 0;
        private long[] onlineInputs = new long[7];
        private long onlineInputIndex = 0;

        public void RunFrame() {
            var result = GGPO.OK;
            //GGPO.Session.SynchronizeInput(MAX_PLAYERS, out _);
            for (int i = 0; i < GameInfo.players.Length; ++i) {
                var player = GameInfo.players[i];
                if (player.type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                    var input = Game.ReadInputs(player.controllerId);

                    localInputs[localInputIndex] = input;
                    localInputIndex++;

                    if (localInputIndex >= 7)
                        localInputIndex = 0;

                    result = GGPO.Session.AddLocalInput(player.handle, input);
                }

//#if SYNC_TEST
//                else if (player.type == GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE) {
//                    var input = Game.ReadInputs(player.controllerId);

//                    onlineInputs[localInputIndex] = input;
//                    onlineInputIndex++;

//                    if (onlineInputIndex >= 7)
//                        onlineInputIndex = 0;

//                    result = GGPO.Session.AddLocalInput(player.handle, input);
//                }
//#endif
            }

            //Log.Debug($"Local Inputs: {string.Join(", ", localInputs)}");


            Log.Debug("Input Result: " + GGPO.GetErrorCodeMessage(result));

            // synchronize these inputs with ggpo. If we have enough input to proceed ggpo will
            // modify the input list with the correct inputs to use and return 1.
            if (GGPO.SUCCEEDED(result)) {
                frameWatch.Start();
                try {
                    // inputs[0] and inputs[1] contain the inputs for p1 and p2. Advance the game by
                    // 1 frame using those inputs.
                    var inputs = GGPO.Session.SynchronizeInput(MAX_PLAYERS, out var disconnect_flags);

                    onlineInputs = inputs;

                    //Log.Debug($"Online Inputs: {string.Join(", ", onlineInputs)}");
                    AdvanceFrame(inputs, disconnect_flags);
                }
                catch (Exception ex) {
                    Log.Debug("Error {0}", ex);
                }
                frameWatch.Stop();
            }
        }

        /*
         * Spend our idle time in ggpo so it can use whatever time we have left over
         * for its internal bookkeeping.
         */

        public void Idle(int time) {
            idleWatch.Start();
            CheckAndReport(GGPO.Session.Idle(time));
            idleWatch.Stop();
        }

        public void Exit() {
            Log.Debug("Exit");

            if (GGPO.Session.IsStarted()) {
                CheckAndReport(GGPO.Session.CloseSession());
            }
        }

        private void SetStatusText(string status) {
            GameInfo.status = status;
            Log.Debug(status);
        }

        private void CheckAndReport(int result) {
            if (!GGPO.SUCCEEDED(result)) {
                Log.Debug(GGPO.GetErrorCodeMessage(result));

#if SYNC_TEST
//GGPO.Session.CloseSession();
#endif
            }
        }

        public StatusInfo GetStatus(Stopwatch updateWatch) {
            var status = new StatusInfo();
            status.idlePerc = (float)idleWatch.ElapsedMilliseconds / (float)updateWatch.ElapsedMilliseconds;
            status.updatePerc = (float)frameWatch.ElapsedMilliseconds / (float)updateWatch.ElapsedMilliseconds;
            status.periodic = GameInfo.periodic;
            status.now = GameInfo.now;
            return status;
        }

        public void Shutdown() {
            Exit();
            GGPO.SetLogDelegate(null);
        }

        public static void LogPlugin(string value) {
            Log.Verbose(value);
        }

        public void ResetTimers() {
            frameWatch.Reset();
            idleWatch.Reset();
        }
    }
}
