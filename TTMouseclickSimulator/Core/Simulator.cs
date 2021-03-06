﻿using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core
{
    public class Simulator
    {
        private readonly IAction mainAction;
        private readonly AbstractWindowsEnvironment environmentInterface;

        private readonly StandardInteractionProvider provider;
        private readonly Action cancelCallback;

        private volatile bool canceled = false;


        public event Action SimulatorStarted;
        public event Action SimulatorStopped;

        /// <summary>
        /// When an exception (which is not a <see cref="SimulatorCanceledException"/>) occurs while an action runs,
        /// this allows the action to check if it should retry or cancel the simulator (in that case, it should
        /// throw an <see cref="SimulatorCanceledException"/>).
        /// </summary>
        public Func<ExceptionDispatchInfo, Task<bool>> AsyncRetryHandler;
        

        public Simulator(IAction mainAction, AbstractWindowsEnvironment environmentInterface)
        {
            if (mainAction == null)
                throw new ArgumentNullException(nameof(mainAction));
            if (environmentInterface == null)
                throw new ArgumentNullException(nameof(environmentInterface));


            this.mainAction = mainAction;
            this.environmentInterface = environmentInterface;

            provider = new StandardInteractionProvider(this, environmentInterface, out cancelCallback);
        }

        /// <summary>
        /// Asynchronously runs this simulator.
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            if (canceled)
                throw new InvalidOperationException("The simulator has already been canceled.");

            try
            {
                using (provider)
                {
                    OnSimulatorStarted();

                    // InitializeAsync() does not need to be in the try block because it has its own.
                    await provider.InitializeAsync();

                    for (;;)
                    {
                        try
                        {
                            // Run the action.
                            await mainAction.RunAsync(provider);

                            // Normally the main action would be a CompoundAction that never returns, but
                            // it is possible that the action will return normally.
                        }
                        catch (Exception ex) when (!(ex is SimulatorCanceledException))
                        {
                            await provider.CheckRetryForExceptionAsync(ExceptionDispatchInfo.Capture(ex));
                            continue;
                        }
                        break;
                    }
                }

            }
            finally
            {
                canceled = true;
                OnSimulatorStopped();
            }
        }

        /// <summary>
        /// Cancels the simulator. This method can be called from the GUI thread while
        /// the task that runs RunAsync is still active. It can also be called from
        /// another thread.
        /// </summary>
        public void Cancel()
        {
            canceled = true;
            cancelCallback();
        }


        protected void OnSimulatorStarted() => SimulatorStarted?.Invoke();

        protected void OnSimulatorStopped() => SimulatorStopped?.Invoke();
    }
}
