using LiveCharts;
using LiveCharts.Helpers;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace CSGODataExtractor
{
    public partial class plot : Form
    {
        public plot(List<double> X, List<double> Y)
        {
            InitializeComponent();
            
            
            cartesianChart1.AxisX.Add(new Axis
            {
                Title = "Ticks",
                Labels = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }
            });

            cartesianChart1.AxisY.Add(new Axis
            {
                Title = "Angles",
            });

            cartesianChart1.LegendLocation = LegendLocation.Right;

            //modifying the series collection will animate and update the chart
            cartesianChart1.Series.Add(new LineSeries
            {
                Values =X.AsChartValues(),
                LineSmoothness = 0,
                Title = "ViewAngleX",
            });
            cartesianChart1.Series.Add(new LineSeries
            {
                Values = Y.AsChartValues(),
                LineSmoothness = 0,
                Title = "ViewAngleY",
            });



        }
    }

}
