// net48 has no System.Runtime.CompilerServices.IsExternalInit, which the C#
// compiler requires to emit `init` accessors and record types. This shim makes
// records / record structs / init-only setters compile on the classic framework.
// It must be internal-per-assembly; other projects that need it declare their own.

namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
