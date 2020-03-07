using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public enum PlayerEvents
    {
        Play,
        TogglePlayPause,
        PlayCompleted,
        ListFinished,
        WaitTimeout,
        PlayerStarted,
        FaultOccurred,
        Reposition,
        SetVolume,
        Reset
    }
    public abstract class DevicePlayer
    {
        public delegate PlayerStates FSMAction(PlayerStates state, PlayerEvents ev, params object[] args);
        public class FSM
        {
            public static int counter;
            public readonly string name;
            private readonly ILogger log;
            private PlayerStates currentState;
            private readonly FSMAction[,] actions = new FSMAction[Enum.GetValues(typeof(PlayerStates)).Length, Enum.GetValues(typeof(PlayerEvents)).Length];
            public FSM(string name, PlayerStates startState, ILogger<FSM> log)
            {
                this.name = $"{name}-{counter++}";
                this.log = log;
                this.currentState = startState;
            }
            public PlayerStates GetState()
            {
                return this.currentState;
            }
            public FSMAction this[PlayerStates state, PlayerEvents ev]
            {
                get
                {
                    return actions[(int)state, (int)ev] ?? DefaultAction;
                }
                //set { }
            }
            public PlayerStates DefaultAction(PlayerStates state, PlayerEvents ev, params object[] args)
            {
                log.Information($"{name}: default action called with state {state.ToString()}, event {ev.ToString()}");
                return state;
            }
            public void AddAction(PlayerStates state, PlayerEvents ev, FSMAction action)
            {
                if (actions[(int)state, (int)ev] != null)
                {
                    log.Warning($"{name}: State {state}, Event {ev}, existing action being overwritten!!");
                }
                actions[(int)state, (int)ev] = action;

            }
            public void AddAction(IEnumerable<PlayerStates> states, PlayerEvents ev, FSMAction action)
            {
                foreach (var state in states)
                {
                    AddAction(state, ev, action);
                }
            }
            public void Act(PlayerEvents ev, params object[] args)
            {
                var action = this[this.currentState, ev];
                var nextState = action(this.currentState, ev, args);
                if (action != DefaultAction)
                {
                    log.Debug($"{name}: event {ev.ToString()} caused a transition from {this.currentState} to {nextState}");
                }
                this.currentState = nextState;
            }
        }
        private Timer waitingForEventTimer;
        private CancellationToken cancellationToken;
        protected ILoggerFactory lf;
        protected ILogger log;
        protected bool userStopRequest;
        protected Func<string, Task> requestNext;
        protected string musicServerUrl;
        protected Action<DeviceStatus> onDeviceStatusUpdate;
        protected readonly AudioDevice device;
        protected readonly FSM stateMachine;
        protected readonly MusicPlayerOptions musicPlayerOptions;
        public DevicePlayer(AudioDevice device, MusicPlayerOptions mpo, Func<string, Task> requestNext, ILoggerFactory loggerFactory)
        {
            this.musicPlayerOptions = mpo;
            this.lf = loggerFactory;
            this.log = lf.CreateLogger(this.GetType().FullName);
            this.device = device;
            this.requestNext = requestNext;
            this.stateMachine = new FSM(device.DisplayName, PlayerStates.Initial, lf.CreateLogger<FSM>());
            this.Init();
        }
        public static DevicePlayer GetDevicePlayer(AudioDevice device, MusicPlayerOptions mpo, Func<string, Task> requestNext, ILoggerFactory loggerFactory)
        {
            DevicePlayer dp = null;
            switch (device.Type)
            {
                case Music.Core.AudioDeviceType.Wasapi:
                    dp = new WasapiDevicePlayer(device, mpo, requestNext, loggerFactory);
                    break;
                case Music.Core.AudioDeviceType.Logitech:
                    dp = new LogitechDevicePlayer(device, mpo, requestNext, loggerFactory);
                    break;
                default:
                    break;
            }
            return dp;
        }
        public void SetMusicServerUrl(string url)
        {
            this.musicServerUrl = url;
        }
        public async Task Start(Action<DeviceStatus> onDeviceStatusUpdate, CancellationToken cancellationToken)
        {
            try
            {
                this.onDeviceStatusUpdate = onDeviceStatusUpdate;
                var interval = this.musicPlayerOptions.DeviceStatusUpdateInterval;// * 2; // initial wait is twice as long
                this.cancellationToken = cancellationToken;
                log.Information($"{device.Name} started");
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(interval));
                    interval = this.musicPlayerOptions.DeviceStatusUpdateInterval; //set interval back to normal
                    var ds = await GetDeviceStatus();// await OnPulse();
                    if (ds != null)
                    {
                        this.onDeviceStatusUpdate(ds);
                    }
                    if (this.cancellationToken.IsCancellationRequested)
                    {
                        log.Debug($"{device.Name} cancellation requested, shutting down");
                        OnCancel();
                        break;
                    }
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
                throw;
            }
        }
        public void OnException(Task task)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception);
            }
            else
            {
                log.Warning($"did not expect to be here!!!!!");
            }
        }
        public override string ToString()
        {
            string text = $"{this.GetType().Name} state is {this.GetState().ToString()} ({device.Name})";
            return text;
        }
        public void OnEvent(PlayerEvents ev, params object[] args)
        {
            this.stateMachine.Act(ev, args);
        }
        protected abstract Task Play(string musicServerUrl, float volume);
        // return true if the player is playing, false otherwise
        protected abstract Task<bool> TogglePlayPause();
        protected abstract Task Reposition(float position);
        protected abstract Task SetVolume(float level);
        protected abstract Task StopPlaying();
        /// <summary>
        /// return null to prevent a onDeviceStatusUpdate call
        /// </summary>
        /// <returns></returns>
        protected abstract Task<DeviceStatus> GetDeviceStatus();
        protected abstract void OnCancel();
        protected PlayerStates GetState()
        {
            return this.stateMachine.GetState();
        }
        protected void OnCurrentPlayStopped()
        {
            this.OnEvent(PlayerEvents.PlayCompleted);
        }
        private void Init()
        {
            this.stateMachine.AddAction(PlayerStates.Initial, PlayerEvents.FaultOccurred, (s, e, args) =>
            {
                return PlayerStates.Fault;
            });
            this.stateMachine.AddAction(PlayerStates.Initial, PlayerEvents.PlayerStarted, (s, e, args) =>
            {
                StartWaitTimer();
                return PlayerStates.Idle;
                //return PlayerStates.SilentIdle;
            });
            this.stateMachine.AddAction(new PlayerStates[] { PlayerStates.SilentIdle, PlayerStates.Idle,
                PlayerStates.Playing, PlayerStates.Paused, PlayerStates.WaitingNext },
                PlayerEvents.Play, (s, e, args) =>
                {
                    if (args.Length > 1)
                    {
                        var streamUrl = (string)args[0];
                        var volume = (float)args[1];
                        this.Play(streamUrl, volume);
                        return PlayerStates.Playing;
                    }
                    else
                    {
                        log.Warning($"{this.stateMachine.name}: play requires a stream url and initial volume");
                        return this.stateMachine.GetState();
                    }
                });
            this.stateMachine.AddAction(new PlayerStates[] { PlayerStates.Playing, PlayerStates.Paused }, PlayerEvents.TogglePlayPause, (s, e, args) =>
            {
                var r = this.TogglePlayPause().Result;
                return r ? PlayerStates.Playing : PlayerStates.Paused;
            });
            this.stateMachine.AddAction(new PlayerStates[] { PlayerStates.Playing, PlayerStates.Paused }, PlayerEvents.Reposition, (s, e, args) =>
            {
                if (args.Length > 0)
                {
                    float position = (float)args[0];
                    this.Reposition(position);
                }
                else
                {
                    log.Warning($"{this.stateMachine.name}: reposition requires a position argument");
                }
                return PlayerStates.Playing;
            });
            this.stateMachine.AddAction(new PlayerStates[] { PlayerStates.Playing, PlayerStates.Paused, PlayerStates.Idle }, PlayerEvents.SetVolume, (s, e, args) =>
            {
                if (args.Length > 0)
                {
                    float level = (float)args[0];
                    this.SetVolume(level);
                }
                else
                {
                    log.Warning($"{this.stateMachine.name}: set volume requires a level argument");
                }
                return this.stateMachine.GetState();
            });
            this.stateMachine.AddAction(PlayerStates.Playing, PlayerEvents.PlayCompleted, (s, e, args) =>
            {
                Task.Run(async () => await this.requestNext(this.device.Key));
                StartWaitTimer();
                return PlayerStates.WaitingNext;
            });
            this.stateMachine.AddAction(PlayerStates.Playing, PlayerEvents.ListFinished, (s, e, args) =>
            {
                // happens when a user skips forward on the lat item of a play list
                // logitech devices need to stop play
                Task.Run(async () => await StopPlaying());
                StartWaitTimer();
                return PlayerStates.Idle;
            });
            this.stateMachine.AddAction(PlayerStates.WaitingNext, PlayerEvents.ListFinished, (s, e, args) =>
            {
                StartWaitTimer();
                return PlayerStates.Idle;
            });

            this.stateMachine.AddAction(PlayerStates.Idle, PlayerEvents.WaitTimeout, (s, e, args) =>
            {
                return PlayerStates.SilentIdle;
            });
            this.stateMachine.AddAction(PlayerStates.WaitingNext, PlayerEvents.WaitTimeout, (s, e, args) =>
            {
                Task.Run(async () => await StopPlaying());
                log.Warning($"{device.DisplayName} timeout waiting for next playlist item");
                StartWaitTimer();
                return PlayerStates.Idle;
            });
            this.stateMachine.AddAction(
                new PlayerStates[] { PlayerStates.Playing, PlayerStates.Paused },
                PlayerEvents.Reset, (s, e, args) =>
                {
                    Task.Run(async () => await StopPlaying());
                    StartWaitTimer();
                    return PlayerStates.Idle;
                });
            this.stateMachine.AddAction(
                new PlayerStates[] { PlayerStates.WaitingNext },
                PlayerEvents.Reset, (s, e, args) =>
                {
                    StartWaitTimer();
                    return PlayerStates.Idle;
                });
        }
        private void StartWaitTimer()
        {
            if (waitingForEventTimer != null)
            {
                waitingForEventTimer.Dispose();
                waitingForEventTimer = null;
            }
            waitingForEventTimer = new Timer((s) =>
            {
                //log.Information("idle timer fired");
                OnEvent(PlayerEvents.WaitTimeout);
                waitingForEventTimer.Dispose();
                waitingForEventTimer = null;
            }, null, this.musicPlayerOptions.WaitingForEventTimer, Timeout.Infinite);
            //log.Information("idle timer started");
        }
    }
}
