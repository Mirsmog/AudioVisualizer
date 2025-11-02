using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioVisualizer;

public partial class MainWindow : Window
{
    private AudioCapture? audioCapture;
    private List<Rectangle> bars = new List<Rectangle>();
    private List<double> barHeights = new List<double>();
    private DispatcherTimer updateTimer;
    private const string currentColorMode = "rainbow";
    private const double sensitivity = 15.0; // High sensitivity

    public MainWindow()
    {
        InitializeComponent();
        updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        updateTimer.Tick += UpdateTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeBars();
        StartVisualization();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitializeBars();
    }

    private void InitializeBars()
    {
        VisualizerCanvas.Children.Clear();
        bars.Clear();
        barHeights.Clear();

        double canvasWidth = VisualizerCanvas.ActualWidth;
        double canvasHeight = VisualizerCanvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        // Adaptive bar count based on window width (1 bar per ~10 pixels)
        int barCount = Math.Max(50, (int)(canvasWidth / 10));
        
        double barWidth = (canvasWidth / barCount) * 0.85;
        double spacing = (canvasWidth / barCount) * 0.15;

        for (int i = 0; i < barCount; i++)
        {
            Rectangle bar = new Rectangle
            {
                Width = barWidth,
                Height = 0,
                Fill = GetBarBrush(i, barCount),
                RadiusX = 2,
                RadiusY = 2
            };

            Canvas.SetLeft(bar, i * (barWidth + spacing));
            Canvas.SetBottom(bar, 0);

            VisualizerCanvas.Children.Add(bar);
            bars.Add(bar);
            barHeights.Add(0);
        }
    }

    private Brush GetBarBrush(int index, int total)
    {
        double hue = (double)index / total * 360;
        return new SolidColorBrush(HsvToRgb(hue, 1.0, 1.0));
    }

    private Color HsvToRgb(double h, double s, double v)
    {
        int hi = (int)(h / 60) % 6;
        double f = h / 60 - Math.Floor(h / 60);

        byte vByte = (byte)(v * 255);
        byte p = (byte)(v * (1 - s) * 255);
        byte q = (byte)(v * (1 - f * s) * 255);
        byte t = (byte)(v * (1 - (1 - f) * s) * 255);

        return hi switch
        {
            0 => Color.FromRgb(vByte, t, p),
            1 => Color.FromRgb(q, vByte, p),
            2 => Color.FromRgb(p, vByte, t),
            3 => Color.FromRgb(p, q, vByte),
            4 => Color.FromRgb(t, p, vByte),
            _ => Color.FromRgb(vByte, p, q),
        };
    }

    private void StartVisualization()
    {
        audioCapture = new AudioCapture();
        audioCapture.FftCalculated += OnFftCalculated;
        audioCapture.StartCapture();
        updateTimer.Start();
    }

    private void StopVisualization()
    {
        audioCapture?.StopCapture();
        audioCapture?.Dispose();
        audioCapture = null;
        updateTimer.Stop();
    }

    private void OnFftCalculated(object? sender, float[] fftData)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (bars.Count == 0) return;

            int barCount = bars.Count;
            double canvasHeight = VisualizerCanvas.ActualHeight;

            if (canvasHeight <= 0) return;

            // Mirror spectrum: low frequencies at center, high towards edges
            int half = barCount / 2;
            for (int i = 0; i < barCount; i++)
            {
                int posFromCenter = i < half ? (half - 1 - i) : (i - half);
                double t = posFromCenter / (double)Math.Max(1, half - 1);
                // Nonlinear mapping emphasizes lows
                int fftIndex = (int)(Math.Pow(t, 2) * (fftData.Length / 2));
                fftIndex = Math.Min(fftIndex, fftData.Length / 2 - 1);

                // Use magnitude with simple smoothing factor
                double magnitude = fftData[fftIndex] * sensitivity;
                double targetHeight = Math.Min(magnitude * canvasHeight * 0.9, canvasHeight);
                barHeights[i] = targetHeight;
            }
        });
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        double canvasHeight = VisualizerCanvas.ActualHeight;
        if (canvasHeight <= 0) return;

        for (int i = 0; i < bars.Count; i++)
        {
            double currentHeight = bars[i].Height;
            double targetHeight = barHeights[i];

            // Smooth animation
            double newHeight = currentHeight + (targetHeight - currentHeight) * 0.35;

            // Gravity effect when falling
            if (newHeight < currentHeight)
            {
                newHeight = currentHeight - Math.Max((currentHeight - targetHeight) * 0.15, 5);
            }

            newHeight = Math.Max(0, Math.Min(newHeight, canvasHeight));
            bars[i].Height = newHeight;
            Canvas.SetTop(bars[i], canvasHeight - newHeight);
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        StopVisualization();
    }
}
