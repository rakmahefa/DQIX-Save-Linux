// Expose the original `internal` visibility of the moved types to the two
// consumers that previously shared the same assembly:
//  - DQ9_Cheat        : legacy WinForms application
//  - DQ9.Avalonia     : new cross-platform shell (transitional)
//  - DQ9.Core.Tests   : reference test suite
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DQ9_Cheat")]
[assembly: InternalsVisibleTo("DQ9.Avalonia")]
[assembly: InternalsVisibleTo("DQ9.Core.Tests")]
