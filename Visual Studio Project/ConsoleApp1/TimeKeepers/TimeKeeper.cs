using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// Used to run logic via timers
    /// </summary>
    internal abstract class TimeKeeper
    {
        Stopwatch swatch = new Stopwatch();  // internal timer used to track time
        protected int iTimeToCheck;  // the time in milliseconds that 'swatch' will be checked for
        bool bActivated = false;  // Whether the timer is running and changes over time, while running certain functions
        protected int iRepeatsAfterReset = 0;  // The amount of times the timer has reached the time it is detecting after the most recent reset
        public bool bRemoveTimer = false;  // Whether external systems should ignore this timer forever

        /// <summary>
        /// What occurs at every server tick
        /// </summary>
        public void Update()
        {
            if (iRepeatsAfterReset > 0)
            {
                if (ResetCondition())
                {
                    ResetTimer();
                }
            }
            if (bActivated)
            {
                if (InterruptTimer())
                {
                    swatch.Stop();
                    bActivated = false;
                }
                else if (swatch.ElapsedMilliseconds > iTimeToCheck)
                {
                    iRepeatsAfterReset++;
                    TimeComplete();
                    if (ImmediateRestart())
                    {
                        swatch.Restart();
                    }
                    else
                    {
                        swatch.Stop();
                        bActivated = false;
                    }
                }
            }
            else
            {
                if (ActivationCondition())
                {
                    swatch.Restart();
                    bActivated = true;
                }
            }
        }

        /// <summary>
        /// What condition needs to be met for the timer to start
        /// </summary>
        /// <returns>Whether the timer should start</returns>
        protected virtual bool ActivationCondition() { return true; }

        /// <summary>
        /// What condition can cause the time to stop before reaching its intended check
        /// </summary>
        /// <returns>Whether the timer should be interrupted (true = stop timer)</returns>
        protected virtual bool InterruptTimer() { return !ActivationCondition(); }

        /// <summary>
        /// What occurs when the timer reaches its expected duration
        /// </summary>
        protected virtual void TimeComplete() { }

        /// <summary>
        /// Whether the timer can restart immediately due to an alternate condition
        /// </summary>
        /// <returns>Whether the timer can immdiately restart</returns>
        protected virtual bool ImmediateRestart() { return false; }

        /// <summary>
        /// Under what condition that 'iRepeatsAfterReset' will be set to 0
        /// </summary>
        /// <returns>Whether 'iRepeatsAfterReset' should reset</returns>
        protected virtual bool ResetCondition() { return false; }

        /// <summary>
        /// What occurs during a reset of 'iRepeatsAfterReset'
        /// </summary>
        public void ResetTimer() { iRepeatsAfterReset = 0; }
    }
}
