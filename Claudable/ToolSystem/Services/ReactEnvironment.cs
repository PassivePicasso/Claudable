using JavaScriptEngineSwitcher.Core;
using React;

namespace Claudable.ToolSystem.Services
{
    public class ReactEnvironment : IReactEnvironment
    {
        private readonly IJsEngine _jsEngine;

        public async Task<bool> ValidateComponentAsync(string componentCode)
        {
            try
            {
                _jsEngine.Evaluate($@"
                try {{ 
                    eval('{componentCode}');
                    true;
                }} catch (e) {{ 
                    throw new Error(`Invalid component: ${e.message}`);
                }}
            ");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
