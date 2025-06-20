using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Abies;

// Make this accessible to the Abies.Types HandleCommand method
public abstract class ApiCommand : Abies.Command 
{
    public abstract Task<Abies.Message> ExecuteAsync();
}
