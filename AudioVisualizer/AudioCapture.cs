using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Linq;

namespace AudioVisualizer
{
    public class AudioCapture : IDisposable
    {
        private WasapiLoopbackCapture? capture;
        private readonly int fftLength = 2048;
        private readonly Complex[] fftBuffer;
        private readonly float[] fftResult;
        private int bufferPosition = 0;

        public event EventHandler<float[]>? FftCalculated;
        public bool IsCapturing { get; private set; }

        public AudioCapture()
        {
            fftBuffer = new Complex[fftLength];
            fftResult = new float[fftLength / 2];
        }

        public void StartCapture()
        {
            if (IsCapturing) return;

            try
            {
                capture = new WasapiLoopbackCapture();
                capture.DataAvailable += OnDataAvailable;
                capture.RecordingStopped += OnRecordingStopped;
                capture.StartRecording();
                IsCapturing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting capture: {ex.Message}");
                IsCapturing = false;
            }
        }

        public void StopCapture()
        {
            if (!IsCapturing || capture == null) return;

            try
            {
                capture.StopRecording();
                IsCapturing = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping capture: {ex.Message}");
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            for (int i = 0; i < e.BytesRecorded; i += 4)
            {
                if (i + 3 < e.BytesRecorded)
                {
                    float sample = BitConverter.ToSingle(e.Buffer, i);
                    fftBuffer[bufferPosition].X = sample;
                    fftBuffer[bufferPosition].Y = 0;
                    bufferPosition++;

                    if (bufferPosition >= fftLength)
                    {
                        bufferPosition = 0;
                        CalculateFft();
                    }
                }
            }
        }

        private void CalculateFft()
        {
            FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2), fftBuffer);

            for (int i = 0; i < fftLength / 2; i++)
            {
                float real = fftBuffer[i].X;
                float imaginary = fftBuffer[i].Y;
                fftResult[i] = (float)Math.Sqrt(real * real + imaginary * imaginary);
            }

            FftCalculated?.Invoke(this, fftResult);
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            IsCapturing = false;
            if (e.Exception != null)
            {
                System.Diagnostics.Debug.WriteLine($"Recording stopped with error: {e.Exception.Message}");
            }
        }

        public void Dispose()
        {
            StopCapture();
            if (capture != null)
            {
                capture.DataAvailable -= OnDataAvailable;
                capture.RecordingStopped -= OnRecordingStopped;
                capture.Dispose();
                capture = null;
            }
        }
    }
}
