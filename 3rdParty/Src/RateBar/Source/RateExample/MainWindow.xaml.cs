using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;

namespace RateExample
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Test

		double lastValue = 0;
		DateTime startTime = DateTime.Now;

		public void Start()
		{
			this.rateGraph1.Maximum = 100;
			Timer timer = new Timer(10);
			timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
			timer.Start();
		}

		int count = 0;
		void timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				TimeSpan endTime = DateTime.Now - startTime;
				Random rnd = new Random();
				lastValue = (Double)rnd.Next(Math.Max(0, (int)lastValue - 25), Math.Min(100, (int)lastValue + 25));

				this.Dispatcher.Invoke(new Action(() =>
				{
					this.rateGraph1.Value = endTime.TotalSeconds;
					this.rateGraph1.Rate = lastValue;
					this.rateGraph1.Caption = lastValue.ToString("N") + " /sec";
				}));

			}
			catch
			{
				return;
			}
		}

		#endregion

		public MainWindow()
		{
			InitializeComponent();
			Start();
		}
	}
}
