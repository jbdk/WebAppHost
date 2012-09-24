using System;
using System.Threading;
using Castle.Core;
using SignalR.Hubs;

namespace SampleApp.Hubs
{
	public class CounterHub : Hub
	{
		private static int _counter = new Random().Next();
		private Timer _timer;
		private readonly object _lockObject = new object();

		public CounterHub()
		{
			
		}

		public void Start()
		{
			lock (_lockObject)
			{
				if (_timer == null)
				{
					_timer = new Timer(TimerCallback, null, 100, 1000);
				}
			}
		}

		public void Stop()
		{
			lock (_lockObject)
			{
				if (_timer != null)
				{
					_timer.Dispose();
					_timer = null;
				}
			}
		}

		private void TimerCallback(object state)
		{
			if (Clients != null)
			{
				Clients.setCounter(++_counter);
			}
		}
	}
}