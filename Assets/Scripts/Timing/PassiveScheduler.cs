using System;
using UnityEngine;

namespace JLChnToZ.Toolset.Timing {
    public class PassiveScheduler {
        long scheduledTime;
        bool hasScheduledTime;
        public DateTime ScheduledTime {
            get { return new DateTime(scheduledTime, DateTimeKind.Utc).ToLocalTime(); }
            set {
                scheduledTime = value.ToUniversalTime().Ticks;
                hasScheduledTime = true;
            }
        }

        float rate;
        public float Rate {
            get { return rate; }
            set { rate = value; }
        }

        public PassiveScheduler() { }

        public PassiveScheduler(float rate) {
            this.rate = rate;
        }

        public PassiveScheduler(float rate, DateTime previousScheduledTime) : this(rate) {
            ScheduledTime = previousScheduledTime;
        }

        public int Update() {
            return Update(DateTime.UtcNow);
        }

        public int Update(DateTime currentTime) {
            if(rate <= 0) {
                hasScheduledTime = false;
                return 0;
            }
            long currentTicks = currentTime.ToUniversalTime().Ticks;
            long interval = (long)Math.Floor(TimeSpan.TicksPerSecond / rate);
            int times = 0;
            if(hasScheduledTime && scheduledTime <= currentTicks) {
                long diffTicks = currentTicks - scheduledTime;
                times = (int)(diffTicks / interval) + 1;
                if(OnUpdate != null)
                    OnUpdate.Invoke(times);
                if(OnUpdateEach != null)
                    for(int i = 0; i < times; i++) OnUpdateEach.Invoke();
                currentTicks -= diffTicks - interval * times;
            }
            if(currentTicks >= scheduledTime) {
                scheduledTime = currentTicks + interval;
                hasScheduledTime = true;
            }
            return times;
        }

        public event Action<int> OnUpdate;
        public event Action OnUpdateEach;
    }
}
