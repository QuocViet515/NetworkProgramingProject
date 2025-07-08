using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Pingme.Views.Controls
{
    public partial class ArcSegmentControl : UserControl
    {
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(ArcSegmentControl),
                new PropertyMetadata(0.0, OnAngleChanged));

        public double Angle
        {
            get => (double)GetValue(AngleProperty);
            set => SetValue(AngleProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(Geometry), typeof(ArcSegmentControl),
                new PropertyMetadata(null));

        public Geometry Data
        {
            get => (Geometry)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public ArcSegmentControl()
        {
            InitializeComponent();
            Loaded += ArcSegmentControl_Loaded;
            SizeChanged += ArcSegmentControl_SizeChanged;
        }

        private void ArcSegmentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DrawArc();
        }

        private void ArcSegmentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawArc();
        }

        private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ArcSegmentControl;
            control?.DrawArc();
        }

        private void DrawArc()
        {
            double angle = Angle;
            double width = ActualWidth;
            double height = ActualHeight;
            double radius = Math.Min(width, height) / 2;

            Point center = new Point(width / 2, height / 2);
            Point startPoint = new Point(center.X, center.Y - radius);

            double radians = angle * Math.PI / 180;
            Point endPoint = new Point(
                center.X + radius * Math.Sin(radians),
                center.Y - radius * Math.Cos(radians)
            );

            bool isLargeArc = angle > 180;

            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false
            };

            figure.Segments.Add(new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            ArcPath.Data = geometry;
            Data = geometry; // cập nhật để binding được
        }
    }
}
