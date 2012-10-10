/*
 * Developer : Ian Wright
 * Date : 31/05/2012
 * All code (c) Ian Wright. 
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RateBar
{
	/// <summary>
	/// Interaction logic for RateGraph.xaml
	/// </summary>
	public partial class RateGraph : RateBase
	{
        /// <summary>
        /// The polygon of points
        /// </summary>
        private Polygon polygon;
        private Queue<double[]> _savedRateChanges = new Queue<double[]>();         // RKS
        private bool _isLoaded = false;                                          // RKS

        /// <summary>
        /// The list of points
        /// </summary>
        public ObservableCollection<Double[]> ratePoints = new ObservableCollection<Double[]>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RateGraph"/> class.
        /// </summary>
		public RateGraph()
		{
			InitializeComponent();
			this.Template = (ControlTemplate)this.Resources["rateGraphTemplate"];

            // Find the polygon once loaded, it's points
			// will be modified as we go along
            this.Loaded += (o, e) => 
            {
                if (!this._isLoaded)
                {
                    this.polygon = this.Template.FindName("graph", this) as Polygon;

                    // Ensure we have the bottom left point of a graph to fill correctly
                    // and another point which will be moved
                    this.ratePoints.Add(new Double[] { 0, 0 });
                    this.ratePoints.Add(new Double[] { 0, 0 });
                    this._isLoaded = true;
                }
            };
		}

        /// <inheritdoc />
        protected override void OnRateChanged(double oldValue, double newValue)
        {
            if (!_isLoaded)
            {
                // The data objects are not allocated until the loaded event, but OnRateChanged fires early.  Save the event and replay later.
                Double[] rateChangedEvent = new Double[] { oldValue, newValue };
                _savedRateChanges.Enqueue(rateChangedEvent);
                return;
            }

            // Process any saved rate changes
            while (_savedRateChanges.Count > 0)
            {
                double[] retrieveSavedChange = _savedRateChanges.Dequeue();
                ProcessRateChangedEvent(retrieveSavedChange[0], retrieveSavedChange[1]);
            }

            ProcessRateChangedEvent(oldValue, newValue);
        }

        private void ProcessRateChangedEvent(double oldValue, double newValue)
        {
            // Modify the Maximum if the Rate exceeds it
            if (newValue * 1.2 > this.RateMaximum)
                this.RateMaximum = newValue * 1.2;

            // Move the existing point along the X-axis, this ensures our fill works correctly.
            this.ratePoints[0] = new Double[] { this.Value, 0 };

            // Add on the new point
            this.ratePoints.Add(new Double[] { this.Value, newValue });

            this.polygon.Points = new PointCollection(this.ratePoints.Select(dba =>
            {
                // Don't adjust the height for the line that runs alone the bottom
                return new Point(CalculateX(dba[0]), CalculateY(dba[1]));
            }).AsParallel());

            // Update the base rate
            base.OnRateChanged(oldValue, newValue);
        }

		/// <summary>
		/// Returns the X position of a point on the graph based on the progress value
		/// </summary>
		/// <param name="progressValue">The progress value to calculate the X point for</param>
		private Double CalculateX(Double progressValue)
		{
			//RKSreturn progressValue / this.Maximum * this.Width;
            return progressValue / this.Maximum * this.ActualWidth;
        }

		/// <summary>
		/// Returns the Y position of a point on the graph based on the rate value
		/// </summary>
		/// <param name="rateValue">The rate value to calculate the Y point for</param>
		private Double CalculateY(Double rateValue)
		{
			// Just return the height for 0 values to keep the graph on the baseline
            if (rateValue == this.RateMinimum)
            {
                // RKS return this.Height;
                return this.ActualHeight;
            }

			// The range that the graph is currently displaying
			Double range = this.RateMaximum - this.RateMinimum;
			//RKSreturn this.Height - ((this.Height / range) * rateValue);
            return this.ActualHeight - ((this.ActualHeight / range) * rateValue);
        }
	}
}
