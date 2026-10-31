# Project Seal Stamp

A 3D character controller and mechanics framework built in Unity, inspired by classic 3D platformers (Super Mario 64) and modern action games (Genshin Impact). The project features a modular Finite State Machine (FSM), seal transformation abilities, item suction and kicking, camera-relative movement, an animation controller with validation rules, and a dedicated Game Time synchronization system.

---

## Table of Contents
- [Architecture Overview](#architecture-overview)
- [Player State Machine & States](#player-state-machine--states)
  - [Grounded Movement (Walking & Running)](#grounded-movement-walking--running)
  - [Airborne & Aerial Agility](#airborne--aerial-agility)
  - [Wall & Ledge Interaction](#wall--ledge-interaction)
  - [Special Acrobatics](#special-acrobatics)
  - [Crouch & Seal Transformation](#crouch--seal-transformation)
- [StateLogger](#statelogger)
- [Game Time System (Pausing & Desync Reconciliation)](#game-time-system-pausing--desync-reconciliation)
- [Player Animation Controller](#player-animation-controller)
  - [Supported Animation States](#supported-animation-states)
  - [Animator Parameters](#animator-parameters)
  - [Core Animation Validation Rules](#core-animation-validation-rules)
  - [Model Tilt in Air](#model-tilt-in-air)
- [Controls Summary](#controls-summary)

---

## Architecture Overview

The character controller is divided into modular, decoupled systems:
- **`SM64PlayerController`**: Main controller handling physics, movement configuration, velocity, collision detection, and state machine lifecycle.
- **`PlayerStateMachine`**: Manages state transitions, previous/current state references, and invokes the `OnStateChanged` event.
- **`IPlayerState` / `PlayerState`**: Interface and abstract base defining lifecycle hooks (`Enter`, `HandleInput`, `LogicUpdate`, `PhysicsUpdate`, `Exit`).
- **`SM64PlayerInput`**: Reads keyboard, mouse, and gamepad inputs, supporting input locking/blocking for menus and UI.
- **`GameTime` & `GameTimeManager`**: Centralized simulation clock decoupled from `Time.timeScale` for custom pause and state reconciliation.
- **`PlayerAnimationController`**: Evaluates active states and controller velocities to drive Animator states and parameter updates.
- **`StateLogger`**: Read-only observer component that reports player state transitions to the Unity console.

---

## Player State Machine & States

```
                ┌───────────────────────────────────┐
                │        PlayerWalkingState         │
                └──────────▲──────────────┬─────────┘
                           │              │ (Hold direction 3s)
            (No input 0.25s│              ▼
            momentum kept) │      PlayerRunningState
                           │              │
                ┌──────────┴──────────────┴─────────┐
                │        PlayerAirborneState        │
                └──────────┬──────────────▲─────────┘
                           │              │
            ┌──────────────┼──────────────┼──────────────┐
            ▼              ▼              ▼              ▼
     LongJumpState   BackflipState   WallSlideState  LedgeGrabState
```

### Grounded Movement (Walking & Running)

#### `PlayerWalkingState`
- Standard grounded movement and idle handling.
- Smooth acceleration and deceleration toward `walkSpeed` (default: 3.5 m/s).
- **Run Activation**: Tracks continuous directional hold. Holding a single movement direction for `runActivationTime` (default: 3.0s) seamlessly transitions into `PlayerRunningState`.

#### `PlayerRunningState`
- High-speed movement targeting `runSpeed` (default: 7.0 m/s) with strafing steering.
- Spawns periodic dust/running particle effects (`runParticleInterval`).
- **Stop Delay & Momentum Preservation**:
  - When the player releases movement keys while grounded, the controller waits for `runStopDelay` (default: **0.25 seconds**).
  - During this window and upon transitioning to `WalkingState`, the player's speed is **not abruptly cleared or clamped**.
  - After 0.25 seconds without movement keys, the player is immediately transitioned to `WalkingState` (grounded state), allowing `WalkingState`'s natural deceleration to smoothly bring the character to idle.
  - This prevents the animation system and state machine from staying stuck in the running state while the player is already decelerating.
  - **Airborne Decoupling**: If the player is airborne (jumping or stepping off an edge), standard airborne physics and landing evaluation apply instead.

---

### Airborne & Aerial Agility

#### `PlayerAirborneState`
- Controls gravity, terminal fall speed (`maxFallSpeed`), and air steering (`airControl`).
- Gradually decays horizontal velocity over time (`airSpeedDecay`).
- **Responsive Double Jump**: Instant vertical velocity reset to `doubleJumpForce` with directional air reorientation.
- **Coyote Time**: Allows jumping within `coyoteTime` (0.15s) after walking off ledges.
- **Jump Buffering**: Inputs pressed within `jumpBufferTime` (0.15s) before landing trigger immediately upon touchdown.
- **Speed-Preserving Landing**: Evaluates horizontal velocity upon landing (`RunLandingThreshold = walkSpeed * 1.5`); maintains `RunningState` if landing with sufficient forward speed.

#### `PlayerWallJumpState`
- Triggered when pressing Jump while sliding or near a valid wall.
- Launches the player away from the wall normal with combined horizontal and vertical impulse.

#### `PlayerLeapState`
- Triggered on a 3rd jump after a double jump when no air jumps remain.
- Launches the player forward with powerful horizontal impulse (`leapSpeedMultiplier`).
- If airborne for over 1.0 second, automatically initiates Seal transformation.

---

### Wall & Ledge Interaction

#### `PlayerWallSlideState`
- Utilizes dual forward raycast sensors (Top & Middle dots).
- When both sensors detect a wall surface, character clings for `wallClingDuration` (0.35s) before slowly sliding downward at `wallSlideSpeed`.

#### `PlayerLedgeGrabState`
- Activates when the Middle sensor hits a wall while the Top sensor is clear.
- Snaps character to `ledgeHangOffset` facing the wall.
- Supports climbing up to the ledge surface (`IsClimbing`) or dropping down.

---

### Special Acrobatics

- **`PlayerLongJumpState`**: Triggered by crouching while running, launching forward with flat trajectory and high speed.
- **`PlayerBackflipState`**: Triggered by jumping backward while crouching, providing high vertical height with reverse momentum.
- **`PlayerGroundPoundState`**: Triggered by crouching in mid-air (after at least double jump). Features three explicit phases:
  1. **Stall**: Brief hang in the air (`IsStalling`).
  2. **Slam**: Rapid downward plunge at high velocity (`IsSlamming`).
  3. **Recovery / Land**: Spawns ground impact particle effect, brief recovery delay (`IsRecovering`), and allows instant jump-cancel.

---

### Crouch & Seal Transformation

#### `PlayerCrouchingState`
- Reduces movement speed (`crouchSpeedMultiplier = 0.5`).
- Swaps visual model to Crouch Model if assigned.
- Pressing Jump while crouching triggers the Seal transformation.

#### `PlayerSealState`
- Transforms the player into a seal with transformation VFX and model swapping.
- **Nudge Movement**: Does not walk; advances via periodic nudges (`NudgeInterval`).
- **`PlayerSealSuckingState`**: Holding Shift enters suction mode. Suction cone pulls in nearby suckable objects (`SealSuction`).
- Spits swallowed items on action press; pressing Space reverts back to humanoid form with revert VFX.

---

## StateLogger

Located in `Assets/_Canon/Script/StateLogger.cs`.

`StateLogger` is an observer component that monitors player state transitions and outputs them to the Unity Console:
- **Strictly Read-Only**: Subscribes exclusively to `PlayerStateMachine.OnStateChanged`. Has **zero authority or methods to modify the player state machine or controller**.
- **Formatted Logging**: Formats state class names cleanly (e.g. `[StateLogger] Player state changed: Running -> Walking`).
- **Auto-Discovery**: Automatically finds `SM64PlayerController` in the scene if unassigned.
- **Safe Lifecycle**: Subscribes on `OnEnable` and unsubscribes on `OnDisable` / `OnDestroy` to prevent memory leaks.

---

## Game Time System (Pausing & Desync Reconciliation)

Located in `Assets/_Canon/Script/GameTime.cs`.

To prevent gameplay mechanics, animation timers, and network ticks from relying directly on Unity's global `Time.time` or interfering with `Time.timeScale`, game time is encapsulated in the static `GameTime` class and managed by `GameTimeManager`.

### Public Access Values & Methods

```csharp
// Global Access
float currentTime = SM64.GameTime.Time;
float delta       = SM64.GameTime.DeltaTime;
bool isPaused     = SM64.GameTime.IsPaused;
ulong ticks       = SM64.GameTime.TickCount;

// Player Controller Shortcuts
float pTime   = playerController.GameTime;
float pDelta  = playerController.GameDeltaTime;
bool pPaused  = playerController.IsGamePaused;

// Pausing Controls (Does not freeze Unity's rendering or UI)
SM64.GameTime.Pause();
SM64.GameTime.Resume();
SM64.GameTime.TogglePause();

// State Desync Reconciliation / Network Rollback
SM64.GameTime.SetTime(reconciledServerTime);
```

### Why GameTime Is Crucial
1. **Menu & UI Pausing**: Enables pausing player simulation, state timers, and cooldowns without setting `Time.timeScale = 0`, keeping UI animations, camera movement, or background visual effects completely smooth.
2. **Desync Reconciliation**: When networking or replaying character physics, client state desyncs can be corrected by calling `GameTime.SetTime(newTime)`. Systems listening to `GameTime.OnTimeResynced` can snap or interpolate without discontinuities in Unity's internal engine clock.

---

## Player Animation Controller

Located in `Assets/_Canon/Script/PlayerAnimationController.cs`.

The animation controller monitors `SM64PlayerController`, evaluates physical parameters, and crossfades Animator states.

### Supported Animation States (`PlayerAnimationType`)

| Enum Value | Mapped State / Condition |
| :--- | :--- |
| `Idle` | `PlayerWalkingState` with horizontal speed $\le$ `walkSpeedThreshold` |
| `Walk` | `PlayerWalkingState` with horizontal speed > `walkSpeedThreshold` |
| `Run` | `PlayerRunningState` |
| `Jump` | `PlayerAirborneState` ascending ($V_y > \text{fallThreshold}$) |
| `DoubleJump` | `PlayerAirborneState` ascending with `HasDoubleJumped == true` |
| `Fall` | `PlayerAirborneState` descending ($V_y \le \text{fallThreshold}$) |
| `LongJump` | `PlayerLongJumpState` |
| `Backflip` | `PlayerBackflipState` |
| `WallSlide` | `PlayerWallSlideState` |
| `WallJump` | `PlayerWallJumpState` |
| `LedgeHang` | `PlayerLedgeGrabState` hanging (`IsClimbing == false`) |
| `LedgeClimb` | `PlayerLedgeGrabState` climbing up (`IsClimbing == true`) |
| `GroundPoundStall`| `PlayerGroundPoundState` during initial stall (`IsStalling`) |
| `GroundPoundSlam` | `PlayerGroundPoundState` descending rapidly (`IsSlamming`) |
| `GroundPoundLand` | `PlayerGroundPoundState` impact & recovery (`IsRecovering` or grounded) |
| `Leap` | `PlayerLeapState` |
| `Crouch` | `PlayerCrouchingState` |
| `Seal` | `PlayerSealState` |
| `SealSuck` | `PlayerSealSuckingState` |

### Animator Parameters
If present on the character's `Animator`, the controller automatically updates:
- **`Speed`** (*Float*): Horizontal speed magnitude.
- **`VerticalSpeed`** (*Float*): Vertical velocity ($Y$ axis).
- **`IsGrounded`** (*Bool*): CharacterController ground check status.
- **`IsRunning`** (*Bool*): Whether current movement substate is `Running`.
- **`IsAirborne`** (*Bool*): Inversion of grounded status.
- **`IsWallSliding`** (*Bool*): Wall slide state active.
- **`IsLedgeHanging`** (*Bool*): Ledge grab state active.
- **`IsCrouching`** (*Bool*): Crouch state active.
- **`IsSeal`** (*Bool*): Seal state active (non-sucking).
- **`IsSealSucking`** (*Bool*): Seal suction sub-state active.
- **`IsDoubleJumping`** (*Bool*): In double-jump ascension.
- **`IsClimbing`** (*Bool*): Ledge climb animation in progress.
- **`GameTime`** (*Float*): Synchronized simulation time from `GameTime.Time`.
- **`AnimState`** (*Int*): Integer representation of `PlayerAnimationType`.

### Core Animation Validation Rules
The controller enforces rules to prevent invalid animation artifacts:
1. **Rule 1 (Cannot Run while Airborne)**: If a grounded locomotion animation (`Run`, `Walk`, `Idle`) is requested while `IsGrounded() == false`, it is corrected to `Jump` or `Fall`.
2. **Rule 2 (Cannot Fall while Grounded)**: If `Fall` is requested while `IsGrounded() == true`, it is corrected to `Idle`, `Walk`, or `Run` based on current speed and substate.

### Model Tilt in Air
While airborne, `PlayerAnimationController` tilts the visual model root along the pitch and roll axes based on movement input, smoothly returning to upright when grounded or input is neutral.

---

## Controls Summary

| Action | Keyboard / Mouse | Gamepad |
| :--- | :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` | Left Stick |
| **Look / Orbit** | Mouse Movement | Right Stick |
| **Jump** | `Space` | A / South Button |
| **Double Jump** | `Space` in air | A / South Button in air |
| **Crouch** | `Left Ctrl` / `C` | Left Trigger / B |
| **Long Jump** | Run + Crouch + Jump | Run + LT + A |
| **Backflip** | Crouch + Backward Jump | LT + Back + A |
| **Ground Pound** | Crouch while in air (jump $\ge$ 2) | LT / B in air |
| **Kick Ball** | `E` / `F` / Left Click | X / West Button |
| **Seal Transform** | Crouch + Jump | LT + A while crouched |
| **Seal Suction** | Hold `Left Shift` | Hold Right Trigger |
| **Seal Spit** | Tap `Left Shift` with item | Tap Right Trigger |
| **Revert from Seal**| `Space` (Jump) | A / South Button |
| **Menu Toggle** | `ESC` | Start / Options |

