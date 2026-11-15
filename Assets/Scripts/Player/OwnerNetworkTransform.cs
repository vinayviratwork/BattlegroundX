using Unity.Netcode;

// Prototype setting: let the owning client publish player movement.
// Production games should use server-authoritative movement validation.
public class OwnerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}
