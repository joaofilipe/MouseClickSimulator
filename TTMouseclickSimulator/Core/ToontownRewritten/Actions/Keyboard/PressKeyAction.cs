﻿using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Keyboard
{
    /// <summary>
    /// An action for pressing a key for a specific amount of time.
    /// </summary>
    public class PressKeyAction : AbstractAction
    {

        private readonly AbstractWindowsEnvironment.VirtualKeyShort key;
        private readonly int duration;

        public PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort key, int duration)
        {
            this.key = key;
            this.duration = duration;
        }


        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            provider.PressKey(key);
            // Use a accurate timer for measuring the time after we need to release the key.
            await provider.WaitAsync(duration, true);
            provider.ReleaseKey(key);
        }


        public override string ToString() => $"Press Key – Key: {key}, Duration: {duration}";
    }
}
