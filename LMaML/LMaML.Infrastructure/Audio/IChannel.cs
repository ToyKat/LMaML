﻿using System;

namespace LMaML.Infrastructure.Audio
{
    /// <summary>
    /// IChannel
    /// </summary>
    public interface ITrack : IDisposable
    {
        /// <summary>
        /// Gets the current progress.
        /// </summary>
        /// <value>
        /// The current progress.
        /// </value>
        double CurrentProgress { get; }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Pauses this instance.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes this instance.
        /// </summary>
        void Play(float volume);

        /// <summary>
        /// Seeks the specified offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        void Seek(TimeSpan offset);

        /// <summary>
        /// Seeks to the specified offset (In milliseconds).
        /// </summary>
        /// <param name="offset">The offset.</param>
        void Seek(double offset);

        /// <summary>
        /// Gets a value indicating whether this instance is paused.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is paused; otherwise, <c>false</c>.
        /// </value>
        bool IsPaused { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is playing.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is playing; otherwise, <c>false</c>.
        /// </value>
        bool IsPlaying { get; }

        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        /// <value>
        /// The volume.
        /// </value>
        float Volume { get; set; }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        TimeSpan Length { get; }

        /// <summary>
        /// FFTs the specified channel offset.
        /// </summary>
        /// <param name="channelOffset">The channel offset.</param>
        /// <param name="fftSize">Size of the FFT.</param>
        /// <returns></returns>
        float[] FFT(int channelOffset = -1, int fftSize = 64);

        /// <summary>
        /// Gets a waveform of the specified channel
        /// </summary>
        /// <param name="channelOffset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        float[] GetWaveform(int channelOffset = -1, int size = 256);

        /// <summary>
        /// Gets the sample rate.
        /// </summary>
        /// <value>
        /// The sample rate.
        /// </value>
        float SampleRate { get; }

        /// <summary>
        /// FFTs the stereo.
        /// </summary>
        /// <param name="fftSize">Size of the FFT.</param>
        /// <returns></returns>
        float[] FFTStereo(int fftSize = 64);

        /// <summary>
        /// Gets the current position.
        /// </summary>
        /// <value>
        /// The current position.
        /// </value>
        double CurrentPositionMillisecond { get; }

        /// <summary>
        /// Gets the name of this track.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }
    }
}