# Source map

- `Networking/RelaySessionUI.cs`: anonymous auth, Relay allocation, host/join/leave.
- `Player/NetworkPlayerController.cs`: owner movement, PC keyboard/mouse, mobile hooks.
- `Player/OwnerNetworkTransform.cs`: owner-authoritative NetworkTransform for prototype movement.
- `Combat/NetworkHealth.cs`: server-write replicated health and healing.
- `Combat/NetworkWeapon.cs`: fire request and server-side raycast.
- `World/NetworkSafeZone.cs`: shrinking safe zone and outside-zone damage.
- `World/NetworkLootPickup.cs`: server distance validation and medkit pickup.
- `World/NetworkMatchManager.cs`: timer and last-survivor winner state.
- `UI/NetworkHUD.cs`: health, player count, timer and result.
- `UI/FireButton.cs`: touch fire adapter.
- `UI/MobileJoystickBridge.cs`: joystick and look-pad bridge.
