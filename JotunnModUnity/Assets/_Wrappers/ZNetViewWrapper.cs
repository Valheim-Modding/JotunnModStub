namespace ValheimMod.UnityWrappers
{
    /// <summary>
    /// A wrapper for Valheim's <see cref="ZNetView" />. Put this on your prefabs instead of <see cref="ZNetView" />.
    /// </summary>
    /// <remarks>
    /// Since Valheim's assemblies can't be redistributed they'll get new GUIDs when they're imported.
    /// Wrapping the class avoids broken script references between machines.
    /// </remarks>
    public class ZNetViewWrapper : ZNetView
    {

    }
}