using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Sleipnir.App.Models;
using Sleipnir.App.Services;

namespace Sleipnir.App.Views
{
    public partial class SprintTimelineWindow : Window
    {
        private readonly Sprint _sprint;
        private readonly IDataService _dataService;
        private readonly List<Issue> _sprintIssues;

        private bool _isInitialized = false;

        public SprintTimelineWindow(Sprint sprint, List<Issue> allProjectIssues, IDataService dataService)
        {
            InitializeComponent();
            _sprint = sprint;
            _dataService = dataService;
            _sprintIssues = allProjectIssues.Where(i => i.SprintId == sprint.Id).ToList();

            SprintTitleText.Text = $"{sprint.Name} Timeline";
            SprintDateText.Text = $"{sprint.StartDate:dd.MM.yyyy} - {sprint.EndDate:dd.MM.yyyy}";

            _isInitialized = true;
            SetDefaultInterval();
            Loaded += async (s, e) => await RefreshData();
        }

        private void SetDefaultInterval()
        {
            if (!_isInitialized) return;

            DateTime from, to;
            to = DateTime.Now;

            if (HoursRadio.IsChecked == true)
            {
                from = DateTime.Now.AddHours(-24);
            }
            else if (MinutesRadio.IsChecked == true)
            {
                from = DateTime.Now.AddHours(-1);
            }
            else // Days
            {
                from = _sprint.StartDate.Date;
                to = _sprint.EndDate.Date.AddDays(1).AddTicks(-1);
                if (to > DateTime.Now) to = DateTime.Now;
            }

            _isInitialized = false; // Prevent trigger during setup
            FromDate.SelectedDate = from.Date;
            FromTime.Text = from.ToString("HH:mm");
            ToDate.SelectedDate = to.Date;
            ToTime.Text = to.ToString("HH:mm");
            _isInitialized = true;
        }

        private async void Scale_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            SetDefaultInterval();
            await RefreshData();
        }

        private async void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            await RefreshData();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }

        private DateTime? GetFromDateTime()
        {
            if (FromDate.SelectedDate == null) return null;
            if (TimeSpan.TryParse(FromTime.Text, out TimeSpan ts))
            {
                return FromDate.SelectedDate.Value.Date.Add(ts);
            }
            return FromDate.SelectedDate.Value.Date;
        }

        private DateTime? GetToDateTime()
        {
            if (ToDate.SelectedDate == null) return null;
            if (TimeSpan.TryParse(ToTime.Text, out TimeSpan ts))
            {
                return ToDate.SelectedDate.Value.Date.Add(ts);
            }
            return ToDate.SelectedDate.Value.Date;
        }

        private async Task RefreshData()
        {
            try
            {
                LoadingText.Visibility = Visibility.Visible;
                ChartCanvas.Children.Clear();

                int scaleMode = 0; // 0=Days, 1=Hours, 2=Minutes
                if (HoursRadio.IsChecked == true) scaleMode = 1;
                else if (MinutesRadio.IsChecked == true) scaleMode = 2;

                var data = await ProcessLogs(scaleMode);
                
                LoadingText.Visibility = Visibility.Collapsed;
                DrawChart(data, scaleMode);
            }
            catch (Exception ex)
            {
                LoadingText.Text = "Error: " + ex.Message;
                LoadingText.Foreground = Brushes.Red;
            }
        }

        private async Task<List<StatusCounts>> ProcessLogs(int scaleMode)
        {
            var logsByIssue = new Dictionary<Guid, List<IssueLog>>();
            foreach (var issue in _sprintIssues)
            {
                var logs = await _dataService.GetLogsAsync(issue.Id);
                logsByIssue[issue.Id] = logs.OrderBy(l => l.Timestamp).ToList();
            }

            var result = new List<StatusCounts>();
            
            DateTime startDate = GetFromDateTime() ?? _sprint.StartDate.Date;
            DateTime endDate = GetToDateTime() ?? DateTime.Now;
            
            if (endDate < startDate) endDate = startDate.AddHours(1);

            if (scaleMode == 2) // Minutes (every minute)
            {
                for (var time = startDate; time <= endDate; time = time.AddMinutes(1))
                {
                    result.Add(GetCountAtPoint(time, logsByIssue));
                }
            }
            else if (scaleMode == 1) // Hours
            {
                for (var time = startDate; time <= endDate; time = time.AddHours(1))
                {
                    result.Add(GetCountAtPoint(time, logsByIssue));
                }
            }
            else // Days
            {
                for (var date = startDate; date <= endDate.Date; date = date.AddDays(1))
                {
                    var endOfDay = date.Date.AddDays(1).AddTicks(-1);
                    result.Add(GetCountAtPoint(endOfDay, logsByIssue));
                }
            }

            return result;
        }

        private StatusCounts GetCountAtPoint(DateTime pointInTime, Dictionary<Guid, List<IssueLog>> logsByIssue)
        {
            var counts = new StatusCounts { Time = pointInTime };
            foreach (var issue in _sprintIssues)
            {
                var status = GetStatusAtSpecificTime(issue, pointInTime, logsByIssue[issue.Id]);
                switch (status.ToLower())
                {
                    case "open": counts.Open++; break;
                    case "in progress": counts.InProgress++; break;
                    case "in testing": 
                    case "testing": counts.Testing++; break;
                    case "finished":
                    case "done": counts.Done++; break;
                }
            }
            return counts;
        }

        private string GetStatusAtSpecificTime(Issue issue, DateTime time, List<IssueLog> logs)
        {
            var lastStatusLog = logs.LastOrDefault(l => l.FieldChanged != null && l.FieldChanged.Equals("Status", StringComparison.OrdinalIgnoreCase) && l.Timestamp <= time);
            
            if (lastStatusLog != null)
            {
                return lastStatusLog.NewValue ?? "Open";
            }

            if (issue.CreatedAt > time) return "NotCreated";
            return "Open";
        }

        private void DrawChart(List<StatusCounts> data, int scaleMode)
        {
            if (data.Count == 0 || ChartCanvas.ActualWidth <= 0) return;

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;

            int maxIssues = data.Max(d => d.Total);
            if (maxIssues == 0) maxIssues = 10;
            maxIssues = (int)(maxIssues * 1.1) + 1;

            double xStep = width / (data.Count > 1 ? data.Count - 1 : 1);
            double yStep = height / maxIssues;

            // Grid lines (Horizontal)
            for (int i = 0; i <= 5; i++)
            {
                double y = height - (i * (height / 5));
                ChartCanvas.Children.Add(new Line { X1 = 0, X2 = width, Y1 = y, Y2 = y, Stroke = new SolidColorBrush(Color.FromRgb(48, 54, 61)), StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) });
                
                var label = new TextBlock { Text = ((maxIssues * i) / 5).ToString(), Foreground = Brushes.Gray, FontSize = 10 };
                Canvas.SetLeft(label, -25);
                Canvas.SetTop(label, y - 7);
                ChartCanvas.Children.Add(label);
            }

            // Draw Lines
            DrawLine(data, d => d.Open, Color.FromRgb(255, 82, 82), xStep, yStep, height);
            DrawLine(data, d => d.InProgress, Color.FromRgb(255, 215, 64), xStep, yStep, height);
            DrawLine(data, d => d.Testing, Color.FromRgb(77, 124, 255), xStep, yStep, height);
            DrawLine(data, d => d.Done, Color.FromRgb(76, 175, 80), xStep, yStep, height);

            // X Axis Labels
            int labelStep;
            if (scaleMode == 2) labelStep = data.Count > 1000 ? 120 : (data.Count > 300 ? 60 : 15); 
            else if (scaleMode == 1) labelStep = data.Count > 48 ? 24 : 6;
            else labelStep = data.Count > 14 ? 3 : 1;

            for (int i = 0; i < data.Count; i += labelStep)
            {
                string text = data[i].Time.ToString("HH:mm");
                if (scaleMode == 0) text = data[i].Time.ToString("dd.MM");
                else if (i % (scaleMode == 2 ? 1440 : 24) == 0 && data.Count > (scaleMode == 2 ? 1440 : 24)) 
                    text = data[i].Time.ToString("dd.MM HH:mm");

                var label = new TextBlock { Text = text, Foreground = Brushes.Gray, FontSize = 9 };
                Canvas.SetLeft(label, i * xStep - 15);
                Canvas.SetTop(label, height + 5);
                ChartCanvas.Children.Add(label);
            }
        }

        private void DrawLine(List<StatusCounts> data, Func<StatusCounts, int> selector, Color color, double xStep, double yStep, double canvasHeight)
        {
            var points = new PointCollection();
            for (int i = 0; i < data.Count; i++)
            {
                points.Add(new Point(i * xStep, canvasHeight - (selector(data[i]) * yStep)));
            }

            ChartCanvas.Children.Add(new Polyline { Points = points, Stroke = new SolidColorBrush(color), StrokeThickness = 3, StrokeLineJoin = PenLineJoin.Round });
            
            var areaPoints = new PointCollection(points) { new Point((data.Count - 1) * xStep, canvasHeight), new Point(0, canvasHeight) };
            ChartCanvas.Children.Insert(0, new Polygon { Points = areaPoints, Fill = new SolidColorBrush(color) { Opacity = 0.05 } });
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private class StatusCounts
        {
            public DateTime Time { get; set; }
            public int Open { get; set; }
            public int InProgress { get; set; }
            public int Testing { get; set; }
            public int Done { get; set; }
            public int Doing => InProgress + Testing;
            public int Total => Open + InProgress + Testing + Done;
        }
    }
}
