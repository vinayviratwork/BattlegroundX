# Relay and multiplayer configuration

## Cloud setup
- Create one Unity Cloud project.
- Link the Unity Editor project to it.
- Enable Authentication and Relay.
- Both host and client must use the same Cloud project and environment.
- Relay join codes are temporary; generate a new one for each host session.

## Prototype architecture
- Host client also acts as server.
- Relay forwards traffic between clients.
- Player movement is owner-authoritative for this prototype.
- Health state is server-write.
- Weapon hit requests are sent to server and raycast is performed on server.

## Security limitations
This is not anti-cheat. A production game must:
- Server-author player movement or validate movement speed/teleports.
- Validate shot origin against server-known player pose.
- Validate weapon fire cadence, ammo, range and line of sight.
- Rate-limit all client RPCs.
- Prevent clients from choosing arbitrary pickup targets.
- Add authentication/account lifecycle, moderation and abuse reporting.
- Use dedicated servers for competitive matches.

## Scale
Relay plus host-as-server is for prototype sessions only. It does not implement matchmaking, dedicated server fleet, 100-player capacity, regional routing or load-tested scale.
