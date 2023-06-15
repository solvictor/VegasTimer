using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using ScriptPortal.Vegas;
using System.ComponentModel;

namespace VegasTimer
{
    public class Chronometer : DockableControl
    {
        private bool IsRunning;
        private DateTime StartTime;
        private readonly Label Time;
        private readonly Button Start;
        private readonly Button Reset;
        private readonly Timer Timer;
        private readonly VegasTimer VegasTimer;

        public Chronometer(VegasTimer vegasTimer) : base("Timer")
        {
            DefaultDockWindowStyle = DockWindowStyle.Floating;
            Dock = DockStyle.Fill;
            Text = "Timer";
            VegasTimer = vegasTimer;
            BackColor = Color.FromArgb(34, 34, 34);
            Closing += OnClose;
            DefaultFloatingSize = new Size(200, 120);

            Timer = new Timer()
            {
                Interval = 1
            };
            Timer.Tick += OnTick;

            Start = new Button
            {
                Text = "Start",
                Location = new Point(10, 50),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            Start.Click += ClickStart;
            Controls.Add(Start);

            Reset = new Button
            {
                Text = "Reset",
                Location = Point.Add(Start.Location, new Size(90, 0)),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            Reset.Click += ClickReset;
            Controls.Add(Reset);

            Time = new Label
            {
                Text = VegasTimer.Elapsed.ToString(@"hh\:mm\:ss\:fff"),
                Location = new Point(10, 10),
                Font = new Font("Arial", 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            Controls.Add(Time);
        }

        private void OnClose(object sender, CancelEventArgs e)
        {
            if (IsRunning)
            {
                ClickStart(this, EventArgs.Empty);
                Timer.Stop();
            }
        }

        private void ClickStart(object sender, EventArgs e)
        {
            if (IsRunning)
            {
                Timer.Stop();
                VegasTimer.Elapsed += DateTime.Now - StartTime;
                Start.Text = "Start";
            }
            else
            {
                Timer.Start();
                StartTime = DateTime.Now;
                Start.Text = "Pause";
            }
            IsRunning = !IsRunning;
        }

        private void ClickReset(object sender, EventArgs e)
        {
            Timer.Stop();
            VegasTimer.Elapsed = TimeSpan.Zero;
            IsRunning = false;
            Time.Text = "00:00:00:000";
        }

        private void OnTick(object sender, EventArgs e)
        {
            TimeSpan currentTime = DateTime.Now - StartTime + VegasTimer.Elapsed;
            Time.Text = currentTime.ToString(@"hh\:mm\:ss\:fff");
        }
    }

    public class VegasTimer : ICustomCommandModule
    {
        public Vegas Vegas = null;
        public TimeSpan Elapsed = TimeSpan.Zero;

        public ICollection GetCustomCommands()
        {
            CustomCommand timer = new CustomCommand(CommandCategory.Tools, "VegasTimer")
            {
                DisplayName = "Timer",
                MenuSelectMessage = "Open a timer."
            };
            
            timer.Invoked += (s, a) =>
            {
                if (!Vegas.ActivateDockView("TimerView"))
                {
                    Chronometer chrono = new Chronometer(this)
                    {
                        AutoLoadCommand = timer
                    };

                    Vegas.LoadDockView(chrono);
                }
            };

            return new CustomCommand[] { timer };
        }

        public void InitializeModule(Vegas vegas)
        {
            Vegas = vegas;
        }
    }
}
